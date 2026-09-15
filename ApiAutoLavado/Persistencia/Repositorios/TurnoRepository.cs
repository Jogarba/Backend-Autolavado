using Dapper;
using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Models;
using ApiAutoLavado.Persistencia.Mapeo;

namespace ApiAutoLavado.Persistencia.Repositorios
{
    internal sealed class TurnoRepository : ITurnoRepository
    {
        private const string Columnas =
            "id_turno AS Id, numero_turno AS NumeroTurno, placa AS Placa, tipo_vehiculo AS TipoVehiculo, " +
            "telefono_cliente AS TelefonoCliente, id_servicio AS IdServicio, id_operario AS IdOperario, " +
            "id_bahia AS IdBahia, estado_actual AS EstadoActual, fecha_ingreso AS FechaIngreso, " +
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

        public long Agregar(Turno turno)
        {
            using var conexion = _fabrica.Crear();
            conexion.Open();

            conexion.Execute(
                "INSERT INTO turnos (numero_turno, placa, tipo_vehiculo, telefono_cliente, id_servicio, " +
                "id_operario, id_bahia, estado_actual, fecha_ingreso, hash_consulta) " +
                "VALUES (@NumeroTurno, @Placa, @TipoVehiculo, @TelefonoCliente, @IdServicio, " +
                "@IdOperario, @IdBahia, @EstadoActual, @FechaIngreso, @HashConsulta)",
                new
                {
                    turno.NumeroTurno,
                    turno.Placa,
                    TipoVehiculo = turno.TipoVehiculo.ANombreBd(),
                    turno.TelefonoCliente,
                    turno.IdServicio,
                    turno.IdOperario,
                    turno.IdBahia,
                    EstadoActual = turno.EstadoActual.ANombreBd(),
                    turno.FechaIngreso,
                    turno.HashConsulta
                });

            return conexion.ExecuteScalar<long>("SELECT LAST_INSERT_ID()");
        }
    }
}
