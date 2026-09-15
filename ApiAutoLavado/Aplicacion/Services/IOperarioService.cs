using ApiAutoLavado.Aplicacion.Dtos;

namespace ApiAutoLavado.Aplicacion.Services
{
    public interface IOperarioService
    {
        IReadOnlyCollection<OperarioResponse> ObtenerActivos();

        OperarioResponse Crear(CrearOperarioRequest request);
    }
}
