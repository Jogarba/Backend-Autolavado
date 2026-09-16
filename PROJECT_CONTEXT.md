# Contexto del proyecto: ApiAutoLavado

## Resumen
Proyecto API para la gestión de un autolavado. Implementa registros de turnos, gestión de operarios, bahías y servicios. Está organizado en capas de Persistencia, Aplicación y UI (Controllers). Objetivo principal: permitir registrar ingreso de vehículos, asignar operarios/bahías y gestionar el ciclo del turno.

## Ubicación y solución
- Ruta del repositorio local: C:\Users\ASUS\source\repos\Backend-Autolavado
- Archivo de solución: C:\Users\ASUS\source\repos\Backend-Autolavado\ApiAutoLavado\ApiAutoLavado.slnx
- Branch activo: main
- Remote origin: https://github.com/Jogarba/Backend-Autolavado

## Entorno de desarrollo
- Visual Studio: Microsoft Visual Studio Community 2026 (18.10.1)
- Target framework: .NET 10 (proyectos apuntan a .NET 10)
- Shell preferido: powershell.exe

## Cómo ejecutar
1. Definir variable de entorno en archivo .env en la raíz del proyecto: CONECTION_STRING (cadena de conexión MySQL). El proyecto usa CargadorEnv.Cargar() en Program.cs.
2. Abrir la solución ApiAutoLavado en Visual Studio o desde terminal: `dotnet run --project ApiAutoLavado/ApiAutoLavado.csproj`.

## Configuración y middleware relevante (Program.cs)
- Carga .env antes de construir la app.
- Añade controladores y configura serialización JSON con snake_case y enum como string.
- Configura Forwarded Headers (X-Forwarded-For / Proto).
- Habilita CORS (AllowAnyOrigin/Method/Header).
- Registra repositorios y servicios como singletons (Bahia, Operario, Servicio, Turno y sus servicios).
- Documentación OpenAPI/Scalar.
- Usa middleware de manejo global de excepciones y autenticación JWT (`AddAuthentication` + `AddJwtBearer`) con `UseAuthentication()` antes de `UseAuthorization()`.

## Requisitos funcionales (RF) y estado
- RF-001 (Registrar ingreso: placa, tipo, servicio, hora) — Cumple.
  - Endpoint POST /api/v1/turnos. DTO: `CrearTurnoRequest` requiere Placa, TipoVehiculo, TelefonoCliente, IdServicio, IdOperario, IdBahia.
  - Fecha de ingreso (FechaIngreso) asignada en `TurnoService` con DateTime.UtcNow.

- RF-002 (Generar número de turno consecutivo) — Cumple.
  - Implementado en `TurnoService.GenerarNumeroTurno(DateTime fecha)` que mantiene un contador reiniciado por fecha y devuelve formato `T-XXX`.

- RF-003 (Inicio de sesión con usuario/contraseña; roles Administrador y Operario) — Cumple (Incremento 1).
  - `POST /api/v1/auth/login` (`AuthController` + `AuthService`) valida BCrypt y devuelve JWT con claims `sub`, `unique_name` y `role`. `Program.cs` configura `AddAuthentication`/`AddJwtBearer`, `AddAuthorization` y `UseAuthentication` antes de `UseAuthorization`.

- RF-004 (Administrador registra/edita/desactiva operarios) — Cumple (Incremento 1).
  - `POST /api/v1/operarios` crea `Usuario` + `Operario` transaccionalmente; `PUT /api/v1/operarios/{id}` edita y `PATCH /api/v1/operarios/{id}/desactivar` desactiva. Todos los endpoints de operarios requieren rol `Administrador`.
  - Contraseñas hasheadas con BCrypt; ningún DTO expone `ContrasenaHash`.

## Endpoints principales detectados
- Operarios
  - GET /api/v1/operarios — ObtenerTodos
  - GET /api/v1/operarios/activos — ObtenerActivos
  - GET /api/v1/operarios/inactivos — ObtenerInactivos
  - GET /api/v1/operarios/ocupados — ObtenerOcupados
  - POST /api/v1/operarios — Crear (CrearOperarioRequest)

- Turnos
  - GET /api/v1/turnos/activos — ObtenerActivos
  - POST /api/v1/turnos — Crear (CrearTurnoRequest)
  - PATCH /api/v1/turnos/{id}/finalizar — Finalizar
  - PATCH /api/v1/turnos/{id}/cancelar — Cancelar

## Ficheros/clases clave
- UI/Controllers
  - ApiAutoLavado/UI/Controllers/TurnosController.cs — endpoints de turnos.
  - ApiAutoLavado/UI/Controllers/OperariosController.cs — endpoints de operarios.

- Aplicación/Servicios
  - ApiAutoLavado/Aplicacion/Services/TurnoService.cs — lógica de creación de turno, validaciones, generación de número de turno y hash de consulta, ocupación/liberación de operarios y bahías.
  - ApiAutoLavado/Aplicacion/Services/OperarioService.cs — creación y consultas de operarios.

- DTOs / Mapeos
  - ApiAutoLavado/Aplicacion/Dtos/CrearTurnoRequest.cs
  - ApiAutoLavado/Aplicacion/Dtos/TurnoResponse.cs
  - ApiAutoLavado/Aplicacion/Dtos/OperarioResponse.cs
  - ApiAutoLavado/Aplicacion/Dtos/MapeoExtensiones.cs (mapeos de modelos a responses)

- Persistencia
  - ApiAutoLavado/Persistencia/Repositiorios (implementaciones de IBahiaRepository, IOperarioRepository, ITurnoRepository, IServicioRepository)
  - ApiAutoLavado/Persistencia/ConstructorConexion.cs — normaliza la cadena de conexión MySQL.

## Notas técnicas importantes
- Generación de hash de consulta: `TurnoService.GenerarHash(Turno)` usa SHA256 sobre una concatenación de campos y genera `HashConsulta` en hex lower-case.
- Concurrencia para número de turno: se usa un lock (_consecutivoLock) y un DateOnly para reiniciar contador diario.
- Transacciones lógicas: TurnoService intenta ocupar operario y bahía; libera en caso de excepciones para evitar estados inconsistentes.

## Limitaciones / tareas recomendadas
1. Implementar autenticación y autorización (por ejemplo JWT o ASP.NET Identity) para cumplir RF-003. `UseAuthorization()` está presente pero no hay `AddAuthentication(...)` ni validadores.
2. Añadir endpoints para editar y desactivar operarios y protegerlos con roles (Administradores). Añadir [Authorize(Roles = "Administrador")] donde aplique.
3. Añadir pruebas automáticas (unit/integration) para TurnoService y OperarioService.
4. Revisar políticas CORS y seguridad de producción (no usar AllowAnyOrigin en producción).

## Contacto / Próximos pasos
Si desea, puedo:
- Generar un plan e implementar autenticación JWT y protección de endpoints.
- Añadir endpoints para editar/desactivar operarios y pruebas básicas.
