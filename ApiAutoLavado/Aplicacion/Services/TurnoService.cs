using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ApiAutoLavado.Aplicacion.Catalogo;
using ApiAutoLavado.Aplicacion.Dtos;
using ApiAutoLavado.Aplicacion.Repositorios;
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
        private readonly IFabricaTransacciones _transacciones;

        private readonly object _consecutivoLock = new();
        private int _consecutivo;
        private DateOnly _fechaConsecutivo = DateOnly.FromDateTime(DateTime.UtcNow);

        public TurnoService(
            IVehiculoRepository vehiculos,
            IOperarioRepository operarios,
            IServicioRepository servicios,
            ITurnoRepository turnos,
            ITurnoRealtimeNotifier realtimeNotifier,
            IFabricaTransacciones transacciones)
        {
            _vehiculos = vehiculos;
            _operarios = operarios;
            _servicios = servicios;
            _turnos = turnos;
            _realtimeNotifier = realtimeNotifier;
            _transacciones = transacciones;
        }

        public IReadOnlyCollection<TurnoResponse> ObtenerActivos()
        {
            return _turnos.ObtenerTodos()
                .Where(t => !EsFinalizado(t.EstadoActual))
                .OrderBy(t => t.FechaIngreso)
                .Select(t => t.ToResponse())
                .ToList();
        }

        public TableroTurnosResponse ObtenerTablero()
        {
            var activos = _turnos.ObtenerTodos()
                .Where(t => !EsFinalizado(t.EstadoActual))
                .OrderBy(t => t.FechaIngreso)
                .ToList();

            var enAtencion = new List<TurnoDetalleResponse>();
            var enCola = new List<TurnoDetalleResponse>();

            foreach (var turno in activos)
            {
                var detalle = ConstruirDetalle(turno);
                if (turno.IdOperario.HasValue)
                {
                    enAtencion.Add(detalle);
                }
                else
                {
                    enCola.Add(detalle);
                }
            }

            return new TableroTurnosResponse
            {
                EnAtencion = enAtencion,
                EnCola = enCola
            };
        }

        public TurnoCreadoResponse Crear(CrearTurnoRequest request)
        {
            if (request.IdServicio <= 0)
            {
                throw new ReglaNegocioException("El servicio es obligatorio.");
            }

            var servicio = _servicios.ObtenerPorId(request.IdServicio)
                ?? throw new NoEncontradoException($"No existe un servicio con id {request.IdServicio}.");

            var ahora = DateTime.UtcNow;
            Turno turno;
            long idTurno;

            // RNF-04: la verificación/ocupación de operario y la creación del turno son atómicas.
            using var transaccion = _transacciones.Iniciar();
            try
            {
                var placaNormalizada = request.Placa.Trim().ToUpperInvariant();
                var vehiculo = _vehiculos.ObtenerPorPlaca(placaNormalizada, transaccion);

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
                        FechaPrimerRegistro = ahora
                    };
                    _vehiculos.Crear(vehiculo, transaccion);
                }

                // RF-02 / RN-02: sorteo equitativo entre los operarios libres.
                // La lectura se hace fuera de la transacción para no bloquear el padrón completo;
                // la exclusión mutua real ocurre en IntentarOcupar (UPDATE condicional).
                int? idOperarioAsignado = null;
                var operariosLibres = _operarios.ObtenerTodos()
                    .Where(o => o.EstaDisponible)
                    .OrderBy(_ => Guid.NewGuid())
                    .ToList();

                foreach (var operario in operariosLibres)
                {
                    if (_operarios.IntentarOcupar(operario.Id, transaccion))
                    {
                        idOperarioAsignado = operario.Id;
                        break;
                    }
                }

                turno = new Turno
                {
                    NumeroTurno = GenerarNumeroTurno(ahora),
                    Placa = vehiculo.Placa,
                    IdServicio = servicio.Id,
                    IdOperario = idOperarioAsignado,
                    // RN-04: sin operario libre el turno espera en cola; con operario queda "Por Iniciar".
                    EstadoActual = CatalogoFases.FaseInicial,
                    FechaIngreso = ahora,
                    HashConsulta = string.Empty
                };

                turno.HashConsulta = GenerarHash(turno);
                idTurno = _turnos.Agregar(turno, transaccion);
                turno.Id = idTurno;

                transaccion.Confirmar();
            }
            catch
            {
                transaccion.Revertir();
                throw;
            }

            NotificarCambio(turno);

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
            var turno = _turnos.ObtenerPorId(id)
                ?? throw new NoEncontradoException($"No existe un turno con id {id}.");

            var servicio = _servicios.ObtenerPorId(turno.IdServicio);
            var secuencia = CatalogoFases.ObtenerSecuencia(servicio?.Fases);
            var fase = CatalogoFases.NormalizarFase(nuevaFase, secuencia);

            if (string.IsNullOrWhiteSpace(fase))
            {
                throw new ReglaNegocioException("La nueva fase del turno es obligatoria.");
            }

            // RN-05: solo se aceptan fases definidas en el catálogo del servicio contratado.
            if (!secuencia.Any(c => string.Equals(c, fase, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ReglaNegocioException(
                    $"La fase '{nuevaFase}' no existe para el servicio '{servicio?.Nombre ?? turno.IdServicio.ToString()}'.");
            }

            return CambiarEstado(id, fase);
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

            if (EsFinalizado(estadoAnterior))
            {
                throw new ReglaNegocioException(
                    $"El turno {turno.NumeroTurno} ya está {estadoAnterior}.");
            }

            using var transaccion = _transacciones.Iniciar();
            try
            {
                if (!_turnos.IntentarCambiarEstado(id, estadoAnterior, nuevo, transaccion))
                {
                    throw new ReglaNegocioException($"No se pudo actualizar el turno {turno.NumeroTurno}.");
                }

                turno.EstadoActual = nuevo;

                // RN-04 / RF-04: al cerrar la última fase se libera al operario o se le encadena el primer turno en cola.
                if (CatalogoFases.EsTerminal(nuevo) && turno.IdOperario.HasValue)
                {
                    // Asignación encadenada (RN-04). La lectura se hace fuera de la transacción;
                    // AsignarOperario solo afecta filas aún en EN_COLA, por lo que es seguro.
                    var siguienteEnCola = _turnos.ObtenerTodos()
                        .Where(t => t.IdOperario == null
                                    && string.Equals(t.EstadoActual, CatalogoFases.FaseInicial, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(t => t.FechaIngreso)
                        .FirstOrDefault();

                    if (siguienteEnCola != null)
                    {
                        _turnos.AsignarOperario(
                            siguienteEnCola.Id,
                            turno.IdOperario.Value,
                            CatalogoFases.FaseInicial,
                            transaccion);
                    }
                    else
                    {
                        _operarios.Liberar(turno.IdOperario.Value, transaccion);
                    }
                }

                transaccion.Confirmar();
            }
            catch
            {
                transaccion.Revertir();
                throw;
            }

            NotificarCambio(turno);

            return turno.ToResponse();
        }

        private void NotificarCambio(Turno turno)
        {
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
        }

        private TurnoDetalleResponse ConstruirDetalle(Turno turno)
        {
            var servicio = _servicios.ObtenerPorId(turno.IdServicio);
            var operario = turno.IdOperario.HasValue ? _operarios.ObtenerPorId(turno.IdOperario.Value) : null;
            var secuencia = CatalogoFases.ObtenerSecuencia(servicio?.Fases);
            var estado = CatalogoFases.NormalizarFase(turno.EstadoActual, secuencia);
            var indice = IndiceDe(secuencia, estado);

            return new TurnoDetalleResponse
            {
                Id = turno.Id,
                NumeroTurno = turno.NumeroTurno,
                Placa = turno.Placa,
                IdServicio = turno.IdServicio,
                NombreServicio = servicio?.Nombre ?? "LAVADO_GENERAL",
                IdOperario = turno.IdOperario,
                NombreOperario = operario != null ? $"{operario.Nombres} {operario.Apellidos}" : null,
                EstadoActual = estado,
                FaseTitulo = CatalogoFases.Titulo(estado),
                ProgresoPorcentaje = CalcularPorcentaje(secuencia, estado, indice),
                FechaIngreso = turno.FechaIngreso,
                HashConsulta = turno.HashConsulta
            };
        }

        private TrazabilidadTurnoResponse ConstruirTrazabilidad(Turno turno)
        {
            var vehiculo = _vehiculos.ObtenerPorPlaca(turno.Placa);
            var servicio = _servicios.ObtenerPorId(turno.IdServicio);
            var operario = turno.IdOperario.HasValue ? _operarios.ObtenerPorId(turno.IdOperario.Value) : null;

            var secuencia = CatalogoFases.ObtenerSecuencia(servicio?.Fases);
            var estado = CatalogoFases.NormalizarFase(turno.EstadoActual, secuencia);
            var indice = IndiceDe(secuencia, estado);

            // Un turno finalizado completó todas las fases aunque "FINALIZADO" no sea una clave del catálogo.
            if (estado == "FINALIZADO" && secuencia.Count > 0)
            {
                indice = secuencia.Count - 1;
            }

            var listo = estado is "LISTO" or "FINALIZADO";
            var porcentaje = CalcularPorcentaje(secuencia, estado, indice);

            string mensaje = estado switch
            {
                "EN_COLA" when turno.IdOperario is null => "Tu vehículo está en cola para ingresar al área de lavado.",
                "EN_COLA" => "Tu vehículo está por iniciar el proceso de lavado.",
                "LISTO" or "FINALIZADO" => "Tu vehículo ya está listo. Puedes pasar a recogerlo.",
                "CANCELADO" => "El turno de este vehículo ha sido cancelado.",
                _ => $"Tu vehículo se encuentra en fase: {CatalogoFases.Titulo(estado)}."
            };

            var fases = secuencia
                .Select((clave, i) => new FaseHitoDto
                {
                    Clave = clave,
                    Titulo = CatalogoFases.Titulo(clave),
                    Descripcion = CatalogoFases.Descripcion(clave),
                    Completada = estado != "CANCELADO" && i <= indice,
                    EnCurso = estado != "CANCELADO" && i == indice
                })
                .ToList();

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

                // Recupera el consecutivo desde la base de datos para sobrevivir reinicios de la API.
                var maximoBd = _turnos.ObtenerMaximoSecuenciaDelDia(hoy);
                if (maximoBd > _consecutivo)
                {
                    _consecutivo = maximoBd;
                }

                return $"T-{++_consecutivo:D3}";
            }
        }

        private static int IndiceDe(IReadOnlyList<string> secuencia, string estado)
        {
            for (var i = 0; i < secuencia.Count; i++)
            {
                if (string.Equals(secuencia[i], estado, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int CalcularPorcentaje(IReadOnlyList<string> secuencia, string estado, int indice)
        {
            if (estado == "CANCELADO")
            {
                return 0;
            }

            if (estado == "FINALIZADO")
            {
                return 100;
            }

            if (indice < 0 || secuencia.Count == 0)
            {
                return 15;
            }

            return (int)Math.Round((indice + 1) * 100.0 / secuencia.Count);
        }

        private static bool EsFinalizado(string? estado)
            => string.Equals(estado, "FINALIZADO", StringComparison.OrdinalIgnoreCase)
            || string.Equals(estado, "CANCELADO", StringComparison.OrdinalIgnoreCase);

        private static string GenerarHash(Turno turno)
        {
            var contenido = $"{turno.Id}|{turno.Placa}|{turno.FechaIngreso:O}";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(contenido));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
