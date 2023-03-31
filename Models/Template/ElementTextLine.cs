namespace PDF_POC.Models.Template
{
    public class ElementTextLine : Element
    {
        public string ContentKey { get; set; }

        public bool FirstTokenBold { get; set; }

        public ExpandMode ExpandMode { get; set; } = ExpandMode.Full;

        public TextFormat Format { get; set; } = new TextFormat();

        public bool Truncate { get; set; } = false;
    }
}
