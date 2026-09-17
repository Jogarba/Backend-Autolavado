using System.ComponentModel.DataAnnotations;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    /// <summary>Cambio de contraseña de una cuenta de usuario.</summary>
    public class CambiarContrasenaRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 6)]
        public required string Contrasena { get; set; }
    }
}
