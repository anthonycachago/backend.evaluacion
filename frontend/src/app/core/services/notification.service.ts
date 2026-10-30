import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { errorMessage } from '../http/api-error';

/** Mensajes de éxito o error de las operaciones. */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.snackBar.open(message, 'Cerrar', {
      duration: 4000,
      panelClass: 'snackbar-success',
      horizontalPosition: 'right',
      verticalPosition: 'top',
    });
  }

  error(errorOrMessage: unknown): void {
    const message = typeof errorOrMessage === 'string' ? errorOrMessage : errorMessage(errorOrMessage);

    this.snackBar.open(message, 'Cerrar', {
      duration: 7000,
      panelClass: 'snackbar-error',
      horizontalPosition: 'right',
      verticalPosition: 'top',
    });
  }
}
