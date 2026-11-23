# Revisión del clon de Programación 6

Fecha: 18 de noviembre de 2026. Se conserva el juego descargado: mapas Desert, Forest y Night City, estilo retro, canvas, SignalStore, chat, SignalR, MQTT y backend C#. Las fuentes locales consultadas se inventarían con hashes en fuentes.json (A.1–A.9 y contenidos semanales).

## Fallos corregidos

- Compose no incluía PostgreSQL ni Redis. Se añadieron servicios y volúmenes propios, puertos 5434/6381 y migraciones automáticas. EMQX conserva los puertos MQTT del clon. InfluxDB/Grafana se activan aparte con el perfil observability; Grafana usa 3010 y no interfiere con Hoteles.
- El proyecto requiere .NET 10, pero la máquina solo tenía el ejecutable sin SDK. preparar.sh instala el SDK en .tools de esta materia. El laboratorio A.4 sugiere .NET 8; se conserva .NET 10 para mantener compatible el clon descargado.
- El Hub aceptaba conexiones y nombres arbitrarios. Ahora exige JWT, usa la identidad autenticada y comprueba pertenencia a sala. Angular envía el token en la conexión SignalR.
- Logout escribía una lista de revocación en Redis que no se consultaba. Se comprueba al validar JWT, y Angular comunica el cierre al backend.
- Entradas simultáneas podían superar cuatro jugadores. La actualización condicional en PostgreSQL y el control de miembros limitan la capacidad; entrar repetidamente no incrementa el contador.
- Cualquiera podía iniciar o borrar una sala. Ahora solo su creador; se validan miembros y colisiones dentro de una misma sala.
- El ganador persistido era el cliente que notificaba el final. Se resuelve el ganador en la lista de jugadores; las puntuaciones repetidas del mismo jugador se deduplican por sala.
- Cada impacto se aplicaba por SignalR y MQTT, descontando vida dos veces. SignalR aplica el daño; MQTT conserva las notificaciones e historial.
- Un temporizador se creaba al destruir el componente, en lugar de cancelarse. Ahora se cancela el temporizador que se inició.
- El canvas cambiaba su resolución lógica al tamaño del navegador, desalineando colisiones. Se mantiene el sistema de coordenadas 400×400 y el escalado CSS.
- Escribir en el chat podía mover o disparar el tanque. El manejador de juego ignora los campos de texto.
- Se añadieron guard de acceso, rutas diferidas de perfil y ranking, caché Redis de ranking (TTL diez segundos), middleware de errores y captura de excepciones sin detalles internos en HTTP.
- Se actualizaron los mocks desfasados de las pruebas existentes de SignalR y SignalStore.

## Relación con el curso

| Fuente | Aplicación |
| --- | --- |
| A.1–A.3 | Angular, SignalStore/computed, canvas, mapas, movimiento y colisiones del clon |
| A.4 | ASP.NET, JWT, PostgreSQL, registro, salas y ranking |
| A.5 | SignalR: movimiento, chat, conexiones y sincronización entre dos clientes |
| A.6 | EMQX/MQTT, power-ups, notificaciones e historial Redis |
| A.7 | Índices y AsNoTracking existentes, caché de ranking y revocación en Redis |
| A.8 | Se conservan scripts k6/Artillery y configuración InfluxDB/Grafana del clon |
| A.9 | MiniProfiler existente, rutas diferidas y OnPush; se corrige la fuga del temporizador |

## Verificación reproducible

Con la demo arrancada, desde este repositorio:

```bash
node scripts/test-system.mjs
dotnet build
cd ../prog_6_capstone
npm run build
npm test -- --watch=false
```

La prueba de sistema registra cinco usuarios, intenta cuatro entradas concurrentes en una sala ya ocupada y acepta solo tres. Usa dos conexiones SignalR reales para inicio, chat y movimiento; comprueba permisos y logout. El navegador registra dos cuentas, crea/une sala y abre la partida en ambas pestañas. Registros y capturas acompañan este informe.

## Límites comprobados y tareas de laboratorio

Se trata de una demo local de una instancia; la membresía vive en memoria y las sesiones activas se cierran al reiniciar el backend. El motor original usa colisiones del cliente y no es un servidor autoritativo contra trampas. No se declara apto para competición pública.

No se simularon 1000 jugadores ni se inventaron métricas de Lighthouse, dotTrace, JMeter o Gatling. Los scripts heredados de carga no equivalen a pruebas ejecutadas. Tampoco se configuraron sharding ni réplicas de PostgreSQL: esos ejercicios de infraestructura de A.7 y los informes de profiling siguen separados del funcionamiento de la demo. Se actualizaron Microsoft.OpenApi y System.Security.Cryptography.Pkcs para corregir los avisos heredados; la compilación final del backend termina con cero avisos y cero errores.

## Resultado final

Pruebas de API y compilación aprobadas. Veintitrés pruebas Angular aprobadas. La prueba de API usa dos conexiones SignalR reales y comprueba concurrencia y revocación. La partida entre dos navegadores también fue comprobada durante la revisión.
