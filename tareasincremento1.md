## CONTEXTO

Proyecto: **ApiAutoLavado** (.NET 10, C#, MySQL, arquitectura por capas: Persistencia / Aplicación / UI-Controllers).

Repositorio: `C:\Users\ASUS\source\repos\Backend-Autolavado`
Solución: `ApiAutoLavado/ApiAutoLavado.slnx`
Ejecución: `dotnet run --project ApiAutoLavado/ApiAutoLavado.csproj`
Variable de entorno: `CONECTION_STRING` (cargada con `CargadorEnv.Cargar()` en `Program.cs`).

**Estado actual:** ya existen `TurnoService`, `OperarioService`, repositorios, DTOs y controllers para turnos y operarios. La serialización JSON ya está configurada en snake\_case y enums como string. CORS, Forwarded Headers y manejo global de excepciones ya están configurados.

---

## OBJETIVO

Completar el **Incremento 1** implementando los requisitos **RF-003** y **RF-004** (los únicos que faltan). RF-001 y RF-002 ya están cumplidos y **no deben romperse**.

### Requisitos a implementar

- **RF-003 (prioridad Alta):** El sistema debe permitir el inicio de sesión mediante usuario y contraseña, diferenciando los roles **Administrador** y **Operario**.
- **RF-004 (prioridad Media):** El sistema debe permitir al administrador **registrar, editar y desactivar** cuentas de operarios.

---

## RESTRICCIONES TÉCNICAS OBLIGATORIAS

1. **No romper nada existente.** `POST /api/v1/turnos`, `GET /api/v1/operarios`, etc. deben seguir funcionando. Solo se les añade protección con `[Authorize]`.
2. **Arquitectura por capas respetada:** Persistencia → Aplicación (Services + DTOs) → UI (Controllers). No meter lógica de negocio en controllers.
3. **Nunca guardar contraseñas en texto plano.** Usar `BCrypt.Net-Next`.
4. **Nunca devolver** **`ContrasenaHash`** **en responses.** Usar DTOs.
5. **JWT con roles en claims.** No usar [ASP.NET](https://asp.net/) Identity (overkill para 2 roles).
6. **No hacer DELETE físico de operarios.** Desactivar con campo `Activo` / `Estado`.
7. **Mantener el patrón de inyección de dependencias** ya usado (servicios y repositorios registrados en `Program.cs`).
8. **Mantener el estilo de código y nombres existentes** (snake\_case en JSON, DTOs `XxxRequest`/`XxxResponse`, extensiones de mapeo en `MapeoExtensiones.cs`).

---

## TAREAS A REALIZAR

### Tarea 1 — Modelo de datos

Crear/ajustar las tablas:

**`Usuario`**

- `Id` PK
- `NombreUsuario` UNIQUE
- `ContrasenaHash` (BCrypt)
- `Rol` ENUM (`Administrador`, `Operario`)
- `Activo` BOOL
- `FechaCreacion` DATETIME

**`Operario`** (ajustar si ya existe)

- `Id` PK
- `UsuarioId` FK → `Usuario.Id` (UNIQUE, relación 1-a-1)
- `Nombre`, `Documento`
- `Estado` ENUM (`Activo`, `Inactivo`, `Ocupado`)
- `FechaCreacion` DATETIME

Dejar preparados (sin usarlos aún) los estados `Ocupado` en Operario y `Libre`/`Ocupada`/`Mantenimiento` en Bahía, porque el Incremento 2 los necesitará.

---

### Tarea 2 — Configuración de JWT en `Program.cs`

Añadir:

1. Lectura de variables de entorno: `JWT_KEY`, `JWT_ISSUER`, `JWT_AUDIENCE` (desde `.env`, mismo mecanismo que `CONECTION_STRING`).
2. `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` con validación de issuer, audience, lifetime y signing key.
3. `builder.Services.AddAuthorization()`.
4. En el pipeline, **en este orden exacto**:
   - `app.UseAuthentication();`
   - `app.UseAuthorization();`

**Importante:** hoy existe `UseAuthorization()` pero **no** `UseAuthentication()` ni `AddAuthentication`. Ese es el bug principal a corregir.

5. Registrar `AuthService` y `UsuarioService` en DI.

---

### Tarea 3 — DTOs (en `Aplicacion/Dtos/`)

Crear:

text

```
LoginRequest              { NombreUsuario, Contrasena }
LoginResponse             { Token, Rol, NombreUsuario, ExpiraEn }
CrearOperarioRequest      { Nombre, Documento, NombreUsuario, Contrasena }
EditarOperarioRequest     { Nombre, Documento }
OperarioResponse          { Id, Nombre, Documento, NombreUsuario, Estado, Activo }
```

svgsvg

**Regla:** `OperarioResponse` **nunca** debe incluir `ContrasenaHash`.

Añadir los mapeos correspondientes en `MapeoExtensiones.cs`.

---

### Tarea 4 — `AuthService` (Aplicacion/Services/AuthService.cs)

Responsabilidades:

- `Login(LoginRequest)`:
  1. Buscar `Usuario` por `NombreUsuario`.
  2. Verificar `BCrypt.Verify(contrasena, hash)`.
  3. Verificar `Usuario.Activo == true`.
  4. Generar JWT firmado con claims: `sub` (Id), `unique_name` (NombreUsuario), `role` (Rol).
  5. Devolver `LoginResponse` con token, rol y expiración.
  6. Si algo falla → lanzar excepción de credenciales inválidas (que el middleware global convierta en 401).
- `GenerarToken(Usuario)` como método privado.

No debe conocer detalles HTTP.

---

### Tarea 5 — `UsuarioService` (Aplicacion/Services/UsuarioService.cs)

Responsabilidades:

- `Crear(NombreUsuario, Contrasena, Rol)` → hashea con BCrypt y persiste.
- `ObtenerPorNombreUsuario(string)`.
- `CambiarEstado(int id, bool activo)`.

Registrar en DI.

---

### Tarea 6 — Ajustar `OperarioService`

- `Crear(CrearOperarioRequest)`:
  - Crear **transaccionalmente** `Usuario` (rol `Operario`, contraseña hasheada) **y** `Operario` apuntando a ese `UsuarioId`.
  - Si falla uno → rollback de ambos.
- `Editar(int id, EditarOperarioRequest)` → actualiza `Nombre` y `Documento`.
- `Desactivar(int id)` → pone `Usuario.Activo = false` y `Operario.Estado = Inactivo`.
- `ObtenerTodos`, `ObtenerActivos`, `ObtenerInactivos`, `ObtenerOcupados` ya existen, mantenerlos.

---

### Tarea 7 — `AuthController` (UI/Controllers/AuthController.cs)

text

```
POST /api/v1/auth/login      → [AllowAnonymous] → AuthService.Login
```

svgsvg

---

### Tarea 8 — `OperariosController` (ajustar el existente)

Añadir los endpoints que faltan y proteger TODOS con rol Administrador:

text

```
POST   /api/v1/operarios                     → Crear        [Authorize(Roles="Administrador")]
PUT    /api/v1/operarios/{id}                → Editar       [Authorize(Roles="Administrador")]
PATCH  /api/v1/operarios/{id}/desactivar     → Desactivar   [Authorize(Roles="Administrador")]
GET    /api/v1/operarios                     → ObtenerTodos [Authorize(Roles="Administrador")]
GET    /api/v1/operarios/activos             → [Authorize(Roles="Administrador")]
GET    /api/v1/operarios/inactivos           → [Authorize(Roles="Administrador")]
GET    /api/v1/operarios/ocupados            → [Authorize(Roles="Administrador")]
```

svgsvg

---

### Tarea 9 — Proteger `TurnosController`

Añadir `[Authorize]` a nivel de controller (cualquier rol autenticado puede operar turnos). No cambiar la lógica interna.

---

### Tarea 10 — Variables de entorno

Documentar (y añadir al `.env` de ejemplo) estas claves nuevas:

text

```
JWT_KEY=<clave larga y aleatoria, mínimo 32 caracteres>
JWT_ISSUER=ApiAutoLavado
JWT_AUDIENCE=ApiAutoLavadoClientes
JWT_EXPIRACION_MINUTOS=60
```

svgsvg

---

### Tarea 11 — Script SQL de migración

Generar el script SQL necesario para:

- Crear tabla `Usuario`.
- Añadir `UsuarioId` a `Operario`.
- Insertar un usuario **Administrador** inicial de prueba (credenciales documentadas en el README o en un archivo aparte).
- No borrar ni modificar datos existentes de otras tablas.

---

### Tarea 12 — Pruebas manuales mínimas

Documentar cómo verificar (con `curl` o Scalar/OpenAPI) los siguientes casos:

| **CasoEntradaResultado esperado**      |                                   |                                 |
| -------------------------------------- | --------------------------------- | ------------------------------- |
| Login correcto                         | admin / contraseña correcta       | 200 + token JWT                 |
| Login incorrecto                       | admin / contraseña mala           | 401                             |
| Login usuario inactivo                 | usuario desactivado               | 401                             |
| Crear operario sin token               | POST /operarios                   | 401                             |
| Crear operario con token Operario      | POST /operarios                   | 403                             |
| Crear operario con token Administrador | POST /operarios con datos válidos | 201 + OperarioResponse sin hash |
| Editar operario (admin)                | PUT /operarios/{id}               | 200                             |
| Desactivar operario (admin)            | PATCH /operarios/{id}/desactivar  | 200, estado `Inactivo`          |
| Registrar turno con token              | POST /turnos                      | 201, turno `T-001`              |
| Registrar turno sin token              | POST /turnos                      | 401                             |

---

## CRITERIOS DE ACEPTACIÓN

El Incremento 1 se considera terminado cuando:

- □ 

  `POST /api/v1/auth/login` devuelve JWT válido con claim `role`.
- □ 

  `Program.cs` tiene `AddAuthentication`, `AddAuthorization`, `UseAuthentication` y `UseAuthorization` en orden correcto.
- □ 

  Todos los endpoints existentes están protegidos con `[Authorize]` o `[Authorize(Roles="...")]`, excepto `/auth/login`.
- □ 

  `OperarioService.Crear` crea `Usuario` + `Operario` transaccionalmente.
- □ 

  `OperarioService.Editar` y `OperarioService.Desactivar` existen y funcionan.
- □ 

  Las contraseñas están hasheadas con BCrypt en la BD.
- □ 

  Ningún DTO expone `ContrasenaHash`.
- □ 

  `POST /api/v1/turnos` sigue funcionando igual que antes (no regresión) y genera `T-001`, `T-002`...
- □ 

  Existe usuario Administrador inicial documentado.
- □ 

  El script SQL de migración está incluido.

---

## LO QUE NO DEBES HACER

- No implementar todavía las etapas de servicio (eso es Incremento 3).
- No implementar el panel de bahías ni la asignación automática (Incremento 2).
- No implementar el portal de consulta por placa ni por hash (Incremento 4).
- No cambiar la estructura de `Turno` más allá de lo estrictamente necesario.
- No usar [ASP.NET](https://asp.net/) Identity.
- No cambiar la serialización JSON ya configurada.
- No hacer DELETE físico de operarios.

---

## ENTREGABLE

Al final, entrega:

1. Lista de archivos **creados** (con su ruta completa).
2. Lista de archivos **modificados** (con un resumen de qué cambió en cada uno).
3. Script SQL de migración.
4. Comandos `curl` de prueba (uno por caso de la tabla de pruebas).
5. Notas de cualquier decisión técnica tomada durante la implementación.