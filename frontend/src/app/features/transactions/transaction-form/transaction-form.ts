import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, input, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';
import { Observable, debounceTime, distinctUntilChanged, filter, map, of, switchMap } from 'rxjs';
import { ApiError } from '../../../core/http/api-error';
import { Product } from '../../../core/models/product.models';
import {
  TRANSACTION_TYPE_LABELS,
  Transaction,
  TransactionCreateRequest,
  TransactionType,
  stockEffect,
} from '../../../core/models/transaction.models';
import { NotificationService } from '../../../core/services/notification.service';
import { ProductService } from '../../../core/services/product.service';
import { TransactionService } from '../../../core/services/transaction.service';
import { AppValidators } from '../../../shared/forms/app-validators';
import { applyServerErrors, fieldErrorMessage } from '../../../shared/forms/form-errors';
import { isSameDay, transactionDateIso } from '../../../shared/utils/date-utils';
import { saleWithinStockValidator, selectedOptionValidator } from './stock-validators';

/** Datos con los que se inicializa el formulario: transacción a editar y/o producto preseleccionado. */
interface InitialData {
  transaction: Transaction | null;
  product: Product;
}

@Component({
  selector: 'app-transaction-form',
  imports: [
    CurrencyPipe,
    ReactiveFormsModule,
    RouterLink,
    MatAutocompleteModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
  ],
  templateUrl: './transaction-form.html',
  styleUrl: './transaction-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransactionForm {
  private readonly transactionService = inject(TransactionService);
  private readonly productService = inject(ProductService);
  private readonly notification = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** Id de la transacción (parámetro de ruta). Si no viene, el formulario es de creación. */
  readonly id = input<string>();
  /** Producto preseleccionado (query param ?productId=...). */
  readonly productId = input<string>();

  protected readonly isEdit = computed(() => !!this.id());
  protected readonly saving = signal(false);
  protected readonly today = new Date();
  protected readonly typeLabels = TRANSACTION_TYPE_LABELS;
  protected readonly fieldErrorMessage = fieldErrorMessage;

  /** Transacción original (solo en edición): su efecto se revierte al calcular el stock disponible. */
  protected readonly original = signal<Transaction | null>(null);
  protected readonly selectedProduct = signal<Product | null>(null);

  /** Stock disponible para esta operación, considerando la transacción original al editar. */
  protected readonly availableStock = computed(() => {
    const product = this.selectedProduct();
    if (!product) return null;

    const original = this.original();
    return original && original.productId === product.id
      ? product.stock - stockEffect(original.type, original.quantity)
      : product.stock;
  });

  protected readonly form = new FormGroup({
    product: new FormControl<Product | string | null>(null, [selectedOptionValidator]),
    type: new FormControl<TransactionType>('Sale', { nonNullable: true }),
    date: new FormControl<Date | null>(new Date(), [Validators.required]),
    quantity: new FormControl<number | null>(null, [
      Validators.required,
      Validators.min(1),
      AppValidators.integer,
      saleWithinStockValidator(() => this.availableStock()),
    ]),
    unitPrice: new FormControl<number | null>(null, [Validators.required, Validators.min(0.01), AppValidators.maxDecimals(2)]),
    detail: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(500)] }),
  });

  private readonly formValue = toSignal(this.form.valueChanges.pipe(map(() => this.form.getRawValue())), {
    initialValue: this.form.getRawValue(),
  });

  protected readonly total = computed(() => {
    const { quantity, unitPrice } = this.formValue();
    return quantity && unitPrice ? Math.round(quantity * unitPrice * 100) / 100 : 0;
  });

  protected readonly resultingStock = computed(() => {
    const available = this.availableStock();
    const { quantity, type } = this.formValue();
    if (available === null) return null;
    return available + (quantity ? stockEffect(type, quantity) : 0);
  });

  // ---- Autocompletado de productos ----
  private readonly searchTerm = toSignal(
    this.form.controls.product.valueChanges.pipe(
      filter((value): value is string => typeof value === 'string'),
      debounceTime(300),
      distinctUntilChanged(),
    ),
    { initialValue: '' },
  );

  protected readonly productOptions = rxResource({
    params: () => this.searchTerm(),
    stream: ({ params: search }) =>
      this.productService
        .search({ search, pageNumber: 1, pageSize: 10, sortBy: 'name', sortDirection: 'Asc' })
        .pipe(map((page) => page.items)),
  });

  protected readonly displayProduct = (value: Product | string | null): string =>
    typeof value === 'string' ? value : (value?.name ?? '');

  // ---- Carga inicial ----
  protected readonly initialResource = rxResource<InitialData | null, { id?: string; productId?: string }>({
    params: () => ({ id: this.id(), productId: this.productId() }),
    stream: ({ params }): Observable<InitialData | null> => {
      if (params.id) {
        return this.transactionService.getById(params.id).pipe(
          switchMap((transaction) =>
            this.productService.getById(transaction.productId).pipe(map((product) => ({ transaction, product }))),
          ),
        );
      }
      if (params.productId) {
        return this.productService.getById(params.productId).pipe(map((product) => ({ transaction: null, product })));
      }
      return of(null);
    },
  });

  constructor() {
    effect(() => {
      const initial = this.initialResource.hasValue() ? this.initialResource.value() : null;
      if (!initial) return;

      if (initial.transaction) this.fillForm(initial.transaction, initial.product);
      else this.selectProduct(initial.product);
    });

    effect(() => {
      const error = this.initialResource.error();
      if (error) this.notification.error(error);
    });

    // Al cambiar compra/venta se vuelve a validar la cantidad contra el stock.
    this.form.controls.type.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.form.controls.quantity.updateValueAndValidity());

    // Si el usuario vuelve a escribir en el campo producto, se pierde la selección.
    this.form.controls.product.valueChanges
      .pipe(
        filter((value) => typeof value === 'string'),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.selectedProduct.set(null);
        this.form.controls.quantity.updateValueAndValidity();
      });
  }

  protected onProductSelected(event: MatAutocompleteSelectedEvent): void {
    this.selectProduct(event.option.value as Product);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notification.error('Revise los campos marcados en el formulario.');
      return;
    }

    const value = this.form.getRawValue();
    const product = value.product as Product;
    const request: TransactionCreateRequest = {
      date: this.dateToSend(value.date!),
      type: value.type,
      productId: product.id,
      quantity: value.quantity!,
      unitPrice: value.unitPrice!,
      detail: value.detail.trim() || null,
    };

    const id = this.id();
    const operation = id
      ? this.transactionService.update(id, { ...request, date: request.date!, unitPrice: request.unitPrice! })
      : this.transactionService.create(request);

    this.saving.set(true);
    operation.subscribe({
      next: (transaction) => {
        this.saving.set(false);
        const action = id ? 'actualizada' : 'registrada';
        this.notification.success(
          `${TRANSACTION_TYPE_LABELS[transaction.type]} ${action} correctamente. ` +
            `Stock actual de "${transaction.productName}": ${transaction.productStock}.`,
        );
        this.router.navigate(['/transactions']);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.handleSaveError(error, product.id);
      },
    });
  }

  private handleSaveError(error: unknown, productId: string): void {
    if (error instanceof ApiError) {
      const { productId: productErrors, ...otherErrors } = error.fieldErrors;
      applyServerErrors(this.form, productErrors ? { ...otherErrors, product: productErrors } : otherErrors);

      // Si el stock cambió mientras se completaba el formulario, se refresca el producto.
      if (error.status === 422) {
        this.productService.getById(productId).subscribe((product) => {
          this.selectedProduct.set(product);
          this.form.controls.quantity.updateValueAndValidity();
          this.form.controls.quantity.markAsTouched();
        });
      }
    }
    this.notification.error(error);
  }

  private selectProduct(product: Product): void {
    this.form.controls.product.setValue(product, { emitEvent: false });
    this.selectedProduct.set(product);

    // Al crear, se propone el precio actual del producto.
    if (!this.isEdit() && !this.form.controls.unitPrice.value) {
      this.form.controls.unitPrice.setValue(product.price);
    }
    this.form.controls.quantity.updateValueAndValidity();
  }

  private fillForm(transaction: Transaction, product: Product): void {
    this.original.set(transaction);
    this.selectedProduct.set(product);
    this.form.reset({
      product,
      type: transaction.type,
      date: new Date(transaction.date),
      quantity: transaction.quantity,
      unitPrice: transaction.unitPrice,
      detail: transaction.detail ?? '',
    });
  }

  /** Al editar sin cambiar el día se conserva la fecha y hora original. */
  private dateToSend(selected: Date): string {
    const original = this.original();
    if (original && isSameDay(new Date(original.date), selected)) return original.date;
    return transactionDateIso(selected);
  }
}
