using System.Text.Json;
using System.Text.Json.Serialization;

namespace KadreeBank.IntegrationTests.Common;

public static class TestJsonOptions
{
    // Debe reflejar la configuración de JsonStringEnumConverter registrada en Program.cs
    // para que el HttpClient de las pruebas pueda (de)serializar los mismos payloads
    // que produce/consume la API real.
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
