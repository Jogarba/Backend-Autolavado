using System.Data;
using ApiAutoLavado.Aplicacion.Repositorios;

namespace ApiAutoLavado.Persistencia.Repositorios
{
    internal sealed class FabricaTransaccionesMySql : IFabricaTransacciones
    {
        private readonly IFabricaConexion _fabrica;

        public FabricaTransaccionesMySql(IFabricaConexion fabrica)
        {
            _fabrica = fabrica;
        }

        public ITransaccionBd Iniciar() => new TransaccionMySql(_fabrica.Crear());
    }

    internal sealed class TransaccionMySql : ITransaccionBd
    {
        private bool _finalizada;

        public TransaccionMySql(IDbConnection conexion)
        {
            Conexion = conexion;
            Conexion.Open();
            Transaccion = Conexion.BeginTransaction();
        }

        public IDbConnection Conexion { get; }

        public IDbTransaction Transaccion { get; }

        public void Confirmar()
        {
            if (_finalizada)
            {
                return;
            }

            Transaccion.Commit();
            _finalizada = true;
        }

        public void Revertir()
        {
            if (_finalizada)
            {
                return;
            }

            Transaccion.Rollback();
            _finalizada = true;
        }

        public void Dispose()
        {
            if (!_finalizada)
            {
                try
                {
                    Transaccion.Rollback();
                }
                catch
                {
                    // La conexión pudo haberse cerrado; se ignora para no ocultar el error original.
                }
            }

            Transaccion.Dispose();
            Conexion.Dispose();
        }
    }
}
