using ApiAutoLavado.Domain.Enums;

namespace ApiAutoLavado.Domain.Models
{
    public class Turno
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string NumeroTurno { get; set; }

        public required string Placa { get; set; }

        public TipoVehiculo TipoVehiculo { get; set; }

        public required string TelefonoCliente { get; set; }

        public Guid IdServicio { get; set; }

        public Guid IdOperario { get; set; }

        public Guid IdBahia { get; set; }

        public EstadoTurno EstadoActual { get; set; } = EstadoTurno.Recepcion;

        public DateTime FechaIngreso { get; set; }

        public required string HashConsulta { get; set; }
    }
}
