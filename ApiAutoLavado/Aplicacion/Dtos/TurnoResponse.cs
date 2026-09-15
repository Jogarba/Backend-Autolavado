using ApiAutoLavado.Domain.Enums;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    public class TurnoResponse
    {
        public long Id { get; set; }

        public required string NumeroTurno { get; set; }

        public required string Placa { get; set; }

        public TipoVehiculo TipoVehiculo { get; set; }

        public required string TelefonoCliente { get; set; }

        public int IdServicio { get; set; }

        public int IdOperario { get; set; }

        public int IdBahia { get; set; }

        public EstadoTurno EstadoActual { get; set; }

        public DateTime FechaIngreso { get; set; }

        public required string HashConsulta { get; set; }
    }
}
