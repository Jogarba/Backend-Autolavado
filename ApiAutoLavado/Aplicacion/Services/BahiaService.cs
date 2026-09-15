using ApiAutoLavado.Aplicacion.Dtos;
using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Enums;

namespace ApiAutoLavado.Aplicacion.Services
{
    public class BahiaService : IBahiaService
    {
        private readonly IBahiaRepository _bahias;

        public BahiaService(IBahiaRepository bahias)
        {
            _bahias = bahias;
        }

        public IReadOnlyCollection<BahiaResponse> ObtenerDisponibles()
        {
            return _bahias.ObtenerTodas()
                .Where(b => b.Estado == EstadoBahia.Disponible)
                .OrderBy(b => b.NombreBahia)
                .Select(b => b.ToResponse())
                .ToList();
        }
    }
}
