using System;
using System.Collections.Generic;

namespace Tools.Xml
{
    public interface IXmlReestrParser
    {
        string XslHref { get; }

        string RequeryNumber { get; }

        DateTime RequeryDate { get; }

        List<XmlPerson> Persons { get; }

        List<XmlGovernance> Governances { get; }

        List<XmlOrganization> Organizations { get; }

        List<XmlResident> Resident { get; }

        List<XmlMunicipality> Municipality { get; }

        XmlBuildingInfo BuildingInfo { get; }

        string GetHtmlText();


        int DebugMark { get; }
    }
}
