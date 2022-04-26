namespace blog_generator;

public class StyleData
{
    public long revision_id {get;set;}
    public string author {get;set;} = "";
    public long page_id {get;set;}
    public DateTime render_date {get;set;} = DateTime.UtcNow;
    public string raw_style {get;set;} = "";
}