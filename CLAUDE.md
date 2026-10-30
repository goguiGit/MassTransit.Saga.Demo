# MassTransit.Saga.Demo

Saga de registro de usuario con MassTransit 8, dos servicios sobre SQL Server como transporte. La explicación completa está en `README.md`; aquí solo decisiones, trampas y convenciones que no se deducen del código.

## Estructura
- `src/Registration.Messages` (contratos del bus), `Registration.Messaging` (configuración compartida del transporte: `AddMessaging`), `Registration.Domain`, `Registration.Application`, `Registration.Infrastructure`, `Registration.Api` (puerto 5100) y `Notifications.Worker` (puerto 5200). Tests en `tests/`, espejo por capa. Solución `MassTransit.Saga.Demo.slnx`, Central Package Management en `Directory.Packages.props`.
- Capas más vertical slice: cada operación es una carpeta en `Registration.Application/Features/<Nombre>/` con comando o consulta, handler y validador, y una carpeta homónima en `Registration.Api/Features/<Nombre>/` con solo el endpoint.
- Nombres: `XCommand` / `XCommandHandler` / `XCommandValidator`, `XQuery` / `XQueryHandler`, `XEndpoint`. Eventos en pasado (`UserRegistered`), comandos en imperativo (`SendWelcomeEmail`). Un tipo por fichero.

## Reglas de código
- Handlers devuelven `Result` o `Result<T>` de Ardalis.Result; nunca excepciones para flujo de negocio. Los endpoints solo hacen `mediator.Send` y `result.ToMinimalApiResult()`.
- Validación con FluentValidation en `ValidationBehavior`: devuelve `Result.Invalid`, no lanza. Todo `IRequest` debe cerrar sobre `Result` o `Result<T>`; si no, el behavior falla al construirse (busca `Invalid(List<ValidationError>)` por reflexión).
- La saga (`RegistrationStateMachine`) solo publica mensajes. No inyectar repositorios en ella: los cambios en `Users` los hacen `UserActivatedConsumer` y `RegistrationDiscardedConsumer`, que deben seguir siendo idempotentes.
- Lógica de dominio en `Registration.Domain` (`User.Register`, `User.Activate`, `RegistrationState.Start`, `RegistrationState.Accepts`). `Domain` solo referencia `MassTransit.Abstractions`.
- Comentarios en inglés y solo para trampas o porqués. Documentación en español.
- CRLF en todo (`.gitattributes`); los PNG de `docs/images` son binarios.

## Trampas
- **Transporte SQL con autenticación integrada**: el migrador siempre ejecuta `CREATE LOGIN {Username} WITH PASSWORD` en el paso de crear la base y falla con "Incorrect syntax near 'WITH'" si no hay `Username`. Por eso `CreateDatabase = false` y la base la crea EF (`EnsureRegistrationDatabaseAsync`) antes de que arranquen los hosted services. El worker necesita que la base exista: la API arranca primero.
- `EnsureCreated` no migra. Si cambia el modelo (`User`, `RegistrationState`), hay que borrar `MassTransitSagaDemo` y arrancar la API. Hacerlo con `SET SINGLE_USER WITH ROLLBACK IMMEDIATE` si hay conexiones.
- El transporte se elige con `Messaging:Transport` (`Sql` por defecto, `RabbitMq`) y lo configura solo `Registration.Messaging/MessagingExtensions.cs`; no configurar MassTransit en los `Program.cs`. Los dos servicios deben usar el mismo valor, y con SQL el mismo `Schema` y `Role` (`transport`); si no, no comparten colas.
- RabbitMQ: usar la imagen `masstransit/rabbitmq` (trae `rabbitmq_delayed_message_exchange`, necesario para `UseDelayedMessageScheduler`). La oficial falla al programar el plazo. Contenedor: `podman run -d --name rabbitmq -p 5672:5672 -p 15672:15672 docker.io/masstransit/rabbitmq:latest`. Credenciales `guest`/`guest` del contenedor local, no son un secreto.
- Test harness de MassTransit: `Published.Select<T>()` bloquea hasta el timeout de inactividad; `Published.Any<T>()` devuelve `false` al agotarlo. Los tests leen el código de la instancia de la saga y suben `TestInactivityTimeout` a plazo + 1 s. Los eventos con `Ignore` no aparecen en el `Consumed` del harness de la saga, sí en el del bus.
- La saga publica antes de persistir la instancia: en tests, esperar `Exists(id, AwaitingConfirmation)` antes de publicar `EmailConfirmed`. En producción iría el outbox transaccional (no está en la demo).
- Visual Studio puede reescribir los csproj al abrir la solución (añadió `UserSecretsId` y borró una `PackageReference`). Tras abrirla, revisar `git diff` de los csproj.
- Versiones fijadas a propósito: MassTransit **8.x** (la 9 es comercial), MediatR **12.x** (la 13 es comercial). No subir. `System.Security.Cryptography.Pkcs` pinneado por un aviso de NuGet audit que arrastra el transporte SQL.

## Verificar
- `dotnet build MassTransit.Saga.Demo.slnx` y `dotnet test MassTransit.Saga.Demo.slnx` (30 tests, ~25 s, sin SQL Server).
- Flujo real: arrancar `Registration.Api`, luego `Notifications.Worker`, y seguir `src/Registration.Api/Registration.Api.http`. El plazo de la demo es `Registration:ConfirmationWindow = 00:02:00`. Para probar RabbitMQ, `Messaging__Transport=RabbitMq` en el entorno de los dos procesos con el contenedor arrancado (`podman machine start` antes si hace falta).
- No hay secretos: cadena de conexión integrada a `localhost` en los `appsettings.json`. Si alguna vez hace falta una credencial, user-secrets (`UserSecretsId` ya existe en la API), nunca en el repo.

## Git
- Conventional commits (`feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`). No force push.
- Diagramas: Mermaid en el README y PNG exportados de FigJam en `docs/images` (tablero: https://www.figma.com/board/iKgAEJdZhUd8wiztAhA4ro). Si cambia la saga, regenerar los tres.
