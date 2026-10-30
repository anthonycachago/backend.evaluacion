import { AbstractControl, FormGroup } from '@angular/forms';

/** Mensaje en español para el primer error de un control (o null si es válido / no se tocó). */
export function fieldErrorMessage(control: AbstractControl | null): string | null {
  if (!control || !control.errors || !(control.touched || control.dirty)) return null;

  const errors = control.errors;
  if (errors['required']) return 'Este campo es obligatorio.';
  if (errors['maxlength']) return `Máximo ${errors['maxlength'].requiredLength} caracteres.`;
  if (errors['min']) return `El valor mínimo es ${errors['min'].min}.`;
  if (errors['max']) return `El valor máximo es ${errors['max'].max}.`;
  if (errors['url']) return 'Ingrese una URL válida que empiece con http:// o https://';
  if (errors['integer']) return 'Debe ser un número entero.';
  if (errors['maxDecimals']) return `Máximo ${errors['maxDecimals'].decimals} decimales.`;
  if (errors['matDatepickerParse']) return 'Fecha con formato inválido.';
  if (errors['matDatepickerMax']) return 'La fecha no puede ser futura.';
  if (errors['productNotSelected']) return 'Seleccione un producto de la lista.';
  if (errors['insufficientStock']) return `Stock insuficiente. Disponible: ${errors['insufficientStock'].available}.`;
  if (errors['server']) return errors['server'];

  return 'Valor inválido.';
}

/**
 * Asigna a cada control los errores de validación devueltos por el backend (400).
 * Devuelve true si al menos un error se pudo asociar a un campo del formulario.
 */
export function applyServerErrors(form: FormGroup, fieldErrors: Record<string, string[]>): boolean {
  let applied = false;

  for (const [field, messages] of Object.entries(fieldErrors)) {
    const control = form.get(field);
    if (!control || messages.length === 0) continue;

    control.setErrors({ ...control.errors, server: messages[0] });
    control.markAsTouched();
    applied = true;
  }

  return applied;
}
