using Dapper;
using MySqlConnector;
using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Models;
using ApiAutoLavado.Persistencia.Mapeo;

namespace ApiAutoLavado.Persistencia.Repositorios
{
    internal sealed class OperarioRepository : IOperarioRepository
    {
        private const string Columnas =
            "o.id_operario AS Id, o.nombres AS Nombres, o.apellidos AS Apellidos, o.documento AS Documento, " +
            "o.telefono AS Telefono, o.usuario_id AS UsuarioId, u.nombre_usuario AS NombreUsuario, " +
            "o.activo AS Activo, o.estado AS Estado, o.fecha_creacion AS FechaCreacion";

        private const string Origen = "operarios o LEFT JOIN usuarios u ON u.id_usuario = o.usuario_id";

        private readonly IFabricaConexion _fabrica;

        public OperarioRepository(IFabricaConexion fabrica)
        {
            _fabrica = fabrica;
        }

        public IReadOnlyCollection<Operario> ObtenerTodos()
        {
            using var conexion = _fabrica.Crear();
            var filas = conexion.Query<OperarioFila>($"SELECT {Columnas} FROM {Origen}");
            return filas.Select(f => f.AModelo()).ToList();
        }

        public Operario? ObtenerPorId(int id)
        {
            using var conexion = _fabrica.Crear();
            var fila = conexion.QuerySingleOrDefault<OperarioFila>(
                $"SELECT {Columnas} FROM {Origen} WHERE o.id_operario = @Id",
                new { Id = id });

            return fila?.AModelo();
        }

        public int? IntentarAgregar(Operario operario, ITransaccionBd? transaccion = null)
        {
            var conexion = transaccion?.Conexion ?? _fabrica.Crear();

            try
            {
                conexion.Execute(
                    "INSERT INTO operarios (nombres, apellidos, documento, telefono, usuario_id, activo, estado, fecha_creacion) " +
                    "VALUES (@Nombres, @Apellidos, @Documento, @Telefono, @UsuarioId, @Activo, @Estado, @FechaCreacion)",
                    new
                    {
                        operario.Nombres,
                        operario.Apellidos,
                        operario.Documento,
                        operario.Telefono,
                        operario.UsuarioId,
                        Activo = operario.Activo ? 1 : 0,
                        Estado = operario.Estado.ANombreBd(),
                        operario.FechaCreacion
                    },
                    transaccion?.Transaccion);

                return conexion.ExecuteScalar<int>(
                    "SELECT LAST_INSERT_ID()",
                    transaction: transaccion?.Transaccion);
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                // Documento o usuario_id duplicado (índices únicos)
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

        public bool Actualizar(Operario operario)
        {
            using var conexion = _fabrica.Crear();

            try
            {
                var afectadas = conexion.Execute(
                    "UPDATE operarios SET nombres = @Nombres, apellidos = @Apellidos, documento = @Documento, " +
                    "telefono = @Telefono WHERE id_operario = @Id",
                    new
                    {
                        operario.Id,
                        operario.Nombres,
                        operario.Apellidos,
                        operario.Documento,
                        operario.Telefono
                    });

                return afectadas > 0;
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                // Documento duplicado (índice único)
                return false;
            }
        }

        public bool Desactivar(int id, ITransaccionBd? transaccion = null)
        {
            var conexion = transaccion?.Conexion ?? _fabrica.Crear();

            try
            {
                var afectadas = conexion.Execute(
                    "UPDATE operarios SET activo = 0, estado = @Inactivo WHERE id_operario = @Id",
                    new
                    {
                        Id = id,
                        Inactivo = EstadoOperario.Inactivo.ANombreBd()
                    },
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

        public bool CambiarEstado(int id, EstadoOperario estado, ITransaccionBd? transaccion = null)
        {
            var conexion = transaccion?.Conexion ?? _fabrica.Crear();

            try
            {
                var afectadas = conexion.Execute(
                    "UPDATE operarios SET estado = @Estado, activo = @Activo WHERE id_operario = @Id",
                    new
                    {
                        Id = id,
                        Estado = estado.ANombreBd(),
                        Activo = estado == EstadoOperario.Inactivo ? 0 : 1
                    },
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

        public bool IntentarOcupar(int id, ITransaccionBd? transaccion = null)
        {
            var conexion = transaccion?.Conexion ?? _fabrica.Crear();

            try
            {
                var afectadas = conexion.Execute(
                    "UPDATE operarios SET estado = @Ocupado WHERE id_operario = @Id AND estado = @Disponible",
                    new
                    {
                        Id = id,
                        Ocupado = EstadoOperario.Ocupado.ANombreBd(),
                        Disponible = EstadoOperario.Disponible.ANombreBd()
                    },
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

        public bool Liberar(int id, ITransaccionBd? transaccion = null)
        {
            var conexion = transaccion?.Conexion ?? _fabrica.Crear();

            try
            {
                var afectadas = conexion.Execute(
                    "UPDATE operarios SET estado = @Disponible WHERE id_operario = @Id AND estado = @Ocupado",
                    new
                    {
                        Id = id,
                        Disponible = EstadoOperario.Disponible.ANombreBd()
                    },
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
