using System.ComponentModel.DataAnnotations;

namespace ApiAutoLavado.Models.Dtos
{
    public class CrearProductoRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public required string Nombre { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor que cero.")]
        public decimal Precio { get; set; }
    }
}
