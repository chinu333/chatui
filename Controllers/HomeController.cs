using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using dotnetmvc.Models;
using System.Text;
using System.Runtime.CompilerServices;

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
        // string url = "http://localhost:7071/api/MyChatFunction";
        string url = "";

        Console.WriteLine("Question from the model :: " + q.Question);
        ModelState.Clear();

        using HttpClient client = new();

        HttpResponseMessage response = await client.PostAsync(
            requestUri: url,
            content: new StringContent(question, Encoding.UTF8, "text/plain"));

        var answer = await response.Content.ReadAsStringAsync();
        q.Answer = answer;
        q.Question = "";

        Console.WriteLine($"\nAI: {answer}");

        return View(q);
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
