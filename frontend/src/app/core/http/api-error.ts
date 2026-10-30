import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../models/api.models';

/** Error normalizado de la API: mensaje legible y errores por campo (si los hay). */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly fieldErrors: Record<string, string[]> = {},
  ) {
    super(message);
    this.name = 'ApiError';
  }

  static fromHttpError(error: HttpErrorResponse): ApiError {
    if (error.status === 0) {
      return new ApiError(0, 'No se pudo conectar con el servidor. Verifique que los servicios estén en ejecución.');
    }

    const problem = (typeof error.error === 'object' ? error.error : null) as ProblemDetails | null;
    const fieldErrors = problem?.errors ?? {};
    const message = problem?.detail ?? problem?.title ?? defaultMessage(error.status);

    return new ApiError(error.status, message, fieldErrors);
  }
}

/** Obtiene un mensaje mostrable a partir de cualquier error. */
export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const fieldMessages = Object.values(error.fieldErrors).flat();
    return fieldMessages.length > 0 ? `${error.message} ${fieldMessages.join(' ')}` : error.message;
  }

  return 'Ocurrió un error inesperado.';
}

function defaultMessage(status: number): string {
  switch (status) {
    case 400:
      return 'La solicitud no es válida.';
    case 404:
      return 'El recurso solicitado no existe.';
    case 422:
      return 'La operación no cumple las reglas de negocio.';
    case 503:
      return 'Un servicio dependiente no está disponible.';
    default:
      return 'Ocurrió un error inesperado en el servidor.';
  }
}
