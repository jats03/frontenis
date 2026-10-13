# Frontenis – ligas y torneos

Aplicación web/móvil (PWA) para gestionar **ligas y torneos de frontenis** con clasificación.

- **Backend**: .NET 8 Web API · EF Core · **SQL Server** · JWT
- **Frontend**: React + TypeScript (PWA instalable en móvil, diseño responsive)

## Arrancar

```bash
# 1) Base de datos (o usa tu SQL Server y cambia ConnectionStrings:Default)
docker compose up -d

# 2) API  -> http://localhost:5080  (Swagger en /swagger)
dotnet test
dotnet run --project src/Frontenis.Api

# 3) Web  -> http://localhost:5173
cd web && cp .env.example .env && npm install && npm run dev
```

La API crea la base de datos al arrancar (`EnsureCreated`; el esquema equivalente está en `sql/schema.sql`)
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

## Arquitectura (Clean Architecture / SOLID)

```
Domain          entidades y reglas puras (Competition es el aggregate root); estrategias intercambiables:
                IPairingStrategy, IGroupDistributor, IScheduleGenerator, IStandingsCalculator, IBracketGenerator
Application     casos de uso (servicios), DTOs y puertos (repositorios, hasher, tokens)
Infrastructure  EF Core/SQL Server, repositorios, JWT, PBKDF2
Api             controladores finos + middleware de errores
tests           xUnit sobre la lógica de dominio
web             PWA React
```

Cada pieza depende de abstracciones (DIP), cada servicio tiene una única responsabilidad (SRP) y
añadir un nuevo criterio de desempate o sistema de calendario solo requiere una nueva implementación (OCP).
