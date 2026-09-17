using System.ComponentModel.DataAnnotations;
using ApiAutoLavado.Domain.Enums;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    public class BahiaResponse
    {
        public int Id { get; set; }

        public required string Nombre { get; set; }

        public EstadoBahia Estado { get; set; }
    }

    public class CrearBahiaRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public required string Nombre { get; set; }
    }

    public class EditarBahiaRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public required string Nombre { get; set; }
    }

    /// <summary>Admin: cambia el estado operativo de una bahía (DISPONIBLE/OCUPADA/MANTENIMIENTO).</summary>
    public class CambiarEstadoBahiaRequest
    {
        [Required]
        public required string Estado { get; set; }
    }

    /// <summary>Operario/admin: asigna una bahía a un turno.</summary>
    public class AsignarBahiaRequest
    {
        [Range(1, int.MaxValue)]
        public int IdBahia { get; set; }
    }
}
