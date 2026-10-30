import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';
import { of } from 'rxjs';
import { ApiError } from '../../../core/http/api-error';
import { Product, ProductCreateRequest } from '../../../core/models/product.models';
import { NotificationService } from '../../../core/services/notification.service';
import { ProductService } from '../../../core/services/product.service';
import { AppValidators, isHttpUrl } from '../../../shared/forms/app-validators';
import { applyServerErrors, fieldErrorMessage } from '../../../shared/forms/form-errors';

@Component({
  selector: 'app-product-form',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatAutocompleteModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
  ],
  templateUrl: './product-form.html',
  styleUrl: './product-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductForm {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly productService = inject(ProductService);
  private readonly notification = inject(NotificationService);
  private readonly router = inject(Router);

  /** Id del producto (parámetro de ruta). Si no viene, el formulario es de creación. */
  readonly id = input<string>();
  protected readonly isEdit = computed(() => !!this.id());
  protected readonly saving = signal(false);

  protected readonly form = this.fb.group({
    name: ['', [AppValidators.notBlank, Validators.maxLength(150)]],
    category: ['', [AppValidators.notBlank, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(1000)]],
    imageUrl: ['', [AppValidators.httpUrl, Validators.maxLength(500)]],
    price: this.fb.control<number | null>(null, [Validators.required, Validators.min(0.01), AppValidators.maxDecimals(2)]),
    stock: this.fb.control<number | null>(0, [Validators.required, Validators.min(0), AppValidators.integer]),
  });

  protected readonly productResource = rxResource({
    params: () => this.id(),
    stream: ({ params: id }) => (id ? this.productService.getById(id) : of(null)),
  });

  private readonly categoriesResource = rxResource({ stream: () => this.productService.getCategories() });
  private readonly categoryValue = toSignal(this.form.controls.category.valueChanges, { initialValue: '' });
  protected readonly categorySuggestions = computed(() => {
    const categories = this.categoriesResource.hasValue() ? this.categoriesResource.value() : [];
    const term = this.categoryValue().trim().toLowerCase();
    return categories.filter((c) => c.toLowerCase().includes(term));
  });

  private readonly imageUrlValue = toSignal(this.form.controls.imageUrl.valueChanges, { initialValue: '' });
  protected readonly imagePreview = computed(() => {
    const url = this.imageUrlValue().trim();
    return url && isHttpUrl(url) ? url : null;
  });
  protected readonly imageFailed = signal(false);

  protected readonly fieldErrorMessage = fieldErrorMessage;

  constructor() {
    effect(() => {
      const product = this.productResource.hasValue() ? this.productResource.value() : null;
      if (product) this.fillForm(product);
    });

    effect(() => {
      const error = this.productResource.error();
      if (error) this.notification.error(error);
    });

    effect(() => {
      // El stock solo se define al crear; luego cambia exclusivamente con transacciones.
      if (this.isEdit()) this.form.controls.stock.disable();
      else this.form.controls.stock.enable();
    });

    effect(() => {
      this.imagePreview();
      this.imageFailed.set(false);
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notification.error('Revise los campos marcados en el formulario.');
      return;
    }

    const value = this.form.getRawValue();
    const request: ProductCreateRequest = {
      name: value.name.trim(),
      category: value.category.trim(),
      description: value.description.trim() || null,
      imageUrl: value.imageUrl.trim() || null,
      price: value.price!,
      stock: value.stock!,
    };

    const id = this.id();
    const operation = id ? this.productService.update(id, request) : this.productService.create(request);

    this.saving.set(true);
    operation.subscribe({
      next: (product) => {
        this.saving.set(false);
        this.notification.success(
          id ? `Producto "${product.name}" actualizado correctamente.` : `Producto "${product.name}" creado correctamente.`,
        );
        this.router.navigate(['/products']);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        if (error instanceof ApiError) applyServerErrors(this.form, error.fieldErrors);
        this.notification.error(error);
      },
    });
  }

  private fillForm(product: Product): void {
    this.form.reset({
      name: product.name,
      category: product.category,
      description: product.description ?? '',
      imageUrl: product.imageUrl ?? '',
      price: product.price,
      stock: product.stock,
    });
  }
}
