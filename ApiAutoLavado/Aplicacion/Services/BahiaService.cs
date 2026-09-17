using System.Linq;
using ApiAutoLavado.Aplicacion.Dtos;
using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Exceptions;
using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Services
{
    public class BahiaService : IBahiaService
    {
        private readonly IBahiaRepository _bahias;

        public BahiaService(IBahiaRepository bahias)
        {
            _bahias = bahias;
        }

        public IReadOnlyCollection<BahiaResponse> ObtenerTodas()
        {
            return _bahias.ObtenerTodas().Select(b => b.ToResponse()).ToList();
        }

        public IReadOnlyCollection<BahiaResponse> ObtenerDisponibles()
        {
            return _bahias.ObtenerTodas()
                .Where(b => b.Estado == EstadoBahia.Disponible)
                .Select(b => b.ToResponse())
                .ToList();
        }

        public BahiaResponse Crear(CrearBahiaRequest request)
        {
            var bahia = new Bahia
            {
                Nombre = request.Nombre.Trim().ToUpperInvariant(),
                Estado = EstadoBahia.Disponible,
                FechaCreacion = System.DateTime.UtcNow
            };

            var id = _bahias.IntentarAgregar(bahia)
                ?? throw new ReglaNegocioException($"Ya existe una bahía con el nombre {bahia.Nombre}.");

            bahia.Id = id;
            return bahia.ToResponse();
        }

        public BahiaResponse Editar(int id, EditarBahiaRequest request)
        {
            var bahia = _bahias.ObtenerPorId(id)
                ?? throw new NoEncontradoException($"No existe una bahía con id {id}.");

            bahia.Nombre = request.Nombre.Trim().ToUpperInvariant();

            if (!_bahias.Actualizar(bahia))
            {
                throw new ReglaNegocioException($"Ya existe una bahía con el nombre {bahia.Nombre}.");
            }

            return bahia.ToResponse();
        }

        public BahiaResponse CambiarEstado(int id, string estado)
        {
            var bahia = _bahias.ObtenerPorId(id)
                ?? throw new NoEncontradoException($"No existe una bahía con id {id}.");

            var normalizado = (estado ?? string.Empty).Trim().ToUpperInvariant();
            if (!System.Enum.TryParse<EstadoBahia>(normalizado, ignoreCase: true, out var nuevoEstado))
            {
                throw new ReglaNegocioException(
                    $"Estado '{estado}' no válido. Use DISPONIBLE, OCUPADA o MANTENIMIENTO.");
            }

            _bahias.CambiarEstado(id, nuevoEstado);
            bahia.Estado = nuevoEstado;
            return bahia.ToResponse();
        }
    }
}
