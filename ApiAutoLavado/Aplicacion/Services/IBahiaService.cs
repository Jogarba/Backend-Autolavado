using ApiAutoLavado.Aplicacion.Dtos;

namespace ApiAutoLavado.Aplicacion.Services
{
    public interface IBahiaService
    {
        IReadOnlyCollection<BahiaResponse> ObtenerTodas();

        IReadOnlyCollection<BahiaResponse> ObtenerDisponibles();

        BahiaResponse Crear(CrearBahiaRequest request);

        BahiaResponse Editar(int id, EditarBahiaRequest request);

        BahiaResponse CambiarEstado(int id, string estado);
    }
}
