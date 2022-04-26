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
    protected TemplateLoader renderer;

    public const string ShareStylesKey = "share_styles";

    public BlogGenerator(ILogger<BlogGenerator> logger, TemplateConfig templateConfig, BlogPathManager pathManager, TemplateLoader renderer)
    {
        this.logger = logger;
        this.templateConfig = templateConfig;
        this.renderer = renderer;
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

    public string GetAuthorFromList(long id, IEnumerable<UserView> users) => users.FirstOrDefault(x => x.id == id)?.username ?? "???";

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
        //TODO: at some point, don't just outright delete it, go move it to some archive
        var indexPath = pathManager.LocalBlogMainPath(hash);
        var path = Path.GetDirectoryName(indexPath) ??
            throw new InvalidOperationException($"[DELETE]: Unable to compute blog directory for blog {indexPath}");

        if(Directory.Exists(path))
        {
            Directory.Delete(path, true);
            logger.LogWarning($"Deleted blog {hash}");
        }
        else
        {
            logger.LogDebug($"No blog at {path}, ignoring delete");
        }
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

    public List<string> GetStylesForParent(ContentView parent)
    {
        if(parent.values.ContainsKey(ShareStylesKey))
        {
            try
            {
                var styles = ((JObject)parent.values[ShareStylesKey]).ToObject<List<string>>() ?? 
                    throw new InvalidOperationException($"Couldn't cast {ShareStylesKey} to list!");
                return styles;
            }
            catch(Exception ex)
            {
                logger.LogWarning($"Couldn't parse the parent styles for {parent.hash}({parent.id}): {ex}");
            }
        }
        else
        {
            logger.LogDebug($"No styles for parent {parent.hash}({parent.id}) found when requested");
        }

        return new List<string>();
    }

    public async Task GenerateBlogpost(ContentView page, ContentView parent, List<ContentView> pages, List<UserView> users)
    {
        logger.LogDebug($"Generating blogpost: {page.hash}({page.id})");

        //This generates a single blogpost. It figures out how to generate it based on the data given. If the page itself IS the parent,
        //something else MAY be done.
        var templateData = new MainTemplateData()
        {
            scripts = templateConfig.ScriptIncludes.Select(x => pathManager.GetRootedWebResource(x)).ToList(),
            styles = templateConfig.StyleIncludes.Select(x => pathManager.GetRootedWebResource(x)).ToList(),
            revision_id = page.lastRevisionId,
            title = page.name,
            content = page.text,
            page_id = page.id,
            parent_id = parent.id,
            create_date = page.createDate,
            parent_title = parent.name,
            render_date = DateTime.UtcNow,
            author = GetAuthorFromList(page.createUserId, users)
        };

        templateData.styles.AddRange(GetStylesForParent(parent).Select(x => pathManager.WebStylePath(x)));

        templateData.navlinks = pages.OrderByDescending(x => x.createDate).Select(x => new NavigationItem()
        {
            text = x.name,
            link = pathManager.WebBlogPagePath(parent.hash, x.hash),
            current = x.id == page.id
        }).ToList();

        //Need to use mustache here to generate the template and write it
        var renderedPage = await renderer.RenderPageAsync("main", templateData);

        var path = page.id == parent.id ? pathManager.LocalBlogMainPath(parent.hash) : pathManager.LocalBlogPagePath(parent.hash, page.hash);
        await WriteAny(path, renderedPage, "page");
    }

    public async Task GenerateFullBlog(ContentView parent, List<ContentView> pages, List<UserView> users)
    {
        //First, delete the original blog to make things easy
        DeleteBlog(parent.hash);

        //Then, generate the blog for the parent
        await GenerateBlogpost(parent, parent, pages, users);

        //Then just one for each
        foreach(var page in pages)
        {
            await GenerateBlogpost(page, parent, pages, users);
        }
    }

    public async Task GenerateStyle(ContentView style, List<UserView> users)
    {
        logger.LogDebug($"Generating style: {style.hash}({style.id})");

        var templateData = new StyleData()
        {
            revision_id = style.lastRevisionId,
            page_id = style.id,
            render_date = DateTime.UtcNow,
            raw_style = style.text,
            author = GetAuthorFromList(style.createUserId, users)
        };

        //Need to use mustache here to generate the template and write it
        var renderedPage = await renderer.RenderPageAsync("style", templateData);

        var path = pathManager.LocalStylePath(style.hash);
        await WriteAny(path, renderedPage, "style");
    }
}