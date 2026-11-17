export interface Order {
  id: string;
  clienteNombre: string;
  sku: string;
  cantidad: number;
  estado: 'Pending' | 'Confirmed' | 'Rejected';
  creadoEn: string;
}

export interface CreateOrder {
  clienteNombre: string;
  sku: string;
  cantidad: number;
}
