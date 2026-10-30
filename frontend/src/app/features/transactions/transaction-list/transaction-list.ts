import { ChangeDetectionStrategy, Component, computed, effect, inject, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Sort } from '@angular/material/sort';
import { RouterLink } from '@angular/router';
import { PagedResult, SortDirection } from '../../../core/models/api.models';
import { Transaction, TransactionQuery } from '../../../core/models/transaction.models';
import { NotificationService } from '../../../core/services/notification.service';
import { ProductService } from '../../../core/services/product.service';
import { TransactionService } from '../../../core/services/transaction.service';
import { DynamicFilter } from '../../../shared/components/dynamic-filter/dynamic-filter';
import { FilterValues } from '../../../shared/components/dynamic-filter/dynamic-filter.models';
import { TransactionDeleteFlow } from '../transaction-delete';
import { toTransactionQueryFilters, transactionFilterFields } from '../transaction-filters';
import { TransactionTable } from '../transaction-table/transaction-table';

@Component({
  selector: 'app-transaction-list',
  imports: [RouterLink, MatButtonModule, MatIconModule, MatPaginatorModule, MatProgressBarModule, DynamicFilter, TransactionTable],
  templateUrl: './transaction-list.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransactionList {
  private readonly transactionService = inject(TransactionService);
  private readonly productService = inject(ProductService);
  private readonly notification = inject(NotificationService);
  private readonly deleteFlow = inject(TransactionDeleteFlow);

  private readonly filters = signal<Partial<TransactionQuery>>({});
  private readonly sort = signal<{ sortBy: string | null; sortDirection: SortDirection | null }>({
    sortBy: null,
    sortDirection: null,
  });
  protected readonly paging = signal({ pageNumber: 1, pageSize: 10 });

  private readonly query = computed<TransactionQuery>(() => ({ ...this.filters(), ...this.sort(), ...this.paging() }));

  protected readonly transactionsResource = rxResource({
    params: () => this.query(),
    stream: ({ params }) => this.transactionService.search(params),
  });

  protected readonly page = linkedSignal<PagedResult<Transaction> | undefined, PagedResult<Transaction> | undefined>({
    source: () => (this.transactionsResource.hasValue() ? this.transactionsResource.value() : undefined),
    computation: (current, previous) => current ?? previous?.value,
  });

  /** Productos para el filtro (los 100 primeros por nombre). */
  private readonly productsResource = rxResource({
    stream: () => this.productService.search({ pageNumber: 1, pageSize: 100, sortBy: 'name', sortDirection: 'Asc' }),
  });

  protected readonly filterFields = computed(() =>
    transactionFilterFields(
      (this.productsResource.hasValue() ? this.productsResource.value().items : []).map((p) => ({ value: p.id, label: p.name })),
    ),
  );

  constructor() {
    effect(() => {
      const error = this.transactionsResource.error();
      if (error) this.notification.error(error);
    });
  }

  protected onFiltersChange(values: FilterValues): void {
    this.filters.set(toTransactionQueryFilters(values));
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

  protected delete(transaction: Transaction): void {
    this.deleteFlow.run(transaction).subscribe({
      next: () => {
        this.notification.success('Transacción eliminada y stock actualizado correctamente.');
        const current = this.page();
        if (current && current.items.length === 1 && current.pageNumber > 1) {
          this.paging.update((p) => ({ ...p, pageNumber: p.pageNumber - 1 }));
        }
        this.transactionsResource.reload();
      },
      error: (error) => this.notification.error(error),
    });
  }
}
