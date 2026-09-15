using System.Data;
using MySqlConnector;

namespace ApiAutoLavado.Persistencia
{
    internal interface IFabricaConexion
    {
        IDbConnection Crear();
    }

    internal sealed class FabricaConexionMySql : IFabricaConexion
    {
        private readonly string _cadenaConexion;

        public FabricaConexionMySql(string cadenaConexion)
        {
            _cadenaConexion = cadenaConexion;
        }

        public IDbConnection Crear() => new MySqlConnection(_cadenaConexion);
    }
}
