import { PagedQuery, PagedResult } from './api.models';

export type TransactionType = 'Purchase' | 'Sale';

export const TRANSACTION_TYPE_LABELS: Record<TransactionType, string> = {
  Purchase: 'Compra',
  Sale: 'Venta',
};

export interface Transaction {
  id: string;
  date: string;
  type: TransactionType;
  productId: string;
  productName: string | null;
  productStock: number | null;
  productIsDeleted: boolean | null;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  detail: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface TransactionCreateRequest {
  date: string | null;
  type: TransactionType;
  productId: string;
  quantity: number;
  unitPrice: number | null;
  detail: string | null;
}

export interface TransactionUpdateRequest {
  date: string;
  type: TransactionType;
  productId: string;
  quantity: number;
  unitPrice: number;
  detail: string | null;
}

export interface TransactionQuery extends PagedQuery {
  productId?: string | null;
  type?: TransactionType | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  minTotal?: number | null;
  maxTotal?: number | null;
  search?: string | null;
}

/** Datos del producto que acompañan al historial. */
export interface ProductSummary {
  id: string;
  name: string;
  category: string;
  imageUrl: string | null;
  price: number;
  stock: number;
  isDeleted: boolean;
}

export interface ProductTransactionHistory {
  product: ProductSummary;
  transactions: PagedResult<Transaction>;
}

/** Cantidad con signo que la transacción aplica al stock: compra suma, venta resta. */
export function stockEffect(type: TransactionType, quantity: number): number {
  return type === 'Purchase' ? quantity : -quantity;
}
