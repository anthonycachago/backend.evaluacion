export type FilterFieldType = 'text' | 'number' | 'select' | 'date';

export interface FilterOption {
  value: string;
  label: string;
}

/** Definición de un filtro que el usuario puede agregar o quitar. */
export interface FilterField {
  key: string;
  label: string;
  type: FilterFieldType;
  options?: FilterOption[];
  min?: number;
  placeholder?: string;
}

/** Filtros aplicados: solo contiene los que tienen valor. */
export type FilterValues = Record<string, string | number | Date>;
