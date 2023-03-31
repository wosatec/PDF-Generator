using PDF_POC.Models.Template;
using System.Collections.Generic;

namespace PDF_POC.Models.Generator
{
    internal class ElementTableRow
    {
        public float Height { get; set; }

        public TextFormat Format { get; set; } = new TextFormat();

        public ICollection<object> Values { get; set; } = new List<object>();

        public IDictionary<string, string> ExtraRows { get; set; } = new Dictionary<string, string>();
    }
}
