using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntDesign.Docs.Demos.Pages;

[Route("/docs/page")]
public class PageComponent : ComponentBase
{
    string html = "<div>hello world</div>";
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddMarkupContent(0, html);
    }
}
