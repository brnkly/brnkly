using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Brnkly.Framework.Xml
{
    public static class XElementExtensions
    {
        public static XElement ReplaceRootElementWith(this XElement element, string newRootElementName)
        {
            var newRoot = new XElement(newRootElementName);
            foreach (var el in element.Elements())
            {
                newRoot.Add(el);
            }

            return newRoot;
        }

        public static T GetValue<T>(this XElement element)
        {
            string result = element.Value;
            if (string.IsNullOrEmpty(result))
            {
                return default(T);
            }
            return (T)Convert.ChangeType(result, typeof(T));
        }

        public static T GetValue<T>(this XElement element, string xpath)
        {
            var namespaceManager = new XmlNamespaceManager(new NameTable());
            var result = element.XPathEvaluate(xpath, namespaceManager);

            if (result is IEnumerable)
            {
                result = GetElementOrAttributeValue(
                    (result as IEnumerable).Cast<XObject>().FirstOrDefault());
            }

            return result == null ? default(T) : (T)Convert.ChangeType(result, typeof(T));
        }

        public static string GetAllText(this XElement element)
        {
            var text = element
                .DescendantNodes()
                .OfType<XText>()
                .Select(n => n.ToString());

            return string.Join(" ", text);
        }

        public static string GetInnerXml(this XElement element)
        {
            var reader = element.CreateReader();
            reader.MoveToContent();
            return reader.ReadInnerXml();
        }

        public static string GetAttributeValue(this XElement element, string name)
        {
            var attr = element.Attribute(name);

            return attr != null
                       ? attr.Value
                       : null;
        }

        private static string GetElementOrAttributeValue(XObject node)
        {
            if (node == null)
            {
                return null;
            }

            if (node is XElement)
            {
                return (string)(XElement)node;
            }

            if (node is XAttribute)
            {
                return (string)(XAttribute)node;
            }

            return null;
        }

        public static IEnumerable<XElement> GetElementsWithValueLongerThan(
            this XElement element,
            string elementNameToSearchFor,
            int characterCount)
        {
            return element
                .Elements(elementNameToSearchFor)
                .Where(el => el.Value.Length > characterCount)
                .ToList();
        }
    }
}
