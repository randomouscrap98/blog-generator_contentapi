using contentapi.data.Views;

namespace blog_generator;

public class MainTemplateData
{
    public ContentView? page {get;set;}
    public ContentView? parent {get;set;}
    public List<string> scripts {get;set;} = new List<string>();
    public List<string> styles {get;set;} = new List<string>();

    public List<NavigationItem> navlinks {get;set;} = new List<NavigationItem>();

    public DateTime render_date {get;set;}
    public string author {get;set;} = "???";
    public string keywords {get;set;} = "";
    public bool is_parent {get;set;}

    public PageIcon? icon {get;set;} = null;

    public string? render_date_str => Constants.ShortIsoFormat(render_date);
    public string? page_create_date_str => Constants.ShortIsoFormat(page?.createDate);
}
