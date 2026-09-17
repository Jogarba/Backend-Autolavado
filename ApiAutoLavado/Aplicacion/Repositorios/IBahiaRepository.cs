using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Repositorios
{
    public interface IBahiaRepository
    {
        IReadOnlyCollection<Bahia> ObtenerTodas();

        Bahia? ObtenerPorId(int id);

        int? IntentarAgregar(Bahia bahia);

        bool Actualizar(Bahia bahia);

        bool CambiarEstado(int id, EstadoBahia estado);

        bool IntentarOcupar(int id, ITransaccionBd? transaccion = null);

        bool Liberar(int id, ITransaccionBd? transaccion = null);
    }
}
