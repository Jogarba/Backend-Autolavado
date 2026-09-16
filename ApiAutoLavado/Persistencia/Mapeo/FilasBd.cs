using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Persistencia.Mapeo
{
    internal sealed class BahiaFila
    {
        public int Id { get; set; }

        public string NombreBahia { get; set; } = string.Empty;

        public string Tipo { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public Bahia AModelo() => new()
        {
            Id = Id,
            NombreBahia = NombreBahia,
            Tipo = Enum.Parse<TipoBahia>(Tipo, ignoreCase: true),
            Estado = Enum.Parse<EstadoBahia>(Estado, ignoreCase: true)
        };
    }

    internal sealed class OperarioFila
    {
        public int Id { get; set; }

        public string Nombres { get; set; } = string.Empty;

        public string Apellidos { get; set; } = string.Empty;

        public string Documento { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public bool Activo { get; set; }

        public string Estado { get; set; } = string.Empty;

        public Operario AModelo() => new()
        {
            Id = Id,
            Nombres = Nombres,
            Apellidos = Apellidos,
            Documento = Documento,
            Telefono = Telefono,
            Activo = Activo,
            Estado = Enum.Parse<EstadoOperario>(Estado, ignoreCase: true)
        };
    }

    internal sealed class ServicioFila
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public decimal PrecioBase { get; set; }

        public int TiempoEstimadoMin { get; set; }

        public Servicio AModelo() => new()
        {
            Id = Id,
            Nombre = Nombre,
            PrecioBase = PrecioBase,
            TiempoEstimadoMin = TiempoEstimadoMin
        };
    }

    internal sealed class TurnoFila
    {
        public long Id { get; set; }

        public string NumeroTurno { get; set; } = string.Empty;

        public string Placa { get; set; } = string.Empty;

        public string TipoVehiculo { get; set; } = string.Empty;

        public string TelefonoCliente { get; set; } = string.Empty;

        public int IdServicio { get; set; }

        public int IdOperario { get; set; }

        public int IdBahia { get; set; }

        public string EstadoActual { get; set; } = string.Empty;

        public DateTime FechaIngreso { get; set; }

        public string HashConsulta { get; set; } = string.Empty;

        public Turno AModelo() => new()
        {
            Id = Id,
            NumeroTurno = NumeroTurno,
            Placa = Placa,
            TipoVehiculo = Enum.Parse<TipoVehiculo>(TipoVehiculo, ignoreCase: true),
            TelefonoCliente = TelefonoCliente,
            IdServicio = IdServicio,
            IdOperario = IdOperario,
            IdBahia = IdBahia,
            EstadoActual = Enum.Parse<EstadoTurno>(EstadoActual, ignoreCase: true),
            FechaIngreso = DateTime.SpecifyKind(FechaIngreso, DateTimeKind.Utc),
            HashConsulta = HashConsulta
        };
    }
}
