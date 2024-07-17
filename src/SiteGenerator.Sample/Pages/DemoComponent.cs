using AntDesign.Docs;
using AntDesign.Docs.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace SiteGenerator.Sample.Pages;
public class DemoComponent : ComponentBase
{
    private static DemoItem demo = new()
    {
        Name = "basic",
        Description = "",
        Code = "<div>hello</div>",
        Type = typeof(Demos.Components.Affix.demo.Basic),
        Style = "",
        Iframe = 360,
        Link = "http://",
        Docs = false,
        Debug = false,
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CodeBox>(0);

        builder.AddAttribute(1, nameof(CodeBox.ComponentName), "affix");
        builder.AddAttribute(2, nameof(CodeBox.Demo), demo);

        builder.CloseComponent();
    }
}