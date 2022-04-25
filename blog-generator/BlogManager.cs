//using System.Text.RegularExpressions;
//using blog_generator.Configs;
//
//namespace blog_generator;
//
//public class BlogManager
//{
//    protected ILogger logger;
//    protected TemplateConfig templateConfig;
//    protected BlogPathGenerator pathGenerator;
//
//    public BlogManager(ILogger<BlogManager> logger, TemplateConfig templateConfig, BlogPathGenerator pathGenerator)
//    {
//        this.logger = logger;
//        this.templateConfig = templateConfig;
//        this.pathGenerator = pathGenerator;
//    }
//
//
//
//    public Task WriteStyle(string hash, string rawContents) => WriteAny(StylePath(hash), rawContents, "style");
//    public Task WriteBlogMain(string hash, string rawContents) => WriteAny(BlogMainPath(hash), rawContents, "blog-main");
//    public Task WriteBlogPage(string parentHash, string pageHash, string rawContents) => WriteAny(BlogPagePath(parentHash, pageHash), rawContents, "blog-page");
//
//    //Styles aren't important, we can keep those around forever really...
//}