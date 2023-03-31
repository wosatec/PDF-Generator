using iText.IO.Font;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using PDF_POC.Core.Json;
using PDF_POC.Models.Data;
using PDF_POC.Models.Generator;
using PDF_POC.Models.Template;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PDF_POC.Core
{
    public static class Worker
    {
        private static readonly float _pageHeightPoints = PageSize.A4.GetHeight();
        private static readonly float _pageWidthPoints = PageSize.A4.GetWidth();

        private static readonly float _pageHeighMillimeters = 297;
        private static readonly float _pageWidthMillimeters = 210;

        private static readonly float _factor = _pageWidthPoints / _pageWidthMillimeters;

        private static readonly IDictionary<string, PdfFont> _fonts = new Dictionary<string, PdfFont>();

        private static Template _template;

        private static JsonAdvancedElement<object> _data;

        private static IEnumerable<DataDocumentContent> _documentContents;

        private static readonly string _fallbackPlaceholderSPath = "./Resources/S_placeholder.png";
        private static readonly string _fallbackPlaceholderMPath = "./Resources/M_placeholder.png";

        public static void DoWork(string templatePath, IEnumerable<string> pagesPaths, string dataPath, string outputPath, bool isDraft)
        {
            // 1. Load Template
            LoadTemplate(templatePath, pagesPaths);

            // 2. SetFont
            LoadFonts();

            // 4. Load Data
            LoadData(dataPath);

            // 5. Create PDF
            CreatePdf(outputPath, isDraft);
        }

        private static void LoadTemplate(string templatePath, IEnumerable<string> pagesPaths)
        {
            _template = LoadTemplate(templatePath);

            foreach (string pagePath in pagesPaths)
            {
                Page page = LoadPage(pagePath);

                _template.Pages.Add(page);
            }
        }

        private static Template LoadTemplate(string templatePath)
        {
            string json = ReadTextFile(templatePath);

            return new JsonAdvancedElement<Template>(json).Value;
        }

        private static Page LoadPage(string templatePath)
        {
            string json = ReadTextFile(templatePath);

            return new JsonAdvancedElement<Page>(json).Value;
        }

        private static void LoadFonts()
        {
            foreach (KeyValuePair<string, string> font in _template.Fonts)
            {
                FontProgram fontProgram = FontProgramFactory.CreateFont(font.Value);
                PdfFont pdfFont = PdfFontFactory.CreateFont(fontProgram, PdfEncodings.IDENTITY_H);

                _fonts.Add(font.Key, pdfFont);
            }
        }

        private static (PdfFont Font, PdfFont IconFont) GetFonts(string fontKey = null, string iconFontKey = null)
        {
            const string defaultFontKey = "default";
            const string defaultIconFontKey = "default-icon";

            if (string.IsNullOrWhiteSpace(fontKey))
            {
                fontKey = defaultFontKey;
            }

            if (string.IsNullOrWhiteSpace(iconFontKey))
            {
                iconFontKey = defaultIconFontKey;
            }

            if (!_fonts.ContainsKey(fontKey))
            {
                fontKey = defaultFontKey;
            }

            if (!_template.Fonts.ContainsKey(defaultIconFontKey))
            {
                iconFontKey = defaultIconFontKey;
            }

            return (_fonts[fontKey], _fonts[iconFontKey]);
        }

        private static void CreatePdf(string outputPath, bool isDraft)
        {
            //Create pdfWriter
            PdfWriter pdfWriter = new(outputPath);
            PdfDocument pdfDocument = new(pdfWriter);
            Document document = new(pdfDocument, PageSize.A4, false);
            _ = pdfDocument.AddNewPage();

            foreach (Page page in _template.Pages)
            {
                AddElements(document, page);
            }

            if (isDraft)
            {
                CreateDraft(document);
            }

            if (_template.Header != null && _template.Header.Elements?.Any() == true)
            {
                CreateHeader(document, _template.Header.StartsAtPage);
            }

            if (_template.Footer != null && _template.Footer.Elements?.Any() == true)
            {
                CreateFooter(document, _template.Footer.StartsAtPage);
            }

            document.Flush();

            pdfDocument.RemovePage(pdfDocument.GetNumberOfPages());

            //Close Document
            pdfDocument.Close();

#if DEBUG
            string strCmdText = "/C \"" + outputPath + "\"";
            System.Diagnostics.Process.Start("CMD.exe", strCmdText);
#endif
        }

        private static void AddElements(Document document, Page page)
        {
            ICollection<Element> elements = page.Elements;

            IEnumerable<JsonAdvancedElement<object>> pageDataSets = _data.FindNodeArray<object>(page.ContentKey);

            _documentContents = _data.FindDataArray<DataDocumentContent>("documentContents");

            document.SetMargins(
                MillimetersToPoints(page.Margin.Top),
                MillimetersToPoints(page.Margin.Right),
                MillimetersToPoints(page.Margin.Bottom),
                MillimetersToPoints(page.Margin.Left));

            foreach (JsonAdvancedElement<object> pageDataSet in pageDataSets)
            {
                foreach (Element element in elements)
                {
                    try
                    {
                        CreateBlockElementFromElement(document, pageDataSet, element);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex.Message);
                    }
                }

                document.Add(new AreaBreak());
            }
        }

        private static void CreateBlockElementFromElement(Document document, JsonAdvancedElement<object> pageDataSet, Element element, int pageNumber = 0)
        {
            if (pageNumber == 0)
            {
                pageNumber = document.GetPdfDocument().GetNumberOfPages();
            }

            if (element is ElementTextLine elementTextLine)
            {
                document.Add(CreateParagraph(elementTextLine, document, pageDataSet, pageNumber));
            }

            if (element is ElementTextBlock elementTextBlock)
            {
                document.Add(CreateParagraph(elementTextBlock, document, pageDataSet, pageNumber));
            }

            if (element is ElementImage elementImage)
            {
                ImageData image = CreateImage(elementImage, pageDataSet);

                if (image != null)
                {
                    DrawImageOnPage(document, elementImage, image, pageNumber);
                }
            }

            if (element is ElementTable elementTable)
            {
                if (!string.IsNullOrWhiteSpace(elementTable.ListKey))
                {
                    IEnumerable<JsonAdvancedElement<object>> contentList = pageDataSet.FindNodeArray<object>(elementTable.ListKey);

                    if (contentList.Count() > 1)
                    {
                        Table outerTable = new Table(UnitValue.CreatePercentArray(1))
                            .SetBorder(Border.NO_BORDER)
                            .SetWidth(GetWidth(element, document))
                            .SetBorderCollapse(BorderCollapsePropertyValue.SEPARATE)
                            .SetVerticalBorderSpacing(MillimetersToPoints(3)); //TODO magic number

                        outerTable.SetMarginTop(MillimetersToPoints(elementTable.Margin.Top));

                        outerTable.SetMarginBottom(MillimetersToPoints(elementTable.Margin.Bottom));

                        outerTable.AddHeaderCell(
                            new Cell()
                                .SetHeight(MillimetersToPoints(elementTable.Position.Top))
                                .SetBorder(Border.NO_BORDER));

                        foreach (JsonAdvancedElement<object> contentNode in contentList)
                        {
                            Table table = CreateTable(elementTable, document, contentNode, false);

                            outerTable.AddCell(
                                new Cell()
                                    .Add(table)
                                    .SetBorder(Border.NO_BORDER));
                        }

                        document.Add(outerTable.SetPageNumber(pageNumber));
                    }
                }
                else
                {
                    document.Add(CreateTable(elementTable, document, pageDataSet));
                }
            }

            if (element is ElementLine elementLine)
            {
                CreateLine(elementLine, document, pageNumber);
            }

            if (element is ElementAreaBreak)
            {
                document.Add(new AreaBreak());
            }
        }

        private static void DrawImageOnPage(Document document, ElementImage elementImage, ImageData image, int pageNumber = 0)
        {
            if (pageNumber == 0)
            {
                pageNumber = document.GetPdfDocument().GetNumberOfPages();
            }

            PdfCanvas canvas = new(document.GetPdfDocument().GetPage(pageNumber));

            float imageWidth = GetImageWidth(elementImage, image);
            float imageHeight = GetImageHeight(elementImage, image);

            (imageWidth, imageHeight) = ApplyMaxSize(elementImage, imageWidth, imageHeight);

            float elementWidth = elementImage.Width;
            float elementHeight = elementImage.Height;

            elementImage.Width = PointsToMillimeters(imageWidth);
            elementImage.Height = PointsToMillimeters(imageHeight);

            Rectangle rect = new(
                GetLeft(elementImage, document),
                GetBottom(elementImage, document),
                imageWidth,
                imageHeight);

            canvas.AddImageFittedIntoRectangle(image, rect, false);

            elementImage.Width = elementWidth;
            elementImage.Height = elementHeight;
        }

        private static (float Width, float Height) ApplyMaxSize(ElementImage elementImage, float imageWidth, float imageHeight)
        {
            float maxWidth = MillimetersToPoints(elementImage.MaxWidth);
            float maxHeight = MillimetersToPoints(elementImage.MaxHeight);

            float ratio;

            if (maxWidth > 0 && imageWidth > maxWidth)
            {
                ratio = maxWidth / imageWidth;

                imageWidth = maxWidth;
                imageHeight *= ratio;
            }

            if (maxHeight > 0 && imageHeight > maxHeight)
            {
                ratio = maxHeight / imageHeight;

                imageHeight = maxHeight;
                imageWidth *= ratio;
            }

            return (imageWidth, imageHeight);
        }

        private static void CreateLine(ElementLine element, Document document, int pageNumber = 0)
        {
            if (pageNumber == 0)
            {
                pageNumber = document.GetPdfDocument().GetNumberOfPages();
            }

            float xFrom = GetLeft(element, document);
            float width = GetWidth(element, document);
            float y = GetBottom(element, document);
            (Color color, _) = GetColor(element.Color);
            float lineWidth = GetHeight(element, document);

            PdfCanvas canvas = new(document.GetPdfDocument().GetPage(pageNumber));

            canvas
                .MoveTo(xFrom, y)
                .SetStrokeColor(color)
                .SetLineWidth(lineWidth)
                .LineTo(xFrom + width, y)
                .ClosePathStroke();
        }

        private static Paragraph CreateParagraph(ElementTextBlock element, Document document, JsonAdvancedElement<object> contentRoot, int pageNumber = 0)
        {
            (Color color, float opacity) = GetColor(element.Format.Color);
            (Color background, float backgroundOpacity) = GetColor(element.Format.Background);

            IEnumerable<string> lines = contentRoot.FindStringArray(element.ContentKey);

            string text = string.Join("\n", lines);

            if (element.Truncate)
            {
                text = Truncate(text, element);
            }

            Paragraph paragraph = new Paragraph(text)
                .SetFont(GetFonts(element.Format.Font).Font)
                .SetFontColor(color, opacity)
                .SetFontSize(element.Format.Size)
                .SetBackgroundColor(background, backgroundOpacity)
                .SetMultipliedLeading(1.25f);

            if (element.Format.FontBold)
            {
                paragraph.SetBold();
            }

            if (element.Format.FontItalic)
            {
                paragraph.SetItalic();
            }

            paragraph.SetTextAlignment(element.Format.TextAlign switch
            {
                TextAlign.Left => TextAlignment.LEFT,
                TextAlign.Center => TextAlignment.CENTER,
                TextAlign.Right => TextAlignment.RIGHT,
                _ => throw new InvalidOperationException("unsupported TextAlign " + element.Format.TextAlign.ToString()),
            });

            float width = GetWidth(element, document);

            if (element.Width == 0
                && element.ExpandMode == ExpandMode.FitContent)
            {
                string entireText = string.Join("\n", lines);

                width = GetFonts(element.Format.Font).Font.GetWidth(entireText, element.Format.Size) + 3;
            }

            paragraph.SetFixedPosition(GetLeft(element, document), GetBottom(element, document), width);

            return paragraph.SetPageNumber(pageNumber);
        }

        private static Paragraph CreateParagraph(ElementTextLine element, Document document, JsonAdvancedElement<object> contentRoot, int pageNumber = 0)
        {
            (Color color, float opacity) = GetColor(element.Format.Color);
            (Color background, float backgroundOpacity) = GetColor(element.Format.Background);

            IEnumerable<string> tokens = contentRoot.FindStringArray(element.ContentKey);

            Paragraph paragraph = new Paragraph()
                .SetFont(GetFonts(element.Format.Font).Font)
                .SetFontColor(color, opacity)
                .SetFontSize(element.Format.Size)
                .SetBackgroundColor(background, backgroundOpacity)
                .SetMultipliedLeading(1.25f);

            Text firstToken = GetTextForToken(tokens.FirstOrDefault());

            if (element.FirstTokenBold)
            {
                firstToken = firstToken.SetBold();
            }

            paragraph.Add(firstToken);

            foreach (string token in tokens.Skip(1))
            {
                paragraph.Add(new Text(" ")).Add(GetTextForToken(token));
            }

            if (element.Format.FontBold)
            {
                paragraph.SetBold();
            }

            if (element.Format.FontItalic)
            {
                paragraph.SetItalic();
            }

            float width = GetWidth(element, document);

            if (element.Width == 0
                && element.ExpandMode == ExpandMode.FitContent)
            {
                string entireText = string.Join(" ", tokens);

                width = GetFonts(element.Format.Font).Font.GetWidth(entireText, element.Format.Size) + 3;
            }

            return paragraph
                .SetFixedPosition(GetLeft(element, document), GetBottom(element, document), width)
                .SetPageNumber(pageNumber == 0 ? document.GetPdfDocument().GetNumberOfPages() : pageNumber);
        }

        private static Text GetTextForToken(string token)
        {
            if (token != null
                && token.Length == 1
                && token[0] > '\uF000')
            {
                return new Text(token).SetFont(GetFonts().IconFont);
            }

            return new Text(token);
        }

        private static SolidBorder GetBorderColor(string key, float borderThickness)
        {
            (Color color, float opacity) = GetColor(key);
            SolidBorder solidBorder = new(color, borderThickness, opacity);

            return solidBorder;
        }

        private static (Color Color, float Opacity) GetColor(string key)
        {
            string colorKey = "default";
            int red = 0;
            int green = 0;
            int blue = 0;
            float opacity = 1.0f;

            if (!string.IsNullOrWhiteSpace(key))
            {
                colorKey = key;
            }

            if (_template.Colors.ContainsKey(colorKey))
            {
                IEnumerable<int> values = _template.Colors[colorKey];

                if (values.Count() >= 3)
                {
                    red = values.ElementAt(0);
                    green = values.ElementAt(1);
                    blue = values.ElementAt(2);
                }

                if (values.Count() == 4)
                {
                    opacity = values.ElementAt(3) / 255f;
                }
            }

            return (new DeviceRgb(red, green, blue), opacity);
        }

        private static ImageData CreateImage(ElementImage element, JsonAdvancedElement<object> contentRoot)
        {
            JsonAdvancedElement<DataImage> imageElement = contentRoot.FindNode<DataImage>(element.ContentKey);

            return CreateImage(imageElement);
        }

        private static ImageData CreateImage(JsonAdvancedElement<DataImage> imageElement)
        {
            DataImage dataImage = imageElement.Value;

            if (dataImage == null)
            {
                return null;
            }

            ImageData image = null;

            if (dataImage.Type?.ToUpperInvariant() == "BASE64"
                && !string.IsNullOrWhiteSpace(dataImage.Content))
            {
                string base64 = dataImage.Content;
                byte[] imageBytes = Convert.FromBase64String(base64.Split(",").Last());

                image = ImageDataFactory.Create(imageBytes);
            }
            else if (dataImage.Type?.ToUpperInvariant() == "ID"
                && !string.IsNullOrWhiteSpace(dataImage.Content)
                && !string.IsNullOrWhiteSpace(dataImage.Size))
            {
                DataDocumentContent document = _documentContents.FirstOrDefault(document => document.Id == new Guid(dataImage.Content));

                if (document != null)
                {
                    string base64 = dataImage.Size.ToUpperInvariant() switch
                    {
                        "SMALL" => document.Base64Small,
                        "MEDIUM" => document.Base64Medium,
                        _ => throw new InvalidOperationException("unsupported image size " + dataImage.Size),
                    };

                    if (string.IsNullOrWhiteSpace(base64))
                    {
                        // Emergency fallback when the document content is broken
                        image = dataImage.Size.ToUpperInvariant() switch
                        {
                            "SMALL" => ImageDataFactory.Create(_fallbackPlaceholderSPath),
                            "MEDIUM" => ImageDataFactory.Create(_fallbackPlaceholderMPath),
                            _ => throw new InvalidOperationException("unsupported image size " + dataImage.Size),
                        };
                    }
                    else
                    {
                        byte[] imageBytes = Convert.FromBase64String(base64.Split(",").Last());

                        image = ImageDataFactory.Create(imageBytes);
                    }
                }
            }
            else if (dataImage.Type?.ToUpperInvariant() == "PATH"
                && !string.IsNullOrWhiteSpace(dataImage.Content))
            {
                image = ImageDataFactory.Create(dataImage.Content);
            }

            return image;
        }

        private static Table CreateTable(ElementTable element, Document document, JsonAdvancedElement<object> contentRoot, bool isOuter = true)
        {
            Table table = new Table(UnitValue.CreatePercentArray(element.WidthList.ToArray()))
                .SetWidth(GetWidth(element, document))
                .SetHorizontalAlignment(HorizontalAlignment.CENTER);

            if (isOuter)
            {
                table.SetMarginTop(MillimetersToPoints(element.Margin.Top));

                table.SetMarginBottom(MillimetersToPoints(element.Margin.Bottom));
            }

            IEnumerable<string> tableHeader = contentRoot.FindStringArray(element.HeaderKey);

            ElementTableRow headerRow = new()
            {
                Format = element.HeaderFormat,
                Values = tableHeader.Select(header => (object)header).ToList(),
            };

            if (isOuter)
            {
                table.AddHeaderCell(
                    new Cell(1, headerRow.Values.Count)
                        .SetHeight(MillimetersToPoints(element.Position.Top))
                        .SetBorder(Border.NO_BORDER));
            }

            CreateTableRow(element, table, headerRow, true, element.WidthList.ToArray());

            IEnumerable<Dictionary<string, JsonElement>> dataForTable = contentRoot.FindDataArray<Dictionary<string, JsonElement>>(element.ContentKey);

            ICollection<ElementTableRow> rows = new List<ElementTableRow>();

            foreach (Dictionary<string, JsonElement> dataForRow in dataForTable)
            {
                ElementTableRow newRow = new()
                {
                    Format = element.RowFormat,
                    Height = element.RowHeight,
                };
                rows.Add(newRow);

                foreach (JsonElement rowValue in dataForRow.Where(kvp => kvp.Key != "extraRows").Select(kvp => kvp.Value))
                {
                    if (rowValue.ValueKind == JsonValueKind.Null)
                    {
                        newRow.Values.Add(null);

                        continue;
                    }

                    if (rowValue.ValueKind == JsonValueKind.String)
                    {
                        newRow.Values.Add(rowValue.GetString());
                    }
                    else if (rowValue.ValueKind == JsonValueKind.Array)
                    {
                        IEnumerable<string> stringValues = rowValue.EnumerateArray().Select(value => value.GetString());

                        newRow.Values.Add(string.Join("\n", stringValues));
                    }
                    else if (rowValue.ValueKind == JsonValueKind.Object)
                    {
                        ImageData image = CreateImage(new JsonAdvancedElement<DataImage>(rowValue));

                        newRow.Values.Add(image != null ? new Image(image) : null);
                    }
                    else
                    {
                        throw new InvalidOperationException($"{rowValue.GetType()} is currently not supported as table value");
                    }
                }

                JsonElement? extraRows = dataForRow.FirstOrDefault(kvp => kvp.Key == "extraRows").Value;

                if (extraRows.HasValue)
                {
                    newRow.ExtraRows = extraRows.Value.Deserialize<Dictionary<string, string>>();
                }
            }

            foreach (ElementTableRow row in rows)
            {
                CreateTableRow(element, table, row, false, element.WidthList.ToArray());
            }

            return table;
        }

        private static void CreateTableRow(ElementTable element, Table table, ElementTableRow row, bool isHeader, float[] widthList)
        {
            ICollection<object> cols = row.Values;

            Table innerTable = new Table(UnitValue.CreatePercentArray(widthList))
                .UseAllAvailableWidth()
                .SetKeepTogether(true);

            foreach (object col in cols)
            {
                if (isHeader)
                {
                    (Color color, float opacity) = GetColor(row.Format.Color);
                    (Color background, float backOpacity) = GetColor(row.Format.Background);

                    table.AddHeaderCell(new Cell()
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .SetBorder(Border.NO_BORDER)
                        .SetBorderTop(GetBorderColor(element.HeaderFormat.BorderColors.Top, element.HeaderFormat.BorderThickness))
                        .SetBorderLeft(GetBorderColor(element.HeaderFormat.BorderColors.Left, element.HeaderFormat.BorderThickness))
                        .SetBorderRight(GetBorderColor(element.HeaderFormat.BorderColors.Right, element.HeaderFormat.BorderThickness))
                        .SetBorderBottom(GetBorderColor(element.HeaderFormat.BorderColors.Bottom, element.HeaderFormat.BorderThickness))
                        .SetBackgroundColor(background, backOpacity)
                        .Add(new Paragraph((string)col)
                            .SetFont(GetFonts(row.Format.Font).Font)
                            .SetFixedLeading(row.Format.Size)
                            .SetFontSize(row.Format.Size)
                            .SetFontColor(color, opacity)));
                }
                else
                {
                    Cell cell = new Cell()
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .SetMaxWidth(element.Width)
                        .SetBorder(Border.NO_BORDER)
                        .SetBorderTop(GetBorderColor(element.RowFormat.BorderColors.Top, element.RowFormat.BorderThickness))
                        .SetBorderLeft(GetBorderColor(element.RowFormat.BorderColors.Left, element.RowFormat.BorderThickness))
                        .SetBorderRight(GetBorderColor(element.RowFormat.BorderColors.Right, element.RowFormat.BorderThickness))
                        .SetBorderBottom(GetBorderColor(element.RowFormat.BorderColors.Bottom, element.RowFormat.BorderThickness))
                        .SetHeight(row.Height)
                        .SetKeepTogether(true)
                        .SetPaddings(
                            MillimetersToPoints(element.RowFormat.Padding.Top),
                            MillimetersToPoints(element.RowFormat.Padding.Right),
                            MillimetersToPoints(element.RowFormat.Padding.Bottom),
                            MillimetersToPoints(element.RowFormat.Padding.Left))
                        .SetKeepWithNext(row.ExtraRows.Any());

                    if (col == null
                        || col is string)
                    {
                        string colString = col as string;

                        (Color color, float opacity) = GetColor(row.Format.Color);
                        (Color background, float backOpacity) = GetColor(row.Format.Background);

                        Paragraph paragraph = new Paragraph(colString ?? string.Empty)
                            .SetFont(GetFonts(row.Format.Font).Font)
                            .SetFontSize(row.Format.Size)
                            .SetFontColor(color, opacity)
                            .SetFixedLeading(row.Format.Size)
                            .SetBackgroundColor(background, backOpacity);

                        cell = cell
                            .Add(paragraph);
                    }
                    else if (col is Image imageCol)
                    {
                        Image image = imageCol
                            .SetAutoScale(true)
                            .SetMaxHeight(row.Height - 4)
                            .SetHorizontalAlignment(HorizontalAlignment.CENTER);

                        cell = cell
                            .Add(image);
                    }

                    innerTable.AddCell(cell);
                }
            }

            foreach (KeyValuePair<string, string> extraRow in row.ExtraRows)
            {
                (Color color, float opacity) = GetColor(element.ExtraRowFormat.Background);
                (Color fontColor, float fontOpacity) = GetColor(element.ExtraRowFormat.Color);

                Paragraph paragraphKey = new Paragraph(extraRow.Key ?? string.Empty)
                    .SetFont(GetFonts().Font)
                    .SetFontSize(row.Format.Size)
                    .SetFixedLeading(row.Format.Size);

                Paragraph paragraphValue = new Paragraph(extraRow.Value ?? string.Empty)
                    .SetFont(GetFonts().Font)
                    .SetFontSize(row.Format.Size)
                    .SetFixedLeading(row.Format.Size);

                Table innerInnerTable = new Table(UnitValue.CreatePercentArray(element.ExtraRowsWidthList.ToArray()))
                     .UseAllAvailableWidth()
                     .SetKeepTogether(true);

                innerInnerTable.AddCell(
                    new Cell()
                        .Add(paragraphKey)
                        .SetKeepWithNext(true)
                        .SetBorder(Border.NO_BORDER)
                        .SetBackgroundColor(color, opacity)
                        .SetFontColor(fontColor, fontOpacity));

                innerInnerTable.AddCell(
                    new Cell()
                        .Add(paragraphValue)
                        .SetKeepWithNext(extraRow.Key != row.ExtraRows.Keys.Last())
                        .SetBorder(Border.NO_BORDER)
                        .SetBackgroundColor(color, opacity)
                        .SetFontColor(fontColor, fontOpacity));

                innerTable.AddCell(new Cell(1, cols.Count)
                    .Add(innerInnerTable)
                    .SetBorder(Border.NO_BORDER)
                    .SetPadding(0));
            }

            table.AddCell(new Cell(1, cols.Count)
                .Add(innerTable)
                .SetBorder(Border.NO_BORDER)
                .SetPadding(0));
        }

        private static float GetWidth(Element element, Document document)
        {
            float rtn = MillimetersToPoints(element.Width);

            return rtn == 0 ? GetMaxWidth(document) : rtn;
        }

        private static float GetMaxWidth(Document document)
        {
            float rtn = _pageWidthPoints - document.GetLeftMargin() - document.GetRightMargin();

            return rtn;
        }

        private static float GetHeight(Element element, Document document)
        {
            float rtn = MillimetersToPoints(element.Height);

            return rtn == 0 ? GetMaxHeight(document) : rtn;
        }

        private static float GetMaxHeight(Document document)
        {
            float rtn = _pageHeightPoints - document.GetBottomMargin() - document.GetTopMargin();

            return rtn;
        }

        private static float GetImageWidth(ElementImage element, ImageData image)
        {
            float rtn = MillimetersToPoints(element.Width);

            if (rtn != 0)
            {
                return rtn;
            }

            float desiredHeight = MillimetersToPoints(element.Height);

            if (desiredHeight == 0)
            {
                return image.GetWidth();
            }

            float ratio = desiredHeight / image.GetHeight();

            return image.GetWidth() * ratio;
        }

        private static float GetImageHeight(ElementImage element, ImageData image)
        {
            float rtn = MillimetersToPoints(element.Height);

            if (rtn != 0)
            {
                return rtn;
            }

            float desiredWidth = MillimetersToPoints(element.Width);

            if (desiredWidth == 0)
            {
                return image.GetHeight();
            }

            float ratio = desiredWidth / image.GetWidth();

            return image.GetHeight() * ratio;
        }

        private static float GetBottom(Element element, Document document)
        {
            float overallHeight = _pageHeightPoints - document.GetBottomMargin() - document.GetTopMargin();

            return overallHeight - MillimetersToPoints(element.Position.Top + element.Height);
        }

        private static float GetLeft(Element element, Document document)
        {
            float rtn = MillimetersToPoints(element.Position.Left) + document.GetLeftMargin();
            return rtn;
        }

        private static void LoadData(string dataPath)
        {
            string json = ReadTextFile(dataPath);

            _data = new JsonAdvancedElement<object>(json);
        }

        private static string ReadTextFile(string path)
        {
            return File.ReadAllText(path);
        }

        private static void CreateDraft(Document document)
        {
            int endPage = document.GetPdfDocument().GetNumberOfPages();

            for (int i = 1; i <= endPage; i++)
            {
                foreach (Element element in _template.Draft.Elements)
                {
                    CreateBlockElementFromElement(document, _data, element, i);
                }
            }
        }

        private static void CreateHeader(Document document, int startPage)
        {
            int endPage = document.GetPdfDocument().GetNumberOfPages();

            for (int i = startPage; i <= endPage; i++)
            {
                foreach (Element element in _template.Header.Elements)
                {
                    CreateBlockElementFromElement(document, _data, element, i);
                }
            }
        }

        private static void CreateFooter(Document document, int startPage)
        {
            int endPage = document.GetPdfDocument().GetNumberOfPages();

            for (int i = startPage; i <= endPage; i++)
            {
                foreach (Element element in _template.Footer.Elements)
                {
                    CreateBlockElementFromElement(document, _data, element, i);
                }
            }
        }

        private static float MillimetersToPoints(float millimeters)
        {
            return millimeters * _factor;
        }

        private static float PointsToMillimeters(float millimeters)
        {
            return millimeters / _factor;
        }

        private static string Truncate(string text, ElementTextBlock element)
        {
            float textWidth = PointsToMillimeters(GetFonts(element.Format.Font).Font.GetWidth(text, element.Format.Size));
            float elementWidth = element.Width;
            int textLength = text.Length;

            while (textWidth > elementWidth)
            {
                textLength--;
                text = text[..textLength].TrimEnd(new char[0]);
                textWidth = PointsToMillimeters(GetFonts(element.Format.Font).Font.GetWidth(text, element.Format.Size));
            }

            return text;
        }
    }
}
