namespace ApiAutoLavado.Aplicacion.Dtos
{
    public class TurnoResponse
    {
        public long Id { get; set; }

        public required string NumeroTurno { get; set; }

        public required string Placa { get; set; }

        public int IdServicio { get; set; }

        public int? IdOperario { get; set; }

        public int? IdBahia { get; set; }

        public string EstadoActual { get; set; } = string.Empty;

        public DateTime FechaIngreso { get; set; }

        public required string HashConsulta { get; set; }
    }
}
