namespace ApiAutoLavado.Aplicacion.Dtos
{
    public class TurnoCreadoResponse
    {
        public long IdTurno { get; set; }

        public required string NumeroTurno { get; set; }

        public string Estado { get; set; } = string.Empty;

        public required string HashConsulta { get; set; }

        public required string QrUrl { get; set; }

        public DateTime FechaIngreso { get; set; }
    }
}
