using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ApiAutoLavado.Domain.Enums;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    public class CrearReservaRequest
    {
        [Required]
        [StringLength(6, MinimumLength = 5)]
        public string Placa { get; set; } = string.Empty;

        public TipoVehiculo? TipoVehiculo { get; set; }

        public string? TelefonoCliente { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "El servicio seleccionado no es válido.")]
        public int IdServicio { get; set; }

        [Required]
        public DateOnly FechaReserva { get; set; }

        [Required]
        public TimeOnly HoraReserva { get; set; }
    }

    public class ReservaResponse
    {
        public long IdReserva { get; set; }
        public string CodigoReserva { get; set; } = string.Empty;
        public string Placa { get; set; } = string.Empty;
        public int IdServicio { get; set; }
        public string NombreServicio { get; set; } = string.Empty;
        public decimal TarifaEstimada { get; set; }
        public int TiempoEstimadoMin { get; set; }
        public DateOnly FechaReserva { get; set; }
        public TimeOnly HoraReserva { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? TipoVehiculo { get; set; }
        public string? TelefonoCliente { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string TrackingUrl { get; set; } = string.Empty;
    }

    public class FranjaHorariaDto
    {
        public string Hora { get; set; } = string.Empty;
        public int CapacidadTotal { get; set; }
        public int CuposOcupados { get; set; }
        public int CuposDisponibles { get; set; }
        public bool Disponible { get; set; }
    }

    public class DisponibilidadFechaResponse
    {
        public DateOnly Fecha { get; set; }
        public int OperariosActivos { get; set; }
        public List<FranjaHorariaDto> Franjas { get; set; } = new();
    }
}
