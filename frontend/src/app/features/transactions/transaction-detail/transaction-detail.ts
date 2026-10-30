import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';
import { TRANSACTION_TYPE_LABELS, Transaction } from '../../../core/models/transaction.models';
import { NotificationService } from '../../../core/services/notification.service';
import { TransactionService } from '../../../core/services/transaction.service';
import { TransactionDeleteFlow } from '../transaction-delete';

/** Consulta de solo lectura de una transacción. */
@Component({
  selector: 'app-transaction-detail',
  imports: [CurrencyPipe, DatePipe, RouterLink, MatButtonModule, MatIconModule, MatProgressBarModule],
  templateUrl: './transaction-detail.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransactionDetail {
  private readonly transactionService = inject(TransactionService);
  private readonly notification = inject(NotificationService);
  private readonly deleteFlow = inject(TransactionDeleteFlow);
  private readonly router = inject(Router);

  readonly id = input.required<string>();

  protected readonly typeLabels = TRANSACTION_TYPE_LABELS;

  protected readonly transactionResource = rxResource({
    params: () => this.id(),
    stream: ({ params: id }) => this.transactionService.getById(id),
  });

  constructor() {
    effect(() => {
      const error = this.transactionResource.error();
      if (error) this.notification.error(error);
    });
  }

  protected delete(transaction: Transaction): void {
    this.deleteFlow.run(transaction).subscribe({
      next: () => {
        this.notification.success('Transacción eliminada y stock actualizado correctamente.');
        this.router.navigate(['/transactions']);
      },
      error: (error) => this.notification.error(error),
    });
  }
}
