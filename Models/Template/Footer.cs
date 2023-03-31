using System.Collections.Generic;

namespace PDF_POC.Models.Template
{
    public class Footer
    {
        public int StartsAtPage { get; set; }

        public ICollection<Element> Elements { get; set; } = new List<Element>();
    }
}
