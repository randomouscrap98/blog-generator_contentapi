window.onload = function()
{
    var rawText = content_raw.textContent;
    var markupLang = 
        content_rendered.getAttribute("data-markup") ||
        content_rendered.getAttribute("data-markuplang") ||
        "plaintext";

    var rendered = Markup.convert_lang(rawText, markupLang, content_rendered);
    //content_rendered.appendChild(rendered); 

    content_rendered.style = "";
    content_raw.style.display = "none";
};