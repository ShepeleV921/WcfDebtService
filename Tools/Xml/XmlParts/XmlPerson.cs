using System;
using Tools.Classes;

namespace Tools.Xml
{
    public class XmlPerson
    {
        public string FullName { get; set; }

        public string RegType { get; set; }

        public string RegName { get; set; }

        public string INN { get; set; }

        public string SNILS { get; set; }

        public DateTime RegDate { get; set; }

        public Fraction Fraction { get; set; }

        public string FullFraction { get; set; }  // Если доля собственности пришла непарсируемым представлением
    }
}
