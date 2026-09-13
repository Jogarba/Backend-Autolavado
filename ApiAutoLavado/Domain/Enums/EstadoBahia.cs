using System.Text.Json.Serialization;

namespace ApiAutoLavado.Domain.Enums
{
    public enum EstadoBahia
    {
        [JsonStringEnumMemberName("DISPONIBLE")]
        Disponible,

        [JsonStringEnumMemberName("OCUPADA")]
        Ocupada,

        [JsonStringEnumMemberName("MANTENIMIENTO")]
        Mantenimiento
    }
}
