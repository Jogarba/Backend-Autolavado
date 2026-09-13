using ApiAutoLavado.Models;
using ApiAutoLavado.Models.Dtos;

namespace ApiAutoLavado.Services
{
    public class ProductoService : IProductoService
    {
        private readonly Dictionary<int, Producto> _productos = new();
        private readonly object _lock = new();
        private int _ultimoId;

        public IReadOnlyCollection<Producto> ObtenerTodos()
        {
            lock (_lock)
            {
                return _productos.Values.OrderBy(p => p.Id).ToList();
            }
        }

        public Producto? ObtenerPorId(int id)
        {
            lock (_lock)
            {
                return _productos.TryGetValue(id, out var producto) ? producto : null;
            }
        }

        public Producto Crear(CrearProductoRequest request)
        {
            lock (_lock)
            {
                var producto = new Producto
                {
                    Id = ++_ultimoId,
                    Nombre = request.Nombre.Trim(),
                    Precio = request.Precio
                };

                _productos.Add(producto.Id, producto);
                return producto;
            }
        }

        public Producto? Actualizar(int id, CrearProductoRequest request)
        {
            lock (_lock)
            {
                if (!_productos.TryGetValue(id, out var producto))
                {
                    return null;
                }

                producto.Nombre = request.Nombre.Trim();
                producto.Precio = request.Precio;
                return producto;
            }
        }

        public bool Eliminar(int id)
        {
            lock (_lock)
            {
                return _productos.Remove(id);
            }
        }
    }
}
