using System.Text.Json.Serialization;

namespace ApiAutoLavado.Domain.Enums
{
    public enum TipoVehiculo
    {
        [JsonStringEnumMemberName("AUTO")]
        Auto,

        [JsonStringEnumMemberName("MOTO")]
        Moto,

        [JsonStringEnumMemberName("CAMIONETA")]
        Camioneta
    }
}
