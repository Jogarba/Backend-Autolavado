using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Repositorios
{
    public interface IOperarioRepository
    {
        IReadOnlyCollection<Operario> ObtenerTodos();

        Operario? ObtenerPorId(int id);

        int? IntentarAgregar(Operario operario, ITransaccionBd? transaccion = null);

        bool Actualizar(Operario operario);

        bool Desactivar(int id, ITransaccionBd? transaccion = null);

        bool CambiarEstado(int id, EstadoOperario estado, ITransaccionBd? transaccion = null);

        bool IntentarOcupar(int id, ITransaccionBd? transaccion = null);

        bool Liberar(int id, ITransaccionBd? transaccion = null);
    }
}
