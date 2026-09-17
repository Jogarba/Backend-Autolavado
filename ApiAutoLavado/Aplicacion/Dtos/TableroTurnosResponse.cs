using System;
using System.Collections.Generic;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    /// <summary>
    /// RF-05: vista consolidada del patio dividida entre vehículos en atención y en cola.
    /// </summary>
    public class TableroTurnosResponse
    {
        public IReadOnlyCollection<TurnoDetalleResponse> EnAtencion { get; set; } = Array.Empty<TurnoDetalleResponse>();

        public IReadOnlyCollection<TurnoDetalleResponse> EnCola { get; set; } = Array.Empty<TurnoDetalleResponse>();

        public int Total => EnAtencion.Count + EnCola.Count;
    }

    public class TurnoDetalleResponse
    {
        public long Id { get; set; }

        public string NumeroTurno { get; set; } = string.Empty;

        public string Placa { get; set; } = string.Empty;

        public int IdServicio { get; set; }

        public string NombreServicio { get; set; } = string.Empty;

        public int? IdOperario { get; set; }

        public string? NombreOperario { get; set; }

        public int? IdBahia { get; set; }

        public string? NombreBahia { get; set; }

        public string EstadoActual { get; set; } = string.Empty;

        public string FaseTitulo { get; set; } = string.Empty;

        public int ProgresoPorcentaje { get; set; }

        public DateTime FechaIngreso { get; set; }

        public string HashConsulta { get; set; } = string.Empty;
    }
}
