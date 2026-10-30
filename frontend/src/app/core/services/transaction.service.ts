import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { toHttpParams } from '../http/query-params';
import { PagedResult } from '../models/api.models';
import {
  ProductTransactionHistory,
  Transaction,
  TransactionCreateRequest,
  TransactionQuery,
  TransactionUpdateRequest,
} from '../models/transaction.models';

/** Cliente del microservicio de Transacciones. */
@Injectable({ providedIn: 'root' })
export class TransactionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.transactionsApiUrl}/transactions`;

  search(query: TransactionQuery): Observable<PagedResult<Transaction>> {
    return this.http.get<PagedResult<Transaction>>(this.baseUrl, { params: toHttpParams(query) });
  }

  getById(id: string): Observable<Transaction> {
    return this.http.get<Transaction>(`${this.baseUrl}/${id}`);
  }

  getProductHistory(productId: string, query: TransactionQuery): Observable<ProductTransactionHistory> {
    return this.http.get<ProductTransactionHistory>(`${this.baseUrl}/products/${productId}/history`, {
      params: toHttpParams(query),
    });
  }

  create(request: TransactionCreateRequest): Observable<Transaction> {
    return this.http.post<Transaction>(this.baseUrl, request);
  }

  update(id: string, request: TransactionUpdateRequest): Observable<Transaction> {
    return this.http.put<Transaction>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
