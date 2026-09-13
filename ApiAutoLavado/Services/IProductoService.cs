using ApiAutoLavado.Models;
using ApiAutoLavado.Models.Dtos;

namespace ApiAutoLavado.Services
{
    public interface IProductoService
    {
        IReadOnlyCollection<Producto> ObtenerTodos();

        Producto? ObtenerPorId(int id);

        Producto Crear(CrearProductoRequest request);

        Producto? Actualizar(int id, CrearProductoRequest request);

        bool Eliminar(int id);
    }
}
