using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
//zapis:https://www.pdf2go.com/
class Program
{
    static readonly HttpClient client = new HttpClient();

    static async Task MainAsync()
    {
        Console.OutputEncoding = Encoding.UTF8;

        string baseUrl = "https://gutendex.com/books";
        Console.WriteLine("Author?:");
        var author = Console.ReadLine();
        Console.WriteLine("Language?: (zh,en,jp,pl ....)");
        var language = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(author) || string.IsNullOrWhiteSpace(language))
        {
            Console.WriteLine("Author and Language must not be empty.");
            return;
        }

        string query = $"?search={Uri.EscapeDataString(author)}&languages={language}";

        try
        {
            string fullUrl = baseUrl + query;
            Console.WriteLine("Fetching data from: " + fullUrl);

            HttpResponseMessage response = await client.GetAsync(fullUrl);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            var booksData = JsonConvert.DeserializeObject<BookList>(responseBody);

            Console.WriteLine($"Total Books Found: {booksData.Count}");
            for (int i = 0; i < booksData.Results.Length; i++)
            {
                var book = booksData.Results[i];
                Console.WriteLine($"[{i}] ID: {book.Id}, Title: {book.Title}, Downloads: {book.DownloadCount}");
            }

            Console.WriteLine("Do you want to open any book? (Enter the book number or -1 to exit)");

            int choice;
            while (!int.TryParse(Console.ReadLine(), out choice) || choice < -1 || choice >= booksData.Results.Length)
            {
                Console.WriteLine("Invalid input. Please enter a valid book number or -1 to exit:");
            }

            if (choice != -1)
            {
                var selectedBook = booksData.Results[choice];

                // Check if the key 'text/plain; charset=utf-8' exists, otherwise, use 'text/plain; charset=iso-8859-1' as fallback
                string plainTextLink = selectedBook.Formats.ContainsKey("text/plain; charset=utf-8") ?
                                       selectedBook.Formats["text/plain; charset=utf-8"] :
                                       selectedBook.Formats.ContainsKey("text/plain; charset=iso-8859-1") ?
                                       selectedBook.Formats["text/plain; charset=iso-8859-1"] :
                                       selectedBook.Formats["text/plain; charset=us-ascii"]; // Use ASCII as fallback

                Console.WriteLine($"Opening link: {plainTextLink}");
                await ReadBookContent(plainTextLink);
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("\nException Caught!");
            Console.WriteLine("Message: {0} ", e.Message);
        }
    }

    static async Task Main()
    {
        await MainAsync();
    }

    static async Task ReadBookContent(string url)
    {
        HtmlWeb web = new HtmlWeb();
        HtmlDocument doc = await web.LoadFromWebAsync(url);

        string textContent = doc.DocumentNode.InnerText;

        
        textContent = RemoveFirstCharacters(textContent, 651);

        int currentPosition = 0;
        const int maxCharsToShow = 100000;

        while (currentPosition < textContent.Length)
        {
            Console.WriteLine(textContent.Substring(currentPosition, Math.Min(maxCharsToShow, textContent.Length - currentPosition)));

            if (textContent.Length - currentPosition > maxCharsToShow)
            {
                Console.WriteLine("Do you want to continue reading? Press 1 to continue, any other key to exit.");
                var continueReading = Console.ReadLine();
                if (continueReading != "1")
                {
                    return;
                }
            }

            currentPosition += maxCharsToShow;
        }
    }

    public static string RemoveFirstCharacters(string text, int count)
    {
        if (count >= text.Length)
        {
            return string.Empty;
        }
        else
        {
            return text.Substring(count);
        }
    }
}

public class BookList
{
    public int Count { get; set; }
    public string? Next { get; set; }
    public string? Previous { get; set; }
    public Book[] Results { get; set; }
}

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int DownloadCount { get; set; }
    public Dictionary<string, string> Formats { get; set; }
}
