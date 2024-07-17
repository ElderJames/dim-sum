using Microsoft.AspNetCore.Components.Web;

namespace SiteGenerator.Sample;

public static class TestCustomElementRegistion
{
    public static void RegisterCustomElements(this IJSComponentConfiguration rootComponents)
    {
        rootComponents.RegisterCustomElement<Demos.Components.Affix.demo.Basic>("my-counter");
    }
}