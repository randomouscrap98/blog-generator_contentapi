using blog_generator.Configs;

namespace blog_generator;

public class BlogPathManager
{
    protected PathManagementConfig config;

    public BlogPathManager(PathManagementConfig config)
    {
        this.config = config;
    }

    protected string StylesFolder => Path.Join(config.LocalRoot, config.StylesFolder);
    protected string BlogFolder => Path.Join(config.LocalRoot, config.BlogFolder);

    public string LocalStylePath(string hash) => Path.Join(StylesFolder, $"{hash}.css");
    public string LocalBlogMainPath(string hash) => Path.Join(BlogFolder, hash, "index.html");
    public string LocalBlogPagePath(string hash, string pageHash) => Path.Join(BlogFolder, hash, $"{pageHash}.html");

    public string WebStylePath(string hash) => GetRootedWebResource($"{config.StylesFolder}/{hash}.css");
    public string WebBlogMainPath(string hash) => GetRootedWebResource($"{config.BlogFolder}/{hash}/index.html");
    public string WebBlogPagePath(string hash, string pageHash) => GetRootedWebResource($"{config.BlogFolder}/{hash}/{pageHash}.html");

    public string GetRootedWebResource(string resource) => $"{config.WebRoot}{resource}";

    public bool LocalStyleExists(string hash) => File.Exists(LocalStylePath(hash));
    public bool LocalBlogMainExists(string hash) => File.Exists(LocalBlogMainPath(hash));

    public string ImagePath(string hash) => $"{config.ImageRoot}/hash";

    public List<string> GetAllBlogHashes()
    {
        if(!Directory.Exists(BlogFolder))
            return new List<string>();

        return Directory.EnumerateDirectories(BlogFolder).Select(x => Path.GetFileName(x)).ToList();
    }
}