using ApiAutoLavado.Domain.Enums;

namespace ApiAutoLavado.Domain.Models
{
    public class Vehiculo
    {
        public required string Placa { get; set; }
        
        public TipoVehiculo TipoVehiculo { get; set; }
        
        public required string TelefonoCliente { get; set; }
        
        public DateTime FechaPrimerRegistro { get; set; } = DateTime.UtcNow;
    }
}
