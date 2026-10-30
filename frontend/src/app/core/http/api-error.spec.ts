import { HttpErrorResponse } from '@angular/common/http';
import { ApiError, errorMessage } from './api-error';

describe('ApiError', () => {
  it('usa el detalle del ProblemDetails como mensaje', () => {
    const error = ApiError.fromHttpError(
      new HttpErrorResponse({
        status: 422,
        error: { title: 'Regla de negocio incumplida', detail: 'Stock insuficiente para "Mouse".' },
      }),
    );

    expect(error.status).toBe(422);
    expect(errorMessage(error)).toBe('Stock insuficiente para "Mouse".');
  });

  it('conserva los errores por campo de una validación 400', () => {
    const error = ApiError.fromHttpError(
      new HttpErrorResponse({
        status: 400,
        error: { detail: 'Uno o más campos no son válidos.', errors: { name: ['El nombre es obligatorio.'] } },
      }),
    );

    expect(error.fieldErrors).toEqual({ name: ['El nombre es obligatorio.'] });
    expect(errorMessage(error)).toContain('El nombre es obligatorio.');
  });

  it('indica cuando el servidor no está disponible', () => {
    const error = ApiError.fromHttpError(new HttpErrorResponse({ status: 0 }));

    expect(error.message).toContain('No se pudo conectar');
  });
});
