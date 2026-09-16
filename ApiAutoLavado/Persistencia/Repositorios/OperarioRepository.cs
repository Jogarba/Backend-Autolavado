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
            "id_operario AS Id, nombres AS Nombres, apellidos AS Apellidos, documento AS Documento, " +
            "telefono AS Telefono, activo AS Activo, estado AS Estado";

        private readonly IFabricaConexion _fabrica;

        public OperarioRepository(IFabricaConexion fabrica)
        {
            _fabrica = fabrica;
        }

        public IReadOnlyCollection<Operario> ObtenerTodos()
        {
            using var conexion = _fabrica.Crear();
            var filas = conexion.Query<OperarioFila>($"SELECT {Columnas} FROM operarios");
            return filas.Select(f => f.AModelo()).ToList();
        }

        public Operario? ObtenerPorId(int id)
        {
            using var conexion = _fabrica.Crear();
            var fila = conexion.QuerySingleOrDefault<OperarioFila>(
                $"SELECT {Columnas} FROM operarios WHERE id_operario = @Id",
                new { Id = id });

            return fila?.AModelo();
        }

        public int? IntentarAgregar(Operario operario)
        {
            using var conexion = _fabrica.Crear();
            conexion.Open();

            try
            {
                conexion.Execute(
                    "INSERT INTO operarios (nombres, apellidos, documento, telefono, activo, estado) " +
                    "VALUES (@Nombres, @Apellidos, @Documento, @Telefono, @Activo, @Estado)",
                    new
                    {
                        operario.Nombres,
                        operario.Apellidos,
                        operario.Documento,
                        operario.Telefono,
                        Activo = operario.Activo ? 1 : 0,
                        Estado = operario.Estado.ANombreBd()
                    });

                return conexion.ExecuteScalar<int>("SELECT LAST_INSERT_ID()");
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                // Documento duplicado (índice único)
                return null;
            }
        }

        public bool IntentarOcupar(int id)
        {
            using var conexion = _fabrica.Crear();
            var afectadas = conexion.Execute(
                "UPDATE operarios SET estado = @Ocupado WHERE id_operario = @Id AND estado = @Disponible",
                new
                {
                    Id = id,
                    Ocupado = EstadoOperario.Ocupado.ANombreBd(),
                    Disponible = EstadoOperario.Disponible.ANombreBd()
                });

            return afectadas > 0;
        }

        public bool Liberar(int id)
        {
            using var conexion = _fabrica.Crear();
            var afectadas = conexion.Execute(
                "UPDATE operarios SET estado = @Disponible WHERE id_operario = @Id",
                new
                {
                    Id = id,
                    Disponible = EstadoOperario.Disponible.ANombreBd()
                });

            return afectadas > 0;
        }
    }
}
