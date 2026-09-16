using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    public static class MapeoExtensiones
    {

        public static OperarioResponse ToResponse(this Operario operario) => new()
        {
            Id = operario.Id,
            Nombres = operario.Nombres,
            Apellidos = operario.Apellidos,
            Documento = operario.Documento,
            Telefono = operario.Telefono,
            NombreUsuario = operario.NombreUsuario,
            Activo = operario.Activo,
            Estado = operario.Estado,
            FechaCreacion = operario.FechaCreacion
        };

        public static ServicioResponse ToResponse(this Servicio servicio) => new()
        {
            Id = servicio.Id,
            Nombre = servicio.Nombre,
            PrecioBase = servicio.PrecioBase,
            TiempoEstimadoMin = servicio.TiempoEstimadoMin
        };

        public static TurnoResponse ToResponse(this Turno turno) => new()
        {
            Id = turno.Id,
            NumeroTurno = turno.NumeroTurno,
            Placa = turno.Placa,
            IdServicio = turno.IdServicio,
            IdOperario = turno.IdOperario,
            EstadoActual = turno.EstadoActual,
            FechaIngreso = turno.FechaIngreso,
            HashConsulta = turno.HashConsulta
        };

        public static ReservaResponse ToResponse(this Reserva reserva) => new()
        {
            IdReserva = reserva.IdReserva,
            CodigoReserva = reserva.CodigoReserva,
            Placa = reserva.Placa,
            IdServicio = reserva.IdServicio,
            NombreServicio = reserva.NombreServicio ?? string.Empty,
            TarifaEstimada = reserva.TarifaBase ?? 0m,
            TiempoEstimadoMin = reserva.TiempoEstimadoMin ?? 30,
            FechaReserva = reserva.FechaReserva,
            HoraReserva = reserva.HoraReserva,
            Estado = reserva.Estado,
            TipoVehiculo = reserva.TipoVehiculo,
            TelefonoCliente = reserva.TelefonoCliente,
            FechaCreacion = reserva.FechaCreacion,
            TrackingUrl = $"https://autolavadoexpress.com/track/{reserva.Placa}"
        };
    }
}
