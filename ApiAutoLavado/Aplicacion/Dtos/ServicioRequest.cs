using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    public class CrearServicioRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public required string Nombre { get; set; }

        [Range(0, 99_999_999)]
        public decimal PrecioBase { get; set; }

        [Range(1, 100_000)]
        public int TiempoEstimadoMin { get; set; }

        /// <summary>Secuencia de fases (RN-05). Debe iniciar en EN_COLA y terminar en LISTO.</summary>
        [Required]
        [MinLength(2)]
        public List<string> Fases { get; set; } = new();
    }

    public class EditarServicioRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public required string Nombre { get; set; }

        [Range(0, 99_999_999)]
        public decimal PrecioBase { get; set; }

        [Range(1, 100_000)]
        public int TiempoEstimadoMin { get; set; }

        [Required]
        [MinLength(2)]
        public List<string> Fases { get; set; } = new();
    }
}
