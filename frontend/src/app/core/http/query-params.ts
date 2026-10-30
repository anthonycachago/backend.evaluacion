import { HttpParams } from '@angular/common/http';

type QueryValue = string | number | boolean | null | undefined;

/** Convierte un objeto de filtros en HttpParams omitiendo los valores vacíos. */
export function toHttpParams(query: object): HttpParams {
  let params = new HttpParams();

  for (const [key, value] of Object.entries(query) as [string, QueryValue][]) {
    if (value === null || value === undefined) continue;
    if (typeof value === 'string' && value.trim() === '') continue;

    params = params.set(key, String(value));
  }

  return params;
}
