# Guía arquitectónica para Codex

## Objetivo y alcance

Construir la prueba OrderFlow definida en [`requisitos.md`](requisitos.md). Respetar sus requisitos funcionales. No cambiar estados, contratos visibles, validaciones ni respuestas HTTP por iniciativa propia; si una ambigüedad funcional afecta la implementación, señalarla antes de resolverla con un comportamiento nuevo.

El repositorio contiene dos soluciones .NET separadas:

- `Service Orders API/Q10.Prueba.TiendaOrdersAPI.slnx`
- `Service Inventory Worker/Q10.Prueba.Inventory.Worker.slnx`

Mantenerlas separadas. El Docker Compose de la raíz coordinará los procesos de despliegue sin combinar los servicios en un solo ejecutable.

## Stack fijado

- C# / .NET 10.
- EF Core 10 con SQL Server.
- RabbitMQ mediante MassTransit.
- ASP.NET Core para Orders API y Swagger/OpenAPI.
- xUnit y Moq para pruebas unitarias.
- Frontend con polling periódico inicialmente.
- Dockerfiles independientes y Docker Compose.

No añadir AutoMapper, FluentValidation, bibliotecas de mediator, repositorios genéricos, CQRS, sagas, SignalR u otras dependencias/patrones hasta que exista una necesidad concreta. Reutilizar paquetes ya presentes solo si aportan valor comprobable.

## Arquitectura simplificada acordada

No crear un proyecto separado `Application` para esta prueba. El proyecto llamado `Dominio` contendrá el núcleo del negocio y los casos de uso pequeños acordados para evitar una capa adicional. Esta decisión es intencional: sus responsabilidades incluyen reglas de negocio y coordinación de operaciones del caso de uso; el nombre del proyecto no debe usarse como razón para introducir dependencias de infraestructura.

### Dominio

- Mantener entidades, estados, invariantes, reglas y operaciones del negocio.
- Puede declarar abstracciones pequeñas que los casos de uso necesiten para persistencia o publicación, cuando corresponda.
- No referenciar ASP.NET Core, EF Core, MassTransit, SQL Server ni implementaciones de infraestructura.
- No guardar DTO HTTP, tipos de `DbContext`, configuraciones EF ni credenciales.

### Infraestructura

- Implementar persistencia EF Core y adaptadores externos del servicio.
- Referenciar Dominio; nunca hacer que Dominio referencie Infraestructura.
- Mantener los `DbContext` independientes por servicio y limitar cada uno a su esquema/propiedad.
- Alojar las configuraciones EF, migraciones y registro de dependencias de infraestructura.

### Hosts

- La API y el Worker son puntos de entrada y composition roots: leen configuración, registran dependencias y conectan los casos de uso con sus adaptadores.
- Los controllers/endpoints HTTP deben ser delgados; no poner reglas de negocio ni consultas EF directamente en ellos.
- Los consumidores de MassTransit deben ser delgados: validar el mensaje, invocar la operación pertinente y dejar que la persistencia/infraestructura confirme el resultado.
- No compartir tablas o `DbContext` entre los dos servicios.

## Base de datos y propiedad

- Compose ejecuta SQL Server con una base `PruebaQ10Tienda`.
- Orders es dueño del esquema `Orders`; Inventory es dueño de `Inventory`.
- Cada servicio mantiene su propio `DbContext` y conjunto de migraciones. Configurar los nombres de esquema explícitamente en EF.
- No consultar tablas del otro esquema para implementar el flujo distribuido; intercambiar contratos de mensajes.
- No crear claves foráneas entre los esquemas como sustituto de la comunicación entre servicios.
- Usar PK, `UNIQUE`, `CHECK` e índices apropiados para reforzar invariantes. Evitar índices duplicados y procedimientos almacenados sin una razón medible.
- La base compartida es un trade-off consciente del ejercicio y debe permanecer documentada; no describirla como aislamiento completo de microservicios.

## Migraciones, scripts y seed

- Las migraciones EF Core son la única fuente de verdad del esquema de tablas.
- Generar desde ellas SQL idempotente para despliegue/inspección; no modificar a mano scripts generados para reflejar cambios de modelo.
- Mantener aparte `01_CreateDatabase.sql`, el bootstrap SQL mínimo que crea la base `PruebaQ10Tienda` si aún no existe.
- Generar/versionar scripts SQL idempotentes desde las migraciones, por ejemplo `02_OrdersSchema.sql` y `03_InventorySchema.sql`. No editar manualmente el SQL generado para reflejar cambios de modelo.
- Compose debe esperar al healthcheck de SQL Server y ejecutar un inicializador que aplique primero `01_CreateDatabase.sql` y después los scripts de Orders e Inventory antes de iniciar API y Worker.
- El seed de productos es responsabilidad de Inventory y debe ser idempotente: solo inicializa productos faltantes en una base nueva, nunca restablece saldos existentes. No crear un script `04_InsertData.sql` en paralelo si el seed se hace en código.
- Versionar cualquier script generado con un nombre claro y actualizarlo junto con la migración que lo produce.
- Documentar cómo reconstruir scripts y cómo borrar el volumen Compose para empezar desde cero.

## Mensajería

- Usar RabbitMQ y MassTransit; la API publica `OrderCreated`, Inventory consume y publica el resultado, Orders consume el resultado y persiste la transición del pedido.
- Mantener contratos de mensajes pequeños y estables, con identificadores y datos necesarios para ejecutar la operación. No exponer entidades de dominio ni entidades EF como contratos.
- Los consumidores deben tolerar redeliveries y ser idempotentes.
- En Inventory, guardar la deduplicación del evento y la mutación del stock en una misma transacción y respaldar la unicidad con una restricción de base de datos.
- La cantidad reservada nunca puede llevar stock a negativo, incluso bajo concurrencia.
- Un resultado duplicado no debe cambiar un pedido que ya no está `Pending`.
- Configurar y documentar reintentos y mensajes fallidos. No interpretar una entrega como exactamente una vez.
- Evaluar el outbox transaccional de Orders para evitar la brecha entre guardar el pedido y publicar `OrderCreated`; si se omite por alcance, dejar descrito el fallo posible.

## API y experiencia del panel

- Mantener `POST /orders`, `GET /orders` y `GET /orders/{id}` según `requisitos.md`.
- Validar nombre no vacío, SKU existente y cantidad entre 1 y 100.
- Devolver `201` al crear el pedido `Pending`; no esperar sincrónicamente a Inventory.
- Los errores de validación deben ser claros tanto en HTTP como en el formulario.
- El panel refresca estados con polling y consulta la API como fuente de verdad. Esta decisión satisface el enunciado y reduce el tiempo y costo de implementación para una entrega de 24 horas: evita conexiones persistentes, configuración de hubs y trabajo adicional de reconexión e integración. La carga periódica es aceptable para el volumen pequeño de la prueba; explicar que introduce una demora de hasta un intervalo de polling y consultas aunque nada cambie. No introducir SignalR salvo que se complete el alcance obligatorio y el usuario decida que queda tiempo.

## Configuración y seguridad básica

- Todas las URLs, nombres de host, credenciales y cadenas de conexión deben configurarse desde entorno/configuración externa.
- No escribir secretos en código, appsettings versionados, Dockerfiles ni scripts versionados.
- En Compose, conectar a SQL Server y RabbitMQ por nombre de servicio; `localhost` se reserva para conexiones desde el host.
- Usar un volumen nombrado para datos de SQL Server. Documentar que `docker compose down -v` elimina la base persistida.

## Pruebas y cambios

- Mantener las pruebas enfocadas en reglas críticas: validación, transición de estados e idempotencia.
- Usar xUnit; usar Moq cuando se prueben interacciones con abstracciones. No simular detalles internos de EF Core mediante mocks para pretender validar consultas.
- Implementar entre 3 y 5 pruebas backend para los casos críticos solicitados en `requisitos.md`. No afirmar que pasaron si no se han ejecutado; al ejecutar pruebas, reportar el comando y el resultado.
- No introducir abstracciones genéricas o patrones por anticipado. Cada abstracción debe resolver una necesidad presente y ser fácil de explicar en la defensa técnica.
- Mantener cambios proporcionales a una prueba de 6–8 horas con plazo de dos días (actualizado por el usuario a 24 horas para la entrega): priorizar ejecución Compose, recorrido completo del pedido, idempotencia, pruebas críticas e instrucciones claras.

