using System.Text.Json.Serialization;

namespace ApiAutoLavado.Domain.Enums
{
    public enum RolUsuario
    {
        [JsonStringEnumMemberName("ADMINISTRADOR")]
        Administrador,

        [JsonStringEnumMemberName("OPERARIO")]
        Operario
    }
}
