using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    public static class MapeoExtensiones
    {
        public static BahiaResponse ToResponse(this Bahia bahia) => new()
        {
            Id = bahia.Id,
            NombreBahia = bahia.NombreBahia,
            Tipo = bahia.Tipo,
            Estado = bahia.Estado
        };

        public static OperarioResponse ToResponse(this Operario operario) => new()
        {
            Id = operario.Id,
            Nombres = operario.Nombres,
            Apellidos = operario.Apellidos,
            Documento = operario.Documento,
            Telefono = operario.Telefono,
            Activo = operario.Activo
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
            TipoVehiculo = turno.TipoVehiculo,
            TelefonoCliente = turno.TelefonoCliente,
            IdServicio = turno.IdServicio,
            IdOperario = turno.IdOperario,
            IdBahia = turno.IdBahia,
            EstadoActual = turno.EstadoActual,
            FechaIngreso = turno.FechaIngreso,
            HashConsulta = turno.HashConsulta
        };
    }
}
