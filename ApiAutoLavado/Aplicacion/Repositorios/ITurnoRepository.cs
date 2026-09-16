using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Repositorios
{
    public interface ITurnoRepository
    {
        IReadOnlyCollection<Turno> ObtenerTodos();

        Turno? ObtenerPorId(long id);

        long Agregar(Turno turno);

        bool IntentarCambiarEstado(long id, EstadoTurno estadoEsperado, EstadoTurno estadoNuevo);
    }
}
