using ApiAutoLavado.Domain.Enums;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    public class TurnoCreadoResponse
    {
        public long IdTurno { get; set; }

        public required string NumeroTurno { get; set; }

        public EstadoTurno Estado { get; set; }

        public required string HashConsulta { get; set; }

        public required string QrUrl { get; set; }

        public DateTime FechaIngreso { get; set; }
    }
}
