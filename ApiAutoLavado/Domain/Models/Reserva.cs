using System;

namespace ApiAutoLavado.Domain.Models
{
    public class Reserva
    {
        public long IdReserva { get; set; }
        public string CodigoReserva { get; set; } = string.Empty;
        public string Placa { get; set; } = string.Empty;
        public int IdServicio { get; set; }
        public DateOnly FechaReserva { get; set; }
        public TimeOnly HoraReserva { get; set; }
        public string Estado { get; set; } = "PENDIENTE";
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Propiedades de navegación / vista
        public string? NombreServicio { get; set; }
        public decimal? TarifaBase { get; set; }
        public int? TiempoEstimadoMin { get; set; }
        public string? TipoVehiculo { get; set; }
        public string? TelefonoCliente { get; set; }
    }
}
