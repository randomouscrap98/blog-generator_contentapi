namespace blog_generator.Configs;

public class PathManagementConfig
{
    public string BlogFolder {get;set;} = "";
    public string StylesFolder {get;set;} = "";

    /// <summary>
    /// The location from which the blog and styles are relative to inside the actual filesystem
    /// </summary>
    /// <value></value>
    public string LocalRoot {get;set;} = "";

    /// <summary>
    /// The location on the server from which the blog and styles are relative to
    /// </summary>
    /// <value></value>
    public string WebRoot {get;set;} = "";
    
    public string ImageRoot {get;set;} = "";
}