using System.Security.Cryptography;
using System.Text;
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

        private readonly IBahiaRepository _bahias;
        private readonly IOperarioRepository _operarios;
        private readonly IServicioRepository _servicios;
        private readonly ITurnoRepository _turnos;

        private readonly object _consecutivoLock = new();
        private int _consecutivo;
        private DateOnly _fechaConsecutivo = DateOnly.FromDateTime(DateTime.UtcNow);

        public TurnoService(
            IBahiaRepository bahias,
            IOperarioRepository operarios,
            IServicioRepository servicios,
            ITurnoRepository turnos)
        {
            _bahias = bahias;
            _operarios = operarios;
            _servicios = servicios;
            _turnos = turnos;
        }

        public IReadOnlyCollection<TurnoResponse> ObtenerActivos()
        {
            return _turnos.ObtenerTodos()
                .Where(t => t.EstadoActual == EstadoTurno.Recepcion)
                .OrderBy(t => t.FechaIngreso)
                .Select(t => t.ToResponse())
                .ToList();
        }

        public TurnoCreadoResponse Crear(CrearTurnoRequest request)
        {
            if (request.TipoVehiculo is null)
            {
                throw new ReglaNegocioException(
                    "El tipo de vehículo no es válido. Valores permitidos: AUTO, MOTO, CAMIONETA.");
            }

            if (request.IdServicio <= 0)
            {
                throw new ReglaNegocioException("El servicio es obligatorio.");
            }

            if (request.IdOperario <= 0)
            {
                throw new ReglaNegocioException("El operario es obligatorio.");
            }

            if (request.IdBahia <= 0)
            {
                throw new ReglaNegocioException("La bahía es obligatoria.");
            }

            var tipoVehiculo = request.TipoVehiculo.Value;

            var operario = _operarios.ObtenerPorId(request.IdOperario);
            if (operario is null)
            {
                throw new NoEncontradoException($"No existe un operario con id {request.IdOperario}.");
            }

            if (!operario.Activo)
            {
                throw new ReglaNegocioException($"El operario {operario.Nombres} {operario.Apellidos} no está activo.");
            }

            if (_servicios.ObtenerPorId(request.IdServicio) is null)
            {
                throw new NoEncontradoException($"No existe un servicio con id {request.IdServicio}.");
            }

            if (_bahias.ObtenerPorId(request.IdBahia) is null)
            {
                throw new NoEncontradoException($"No existe una bahía con id {request.IdBahia}.");
            }

            if (!_bahias.IntentarOcupar(request.IdBahia))
            {
                throw new ReglaNegocioException($"La bahía {request.IdBahia} no está disponible.");
            }

            try
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
                var idTurno = _turnos.Agregar(turno);

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
            catch
            {
                _bahias.Liberar(request.IdBahia);
                throw;
            }
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
            var contenido = $"{turno.Id}|{turno.Placa}|{turno.TelefonoCliente}|{turno.FechaIngreso:O}";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(contenido));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
