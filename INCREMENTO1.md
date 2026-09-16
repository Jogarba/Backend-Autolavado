# Incremento 1 — Autenticación y gestión de operarios

Implementación de **RF-003** (inicio de sesión con roles Administrador / Operario) y
**RF-004** (registrar, editar y desactivar cuentas de operarios) sin romper RF-001 / RF-002.

## 1. Archivos creados

| Archivo | Descripción |
| --- | --- |
| `ApiAutoLavado/Domain/Models/Usuario.cs` | Modelo de usuario (credenciales + rol). |
| `ApiAutoLavado/Domain/Enums/RolUsuario.cs` | Enum `Administrador` / `Operario` (serializado en MAYÚSCULAS). |
| `ApiAutoLavado/Domain/Exceptions/CredencialesInvalidasException.cs` | Excepción que el middleware convierte en **401**. |
| `ApiAutoLavado/Aplicacion/Configuracion/JwtOpciones.cs` | Opciones de JWT (key, issuer, audience, expiración). |
| `ApiAutoLavado/Aplicacion/Dtos/LoginRequest.cs` | `{ nombre_usuario, contrasena }`. |
| `ApiAutoLavado/Aplicacion/Dtos/LoginResponse.cs` | `{ token, rol, nombre_usuario, expira_en }`. |
| `ApiAutoLavado/Aplicacion/Dtos/EditarOperarioRequest.cs` | `{ nombres, apellidos, documento, telefono }`. |
| `ApiAutoLavado/Aplicacion/Repositorios/IUsuarioRepository.cs` | Contrato del repositorio de usuarios. |
| `ApiAutoLavado/Aplicacion/Repositorios/ITransaccionBd.cs` | Abstracción de transacción de BD. |
| `ApiAutoLavado/Aplicacion/Repositorios/IFabricaTransacciones.cs` | Fábrica de transacciones. |
| `ApiAutoLavado/Aplicacion/Services/IAuthService.cs` | Contrato de autenticación. |
| `ApiAutoLavado/Aplicacion/Services/AuthService.cs` | Login + generación de JWT. |
| `ApiAutoLavado/Aplicacion/Services/IUsuarioService.cs` | Contrato del servicio de usuarios. |
| `ApiAutoLavado/Aplicacion/Services/UsuarioService.cs` | Alta / consulta / cambio de estado de usuarios (BCrypt). |
| `ApiAutoLavado/Persistencia/Repositorios/UsuarioRepository.cs` | Acceso a datos de `usuarios`. |
| `ApiAutoLavado/Persistencia/Repositorios/FabricaTransaccionesMySql.cs` | Implementación de transacciones MySQL. |
| `ApiAutoLavado/UI/Controllers/AuthController.cs` | `POST /api/v1/auth/login`. |
| `ApiAutoLavado/Sql/migracion_incremento1.sql` | Script SQL de migración + admin inicial. |
| `.env.example` | Plantilla de variables de entorno. |

## 2. Archivos modificados

| Archivo | Cambio |
| --- | --- |
| `ApiAutoLavado/ApiAutoLavado.csproj` | Se añadieron `BCrypt.Net-Next` y `Microsoft.AspNetCore.Authentication.JwtBearer`. |
| `ApiAutoLavado/Program.cs` | `AddAuthentication` + `AddJwtBearer`, `AddAuthorization`, `UseAuthentication` antes de `UseAuthorization`; registro de `IUsuarioRepository`, `IFabricaTransacciones`, `IAuthService`, `IUsuarioService`; lectura de `JWT_*`. |
| `ApiAutoLavado/Domain/Enums/EstadoOperario.cs` | Se añadió el estado `Inactivo`. |
| `ApiAutoLavado/Domain/Models/Operario.cs` | Se añadieron `UsuarioId`, `NombreUsuario` (solo lectura desde BD) y `FechaCreacion`. |
| `ApiAutoLavado/Aplicacion/Dtos/CrearOperarioRequest.cs` | Se añadieron `NombreUsuario` y `Contrasena`. |
| `ApiAutoLavado/Aplicacion/Dtos/OperarioResponse.cs` | Se añadieron `NombreUsuario` y `FechaCreacion` (sigue sin exponer `ContrasenaHash`). |
| `ApiAutoLavado/Aplicacion/Dtos/MapeoExtensiones.cs` | Mapeo de los nuevos campos de `Operario`. |
| `ApiAutoLavado/Aplicacion/Repositorios/IOperarioRepository.cs` | `IntentarAgregar` con transacción, `Actualizar` y `Desactivar`. |
| `ApiAutoLavado/Persistencia/Repositorios/OperarioRepository.cs` | `LEFT JOIN` con `usuarios`; insert/update/desactivar; soporte de transacción. |
| `ApiAutoLavado/Persistencia/Mapeo/FilasBd.cs` | `OperarioFila` extendida + nueva `UsuarioFila`. |
| `ApiAutoLavado/Persistencia/InicializadorBaseDatos.cs` | Creación de `usuarios`, columnas nuevas de `operarios` y siembra del administrador. |
| `ApiAutoLavado/Aplicacion/Services/OperarioService.cs` | `Crear` transaccional (Usuario + Operario), `Editar` y `Desactivar`. |
| `ApiAutoLavado/Aplicacion/Services/IOperarioService.cs` | Se añadieron `Editar` y `Desactivar`. |
| `ApiAutoLavado/UI/Middleware/ManejadorExcepcionesMiddleware.cs` | `CredencialesInvalidasException` → **401**. |
| `ApiAutoLavado/UI/Controllers/OperariosController.cs` | `[Authorize(Roles="Administrador")]` + `PUT /{id}` y `PATCH /{id}/desactivar`. |
| `ApiAutoLavado/UI/Controllers/TurnosController.cs` | `[Authorize]`. |
| `ApiAutoLavado/UI/Controllers/BahiasController.cs` | `[Authorize]`. |
| `ApiAutoLavado/UI/Controllers/ServiciosController.cs` | `[Authorize]`. |
| `.env` | Variables `JWT_*` y `ADMIN_*`. |

## 3. Script SQL

`ApiAutoLavado/Sql/migracion_incremento1.sql` crea `usuarios`, añade `usuario_id` /
`fecha_creacion` a `operarios` e inserta el administrador inicial. La aplicación aplica
estos mismos cambios automáticamente al arrancar, por lo que el script es opcional.

**Usuario administrador inicial:** `admin` / `Admin123*` (configurable con `ADMIN_USUARIO`
y `ADMIN_CONTRASENA`; solo se crea si la tabla `usuarios` está vacía).

## 4. Pruebas manuales (curl)

> Base: `http://localhost:5053`. Reemplace `<TOKEN>` y `<ID>`.

```bash
# 1. Login correcto -> 200 + token JWT
curl -i -X POST http://localhost:5053/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"nombre_usuario\":\"admin\",\"contrasena\":\"Admin123*\"}"

# 2. Login incorrecto -> 401
curl -i -X POST http://localhost:5053/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"nombre_usuario\":\"admin\",\"contrasena\":\"claveMala123\"}"

# 3. Crear operario sin token -> 401
curl -i -X POST http://localhost:5053/api/v1/operarios \
  -H "Content-Type: application/json" \
  -d "{\"nombres\":\"Ana\",\"apellidos\":\"Lopez\",\"documento\":\"123456\",\"telefono\":\"3001112233\",\"nombre_usuario\":\"ana\",\"contrasena\":\"Operario123*\"}"

# 4. Crear operario con token Administrador -> 201 (sin contrasena_hash)
curl -i -X POST http://localhost:5053/api/v1/operarios \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN_ADMIN>" \
  -d "{\"nombres\":\"Ana\",\"apellidos\":\"Lopez\",\"documento\":\"123456\",\"telefono\":\"3001112233\",\"nombre_usuario\":\"ana\",\"contrasena\":\"Operario123*\"}"

# 5. Crear operario con token Operario -> 403
curl -i -X POST http://localhost:5053/api/v1/operarios \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN_OPERARIO>" \
  -d "{\"nombres\":\"Luis\",\"apellidos\":\"Rojas\",\"documento\":\"654321\",\"telefono\":\"3004445566\",\"nombre_usuario\":\"luis\",\"contrasena\":\"Operario123*\"}"

# 6. Editar operario (admin) -> 200
curl -i -X PUT http://localhost:5053/api/v1/operarios/<ID> \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN_ADMIN>" \
  -d "{\"nombres\":\"Ana Maria\",\"apellidos\":\"Lopez\",\"documento\":\"123456\",\"telefono\":\"3009998877\"}"

# 7. Desactivar operario (admin) -> 200, estado INACTIVO
curl -i -X PATCH http://localhost:5053/api/v1/operarios/<ID>/desactivar \
  -H "Authorization: Bearer <TOKEN_ADMIN>"

# 8. Login de operario desactivado -> 401
curl -i -X POST http://localhost:5053/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"nombre_usuario\":\"ana\",\"contrasena\":\"Operario123*\"}"

# 9. Registrar turno con token -> 201, numero_turno T-001
curl -i -X POST http://localhost:5053/api/v1/turnos \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN_ADMIN>" \
  -d "{\"placa\":\"ABC123\",\"tipo_vehiculo\":\"AUTO\",\"telefono_cliente\":\"3000000000\",\"id_servicio\":1,\"id_operario\":1,\"id_bahia\":1}"

# 10. Registrar turno sin token -> 401
curl -i -X POST http://localhost:5053/api/v1/turnos \
  -H "Content-Type: application/json" \
  -d "{\"placa\":\"ABC123\",\"tipo_vehiculo\":\"AUTO\",\"telefono_cliente\":\"3000000000\",\"id_servicio\":1,\"id_operario\":1,\"id_bahia\":1}"
```

Resultado de la verificación local: todos los casos anteriores dieron el código esperado
(200 / 401 / 403 / 201, `estado=INACTIVO` y `numero_turno=T-001`).

## 5. Notas y decisiones técnicas

- **JWT en claims `sub`, `unique_name` y `role`.** Se configuró `MapInboundClaims = false`,
  `RoleClaimType = "role"` y `NameClaimType = "unique_name"` para que `[Authorize(Roles="...")]`
  funcione con los nombres de claim cortos exigidos.
- **No se usó ASP.NET Identity.** El rol vive en la columna `usuarios.rol` y viaja como claim.
- **Contraseñas con BCrypt** (`BCrypt.Net-Next`). Nunca se devuelve `ContrasenaHash`: no existe
  en ningún DTO de salida.
- **Creación transaccional.** `OperarioService.Crear` abre una transacción con
  `IFabricaTransacciones`, inserta `Usuario` + `Operario` y hace rollback de ambos si algo falla.
- **Desactivación lógica.** No hay DELETE físico: se pone `usuarios.activo = 0` y
  `operarios.estado = 'INACTIVO'`.
- **Compatibilidad con lo existente.** Se mantuvieron los campos y nombres previos de `Operario`
  (`nombres`, `apellidos`, `documento`, `telefono`, `activo`, `estado`) y se agregaron los nuevos
  (`usuario_id`, `fecha_creacion`, `nombre_usuario`). `CrearOperarioRequest` conserva los campos
  anteriores y ahora exige también `nombre_usuario` y `contrasena`.
- **`EstadoOperario.Inactivo`** se añadió para reflejar la desactivación; el campo `Activo` se
  conserva porque ya era la base de los filtros `activos` / `inactivos`.
- **Bahías:** se mantuvo el estado existente `Disponible` (equivalente al `Libre` del enunciado),
  además de `Ocupada` y `Mantenimiento`.
- **Admin inicial configurable** por `ADMIN_USUARIO` / `ADMIN_CONTRASENA`; por defecto
  `admin` / `Admin123*`. Solo se siembra si la tabla `usuarios` está vacía.
