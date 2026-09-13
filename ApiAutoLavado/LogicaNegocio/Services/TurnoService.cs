using System.Security.Cryptography;
using System.Text;
using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Exceptions;
using ApiAutoLavado.Domain.Models;
using ApiAutoLavado.UI.Dtos;

namespace ApiAutoLavado.LogicaNegocio.Services
{
    public class TurnoService : ITurnoService
    {
        private const string BaseUrlQr = "https://autolavadoexpress.com/track/";

        private readonly IBahiaService _bahiaService;
        private readonly IOperarioService _operarioService;
        private readonly IServicioService _servicioService;

        private readonly Dictionary<Guid, Turno> _turnos = new();
        private readonly object _lock = new();
        private int _consecutivo;
        private DateOnly _fechaConsecutivo = DateOnly.FromDateTime(DateTime.UtcNow);

        public TurnoService(IBahiaService bahiaService, IOperarioService operarioService, IServicioService servicioService)
        {
            _bahiaService = bahiaService;
            _operarioService = operarioService;
            _servicioService = servicioService;
        }

        public IReadOnlyCollection<Turno> ObtenerActivos()
        {
            lock (_lock)
            {
                return _turnos.Values
                    .Where(t => t.EstadoActual == EstadoTurno.Recepcion)
                    .OrderBy(t => t.FechaIngreso)
                    .ToList();
            }
        }

        public TurnoCreadoResponse Crear(CrearTurnoRequest request)
        {
            if (request.TipoVehiculo is null)
            {
                throw new ReglaNegocioException(
                    "El tipo de vehículo no es válido. Valores permitidos: AUTO, MOTO, CAMIONETA.");
            }

            if (request.IdServicio == Guid.Empty)
            {
                throw new ReglaNegocioException("El servicio es obligatorio.");
            }

            if (request.IdOperario == Guid.Empty)
            {
                throw new ReglaNegocioException("El operario es obligatorio.");
            }

            if (request.IdBahia == Guid.Empty)
            {
                throw new ReglaNegocioException("La bahía es obligatoria.");
            }

            var tipoVehiculo = request.TipoVehiculo.Value;

            var operario = _operarioService.ObtenerPorId(request.IdOperario);
            if (operario is null)
            {
                throw new NoEncontradoException($"No existe un operario con id {request.IdOperario}.");
            }

            if (!operario.Activo)
            {
                throw new ReglaNegocioException($"El operario {operario.Nombres} {operario.Apellidos} no está activo.");
            }

            if (_servicioService.ObtenerPorId(request.IdServicio) is null)
            {
                throw new NoEncontradoException($"No existe un servicio con id {request.IdServicio}.");
            }

            if (_bahiaService.ObtenerPorId(request.IdBahia) is null)
            {
                throw new NoEncontradoException($"No existe una bahía con id {request.IdBahia}.");
            }

            if (!_bahiaService.Ocupar(request.IdBahia))
            {
                throw new ReglaNegocioException($"La bahía {request.IdBahia} no está disponible.");
            }

            lock (_lock)
            {
                var ahora = DateTime.UtcNow;

                var turno = new Turno
                {
                    NumeroTurno = GenerarNumeroTurno(ahora),
                    Placa = request.Placa.Trim().ToUpperInvariant(),
                    TipoVehiculo = tipoVehiculo,
                    TelefonoCliente = request.TelefonoCliente.Trim(),
                    IdServicio = request.IdServicio,
                    IdOperario = request.IdOperario,
                    IdBahia = request.IdBahia,
                    EstadoActual = EstadoTurno.Recepcion,
                    FechaIngreso = ahora,
                    HashConsulta = string.Empty
                };

                turno.HashConsulta = GenerarHash(turno);
                _turnos.Add(turno.Id, turno);

                return new TurnoCreadoResponse
                {
                    IdTurno = turno.Id,
                    NumeroTurno = turno.NumeroTurno,
                    Estado = turno.EstadoActual,
                    HashConsulta = turno.HashConsulta,
                    QrUrl = $"{BaseUrlQr}{turno.HashConsulta}",
                    FechaIngreso = turno.FechaIngreso
                };
            }
        }

        private string GenerarNumeroTurno(DateTime fecha)
        {
            var hoy = DateOnly.FromDateTime(fecha);
            if (hoy != _fechaConsecutivo)
            {
                _fechaConsecutivo = hoy;
                _consecutivo = 0;
            }

            return $"T-{++_consecutivo:D3}";
        }

        private static string GenerarHash(Turno turno)
        {
            var contenido = $"{turno.Id}|{turno.Placa}|{turno.TelefonoCliente}|{turno.FechaIngreso:O}";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(contenido));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
