namespace blog_generator;

public class GeneralConfig
{
    public string TemplatesFolder {get;set;} = "";
    public string WebResourceBase {get;set;} = "";
    public string WebsocketEndpoint {get;set;} = "";
    public string BlogFolder {get;set;} = "";
    public string StylesFolder {get;set;} = "";

    //These should be relative to the WebResourceBase
    public List<string> ScriptIncludes {get;set;} = new List<string>();
    public List<string> StyleIncludes {get;set;} = new List<string>();
}