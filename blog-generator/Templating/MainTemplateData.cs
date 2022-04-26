namespace blog_generator;

public class MainTemplateData
{
    public long page_id {get;set;}
    public long parent_id {get;set;}
    public long revision_id {get;set;}
    public string title {get;set;} = "";
    public string author {get;set;} = "???";
    public DateTime create_date {get;set;} 
    public string parent_title {get;set;} = "";
    public string content {get;set;} = "";
    public List<string> scripts {get;set;} = new List<string>();
    public List<string> styles {get;set;} = new List<string>();

    public List<NavigationItem> navlinks {get;set;} = new List<NavigationItem>();

    public DateTime render_date {get;set;}
    public string? theme_color {get;set;}

    public PageIcon? icon {get;set;} = null;
}
