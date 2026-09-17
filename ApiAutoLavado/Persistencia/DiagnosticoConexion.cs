using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MySqlConnector;

namespace ApiAutoLavado.Persistencia
{
    /// <summary>
    /// Prueba de conectividad de red (DNS + TCP) que se ejecuta solo cuando falla
    /// la inicialización de la base de datos, para distinguir un problema de red
    /// (IP no permitida, servicio apagado, IPv6) de uno de credenciales.
    /// </summary>
    internal static class DiagnosticoConexion
    {
        public static async Task ProbarAsync(string cadenaConexion)
        {
            MySqlConnectionStringBuilder builder;
            try
            {
                builder = new MySqlConnectionStringBuilder(cadenaConexion);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Diagnóstico] No se pudo interpretar la cadena de conexión: {ex.Message}");
                return;
            }

            var host = builder.Server;
            var puerto = (int)builder.Port;

            Console.WriteLine($"[Diagnóstico] Probando conectividad con '{host}:{puerto}'...");

            IPAddress[] direcciones;
            try
            {
                direcciones = await Dns.GetHostAddressesAsync(host);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Diagnóstico] DNS falló para '{host}': {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine("[Diagnóstico] Revise el nombre del host en la cadena de conexión.");
                return;
            }

            if (direcciones.Length == 0)
            {
                Console.WriteLine($"[Diagnóstico] DNS no devolvió direcciones para '{host}'.");
                return;
            }

            Console.WriteLine($"[Diagnóstico] DNS resolvió a: {string.Join(", ", direcciones.Select(d => d.ToString()))}");

            var exitos = 0;
            foreach (var ip in direcciones)
            {
                try
                {
                    using var cliente = new TcpClient(ip.AddressFamily);
                    var conectar = cliente.ConnectAsync(ip, puerto);
                    var completada = await Task.WhenAny(conectar, Task.Delay(TimeSpan.FromSeconds(5)));

                    if (completada == conectar && cliente.Connected)
                    {
                        exitos++;
                        Console.WriteLine($"[Diagnóstico] TCP OK {ip}:{puerto} (familia {ip.AddressFamily}).");
                    }
                    else
                    {
                        Console.WriteLine($"[Diagnóstico] TCP TIMEOUT {ip}:{puerto} (familia {ip.AddressFamily}).");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Diagnóstico] TCP FALLÓ {ip}:{puerto}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            if (exitos == 0)
            {
                Console.WriteLine(
                    "[Diagnóstico] Ninguna dirección aceptó la conexión. Causas típicas: " +
                    "la IP del servicio no está en la lista de permitidas del proveedor de MySQL, " +
                    "el servicio de base de datos está apagado, o el host resuelve solo a IPv6.");
            }
        }
    }
}
