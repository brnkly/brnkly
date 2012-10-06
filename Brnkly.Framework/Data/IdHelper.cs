using System;
using System.Globalization;
using Raven.Client.Document;

namespace Brnkly.Framework.Data
{
    public class IdHelper
    {
        public static Func<Type, string> DefaultBehavior { get; set; }

        static IdHelper()
        {
            DefaultBehavior = DocumentConvention.DefaultTypeTagName;
        }

        public static string For<TModel>(string uniqueId)
        {
            var prefix = DefaultBehavior(typeof(TModel));
            return string.Concat(prefix, "/", uniqueId);
        }

        public static string For<TModel>(int uniqueId)
        {
            return For<TModel>(uniqueId.ToString(CultureInfo.InvariantCulture));
        }
    }
}