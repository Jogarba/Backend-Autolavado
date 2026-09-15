using ApiAutoLavado.Aplicacion.Dtos;

namespace ApiAutoLavado.Aplicacion.Services
{
    public interface IServicioService
    {
        IReadOnlyCollection<ServicioResponse> ObtenerTodos();
    }
}
