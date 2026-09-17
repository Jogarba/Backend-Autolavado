using Dapper;
using MySqlConnector;
using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Models;
using ApiAutoLavado.Persistencia.Mapeo;

namespace ApiAutoLavado.Persistencia.Repositorios
{
    internal sealed class ServicioRepository : IServicioRepository
    {
        private const string Columnas =
            "id_servicio AS Id, nombre AS Nombre, tarifa_base AS PrecioBase, tiempo_estimado_min AS TiempoEstimadoMin, fases AS Fases";

        private readonly IFabricaConexion _fabrica;

        public ServicioRepository(IFabricaConexion fabrica)
        {
            _fabrica = fabrica;
        }

        public IReadOnlyCollection<Servicio> ObtenerTodos()
        {
            using var conexion = _fabrica.Crear();
            var filas = conexion.Query<ServicioFila>($"SELECT {Columnas} FROM servicios");
            return filas.Select(f => f.AModelo()).ToList();
        }

        public Servicio? ObtenerPorId(int id)
        {
            using var conexion = _fabrica.Crear();
            var fila = conexion.QuerySingleOrDefault<ServicioFila>(
                $"SELECT {Columnas} FROM servicios WHERE id_servicio = @Id",
                new { Id = id });

            return fila?.AModelo();
        }

        public int Crear(Servicio servicio)
        {
            using var conexion = _fabrica.Crear();

            try
            {
                conexion.Execute(
                    "INSERT INTO servicios (nombre, tarifa_base, tiempo_estimado_min, fases) " +
                    "VALUES (@Nombre, @PrecioBase, @TiempoEstimadoMin, @Fases)",
                    new
                    {
                        servicio.Nombre,
                        servicio.PrecioBase,
                        servicio.TiempoEstimadoMin,
                        servicio.Fases
                    });

                return conexion.ExecuteScalar<int>("SELECT LAST_INSERT_ID()");
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                // Nombre de servicio duplicado (índice único)
                return 0;
            }
        }

        public bool Actualizar(Servicio servicio)
        {
            using var conexion = _fabrica.Crear();

            try
            {
                var afectadas = conexion.Execute(
                    "UPDATE servicios SET nombre = @Nombre, tarifa_base = @PrecioBase, " +
                    "tiempo_estimado_min = @TiempoEstimadoMin, fases = @Fases WHERE id_servicio = @Id",
                    new
                    {
                        servicio.Id,
                        servicio.Nombre,
                        servicio.PrecioBase,
                        servicio.TiempoEstimadoMin,
                        servicio.Fases
                    });

                return afectadas > 0;
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                // Nombre de servicio duplicado (índice único)
                return false;
            }
        }
    }
}
