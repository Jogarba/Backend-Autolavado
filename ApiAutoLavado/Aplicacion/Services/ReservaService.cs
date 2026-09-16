using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using ApiAutoLavado.Aplicacion.Dtos;
using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Exceptions;
using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Services
{
    public class ReservaService : IReservaService
    {
        private static readonly TimeOnly HoraInicio = new(8, 0);
        private static readonly TimeOnly HoraFin = new(17, 30);
        private const int IntervaloMinutos = 30;

        private readonly IReservaRepository _reservas;
        private readonly IVehiculoRepository _vehiculos;
        private readonly IServicioRepository _servicios;
        private readonly IOperarioRepository _operarios;
        private readonly ITurnoRepository _turnos;
        private readonly ITurnoService _turnoService;
        private readonly IFabricaTransacciones _transacciones;

        public ReservaService(
            IReservaRepository reservas,
            IVehiculoRepository vehiculos,
            IServicioRepository servicios,
            IOperarioRepository operarios,
            ITurnoRepository turnos,
            ITurnoService turnoService,
            IFabricaTransacciones transacciones)
        {
            _reservas = reservas;
            _vehiculos = vehiculos;
            _servicios = servicios;
            _operarios = operarios;
            _turnos = turnos;
            _turnoService = turnoService;
            _transacciones = transacciones;
        }

        public DisponibilidadFechaResponse ConsultarDisponibilidad(DateOnly fecha)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var ahora = TimeOnly.FromDateTime(DateTime.Now);

            var operariosActivos = _operarios.ObtenerTodos().Count(o => o.Activo);
            if (operariosActivos == 0)
            {
                operariosActivos = 1; // Capacidad mínima de contingencia
            }

            var franjas = new List<FranjaHorariaDto>();
            var cursor = HoraInicio;

            while (cursor <= HoraFin)
            {
                // RF-08: se cruzan reservas y turnos ya agendados para el mismo intervalo.
                var ocupados = _reservas.ContarPorFechaYHora(fecha, cursor)
                    + _turnos.ContarActivosPorFechaYHora(fecha, cursor);
                var disponibles = Math.Max(0, operariosActivos - ocupados);

                bool esPasado = (fecha < hoy) || (fecha == hoy && cursor <= ahora);
                bool disponible = disponibles > 0 && !esPasado;

                franjas.Add(new FranjaHorariaDto
                {
                    Hora = cursor.ToString("HH:mm"),
                    CapacidadTotal = operariosActivos,
                    CuposOcupados = ocupados,
                    CuposDisponibles = disponibles,
                    Disponible = disponible
                });

                cursor = cursor.AddMinutes(IntervaloMinutos);
            }

            return new DisponibilidadFechaResponse
            {
                Fecha = fecha,
                OperariosActivos = operariosActivos,
                Franjas = franjas
            };
        }

        public ReservaResponse Crear(CrearReservaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Placa))
            {
                throw new ReglaNegocioException("La placa del vehículo es obligatoria.");
            }

            if (request.IdServicio <= 0)
            {
                throw new ReglaNegocioException("El servicio es obligatorio.");
            }

            var servicio = _servicios.ObtenerPorId(request.IdServicio)
                ?? throw new NoEncontradoException($"No existe el servicio con ID {request.IdServicio}.");

            // RN-CL-02: Anticipación mínima (fechas presentes o futuras dentro del horario comercial)
            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var ahora = TimeOnly.FromDateTime(DateTime.Now);

            if (request.FechaReserva < hoy)
            {
                throw new ReglaNegocioException("No se pueden registrar reservas en fechas pasadas.");
            }

            if (request.FechaReserva == hoy && request.HoraReserva <= ahora)
            {
                throw new ReglaNegocioException("La hora de la reserva debe ser posterior a la hora actual.");
            }

            if (request.HoraReserva < HoraInicio || request.HoraReserva > HoraFin)
            {
                throw new ReglaNegocioException($"El horario de atención es entre {HoraInicio:HH:mm} y {HoraFin:HH:mm}.");
            }

            // RN-CL-01 / RNF-04: la validación de cupo y la inserción son atómicas y serializables
            // para impedir la sobreventa cuando dos clientes reservan la misma franja.
            var operariosActivos = _operarios.ObtenerTodos().Count(o => o.Activo);
            if (operariosActivos == 0) operariosActivos = 1;

            using var transaccion = _transacciones.Iniciar();
            try
            {
                var cuposOcupados = _reservas.ContarPorFechaYHora(request.FechaReserva, request.HoraReserva, transaccion)
                    + _turnos.ContarActivosPorFechaYHora(request.FechaReserva, request.HoraReserva, transaccion);

                if (cuposOcupados >= operariosActivos)
                {
                    throw new ReglaNegocioException($"No hay cupos disponibles para la fecha {request.FechaReserva:yyyy-MM-dd} a las {request.HoraReserva:HH:mm}. Por favor elija otro horario.");
                }

                // Registrar o actualizar vehículo
                var placaNormalizada = request.Placa.Trim().ToUpperInvariant();
                var vehiculo = _vehiculos.ObtenerPorPlaca(placaNormalizada, transaccion);

                if (vehiculo == null)
                {
                    vehiculo = new Vehiculo
                    {
                        Placa = placaNormalizada,
                        TipoVehiculo = request.TipoVehiculo ?? TipoVehiculo.Auto,
                        TelefonoCliente = !string.IsNullOrWhiteSpace(request.TelefonoCliente) ? request.TelefonoCliente.Trim() : "3000000000",
                        FechaPrimerRegistro = DateTime.UtcNow
                    };
                    _vehiculos.Crear(vehiculo, transaccion);
                }
                else if (!string.IsNullOrWhiteSpace(request.TelefonoCliente) || request.TipoVehiculo.HasValue)
                {
                    if (!string.IsNullOrWhiteSpace(request.TelefonoCliente))
                        vehiculo.TelefonoCliente = request.TelefonoCliente.Trim();
                    if (request.TipoVehiculo.HasValue)
                        vehiculo.TipoVehiculo = request.TipoVehiculo.Value;

                    _vehiculos.Actualizar(vehiculo, transaccion);
                }

                // Generar código único de reserva (ej: RES-4821)
                var codigoReserva = GenerarCodigoUnico(transaccion);

                var reserva = new Reserva
                {
                    CodigoReserva = codigoReserva,
                    Placa = vehiculo.Placa,
                    IdServicio = servicio.Id,
                    FechaReserva = request.FechaReserva,
                    HoraReserva = request.HoraReserva,
                    Estado = "CONFIRMADA",
                    FechaCreacion = DateTime.UtcNow,
                    NombreServicio = servicio.Nombre,
                    TarifaBase = servicio.PrecioBase,
                    TiempoEstimadoMin = servicio.TiempoEstimadoMin,
                    TipoVehiculo = vehiculo.TipoVehiculo.ToString(),
                    TelefonoCliente = vehiculo.TelefonoCliente
                };

                var idGenerado = _reservas.Crear(reserva, transaccion);
                reserva.IdReserva = idGenerado;

                transaccion.Confirmar();
                return reserva.ToResponse();
            }
            catch
            {
                transaccion.Revertir();
                throw;
            }
        }

        public ReservaResponse ObtenerPorCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                throw new ReglaNegocioException("El código de reserva es obligatorio.");
            }

            var reserva = _reservas.ObtenerPorCodigo(codigo)
                ?? throw new NoEncontradoException($"No se encontró ninguna reserva con el código {codigo.Trim().ToUpperInvariant()}.");

            return reserva.ToResponse();
        }

        public IReadOnlyCollection<ReservaResponse> ObtenerPorFecha(DateOnly fecha)
        {
            return _reservas.ObtenerPorFecha(fecha).Select(r => r.ToResponse()).ToList();
        }

        public IReadOnlyCollection<ReservaResponse> ObtenerPorPlaca(string placa)
        {
            return _reservas.ObtenerPorPlaca(placa).Select(r => r.ToResponse()).ToList();
        }

        public IReadOnlyCollection<ReservaResponse> ObtenerTodas()
        {
            return _reservas.ObtenerTodas().Select(r => r.ToResponse()).ToList();
        }

        public ReservaResponse Cancelar(long id)
        {
            var reserva = _reservas.ObtenerPorId(id)
                ?? throw new NoEncontradoException($"No se encontró la reserva con ID {id}.");

            if (reserva.Estado == "CANCELADA")
            {
                throw new ReglaNegocioException("La reserva ya se encuentra cancelada.");
            }

            if (reserva.Estado == "EN_ATENCION")
            {
                throw new ReglaNegocioException("No se puede cancelar una reserva que ya está en atención.");
            }

            _reservas.ActualizarEstado(id, "CANCELADA");
            reserva.Estado = "CANCELADA";

            return reserva.ToResponse();
        }

        public TurnoCreadoResponse ConvertirEnTurno(long idReserva)
        {
            var reserva = _reservas.ObtenerPorId(idReserva)
                ?? throw new NoEncontradoException($"No se encontró la reserva con ID {idReserva}.");

            if (reserva.Estado == "CANCELADA")
            {
                throw new ReglaNegocioException("No se puede iniciar el turno de una reserva cancelada.");
            }

            var vehiculo = _vehiculos.ObtenerPorPlaca(reserva.Placa);
            var turnoCreado = _turnoService.Crear(new CrearTurnoRequest
            {
                Placa = reserva.Placa,
                IdServicio = reserva.IdServicio,
                TipoVehiculo = vehiculo?.TipoVehiculo,
                TelefonoCliente = vehiculo?.TelefonoCliente
            });

            _reservas.ActualizarEstado(idReserva, "EN_ATENCION");

            return turnoCreado;
        }

        private string GenerarCodigoUnico(ITransaccionBd? transaccion = null)
        {
            for (int i = 0; i < 10; i++)
            {
                var numero = RandomNumberGenerator.GetInt32(1000, 9999);
                var codigo = $"RES-{numero}";
                if (_reservas.ObtenerPorCodigo(codigo, transaccion) == null)
                {
                    return codigo;
                }
            }

            return $"RES-{RandomNumberGenerator.GetInt32(10000, 99999)}";
        }
    }
}
