using ApiAutoLavado.Domain.Enums;

namespace ApiAutoLavado.Domain.Models
{
    public class Turno
    {
        public long Id { get; set; }

        public required string NumeroTurno { get; set; }

        public required string Placa { get; set; }

        public int IdServicio { get; set; }

        public int? IdOperario { get; set; }

        public string EstadoActual { get; set; } = "EN_COLA";

        public DateTime FechaIngreso { get; set; }

        public required string HashConsulta { get; set; }
    }
}
