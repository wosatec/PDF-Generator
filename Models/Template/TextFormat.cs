namespace PDF_POC.Models.Template
{
    public class TextFormat
    {
        public float Size { get; set; } = 10;

        public string Font { get; set; } = "default";

        public bool FontBold { get; set; } = false;

        public bool FontItalic { get; set; } = false;

        public string Color { get; set; } = "default";

        public string Background { get; set; } = "default-background";

        public TextAlign TextAlign { get; set; } = TextAlign.Left;
    }
}
