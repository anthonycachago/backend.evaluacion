import { TRANSACTION_TYPE_LABELS, TransactionQuery, TransactionType } from '../../core/models/transaction.models';
import { FilterField, FilterOption, FilterValues } from '../../shared/components/dynamic-filter/dynamic-filter.models';
import { endOfDayIso, startOfDayIso } from '../../shared/utils/date-utils';

const TYPE_OPTIONS: FilterOption[] = (Object.keys(TRANSACTION_TYPE_LABELS) as TransactionType[]).map((type) => ({
  value: type,
  label: TRANSACTION_TYPE_LABELS[type],
}));

/** Filtros disponibles para transacciones. Si se pasan productos, se agrega el filtro por producto. */
export function transactionFilterFields(productOptions?: FilterOption[]): FilterField[] {
  const fields: FilterField[] = [
    { key: 'type', label: 'Tipo', type: 'select', options: TYPE_OPTIONS },
    { key: 'dateFrom', label: 'Fecha desde', type: 'date' },
    { key: 'dateTo', label: 'Fecha hasta', type: 'date' },
    { key: 'minTotal', label: 'Total mínimo', type: 'number', min: 0 },
    { key: 'maxTotal', label: 'Total máximo', type: 'number', min: 0 },
    { key: 'search', label: 'Detalle contiene', type: 'text' },
  ];

  return productOptions ? [{ key: 'productId', label: 'Producto', type: 'select', options: productOptions }, ...fields] : fields;
}

/** Convierte los valores del panel de filtros en parámetros para la API. */
export function toTransactionQueryFilters(values: FilterValues): Partial<TransactionQuery> {
  const { dateFrom, dateTo, ...rest } = values;

  return {
    ...(rest as Partial<TransactionQuery>),
    dateFrom: dateFrom instanceof Date ? startOfDayIso(dateFrom) : null,
    dateTo: dateTo instanceof Date ? endOfDayIso(dateTo) : null,
  };
}
