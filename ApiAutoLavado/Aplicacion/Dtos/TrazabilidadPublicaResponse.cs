using System;
using System.Collections.Generic;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    /// <summary>
    /// RNF-05: vista pública de trazabilidad. Omite datos sensibles
    /// (teléfono del cliente, tarifa/montos internos y apellidos del operario).
    /// </summary>
    public class TrazabilidadPublicaResponse
    {
        public long IdTurno { get; set; }

        public string NumeroTurno { get; set; } = string.Empty;

        public string Placa { get; set; } = string.Empty;

        public string TipoVehiculo { get; set; } = string.Empty;

        public string NombreServicio { get; set; } = string.Empty;

        public int TiempoEstimadoMin { get; set; }

        public string? NombreOperario { get; set; }

        public string FaseActual { get; set; } = string.Empty;

        public int ProgresoPorcentaje { get; set; }

        public string MensajeEstado { get; set; } = string.Empty;

        public bool EstaListoParaRecoger { get; set; }

        public DateTime FechaIngreso { get; set; }

        public List<FaseHitoDto> Fases { get; set; } = new();

        // RF-CL-02: identifica si la consulta corresponde a un turno de patio
        // ("TURNO") o a una reserva aún no convertida ("RESERVA").
        public string TipoRegistro { get; set; } = "TURNO";

        public string? CodigoReserva { get; set; }

        public DateOnly? FechaReserva { get; set; }

        public TimeOnly? HoraReserva { get; set; }

        public string? EstadoReserva { get; set; }
    }
}
