---
description: Ejecuta el flujo completo de registro (alta, código, confirmación) contra la API y el worker en marcha y explica el resultado.
argument-hint: [-Expire] [-Email alguien@example.com]
---

Ejecuta el flujo de la saga de registro de punta a punta y cuéntame qué ha pasado.

1. Comprueba que `Registration.Api` (http://localhost:5100) y `Notifications.Worker` (http://localhost:5200) responden. Si no, dímelo y explica cómo arrancarlos (`dotnet run --project src/Registration.Api`, después `dotnet run --project src/Notifications.Worker`); no los arranques tú sin preguntar.
2. Lanza `pwsh -NoProfile -File scripts/registration-flow.ps1 $ARGUMENTS` (si no hay `pwsh`, usa `powershell`). Con `-Expire` el script espera a que venza el plazo de la saga en vez de confirmar; tarda `Registration:ConfirmationWindow` (2 minutos por defecto).
3. Resume el resultado paso a paso en español: el id del usuario, el código que llegó al worker, el código HTTP de cada llamada y el estado del usuario y de la saga antes y después. Si algún paso falla, señala cuál y cuál es la causa probable según el README (sección "Cómo depurarlo de verdad").
4. Si te lo pido, consulta las tablas en SQL Server con `sqlcmd -S localhost -E -d MassTransitSagaDemo` (`dbo.Users`, `dbo.RegistrationSagas`, `transport.Message`, `transport.MessageDelivery`) para enseñar lo que quedó en la base.
