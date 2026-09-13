using ApiAutoLavado.Domain.Models;
using ApiAutoLavado.UI.Dtos;

namespace ApiAutoLavado.LogicaNegocio.Services
{
    public interface ITurnoService
    {
        IReadOnlyCollection<Turno> ObtenerActivos();

        TurnoCreadoResponse Crear(CrearTurnoRequest request);
    }
}
