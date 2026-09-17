using System.ComponentModel.DataAnnotations;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    /// <summary>
    /// Alta de una cuenta con rol Administrador (no requiere ficha de operario).
    /// </summary>
    public class CrearAdministradorRequest
    {
        [Required]
        [StringLength(60, MinimumLength = 3)]
        public required string NombreUsuario { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public required string Contrasena { get; set; }

        public bool Activo { get; set; } = true;
    }
}
