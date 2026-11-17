# Requisitos y contexto de trabajo

## Propósito

Implementar la prueba técnica OrderFlow como una aplicación pequeña y demostrable para registrar pedidos, reservar inventario de forma asíncrona y consultar el estado de cada pedido desde un panel web.

Este documento recoge los requisitos funcionales del enunciado y las decisiones técnicas acordadas para el proyecto. No amplía el alcance funcional sin autorización.

## Requisitos funcionales

### Orders API

- `POST /orders`: validar los datos, crear y persistir el pedido con estado `Pending`, publicar `OrderCreated` y responder `201 Created` con el pedido creado. Los datos inválidos deben producir `400 Bad Request`.
- `GET /orders`: devolver los pedidos junto con su estado actual.
- `GET /orders/{id}`: devolver el detalle del pedido o `404 Not Found` si no existe.
- Pedido mínimo: identificador `Guid`, `clienteNombre`, `sku`, `cantidad`, `estado` y fecha de creación.
- Validación mínima: nombre de cliente no vacío; SKU existente; cantidad entre 1 y 100 inclusive.

### Inventory Worker

- Consumir `OrderCreated` desde RabbitMQ.
- Comprobar y reservar el stock del SKU solicitado.
- Publicar `StockReserved` si la reserva tiene éxito y `StockRejected` si no hay stock suficiente o no se encuentra el SKU.
- El procesamiento repetido del mismo evento no debe descontar el stock más de una vez.
- Cargar automáticamente al menos tres productos iniciales.

### Estados del pedido

- El pedido empieza en `Pending`.
- Orders consume el resultado de Inventory y cambia el pedido a `Confirmed` o `Rejected`.
- El procesamiento es asíncrono; la respuesta de creación no espera a la reserva de inventario.

### Contrato de respuesta de Orders API

- Las respuestas JSON de Orders usarán un envoltorio genérico `ApiResponse<T>` basado en el modelo propuesto: `isSuccess`, `result`, `message`, `statusCode` y `errors`.
- Cada `ApiError` incluirá `message` y `errorCode`; se recomienda agregar `field` opcional para asociar errores de validación a `clienteNombre`, `sku` o `cantidad` en el formulario.
- El código HTTP real debe corresponder con `statusCode` en el cuerpo. Por ejemplo, crear devuelve HTTP `201` y `statusCode: 201`; una validación inválida devuelve HTTP `400` y `statusCode: 400`. No devolver siempre HTTP 200 para indicar fallos solo dentro del JSON.
- Los éxitos devolverán `isSuccess: true`; los errores de validación y negocio devolverán `isSuccess: false`, un mensaje útil y errores estructurados cuando existan.
- Mantener los DTOs de respuesta separados de entidades de Dominio y EF Core.

Ejemplo de error de validación (HTTP 400):

```json
{
  "isSuccess": false,
  "result": null,
  "message": "La solicitud contiene errores.",
  "statusCode": 400,
  "errors": [
    { "field": "cantidad", "message": "La cantidad debe estar entre 1 y 100.", "errorCode": "ORDER_QUANTITY_INVALID" }
  ]
}
```

### Panel web

- Implementar el frontend con Angular.
- Incluir un formulario para crear pedidos con validaciones y errores visibles para el usuario.
- Incluir una lista de pedidos que muestre el estado actual.
- Organizar la interfaz por funcionalidad, al menos con componentes diferenciados para página/listado y formulario, un servicio Angular tipado para la API y modelos del cliente (sin compartir ensamblados con el backend).
- Manejar estado visible de carga, éxito, error y pedidos. Usar estado local sencillo con Signals/RxJS según convenga; no añadir una librería global de estado para este alcance.
- Usar polling periódico para refrescar el estado. SignalR queda como mejora opcional si hay tiempo después de completar los requisitos obligatorios. Se elige polling porque el enunciado lo acepta expresamente, el panel solo necesita refrescar el estado de pedidos y el plazo de entrega es de 24 horas. Requiere menos código, configuración, manejo de conexiones e integración/pruebas. Para la carga pequeña de la prueba, su costo operativo es aceptable; la contrapartida es que el cambio aparece en el siguiente ciclo de consulta, no en tiempo real, y el cliente consulta periódicamente aunque no haya cambios.

## Requisitos técnicos obligatorios

- Backend en C# y .NET 10.
- Dos servicios independientes: Orders API e Inventory Worker.
- Clean Architecture simplificada, principios SOLID y dependencias dirigidas hacia el núcleo.
- EF Core 10 y SQL Server.
- RabbitMQ con MassTransit para la comunicación asíncrona.
- Pruebas unitarias con xUnit y Moq cuando el doble de prueba sea necesario; entre 3 y 5 pruebas backend centradas en reglas críticas.
- Dockerfile propio para cada servicio y Docker Compose para iniciar frontend, API, Worker, SQL Server y RabbitMQ con un solo comando.
- Configuración por variables de entorno; no almacenar secretos ni cadenas de conexión con credenciales en código o archivos versionados.
- Swagger/OpenAPI para explorar la API y posibilidad de probarla desde Postman.
- README con arquitectura, trade-offs, instrucciones de ejecución, comando para las pruebas y sección “Qué haría distinto con más tiempo”.

## Decisiones acordadas

### Persistencia compartida con propiedad separada

- En el entorno de Compose se usará SQL Server.
- Se utilizará una única base de datos llamada `PruebaQ10Tienda` para simplificar la ejecución de la prueba.
- Orders será dueño de las tablas del esquema `Orders`; Inventory será dueño de las tablas del esquema `Inventory`.
- Cada servicio tendrá su propio `DbContext` y sus propias migraciones. Inventory es dueño de las escrituras de su esquema. Como trade-off de simplicidad, Orders puede hacer una consulta de solo lectura al dato de catálogo necesario para validar la existencia de un SKU; no puede modificar stock ni usar esa consulta para reservarlo.
- Configurar una tabla de historial de migraciones independiente para cada contexto, dentro de su propio esquema (por ejemplo `Orders.__EFMigrationsHistory` e `Inventory.__EFMigrationsHistory`). No permitir que ambos contextos compartan la tabla de historial predeterminada en `dbo`.
- No crear claves foráneas entre servicios/esquemas para representar relaciones de negocio entre pedidos y stock. La reserva y los cambios de estado se coordinan mediante mensajes.
- Documentar ambos trade-offs: la base física compartida y la lectura de catálogo desde Orders simplifican la prueba, pero acoplan parcialmente Orders al esquema de Inventory. En un sistema con mayor necesidad de aislamiento, cada servicio tendría su propia base y la validación de catálogo se resolvería mediante un contrato entre servicios.

### Esquema y scripts SQL

- Las migraciones de EF Core son la fuente de verdad para el esquema de cada servicio; evitar definir y mantener el mismo modelo en migraciones y scripts SQL manuales paralelos.
- Generar scripts SQL idempotentes desde las migraciones y versionarlos para inspección y ejecución reproducible.
- Mantener un script de bootstrap `01_CreateDatabase.sql` separado para crear `PruebaQ10Tienda`, ya que la creación de la base precede a la aplicación de migraciones.
- Mantener `02_CreateSchemas.sql` como bootstrap idempotente para los esquemas `Orders` e `Inventory`.
- Generar los scripts SQL idempotentes desde las migraciones por contexto, por ejemplo `03_OrdersSchema.sql` y `04_InventorySchema.sql`. Estos scripts contienen la creación/evolución de tablas, índices y restricciones definida por EF Core; no se mantienen como SQL manual paralelo.
- Compose debe iniciar SQL Server, esperar a que esté listo y ejecutar un inicializador que aplique `01_CreateDatabase.sql`, `02_CreateSchemas.sql` y después los scripts generados de Orders e Inventory. Después podrán iniciar API y Worker.
- El seed de inventario debe ser idempotente y pertenecer a Inventory. Al iniciar una base vacía debe insertar al menos tres SKU; reiniciar el sistema no debe duplicarlos ni reponer stock ya consumido.
- La nomenclatura de scripts debe reflejar el orden real. Para este enfoque no se requiere un `04_InsertData.sql`: Inventory realiza el seed idempotente desde su propio código tras la inicialización del esquema.
- Usar migraciones para PK, índices, restricciones `UNIQUE` y `CHECK`. Ejemplos de invariantes: SKU único, cantidad de pedido dentro de 1..100 y stock no negativo.
- Crear índices para consultas justificadas por el uso (por ejemplo estado/fecha para el listado si aplica). No duplicar índices que ya crean PK o restricciones `UNIQUE`.
- No agregar procedimientos almacenados sin una necesidad concreta; EF Core es suficiente para el alcance inicial.

### Configuración local y Compose

- Compose es el entorno de ejecución reproducible para el evaluador. El flujo debe permitir el comando de aceptación exacto `docker compose up`; declarar `build:` en los servicios para que Compose construya las imágenes al no existir localmente. `--build` puede documentarse como opción para forzar una reconstrucción.
- Dentro de Compose, las aplicaciones se conectan a SQL Server usando el nombre de servicio de Compose, no `localhost` ni la instancia local `.\SQLEXPRESS`.
- La instancia local `.\SQLEXPRESS` puede utilizarse para desarrollo fuera de Compose, pero el flujo verificado para la entrega será SQL Server en Compose.
- Usar un volumen nombrado para persistir datos durante reinicios normales.
- Documentar `docker compose down -v` como la operación para eliminar el volumen y recrear una base limpia con migraciones y seed.
- Los secretos de demostración requeridos por los contenedores deben suministrarse mediante variables de entorno o un archivo local no versionado; nunca codificarlos en los proyectos.

## Fiabilidad y consistencia

- La idempotencia de Inventory debe estar respaldada por persistencia: registrar el identificador del evento procesado con unicidad y guardar ese registro en la misma transacción que el cambio de stock.
- La reserva de stock debe impedir que solicitudes concurrentes reduzcan el stock por debajo de cero.
- Orders debe aplicar resultados de Inventory solo a pedidos todavía `Pending`, para que una entrega repetida no provoque transiciones incorrectas.
- Configurar reintentos razonables y documentar el destino de mensajes que excedan los reintentos (por ejemplo, cola de error).
- Documentar qué observa el usuario si Inventory está fuera de servicio: el pedido permanece `Pending` mientras el evento espera o se reintenta.
- Documentar el riesgo de guardar un pedido y no publicar su mensaje si RabbitMQ está caído. La opción recomendada para evitar perder la publicación es un outbox transaccional; mantenerla proporcional al tiempo disponible y explicar el trade-off si no se implementa.
- No asumir entrega exactamente una vez: diseñar consumidores idempotentes porque puede haber reentrega.

## Criterios de aceptación

- Un único `docker compose up` inicia el sistema, construye las imágenes en un checkout limpio, inicializa la base y el seed, y deja disponibles los servicios y el panel sin pasos manuales de base de datos.
- El panel permite crear un pedido válido y verlo avanzar de `Pending` a `Confirmed` o `Rejected` mediante polling.
- Los errores de validación se muestran claramente en el panel.
- Procesar dos veces el mismo `OrderCreated` descuenta stock una sola vez.
- Los tests backend se ejecutan con un único comando documentado.
- El README permite a un evaluador levantar y entender la solución en menos de diez minutos.

## Fuera de alcance inicial

- SignalR (se reconsiderará solo si queda tiempo; ver la justificación de polling en los requisitos del panel).
- Kubernetes, autenticación/autorización, CI/CD y diseño visual elaborado.
- Procedimientos almacenados sin justificación.
- Alta cobertura de pruebas más allá de los casos críticos solicitados.

## Pendientes que no deben inventarse como requisitos

- Nombres, descripciones y cantidades concretas de los tres productos iniciales.
- Intervalo exacto del polling.
- Política exacta de reintentos, expiración o compensación de pedidos si Inventory permanece inactivo.
- Implementación del outbox si el tiempo disponible obliga a documentar la alternativa en lugar de implementarla.

