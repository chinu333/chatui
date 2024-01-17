namespace dotnetmvc.Models;

public class AskQuestion
{
    public string Question { get; set; }

    public string Answer { get; set; }

    public AskQuestion()
    {
        Question = "";
        Answer = "Answer";
    }
}