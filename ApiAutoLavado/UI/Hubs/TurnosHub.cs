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
    }
}
