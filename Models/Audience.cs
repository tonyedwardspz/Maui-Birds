namespace Maui_Birds.Models;

public class Audience
{
    public int Paricipation { get; set; }
    public bool? IsAwesome { get; set; }

    public Audience(int participation)
    {
        Paricipation = participation;
    }
}