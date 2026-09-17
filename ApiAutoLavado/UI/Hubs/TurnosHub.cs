using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ApiAutoLavado.UI.Hubs
{
    public class TurnosHub : Hub
    {
        public async Task SuscribirVehiculo(string identificador)
        {
            if (!string.IsNullOrWhiteSpace(identificador))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, identificador.Trim().ToUpperInvariant());
            }
        }

        public async Task DesuscribirVehiculo(string identificador)
        {
            if (!string.IsNullOrWhiteSpace(identificador))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, identificador.Trim().ToUpperInvariant());
            }
        }

        /// <summary>El operario se suscribe a los eventos de su propia cuenta.</summary>
        public async Task SuscribirOperario(int idOperario)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"operario-{idOperario}");
        }

        public async Task DesuscribirOperario(int idOperario)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"operario-{idOperario}");
        }

        /// <summary>Suscribe la conexión a los eventos de un turno específico.</summary>
        public async Task SuscribirTurno(long idTurno)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"turno-{idTurno}");
        }

        public async Task DesuscribirTurno(long idTurno)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"turno-{idTurno}");
        }
    }
}
