import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { filter, switchMap } from 'rxjs';
import { PagedResult, SortDirection } from '../../../core/models/api.models';
import { Product, ProductQuery } from '../../../core/models/product.models';
import { NotificationService } from '../../../core/services/notification.service';
import { ProductService } from '../../../core/services/product.service';
import { ConfirmDialogService } from '../../../shared/components/confirm-dialog/confirm-dialog.service';
import { DynamicFilter } from '../../../shared/components/dynamic-filter/dynamic-filter';
import { FilterField, FilterValues } from '../../../shared/components/dynamic-filter/dynamic-filter.models';
import { ProductImage } from '../../../shared/components/product-image/product-image';

interface ColumnOption {
  key: string;
  label: string;
}

const LOW_STOCK_THRESHOLD = 5;

@Component({
  selector: 'app-product-list',
  imports: [
    CurrencyPipe,
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatCheckboxModule,
    MatIconModule,
    MatMenuModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSortModule,
    MatTableModule,
    MatTooltipModule,
    DynamicFilter,
    ProductImage,
  ],
  templateUrl: './product-list.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductList {
  private readonly productService = inject(ProductService);
  private readonly notification = inject(NotificationService);
  private readonly confirmDialog = inject(ConfirmDialogService);

  protected readonly lowStockThreshold = LOW_STOCK_THRESHOLD;

  /** Columnas que el usuario puede mostrar u ocultar. */
  protected readonly columnOptions: ColumnOption[] = [
    { key: 'image', label: 'Imagen' },
    { key: 'name', label: 'Nombre' },
    { key: 'category', label: 'Categoría' },
    { key: 'description', label: 'Descripción' },
    { key: 'price', label: 'Precio' },
    { key: 'stock', label: 'Stock' },
    { key: 'createdAt', label: 'Creado' },
  ];
  protected readonly visibleColumns = signal(new Set(['image', 'name', 'category', 'price', 'stock']));
  protected readonly displayedColumns = computed(() => [
    ...this.columnOptions.map((c) => c.key).filter((key) => this.visibleColumns().has(key)),
    'actions',
  ]);

  // ---- Estado de la consulta (filtros + orden + página) ----
  private readonly filters = signal<Partial<ProductQuery>>({});
  private readonly sort = signal<{ sortBy: string | null; sortDirection: SortDirection | null }>({
    sortBy: null,
    sortDirection: null,
  });
  protected readonly paging = signal({ pageNumber: 1, pageSize: 10 });

  private readonly query = computed<ProductQuery>(() => ({ ...this.filters(), ...this.sort(), ...this.paging() }));

  protected readonly productsResource = rxResource({
    params: () => this.query(),
    stream: ({ params }) => this.productService.search(params),
  });

  /** Mantiene la página anterior visible mientras se carga la siguiente. */
  protected readonly page = linkedSignal<PagedResult<Product> | undefined, PagedResult<Product> | undefined>({
    source: () => (this.productsResource.hasValue() ? this.productsResource.value() : undefined),
    computation: (current, previous) => current ?? previous?.value,
  });

  private readonly categoriesResource = rxResource({ stream: () => this.productService.getCategories() });

  protected readonly filterFields = computed<FilterField[]>(() => [
    { key: 'search', label: 'Nombre o descripción', type: 'text', placeholder: 'Ej. mouse' },
    {
      key: 'category',
      label: 'Categoría',
      type: 'select',
      options: (this.categoriesResource.hasValue() ? this.categoriesResource.value() : []).map((c) => ({
        value: c,
        label: c,
      })),
    },
    { key: 'minPrice', label: 'Precio mínimo', type: 'number', min: 0 },
    { key: 'maxPrice', label: 'Precio máximo', type: 'number', min: 0 },
    { key: 'minStock', label: 'Stock mínimo', type: 'number', min: 0 },
    { key: 'maxStock', label: 'Stock máximo', type: 'number', min: 0 },
  ]);

  constructor() {
    effect(() => {
      const error = this.productsResource.error();
      if (error) this.notification.error(error);
    });
  }

  protected onFiltersChange(values: FilterValues): void {
    this.filters.set(values as Partial<ProductQuery>);
    this.paging.update((p) => ({ ...p, pageNumber: 1 }));
  }

  protected onSortChange(sort: Sort): void {
    this.sort.set({
      sortBy: sort.direction ? sort.active : null,
      sortDirection: sort.direction ? (sort.direction === 'desc' ? 'Desc' : 'Asc') : null,
    });
    this.paging.update((p) => ({ ...p, pageNumber: 1 }));
  }

  protected onPageChange(event: PageEvent): void {
    this.paging.set({ pageNumber: event.pageIndex + 1, pageSize: event.pageSize });
  }

  protected toggleColumn(key: string): void {
    this.visibleColumns.update((current) => {
      const next = new Set(current);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  }

  protected delete(product: Product): void {
    this.confirmDialog
      .confirm({
        title: 'Eliminar producto',
        message: `¿Seguro que desea eliminar "${product.name}"? Su historial de transacciones se conservará.`,
        confirmText: 'Eliminar',
      })
      .pipe(
        filter(Boolean),
        switchMap(() => this.productService.delete(product.id)),
      )
      .subscribe({
        next: () => {
          this.notification.success(`Producto "${product.name}" eliminado correctamente.`);
          this.goBackIfPageBecomesEmpty();
          this.productsResource.reload();
        },
        error: (error) => this.notification.error(error),
      });
  }

  private goBackIfPageBecomesEmpty(): void {
    const current = this.page();
    if (current && current.items.length === 1 && current.pageNumber > 1) {
      this.paging.update((p) => ({ ...p, pageNumber: p.pageNumber - 1 }));
    }
  }
}
