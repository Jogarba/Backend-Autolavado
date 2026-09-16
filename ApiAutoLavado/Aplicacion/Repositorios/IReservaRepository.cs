using System;
using System.Collections.Generic;
using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Repositorios
{
    public interface IReservaRepository
    {
        long Crear(Reserva reserva);
        Reserva? ObtenerPorId(long id);
        Reserva? ObtenerPorCodigo(string codigo);
        IReadOnlyCollection<Reserva> ObtenerPorFecha(DateOnly fecha);
        IReadOnlyCollection<Reserva> ObtenerPorPlaca(string placa);
        IReadOnlyCollection<Reserva> ObtenerTodas();
        int ContarPorFechaYHora(DateOnly fecha, TimeOnly hora);
        bool ActualizarEstado(long idReserva, string nuevoEstado);
    }
}
