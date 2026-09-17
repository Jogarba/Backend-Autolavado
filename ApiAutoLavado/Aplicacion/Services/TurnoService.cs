using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ApiAutoLavado.Aplicacion.Catalogo;
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
        private readonly IBahiaRepository _bahias;
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
            IBahiaRepository bahias,
            ITurnoRealtimeNotifier realtimeNotifier,
            IFabricaTransacciones transacciones)
        {
            _vehiculos = vehiculos;
            _operarios = operarios;
            _servicios = servicios;
            _turnos = turnos;
            _bahias = bahias;
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
                // "En atención" solo cuando ya tiene bahía (el trabajo arrancó);
                // si aún espera bahía, aunque tenga operario, cuenta como cola.
                if (turno.IdBahia.HasValue)
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

        public IReadOnlyCollection<TurnoDisplayResponse> ObtenerDisplay()
        {
            return _turnos.ObtenerTodos()
                .Where(t => !EsFinalizado(t.EstadoActual))
                // Primero los que están en atención (con operario), luego la cola por orden de llegada.
                .OrderBy(t => t.IdOperario is null)
                .ThenBy(t => t.FechaIngreso)
                .Select(ConstruirDisplay)
                .ToList();
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

                // RF-02: si hay operario asignado y una bahía libre, el turno pasa
                // directo a patio sin quedar en cola. La exclusión mutua real ocurre
                // en IntentarOcupar (UPDATE condicional sobre bahías DISPONIBLES).
                int? idBahiaAsignada = null;
                if (idOperarioAsignado.HasValue)
                {
                    var bahiasLibres = _bahias.ObtenerTodas()
                        .Where(b => b.Estado == EstadoBahia.Disponible)
                        .OrderBy(b => b.Nombre)
                        .ToList();

                    foreach (var bahia in bahiasLibres)
                    {
                        if (_bahias.IntentarOcupar(bahia.Id, transaccion))
                        {
                            idBahiaAsignada = bahia.Id;
                            break;
                        }
                    }
                }

                turno = new Turno
                {
                    NumeroTurno = GenerarNumeroTurno(ahora),
                    Placa = vehiculo.Placa,
                    IdServicio = servicio.Id,
                    IdOperario = idOperarioAsignado,
                    IdBahia = idBahiaAsignada,
                    // RN-04: sin operario libre el turno espera en cola; si hay
                    // operario y bahía pasa directo a "En Patio" (por iniciar servicio).
                    EstadoActual = idBahiaAsignada.HasValue
                        ? CatalogoFases.FasePatio
                        : CatalogoFases.FaseInicial,
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

        public TurnoDetalleResponse ObtenerTurnoAsignado(int usuarioId)
        {
            var operario = _operarios.ObtenerPorUsuarioId(usuarioId)
                ?? throw new AccesoDenegadoException("El usuario autenticado no está vinculado a un operario.");

            // Se excluyen los turnos ya en su última fase (LISTO): el operario ya los liberó.
            var turno = _turnos.ObtenerTodos()
                .Where(t => t.IdOperario == operario.Id
                            && !EsFinalizado(t.EstadoActual)
                            && !EstaEnFaseFinal(t))
                .OrderBy(t => t.FechaIngreso)
                .FirstOrDefault()
                ?? throw new NoEncontradoException("No tienes un turno asignado en este momento.");

            return ConstruirDetalle(turno);
        }

        private bool EstaEnFaseFinal(Turno turno)
        {
            var servicio = _servicios.ObtenerPorId(turno.IdServicio);
            var secuencia = CatalogoFases.ObtenerSecuencia(servicio?.Fases);
            var estado = CatalogoFases.NormalizarFase(turno.EstadoActual, secuencia);

            return secuencia.Count > 0 && IndiceDe(secuencia, estado) == secuencia.Count - 1;
        }

        public IReadOnlyCollection<TurnoDetalleResponse> ObtenerHistorialOperario(int usuarioId)
        {
            var operario = _operarios.ObtenerPorUsuarioId(usuarioId)
                ?? throw new AccesoDenegadoException("El usuario autenticado no está vinculado a un operario.");

            return _turnos.ObtenerPorOperario(operario.Id)
                .OrderByDescending(t => t.FechaIngreso)
                .Select(ConstruirDetalle)
                .ToList();
        }

        /// <summary>
        /// Historial completo de turnos (activos y cerrados) para el panel del administrador.
        /// </summary>
        public IReadOnlyCollection<TurnoDetalleResponse> ObtenerHistorial(DateOnly? fecha = null)
        {
            var consulta = _turnos.ObtenerTodos().AsEnumerable();

            if (fecha.HasValue)
            {
                consulta = consulta.Where(t => DateOnly.FromDateTime(t.FechaIngreso) == fecha.Value);
            }

            return consulta
                .OrderByDescending(t => t.FechaIngreso)
                .Take(300)
                .Select(ConstruirDetalle)
                .ToList();
        }

        public TurnoResponse ActualizarFase(long id, string nuevaFase, int usuarioId, bool esAdministrador)
        {
            var turno = _turnos.ObtenerPorId(id)
                ?? throw new NoEncontradoException($"No existe un turno con id {id}.");

            // RF-04: el operario solo puede avanzar el turno que tiene asignado.
            if (!esAdministrador)
            {
                var operario = _operarios.ObtenerPorUsuarioId(usuarioId)
                    ?? throw new AccesoDenegadoException("El usuario autenticado no está vinculado a un operario.");

                if (turno.IdOperario != operario.Id)
                {
                    throw new AccesoDenegadoException(
                        $"El turno {turno.NumeroTurno} está asignado a otro operario.");
                }
            }

            // El operario debe elegir su bahía antes de iniciar (avanzar la primera fase).
            if (!turno.IdBahia.HasValue)
            {
                throw new ReglaNegocioException(
                    "Debe seleccionar una bahía antes de avanzar el turno.");
            }

            var servicio = _servicios.ObtenerPorId(turno.IdServicio);
            var secuencia = CatalogoFases.ObtenerSecuencia(servicio?.Fases);
            var fase = CatalogoFases.NormalizarFase(nuevaFase, secuencia);

            if (string.IsNullOrWhiteSpace(fase))
            {
                throw new ReglaNegocioException("La nueva fase del turno es obligatoria.");
            }

            // RN-05: solo se aceptan fases definidas en el catálogo del servicio contratado.
            var indiceNuevo = IndiceDe(secuencia, fase);
            if (indiceNuevo < 0)
            {
                throw new ReglaNegocioException(
                    $"La fase '{nuevaFase}' no existe para el servicio '{servicio?.Nombre ?? turno.IdServicio.ToString()}'.");
            }

            // RF-04: solo se avanza a la siguiente fase, no se salta ni se retrocede.
            var estadoActual = CatalogoFases.NormalizarFase(turno.EstadoActual, secuencia);
            var indiceActual = IndiceDe(secuencia, estadoActual);

            if (indiceActual < 0)
            {
                throw new ReglaNegocioException(
                    $"El turno está en un estado no reconocido ({turno.EstadoActual}).");
            }

            if (indiceActual >= secuencia.Count - 1)
            {
                throw new ReglaNegocioException(
                    "El turno ya completó todas las fases; use la finalización del turno.");
            }

            if (indiceNuevo != indiceActual + 1)
            {
                throw new ReglaNegocioException(
                    $"Solo se puede avanzar a la siguiente fase ({secuencia[indiceActual + 1]}). " +
                    $"Fase actual: {secuencia[indiceActual]}.");
            }

            // RF-04: al alcanzar la fase final (LISTO) el turno termina y
            // se liberan operario y bahía (el estado pasa a FINALIZADO).
            var esFaseFinal = string.Equals(fase, CatalogoFases.FaseFinal, StringComparison.OrdinalIgnoreCase);
            return CambiarEstado(id, esFaseFinal ? "FINALIZADO" : fase);
        }

        public TurnoResponse AsignarBahia(long idTurno, int idBahia, int usuarioId, bool esAdministrador)
        {
            var turno = _turnos.ObtenerPorId(idTurno)
                ?? throw new NoEncontradoException($"No existe un turno con id {idTurno}.");

            if (EsFinalizado(turno.EstadoActual))
            {
                throw new ReglaNegocioException($"El turno {turno.NumeroTurno} ya está cerrado.");
            }

            // El operario solo puede elegir bahía para el turno que tiene asignado.
            if (!esAdministrador)
            {
                var operario = _operarios.ObtenerPorUsuarioId(usuarioId)
                    ?? throw new AccesoDenegadoException("El usuario autenticado no está vinculado a un operario.");

                if (turno.IdOperario != operario.Id)
                {
                    throw new AccesoDenegadoException(
                        $"El turno {turno.NumeroTurno} está asignado a otro operario.");
                }
            }

            var bahia = _bahias.ObtenerPorId(idBahia)
                ?? throw new NoEncontradoException($"No existe una bahía con id {idBahia}.");

            if (turno.IdBahia == idBahia && bahia.Estado == EstadoBahia.Ocupada)
            {
                return turno.ToResponse();
            }

            if (bahia.Estado == EstadoBahia.Mantenimiento)
            {
                throw new ReglaNegocioException($"La bahía {bahia.Nombre} está en mantenimiento.");
            }

            if (bahia.Estado == EstadoBahia.Ocupada)
            {
                throw new ReglaNegocioException($"La bahía {bahia.Nombre} ya está ocupada por otro turno.");
            }

            using var transaccion = _transacciones.Iniciar();
            try
            {
                // Exclusión mutua real: solo ocupa si sigue DISPONIBLE.
                if (!_bahias.IntentarOcupar(idBahia, transaccion))
                {
                    throw new ReglaNegocioException($"La bahía {bahia.Nombre} acaba de ser ocupada por otro operario.");
                }

                if (turno.IdBahia.HasValue && turno.IdBahia.Value != idBahia)
                {
                    var idBahiaAnterior = turno.IdBahia.Value;
                    _bahias.Liberar(idBahiaAnterior, transaccion);
                    DisponerBahiaEnEspera(idBahiaAnterior, transaccion);
                }

                _turnos.AsignarBahia(idTurno, idBahia, transaccion);
                turno.IdBahia = idBahia;

                // Si el turno aún estaba en cola, al obtener bahía pasa a "En Patio".
                if (string.Equals(turno.EstadoActual, CatalogoFases.FaseInicial, StringComparison.OrdinalIgnoreCase)
                    && _turnos.IntentarCambiarEstado(idTurno, CatalogoFases.FaseInicial, CatalogoFases.FasePatio, transaccion))
                {
                    turno.EstadoActual = CatalogoFases.FasePatio;
                }

                transaccion.Confirmar();
            }
            catch
            {
                transaccion.Revertir();
                throw;
            }

            NotificarCambio(turno);
            NotificarBahiaAsignada(turno);
            return turno.ToResponse();
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

            Turno? turnoConBahia = null;
            using var transaccion = _transacciones.Iniciar();
            try
            {
                if (!_turnos.IntentarCambiarEstado(id, estadoAnterior, nuevo, transaccion))
                {
                    throw new ReglaNegocioException($"No se pudo actualizar el turno {turno.NumeroTurno}.");
                }

                turno.EstadoActual = nuevo;

                Turno? turnoEncadenado = null;

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

                        siguienteEnCola.IdOperario = turno.IdOperario.Value;
                        turnoEncadenado = siguienteEnCola;
                    }
                    else
                    {
                        _operarios.Liberar(turno.IdOperario.Value, transaccion);
                    }
                }

                // Al cerrar el turno se libera la bahía y, si hay un turno esperando
                // (o al operario encadenado), se le asigna para que pueda continuar.
                if (CatalogoFases.EsTerminal(nuevo) && turno.IdBahia.HasValue)
                {
                    var idBahiaLibre = turno.IdBahia.Value;
                    _bahias.Liberar(idBahiaLibre, transaccion);
                    turnoConBahia = DisponerBahiaEnEspera(idBahiaLibre, transaccion, turnoEncadenado);
                }

                transaccion.Confirmar();
            }
            catch
            {
                transaccion.Revertir();
                throw;
            }

            NotificarCambio(turno);

            if (turnoConBahia is not null)
            {
                NotificarCambio(turnoConBahia);
                NotificarBahiaAsignada(turnoConBahia);
            }

            return turno.ToResponse();
        }

        /// <summary>
        /// Asigna una bahía recién liberada al turno más antiguo que tenga operario
        /// asignado y aún no tenga bahía (el que quedó en cola esperando).
        /// </summary>
        private Turno? DisponerBahiaEnEspera(int idBahiaLibre, ITransaccionBd transaccion, Turno? candidato = null)
        {
            // FIFO: primero el turno que lleva más tiempo esperando bahía; si no hay,
            // se usa el turno recién encadenado al operario que acaba de liberarse.
            var esperando = _turnos.ObtenerTodos()
                .Where(t => t.IdOperario.HasValue && !t.IdBahia.HasValue && !EsFinalizado(t.EstadoActual))
                .OrderBy(t => t.FechaIngreso)
                .FirstOrDefault()
                ?? candidato;

            if (esperando is null)
            {
                return null;
            }

            if (!_bahias.IntentarOcupar(idBahiaLibre, transaccion))
            {
                return null;
            }

            _turnos.AsignarBahia(esperando.Id, idBahiaLibre, transaccion);
            esperando.IdBahia = idBahiaLibre;

            // El turno encolado que recibe bahía pasa a "En Patio".
            if (string.Equals(esperando.EstadoActual, CatalogoFases.FaseInicial, StringComparison.OrdinalIgnoreCase)
                && _turnos.IntentarCambiarEstado(esperando.Id, CatalogoFases.FaseInicial, CatalogoFases.FasePatio, transaccion))
            {
                esperando.EstadoActual = CatalogoFases.FasePatio;
            }

            return esperando;
        }

        private void NotificarBahiaAsignada(Turno turno)
        {
            if (!turno.IdBahia.HasValue)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    var bahia = _bahias.ObtenerPorId(turno.IdBahia.Value);
                    var evento = new BahiaAsignadaResponse
                    {
                        IdTurno = turno.Id,
                        NumeroTurno = turno.NumeroTurno,
                        Placa = turno.Placa,
                        IdOperario = turno.IdOperario,
                        IdBahia = turno.IdBahia.Value,
                        NombreBahia = bahia?.Nombre ?? string.Empty
                    };

                    await _realtimeNotifier.NotificarBahiaAsignadaAsync(evento);
                }
                catch { /* Logging silencioso */ }
            });
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

        private TurnoDisplayResponse ConstruirDisplay(Turno turno)
        {
            var servicio = _servicios.ObtenerPorId(turno.IdServicio);
            var bahia = turno.IdBahia.HasValue ? _bahias.ObtenerPorId(turno.IdBahia.Value) : null;
            var secuencia = CatalogoFases.ObtenerSecuencia(servicio?.Fases);
            var estado = CatalogoFases.NormalizarFase(turno.EstadoActual, secuencia);
            var indice = IndiceDe(secuencia, estado);

            return new TurnoDisplayResponse
            {
                IdTurno = turno.Id,
                NumeroTurno = turno.NumeroTurno,
                Placa = turno.Placa,
                EnAtencion = turno.IdBahia.HasValue,
                NombreBahia = bahia?.Nombre,
                EstadoActual = estado,
                FaseTitulo = CatalogoFases.Titulo(estado),
                ProgresoPorcentaje = CalcularPorcentaje(secuencia, estado, indice)
            };
        }

        private TurnoDetalleResponse ConstruirDetalle(Turno turno)
        {
            var servicio = _servicios.ObtenerPorId(turno.IdServicio);
            var operario = turno.IdOperario.HasValue ? _operarios.ObtenerPorId(turno.IdOperario.Value) : null;
            var bahia = turno.IdBahia.HasValue ? _bahias.ObtenerPorId(turno.IdBahia.Value) : null;
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
                IdBahia = turno.IdBahia,
                NombreBahia = bahia?.Nombre,
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
                "EN_COLA" => "Tu vehículo está en cola para ingresar al área de lavado.",
                "EN_PATIO" => "Tu vehículo ya está en el patio; el lavado está por comenzar.",
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
