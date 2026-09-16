using System.Linq;
using ApiAutoLavado.Aplicacion.Catalogo;
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
            TiempoEstimadoMin = servicio.TiempoEstimadoMin,
            Fases = CatalogoFases.ObtenerSecuencia(servicio.Fases).ToList()
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

        /// <summary>
        /// RNF-05: recorta la información sensible antes de exponerla en canales públicos.
        /// </summary>
        public static TrazabilidadPublicaResponse ToPublica(this TrazabilidadTurnoResponse t) => new()
        {
            IdTurno = t.IdTurno,
            NumeroTurno = t.NumeroTurno,
            Placa = t.Placa,
            TipoVehiculo = t.TipoVehiculo,
            NombreServicio = t.NombreServicio,
            TiempoEstimadoMin = t.TiempoEstimadoMin,
            NombreOperario = PrimerNombre(t.NombreOperario),
            FaseActual = t.FaseActual,
            ProgresoPorcentaje = t.ProgresoPorcentaje,
            MensajeEstado = t.MensajeEstado,
            EstaListoParaRecoger = t.EstaListoParaRecoger,
            FechaIngreso = t.FechaIngreso,
            Fases = t.Fases
        };

        private static string? PrimerNombre(string? nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto) ||
                nombreCompleto.Equals("Por asignar", System.StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return nombreCompleto.Trim().Split(' ')[0];
        }
    }
}
