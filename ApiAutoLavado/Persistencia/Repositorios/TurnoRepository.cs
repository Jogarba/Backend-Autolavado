using Dapper;
using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Models;
using ApiAutoLavado.Persistencia.Mapeo;

namespace ApiAutoLavado.Persistencia.Repositorios
{
    internal sealed class TurnoRepository : ITurnoRepository
    {
        private const string Columnas =
            "id_turno AS Id, numero_turno AS NumeroTurno, placa AS Placa, " +
            "id_servicio AS IdServicio, id_operario AS IdOperario, " +
            "estado_actual AS EstadoActual, fecha_ingreso AS FechaIngreso, " +
            "hash_consulta AS HashConsulta";

        private readonly IFabricaConexion _fabrica;

        public TurnoRepository(IFabricaConexion fabrica)
        {
            _fabrica = fabrica;
        }

        public IReadOnlyCollection<Turno> ObtenerTodos()
        {
            using var conexion = _fabrica.Crear();
            var filas = conexion.Query<TurnoFila>($"SELECT {Columnas} FROM turnos");
            return filas.Select(f => f.AModelo()).ToList();
        }

        public Turno? ObtenerPorId(long id)
        {
            using var conexion = _fabrica.Crear();
            var fila = conexion.QuerySingleOrDefault<TurnoFila>(
                $"SELECT {Columnas} FROM turnos WHERE id_turno = @Id",
                new { Id = id });

            return fila?.AModelo();
        }

        public long Agregar(Turno turno)
        {
            using var conexion = _fabrica.Crear();
            conexion.Open();

            conexion.Execute(
                "INSERT INTO turnos (numero_turno, placa, id_servicio, " +
                "id_operario, estado_actual, fecha_ingreso, hash_consulta) " +
                "VALUES (@NumeroTurno, @Placa, @IdServicio, " +
                "@IdOperario, @EstadoActual, @FechaIngreso, @HashConsulta)",
                new
                {
                    turno.NumeroTurno,
                    turno.Placa,
                    turno.IdServicio,
                    turno.IdOperario,
                    turno.EstadoActual,
                    turno.FechaIngreso,
                    turno.HashConsulta
                });

            return conexion.ExecuteScalar<long>("SELECT LAST_INSERT_ID()");
        }

        public bool IntentarCambiarEstado(long id, string estadoEsperado, string estadoNuevo)
        {
            using var conexion = _fabrica.Crear();
            var afectadas = conexion.Execute(
                "UPDATE turnos SET estado_actual = @Nuevo WHERE id_turno = @Id AND estado_actual = @Esperado",
                new
                {
                    Id = id,
                    Nuevo = estadoNuevo,
                    Esperado = estadoEsperado
                });

            return afectadas > 0;
        }

        public bool AsignarOperario(long idTurno, int idOperario, string estadoNuevo)
        {
            using var conexion = _fabrica.Crear();
            var afectadas = conexion.Execute(
                "UPDATE turnos SET id_operario = @IdOperario, estado_actual = @EstadoNuevo WHERE id_turno = @IdTurno",
                new
                {
                    IdTurno = idTurno,
                    IdOperario = idOperario,
                    EstadoNuevo = estadoNuevo
                });

            return afectadas > 0;
        }
    }
}
