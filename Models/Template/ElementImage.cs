namespace PDF_POC.Models.Template
{
    internal class ElementImage : Element
    {
        public string ContentKey { get; set; }

        public float MaxWidth { get; set; } = 0;

        public float MaxHeight { get; set; } = 0;
    }
}
