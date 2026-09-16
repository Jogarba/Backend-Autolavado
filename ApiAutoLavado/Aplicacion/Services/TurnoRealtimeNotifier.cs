using System.Threading.Tasks;
using ApiAutoLavado.Aplicacion.Dtos;
using ApiAutoLavado.UI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ApiAutoLavado.Aplicacion.Services
{
    public class TurnoRealtimeNotifier : ITurnoRealtimeNotifier
    {
        private readonly IHubContext<TurnosHub> _hubContext;

        public TurnoRealtimeNotifier(IHubContext<TurnosHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotificarCambioEstadoAsync(TrazabilidadTurnoResponse trazabilidad)
        {
            // Notifica a los suscriptores por grupo (placa y hash_consulta) y global.
            // RNF-05: los canales en vivo solo transportan la vista pública (sin datos sensibles).
            var publica = trazabilidad.ToPublica();
            var placa = trazabilidad.Placa.ToUpperInvariant();
            var hash = trazabilidad.HashConsulta.ToUpperInvariant();

            await _hubContext.Clients.Group(placa).SendAsync("EstadoTurnoActualizado", publica);
            if (!string.IsNullOrEmpty(hash))
            {
                await _hubContext.Clients.Group(hash).SendAsync("EstadoTurnoActualizado", publica);
            }
            await _hubContext.Clients.All.SendAsync("TurnoGlobalActualizado", publica);
        }

        public async Task NotificarTurnosActualizadosAsync()
        {
            await _hubContext.Clients.All.SendAsync("TurnosRecargados");
        }
    }
}
