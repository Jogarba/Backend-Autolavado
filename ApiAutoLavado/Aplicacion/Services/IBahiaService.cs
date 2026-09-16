using ApiAutoLavado.Aplicacion.Dtos;

namespace ApiAutoLavado.Aplicacion.Services
{
    public interface IBahiaService
    {
        IReadOnlyCollection<BahiaResponse> ObtenerTodas();

        IReadOnlyCollection<BahiaResponse> ObtenerDisponibles();
    }
}
