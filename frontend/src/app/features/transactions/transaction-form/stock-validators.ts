import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { TransactionType } from '../../../core/models/transaction.models';

/** El valor del control debe ser un objeto seleccionado de la lista (no texto escrito). */
export function selectedOptionValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value;
  if (value === null || value === undefined || value === '') return { required: true };
  return typeof value === 'object' && 'id' in value ? null : { productNotSelected: true };
}

/**
 * Validación compleja: una venta no puede superar el stock disponible.
 * Se aplica al control de cantidad y lee el tipo desde el formulario padre.
 *
 * @param availableStock función que devuelve el stock disponible para la operación
 *                       (o null si aún no hay producto seleccionado).
 */
export function saleWithinStockValidator(availableStock: () => number | null): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const type = control.parent?.get('type')?.value as TransactionType | undefined;
    const quantity = Number(control.value);
    const available = availableStock();

    if (type !== 'Sale' || available === null || !quantity) return null;
    return quantity > available ? { insufficientStock: { available: Math.max(available, 0) } } : null;
  };
}
