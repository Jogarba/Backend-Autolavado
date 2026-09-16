using ApiAutoLavado.Aplicacion.Dtos;

namespace ApiAutoLavado.Aplicacion.Services
{
    public interface ITurnoService
    {
        IReadOnlyCollection<TurnoResponse> ObtenerActivos();

        TurnoCreadoResponse Crear(CrearTurnoRequest request);

        TurnoResponse Finalizar(long id);

        TurnoResponse Cancelar(long id);
    }
}
