// Converters/PdfAdvancedConverter.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MarkItDownSharp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace MarkItDownSharp.Converters
{
    public class PdfAdvancedConverter : DocumentConverter
    {
        private readonly LayoutPdfReader _reader;

        public PdfAdvancedConverter(string parserApiUrl)
        {
            _reader = new LayoutPdfReader(parserApiUrl);
        }

        public override bool CanConvertUrl(string url)
        {
            Uri uriResult;
            bool result = Uri.TryCreate(url, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            return result && Path.GetExtension(uriResult.LocalPath).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        }

        public override bool CanConvertFile(string extension)
        {
            return extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task<DocumentConverterResult> ConvertAsync(string localPath, ConversionOptions options)
        {
            if (!CanConvertFile(options.FileExtension)) return null;

            if (!File.Exists(localPath))
                throw new FileNotFoundException($"File not found: {localPath}");

            // Use the LayoutPDFReader to parse the PDF
            Document document = await _reader.ReadPdfAsync(localPath);

            var markdownBuilder = new StringBuilder();
            var title = Path.GetFileNameWithoutExtension(localPath); // Default title: document name

            if (document.Sections().Any())
            {
                var firstSection = document.Sections().First();
                title = firstSection.Title;
            }

            // Iterate through the document's text content
            markdownBuilder.AppendLine(document.ToText(includeDuplicates: false));

            // Finalize the markdown content
            var finalMarkdown = markdownBuilder.ToString().Trim();

            var result = new DocumentConverterResult
            {
                Title = title,
                TextContent = finalMarkdown
            };

            return result;
        }
    }

    /// <summary>
    /// Handles downloading and parsing the PDF using an external parser API.
    /// </summary>
    public class LayoutPdfReader
    {
        private readonly string _parserApiUrl;
        private readonly HttpClient _downloadClient;
        private readonly HttpClient _apiClient;

        public LayoutPdfReader(string parserApiUrl)
        {
            _parserApiUrl = parserApiUrl;
            _downloadClient = new HttpClient();
            _apiClient = new HttpClient();
        }

        private async Task<(string fileName, byte[] data, string contentType)> DownloadPdfAsync(string pdfUrl)
        {
            // Some servers only allow browsers' user agents to download
            var request = new HttpRequestMessage(HttpMethod.Get, pdfUrl);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36");

            HttpResponseMessage response = await _downloadClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsByteArrayAsync();
                var uri = new Uri(pdfUrl);
                string fileName = Path.GetFileName(uri.LocalPath);
                return (fileName, data, "application/pdf");
            }
            throw new HttpRequestException($"Failed to download PDF from URL: {pdfUrl}. Status Code: {response.StatusCode}");
        }

        private async Task<HttpResponseMessage> ParsePdfAsync((string fileName, byte[] data, string contentType) pdfFile)
        {
            using (var content = new MultipartFormDataContent())
            {
                var fileContent = new ByteArrayContent(pdfFile.data);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(pdfFile.contentType);
                content.Add(fileContent, "file", pdfFile.fileName);

                HttpResponseMessage response = await _apiClient.PostAsync(_parserApiUrl, content);
                return response;
            }
        }

        public async Task<Document> ReadPdfAsync(string pathOrUrl, byte[] contents = null)
        {
            (string fileName, byte[] data, string contentType) pdfFile;

            if (contents != null)
            {
                pdfFile = (Path.GetFileName(pathOrUrl), contents, "application/pdf");
            }
            else
            {
                bool isUrl = Uri.TryCreate(pathOrUrl, UriKind.Absolute, out Uri uriResult) &&
                             (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                if (isUrl)
                {
                    pdfFile = await DownloadPdfAsync(pathOrUrl);
                }
                else
                {
                    if (!File.Exists(pathOrUrl))
                        throw new FileNotFoundException($"File not found: {pathOrUrl}");

                    byte[] fileData = File.ReadAllBytes(pathOrUrl);
                    string fileName = Path.GetFileName(pathOrUrl);
                    pdfFile = (fileName, fileData, "application/pdf");
                }
            }

            HttpResponseMessage parserResponse = await ParsePdfAsync(pdfFile);
            if (!parserResponse.IsSuccessStatusCode)
            {
                string errorContent = await parserResponse.Content.ReadAsStringAsync();
                throw new ValueException($"Parser API Error: {errorContent}");
            }

            string jsonResponse = await parserResponse.Content.ReadAsStringAsync();
            JObject responseJson = JObject.Parse(jsonResponse);
            JToken blocksToken = responseJson["return_dict"]?["result"]?["blocks"];

            if (blocksToken == null)
                throw new JsonException("Invalid JSON response: 'blocks' field not found.");

            var blocks = blocksToken.ToObject<List<JObject>>();

            // Use LayoutReader to build the block tree
            LayoutReader layoutReader = new LayoutReader();
            Block root = layoutReader.Read(blocks);

            return new Document(root, blocks);
        }
    }

    /// <summary>
    /// Base class representing a block in the document.
    /// </summary>
    public class Block
    {
        public string Tag { get; set; }
        public int Level { get; set; }
        public int PageIdx { get; set; }
        public int BlockIdx { get; set; }
        public double Top { get; set; }
        public double Left { get; set; }
        public List<double> BBox { get; set; }
        public List<string> Sentences { get; set; }
        public List<Block> Children { get; set; }
        public Block Parent { get; set; }
        public JObject BlockJson { get; set; }

        public Block()
        {
            Children = new List<Block>();
            Sentences = new List<string>();
            BBox = new List<double>();
        }

        public Block(JObject blockJson) : this()
        {
            BlockJson = blockJson;
            Tag = blockJson["tag"]?.ToString();
            Level = blockJson["level"]?.ToObject<int>() ?? -1;
            PageIdx = blockJson["page_idx"]?.ToObject<int>() ?? -1;
            BlockIdx = blockJson["block_idx"]?.ToObject<int>() ?? -1;
            Top = blockJson["top"]?.ToObject<double>() ?? -1;
            Left = blockJson["left"]?.ToObject<double>() ?? -1;
            BBox = blockJson["bbox"]?.ToObject<List<double>>() ?? new List<double>();
            Sentences = blockJson["sentences"]?.ToObject<List<string>>() ?? new List<string>();
        }

        public virtual string ToText(bool includeChildren = false, bool recurse = false)
        {
            throw new NotImplementedException();
        }

        public virtual string ToHtml(bool includeChildren = false, bool recurse = false)
        {
            throw new NotImplementedException();
        }

        public List<Block> ParentChain()
        {
            List<Block> chain = new List<Block>();
            Block current = Parent;
            while (current != null)
            {
                chain.Add(current);
                current = current.Parent;
            }
            chain.Reverse();
            return chain;
        }

        public string ParentText()
        {
            var parents = ParentChain();
            List<string> headerTexts = new List<string>();
            List<string> paraTexts = new List<string>();

            foreach (var p in parents)
            {
                if (p.Tag == "header")
                {
                    headerTexts.Add(p.ToText());
                }
                else if (p.Tag == "list_item" || p.Tag == "para")
                {
                    paraTexts.Add(p.ToText());
                }
            }

            string text = string.Join(" > ", headerTexts);
            if (paraTexts.Any())
            {
                text += "\n" + string.Join("\n", paraTexts);
            }
            return text;
        }

        public string ToContextText(bool includeSectionInfo = true)
        {
            StringBuilder sb = new StringBuilder();
            if (includeSectionInfo)
            {
                sb.AppendLine(ParentText());
            }

            if (Tag == "list_item" || Tag == "para" || Tag == "table")
            {
                sb.AppendLine(ToText(includeChildren: true, recurse: true));
            }
            else
            {
                sb.AppendLine(ToText());
            }

            return sb.ToString();
        }

        public void IterChildren(Block node, int level, Action<Block> nodeVisitor)
        {
            foreach (var child in node.Children)
            {
                nodeVisitor(child);
                if (!(child.Tag == "list_item" || child.Tag == "para" || child.Tag == "table"))
                {
                    IterChildren(child, level + 1, nodeVisitor);
                }
            }
        }

        internal List<Block> Chunks()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Represents a paragraph block.
    /// </summary>
    public class Paragraph : Block
    {
        public Paragraph(JObject paraJson) : base(paraJson)
        {
        }

        public override string ToText(bool includeChildren = false, bool recurse = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Join("\n", Sentences));

            if (includeChildren)
            {
                foreach (var child in Children)
                {
                    sb.AppendLine(child.ToText(includeChildren: recurse, recurse: recurse));
                }
            }

            return sb.ToString();
        }

        public override string ToHtml(bool includeChildren = false, bool recurse = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<p>");
            sb.AppendLine(string.Join("\n", Sentences));

            if (includeChildren && Children.Any())
            {
                sb.Append("<ul>");
                foreach (var child in Children)
                {
                    sb.Append(child.ToHtml(includeChildren: recurse, recurse: recurse));
                }
                sb.Append("</ul>");
            }
            sb.Append("</p>");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Represents a section (header) block.
    /// </summary>
    public class Section : Block
    {
        public string Title { get; set; }

        public Section(JObject sectionJson) : base(sectionJson)
        {
            Title = string.Join("\n", Sentences);
        }

        public override string ToText(bool includeChildren = false, bool recurse = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Title);

            if (includeChildren)
            {
                foreach (var child in Children)
                {
                    sb.Append("\n" + child.ToText(includeChildren: recurse, recurse: recurse));
                }
            }

            return sb.ToString();
        }

        public override string ToHtml(bool includeChildren = false, bool recurse = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"<h{Level + 1}>");
            sb.Append(Title);
            sb.Append($"</h{Level + 1}>");

            if (includeChildren)
            {
                foreach (var child in Children)
                {
                    sb.Append(child.ToHtml(includeChildren: recurse, recurse: recurse));
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Represents a list item block.
    /// </summary>
    public class ListItem : Block
    {
        public ListItem(JObject listJson) : base(listJson)
        {
        }

        public override string ToText(bool includeChildren = false, bool recurse = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Join("\n", Sentences));

            if (includeChildren)
            {
                foreach (var child in Children)
                {
                    sb.AppendLine(child.ToText(includeChildren: recurse, recurse: recurse));
                }
            }

            return sb.ToString();
        }

        public override string ToHtml(bool includeChildren = false, bool recurse = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<li>");
            sb.AppendLine(string.Join("\n", Sentences));

            if (includeChildren && Children.Any())
            {
                sb.Append("<ul>");
                foreach (var child in Children)
                {
                    sb.Append(child.ToHtml(includeChildren: recurse, recurse: recurse));
                }
                sb.Append("</ul>");
            }

            sb.Append("</li>");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Represents a table cell block.
    /// </summary>
    public class TableCell : Block
    {
        public int ColSpan { get; set; }
        public string CellValue { get; set; }
        public Paragraph CellNode { get; set; }

        public TableCell(JObject cellJson) : base(cellJson)
        {
            ColSpan = cellJson["col_span"]?.ToObject<int>() ?? 1;
            CellValue = cellJson["cell_value"]?.ToString() ?? string.Empty;

            if (!(CellValue is string))
            {
                // Assuming cell_value is a JSON object representing a Paragraph
                CellNode = new Paragraph((JObject)cellJson["cell_value"]);
            }
            else
            {
                CellNode = null;
            }
        }

        public override string ToText(bool includeChildren = false, bool recurse = false)
        {
            if (CellNode != null)
            {
                return CellNode.ToText();
            }
            return CellValue;
        }

        public override string ToHtml(bool includeChildren = false, bool recurse = false)
        {
            string cellContent = CellNode != null ? CellNode.ToHtml() : CellValue;
            if (ColSpan == 1)
            {
                return $"<td colSpan=\"{ColSpan}\">{cellContent}</td>";
            }
            else
            {
                return $"<td>{cellContent}</td>";
            }
        }
    }

    /// <summary>
    /// Represents a table row block.
    /// </summary>
    public class TableRow : Block
    {
        public List<TableCell> Cells { get; set; }

        public TableRow(JObject rowJson) : base(rowJson)
        {
            Cells = new List<TableCell>();
            if (rowJson["type"]?.ToString() == "full_row")
            {
                TableCell cell = new TableCell(rowJson);
                Cells.Add((TableCell)cell);
            }
            else
            {
                foreach (var cellJson in rowJson["cells"])
                {
                    TableCell cell = new TableCell((JObject)cellJson);
                    Cells.Add(cell);
                }
            }
        }

        public override string ToText(bool includeChildren = false, bool recurse = false)
        {
            var cellTexts = Cells.Select(c => c.ToText()).ToList();
            return " | " + string.Join(" | ", cellTexts);
        }

        public override string ToHtml(bool includeChildren = false, bool recurse = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<tr>");
            foreach (var cell in Cells)
            {
                sb.Append(cell.ToHtml());
            }
            sb.Append("</tr>");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Represents a table header row block.
    /// </summary>
    public class TableHeader : Block
    {
        public List<TableCell> Cells { get; set; }

        public TableHeader(JObject headerJson) : base(headerJson)
        {
            Cells = new List<TableCell>();
            foreach (var cellJson in headerJson["cells"])
            {
                TableCell cell = new TableCell((JObject)cellJson);
                Cells.Add(cell);
            }
        }

        public override string ToText(bool includeChildren = false, bool recurse = false)
        {
            var cellTexts = Cells.Select(c => c.ToText()).ToList();
            string headerLine = " | " + string.Join(" | ", cellTexts);
            string separatorLine = string.Join(" | ", Cells.Select(c => "---"));
            return $"{headerLine}\n{separatorLine}";
        }

        public override string ToHtml(bool includeChildren = false, bool recurse = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<thead><tr>");
            foreach (var cell in Cells)
            {
                sb.Append(cell.ToHtml());
            }
            sb.Append("</tr></thead>");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Represents a table block.
    /// </summary>
    public class Table : Block
    {
        public List<TableRow> Rows { get; set; }
        public List<TableHeader> Headers { get; set; }
        public string Name { get; set; }

        public Table(JObject tableJson, Block parent) : base(tableJson)
        {
            Rows = new List<TableRow>();
            Headers = new List<TableHeader>();
            Name = tableJson["name"]?.ToString() ?? string.Empty;

            if (tableJson["table_rows"] != null)
            {
                foreach (var rowJson in tableJson["table_rows"])
                {
                    if (rowJson["type"]?.ToString() == "table_header")
                    {
                        TableHeader header = new TableHeader((JObject)rowJson);
                        Headers.Add(header);
                    }
                    else
                    {
                        TableRow row = new TableRow((JObject)rowJson);
                        Rows.Add(row);
                    }
                }
            }
        }

        public override string ToText(bool includeChildren = false, bool recurse = false)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var header in Headers)
            {
                sb.AppendLine(header.ToText());
            }
            foreach (var row in Rows)
            {
                sb.AppendLine(row.ToText());
            }
            return sb.ToString();
        }

        public override string ToHtml(bool includeChildren = false, bool recurse = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<table>");
            foreach (var header in Headers)
            {
                sb.Append(header.ToHtml());
            }
            sb.Append("<tbody>");
            foreach (var row in Rows)
            {
                sb.Append(row.ToHtml());
            }
            sb.Append("</tbody></table>");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Reads the layout tree from JSON and constructs the block hierarchy.
    /// </summary>
    public class LayoutReader
    {
        public Block Read(List<JObject> blocksJson)
        {
            Block root = new Block();
            Block parent = root;
            List<Block> parentStack = new List<Block> { root };
            Block prevNode = root;
            List<Block> listStack = new List<Block>();

            foreach (var blockJson in blocksJson)
            {
                string tag = blockJson["tag"]?.ToString();
                Block node = null;

                switch (tag)
                {
                    case "para":
                        node = new Paragraph(blockJson);
                        parent.AddChild(node);
                        break;
                    case "table":
                        node = new Table(blockJson, prevNode);
                        parent.AddChild(node);
                        break;
                    case "list_item":
                        node = new ListItem(blockJson);
                        HandleListItem(node, ref parent, ref parentStack, ref listStack, prevNode);
                        break;
                    case "header":
                        node = new Section(blockJson);
                        HandleSection(node, ref parent, ref parentStack);
                        break;
                    default:
                        node = new Block(blockJson);
                        parent.AddChild(node);
                        break;
                }

                prevNode = node;
            }

            return root;
        }

        private void HandleListItem(Block node, ref Block parent, ref List<Block> parentStack, ref List<Block> listStack, Block prevNode)
        {
            if (prevNode.Tag == "para" && prevNode.Level == node.Level)
            {
                listStack.Add(prevNode);
            }
            else if (prevNode.Tag == "list_item")
            {
                if (node.Level > prevNode.Level)
                {
                    listStack.Add(prevNode);
                }
                else if (node.Level < prevNode.Level)
                {
                    while (listStack.Any() && listStack.Last().Level > node.Level)
                    {
                        listStack.RemoveAt(listStack.Count - 1);
                    }
                }
            }

            if (listStack.Any())
            {
                listStack.Last().AddChild(node);
            }
            else
            {
                parent.AddChild(node);
            }
        }

        private void HandleSection(Block node, ref Block parent, ref List<Block> parentStack)
        {
            if (node.Level > parent.Level)
            {
                parentStack.Add(node);
                parent.AddChild(node);
            }
            else
            {
                while (parentStack.Count > 1 && parentStack.Last().Level >= node.Level)
                {
                    parentStack.RemoveAt(parentStack.Count - 1);
                }
                parentStack.Last().AddChild(node);
                parentStack.Add(node);
            }
            parent = node;
        }
    }

    /// <summary>
    /// Represents the entire document with methods to extract content.
    /// </summary>
    public class Document
    {
        public Block RootNode { get; set; }
        public List<JObject> Json { get; set; }
        public List<Section> TopSections { get; set; }

        public Document(Block rootNode, List<JObject> json)
        {
            RootNode = rootNode;
            Json = json;
            TopSections = GetTopSections();
        }

        public List<Section> Sections()
        {
            List<Section> sections = new List<Section>();
            IterSections(RootNode, sections);
            return sections;
        }

        private void IterSections(Block node, List<Section> sections)
        {
            if (node is Section section)
            {
                sections.Add(section);
            }

            foreach (var child in node.Children)
            {
                IterSections(child, sections);
            }
        }

        private List<Section> GetTopSections()
        {
            List<Section> topSections = new List<Section>();
            var sections = Sections();
            int sectionsCount = sections.Count;

            for (int i = 0; i < sectionsCount; i++)
            {
                bool isTop = true;
                for (int j = 0; j < sectionsCount; j++)
                {
                    if (i == j) continue;
                    if (sections[j].Children.Contains(sections[i]))
                    {
                        isTop = false;
                        break;
                    }
                }
                if (isTop)
                {
                    topSections.Add(sections[i]);
                }
            }

            return topSections;
        }

        public List<Block> Chunks()
        {
            return RootNode.Chunks();
        }

        public string ToText(bool includeDuplicates = false)
        {
            StringBuilder sb = new StringBuilder();

            if (includeDuplicates)
            {
                foreach (var section in Sections())
                {
                    sb.AppendLine(section.ToText(includeChildren: true, recurse: true));
                }
            }
            else
            {
                foreach (var section in TopSections)
                {
                    sb.AppendLine(section.ToText(includeChildren: true, recurse: true));
                }
            }

            return sb.ToString();
        }

        public string ToHtml(bool includeDuplicates = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<html>");

            if (includeDuplicates)
            {
                foreach (var section in Sections())
                {
                    sb.Append(section.ToHtml(includeChildren: true, recurse: true));
                }
            }
            else
            {
                foreach (var section in TopSections)
                {
                    sb.Append(section.ToHtml(includeChildren: true, recurse: true));
                }
            }

            sb.Append("</html>");
            return sb.ToString();
        }
    }

    // Extension methods for the Block class to add children
    public static class BlockExtensions
    {
        public static void AddChild(this Block parent, Block child)
        {
            parent.Children.Add(child);
            child.Parent = parent;
        }
    }

    // Custom exception for value errors
    public class ValueException : Exception
    {
        public ValueException(string message) : base(message)
        {
        }
    }
}