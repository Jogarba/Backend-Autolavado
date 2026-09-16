using System;
using System.Collections.Generic;
using ApiAutoLavado.Aplicacion.Dtos;

namespace ApiAutoLavado.Aplicacion.Services
{
    public interface IReservaService
    {
        DisponibilidadFechaResponse ConsultarDisponibilidad(DateOnly fecha);
        ReservaResponse Crear(CrearReservaRequest request);
        ReservaResponse ObtenerPorCodigo(string codigo);
        ReservaResponse ObtenerPorId(long id);
        IReadOnlyCollection<ReservaResponse> ObtenerPorFecha(DateOnly fecha);
        IReadOnlyCollection<ReservaResponse> ObtenerPorPlaca(string placa);
        IReadOnlyCollection<ReservaResponse> ObtenerTodas();
        ReservaResponse Cancelar(long id);
        TurnoCreadoResponse ConvertirEnTurno(long idReserva);
    }
}
