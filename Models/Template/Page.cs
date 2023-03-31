using System.Collections.Generic;

namespace PDF_POC.Models.Template
{
    public class Page
    {
        public string ContentKey { get; set; }

        public Margin Margin { get; set; }

        public ICollection<Element> Elements { get; set; } = new List<Element>();
    }
}
