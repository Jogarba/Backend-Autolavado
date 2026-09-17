using System.ComponentModel.DataAnnotations;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    /// <summary>Edición de una cuenta de administrador (nombre de usuario y estado).</summary>
    public class EditarAdministradorRequest
    {
        [Required]
        [StringLength(60, MinimumLength = 3)]
        public required string NombreUsuario { get; set; }

        public bool Activo { get; set; } = true;
    }
}
