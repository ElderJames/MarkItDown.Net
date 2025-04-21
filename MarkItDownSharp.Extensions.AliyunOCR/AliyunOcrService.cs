using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aliyun.Acs.Core.Http;
using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Profile;
using Microsoft.Extensions.Options;
using System.Text.Json;
using MarkItDownSharp.Services;

namespace MarkItDownSharp.Extensions.AliyunOCR
{
    public class AliyunOcrService : IOcrService
    {
        private readonly IClientProfile _profile;

        public AliyunOcrService(IOptions<AliyunOcrOptions> options)
        {
            _profile = DefaultProfile.GetProfile(
                "cn-hangzhou",  //地域ID
                options.Value.AccessKeyId,  //AccessKey ID
                options.Value.AccessKeySecret);//AccessKey Secret    
        }

        public async Task<string> ExtractTextAsync(byte[] imageData)
        {
            DefaultAcsClient client = new DefaultAcsClient(_profile);
            CommonRequest request = new CommonRequest();
            request.Method = MethodType.POST;
            request.Domain = "ocr-api.cn-hangzhou.aliyuncs.com";
            request.Version = "2021-07-07";
            request.Action = "RecognizeAllText";
            request.AddQueryParameters("Type", "Advanced");
            request.SetContent(imageData, "utf-8", FormatType.RAW);

            CommonResponse response = client.GetCommonResponse(request);
            var result = JsonSerializer.Deserialize<OcrResult>(response.Data);
            return ConvertToMarkdown(result);
        }

        private string ConvertToMarkdown(OcrResult? result)
        {
            if (result?.Data?.SubImages == null || result.Data.SubImages.Length == 0)
                return string.Empty;

            var markdown = new StringBuilder();
            var subImage = result.Data.SubImages[0];

            // 1. 处理表格信息
            if (subImage.TableInfo?.TableDetails != null)
            {
                foreach (var table in subImage.TableInfo.TableDetails)
                {
                    AppendTableToMarkdown(markdown, table);
                    markdown.AppendLine();
                }
            }

            // 2. 处理段落信息
            if (subImage.ParagraphInfo?.ParagraphDetails != null)
            {
                foreach (var paragraph in subImage.ParagraphInfo.ParagraphDetails)
                {
                    if (paragraph.ParagraphContent != null)
                    {
                        // 检查是否可能是标题
                        if (IsPotentialHeading(paragraph.ParagraphContent))
                        {
                            markdown.AppendLine($"## {paragraph.ParagraphContent.Trim()}");
                        }
                        else
                        {
                            markdown.AppendLine(paragraph.ParagraphContent.Trim());
                        }
                        markdown.AppendLine();
                    }
                }
            }

            // 3. 处理行信息（可能是列表项）
            if (subImage.RowInfo?.RowDetails != null)
            {
                foreach (var row in subImage.RowInfo.RowDetails)
                {
                    if (row.RowContent != null)
                    {
                        var content = row.RowContent.Trim();
                        if (IsPotentialListItem(content))
                        {
                            markdown.AppendLine($"- {content}");
                        }
                        else
                        {
                            markdown.AppendLine(content);
                        }
                    }
                }
                markdown.AppendLine();
            }

            // 4. 处理KV信息（如果存在）
            if (subImage.KvInfo?.Data != null)
            {
                var kvData = JsonSerializer.Deserialize<Dictionary<string, string>>(subImage.KvInfo.Data);
                if (kvData != null)
                {
                    foreach (var kv in kvData)
                    {
                        markdown.AppendLine($"**{kv.Key}**: {kv.Value}");
                    }
                    markdown.AppendLine();
                }
            }

            // 5. 处理Block信息（当其他结构化信息不足时使用）
            ProcessBlockInfo(markdown, subImage);

            return markdown.ToString();
        }

        private void AppendTableToMarkdown(StringBuilder markdown, TableDetail table)
        {
            if (table.Header != null && table.Header.Contents != null && table.Header.Contents.Any())
            {
                markdown.AppendLine($"### {table.Header.Contents[0]}");
                markdown.AppendLine();
            }

            // 创建表头
            var headers = new string[table.ColumnCount];
            var separators = new string[table.ColumnCount];
            for (int i = 0; i < table.ColumnCount; i++)
            {
                headers[i] = $"Column {i + 1}";
                separators[i] = "---";
            }

            markdown.AppendLine($"|{string.Join("|", headers)}|");
            markdown.AppendLine($"|{string.Join("|", separators)}|");

            // 按行组织单元格
            if (table.CellDetails != null)
            {
                var cells = table.CellDetails
                    .OrderBy(c => c.RowStart)
                    .ThenBy(c => c.ColumnStart)
                    .GroupBy(c => c.RowStart);

                foreach (var row in cells)
                {
                    var rowContent = new string[table.ColumnCount];
                    Array.Fill(rowContent, "");

                    foreach (var cell in row)
                    {
                        if (cell.CellContent != null)
                        {
                            rowContent[cell.ColumnStart] = cell.CellContent.Trim('"');
                        }
                    }

                    markdown.AppendLine($"|{string.Join("|", rowContent)}|");
                }
            }
        }

        private bool IsPotentialHeading(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            text = text.Trim();
            return text.Length < 50 && // 标题通常较短
                   !text.Contains("。") && // 标题通常不包含句号
                   !text.EndsWith("：") && // 标题通常不以冒号结尾
                   text.Length > 0 &&
                   char.IsUpper(text[0]); // 标题通常以大写字母开头
        }

        private bool IsPotentialListItem(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            text = text.Trim();
            return text.StartsWith("•") ||
                   text.StartsWith("-") ||
                   text.StartsWith("*") ||
                   (text.Length < 100 && text.Contains("：")) || // 短文本包含冒号可能是列表项
                   (text.Length > 0 && Char.IsDigit(text[0])); // 数字开头可能是有序列表
        }

        /// <summary>
        /// 处理阿里云OCR返回的BlockInfo和BlockDetails信息，提取文本内容并格式化为Markdown
        /// </summary>
        /// <param name="markdown">Markdown构建器</param>
        /// <param name="subImage">包含OCR识别结果的子图像</param>
        private void ProcessBlockInfo(StringBuilder markdown, SubImage subImage)
        {
            if (subImage.BlockInfo?.BlockDetails == null || !subImage.BlockInfo.BlockDetails.Any())
                return;

            var blocks = subImage.BlockInfo.BlockDetails
                .Where(b => b.BlockContent != null)
                .OrderBy(b => b.BlockRect?.CenterY)
                .ThenBy(b => b.BlockRect?.CenterX)
                .ToList();

            var currentParagraph = new StringBuilder();
            var isInList = false;

            foreach (var block in blocks)
            {
                var content = block.BlockContent?.Trim();
                if (string.IsNullOrEmpty(content))
                    continue;

                // 检查是否是新段落的开始
                bool isNewParagraph = IsNewParagraph(block, blocks);

                if (isNewParagraph && currentParagraph.Length > 0)
                {
                    // 输出当前段落
                    markdown.AppendLine(currentParagraph.ToString());
                    markdown.AppendLine();
                    currentParagraph.Clear();
                    isInList = false;
                }

                // 处理列表项
                if (IsListItem(content))
                {
                    if (currentParagraph.Length > 0)
                    {
                        markdown.AppendLine(currentParagraph.ToString());
                        currentParagraph.Clear();
                    }
                    markdown.AppendLine($"- {content}");
                    isInList = true;
                    continue;
                }

                // 处理标题
                if (IsHeading(content))
                {
                    if (currentParagraph.Length > 0)
                    {
                        markdown.AppendLine(currentParagraph.ToString());
                        currentParagraph.Clear();
                    }
                    markdown.AppendLine($"## {content}");
                    markdown.AppendLine();
                    continue;
                }

                // 普通文本处理
                if (currentParagraph.Length > 0)
                {
                    currentParagraph.Append(" ");
                }
                currentParagraph.Append(content);
            }

            // 输出最后一个段落
            if (currentParagraph.Length > 0)
            {
                markdown.AppendLine(currentParagraph.ToString());
                markdown.AppendLine();
            }
        }

        private bool IsNewParagraph(BlockDetail currentBlock, List<BlockDetail> allBlocks)
        {
            if (currentBlock.BlockRect == null)
                return true;

            var prevBlock = allBlocks
                .Where(b => b.BlockRect != null &&
                           b.BlockRect.CenterY < currentBlock.BlockRect.CenterY)
                .OrderByDescending(b => b.BlockRect.CenterY)
                .FirstOrDefault();

            if (prevBlock?.BlockRect == null)
                return true;

            // 如果与前一个块的垂直距离超过一定阈值，认为是新段落
            int verticalGap = currentBlock.BlockRect.CenterY - prevBlock.BlockRect.CenterY;
            return verticalGap > currentBlock.BlockRect.Height * 1.5;
        }

        private bool IsListItem(string content)
        {
            return content.StartsWith("•") ||
                   content.StartsWith("-") ||
                   content.StartsWith("*") ||
                   (content.Length < 100 && content.Contains("：")) ||
                   (content.Length > 0 && char.IsDigit(content[0]) && content.Contains("."));
        }

        private bool IsHeading(string content)
        {
            return content.Length < 50 &&
                   !content.Contains("。") &&
                   !content.EndsWith("：") &&
                   content.Length > 0 &&
                   char.IsUpper(content[0]);
        }
    }

    public class OcrResult
    {
        public string? RequestId { get; set; }
        public OcrData? Data { get; set; }
        public string? Code { get; set; }
        public string? Message { get; set; }
    }

    public class OcrData
    {
        public int Height { get; set; }
        public int Width { get; set; }
        public string? Content { get; set; }
        public int SubImageCount { get; set; }
        public SubImage[]? SubImages { get; set; }
        public string? XmlResult { get; set; }
        public string? AlgoVersion { get; set; }
        public bool IsMixedMode { get; set; }
        public int PageNo { get; set; }
        public string? KvExcelUrl { get; set; }
    }

    public class SubImage
    {
        public int SubImageId { get; set; }
        public string? Type { get; set; }
        public int Angle { get; set; }
        public Point[]? SubImagePoints { get; set; }
        public Rectangle? SubImageRect { get; set; }
        public KvInfo? KvInfo { get; set; }
        public BlockInfo? BlockInfo { get; set; }
        public TableInfo? TableInfo { get; set; }
        public RowInfo? RowInfo { get; set; }
        public ParagraphInfo? ParagraphInfo { get; set; }
    }

    public class Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class Rectangle
    {
        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class KvInfo
    {
        public int KvCount { get; set; }
        public string? Data { get; set; }
    }

    public class BlockInfo
    {
        public int BlockCount { get; set; }
        public List<BlockDetail>? BlockDetails { get; set; }
    }

    public class BlockDetail
    {
        public int BlockId { get; set; }
        public string? BlockContent { get; set; }
        public int BlockConfidence { get; set; }
        public int BlockAngle { get; set; }
        public Point[]? BlockPoints { get; set; }
        public Rectangle? BlockRect { get; set; }
    }

    public class TableInfo
    {
        public int TableCount { get; set; }
        public List<TableDetail>? TableDetails { get; set; }
        public string? TableExcel { get; set; }
        public string? TableHtml { get; set; }
    }

    public class TableDetail
    {
        public int TableId { get; set; }
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public int CellCount { get; set; }
        public TableHeader? Header { get; set; }
        public TableFooter? Footer { get; set; }
        public List<CellDetail>? CellDetails { get; set; }
        public Point[]? TablePoints { get; set; }
        public Rectangle? TableRect { get; set; }
    }

    public class TableHeader
    {
        public List<string>? Contents { get; set; }
        public int BlockId { get; set; }
    }

    public class TableFooter
    {
        public List<string>? Contents { get; set; }
        public int BlockId { get; set; }
    }

    public class CellDetail
    {
        public int CellId { get; set; }
        public string? CellContent { get; set; }
        public int RowStart { get; set; }
        public int RowEnd { get; set; }
        public int ColumnStart { get; set; }
        public int ColumnEnd { get; set; }
        public List<int>? BlockList { get; set; }
        public Point[]? CellPoints { get; set; }
        public Rectangle? CellRect { get; set; }
        public int CellAngle { get; set; }
    }

    public class RowInfo
    {
        public int RowCount { get; set; }
        public List<RowDetail>? RowDetails { get; set; }
    }

    public class RowDetail
    {
        public int RowId { get; set; }
        public string? RowContent { get; set; }
        public List<int>? BlockList { get; set; }
    }

    public class ParagraphInfo
    {
        public int ParagraphCount { get; set; }
        public List<ParagraphDetail>? ParagraphDetails { get; set; }
    }

    public class ParagraphDetail
    {
        public int ParagraphId { get; set; }
        public string? ParagraphContent { get; set; }
        public List<int>? BlockList { get; set; }
    }
}
