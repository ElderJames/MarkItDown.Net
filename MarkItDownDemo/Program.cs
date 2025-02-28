using System;
using System.Threading.Tasks;
using LangChain.DocumentLoaders;
using LangChain.Extensions;
using LangChain.Providers;
using LangChain.Providers.Ollama;
using MarkItDownSharp;
using MarkItDownSharp.Converters;
using MarkItDownSharp.Models;
using Document = LangChain.DocumentLoaders.Document;





internal class Program
{
    private static async Task Main(string[] args)
    {
        var converter = new MarkItDownConverter();


        var localPath = @"C:\Users\antun\Downloads\Digital Transformation with Low-Code.html";

        // Convert a URL
        var url = "https://www.valantic.com/en/low-code/";

        var urlPdfFile = "https://file-examples.com/storage/fe602ed48f677b2319947f8/2017/10/file-example_PDF_1MB.pdf";
        var urlPath = @"C:\Users\antun\Downloads\file-example_PDF_1MB.pdf";

        var youtubeUrl = "https://www.youtube.com/watch?v=K_w2gHypGx4";

        var bingUrl =
            "https://www.bing.com/search?q=openai&form=QBLH&sp=-1&lq=0&pq=openai&sc=15-6&qs=n&sk=&cvid=9AA2F1E17C8C41BB8E1AA6E714A1D1A2&ghsh=0&ghacc=0&ghpl=";

        var xlsxPath = @"C:\Users\antun\Downloads\file_example_XLSX_100.xlsx";
        var docxPath = @"C:\Users\antun\Downloads\file-sample_100kB.docx";
        var docxPath2 = @"C:\Users\antun\Downloads\shrek.docx";


        var pptxPath = @"C:\Users\antun\Downloads\Genie.pptx";

        var plainText = @"C:\Users\antun\Downloads\sample.txt";

        try
        {
            var result = await converter.ConvertLocalAsync(pptxPath);
            Console.WriteLine($"Title: {result.Title}");
            Console.WriteLine($"Content:\n{result.TextContent}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting URL: {ex.Message}");
        }



        //###############################



        //string parserApiUrl = "https://your-parser-api-endpoint.com/parse";
        //string pdfPath = @"C:\Users\antun\Downloads\file-example_PDF_1MB.pdf";

        //PdfAdvancedConverter converterAdv = new PdfAdvancedConverter(parserApiUrl);
        //ConversionOptions options = new ConversionOptions
        //{
        //    FileExtension = ".pdf"
        //};

        //DocumentConverterResult resultAdv = await converterAdv.ConvertAsync(pdfPath, options);

        //Console.WriteLine("Title: " + resultAdv.Title);
        //Console.WriteLine("Content:\n" + resultAdv.TextContent);




        //###############################


        //var fileAddress = @"C:\Users\antun\Downloads\MiguelAntunes2025.pdf";
        

        //// Create a DataSource from the file path
        //var dataSource = DataSource.FromPath(fileAddress);
       
        //Document document = new Document();

       


        //// Create the PDF loader
        //var loader = new PdfPigPdfLoader();

        //// Load the document
        //var documents = await loader.LoadAsync(dataSource, new DocumentLoaderSettings { ShouldCollectMetadata = true });

        //// Display the content and metadata
        //foreach (var doc in documents)
        //{
        //    Console.WriteLine($"Page {doc.Metadata["page"]}:");
        //    Console.WriteLine(doc.PageContent);
        //    Console.WriteLine();
        //}

        //Console.WriteLine("Press any key to exit...");
        //Console.ReadKey();










    }






}