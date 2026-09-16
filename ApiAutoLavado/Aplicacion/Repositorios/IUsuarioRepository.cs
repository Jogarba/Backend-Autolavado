using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Repositorios
{
    public interface IUsuarioRepository
    {
        Usuario? ObtenerPorNombreUsuario(string nombreUsuario);

        int? IntentarAgregar(Usuario usuario, ITransaccionBd? transaccion = null);

        bool CambiarActivo(int id, bool activo, ITransaccionBd? transaccion = null);
    }
}
