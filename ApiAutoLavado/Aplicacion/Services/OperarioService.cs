using ApiAutoLavado.Aplicacion.Dtos;
using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Exceptions;
using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Services
{
    public class OperarioService : IOperarioService
    {
        private readonly IOperarioRepository _operarios;
        private readonly IUsuarioRepository _usuarios;
        private readonly IFabricaTransacciones _transacciones;

        public OperarioService(
            IOperarioRepository operarios,
            IUsuarioRepository usuarios,
            IFabricaTransacciones transacciones)
        {
            _operarios = operarios;
            _usuarios = usuarios;
            _transacciones = transacciones;
        }

        public IReadOnlyCollection<OperarioResponse> ObtenerTodos()
        {
            return _operarios.ObtenerTodos()
                .OrderBy(o => o.Nombres)
                .Select(o => o.ToResponse())
                .ToList();
        }

        public IReadOnlyCollection<OperarioResponse> ObtenerActivos()
        {
            return _operarios.ObtenerTodos()
                .Where(o => o.EstaDisponible)
                .OrderBy(o => o.Nombres)
                .Select(o => o.ToResponse())
                .ToList();
        }

        public IReadOnlyCollection<OperarioResponse> ObtenerInactivos()
        {
            return _operarios.ObtenerTodos()
                .Where(o => !o.Activo)
                .OrderBy(o => o.Nombres)
                .Select(o => o.ToResponse())
                .ToList();
        }

        public IReadOnlyCollection<OperarioResponse> ObtenerOcupados()
        {
            return _operarios.ObtenerTodos()
                .Where(o => o.Estado == EstadoOperario.Ocupado)
                .OrderBy(o => o.Nombres)
                .Select(o => o.ToResponse())
                .ToList();
        }

        public OperarioResponse Crear(CrearOperarioRequest request)
        {
            var nombreUsuario = request.NombreUsuario.Trim();

            if (_usuarios.ObtenerPorNombreUsuario(nombreUsuario) is not null)
            {
                throw new ReglaNegocioException($"Ya existe un usuario con el nombre {nombreUsuario}.");
            }

            var ahora = DateTime.UtcNow;

            using var transaccion = _transacciones.Iniciar();

            try
            {
                var usuario = new Usuario
                {
                    NombreUsuario = nombreUsuario,
                    ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(request.Contrasena),
                    Rol = RolUsuario.Operario,
                    Activo = request.Activo,
                    FechaCreacion = ahora
                };

                var usuarioId = _usuarios.IntentarAgregar(usuario, transaccion)
                    ?? throw new ReglaNegocioException($"Ya existe un usuario con el nombre {nombreUsuario}.");

                var operario = new Operario
                {
                    Nombres = request.Nombres.Trim(),
                    Apellidos = request.Apellidos.Trim(),
                    Documento = request.Documento.Trim(),
                    Telefono = request.Telefono.Trim(),
                    UsuarioId = usuarioId,
                    NombreUsuario = usuario.NombreUsuario,
                    Activo = request.Activo,
                    Estado = request.Activo ? EstadoOperario.Disponible : EstadoOperario.Inactivo,
                    FechaCreacion = ahora
                };

                var operarioId = _operarios.IntentarAgregar(operario, transaccion)
                    ?? throw new ReglaNegocioException($"Ya existe un operario con el documento {operario.Documento}.");

                transaccion.Confirmar();

                operario.Id = operarioId;
                return operario.ToResponse();
            }
            catch
            {
                transaccion.Revertir();
                throw;
            }
        }

        public OperarioResponse Editar(int id, EditarOperarioRequest request)
        {
            var operario = _operarios.ObtenerPorId(id)
                ?? throw new NoEncontradoException($"No existe un operario con id {id}.");

            operario.Nombres = request.Nombres.Trim();
            operario.Apellidos = request.Apellidos.Trim();
            operario.Documento = request.Documento.Trim();
            operario.Telefono = request.Telefono.Trim();

            if (!_operarios.Actualizar(operario))
            {
                throw new ReglaNegocioException($"Ya existe un operario con el documento {operario.Documento}.");
            }

            return operario.ToResponse();
        }

        public OperarioResponse Desactivar(int id)
        {
            var operario = _operarios.ObtenerPorId(id)
                ?? throw new NoEncontradoException($"No existe un operario con id {id}.");

            using var transaccion = _transacciones.Iniciar();

            try
            {
                if (operario.UsuarioId is int usuarioId)
                {
                    _usuarios.CambiarActivo(usuarioId, false, transaccion);
                }

                _operarios.Desactivar(id, transaccion);
                transaccion.Confirmar();
            }
            catch
            {
                transaccion.Revertir();
                throw;
            }

            operario.Activo = false;
            operario.Estado = EstadoOperario.Inactivo;
            return operario.ToResponse();
        }
    }
}
