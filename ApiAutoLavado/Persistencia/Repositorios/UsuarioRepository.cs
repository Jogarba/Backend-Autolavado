using System.Linq;
using Dapper;
using MySqlConnector;
using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Models;
using ApiAutoLavado.Persistencia.Mapeo;

namespace ApiAutoLavado.Persistencia.Repositorios
{
    internal sealed class UsuarioRepository : IUsuarioRepository
    {
        private const string Columnas =
            "id_usuario AS Id, nombre_usuario AS NombreUsuario, contrasena_hash AS ContrasenaHash, " +
            "rol AS Rol, activo AS Activo, fecha_creacion AS FechaCreacion";

        private readonly IFabricaConexion _fabrica;

        public UsuarioRepository(IFabricaConexion fabrica)
        {
            _fabrica = fabrica;
        }

        public Usuario? ObtenerPorNombreUsuario(string nombreUsuario)
        {
            using var conexion = _fabrica.Crear();
            var fila = conexion.QuerySingleOrDefault<UsuarioFila>(
                $"SELECT {Columnas} FROM usuarios WHERE nombre_usuario = @NombreUsuario",
                new { NombreUsuario = nombreUsuario });

            return fila?.AModelo();
        }

        public Usuario? ObtenerPorId(int id)
        {
            using var conexion = _fabrica.Crear();
            var fila = conexion.QuerySingleOrDefault<UsuarioFila>(
                $"SELECT {Columnas} FROM usuarios WHERE id_usuario = @Id",
                new { Id = id });

            return fila?.AModelo();
        }

        public IReadOnlyCollection<Usuario> ObtenerPorRol(RolUsuario rol)
        {
            using var conexion = _fabrica.Crear();
            var filas = conexion.Query<UsuarioFila>(
                $"SELECT {Columnas} FROM usuarios WHERE rol = @Rol ORDER BY nombre_usuario",
                new { Rol = rol.ANombreBd() });

            return filas.Select(f => f.AModelo()).ToList();
        }

        public int? IntentarAgregar(Usuario usuario, ITransaccionBd? transaccion = null)
        {
            var conexion = transaccion?.Conexion ?? _fabrica.Crear();

            try
            {
                conexion.Execute(
                    "INSERT INTO usuarios (nombre_usuario, contrasena_hash, rol, activo, fecha_creacion) " +
                    "VALUES (@NombreUsuario, @ContrasenaHash, @Rol, @Activo, @FechaCreacion)",
                    new
                    {
                        usuario.NombreUsuario,
                        usuario.ContrasenaHash,
                        Rol = usuario.Rol.ANombreBd(),
                        Activo = usuario.Activo ? 1 : 0,
                        usuario.FechaCreacion
                    },
                    transaccion?.Transaccion);

                return conexion.ExecuteScalar<int>(
                    "SELECT LAST_INSERT_ID()",
                    transaction: transaccion?.Transaccion);
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                // Nombre de usuario duplicado (índice único)
                return null;
            }
            finally
            {
                if (transaccion is null)
                {
                    conexion.Dispose();
                }
            }
        }

        public bool Actualizar(Usuario usuario)
        {
            using var conexion = _fabrica.Crear();

            try
            {
                var afectadas = conexion.Execute(
                    "UPDATE usuarios SET nombre_usuario = @NombreUsuario, activo = @Activo WHERE id_usuario = @Id",
                    new
                    {
                        usuario.Id,
                        usuario.NombreUsuario,
                        Activo = usuario.Activo ? 1 : 0
                    });

                return afectadas > 0;
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                // Nombre de usuario duplicado (índice único)
                return false;
            }
        }

        public bool CambiarContrasena(int id, string contrasenaHash)
        {
            using var conexion = _fabrica.Crear();
            var afectadas = conexion.Execute(
                "UPDATE usuarios SET contrasena_hash = @Hash WHERE id_usuario = @Id",
                new { Id = id, Hash = contrasenaHash });

            return afectadas > 0;
        }

        public bool CambiarActivo(int id, bool activo, ITransaccionBd? transaccion = null)
        {
            var conexion = transaccion?.Conexion ?? _fabrica.Crear();

            try
            {
                var afectadas = conexion.Execute(
                    "UPDATE usuarios SET activo = @Activo WHERE id_usuario = @Id",
                    new { Id = id, Activo = activo ? 1 : 0 },
                    transaccion?.Transaccion);

                return afectadas > 0;
            }
            finally
            {
                if (transaccion is null)
                {
                    conexion.Dispose();
                }
            }
        }
    }
}
