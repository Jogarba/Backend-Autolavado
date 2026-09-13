using System.Text.Json.Serialization;

namespace ApiAutoLavado.Domain.Enums
{
    public enum TipoBahia
    {
        [JsonStringEnumMemberName("GENERAL")]
        General,

        [JsonStringEnumMemberName("DETAILING")]
        Detailing,

        [JsonStringEnumMemberName("SECADO")]
        Secado
    }
}
