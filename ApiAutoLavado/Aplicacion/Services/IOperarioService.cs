using ApiAutoLavado.Aplicacion.Dtos;

namespace ApiAutoLavado.Aplicacion.Services
{
    public interface IOperarioService
    {
        IReadOnlyCollection<OperarioResponse> ObtenerTodos();

        IReadOnlyCollection<OperarioResponse> ObtenerActivos();

        IReadOnlyCollection<OperarioResponse> ObtenerInactivos();

        IReadOnlyCollection<OperarioResponse> ObtenerOcupados();

        OperarioResponse Crear(CrearOperarioRequest request);
    }
}
