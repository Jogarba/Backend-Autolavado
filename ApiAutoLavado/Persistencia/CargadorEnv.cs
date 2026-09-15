namespace ApiAutoLavado.Persistencia
{
    internal static class CargadorEnv
    {
        public static void Cargar(string nombreArchivo = ".env")
        {
            var ruta = Buscar(nombreArchivo);
            if (ruta is null)
            {
                return;
            }

            foreach (var linea in File.ReadAllLines(ruta))
            {
                var texto = linea.Trim();

                if (texto.Length == 0 || texto.StartsWith('#'))
                {
                    continue;
                }

                var separador = texto.IndexOf('=');
                if (separador <= 0)
                {
                    continue;
                }

                var clave = texto[..separador].Trim();
                var valor = texto[(separador + 1)..].Trim().Trim('"');

                if (clave.Length == 0)
                {
                    continue;
                }

                if (Environment.GetEnvironmentVariable(clave) is null)
                {
                    Environment.SetEnvironmentVariable(clave, valor);
                }
            }
        }

        private static string? Buscar(string nombreArchivo)
        {
            var directorio = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (directorio is not null)
            {
                var ruta = Path.Combine(directorio.FullName, nombreArchivo);
                if (File.Exists(ruta))
                {
                    return ruta;
                }

                directorio = directorio.Parent;
            }

            return null;
        }
    }
}
