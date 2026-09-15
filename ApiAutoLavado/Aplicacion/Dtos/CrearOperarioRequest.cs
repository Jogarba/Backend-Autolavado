using System.ComponentModel.DataAnnotations;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    public class CrearOperarioRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public required string Nombres { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public required string Apellidos { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 1)]
        public required string Documento { get; set; }

        [Required]
        [StringLength(10)]
        [RegularExpression("^[0-9]+$", ErrorMessage = "El teléfono solo debe contener números.")]
        public required string Telefono { get; set; }

        public bool Activo { get; set; } = true;
    }
}
