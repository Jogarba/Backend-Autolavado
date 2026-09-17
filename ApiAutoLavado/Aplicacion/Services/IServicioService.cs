using ApiAutoLavado.Aplicacion.Dtos;

namespace ApiAutoLavado.Aplicacion.Services
{
    public interface IServicioService
    {
        IReadOnlyCollection<ServicioResponse> ObtenerTodos();

        ServicioResponse Crear(CrearServicioRequest request);

        ServicioResponse Editar(int id, EditarServicioRequest request);
    }
}
