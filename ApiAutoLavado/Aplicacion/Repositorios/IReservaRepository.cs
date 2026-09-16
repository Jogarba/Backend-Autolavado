using System;
using System.Collections.Generic;
using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Repositorios
{
    public interface IReservaRepository
    {
        long Crear(Reserva reserva, ITransaccionBd? transaccion = null);
        Reserva? ObtenerPorId(long id);
        Reserva? ObtenerPorCodigo(string codigo, ITransaccionBd? transaccion = null);
        IReadOnlyCollection<Reserva> ObtenerPorFecha(DateOnly fecha);
        IReadOnlyCollection<Reserva> ObtenerPorPlaca(string placa);
        IReadOnlyCollection<Reserva> ObtenerTodas();
        int ContarPorFechaYHora(DateOnly fecha, TimeOnly hora, ITransaccionBd? transaccion = null);
        bool ActualizarEstado(long idReserva, string nuevoEstado);
    }
}
