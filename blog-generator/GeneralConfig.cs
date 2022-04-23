namespace blog_generator;

public class GeneralConfig
{
    public string TemplatesFolder {get;set;} = "";
    public string WebsocketEndpoint {get;set;} = "";
    public string BlogFolder {get;set;} = "";
    public string StylesFolder {get;set;} = "";
    public List<string> ExtraStyleIncludes {get;set;} = new List<string>();
    public List<string> ExtraScriptIncludes {get;set;} = new List<string>();
}