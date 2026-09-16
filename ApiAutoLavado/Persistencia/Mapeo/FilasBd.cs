using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Persistencia.Mapeo
{

    internal sealed class OperarioFila
    {
        public int Id { get; set; }

        public string Nombres { get; set; } = string.Empty;

        public string Apellidos { get; set; } = string.Empty;

        public string Documento { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public int? UsuarioId { get; set; }

        public string? NombreUsuario { get; set; }

        public bool Activo { get; set; }

        public string Estado { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; }

        public Operario AModelo() => new()
        {
            Id = Id,
            Nombres = Nombres,
            Apellidos = Apellidos,
            Documento = Documento,
            Telefono = Telefono,
            UsuarioId = UsuarioId,
            NombreUsuario = NombreUsuario,
            Activo = Activo,
            Estado = Enum.Parse<EstadoOperario>(Estado, ignoreCase: true),
            FechaCreacion = FechaCreacion
        };
    }

    internal sealed class UsuarioFila
    {
        public int Id { get; set; }

        public string NombreUsuario { get; set; } = string.Empty;

        public string ContrasenaHash { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }

        public Usuario AModelo() => new()
        {
            Id = Id,
            NombreUsuario = NombreUsuario,
            ContrasenaHash = ContrasenaHash,
            Rol = Enum.Parse<RolUsuario>(Rol, ignoreCase: true),
            Activo = Activo,
            FechaCreacion = FechaCreacion
        };
    }

    internal sealed class ServicioFila
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public decimal PrecioBase { get; set; }

        public int TiempoEstimadoMin { get; set; }

        public string? Fases { get; set; }

        public Servicio AModelo() => new()
        {
            Id = Id,
            Nombre = Nombre,
            PrecioBase = PrecioBase,
            TiempoEstimadoMin = TiempoEstimadoMin,
            Fases = Fases
        };
    }

    internal sealed class TurnoFila
    {
        public long Id { get; set; }

        public string NumeroTurno { get; set; } = string.Empty;

        public string Placa { get; set; } = string.Empty;

        public int IdServicio { get; set; }

        public int? IdOperario { get; set; }

        public string EstadoActual { get; set; } = string.Empty;

        public DateTime FechaIngreso { get; set; }

        public string HashConsulta { get; set; } = string.Empty;

        public Turno AModelo() => new()
        {
            Id = Id,
            NumeroTurno = NumeroTurno,
            Placa = Placa,
            IdServicio = IdServicio,
            IdOperario = IdOperario,
            EstadoActual = EstadoActual,
            FechaIngreso = DateTime.SpecifyKind(FechaIngreso, DateTimeKind.Utc),
            HashConsulta = HashConsulta
        };
    }
}
