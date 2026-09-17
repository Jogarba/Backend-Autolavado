namespace ApiAutoLavado.Aplicacion.Dtos
{
    /// <summary>Evento en tiempo real cuando a un turno se le asigna una bahía.</summary>
    public class BahiaAsignadaResponse
    {
        public long IdTurno { get; set; }

        public string NumeroTurno { get; set; } = string.Empty;

        public string Placa { get; set; } = string.Empty;

        public int? IdOperario { get; set; }

        public int IdBahia { get; set; }

        public string NombreBahia { get; set; } = string.Empty;
    }
}
