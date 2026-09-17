using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Repositorios
{
    public interface IVehiculoRepository
    {
        Vehiculo? ObtenerPorPlaca(string placa, ITransaccionBd? transaccion = null);
        IReadOnlyCollection<Vehiculo> ObtenerTodos();
        IReadOnlyCollection<Vehiculo> BuscarPorPrefijo(string prefijo);
        void Crear(Vehiculo vehiculo, ITransaccionBd? transaccion = null);
        void Actualizar(Vehiculo vehiculo, ITransaccionBd? transaccion = null);
    }
}
