using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using Tools.DAL;

namespace Tools.Xml
{
    public partial class XmlParserFactory
    {
        private Dictionary<string, string> _xmlHrefs;


        public IXmlReestrParser GetReestrParser(XmlDocument doc)
        {
            if (_xmlHrefs == null)
            {
                _xmlHrefs = new Dictionary<string, string>();
                foreach (var pair in Repository.GetXmlHrefs())
                {
                    _xmlHrefs.Add(pair.Key, pair.Value);
                }
            }

            XmlProcessingInstruction instruction = doc.SelectSingleNode(
                "//processing-instruction(\"xml-stylesheet\")") as XmlProcessingInstruction;

            if (instruction == null)
            {
                string Href = "1";
                Type FactoryType = typeof(XmlParserFactory);
                Type[] InnerTypes = FactoryType.GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic);
                Type ParserType = InnerTypes.FirstOrDefault(x => x.Name == _xmlHrefs[Href]);
                ConstructorInfo Ctr = ParserType.GetConstructor(
                BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                new Type[] { typeof(XmlDocument), typeof(string) },
                null);

                object Res = Ctr.Invoke(new object[] { doc, Href });

                return (IXmlReestrParser)Res;


            }

            int hrefIndex = 0;
            if (instruction == null ||
                string.IsNullOrEmpty(instruction.Value) ||
                (hrefIndex = instruction.Value.IndexOf("href=\"", StringComparison.InvariantCultureIgnoreCase)) < 0)
                throw new InvalidOperationException("Не найдена ссылка на таблицу преобразования XSLT");

            hrefIndex += 6;
            string href = instruction.Value.Substring(hrefIndex, instruction.Value.Length - hrefIndex);
            href = href.Trim('\"', ' ');

            if (!_xmlHrefs.ContainsKey(href))
                throw new InvalidOperationException("Обнаружена новая ссылка XSLT преобразования: " + href +
                    "\nОбратитесь к разработчикам для учёта этой ссылки в обработке xml-файла");

            Type factoryType = typeof(XmlParserFactory);
            Type[] innerTypes = factoryType.GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic);
            Type parserType = innerTypes.FirstOrDefault(x => x.Name == _xmlHrefs[href]);

            if (parserType == null)
                throw new InvalidOperationException("Не найден обработчик xml-документа для ссылки " + href);

            ConstructorInfo ctr = parserType.GetConstructor(
                BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                new Type[] { typeof(XmlDocument), typeof(string) },
                null);

            object res = ctr.Invoke(new object[] { doc, href });

            return (IXmlReestrParser)res;
        }
    }
}
