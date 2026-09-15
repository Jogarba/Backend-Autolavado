namespace ApiAutoLavado.Persistencia
{
    internal static class EnumsBdExtensiones
    {
        public static string ANombreBd<T>(this T valor) where T : struct, Enum
            => valor.ToString().ToUpperInvariant();
    }
}
