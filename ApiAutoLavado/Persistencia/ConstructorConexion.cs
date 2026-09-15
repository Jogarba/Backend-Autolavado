namespace ApiAutoLavado.Persistencia
{
    internal static class ConstructorConexion
    {
        public static string NormalizarMySql(string cadenaCruda)
        {
            if (string.IsNullOrWhiteSpace(cadenaCruda))
            {
                throw new InvalidOperationException(
                    "La cadena de conexión está vacía. Revise la variable CONECTION_STRING del archivo .env.");
            }

            var cadena = cadenaCruda.Trim();

            var tieneServidor =
                cadena.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
                cadena.Contains("Data Source=", StringComparison.OrdinalIgnoreCase);

            if (tieneServidor)
            {
                return cadena;
            }

            // El valor del .env viene como "host;Port=...;Database=...;User=...;Password=...".
            // MySqlConnector necesita el prefijo Server= para interpretarlo.
            return $"Server={cadena}";
        }
    }
}
