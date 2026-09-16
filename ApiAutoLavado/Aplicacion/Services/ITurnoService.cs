using ApiAutoLavado.Aplicacion.Dtos;

namespace ApiAutoLavado.Aplicacion.Services
{
    public interface ITurnoService
    {
        IReadOnlyCollection<TurnoResponse> ObtenerActivos();

        TurnoCreadoResponse Crear(CrearTurnoRequest request);

        TurnoResponse ActualizarFase(long id, string nuevaFase);

        TurnoResponse Finalizar(long id);

        TurnoResponse Cancelar(long id);

        TrazabilidadTurnoResponse ObtenerTrazabilidad(string identificador);
    }
}
