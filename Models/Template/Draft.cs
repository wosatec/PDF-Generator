using System.Collections.Generic;

namespace PDF_POC.Models.Template
{
    public class Draft
    {
        public ICollection<Element> Elements { get; set; } = new List<Element>();
    }
}
