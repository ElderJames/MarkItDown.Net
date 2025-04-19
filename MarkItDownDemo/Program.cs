// Example usage for .NET 9

using System;
using System.Threading.Tasks;
using MarkItDownSharp;
using MarkItDownSharp.Converters;
using MarkItDownSharp.Models;

// Set up the main async execution
await RunDemoAsync();

async Task RunDemoAsync()
{
    var converter = new MarkItDownConverter();

    // Confluence page example URL.
    // For example, using a space overview URL for multiple pages.
    var confluenceUrl = "https://yourcompany/wiki/spaces/SPACENAME/overview";

    // Set up conversion options.
    var options = new ConversionOptions
    {
        ConfluenceBaseUrl = "https://yourcompany/wiki/",
        ConfluenceMaxPages = 500000,
        ConfluencePageLimit = 50,
        ConfluenceExpand = "body.export_view",
        ConfluenceUsername = "YOUR_USERNAME",
        ConfluenceApiToken = "YOUR_API_TOKEN"
    };

    try
    {
        // For Confluence URLs, call ConvertToListAsync to receive a list of DocumentConverterResult items.
        var results = await converter.ConvertToListAsync(confluenceUrl, options);

        foreach (var result in results)
        {
            Console.WriteLine($"Title: {result.Title}");
            Console.WriteLine("Content:");
            Console.WriteLine(result.TextContent);
            if (result.MetaData != null)
            {
                Console.WriteLine("Metadata:");
                foreach (var kv in result.MetaData)
                {
                    if (kv.Value is System.Collections.IEnumerable && !(kv.Value is string))
                    {
                        Console.WriteLine($"  {kv.Key}: {string.Join(", ", (System.Collections.IEnumerable)kv.Value)}");
                    }
                    else
                    {
                        Console.WriteLine($"  {kv.Key}: {kv.Value}");
                    }
                }
            }
            Console.WriteLine();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}