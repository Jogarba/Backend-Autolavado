using ApiAutoLavado.Aplicacion.Dtos;

namespace ApiAutoLavado.Aplicacion.Services
{
    public interface IUsuarioService
    {
        UsuarioResponse CrearAdministrador(CrearAdministradorRequest request);

        IReadOnlyCollection<UsuarioResponse> ObtenerAdministradores();

        UsuarioResponse EditarAdministrador(int id, EditarAdministradorRequest request);

        UsuarioResponse CambiarContrasenaAdministrador(int id, CambiarContrasenaRequest request);

        UsuarioResponse DesactivarAdministrador(int id);
    }
}
