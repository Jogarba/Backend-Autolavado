using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Repositorios
{
    public interface IVehiculoRepository
    {
        Vehiculo? ObtenerPorPlaca(string placa);
        IReadOnlyCollection<Vehiculo> BuscarPorPrefijo(string prefijo);
        void Crear(Vehiculo vehiculo);
        void Actualizar(Vehiculo vehiculo);
    }
}
