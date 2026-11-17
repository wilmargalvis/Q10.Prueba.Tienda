import { HttpErrorResponse } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { EMPTY, Subscription, catchError, interval, startWith, switchMap } from 'rxjs';
import { ApiResponse } from './api-response.model';
import { CreateOrder, Order } from './order.model';
import { OrdersApiService } from './orders-api.service';

@Component({
  selector: 'app-orders-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './orders-page.component.html'
})
export class OrdersPageComponent implements OnInit, OnDestroy {
  readonly orders = signal<Order[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly listError = signal('');
  readonly formMessage = signal('');
  readonly formSuccess = signal('');
  private polling?: Subscription;

  readonly form = new FormGroup({
    clienteNombre: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(120)] }),
    sku: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(64)] }),
    cantidad: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(1), Validators.max(100)] })
  });

  constructor(private readonly api: OrdersApiService) {}

  ngOnInit(): void {
    this.polling = interval(5000).pipe(
      startWith(0),
      switchMap(() => this.api.list().pipe(
        catchError(error => {
          this.listError.set(this.errorMessage(error));
          this.loading.set(false);
          return EMPTY;
        })
      ))
    ).subscribe(orders => {
      this.orders.set(orders);
      this.listError.set('');
      this.loading.set(false);
    });
  }

  ngOnDestroy(): void {
    this.polling?.unsubscribe();
  }

  submit(): void {
    this.formMessage.set('');
    this.formSuccess.set('');
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formMessage.set('Revisa los campos indicados.');
      return;
    }

    this.saving.set(true);
    this.api.create(this.form.getRawValue() as CreateOrder).subscribe({
      next: order => {
        this.formSuccess.set(`Pedido ${order.id} recibido. Su estado inicial es Pending.`);
        this.form.reset({ clienteNombre: '', sku: '', cantidad: 1 });
        this.saving.set(false);
        this.refreshNow();
      },
      error: error => {
        this.applyApiErrors(error);
        this.saving.set(false);
      }
    });
  }

  refreshNow(): void {
    this.api.list().subscribe({
      next: orders => { this.orders.set(orders); this.listError.set(''); },
      error: error => this.listError.set(this.errorMessage(error))
    });
  }

  trackOrder(_index: number, order: Order): string { return order.id; }

  private applyApiErrors(error: unknown): void {
    if (error instanceof HttpErrorResponse) {
      const response = error.error as ApiResponse<unknown> | null;
      for (const item of response?.errors ?? []) {
        const control = item.field ? this.form.get(item.field) : null;
        if (control) control.setErrors({ api: item.message });
      }
      this.formMessage.set(response?.message || 'No se pudo registrar el pedido. Intenta de nuevo.');
      return;
    }
    this.formMessage.set(this.errorMessage(error));
  }

  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const response = error.error as ApiResponse<unknown> | null;
      if (response?.message) return response.message;
      if (error.status === 0) return 'No se pudo conectar con Orders API.';
    }
    return 'Ocurrió un error de comunicación. Intenta de nuevo.';
  }
}
