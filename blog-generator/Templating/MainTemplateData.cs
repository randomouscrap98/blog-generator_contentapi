namespace blog_generator;

public class MainTemplateData
{
    public long pageId {get;set;}
    public long parentId {get;set;}
    public long revisionId {get;set;}
    public string title {get;set;} = "";
    public string author {get;set;} = "???";
    public DateTime create_date {get;set;} 
    public string parent_title {get;set;} = "";
    public string content {get;set;} = "";
    public List<string> scripts {get;set;} = new List<string>();
    public List<string> styles {get;set;} = new List<string>();

    public List<string> navlinks {get;set;} = new List<string>();

    public string? theme_color {get;set;}

    public PageIcon? icon {get;set;} = null;
}
