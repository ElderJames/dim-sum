using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntDesign.Docs
{
    public class SiteGeneratorOptions
    {
        public SiteGeneratorOptions()
        {
            Menus = [];
        }

        private Action<IJSComponentConfiguration> _componentConfigAction;

        public Dictionary<string, List<DemoMenuItem>> Menus { get; set; }

        public void ConfigComponent(Action<IJSComponentConfiguration> options)
        {
            _componentConfigAction = options;
        }

        internal void SetRootComponents(IJSComponentConfiguration rootComponents)
        {
            if (_componentConfigAction != null)
            {
                _componentConfigAction(rootComponents);
            }
        }
    }
}
