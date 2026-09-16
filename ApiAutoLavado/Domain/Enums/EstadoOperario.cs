using System.Text.Json.Serialization;

namespace ApiAutoLavado.Domain.Enums
{
    public enum EstadoOperario
    {
        [JsonStringEnumMemberName("DISPONIBLE")]
        Disponible,

        [JsonStringEnumMemberName("OCUPADO")]
        Ocupado,

        [JsonStringEnumMemberName("INACTIVO")]
        Inactivo
    }
}
