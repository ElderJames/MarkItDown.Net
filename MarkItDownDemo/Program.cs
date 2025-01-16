using System;
using System.Threading.Tasks;
using MarkItDown;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var converter = new MarkItDownConverter();


        var localPath = @"C:\Users\antun\Downloads\Digital Transformation with Low-Code.html";

        // Convert a URL
        var url = "https://www.valantic.com/en/low-code/";

        var urlPDFFile = "https://file-examples.com/storage/fe602ed48f677b2319947f8/2017/10/file-example_PDF_1MB.pdf";
        var urlPath = @"C:\Users\antun\Downloads\file-example_PDF_1MB.pdf";

        var youtubeURL = "https://www.youtube.com/watch?v=K_w2gHypGx4";

        var bingURL =
            "https://www.bing.com/search?q=openai&form=QBLH&sp=-1&lq=0&pq=openai&sc=15-6&qs=n&sk=&cvid=9AA2F1E17C8C41BB8E1AA6E714A1D1A2&ghsh=0&ghacc=0&ghpl=";

        var xlsxPath = @"C:\Users\antun\Downloads\file_example_XLSX_100.xlsx";
        var docxPath = @"C:\Users\antun\Downloads\file-sample_100kB.docx";

        var plainText = @"C:\Users\antun\Downloads\sample.txt";

        try
        {
            var result = await converter.ConvertLocalAsync(urlPath);
            Console.WriteLine($"Title: {result.Title}");
            Console.WriteLine($"Content:\n{result.TextContent}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting URL: {ex.Message}");
        }
    }
}