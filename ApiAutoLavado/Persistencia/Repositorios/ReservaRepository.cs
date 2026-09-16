using System;
using System.Collections.Generic;
using System.Linq;
using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Models;
using Dapper;

namespace ApiAutoLavado.Persistencia.Repositorios
{
    internal sealed class ReservaRepository : IReservaRepository
    {
        private readonly IFabricaConexion _fabricaConexion;

        public ReservaRepository(IFabricaConexion fabricaConexion)
        {
            _fabricaConexion = fabricaConexion;
        }

        public long Crear(Reserva reserva, ITransaccionBd? transaccion = null)
        {
            var conexion = transaccion?.Conexion ?? _fabricaConexion.Crear();

            try
            {
                var sql = @"
                    INSERT INTO reservas (
                        codigo_reserva, 
                        placa, 
                        id_servicio, 
                        fecha_reserva, 
                        hora_reserva, 
                        estado, 
                        fecha_creacion
                    ) 
                    VALUES (
                        @CodigoReserva, 
                        @Placa, 
                        @IdServicio, 
                        @FechaReserva, 
                        @HoraReserva, 
                        @Estado, 
                        @FechaCreacion
                    );
                    SELECT LAST_INSERT_ID();";

                return conexion.ExecuteScalar<long>(sql, new
                {
                    reserva.CodigoReserva,
                    reserva.Placa,
                    reserva.IdServicio,
                    FechaReserva = reserva.FechaReserva.ToDateTime(TimeOnly.MinValue),
                    HoraReserva = reserva.HoraReserva.ToTimeSpan(),
                    reserva.Estado,
                    reserva.FechaCreacion
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

        public Reserva? ObtenerPorId(long id)
        {
            using var conexion = _fabricaConexion.Crear();
            var sql = @"
                SELECT 
                    r.id_reserva AS IdReserva,
                    r.codigo_reserva AS CodigoReserva,
                    r.placa AS Placa,
                    r.id_servicio AS IdServicio,
                    r.fecha_reserva AS FechaReservaRaw,
                    r.hora_reserva AS HoraReservaRaw,
                    r.estado AS Estado,
                    r.fecha_creacion AS FechaCreacion,
                    s.nombre AS NombreServicio,
                    s.tarifa_base AS TarifaBase,
                    s.tiempo_estimado_min AS TiempoEstimadoMin,
                    v.tipo_vehiculo AS TipoVehiculo,
                    v.telefono_cliente AS TelefonoCliente
                FROM reservas r
                INNER JOIN servicios s ON r.id_servicio = s.id_servicio
                LEFT JOIN vehiculos v ON r.placa = v.placa
                WHERE r.id_reserva = @Id";

            var row = conexion.QueryFirstOrDefault(sql, new { Id = id });
            return MapReserva(row);
        }

        public Reserva? ObtenerPorCodigo(string codigo, ITransaccionBd? transaccion = null)
        {
            var conexion = transaccion?.Conexion ?? _fabricaConexion.Crear();
            try
            {
                var sql = @"
                    SELECT 
                        r.id_reserva AS IdReserva,
                        r.codigo_reserva AS CodigoReserva,
                        r.placa AS Placa,
                        r.id_servicio AS IdServicio,
                        r.fecha_reserva AS FechaReservaRaw,
                        r.hora_reserva AS HoraReservaRaw,
                        r.estado AS Estado,
                        r.fecha_creacion AS FechaCreacion,
                        s.nombre AS NombreServicio,
                        s.tarifa_base AS TarifaBase,
                        s.tiempo_estimado_min AS TiempoEstimadoMin,
                        v.tipo_vehiculo AS TipoVehiculo,
                        v.telefono_cliente AS TelefonoCliente
                    FROM reservas r
                    INNER JOIN servicios s ON r.id_servicio = s.id_servicio
                    LEFT JOIN vehiculos v ON r.placa = v.placa
                    WHERE r.codigo_reserva = @Codigo";

                var row = conexion.QueryFirstOrDefault(sql, new { Codigo = codigo.Trim().ToUpperInvariant() }, transaccion?.Transaccion);
                return MapReserva(row);
            }
            finally
            {
                if (transaccion is null)
                {
                    conexion.Dispose();
                }
            }
        }

        public IReadOnlyCollection<Reserva> ObtenerPorFecha(DateOnly fecha)
        {
            using var conexion = _fabricaConexion.Crear();
            var sql = @"
                SELECT 
                    r.id_reserva AS IdReserva,
                    r.codigo_reserva AS CodigoReserva,
                    r.placa AS Placa,
                    r.id_servicio AS IdServicio,
                    r.fecha_reserva AS FechaReservaRaw,
                    r.hora_reserva AS HoraReservaRaw,
                    r.estado AS Estado,
                    r.fecha_creacion AS FechaCreacion,
                    s.nombre AS NombreServicio,
                    s.tarifa_base AS TarifaBase,
                    s.tiempo_estimado_min AS TiempoEstimadoMin,
                    v.tipo_vehiculo AS TipoVehiculo,
                    v.telefono_cliente AS TelefonoCliente
                FROM reservas r
                INNER JOIN servicios s ON r.id_servicio = s.id_servicio
                LEFT JOIN vehiculos v ON r.placa = v.placa
                WHERE r.fecha_reserva = @Fecha
                ORDER BY r.hora_reserva ASC";

            var rows = conexion.Query(sql, new { Fecha = fecha.ToDateTime(TimeOnly.MinValue) });
            return rows.Select(MapReserva).Where(r => r != null).Cast<Reserva>().ToList();
        }

        public IReadOnlyCollection<Reserva> ObtenerPorPlaca(string placa)
        {
            using var conexion = _fabricaConexion.Crear();
            var sql = @"
                SELECT 
                    r.id_reserva AS IdReserva,
                    r.codigo_reserva AS CodigoReserva,
                    r.placa AS Placa,
                    r.id_servicio AS IdServicio,
                    r.fecha_reserva AS FechaReservaRaw,
                    r.hora_reserva AS HoraReservaRaw,
                    r.estado AS Estado,
                    r.fecha_creacion AS FechaCreacion,
                    s.nombre AS NombreServicio,
                    s.tarifa_base AS TarifaBase,
                    s.tiempo_estimado_min AS TiempoEstimadoMin,
                    v.tipo_vehiculo AS TipoVehiculo,
                    v.telefono_cliente AS TelefonoCliente
                FROM reservas r
                INNER JOIN servicios s ON r.id_servicio = s.id_servicio
                LEFT JOIN vehiculos v ON r.placa = v.placa
                WHERE r.placa = @Placa
                ORDER BY r.fecha_reserva DESC, r.hora_reserva DESC";

            var rows = conexion.Query(sql, new { Placa = placa.Trim().ToUpperInvariant() });
            return rows.Select(MapReserva).Where(r => r != null).Cast<Reserva>().ToList();
        }

        public IReadOnlyCollection<Reserva> ObtenerTodas()
        {
            using var conexion = _fabricaConexion.Crear();
            var sql = @"
                SELECT 
                    r.id_reserva AS IdReserva,
                    r.codigo_reserva AS CodigoReserva,
                    r.placa AS Placa,
                    r.id_servicio AS IdServicio,
                    r.fecha_reserva AS FechaReservaRaw,
                    r.hora_reserva AS HoraReservaRaw,
                    r.estado AS Estado,
                    r.fecha_creacion AS FechaCreacion,
                    s.nombre AS NombreServicio,
                    s.tarifa_base AS TarifaBase,
                    s.tiempo_estimado_min AS TiempoEstimadoMin,
                    v.tipo_vehiculo AS TipoVehiculo,
                    v.telefono_cliente AS TelefonoCliente
                FROM reservas r
                INNER JOIN servicios s ON r.id_servicio = s.id_servicio
                LEFT JOIN vehiculos v ON r.placa = v.placa
                ORDER BY r.fecha_reserva DESC, r.hora_reserva DESC";

            var rows = conexion.Query(sql);
            return rows.Select(MapReserva).Where(r => r != null).Cast<Reserva>().ToList();
        }

        public int ContarPorFechaYHora(DateOnly fecha, TimeOnly hora, ITransaccionBd? transaccion = null)
        {
            var conexion = transaccion?.Conexion ?? _fabricaConexion.Crear();

            try
            {
                var sql = @"
                    SELECT COUNT(*) 
                    FROM reservas 
                    WHERE fecha_reserva = @Fecha 
                      AND hora_reserva = @Hora 
                      AND estado != 'CANCELADA'";

                return conexion.ExecuteScalar<int>(sql, new
                {
                    Fecha = fecha.ToDateTime(TimeOnly.MinValue),
                    Hora = hora.ToTimeSpan()
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

        public bool ActualizarEstado(long idReserva, string nuevoEstado)
        {
            using var conexion = _fabricaConexion.Crear();
            var sql = @"
                UPDATE reservas 
                SET estado = @NuevoEstado 
                WHERE id_reserva = @Id";

            return conexion.Execute(sql, new { Id = idReserva, NuevoEstado = nuevoEstado }) > 0;
        }

        private static Reserva? MapReserva(dynamic? row)
        {
            if (row == null) return null;

            DateOnly fecha = DateOnly.FromDateTime(DateTime.Today);
            if (row.FechaReservaRaw is DateTime dt)
            {
                fecha = DateOnly.FromDateTime(dt);
            }
            else if (row.FechaReservaRaw is DateOnly d)
            {
                fecha = d;
            }
            else if (row.FechaReservaRaw != null)
            {
                string strFecha = row.FechaReservaRaw.ToString();
                if (DateTime.TryParse(strFecha, out DateTime dtParsed))
                    fecha = DateOnly.FromDateTime(dtParsed);
                else if (DateOnly.TryParse(strFecha, out DateOnly dParsed))
                    fecha = dParsed;
            }

            TimeOnly hora = TimeOnly.MinValue;
            if (row.HoraReservaRaw is TimeSpan ts)
            {
                hora = TimeOnly.FromTimeSpan(ts);
            }
            else if (row.HoraReservaRaw is TimeOnly t)
            {
                hora = t;
            }
            else if (row.HoraReservaRaw is DateTime dtH)
            {
                hora = TimeOnly.FromDateTime(dtH);
            }
            else if (row.HoraReservaRaw != null)
            {
                string strHora = row.HoraReservaRaw.ToString();
                if (TimeSpan.TryParse(strHora, out TimeSpan tsParsed))
                    hora = TimeOnly.FromTimeSpan(tsParsed);
                else if (TimeOnly.TryParse(strHora, out TimeOnly tParsed))
                    hora = tParsed;
            }

            return new Reserva
            {
                IdReserva = (long)row.IdReserva,
                CodigoReserva = row.CodigoReserva,
                Placa = row.Placa,
                IdServicio = (int)row.IdServicio,
                FechaReserva = fecha,
                HoraReserva = hora,
                Estado = row.Estado,
                FechaCreacion = (DateTime)row.FechaCreacion,
                NombreServicio = row.NombreServicio,
                TarifaBase = (decimal?)row.TarifaBase,
                TiempoEstimadoMin = (int?)row.TiempoEstimadoMin,
                TipoVehiculo = row.TipoVehiculo,
                TelefonoCliente = row.TelefonoCliente
            };
        }
    }
}
