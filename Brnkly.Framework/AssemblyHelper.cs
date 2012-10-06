using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Compilation;

namespace Brnkly.Framework
{
    internal static class AssemblyHelper
    {
        private static readonly string[] ValidAssemblyPrefixes = 
        {
            "Brnkly.",
        };

        public static IEnumerable<Assembly> GetAssemblies()
        {
            var assemblies = BuildManager.GetReferencedAssemblies().Cast<Assembly>()
                .Where(assembly => assembly.FullName.StartsWithAny(ValidAssemblyPrefixes));
            return assemblies;
        }

        public static IEnumerable<Type> GetConstructableTypes<T>()
        {
            var types = GetAssemblies()
                .SelectMany(assembly => assembly.GetExportedTypes())
                .Where(type => !type.IsAbstract && typeof(T).IsAssignableFrom(type))
                .ToArray();
            return types;
        }
    }
}
