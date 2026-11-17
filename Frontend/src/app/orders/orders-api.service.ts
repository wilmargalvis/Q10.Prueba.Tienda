import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiResponse } from './api-response.model';
import { CreateOrder, Order } from './order.model';

@Injectable({ providedIn: 'root' })
export class OrdersApiService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = '/api/orders';

  list(): Observable<Order[]> {
    return this.http.get<ApiResponse<Order[]>>(this.endpoint).pipe(
      map(response => this.unwrap(response))
    );
  }

  create(order: CreateOrder): Observable<Order> {
    return this.http.post<ApiResponse<Order>>(this.endpoint, order).pipe(
      map(response => this.unwrap(response))
    );
  }

  private unwrap<T>(response: ApiResponse<T>): T {
    if (!response.isSuccess || response.result === null) {
      throw new Error(response.message || 'La API devolvió una respuesta inesperada.');
    }
    return response.result;
  }
}
