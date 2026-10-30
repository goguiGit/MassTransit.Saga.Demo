---
description: Levanta toda la demo (RabbitMQ en Podman, Registration.Api, Notifications.Worker y el panel Angular), cada uno en su ventana, y comprueba que responden.
argument-hint: [-Transport RabbitMq|Sql|AzureServiceBus] [-SkipWorker] [-SkipWeb] [-NoBrowser]
---

Levanta la demo completa y dime cómo ha quedado.

1. Ejecuta `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/demo-up.ps1 $ARGUMENTS` (usa `pwsh` si existe). Cada servicio se abre en su propia ventana de consola; el script espera a que cada uno responda antes de seguir y al final imprime las URLs.
2. Si algún paso falla, lee el error del script (dice qué servicio no respondió y en qué URL) y mira la ventana de ese servicio. Causas habituales: la máquina de Podman tarda en arrancar; el puerto 5100, 5200 o 4200 está ocupado por una ejecución anterior (`scripts/demo-down.ps1` lo libera); con `-Transport AzureServiceBus` falta la cadena en user-secrets `Saga.Secrets`; con `-Transport Sql` el worker necesita que la API haya creado la base antes.
3. Resume en español qué quedó arrancado y con qué transporte, con las URLs: API (5100), worker (5200), consola de RabbitMQ (15672, `guest`/`guest`) y panel (4200). Si se usó `-SkipWorker`, recuerda que el mensaje de verificación quedará esperando en la cola `EmailSender` hasta arrancar el worker.
4. Sugiere el siguiente paso: abrir el panel en http://localhost:4200, o `/registration-flow` para el flujo automático.
