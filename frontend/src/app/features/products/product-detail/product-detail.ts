import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';
import { filter, switchMap } from 'rxjs';
import { Product } from '../../../core/models/product.models';
import { NotificationService } from '../../../core/services/notification.service';
import { ProductService } from '../../../core/services/product.service';
import { ConfirmDialogService } from '../../../shared/components/confirm-dialog/confirm-dialog.service';
import { ProductImage } from '../../../shared/components/product-image/product-image';

/** Consulta de solo lectura de la información de un producto. */
@Component({
  selector: 'app-product-detail',
  imports: [CurrencyPipe, DatePipe, RouterLink, MatButtonModule, MatIconModule, MatProgressBarModule, ProductImage],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductDetail {
  private readonly productService = inject(ProductService);
  private readonly notification = inject(NotificationService);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly router = inject(Router);

  readonly id = input.required<string>();

  protected readonly productResource = rxResource({
    params: () => this.id(),
    stream: ({ params: id }) => this.productService.getById(id),
  });

  constructor() {
    effect(() => {
      const error = this.productResource.error();
      if (error) this.notification.error(error);
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
          this.router.navigate(['/products']);
        },
        error: (error) => this.notification.error(error),
      });
  }
}
