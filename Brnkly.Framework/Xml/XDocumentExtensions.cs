using System.Text;
using System.Xml.Linq;

namespace Brnkly.Framework.Xml
{
    public static class XDocumentExtensions
    {
        public static string ConvertXmlToUtf8String(this XDocument document)
        {
            var stringBuilder = new StringBuilder();
            var stringWriter = new Utf8StringWriter(stringBuilder);
            document.Save(stringWriter);

            return stringBuilder.ToString();
        }
    }
}
