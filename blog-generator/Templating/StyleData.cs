namespace blog_generator;

public class StyleData
{
    public long revisionId {get;set;}
    public string author {get;set;} = "";
    public long pageId {get;set;}
    public DateTime renderDate {get;set;} = DateTime.UtcNow;
    public string rawStyle {get;set;} = "";
}