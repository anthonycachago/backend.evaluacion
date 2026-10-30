import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Sort } from '@angular/material/sort';
import { RouterLink } from '@angular/router';
import { SortDirection } from '../../../core/models/api.models';
import { ProductTransactionHistory, Transaction, TransactionQuery } from '../../../core/models/transaction.models';
import { NotificationService } from '../../../core/services/notification.service';
import { TransactionService } from '../../../core/services/transaction.service';
import { DynamicFilter } from '../../../shared/components/dynamic-filter/dynamic-filter';
import { FilterValues } from '../../../shared/components/dynamic-filter/dynamic-filter.models';
import { TransactionDeleteFlow } from '../../transactions/transaction-delete';
import { toTransactionQueryFilters, transactionFilterFields } from '../../transactions/transaction-filters';
import { TransactionTable } from '../../transactions/transaction-table/transaction-table';

/** Historial de transacciones de un producto con su nombre y stock, filtrable por fechas y tipo. */
@Component({
  selector: 'app-product-history',
  imports: [
    CurrencyPipe,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
    DynamicFilter,
    TransactionTable,
  ],
  templateUrl: './product-history.html',
  styleUrl: './product-history.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductHistory {
  private readonly transactionService = inject(TransactionService);
  private readonly notification = inject(NotificationService);
  private readonly deleteFlow = inject(TransactionDeleteFlow);

  readonly id = input.required<string>();

  protected readonly filterFields = transactionFilterFields();

  private readonly filters = signal<Partial<TransactionQuery>>({});
  private readonly sort = signal<{ sortBy: string | null; sortDirection: SortDirection | null }>({
    sortBy: null,
    sortDirection: null,
  });
  protected readonly paging = signal({ pageNumber: 1, pageSize: 10 });

  protected readonly historyResource = rxResource({
    params: () => ({ id: this.id(), query: { ...this.filters(), ...this.sort(), ...this.paging() } as TransactionQuery }),
    stream: ({ params }) => this.transactionService.getProductHistory(params.id, params.query),
  });

  protected readonly history = linkedSignal<ProductTransactionHistory | undefined, ProductTransactionHistory | undefined>({
    source: () => (this.historyResource.hasValue() ? this.historyResource.value() : undefined),
    computation: (current, previous) => current ?? previous?.value,
  });

  protected readonly product = computed(() => this.history()?.product);
  protected readonly page = computed(() => this.history()?.transactions);

  constructor() {
    effect(() => {
      const error = this.historyResource.error();
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
        this.historyResource.reload();
      },
      error: (error) => this.notification.error(error),
    });
  }
}
