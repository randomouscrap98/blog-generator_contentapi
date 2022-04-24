namespace blog_generator;

public class BlogGenerator
{
    protected ILogger<BlogGenerator> logger;

    public BlogGenerator(ILogger<BlogGenerator> logger)
    {
        this.logger = logger;
    }
}