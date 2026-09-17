using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Repositorios
{
    public interface IServicioRepository
    {
        IReadOnlyCollection<Servicio> ObtenerTodos();

        Servicio? ObtenerPorId(int id);

        int Crear(Servicio servicio);

        bool Actualizar(Servicio servicio);
    }
}
