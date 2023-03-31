using System;

namespace PDF_POC.Models.Data
{
    internal class DataDocumentContent
    {
        public Guid Id { get; set; }

        public string Base64Small { get; set; }

        public string Base64Medium { get; set; }
    }
}
