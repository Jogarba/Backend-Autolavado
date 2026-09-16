using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Services
{
    public interface IUsuarioService
    {
        Usuario Crear(string nombreUsuario, string contrasena, RolUsuario rol);

        Usuario? ObtenerPorNombreUsuario(string nombreUsuario);

        bool CambiarEstado(int id, bool activo);
    }
}
