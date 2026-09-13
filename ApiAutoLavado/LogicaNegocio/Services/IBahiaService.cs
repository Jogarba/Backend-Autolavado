using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.LogicaNegocio.Services
{
    public interface IBahiaService
    {
        IReadOnlyCollection<Bahia> ObtenerDisponibles();

        Bahia? ObtenerPorId(Guid id);

        bool Ocupar(Guid id);

        bool Liberar(Guid id);
    }
}
