using System.Collections.Generic;
using System.Xml;

namespace Tools.Xml
{
    public partial class XmlParserFactory
    {
        private class ReestrExtractBigOks : ReestrExtractBig
        {
            public override List<XmlPerson> Persons { get; protected set; }

            public override List<XmlGovernance> Governances { get; protected set; }

            public override List<XmlOrganization> Organizations { get; protected set; }


            internal ReestrExtractBigOks(XmlDocument doc, string href)
                : base(doc, href)
            {

            }
        }
    }
}
