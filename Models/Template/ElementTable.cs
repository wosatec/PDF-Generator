using System.Collections.Generic;
using System.Linq;

namespace PDF_POC.Models.Template
{
    internal class ElementTable : Element
    {
        public string ListKey { get; set; }

        public string ContentKey { get; set; }

        public string HeaderKey { get; set; }

        public TableRowFormat HeaderFormat { get; set; }

        public TableRowFormat RowFormat { get; set; }

        public TableRowFormat ExtraRowFormat { get; set; }

        public Margin Margin { get; set; } = new Margin();

        public ICollection<ElementTableColumn> Columns { get; set; } = new List<ElementTableColumn>();

        public ICollection<ICollection<ElementTableColumn>> ExtraRows { get; set; } = new List<ICollection<ElementTableColumn>>();

        public IEnumerable<float> WidthList
        {
            get
            {
                return Columns.Select(column => (float)column.Width);
            }
        }

        public float RowHeight
        {
            get
            {
                return Columns.Max(column => column.Content.Height);
            }
        }

        public IEnumerable<float> ExtraRowsWidthList
        {
            get
            {
                return ExtraRows.SelectMany(rows => rows.Select(row => (float)row.Width));
            }
        }
    }
}
