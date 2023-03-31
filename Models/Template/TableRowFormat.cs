namespace PDF_POC.Models.Template
{
    public class TableRowFormat : TextFormat
    {
        public BorderColors BorderColors { get; set; }

        public float BorderThickness { get; set; }

        public Padding Padding { get; set; } = new Padding();

    }
}
