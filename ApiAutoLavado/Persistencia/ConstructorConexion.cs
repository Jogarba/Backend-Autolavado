using System;
using MySqlConnector;

namespace ApiAutoLavado.Persistencia
{
    internal static class ConstructorConexion
    {
        private static readonly string[] EsquemasUrl =
        {
            "mysql://", "mysqls://", "mariadb://", "mariadbs://"
        };

        public static string NormalizarMySql(string cadenaCruda)
        {
            if (string.IsNullOrWhiteSpace(cadenaCruda))
            {
                throw new InvalidOperationException(
                    "La cadena de conexión está vacía. Revise la variable CONNECTION_STRING del entorno o del archivo .env.");
            }

            var cadena = cadenaCruda.Trim();

            // 1. Formato URL tipo mysql://user:pass@host:port/database?ssl-mode=REQUIRED
            foreach (var esquema in EsquemasUrl)
            {
                if (cadena.StartsWith(esquema, StringComparison.OrdinalIgnoreCase))
                {
                    return DesdeUrl(cadena, esquema);
                }
            }

            // 2. Cadena ADO.NET estándar (contiene al menos un '=')
            if (cadena.Contains('='))
            {
                // Si el primer segmento no tiene clave, viene como "host;Port=...;Database=...".
                var primerSegmento = cadena.Split(';')[0];
                return primerSegmento.Contains('=')
                    ? DesdeAdoNet(cadena)
                    : DesdeAdoNet($"Server={cadena}");
            }

            // 3. Solo un host
            return DesdeAdoNet($"Server={cadena}");
        }

        /// <summary>Devuelve host/puerto/base/usuario (sin contraseña) para diagnóstico.</summary>
        public static string DescribirDestino(string cadena)
        {
            try
            {
                var builder = new MySqlConnectionStringBuilder(cadena);
                return $"host={builder.Server}; puerto={builder.Port}; base={builder.Database}; usuario={builder.UserID}; ssl={builder.SslMode}";
            }
            catch
            {
                return "(no se pudo interpretar la cadena de conexión)";
            }
        }

        private static string DesdeUrl(string url, string esquema)
        {
            // Los esquemas terminados en "s" (mysqls://, mariadbs://) implican TLS.
            var tlsImplicito = esquema.StartsWith("mysqls", StringComparison.OrdinalIgnoreCase)
                || esquema.StartsWith("mariadbs", StringComparison.OrdinalIgnoreCase);

            var builder = new MySqlConnectionStringBuilder
            {
                SslMode = tlsImplicito ? MySqlSslMode.Required : MySqlSslMode.Preferred,
                ConnectionTimeout = 30,
                AllowUserVariables = true
            };

            var resto = url.Substring(esquema.Length);

            string? query = null;
            var signoInterrogacion = resto.IndexOf('?');
            if (signoInterrogacion >= 0)
            {
                query = resto.Substring(signoInterrogacion + 1);
                resto = resto.Substring(0, signoInterrogacion);
            }

            string? database = null;
            var diagonal = resto.IndexOf('/');
            if (diagonal >= 0)
            {
                database = Uri.UnescapeDataString(resto.Substring(diagonal + 1));
                resto = resto.Substring(0, diagonal);
            }

            // El separador es el último '@' para tolerar '@' dentro de la contraseña.
            var arroba = resto.LastIndexOf('@');
            if (arroba >= 0)
            {
                var userInfo = resto.Substring(0, arroba);
                resto = resto.Substring(arroba + 1);

                var dosPuntos = userInfo.IndexOf(':');
                if (dosPuntos >= 0)
                {
                    builder.UserID = Uri.UnescapeDataString(userInfo.Substring(0, dosPuntos));
                    builder.Password = Uri.UnescapeDataString(userInfo.Substring(dosPuntos + 1));
                }
                else
                {
                    builder.UserID = Uri.UnescapeDataString(userInfo);
                }
            }

            if (string.IsNullOrWhiteSpace(resto))
            {
                throw new InvalidOperationException(
                    $"Cadena de conexión inválida: no se encontró el servidor en la URL. {DescribirDestino(url)}");
            }

            // host / puerto, con soporte para IPv6 entre corchetes.
            if (resto.StartsWith('['))
            {
                var cierre = resto.IndexOf(']');
                if (cierre < 0)
                {
                    throw new InvalidOperationException($"Cadena de conexión inválida: IPv6 mal formada en la URL.");
                }

                builder.Server = resto.Substring(1, cierre - 1);
                var restoPuerto = resto.Substring(cierre + 1);
                if (restoPuerto.StartsWith(':') && uint.TryParse(restoPuerto.Substring(1), out var puertoIpv6))
                {
                    builder.Port = puertoIpv6;
                }
            }
            else
            {
                var dosPuntos = resto.LastIndexOf(':');
                if (dosPuntos >= 0 && uint.TryParse(resto.Substring(dosPuntos + 1), out var puerto))
                {
                    builder.Server = resto.Substring(0, dosPuntos);
                    builder.Port = puerto;
                }
                else
                {
                    builder.Server = resto;
                }
            }

            if (!string.IsNullOrWhiteSpace(database))
            {
                builder.Database = database;
            }

            AplicarQuery(builder, query);

            // Para servidores locales sin SSL explícito evitamos negociar TLS.
            if (EsLocal(builder.Server) && builder.SslMode == MySqlSslMode.Preferred)
            {
                builder.SslMode = MySqlSslMode.None;
            }

            return builder.ConnectionString;
        }

        private static string DesdeAdoNet(string cadena)
        {
            var builder = new MySqlConnectionStringBuilder(cadena);

            if (builder.ConnectionTimeout == 0)
            {
                builder.ConnectionTimeout = 30;
            }

            if (EsLocal(builder.Server) && builder.SslMode == MySqlSslMode.Preferred)
            {
                builder.SslMode = MySqlSslMode.None;
            }

            builder.AllowUserVariables = true;

            return builder.ConnectionString;
        }

        private static void AplicarQuery(MySqlConnectionStringBuilder builder, string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return;
            }

            foreach (var par in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var separador = par.IndexOf('=');
                if (separador <= 0)
                {
                    continue;
                }

                var clave = Uri.UnescapeDataString(par.Substring(0, separador)).Trim().ToLowerInvariant();
                var valor = Uri.UnescapeDataString(par.Substring(separador + 1)).Trim();

                switch (clave)
                {
                    case "ssl-mode":
                    case "sslmode":
                    case "ssl_mode":
                        builder.SslMode = MapearSsl(valor);
                        break;
                    case "connect-timeout":
                    case "connect_timeout":
                    case "connectiontimeout":
                        if (uint.TryParse(valor, out var segundos))
                        {
                            builder.ConnectionTimeout = segundos;
                        }
                        break;
                    case "allowpublickeyretrieval":
                    case "allow-public-key-retrieval":
                        builder.AllowPublicKeyRetrieval = valor is "1" or "true" or "TRUE" or "True";
                        break;
                }
            }
        }

        private static MySqlSslMode MapearSsl(string valor) => valor.Trim().ToLowerInvariant() switch
        {
            "disabled" or "disable" or "none" or "false" or "0" => MySqlSslMode.None,
            "preferred" => MySqlSslMode.Preferred,
            "required" or "require" or "true" or "1" => MySqlSslMode.Required,
            "verify-ca" or "verifyca" or "verify_ca" => MySqlSslMode.VerifyCA,
            "verify-identity" or "verifyidentity" or "verify-full" or "verifyfull" => MySqlSslMode.VerifyFull,
            _ => MySqlSslMode.Preferred
        };

        private static bool EsLocal(string? servidor)
        {
            if (string.IsNullOrWhiteSpace(servidor))
            {
                return false;
            }

            return servidor.Trim().ToLowerInvariant() is
                "localhost" or "127.0.0.1" or "::1" or "host.docker.internal" or "mysql" or "db";
        }
    }
}
