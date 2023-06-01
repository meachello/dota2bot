namespace TgBOT;

public class Commands
{
    public string currentStage = "menu";
    public void ChangeStage(string stage)
    {
        currentStage = stage;
    }
}