import { CurrencyPipe } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { Observable, filter, switchMap } from 'rxjs';
import { TRANSACTION_TYPE_LABELS, Transaction } from '../../core/models/transaction.models';
import { TransactionService } from '../../core/services/transaction.service';
import { ConfirmDialogService } from '../../shared/components/confirm-dialog/confirm-dialog.service';

/** Confirma y elimina una transacción. Emite solo si se eliminó. */
@Injectable({ providedIn: 'root' })
export class TransactionDeleteFlow {
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly transactionService = inject(TransactionService);
  private readonly currency = new CurrencyPipe('es-EC');

  run(transaction: Transaction): Observable<void> {
    const type = TRANSACTION_TYPE_LABELS[transaction.type].toLowerCase();
    const effect = transaction.type === 'Purchase' ? 'restará' : 'devolverá';

    return this.confirmDialog
      .confirm({
        title: 'Eliminar transacción',
        message:
          `¿Eliminar la ${type} de ${transaction.quantity} unidad(es) por ${this.currency.transform(transaction.totalPrice, 'USD')}? ` +
          `Se ${effect} ${transaction.quantity} unidad(es) al stock del producto.`,
        confirmText: 'Eliminar',
      })
      .pipe(
        filter(Boolean),
        switchMap(() => this.transactionService.delete(transaction.id)),
      );
  }
}
