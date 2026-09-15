using Dapper;
using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Models;
using ApiAutoLavado.Persistencia.Mapeo;

namespace ApiAutoLavado.Persistencia.Repositorios
{
    internal sealed class BahiaRepository : IBahiaRepository
    {
        private const string Columnas =
            "id_bahia AS Id, nombre_bahia AS NombreBahia, tipo AS Tipo, estado AS Estado";

        private readonly IFabricaConexion _fabrica;

        public BahiaRepository(IFabricaConexion fabrica)
        {
            _fabrica = fabrica;
        }

        public IReadOnlyCollection<Bahia> ObtenerTodas()
        {
            using var conexion = _fabrica.Crear();
            var filas = conexion.Query<BahiaFila>($"SELECT {Columnas} FROM bahias");
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

        public bool IntentarOcupar(int id)
        {
            using var conexion = _fabrica.Crear();
            var afectadas = conexion.Execute(
                "UPDATE bahias SET estado = @Ocupada WHERE id_bahia = @Id AND estado = @Disponible",
                new
                {
                    Id = id,
                    Ocupada = EstadoBahia.Ocupada.ANombreBd(),
                    Disponible = EstadoBahia.Disponible.ANombreBd()
                });

            return afectadas > 0;
        }

        public bool Liberar(int id)
        {
            using var conexion = _fabrica.Crear();
            var afectadas = conexion.Execute(
                "UPDATE bahias SET estado = @Disponible WHERE id_bahia = @Id",
                new
                {
                    Id = id,
                    Disponible = EstadoBahia.Disponible.ANombreBd()
                });

            return afectadas > 0;
        }
    }
}
