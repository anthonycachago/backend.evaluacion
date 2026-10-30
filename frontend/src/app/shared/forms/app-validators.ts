import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

const HTTP_URL = /^https?:\/\/[^\s/$.?#].[^\s]*$/i;

export function isHttpUrl(value: string): boolean {
  return HTTP_URL.test(value.trim());
}

export class AppValidators {
  /** Obligatorio y sin aceptar solo espacios en blanco. */
  static notBlank(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (value === null || value === undefined) return { required: true };
    return typeof value === 'string' && value.trim().length === 0 ? { required: true } : null;
  }

  /** URL http(s) válida (opcional: si está vacío no valida). */
  static httpUrl(control: AbstractControl): ValidationErrors | null {
    const value = control.value as string | null;
    if (!value) return null;
    return isHttpUrl(value) ? null : { url: true };
  }

  /** Número entero. */
  static integer(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (value === null || value === undefined || value === '') return null;
    return Number.isInteger(Number(value)) ? null : { integer: true };
  }

  /** Como máximo N decimales. */
  static maxDecimals(decimals: number): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;
      if (value === null || value === undefined || value === '') return null;
      const [, fraction = ''] = String(value).split('.');
      return fraction.length > decimals ? { maxDecimals: { decimals } } : null;
    };
  }
}
