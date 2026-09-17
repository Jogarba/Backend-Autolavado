using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Repositorios
{
    public interface IUsuarioRepository
    {
        Usuario? ObtenerPorNombreUsuario(string nombreUsuario);

        Usuario? ObtenerPorId(int id);

        IReadOnlyCollection<Usuario> ObtenerPorRol(RolUsuario rol);

        int? IntentarAgregar(Usuario usuario, ITransaccionBd? transaccion = null);

        bool Actualizar(Usuario usuario);

        bool CambiarContrasena(int id, string contrasenaHash);

        bool CambiarActivo(int id, bool activo, ITransaccionBd? transaccion = null);
    }
}
