using System;
using System.Linq;
using Dapper;
using ApiAutoLavado.Aplicacion.Repositorios;
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

        public long Agregar(Turno turno, ITransaccionBd? transaccion = null)
        {
            var conexion = transaccion?.Conexion ?? _fabrica.Crear();

            try
            {
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
                    },
                    transaccion?.Transaccion);

                return conexion.ExecuteScalar<long>(
                    "SELECT LAST_INSERT_ID()",
                    transaction: transaccion?.Transaccion);
            }
            finally
            {
                if (transaccion is null)
                {
                    conexion.Dispose();
                }
            }
        }

        public bool IntentarCambiarEstado(long id, string estadoEsperado, string estadoNuevo, ITransaccionBd? transaccion = null)
        {
            var conexion = transaccion?.Conexion ?? _fabrica.Crear();

            try
            {
                var afectadas = conexion.Execute(
                    "UPDATE turnos SET estado_actual = @Nuevo WHERE id_turno = @Id AND estado_actual = @Esperado",
                    new
                    {
                        Id = id,
                        Nuevo = estadoNuevo,
                        Esperado = estadoEsperado
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

        public bool AsignarOperario(long idTurno, int idOperario, string estadoNuevo, ITransaccionBd? transaccion = null)
        {
            var conexion = transaccion?.Conexion ?? _fabrica.Crear();

            try
            {
                var afectadas = conexion.Execute(
                    "UPDATE turnos SET id_operario = @IdOperario, estado_actual = @EstadoNuevo " +
                    "WHERE id_turno = @IdTurno AND estado_actual = 'EN_COLA'",
                    new
                    {
                        IdTurno = idTurno,
                        IdOperario = idOperario,
                        EstadoNuevo = estadoNuevo
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

        public int ObtenerMaximoSecuenciaDelDia(DateOnly fecha)
        {
            using var conexion = _fabrica.Crear();
            return conexion.ExecuteScalar<int>(
                "SELECT COALESCE(MAX(CAST(SUBSTRING(numero_turno, 3) AS UNSIGNED)), 0) " +
                "FROM turnos WHERE DATE(fecha_ingreso) = @Fecha",
                new { Fecha = fecha.ToDateTime(TimeOnly.MinValue) });
        }

        public int ContarActivosPorFechaYHora(DateOnly fecha, TimeOnly hora, ITransaccionBd? transaccion = null)
        {
            var conexion = transaccion?.Conexion ?? _fabrica.Crear();

            try
            {
                return conexion.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM turnos " +
                    "WHERE DATE(fecha_ingreso) = @Fecha AND HOUR(fecha_ingreso) = @Hora " +
                    "AND estado_actual NOT IN ('FINALIZADO', 'CANCELADO')",
                    new
                    {
                        Fecha = fecha.ToDateTime(TimeOnly.MinValue),
                        Hora = hora.Hour
                    },
                    transaccion?.Transaccion);
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
