using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Exceptions;
using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _usuarios;

        public UsuarioService(IUsuarioRepository usuarios)
        {
            _usuarios = usuarios;
        }

        public Usuario Crear(string nombreUsuario, string contrasena, RolUsuario rol)
        {
            var nombre = nombreUsuario.Trim();

            if (_usuarios.ObtenerPorNombreUsuario(nombre) is not null)
            {
                throw new ReglaNegocioException($"Ya existe un usuario con el nombre {nombre}.");
            }

            var usuario = new Usuario
            {
                NombreUsuario = nombre,
                ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(contrasena),
                Rol = rol,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            var id = _usuarios.IntentarAgregar(usuario)
                ?? throw new ReglaNegocioException($"Ya existe un usuario con el nombre {nombre}.");

            usuario.Id = id;
            return usuario;
        }

        public Usuario? ObtenerPorNombreUsuario(string nombreUsuario)
            => _usuarios.ObtenerPorNombreUsuario(nombreUsuario.Trim());

        public bool CambiarEstado(int id, bool activo)
            => _usuarios.CambiarActivo(id, activo);
    }
}
