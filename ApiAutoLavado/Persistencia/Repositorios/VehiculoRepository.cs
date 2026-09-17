using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Models;
using Dapper;

namespace ApiAutoLavado.Persistencia.Repositorios
{
    internal sealed class VehiculoRepository : IVehiculoRepository
    {
        private readonly IFabricaConexion _fabricaConexion;

        public VehiculoRepository(IFabricaConexion fabricaConexion)
        {
            _fabricaConexion = fabricaConexion;
        }

        public Vehiculo? ObtenerPorPlaca(string placa, ITransaccionBd? transaccion = null)
        {
            var conexion = transaccion?.Conexion ?? _fabricaConexion.Crear();

            try
            {
                var sql = @"
                    SELECT 
                        placa AS Placa, 
                        tipo_vehiculo AS TipoVehiculo, 
                        telefono_cliente AS TelefonoCliente, 
                        fecha_primer_registro AS FechaPrimerRegistro
                    FROM vehiculos
                    WHERE placa = @Placa";

                var result = conexion.QueryFirstOrDefault(sql, new { Placa = placa }, transaccion?.Transaccion);
                if (result == null) return null;

                return new Vehiculo
                {
                    Placa = result.Placa,
                    TipoVehiculo = Enum.Parse<TipoVehiculo>(result.TipoVehiculo),
                    TelefonoCliente = result.TelefonoCliente,
                    FechaPrimerRegistro = result.FechaPrimerRegistro
                };
            }
            finally
            {
                if (transaccion is null)
                {
                    conexion.Dispose();
                }
            }
        }

        public IReadOnlyCollection<Vehiculo> ObtenerTodos()
        {
            using var conexion = _fabricaConexion.Crear();
            var sql = @"
                SELECT 
                    placa AS Placa, 
                    tipo_vehiculo AS TipoVehiculo, 
                    telefono_cliente AS TelefonoCliente, 
                    fecha_primer_registro AS FechaPrimerRegistro
                FROM vehiculos
                ORDER BY placa ASC";

            var rows = conexion.Query(sql);
            var lista = new List<Vehiculo>();
            foreach (var r in rows)
            {
                lista.Add(new Vehiculo
                {
                    Placa = r.Placa,
                    TipoVehiculo = Enum.Parse<TipoVehiculo>(r.TipoVehiculo),
                    TelefonoCliente = r.TelefonoCliente,
                    FechaPrimerRegistro = r.FechaPrimerRegistro
                });
            }
            return lista;
        }

        public IReadOnlyCollection<Vehiculo> BuscarPorPrefijo(string prefijo)
        {
            using var conexion = _fabricaConexion.Crear();
            var sql = @"
                SELECT 
                    placa AS Placa, 
                    tipo_vehiculo AS TipoVehiculo, 
                    telefono_cliente AS TelefonoCliente, 
                    fecha_primer_registro AS FechaPrimerRegistro
                FROM vehiculos
                WHERE placa LIKE @Prefijo
                ORDER BY placa ASC
                LIMIT 10";

            var rows = conexion.Query(sql, new { Prefijo = $"{prefijo.Trim()}%" });
            var lista = new List<Vehiculo>();
            foreach (var r in rows)
            {
                lista.Add(new Vehiculo
                {
                    Placa = r.Placa,
                    TipoVehiculo = Enum.Parse<TipoVehiculo>(r.TipoVehiculo),
                    TelefonoCliente = r.TelefonoCliente,
                    FechaPrimerRegistro = r.FechaPrimerRegistro
                });
            }
            return lista;
        }

        public void Crear(Vehiculo vehiculo, ITransaccionBd? transaccion = null)
        {
            var conexion = transaccion?.Conexion ?? _fabricaConexion.Crear();

            try
            {
                var sql = @"
                    INSERT INTO vehiculos (placa, tipo_vehiculo, telefono_cliente, fecha_primer_registro) 
                    VALUES (@Placa, @TipoVehiculo, @TelefonoCliente, @FechaPrimerRegistro)";

                conexion.Execute(sql, new
                {
                    vehiculo.Placa,
                    TipoVehiculo = vehiculo.TipoVehiculo.ToString(),
                    vehiculo.TelefonoCliente,
                    vehiculo.FechaPrimerRegistro
                }, transaccion?.Transaccion);
            }
            finally
            {
                if (transaccion is null)
                {
                    conexion.Dispose();
                }
            }
        }

        public void Actualizar(Vehiculo vehiculo, ITransaccionBd? transaccion = null)
        {
            var conexion = transaccion?.Conexion ?? _fabricaConexion.Crear();

            try
            {
                var sql = @"
                    UPDATE vehiculos 
                    SET tipo_vehiculo = @TipoVehiculo,
                        telefono_cliente = @TelefonoCliente
                    WHERE placa = @Placa";

                conexion.Execute(sql, new
                {
                    vehiculo.Placa,
                    TipoVehiculo = vehiculo.TipoVehiculo.ToString(),
                    vehiculo.TelefonoCliente
                }, transaccion?.Transaccion);
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
