---
description: Apaga lo que levantó /demo-up (panel Angular, worker y API; con -Rabbit también el contenedor de RabbitMQ y con -Podman la máquina).
argument-hint: [-Rabbit] [-Podman]
---

Apaga la demo.

1. Ejecuta `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/demo-down.ps1 $ARGUMENTS` (usa `pwsh` si existe). Para los procesos que escuchan en 4200, 5200 y 5100; con `-Rabbit` para además el contenedor `rabbitmq`, y con `-Podman` también la máquina de Podman (libera sus 2 GB de RAM).
2. Confirma con `netstat -ano | findstr ":5100 :5200 :4200"` que los puertos quedaron libres y dímelo en una línea. Las ventanas de consola que abrió `/demo-up` quedan abiertas mostrando el último log; se cierran a mano.
