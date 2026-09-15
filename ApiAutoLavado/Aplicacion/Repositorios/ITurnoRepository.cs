using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Repositorios
{
    public interface ITurnoRepository
    {
        IReadOnlyCollection<Turno> ObtenerTodos();

        long Agregar(Turno turno);
    }
}
