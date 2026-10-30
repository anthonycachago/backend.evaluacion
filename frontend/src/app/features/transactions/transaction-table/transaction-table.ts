import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { TRANSACTION_TYPE_LABELS, Transaction } from '../../../core/models/transaction.models';

/** Tabla de transacciones con ordenamiento y botones de acción. La paginación la maneja el contenedor. */
@Component({
  selector: 'app-transaction-table',
  imports: [CurrencyPipe, DatePipe, RouterLink, MatButtonModule, MatIconModule, MatSortModule, MatTableModule, MatTooltipModule],
  templateUrl: './transaction-table.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransactionTable {
  readonly transactions = input.required<Transaction[]>();
  /** Oculta la columna de producto (por ejemplo, en el historial de un producto). */
  readonly showProduct = input(true);

  readonly sortChange = output<Sort>();
  readonly deleteRequested = output<Transaction>();

  protected readonly typeLabels: Record<string, string> = TRANSACTION_TYPE_LABELS;
  protected readonly displayedColumns = computed(() =>
    ['date', 'type', this.showProduct() ? 'product' : null, 'quantity', 'unitPrice', 'totalPrice', 'detail', 'actions'].filter(
      (column): column is string => column !== null,
    ),
  );
}
