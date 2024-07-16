using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AntDesign.Docs.Services
{
    public class AssemblyService
    {
        public IEnumerable<Assembly> Assembly => _assemblies;

        private List<Assembly> _assemblies = [];


        public void AddAssembly(Assembly assembly)
        {
            _assemblies.Add(assembly);
        }
    }
}
