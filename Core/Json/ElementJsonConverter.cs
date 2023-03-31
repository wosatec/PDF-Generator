using PDF_POC.Models.Template;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PDF_POC.Core.Json
{
    public class ElementJsonConverter : JsonConverter<Element>
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeof(Element).IsAssignableFrom(typeToConvert);

        public override Element Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument jsonDoc = JsonDocument.ParseValue(ref reader);

            ElementType typeDiscriminator = (ElementType)jsonDoc.RootElement.GetProperty("type").GetInt32();

            Element element = typeDiscriminator switch
            {
                ElementType.Image => jsonDoc.RootElement.Deserialize<ElementImage>(options)!,
                ElementType.TextLine => jsonDoc.RootElement.Deserialize<ElementTextLine>(options)!,
                ElementType.TextBlock => jsonDoc.RootElement.Deserialize<ElementTextBlock>(options)!,
                ElementType.Table => jsonDoc.RootElement.Deserialize<ElementTable>(options)!,
                ElementType.Line => jsonDoc.RootElement.Deserialize<ElementLine>(options)!,
                ElementType.AreaBreak => jsonDoc.RootElement.Deserialize<ElementAreaBreak>(options)!,
                _ => throw new JsonException(),
            };

            return element;
        }

        public override void Write(Utf8JsonWriter writer, Element person, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, person, options);
        }
    }
}
