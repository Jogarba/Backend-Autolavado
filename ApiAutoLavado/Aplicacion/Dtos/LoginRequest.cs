using System.ComponentModel.DataAnnotations;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    public class LoginRequest
    {
        [Required]
        [StringLength(60, MinimumLength = 3)]
        public required string NombreUsuario { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public required string Contrasena { get; set; }
    }
}
