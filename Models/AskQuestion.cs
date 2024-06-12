namespace dotnetmvc.Models;

public class AskQuestion
{
    public string Question { get; set; }

    public string Answer { get; set; }

    public string ImageURL { get; set; }
    public string LanguageModel { get; set; }

    public AskQuestion()
    {
        Question = "";
        Answer = "Answer";
        ImageURL = "";
        LanguageModel = "";
    }
}
