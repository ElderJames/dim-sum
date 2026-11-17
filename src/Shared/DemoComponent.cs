using System;
using System.Collections.Generic;

namespace AntDesign.Docs
{
    public class DemoComponent
    {
        public string Category { get; set; }
        public string Title { get; set; }

        public string SubTitle { get; set; }

        public string Type { get; set; }

        public string Desc { get; set; }

        public string ApiDoc { get; set; }

        public int? Cols { get; set; }

        public string Cover { get; set; }

        public List<DemoItem> DemoList { get; set; }
    }

    public class DemoItem
    {
        public double Order { get; set; }

        public string Name { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Code { get; set; }

        /// <summary>Compile-time renderer emitted by the source generator.</summary>
        public object Component { get; set; }

        public string SourcePath { get; set; }

        // Generator-only type name. SourceCodeGenerator consumes it to emit Component.
        public string TypeName { get; set; }

        public string Style { get; set; }

        public int? Iframe { get; set; }

        public string Link { get; set; }

        public bool? Docs { get; set; }

        public bool Debug { get; set; }

        public Dictionary<string, string> AdditionalFiles { get; set; }
    }
}
