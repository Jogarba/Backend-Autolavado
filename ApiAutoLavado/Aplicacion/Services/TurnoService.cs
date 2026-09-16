using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ApiAutoLavado.Aplicacion.Dtos;
using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Exceptions;
using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Services
{
    public class TurnoService : ITurnoService
    {
        private const string BaseUrlQr = "https://autolavadoexpress.com/track/";

        private readonly IVehiculoRepository _vehiculos;
        private readonly IOperarioRepository _operarios;
        private readonly IServicioRepository _servicios;
        private readonly ITurnoRepository _turnos;
        private readonly ITurnoRealtimeNotifier _realtimeNotifier;

        private readonly object _consecutivoLock = new();
        private int _consecutivo;
        private DateOnly _fechaConsecutivo = DateOnly.FromDateTime(DateTime.UtcNow);

        public TurnoService(
            IVehiculoRepository vehiculos,
            IOperarioRepository operarios,
            IServicioRepository servicios,
            ITurnoRepository turnos,
            ITurnoRealtimeNotifier realtimeNotifier)
        {
            _vehiculos = vehiculos;
            _operarios = operarios;
            _servicios = servicios;
            _turnos = turnos;
            _realtimeNotifier = realtimeNotifier;
        }

        public IReadOnlyCollection<TurnoResponse> ObtenerActivos()
        {
            return _turnos.ObtenerTodos()
                .Where(t => t.EstadoActual != "FINALIZADO" && t.EstadoActual != "CANCELADO")
                .OrderBy(t => t.FechaIngreso)
                .Select(t => t.ToResponse())
                .ToList();
        }

        public TurnoCreadoResponse Crear(CrearTurnoRequest request)
        {
            if (request.IdServicio <= 0)
            {
                throw new ReglaNegocioException("El servicio es obligatorio.");
            }

            if (_servicios.ObtenerPorId(request.IdServicio) is null)
            {
                throw new NoEncontradoException($"No existe un servicio con id {request.IdServicio}.");
            }

            var placaNormalizada = request.Placa.Trim().ToUpperInvariant();
            var vehiculo = _vehiculos.ObtenerPorPlaca(placaNormalizada);

            if (vehiculo == null)
            {
                if (request.TipoVehiculo is null || string.IsNullOrWhiteSpace(request.TelefonoCliente))
                {
                    throw new ReglaNegocioException("Para un vehículo nuevo, el TipoVehiculo y TelefonoCliente son obligatorios.");
                }

                vehiculo = new Vehiculo
                {
                    Placa = placaNormalizada,
                    TipoVehiculo = request.TipoVehiculo.Value,
                    TelefonoCliente = request.TelefonoCliente.Trim(),
                    FechaPrimerRegistro = DateTime.UtcNow
                };
                _vehiculos.Crear(vehiculo);
            }

            // RF-02: Algoritmo de Asignación Automática
            var operariosLibres = _operarios.ObtenerTodos().Where(o => o.EstaDisponible).ToList();
            int? idOperarioAsignado = null;
            string estadoAsignado = "EN_COLA";

            if (operariosLibres.Count > 0)
            {
                var random = new Random();
                var operarioSeleccionado = operariosLibres[random.Next(operariosLibres.Count)];
                
                if (_operarios.IntentarOcupar(operarioSeleccionado.Id))
                {
                    idOperarioAsignado = operarioSeleccionado.Id;
                    estadoAsignado = "EN_PROGRESO";
                }
            }

            var ahora = DateTime.UtcNow;
            var turno = new Turno
            {
                NumeroTurno = GenerarNumeroTurno(ahora),
                Placa = vehiculo.Placa,
                IdServicio = request.IdServicio,
                IdOperario = idOperarioAsignado,
                EstadoActual = estadoAsignado,
                FechaIngreso = ahora,
                HashConsulta = string.Empty
            };

            turno.HashConsulta = GenerarHash(turno);
            var idTurno = _turnos.Agregar(turno);
            turno.Id = idTurno;

            // Notificación reactiva
            _ = Task.Run(async () =>
            {
                try
                {
                    var trazabilidad = ConstruirTrazabilidad(turno);
                    await _realtimeNotifier.NotificarCambioEstadoAsync(trazabilidad);
                    await _realtimeNotifier.NotificarTurnosActualizadosAsync();
                }
                catch { /* Logging silencioso para no bloquear el hilo HTTP */ }
            });

            return new TurnoCreadoResponse
            {
                IdTurno = idTurno,
                NumeroTurno = turno.NumeroTurno,
                Estado = turno.EstadoActual,
                HashConsulta = turno.HashConsulta,
                QrUrl = $"{BaseUrlQr}{turno.HashConsulta}",
                FechaIngreso = turno.FechaIngreso
            };
        }

        public TurnoResponse ActualizarFase(long id, string nuevaFase)
        {
            var faseNormalizada = nuevaFase?.Trim().ToUpperInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(faseNormalizada))
            {
                throw new ReglaNegocioException("La nueva fase del turno es obligatoria.");
            }

            return CambiarEstado(id, faseNormalizada);
        }

        public TurnoResponse Finalizar(long id) => CambiarEstado(id, "FINALIZADO");

        public TurnoResponse Cancelar(long id) => CambiarEstado(id, "CANCELADO");

        public TrazabilidadTurnoResponse ObtenerTrazabilidad(string identificador)
        {
            if (string.IsNullOrWhiteSpace(identificador))
            {
                throw new ReglaNegocioException("El identificador (placa o código de seguimiento) es obligatorio.");
            }

            var idLimpio = identificador.Trim().ToUpperInvariant();
            var todos = _turnos.ObtenerTodos();

            // Buscar por hash_consulta exacto, o por placa más reciente
            var turno = todos.FirstOrDefault(t => string.Equals(t.HashConsulta, idLimpio, StringComparison.OrdinalIgnoreCase))
                     ?? todos.Where(t => string.Equals(t.Placa, idLimpio, StringComparison.OrdinalIgnoreCase))
                             .OrderByDescending(t => t.FechaIngreso)
                             .FirstOrDefault();

            if (turno == null)
            {
                throw new NoEncontradoException($"No se encontró ningún registro o turno de lavado activo para '{identificador}'.");
            }

            return ConstruirTrazabilidad(turno);
        }

        private TurnoResponse CambiarEstado(long id, string nuevo)
        {
            var turno = _turnos.ObtenerPorId(id)
                ?? throw new NoEncontradoException($"No existe un turno con id {id}.");

            var estadoAnterior = turno.EstadoActual;

            if (estadoAnterior == "FINALIZADO" || estadoAnterior == "CANCELADO")
            {
                throw new ReglaNegocioException(
                    $"El turno {turno.NumeroTurno} ya está {estadoAnterior}.");
            }

            if (!_turnos.IntentarCambiarEstado(id, estadoAnterior, nuevo))
            {
                throw new ReglaNegocioException($"No se pudo actualizar el turno {turno.NumeroTurno}.");
            }

            turno.EstadoActual = nuevo;

            if (nuevo == "FINALIZADO" || nuevo == "CANCELADO")
            {
                if (turno.IdOperario.HasValue)
                {
                    // Asignación encadenada
                    var siguienteEnCola = _turnos.ObtenerTodos()
                        .Where(t => t.EstadoActual == "EN_COLA")
                        .OrderBy(t => t.FechaIngreso)
                        .FirstOrDefault();

                    if (siguienteEnCola != null)
                    {
                        // Le asignamos el turno en cola
                        _turnos.AsignarOperario(siguienteEnCola.Id, turno.IdOperario.Value, "EN_PROGRESO");
                    }
                    else
                    {
                        _operarios.Liberar(turno.IdOperario.Value);
                    }
                }
            }

            // Notificación instantánea en tiempo real vía SignalR / WebSockets
            _ = Task.Run(async () =>
            {
                try
                {
                    var trazabilidad = ConstruirTrazabilidad(turno);
                    await _realtimeNotifier.NotificarCambioEstadoAsync(trazabilidad);
                    await _realtimeNotifier.NotificarTurnosActualizadosAsync();
                }
                catch { /* Logging silencioso */ }
            });

            return turno.ToResponse();
        }

        private TrazabilidadTurnoResponse ConstruirTrazabilidad(Turno turno)
        {
            var vehiculo = _vehiculos.ObtenerPorPlaca(turno.Placa);
            var servicio = _servicios.ObtenerPorId(turno.IdServicio);
            var operario = turno.IdOperario.HasValue ? _operarios.ObtenerPorId(turno.IdOperario.Value) : null;

            var estado = (turno.EstadoActual ?? "EN_COLA").ToUpperInvariant();
            int porcentaje = 15;
            string mensaje = "Tu vehículo está en espera de turno en patio.";
            bool listo = false;

            switch (estado)
            {
                case "EN_COLA":
                case "POR_INICIAR":
                    porcentaje = 15;
                    mensaje = "Tu vehículo está en cola para ingresar al área de lavado.";
                    break;
                case "EN_PROGRESO":
                case "ENJABONADO":
                    porcentaje = 40;
                    mensaje = "Tu vehículo se encuentra actualmente en proceso de enjabonado y limpieza.";
                    break;
                case "ENJUAGADO":
                    porcentaje = 65;
                    mensaje = "Tu vehículo está en fase de enjuagado y retiro de impurezas.";
                    break;
                case "SECADO":
                case "POR_TERMINAR":
                    porcentaje = 85;
                    mensaje = "Tu vehículo está en fase de secado, aspirado y detalles finales.";
                    break;
                case "LISTO":
                case "LISTO_PARA_RECOGER":
                    porcentaje = 100;
                    mensaje = "Tu vehículo ya está listo. Puedes pasar a recogerlo.";
                    listo = true;
                    break;
                case "FINALIZADO":
                    porcentaje = 100;
                    mensaje = "Servicio completado. ¡Gracias por confiar en AutoLavado Express Sincelejo!";
                    listo = true;
                    break;
                case "CANCELADO":
                    porcentaje = 0;
                    mensaje = "El turno de este vehículo ha sido cancelado.";
                    break;
                default:
                    porcentaje = 50;
                    mensaje = $"El vehículo se encuentra en estado: {estado}.";
                    break;
            }

            var fases = new List<FaseHitoDto>
            {
                new() {
                    Clave = "EN_COLA",
                    Titulo = "Por Iniciar",
                    Descripcion = "Vehículo ingresado y registrado en cola.",
                    Completada = porcentaje >= 15,
                    EnCurso = estado is "EN_COLA" or "POR_INICIAR"
                },
                new() {
                    Clave = "ENJABONADO",
                    Titulo = "Enjabonado",
                    Descripcion = "Aplicación de shampoo especializado y espumado activo.",
                    Completada = porcentaje >= 40,
                    EnCurso = estado is "EN_PROGRESO" or "ENJABONADO"
                },
                new() {
                    Clave = "ENJUAGADO",
                    Titulo = "Enjuagado",
                    Descripcion = "Retiro de jabón con agua a alta presión.",
                    Completada = porcentaje >= 65,
                    EnCurso = estado is "ENJUAGADO"
                },
                new() {
                    Clave = "SECADO",
                    Titulo = "Por Terminar / Secado",
                    Descripcion = "Secado en microfibra, llantas y aspirado.",
                    Completada = porcentaje >= 85,
                    EnCurso = estado is "SECADO" or "POR_TERMINAR"
                },
                new() {
                    Clave = "LISTO",
                    Titulo = "Listo para Recoger",
                    Descripcion = "Servicio listo para inspección y entrega.",
                    Completada = porcentaje == 100,
                    EnCurso = estado is "LISTO" or "LISTO_PARA_RECOGER" or "FINALIZADO"
                }
            };

            return new TrazabilidadTurnoResponse
            {
                IdTurno = turno.Id,
                NumeroTurno = turno.NumeroTurno,
                Placa = turno.Placa,
                TipoVehiculo = vehiculo?.TipoVehiculo.ToString().ToUpperInvariant() ?? "AUTO",
                TelefonoCliente = vehiculo?.TelefonoCliente ?? string.Empty,
                NombreServicio = servicio?.Nombre ?? "LAVADO_GENERAL",
                TarifaBase = servicio?.PrecioBase ?? 0m,
                TiempoEstimadoMin = servicio?.TiempoEstimadoMin ?? 30,
                IdOperario = turno.IdOperario,
                NombreOperario = operario != null ? $"{operario.Nombres} {operario.Apellidos}" : "Por asignar",
                FaseActual = estado,
                ProgresoPorcentaje = porcentaje,
                MensajeEstado = mensaje,
                EstaListoParaRecoger = listo,
                HashConsulta = turno.HashConsulta,
                FechaIngreso = turno.FechaIngreso,
                Fases = fases
            };
        }

        private string GenerarNumeroTurno(DateTime fecha)
        {
            lock (_consecutivoLock)
            {
                var hoy = DateOnly.FromDateTime(fecha);
                if (hoy != _fechaConsecutivo)
                {
                    _fechaConsecutivo = hoy;
                    _consecutivo = 0;
                }

                return $"T-{++_consecutivo:D3}";
            }
        }

        private static string GenerarHash(Turno turno)
        {
            var contenido = $"{turno.Id}|{turno.Placa}|{turno.FechaIngreso:O}";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(contenido));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
