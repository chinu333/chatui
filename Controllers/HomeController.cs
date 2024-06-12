using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using dotnetmvc.Models;
using System.Text;
using Microsoft.KernelMemory;
using Azure; 
using Azure.Core.GeoJson; 
using Azure.Maps.Search; 
using Azure.Maps.Search.Models;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace dotnetmvc.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ActionName("Index")]
    public async Task<IActionResult> Ask(AskQuestion q)
    {
        var question = Request.Form["question"];
        var imageurl = Request.Form["imageurl"];
        var lanuagemodel = Request.Form["languagemodel"];
        Console.WriteLine("Image URL :: " + imageurl);
        Console.WriteLine("Language Model :: " + lanuagemodel);

        Stopwatch stopwatch  = new Stopwatch();

        var memory = new MemoryWebClient("http://127.0.0.1:9001/");
        Console.WriteLine("Question from the model :: " + q.Question);
        ModelState.Clear();
        

        // var answer = await memory.AskAsync(prompt);
        // Console.WriteLine($"\nAI: {answer.Result}");
        
        if((String)lanuagemodel != null && (String)lanuagemodel == "gpt4o" && (String)imageurl != null && (String)imageurl != "")
        {
            stopwatch.Start();
            q.Answer = await ProcessImage(imageurl, question);
            stopwatch.Stop();
            q.Answer = q.Answer + "\n\n" + "Response Time (Gpt-4o :: Server side) :: " + stopwatch.ElapsedMilliseconds + " ms";
            q.Question = "";
            q.ImageURL = null;
            q.LanguageModel = null;
            return View(q);
            
        } else if((String)lanuagemodel != null && (String)lanuagemodel == "phi" && (String)imageurl != null && (String)imageurl != "")
        {
            stopwatch.Start();
            q.Answer = await ProcessPhiImage(imageurl, question);
            stopwatch.Stop();
            q.Answer = q.Answer + "\n\n" + "Response Time (Phi :: Server side) :: " + stopwatch.ElapsedMilliseconds + " ms";
            q.Question = "";
            q.ImageURL = null;
            q.LanguageModel = null;
            return View(q);
            
        }

        stopwatch.Start();
        var answer = await memory.AskAsync(question,
                                    index: "miscindex",
                                    filter: MemoryFilters.ByTag("user", "Blake"));

        stopwatch.Stop();
        q.Answer = answer.Result;
        string citations = "Citations::\n";

        // Show sources / citations
        foreach (var x in answer.RelevantSources)
        {
            Console.WriteLine(x.SourceUrl != null
                ? $"  - {x.SourceUrl} [{x.Partitions.First().LastUpdate:D}]"
                : $"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]");

            // citations = citations + " - " + x.SourceName ?? "" + " - " + x.Link ?? "" + $"  - {x.SourceName}  - {x.Link} [{x.Partitions.First().LastUpdate:D}]" + "\n";
            citations = citations + $"  - {x.SourceName}  |  Last Updated: [{x.Partitions.First().LastUpdate:D}]" + "\n";
        }

        if(answer.Result == "INFO NOT FOUND")
        {
            q.Answer = answer.Result + "\n\n" + "Response Time (Server side) :: " + stopwatch.ElapsedMilliseconds + " ms";
        } else {
            q.Answer = answer.Result + "\n\n" + citations + "\n\n" + "Response Time (Server side) :: " + stopwatch.ElapsedMilliseconds + " ms";
        }

        q.Question = "";

        Console.WriteLine($"\nAI: {answer.Result}");
        citations = "";

        return View(q);
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public void CallAzureMaps()
    {
        // Use Azure Maps subscription key authentication 
        var subscriptionKey = "azure maps key";
        var credential = new AzureKeyCredential(subscriptionKey);
        var client = new MapsSearchClient(credential); 

        SearchAddressResult searchResult = client.SearchAddress(
            // "1301 Alaskan Way, Seattle, WA 98101, US");
            // "1800 Greystone Summit Drive, Cumming, GA 30040, US");
            // "1800 Greystone Summit Dr., Cumming, GA 30040, US");
            "1800 Greystone Summit Dr., Cumming, Georgia 30040, US");

        if (searchResult.Results.Count > 0) 
        {
            SearchAddressResultItem result = searchResult.Results.First();
            Console.WriteLine($"The Street Name: ({result.Address.StreetName})");
            Console.WriteLine($"The Coordinate: ({result.Position.Latitude:F4}, {result.Position.Longitude:F4})"); 
        }

        SearchAddressResult fuzzySearchResult = client.FuzzySearch( 
        "WellStar", new FuzzySearchOptions 
        { 
            Coordinates = new GeoPosition(-84.549934, 33.952602), // Marietta, GA
            Language = SearchLanguage.EnglishUsa 
        }); 

        Console.WriteLine("\n\n");
        // Print the search results 
        foreach (var result in fuzzySearchResult.Results) 
        { 
            Console.WriteLine($""" 
                * {result.Address.StreetNumber} {result.Address.StreetName} :: {result.Address.Municipality} {result.Address.CountryCode} {result.Address.PostalCode} 
                Coordinate: ({result.Position.Latitude:F4}, {result.Position.Longitude:F4}) 
                """); 
        } 

    }

    private async Task<string> ProcessImage(string imageURL, string prompt) // return Task<string>
    {
        Console.WriteLine("Gpt-4o Image URI :: " + imageURL);

        HttpClient httpClient = new HttpClient();
        httpClient.BaseAddress = new System.Uri("gpt4o uri");
        httpClient.DefaultRequestHeaders.ExpectContinue = false;
        httpClient.Timeout = TimeSpan.FromSeconds(200);
        // httpClient.DefaultRequestHeaders.Accept.Clear();
        // httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json;charset=UTF-8"));
        httpClient.DefaultRequestHeaders.Add("Api-Key", "key");
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "gpt-4o/chat/completions?api-version=2024-02-15-preview");
        var requestBody = "{\"temperature\":0,\"max_tokens\":1500,\"top_p\":1.0,\"messages\":[{\"role\":\"system\",\"content\":\"You are a helpful assistant that analyzes image.\"},{\"role\":\"user\",\"content\":[{\"type\":\"text\",\"text\":\"" +
                            prompt +
                            "\"},{\"type\":\"image_url\",\"image_url\":{\"url\":\"" +
                            imageURL +
                            "\"}}]}]}";
        request.Content = new StringContent(JsonConvert.SerializeObject(JObject.Parse(requestBody)), Encoding.UTF8, "application/json");        
        // request.Content.Headers.Add("Content-Type", "application/json;charset=UTF-8");

        var result = await httpClient.SendAsync(request);
        Console.WriteLine("Result :: " + result.StatusCode);
        var answer = await result.Content.ReadAsStringAsync();
        JObject resultContent = JObject.Parse(answer);
        var responseContent = resultContent["choices"][0]["message"]["content"].ToString();

        
        Console.WriteLine("Result Content :: " + responseContent);

        return responseContent;
    }

    private async Task<string> ProcessPhiImage(string imageURL, string prompt) // return Task<string>
    {
        Console.WriteLine("Phi Image URI :: " + imageURL);

        HttpClient httpClient = new HttpClient();
        httpClient.BaseAddress = new System.Uri("model uri");
        httpClient.DefaultRequestHeaders.ExpectContinue = false;
        httpClient.Timeout = TimeSpan.FromSeconds(200);
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer key");

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions");
        var requestBody = "{\"messages\": [ { \"role\": \"system\", \"content\": \"You are a helpful assistant.\" }, { \"role\": \"user\", \"content\": [ { \"type\": \"image_url\"," + 
                          "\"image_url\": { \"url\": \"" + imageURL + 
                          "\" } }, { \"type\": \"text\", \"text\": \"" + 
                          prompt + "\" } ] } ]}";
        request.Content = new StringContent(JsonConvert.SerializeObject(JObject.Parse(requestBody)), Encoding.UTF8, "application/json");

        var result = await httpClient.SendAsync(request);
        Console.WriteLine("Result :: " + result.StatusCode);
        var answer = await result.Content.ReadAsStringAsync();
        JObject resultContent = JObject.Parse(answer);
        var responseContent = resultContent["choices"][0]["message"]["content"].ToString();

        
        Console.WriteLine("Result Content :: " + responseContent);
        return responseContent;
    }
}
