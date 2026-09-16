using ApiAutoLavado.Domain.Enums;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    public class LoginResponse
    {
        public required string Token { get; set; }

        public RolUsuario Rol { get; set; }

        public required string NombreUsuario { get; set; }

        public DateTime ExpiraEn { get; set; }
    }
}
