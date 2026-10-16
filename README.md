# Frontenis – ligas y torneos

Aplicación web/móvil (PWA) para gestionar **ligas y torneos de frontenis** con clasificación.

- **Backend**: .NET 8 Web API · EF Core · **PostgreSQL (Neon)** · JWT
- **Frontend**: React + TypeScript (PWA instalable en móvil, diseño responsive)

## Arrancar

```bash
# 1) Base de datos (PostgreSQL; o usa tu propia BD y cambia ConnectionStrings:Default)
docker compose up -d

# 2) API  -> http://localhost:5080  (Swagger en /swagger)
cd api
dotnet test
dotnet run --project src/Frontenis.Api
cd ..

# 3) Web  -> http://localhost:5173
cd web && cp .env.example .env && npm install && npm run dev
```

La API crea la base de datos al arrancar (`EnsureCreated`; el esquema equivalente está en `sql/schema.sql`; el antiguo de SQL Server, en `sql/sqlserver/schema.sql`)
y siembra el administrador definido en `appsettings.json` (`Admin:Username` / `Admin:Password`,
por defecto `admin` / `Cambia-Esta-Clave-2026!`). **Cámbialos y cambia `Jwt:Key` antes de publicar:** en producción
la API se niega a arrancar con la clave JWT de ejemplo, y la contraseña inicial debe tener al menos 12 caracteres.
Cualquier visitante puede consultar; solo el administrador crea o modifica datos.

### Seguridad de la autenticación

- Contraseñas con PBKDF2-SHA256 (600.000 iteraciones, sal aleatoria); los hashes antiguos se actualizan solos al iniciar sesión.
- Límite de intentos: 5 fallos seguidos bloquean al usuario 15 minutos y 20 fallos bloquean la IP; además, 8 peticiones por minuto
  y por IP a `/api/auth/*` y 300 por minuto al resto. Responde 429 con `Retry-After`.
- Respuesta idéntica y mismo tiempo para «usuario inexistente» y «contraseña incorrecta».
- JWT firmado con HS256 (algoritmo fijado), caduca a las 4 h (`Jwt:ExpiresMinutes`).
- El administrador puede cambiar su contraseña en `/cuenta` (pide la actual; mínimo 12 caracteres).
- Cabeceras de seguridad en la API y CSP en la web (`web/public/_headers`).

## Reglas del dominio

| Parámetro | Descripción |
|---|---|
| Nº de parejas / grupos / parejas por grupo | Configurables. Si se indican los tres deben cumplir `grupos × parejas por grupo = parejas`; si no, los grupos se equilibran. |
| Tipo | **Liga** (solo clasificación) o **Torneo** (grupos + eliminatorias con los N primeros de cada grupo). |
| Puntos por victoria / derrota | Configurables (p. ej. 3 y 0). |
| Ida y vuelta | Opcional. |
| Resultado | Puntos de cada pareja (p. ej. 15-11). No hay empates. |
| Desempate | Puntos → diferencia de tantos → tantos a favor. |

**Emparejamiento aleatorio**: se marcan jugadores (delanteros y zagueros) y se forman parejas 1 zaguero + 1 delantero.
Si sobran de un tipo, se emparejan entre sí (caso extremo); si el total es impar, uno queda sin pareja.

**Flujo**: crear competición → inscribir parejas (aleatorias o manuales) → sorteo de grupos y calendario
(todos contra todos, método del círculo) → introducir resultados → (torneo) generar cuadro final con byes y cabezas de serie.

## Reservas de entrenos

Los jugadores ven las franjas de entrenamiento de la semana y la siguiente (`/reservas`); el administrador las gestiona en `/reservas/gestion`.

- **Plantilla semanal** (`SlotTemplate`): el horario que se repite (p. ej. lunes 17-18, 18-20 y 20-22; viernes 19-22). Editarla no cambia las franjas ya generadas.
- **Franja** (`TrainingSlot`): una fecha y un horario concretos. Se generan desde la plantilla (acción idempotente) y se pueden mover, cancelar o crear sueltas.
- **Apertura**: cada franja guarda `OpensAt`. Por defecto toda la semana (lunes a domingo) se abre el viernes anterior a las 19:00 (hora de Madrid); el administrador puede abrir antes una franja o una semana entera. Se configura en `Bookings` (`MinPlayers`, `OpenDay`, `OpenTime`; variables de entorno `Bookings__MinPlayers`, etc.).
- **Estado calculado** (`Pending`, `Open`, `Closed`, `Cancelled`) y **aviso de pocos jugadores** (menos de `MinPlayers` con la franja abierta): los calcula el dominio con la hora de Madrid (`IClock`), por eso las consultas de franjas no se cachean.
- **Vínculo por dispositivo**: sin cuentas. Cada móvil genera un identificador aleatorio (se guarda en `localStorage` y viaja en la cabecera `X-Device-Token`); el servidor solo guarda su hash SHA-256 (`DeviceLink`). El jugador se elige una vez ("Soy X"); un móvil pertenece a un solo jugador y viceversa. Si cambia de móvil, el administrador lo libera en `/reservas/gestion`. Endpoints: `api/device` (`me`, `available-players`, `link`).
- **Apuntarse**: con la franja abierta, el jugador vinculado se apunta o se quita con un toque (`POST/DELETE api/slots/{id}/attend`, con límite de peticiones). `SlotAttendance.RegisteredByPlayerId` guarda quién apuntó (vacío = administrador).
- **Apadrinados** (`PlayerDelegate`): el administrador define qué jugadores puede apuntar además de sí mismo un titular (`api/player-delegates`); hay jugadores que apuntan a varios y otros solo a sí mismos.
- La API necesita datos de zona horaria (`Europe/Madrid`); la imagen `mcr.microsoft.com/dotnet/aspnet` los incluye.

## Estructura del repositorio

```
api/   backend .NET: Frontenis.sln, src/ (Domain, Application, Infrastructure, Api) y tests/
web/   PWA React (Cloudflare Workers)
sql/   esquema de PostgreSQL (y el antiguo de SQL Server en sql/sqlserver)
Dockerfile, docker-compose.yml   en la raíz (Render construye desde aquí)
```

## Arquitectura (Clean Architecture / SOLID)

```
Domain          entidades y reglas puras (Competition es el aggregate root); estrategias intercambiables:
                IPairingStrategy, IGroupDistributor, IScheduleGenerator, IStandingsCalculator, IBracketGenerator
Application     casos de uso (servicios), DTOs y puertos (repositorios, hasher, tokens)
Infrastructure  EF Core/PostgreSQL (Npgsql), repositorios, JWT, PBKDF2
Api             controladores finos + middleware de errores
api/tests       xUnit sobre la lógica de dominio
web             PWA React
```

Cada pieza depende de abstracciones (DIP), cada servicio tiene una única responsabilidad (SRP) y
añadir un nuevo criterio de desempate o sistema de calendario solo requiere una nueva implementación (OCP).

## Estructura del backend

Arquitectura por capas (las dependencias apuntan siempre hacia el dominio). Cada carpeta tiene su propio namespace.

| Capa | Carpetas | Responsabilidad |
|---|---|---|
| `Frontenis.Domain` | `Entities`, `Enums`, `Exceptions`, `Interfaces`, `Models`, `Services` | Reglas de negocio puras: entidades, cálculo de clasificaciones, sorteos, estadísticas. Sin dependencias externas. |
| `Frontenis.Application` | `Interfaces/{Repositories,Services,Security,Caching}`, `Services`, `Caching`, `Dtos`, `Mapping`, `Security`, `Models`, `Exceptions`, `Extensions` | Casos de uso. Cada funcionalidad separa **consultas** (`I…Queries`) y **comandos** (`I…Commands`); las consultas públicas se decoran con la caché de lecturas (`Caching/Cached…`). |
| `Frontenis.Infrastructure` | `Persistence`, `Repositories`, `Security`, `Extensions` | EF Core / PostgreSQL, actualización del esquema (`SchemaScripts`), JWT, hash de contraseñas y registro de dependencias. |
| `Frontenis.Api` | `Controllers`, `Middleware`, `Extensions`, `Authorization`, `RateLimiting`, `HostedServices` | HTTP: controladores finos, autenticación, límites de peticiones, CORS y arranque. |

Normas del proyecto: un tipo por archivo (los DTO se agrupan por funcionalidad), cada interfaz en su carpeta `Interfaces`, y namespace igual a la carpeta.

## Traspaso de Azure SQL a Neon (PostgreSQL)

1. Crea el proyecto en Neon y copia su cadena de conexión (formato `postgresql://usuario:clave@host/bd?sslmode=require`).
2. Copia los datos. Es **reejecutable**: cada ejecución vacía el destino y lo vuelve a llenar, así que puedes ensayarlo cuantas veces quieras
   mientras Azure sigue en producción. Sin `--yes` solo muestra los recuentos.

```powershell
$env:SOURCE_SQLSERVER = "<cadena de conexión de Azure SQL>"
$env:TARGET_POSTGRES  = "<URL de Neon>"
dotnet run --project api/tools/Frontenis.DataMigration           # ensayo: solo recuentos
dotnet run --project api/tools/Frontenis.DataMigration -- --yes  # vacía el destino y copia
```

3. El día del cambio: repite el paso 2 con `--yes` (copia final), pon `ConnectionStrings__Default` en Render con la URL de Neon
   y despliega. Conserva Azure unos días como respaldo.
