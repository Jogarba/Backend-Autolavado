using System.Linq;
using Dapper;
using MySqlConnector;
using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Models;
using ApiAutoLavado.Persistencia.Mapeo;

namespace ApiAutoLavado.Persistencia.Repositorios
{
    internal sealed class BahiaRepository : IBahiaRepository
    {
        private const string Columnas =
            "id_bahia AS Id, nombre AS Nombre, estado AS Estado, fecha_creacion AS FechaCreacion";

        private readonly IFabricaConexion _fabrica;

        public BahiaRepository(IFabricaConexion fabrica)
        {
            _fabrica = fabrica;
        }

        public IReadOnlyCollection<Bahia> ObtenerTodas()
        {
            using var conexion = _fabrica.Crear();
            var filas = conexion.Query<BahiaFila>($"SELECT {Columnas} FROM bahias ORDER BY nombre");
            return filas.Select(f => f.AModelo()).ToList();
        }

        public Bahia? ObtenerPorId(int id)
        {
            using var conexion = _fabrica.Crear();
            var fila = conexion.QuerySingleOrDefault<BahiaFila>(
                $"SELECT {Columnas} FROM bahias WHERE id_bahia = @Id",
                new { Id = id });

            return fila?.AModelo();
        }

        public int? IntentarAgregar(Bahia bahia)
        {
            using var conexion = _fabrica.Crear();

            try
            {
                conexion.Execute(
                    "INSERT INTO bahias (nombre, estado, fecha_creacion) " +
                    "VALUES (@Nombre, @Estado, @FechaCreacion)",
                    new
                    {
                        bahia.Nombre,
                        Estado = bahia.Estado.ANombreBd(),
                        bahia.FechaCreacion
                    });

                return conexion.ExecuteScalar<int>("SELECT LAST_INSERT_ID()");
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                // Nombre de bahía duplicado
                return null;
            }
        }

        public bool Actualizar(Bahia bahia)
        {
            using var conexion = _fabrica.Crear();

            try
            {
                var afectadas = conexion.Execute(
                    "UPDATE bahias SET nombre = @Nombre WHERE id_bahia = @Id",
                    new { bahia.Id, bahia.Nombre });

                return afectadas > 0;
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return false;
            }
        }

        public bool CambiarEstado(int id, EstadoBahia estado)
        {
            using var conexion = _fabrica.Crear();
            var afectadas = conexion.Execute(
                "UPDATE bahias SET estado = @Estado WHERE id_bahia = @Id",
                new { Id = id, Estado = estado.ANombreBd() });

            return afectadas > 0;
        }

        public bool IntentarOcupar(int id, ITransaccionBd? transaccion = null)
        {
            var conexion = transaccion?.Conexion ?? _fabrica.Crear();

            try
            {
                var afectadas = conexion.Execute(
                    "UPDATE bahias SET estado = @Ocupada WHERE id_bahia = @Id AND estado = @Disponible",
                    new
                    {
                        Id = id,
                        Ocupada = EstadoBahia.Ocupada.ANombreBd(),
                        Disponible = EstadoBahia.Disponible.ANombreBd()
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
                    "UPDATE bahias SET estado = @Disponible WHERE id_bahia = @Id AND estado = @Ocupada",
                    new
                    {
                        Id = id,
                        Disponible = EstadoBahia.Disponible.ANombreBd()
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
