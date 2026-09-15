using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Repositorios
{
    public interface IBahiaRepository
    {
        IReadOnlyCollection<Bahia> ObtenerTodas();

        Bahia? ObtenerPorId(int id);

        bool IntentarOcupar(int id);

        bool Liberar(int id);
    }
}
