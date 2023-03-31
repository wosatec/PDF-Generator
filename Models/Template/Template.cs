using System.Collections.Generic;

namespace PDF_POC.Models.Template
{
    public class Template
    {
        public IDictionary<string, IEnumerable<int>> Colors { get; set; } = new Dictionary<string, IEnumerable<int>>();

        public IDictionary<string, string> Fonts { get; set; } = new Dictionary<string, string>();

        public Header Header { get; set; }

        public Footer Footer { get; set; }

        public Draft Draft { get; set; }

        public ICollection<Page> Pages { get; set; } = new List<Page>();
    }
}
