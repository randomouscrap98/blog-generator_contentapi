namespace blog_generator.Configs;

public class TemplateConfig
{
    public string TemplatesFolder {get;set;} = "";
    public string WebResourceBase {get;set;} = "";
    public string BlogFolder {get;set;} = "";
    public string StylesFolder {get;set;} = "";

    //These should be relative to the WebResourceBase
    public List<string> ScriptIncludes {get;set;} = new List<string>();
    public List<string> StyleIncludes {get;set;} = new List<string>();
}