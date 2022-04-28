# blog-generator_contentapi
A simple static-rendering blog generator which connects directly to a contentapi instance

## How to configure pages
- Create a parent page which will house your blog
  - set `"share"=true` in the values
  - set `"share_styles"=["hash1","hash1"]` to add custom styles to your page (not required). Must use the page hash, the page can exist anywhere
  - set meta theme color (tab color) with `"share_theme_color"="#FEDEFF"` etc (not required)
  - set favicon with `"share_favicon"="https://full/path/to/favicon"` (not required). No resizing is done: please use small file or add resize parameters to raw file. **Must be png**
  - The link to your blog is the hash of the parent page. You can set hashes on page create
  - The title of your blog is the name of the parent page, which you can change at any time
- All child pages **that are resource types** are automatically part of your blog (set `literalType="resource"`)
  - Child pages are at /parent-hash/child-hash, again you can set hashes only on page create
- Child pages must be of `literalType="resource"` to identify them as a page in the blog. All other types are ignored

### Additional:
- The description on content becomes the meta description, used by crawlers. Set description on any page, including children
- The keywords on content becomes the meta keywords. Again, all pages, including children
- The original author and create date for each page are used anywhere it is displayed. The edit user and date is not considered
