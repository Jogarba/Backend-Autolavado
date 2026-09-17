using System.Threading.Tasks;
using ApiAutoLavado.Aplicacion.Dtos;

namespace ApiAutoLavado.Aplicacion.Services
{
    public interface ITurnoRealtimeNotifier
    {
        Task NotificarCambioEstadoAsync(TrazabilidadTurnoResponse trazabilidad);
        Task NotificarBahiaAsignadaAsync(BahiaAsignadaResponse evento);
        Task NotificarTurnosActualizadosAsync();
    }
}
