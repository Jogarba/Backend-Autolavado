using Dapper;
using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Models;
using ApiAutoLavado.Persistencia.Mapeo;

namespace ApiAutoLavado.Persistencia.Repositorios
{
    internal sealed class ServicioRepository : IServicioRepository
    {
        private const string Columnas =
            "id_servicio AS Id, nombre AS Nombre, tarifa_base AS PrecioBase, tiempo_estimado_min AS TiempoEstimadoMin";

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
    }
}
