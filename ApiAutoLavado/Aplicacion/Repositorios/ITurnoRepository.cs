using System;
using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Repositorios
{
    public interface ITurnoRepository
    {
        IReadOnlyCollection<Turno> ObtenerTodos();

        Turno? ObtenerPorId(long id);

        IReadOnlyCollection<Turno> ObtenerPorOperario(int idOperario);

        long Agregar(Turno turno, ITransaccionBd? transaccion = null);

        bool IntentarCambiarEstado(long id, string estadoEsperado, string estadoNuevo, ITransaccionBd? transaccion = null);

        bool AsignarOperario(long idTurno, int idOperario, string estadoNuevo, ITransaccionBd? transaccion = null);

        bool AsignarBahia(long idTurno, int? idBahia, ITransaccionBd? transaccion = null);

        int ObtenerMaximoSecuenciaDelDia(DateOnly fecha);

        int ContarActivosPorFechaYHora(DateOnly fecha, TimeOnly hora, ITransaccionBd? transaccion = null);
    }
}
