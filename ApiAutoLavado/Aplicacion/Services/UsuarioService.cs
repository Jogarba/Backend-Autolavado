using System.Linq;
using ApiAutoLavado.Aplicacion.Dtos;
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

        public UsuarioResponse CrearAdministrador(CrearAdministradorRequest request)
        {
            var nombre = request.NombreUsuario.Trim();

            if (_usuarios.ObtenerPorNombreUsuario(nombre) is not null)
            {
                throw new ReglaNegocioException($"Ya existe un usuario con el nombre {nombre}.");
            }

            var usuario = new Usuario
            {
                NombreUsuario = nombre,
                ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(request.Contrasena),
                Rol = RolUsuario.Administrador,
                Activo = request.Activo,
                FechaCreacion = DateTime.UtcNow
            };

            var id = _usuarios.IntentarAgregar(usuario)
                ?? throw new ReglaNegocioException($"Ya existe un usuario con el nombre {nombre}.");

            usuario.Id = id;
            return usuario.ToResponse();
        }

        public IReadOnlyCollection<UsuarioResponse> ObtenerAdministradores()
        {
            return _usuarios.ObtenerPorRol(RolUsuario.Administrador)
                .Select(u => u.ToResponse())
                .ToList();
        }

        public UsuarioResponse EditarAdministrador(int id, EditarAdministradorRequest request)
        {
            var usuario = ObtenerAdministrador(id);
            var nuevoNombre = request.NombreUsuario.Trim();

            if (!string.Equals(usuario.NombreUsuario, nuevoNombre, StringComparison.OrdinalIgnoreCase))
            {
                var existente = _usuarios.ObtenerPorNombreUsuario(nuevoNombre);
                if (existente is not null && existente.Id != id)
                {
                    throw new ReglaNegocioException($"Ya existe un usuario con el nombre {nuevoNombre}.");
                }
            }

            if (usuario.Activo && !request.Activo)
            {
                var administradores = _usuarios.ObtenerPorRol(RolUsuario.Administrador);
                if (administradores.Count(a => a.Activo) <= 1)
                {
                    throw new ReglaNegocioException("No se puede desactivar al último administrador activo.");
                }
            }

            usuario.NombreUsuario = nuevoNombre;
            usuario.Activo = request.Activo;

            if (!_usuarios.Actualizar(usuario))
            {
                throw new ReglaNegocioException($"Ya existe un usuario con el nombre {nuevoNombre}.");
            }

            return usuario.ToResponse();
        }

        public UsuarioResponse CambiarContrasenaAdministrador(int id, CambiarContrasenaRequest request)
        {
            var usuario = ObtenerAdministrador(id);

            var hash = BCrypt.Net.BCrypt.HashPassword(request.Contrasena);
            if (!_usuarios.CambiarContrasena(id, hash))
            {
                throw new ReglaNegocioException($"No se pudo cambiar la contraseña del administrador {id}.");
            }

            return usuario.ToResponse();
        }

        public UsuarioResponse DesactivarAdministrador(int id)
        {
            var objetivo = ObtenerAdministrador(id);

            // Evita dejar el sistema sin ningún administrador activo.
            if (objetivo.Activo && _usuarios.ObtenerPorRol(RolUsuario.Administrador).Count(a => a.Activo) <= 1)
            {
                throw new ReglaNegocioException("No se puede desactivar al último administrador activo.");
            }

            _usuarios.CambiarActivo(id, false);
            objetivo.Activo = false;
            return objetivo.ToResponse();
        }

        private Usuario ObtenerAdministrador(int id)
        {
            var usuario = _usuarios.ObtenerPorId(id)
                ?? throw new NoEncontradoException($"No existe un usuario con id {id}.");

            if (usuario.Rol != RolUsuario.Administrador)
            {
                throw new ReglaNegocioException($"El usuario {id} no es un administrador.");
            }

            return usuario;
        }
    }
}
