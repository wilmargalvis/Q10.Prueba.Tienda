# Q10 OrderFlow

Implementación de la prueba técnica: Orders API recibe pedidos; Inventory reserva unidades mediante RabbitMQ; el panel Angular consulta y muestra los estados.

## Requisitos previos

- Docker Desktop con el motor Linux iniciado y Docker Compose.
- Para ejecutar las pruebas fuera de Docker: .NET SDK 10.

## Iniciar todo

1. Copiar `.env.example` como `.env` y reemplazar las credenciales locales de demostración.
2. Desde la raíz del repositorio ejecutar:

```powershell
docker compose up
```

Las migraciones EF son la fuente de verdad. `database-init` regenera y aplica `Database/Generated/03_OrdersSchema.sql` y `04_InventorySchema.sql` al iniciar. Después de cambiar una migración, ejecutar `docker compose up --build database-init`, revisar/versionar los scripts generados junto con la migración y volver a levantar la aplicación. No editar esos scripts a mano. Para reconstruir la base y volver al seed inicial, usar `docker compose down -v` y luego `docker compose up --build`.

Compose construye las imágenes en el primer inicio. SQL Server inicia con un volumen persistente; `database-init` crea la base y los esquemas, genera scripts idempotentes desde las migraciones EF Core y los aplica. Orders e Inventory mantienen tablas e historial de migraciones separados. Inventory inserta los productos iniciales solo si faltan.

Servicios disponibles:

- Panel: <http://localhost:4200>
- Orders API / Swagger: <http://localhost:8080/swagger>
- RabbitMQ Management: <http://localhost:15672> (usuario y clave de `.env`)
- SQL Server: `localhost,1433`, base `PruebaQ10Tienda`

Para cerrar manteniendo los datos: `docker compose down`. Para empezar de cero, eliminar también el volumen: `docker compose down -v`; el siguiente `docker compose up` recrea la base y el seed.

## API

| Método | Ruta | Resultado |
|---|---|---|
| `POST` | `/orders` | Crea pedido `Pending` y devuelve HTTP 201 |
| `GET` | `/orders` | Lista pedidos y estados |
| `GET` | `/orders/{id}` | Detalle o HTTP 404 |

Ejemplo de creación:

```json
{
  "clienteNombre": "Laura Gómez",
  "sku": "GTR-ACU-001",
  "cantidad": 2
}
```

Las respuestas usan `ApiResponse<T>`. El estado HTTP real y `statusCode` del JSON coinciden. Los errores incluyen `field`, `errorCode` y `message` para que el panel relacione la validación con el campo correspondiente.

## Flujo y decisiones

1. Orders valida nombre, cantidad y existencia del SKU. Para esta prueba consulta solo en lectura el SKU de `Inventory.Stock`; Inventory sigue siendo dueño de sus escrituras. Esta consulta cruzada y la base física compartida reducen el trabajo, pero acoplan parcialmente los servicios.
2. Orders persiste el pedido y `OrderCreated` en el outbox dentro del mismo `DbContext`/guardado. Un proceso publica los mensajes pendientes. Si RabbitMQ está caído, el pedido queda `Pending`, la API puede responder 201 y el outbox reintenta al recuperarse el broker.
3. Inventory registra el `EventId` procesado y descuenta el stock en una transacción serializable. Reentregas no descuentan dos veces; se vuelve a publicar el mismo resultado lógico para que Orders pueda actualizarse si se perdió la respuesta anterior.
4. Orders solo aplica resultados a pedidos `Pending`. El panel Angular consulta `/orders` cada cinco segundos; SignalR queda fuera del alcance inicial.

Los consumidores MassTransit reintentan los fallos con intervalos de 1, 3 y 5 segundos. Si se agotan, MassTransit mueve el mensaje a su cola de error (`*_error`) para inspección; no se asume entrega exactamente una vez. El dispatcher del outbox conserva el mensaje pendiente y vuelve a intentar publicarlo cada dos segundos.

La base `PruebaQ10Tienda` tiene esquemas separados `Orders` e `Inventory`, cada uno con su `DbContext`, migraciones e historial. En un sistema que requiera mayor independencia, se separarían las bases y se reemplazaría la lectura de catálogo por un contrato de servicio.

El seed de demostración incluye una guitarra, un teclado y una batería. Si el SKU ya existe, se conserva su stock actual; reiniciar Compose no reabastece lo consumido.

## Pruebas

```powershell
dotnet test "Service Orders API/Q10.Prueba.Tienda.Api.Test/Q10.Prueba.Tienda.Api.Test.csproj"
```

Las pruebas cubren validación, SKU inexistente, creación en estado pendiente y transición idempotente del estado.

## Estructura

- `Service Orders API`: endpoints, casos de uso de pedidos, persistencia y consumidor de respuestas de inventario.
- `Service Inventory Worker`: consumidores RabbitMQ, reserva y seed de inventario.
- `Q10.Prueba.Tienda.Contracts`: contratos pequeños de integración, sin entidades de dominio.
- `Database`: bootstrap SQL; el inicializador genera y ejecuta scripts de migración idempotentes.
- `Frontend`: aplicación Angular con componentes separados de formulario/lista, servicio HTTP tipado y estado local con Signals/RxJS.

## Qué haría distinto con más tiempo

- Separar físicamente las bases de datos y reemplazar la lectura compartida del catálogo con un contrato de servicio.
- Añadir observabilidad y una vista operativa de mensajes de error/reintentos.
- Ampliar las pruebas de integración del consumidor contra SQL Server y RabbitMQ.
- Evaluar SignalR si el volumen o la latencia del panel lo justifican.
