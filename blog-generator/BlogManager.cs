using System.Text.RegularExpressions;
using blog_generator.Configs;

namespace blog_generator;

public class BlogManager
{
    protected ILogger logger;
    protected TemplateConfig templateConfig;

    public BlogManager(ILogger<BlogManager> logger, TemplateConfig templateConfig)
    {
        this.logger = logger;
        this.templateConfig = templateConfig;
    }

    public string StylePath(string hash) => Path.Join(templateConfig.StylesFolder, $"{hash}.css");
    public bool StyleExists(string hash) => File.Exists(StylePath(hash));
    public string BlogMainPath(string hash) => Path.Join(templateConfig.BlogFolder, hash, "index.html");
    public string BlogPagePath(string hash, string pageHash) => Path.Join(templateConfig.BlogFolder, hash, $"{pageHash}.html");
    public bool BlogMainExists(string hash) => File.Exists(BlogMainPath(hash));

    public List<string> GetAllBlogHashes()
    {
        return Directory.EnumerateDirectories(templateConfig.BlogFolder).Select(x => Path.GetFileName(x)).ToList();
    }

    public async Task<bool> ShouldRegenStyle(string hash, long revisionId)
    {
        if(!StyleExists(hash))
            return true;

        return !Regex.IsMatch(await File.ReadAllTextAsync(StylePath(hash)), @$"^/\*{revisionId}\*/");
    }

    public async Task<bool> ShouldRegenBlog(string hash, long revisionId)
    {
        if(!BlogMainExists(hash))
            return true;

        var lines = await File.ReadAllLinesAsync(BlogMainPath(hash));

        //First line doctype, next html
        return !Regex.IsMatch(lines[2], @$"^<!--{revisionId}-->");
    }

    private Task WriteAny(string path, string rawContents, string type)
    {
        //First, create the directory
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? 
            throw new InvalidOperationException($"Unable to create {type} directory for {type} {path}"));

        //Then, just... write the data!
        return File.WriteAllTextAsync(path, rawContents);
    }

    public Task WriteStyle(string hash, string rawContents) => WriteAny(StylePath(hash), rawContents, "style");
    public Task WriteBlogMain(string hash, string rawContents) => WriteAny(BlogMainPath(hash), rawContents, "blog-main");
    public Task WriteBlogPage(string parentHash, string pageHash, string rawContents) => WriteAny(BlogPagePath(parentHash, pageHash), rawContents, "blog-page");
}