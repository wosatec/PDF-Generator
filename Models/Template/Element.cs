using PDF_POC.Core.Json;
using System.Text.Json.Serialization;

namespace PDF_POC.Models.Template
{
    [JsonConverter(typeof(ElementJsonConverter))]
    public abstract class Element
    {
        public ElementType Type { get; set; }

        public Position Position { get; set; }

        public float Width { get; set; }

        public float Height { get; set; }
    }
}
