using System;
using System.Collections.Generic;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    public class ActualizarFaseRequest
    {
        public string NuevaFase { get; set; } = string.Empty;
    }

    public class TrazabilidadTurnoResponse
    {
        public long IdTurno { get; set; }
        public string NumeroTurno { get; set; } = string.Empty;
        public string Placa { get; set; } = string.Empty;
        public string TipoVehiculo { get; set; } = string.Empty;
        public string TelefonoCliente { get; set; } = string.Empty;
        public string NombreServicio { get; set; } = string.Empty;
        public decimal TarifaBase { get; set; }
        public int TiempoEstimadoMin { get; set; }
        public int? IdOperario { get; set; }
        public string? NombreOperario { get; set; }
        public string FaseActual { get; set; } = string.Empty;
        public int ProgresoPorcentaje { get; set; }
        public string MensajeEstado { get; set; } = string.Empty;
        public bool EstaListoParaRecoger { get; set; }
        public string HashConsulta { get; set; } = string.Empty;
        public DateTime FechaIngreso { get; set; }
        public List<FaseHitoDto> Fases { get; set; } = new();
    }

    public class FaseHitoDto
    {
        public string Clave { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Completada { get; set; }
        public bool EnCurso { get; set; }
    }
}
