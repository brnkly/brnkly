using System.Collections.ObjectModel;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace Brnkly.Framework.Xml
{
    public class MarkupCleaner
    {
        private static readonly Collection<string> Whitelist;

        static MarkupCleaner()
        {
            Whitelist = new Collection<string>
                            {
                                "a",
                                "br",
                                "strong",
                                "em",
                                "b",
                                "p",
                                "ul",
                                "ol",
                                "li",
                                "div",
                                "strike",
                                "u",
                                "sub",
                                "sup",
                                "table",
                                "tr",
                                "td",
                                "th",
                            };
        }

        public static string DecodeEntities(string text)
        {
            return HttpUtility.HtmlDecode(text);
        }

        public static void RemoveNonDataAttributesFromTopLevelElements(XElement xml)
        {
            var nonDataAttributes =
                xml.Elements()
                    .Attributes()
                    .Where(attribute => !attribute.Name.LocalName.StartsWith("data-"))
                    .ToList();

            nonDataAttributes.Remove();
        }

        public static void AddTargetBlankToLinks(XElement xml)
        {
            xml.Descendants("a")
                .Where(a => a.Attribute("target") == null &&
                            a.Attribute("href") != null)
                .ToList()
                .ForEach(a =>
                    a.Add(new XAttribute("target", "_blank")));
        }

        public static void RemoveElementsNotInWhitelist(XElement xml)
        {
            RemoveElementsNotInWhitelist(xml, Whitelist);
        }

        public static void RemoveElementsNotInWhitelist(XElement xml, Collection<string> whitelist)
        {
            var badTags =
                xml.Descendants()
                    .Where(d => !whitelist.Contains(d.Name.ToString()))
                    .Select(d => d).ToList();

            foreach (var tag in badTags)
            {
                var nodes = tag.Nodes();
                tag.ReplaceWith(nodes);
            }
        }

    }
}