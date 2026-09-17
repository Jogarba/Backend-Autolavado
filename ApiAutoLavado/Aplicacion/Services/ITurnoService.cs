using ApiAutoLavado.Aplicacion.Dtos;

namespace ApiAutoLavado.Aplicacion.Services
{
    public interface ITurnoService
    {
        IReadOnlyCollection<TurnoResponse> ObtenerActivos();

        TableroTurnosResponse ObtenerTablero();

        IReadOnlyCollection<TurnoDisplayResponse> ObtenerDisplay();

        TurnoCreadoResponse Crear(CrearTurnoRequest request);

        TurnoDetalleResponse ObtenerTurnoAsignado(int usuarioId);

        IReadOnlyCollection<TurnoDetalleResponse> ObtenerHistorialOperario(int usuarioId);

        TurnoResponse AsignarBahia(long idTurno, int idBahia, int usuarioId, bool esAdministrador);

        TurnoResponse ActualizarFase(long id, string nuevaFase, int usuarioId, bool esAdministrador);

        TurnoResponse Finalizar(long id);

        TurnoResponse Cancelar(long id);

        TrazabilidadTurnoResponse ObtenerTrazabilidad(string identificador);
    }
}
