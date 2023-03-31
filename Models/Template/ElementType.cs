namespace PDF_POC.Models.Template
{
    public enum ElementType
    {
        /// <summary>
        /// <para>Describes an <see cref="Element"/> of type <see cref="ElementTextLine"/>.</para>
        /// <para>Prints all text tokens next to each other, delimitted by a space.</para>
        /// </summary>
        TextLine = 1,

        /// <summary>
        /// <para>Describes an <see cref="Element"/> of type <see cref="ElementTextBlock"/>.</para>
        /// <para>Prints all text tokens under each other, delimitted by a new line character.</para>
        /// </summary>
        TextBlock = 2,

        /// <summary>
        /// <para>Describes an <see cref="Element"/> of type <see cref="ElementImage"/>.</para>
        /// <para>Displays an image.</para>
        /// </summary>
        Image = 3,

        /// <summary>
        /// <para>Describes an <see cref="Element"/> of type <see cref="ElementTable"/>.</para>
        /// <para>Draws a table.</para>
        /// </summary>
        Table = 4,

        /// <summary>
        /// <para>Describes an <see cref="Element"/> of type <see cref="ElementLine"/>.</para>
        /// <para>Draws a horizontal line.</para>
        /// </summary>
        Line = 5,

        /// <summary>
        /// <para>Describes an <see cref="Element"/> of type <see cref="ElementAreaBreak"/>.</para>
        /// <para>Renders a page break and creates a new page.</para>
        /// </summary>
        AreaBreak = 6,
    }
}
