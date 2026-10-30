import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'products' },
  {
    path: 'products',
    children: [
      {
        path: '',
        title: 'Productos',
        loadComponent: () => import('./features/products/product-list/product-list').then((m) => m.ProductList),
      },
      {
        path: 'new',
        title: 'Nuevo producto',
        loadComponent: () => import('./features/products/product-form/product-form').then((m) => m.ProductForm),
      },
      {
        path: ':id',
        title: 'Detalle de producto',
        loadComponent: () => import('./features/products/product-detail/product-detail').then((m) => m.ProductDetail),
      },
      {
        path: ':id/edit',
        title: 'Editar producto',
        loadComponent: () => import('./features/products/product-form/product-form').then((m) => m.ProductForm),
      },
      {
        path: ':id/history',
        title: 'Historial del producto',
        loadComponent: () =>
          import('./features/products/product-history/product-history').then((m) => m.ProductHistory),
      },
    ],
  },
  {
    path: 'transactions',
    children: [
      {
        path: '',
        title: 'Transacciones',
        loadComponent: () =>
          import('./features/transactions/transaction-list/transaction-list').then((m) => m.TransactionList),
      },
      {
        path: 'new',
        title: 'Nueva transacción',
        loadComponent: () =>
          import('./features/transactions/transaction-form/transaction-form').then((m) => m.TransactionForm),
      },
      {
        path: ':id',
        title: 'Detalle de transacción',
        loadComponent: () =>
          import('./features/transactions/transaction-detail/transaction-detail').then((m) => m.TransactionDetail),
      },
      {
        path: ':id/edit',
        title: 'Editar transacción',
        loadComponent: () =>
          import('./features/transactions/transaction-form/transaction-form').then((m) => m.TransactionForm),
      },
    ],
  },
  { path: '**', redirectTo: 'products' },
];
