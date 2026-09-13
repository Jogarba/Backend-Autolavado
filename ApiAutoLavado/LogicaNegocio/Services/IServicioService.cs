using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.LogicaNegocio.Services
{
    public interface IServicioService
    {
        IReadOnlyCollection<Servicio> ObtenerTodos();

        Servicio? ObtenerPorId(Guid id);
    }
}
