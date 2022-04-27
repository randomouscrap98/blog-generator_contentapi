namespace blog_generator;

public class NavigationItem
{
    public bool current {get;set;}
    public string link {get;set;} = "";
    public string text {get;set;} = "<NO TITLE>";
    public DateTime? create_date {get;set;}

    public string? create_date_str => Constants.ShortIsoFormat(create_date);
}