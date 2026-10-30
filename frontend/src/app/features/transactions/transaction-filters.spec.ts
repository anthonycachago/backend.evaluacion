import { toTransactionQueryFilters, transactionFilterFields } from './transaction-filters';

describe('transaction filters', () => {
  it('convierte las fechas al inicio y fin del día local', () => {
    const query = toTransactionQueryFilters({
      type: 'Sale',
      dateFrom: new Date(2026, 9, 1, 15, 30),
      dateTo: new Date(2026, 9, 29, 8, 0),
    });

    expect(query.type).toBe('Sale');
    expect(new Date(query.dateFrom!).getHours()).toBe(0);
    expect(new Date(query.dateTo!).getHours()).toBe(23);
    expect(new Date(query.dateTo!).getDate()).toBe(29);
  });

  it('sin fechas envía null', () => {
    const query = toTransactionQueryFilters({ search: 'factura' });

    expect(query.dateFrom).toBeNull();
    expect(query.dateTo).toBeNull();
    expect(query.search).toBe('factura');
  });

  it('agrega el filtro por producto solo si se pasan productos', () => {
    expect(transactionFilterFields().some((f) => f.key === 'productId')).toBe(false);
    expect(transactionFilterFields([{ value: '1', label: 'Mouse' }])[0].key).toBe('productId');
  });
});
