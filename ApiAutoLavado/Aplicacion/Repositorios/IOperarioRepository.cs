using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Repositorios
{
    public interface IOperarioRepository
    {
        IReadOnlyCollection<Operario> ObtenerTodos();

        Operario? ObtenerPorId(int id);

        int? IntentarAgregar(Operario operario);
    }
}
