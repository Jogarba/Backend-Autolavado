using System;
using ApiAutoLavado.Domain.Enums;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    /// <summary>Vista de una cuenta de usuario. Nunca expone ContrasenaHash.</summary>
    public class UsuarioResponse
    {
        public int Id { get; set; }

        public required string NombreUsuario { get; set; }

        public RolUsuario Rol { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }
    }
}
