namespace ApiAutoLavado.Aplicacion.Dtos
{
    /// <summary>
    /// Vista pública para la pantalla de sala de espera: id, placa y estado del turno,
    /// sin datos del cliente, operario ni montos (RNF-05).
    /// </summary>
    public class TurnoDisplayResponse
    {
        public long IdTurno { get; set; }

        public string NumeroTurno { get; set; } = string.Empty;

        public string Placa { get; set; } = string.Empty;

        public bool EnAtencion { get; set; }

        public string? NombreBahia { get; set; }

        public string EstadoActual { get; set; } = string.Empty;

        public string FaseTitulo { get; set; } = string.Empty;

        public int ProgresoPorcentaje { get; set; }
    }
}
