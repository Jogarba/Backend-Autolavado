using System.Data;

namespace ApiAutoLavado.Aplicacion.Repositorios
{
    public interface ITransaccionBd : IDisposable
    {
        IDbConnection Conexion { get; }

        IDbTransaction Transaccion { get; }

        void Confirmar();

        void Revertir();
    }
}
