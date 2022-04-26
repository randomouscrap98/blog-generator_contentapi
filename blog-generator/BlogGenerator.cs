using System.Text.RegularExpressions;
using blog_generator.Configs;
using contentapi.data.Views;
using Newtonsoft.Json.Linq;

namespace blog_generator;

public class BlogGenerator
{
    protected ILogger<BlogGenerator> logger;
    protected BlogPathManager pathManager;
    protected TemplateConfig templateConfig;

    public const string ShareStylesKey = "share_styles";

    public BlogGenerator(ILogger<BlogGenerator> logger, TemplateConfig templateConfig, BlogPathManager pathManager)
    {
        this.logger = logger;
        this.templateConfig = templateConfig;
        this.pathManager = pathManager;
    }

    public async Task<bool> ShouldRegenStyle(string hash, long revisionId)
    {
        if(!pathManager.LocalStyleExists(hash))
            return true;

        return !Regex.IsMatch(await File.ReadAllTextAsync(pathManager.LocalStylePath(hash)), @$"^/\*{revisionId}\*/");
    }

    public async Task<bool> ShouldRegenBlog(string hash, long revisionId)
    {
        if(!pathManager.LocalBlogMainExists(hash))
            return true;

        var lines = await File.ReadAllLinesAsync(pathManager.LocalBlogMainPath(hash));

        //First line doctype, next html
        return !Regex.IsMatch(lines[2], @$"^<!--{revisionId}-->");
    }

    private Task WriteAny(string path, string rawContents, string type)
    {
        //First, create the directory
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? 
            throw new InvalidOperationException($"[CREATE]: Unable to compute {type} directory for {type} {path}"));
        
        logger.LogInformation($"Writing {type} to {path}, length: {rawContents.Length}");

        //Then, just... write the data!
        return File.WriteAllTextAsync(path, rawContents);
    }

    public void DeleteBlog(string hash)
    {
        var path = pathManager.LocalBlogMainPath(hash);
        Directory.Delete(Path.GetDirectoryName(path) ??
            throw new InvalidOperationException($"[DELETE]: Unable to compute blog directory for blog {path}"), true);
        logger.LogWarning($"Deleted blog {hash}");
    }

    /// <summary>
    /// Remove any blogs from the system that aren't in the given list of hashes
    /// </summary>
    /// <param name="allBlogHashes"></param>
    public void CleanupMissingBlogs(IEnumerable<string> allBlogHashes)
    {
        var existingBlogs = pathManager.GetAllBlogHashes();
        var removeHashes = existingBlogs.Except(allBlogHashes);

        logger.LogInformation($"Removing {removeHashes.Count()} blogs on the system which are no longer configured to be blogs");

        foreach(var remHash in removeHashes)
            DeleteBlog(remHash);
    }

    public async Task GenerateBlogpost(ContentView page, ContentView parent, List<ContentView> pages, List<UserView> users)
    {
        //This generates a single blogpost. It figures out how to generate it based on the data given. If the page itself IS the parent,
        //something else MAY be done.
        var templateData = new MainTemplateData()
        {
            scripts = templateConfig.ScriptIncludes.Select(x => pathManager.GetRootedWebResource(x)).ToList(),
            styles = templateConfig.StyleIncludes.Select(x => pathManager.GetRootedWebResource(x)).ToList(),
            revisionId = page.lastRevisionId,
            title = page.name,
            content = page.text,
            pageId = page.id,
            parentId = parent.id,
            create_date = page.createDate,
            parent_title = parent.name,
            author = users.FirstOrDefault(x => x.id == page.createUserId)?.username ?? "???"
        };

        if(parent.values.ContainsKey(ShareStylesKey))
        {
            try
            {
                var styles = ((JObject)parent.values[ShareStylesKey]).ToObject<List<string>>() ?? 
                    throw new InvalidOperationException($"Couldn't cast {ShareStylesKey} to list!");
                templateData.styles.AddRange(styles.Select(x => pathManager.WebStylePath(x)));
            }
            catch(Exception ex)
            {
                logger.LogWarning($"Couldn't parse the parent styles: {ex}");
            }
        }

        //Need to use mustache here to generate the template and write it
    }
}