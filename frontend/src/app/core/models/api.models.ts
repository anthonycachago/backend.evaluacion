export type SortDirection = 'Asc' | 'Desc';

/** Respuesta paginada que devuelven ambos microservicios. */
export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

/** Parámetros comunes de paginación y ordenamiento. */
export interface PagedQuery {
  pageNumber: number;
  pageSize: number;
  sortBy?: string | null;
  sortDirection?: SortDirection | null;
}

/** Formato de error RFC 7807 que devuelve el backend. */
export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}
