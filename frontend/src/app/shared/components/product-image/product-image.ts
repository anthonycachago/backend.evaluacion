import { ChangeDetectionStrategy, Component, effect, input, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

/** Imagen de producto con ícono de respaldo cuando no hay URL o la imagen no carga. */
@Component({
  selector: 'app-product-image',
  imports: [MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'product-image' },
  template: `
    @if (src() && !failed()) {
      <img [src]="src()" [alt]="alt()" loading="lazy" (error)="failed.set(true)" />
    } @else {
      <mat-icon [attr.aria-label]="alt()">{{ src() ? 'broken_image' : 'image_not_supported' }}</mat-icon>
    }
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      overflow: hidden;
      border-radius: 8px;
      background: var(--mat-sys-surface-container);
      color: var(--mat-sys-on-surface-variant);
      vertical-align: middle;
    }
    img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }
  `,
})
export class ProductImage {
  readonly src = input<string | null>(null);
  readonly alt = input('');

  protected readonly failed = signal(false);

  constructor() {
    // Si cambia la URL se vuelve a intentar cargarla.
    effect(() => {
      this.src();
      this.failed.set(false);
    });
  }
}
