import { PagedQuery } from './api.models';

export interface Product {
  id: string;
  name: string;
  description: string | null;
  category: string;
  imageUrl: string | null;
  price: number;
  stock: number;
  isDeleted: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface ProductCreateRequest {
  name: string;
  description: string | null;
  category: string;
  imageUrl: string | null;
  price: number;
  stock: number;
}

/** El stock no se edita: solo cambia mediante transacciones. */
export type ProductUpdateRequest = Omit<ProductCreateRequest, 'stock'>;

export interface ProductQuery extends PagedQuery {
  search?: string | null;
  category?: string | null;
  minPrice?: number | null;
  maxPrice?: number | null;
  minStock?: number | null;
  maxStock?: number | null;
}
