namespace PDF_POC.Models.Template
{
    public enum ExpandMode
    {
        /// <summary>
        /// Describes that the <see cref="Element"/> should be stretched to the page maximum when <see cref="Element.Width"/> is 0.
        /// </summary>
        Full = 1,

        /// <summary>
        /// Describes that the <see cref="ElementTextBlock"/> or <see cref="ElementTextLine"/> should be
        /// stretched to fit its containing text when <see cref="Element.Width"/> is 0.
        /// </summary>
        FitContent = 2,
    }
}
