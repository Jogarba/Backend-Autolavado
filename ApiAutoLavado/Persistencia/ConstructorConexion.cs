using System;
using System.Text.RegularExpressions;

namespace ApiAutoLavado.Persistencia
{
    internal static class ConstructorConexion
    {
        public static string NormalizarMySql(string cadenaCruda)
        {
            if (string.IsNullOrWhiteSpace(cadenaCruda))
            {
                throw new InvalidOperationException(
                    "La cadena de conexión está vacía. Revise la variable CONECTION_STRING del entorno o del archivo .env.");
            }

            var cadena = cadenaCruda.Trim();

            // 1. Soporte para formato URL: mysql://user:pass@host:port/database
            if (cadena.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase) ||
                cadena.StartsWith("mysqls://", StringComparison.OrdinalIgnoreCase))
            {
                if (Uri.TryCreate(cadena, UriKind.Absolute, out var uri))
                {
                    var partesUsuario = (uri.UserInfo ?? "").Split(':');
                    var usuario = partesUsuario.Length > 0 ? Uri.UnescapeDataString(partesUsuario[0]) : "";
                    var pass = partesUsuario.Length > 1 ? Uri.UnescapeDataString(partesUsuario[1]) : "";
                    var host = uri.Host;
                    var puerto = uri.Port > 0 ? uri.Port : 3306;
                    var database = uri.AbsolutePath.TrimStart('/');

                    return $"Server={host};Port={puerto};Database={database};User ID={usuario};Password={pass};SslMode=Preferred;Connection Timeout=30;AllowUserVariables=True;";
                }
            }

            // 2. Si ya tiene el formato ADO.NET estándar
            var tieneServidor =
                cadena.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
                cadena.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) ||
                cadena.Contains("Host=", StringComparison.OrdinalIgnoreCase);

            string resultado;
            if (tieneServidor)
            {
                resultado = cadena;
            }
            else
            {
                // El valor viene como "host;Port=...;Database=...;User=...;Password=...".
                resultado = $"Server={cadena}";
            }

            // Añadir SslMode=Preferred y Connection Timeout si no están presentes
            if (!resultado.Contains("SslMode", StringComparison.OrdinalIgnoreCase) &&
                !resultado.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase))
            {
                resultado += ";SslMode=Preferred";
            }

            if (!resultado.Contains("Connection Timeout", StringComparison.OrdinalIgnoreCase) &&
                !resultado.Contains("Connect Timeout", StringComparison.OrdinalIgnoreCase))
            {
                resultado += ";Connection Timeout=30";
            }

            if (!resultado.Contains("AllowUserVariables", StringComparison.OrdinalIgnoreCase))
            {
                resultado += ";AllowUserVariables=True";
            }

            return resultado;
        }
    }
}
