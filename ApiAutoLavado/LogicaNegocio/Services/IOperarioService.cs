using ApiAutoLavado.Domain.Models;
using ApiAutoLavado.UI.Dtos;

namespace ApiAutoLavado.LogicaNegocio.Services
{
    public interface IOperarioService
    {
        IReadOnlyCollection<Operario> ObtenerActivos();

        Operario? ObtenerPorId(Guid id);

        Operario Crear(CrearOperarioRequest request);
    }
}
