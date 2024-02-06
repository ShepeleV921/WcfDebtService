using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;
using System.Globalization;
using Tools.Classes;
using System.IO;


namespace Tools.Xml
{
    public partial class XmlParserFactory
    {
        private class ReestrExtractNewType : XmlReestr
        {
            public override List<XmlPerson> Persons { get; protected set; }

            public override List<XmlGovernance> Governances { get; protected set; }

            public override List<XmlOrganization> Organizations { get; protected set; }
            public override List<XmlResident> Resident { get; protected set; }
            public override List<XmlMunicipality> Municipality { get; protected set; }
            public override XmlBuildingInfo BuildingInfo { get; protected set; }


            internal ReestrExtractNewType(XmlDocument doc, string href)
                : base(doc, href)
            {
                Persons = new List<XmlPerson>();
                Governances = new List<XmlGovernance>();
                Organizations = new List<XmlOrganization>();
                Resident = new List<XmlResident>();
                Municipality = new List<XmlMunicipality>();

                foreach (XmlElement elem in _document.DocumentElement.SelectNodes(
                    "//x:right_records/x:right_record", _nsManager))
                {
                    XmlNodeList xPersons = elem.SelectNodes("x:right_holders/x:right_holder/x:individual", _nsManager); //Актуально 23.03.23
                    XmlNodeList xMunicipality = elem.SelectNodes("x:right_holders/x:right_holder/x:public_formation/x:public_formation_type/x:municipality", _nsManager);
                    XmlNodeList xResident = elem.SelectNodes("x:right_holders/x:right_holder/x:legal_entity/x:entity/x:resident", _nsManager);
                    XmlNode xRegistr = elem.SelectSingleNode("x:right_data", _nsManager); //Актуально 23.03.23


                    XmlNode surname = elem.SelectSingleNode("x:right_holders/x:right_holder/x:individual/x:surname", _nsManager); //Актуальное до 23.03.23
                    XmlNode name = elem.SelectSingleNode("x:right_holders/x:right_holder/x:individual/x:name", _nsManager); //Актуально 23.03.23
                    XmlNode patronymic = elem.SelectSingleNode("x:right_holders/x:right_holder/x:individual/x:patronymic", _nsManager); //Актуальное до 23.03.23
                    XmlNode date = elem.SelectSingleNode("x:record_info/x:registration_date", _nsManager);

                    string FIO = null;
                    string Date = date.InnerText;
                    Date = Date.Substring(0, 10);
                    //DateTime Date = (DateTime)date;

                    if (surname != null && name != null && patronymic != null)
                        FIO = surname.InnerText + " " + name.InnerText + " " + patronymic.InnerText;
                    else if (name != null)
                        FIO = name.InnerText;

                    XmlNode Right_Data_numerator = null;
                    XmlNode Right_Data_denominator = null;
                    XmlNode Right_Data_code = null;
                    XmlNode Right_Data_value = null;
                    XmlNode Right_Data_number = null;


                    if (xRegistr != null) //Актуально 23.03.23
                    {
                        Right_Data_numerator = xRegistr.SelectSingleNode("x:shares/x:share/x:numerator", _nsManager);
                        Right_Data_denominator = xRegistr.SelectSingleNode("x:shares/x:share/x:denominator", _nsManager);
                        Right_Data_code = xRegistr.SelectSingleNode("x:right_type/x:code", _nsManager);
                        Right_Data_value = xRegistr.SelectSingleNode("x:right_type/x:value", _nsManager);
                        Right_Data_number = xRegistr.SelectSingleNode("x:right_number", _nsManager);
                    }


                    foreach (XmlNode node in xPersons)
                    {

                        XmlPerson tmp = new XmlPerson
                        {
                            RegType = Right_Data_code?.InnerText,
                            RegName = Right_Data_number?.InnerText,
                            RegDate = DateTime.ParseExact(Date, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                            FullName = FIO,
                        };

                        // Доли собственности
                        int numerator = 1;
                        int denominator = 1;

                        if (Right_Data_code.Value == XmlParserFactory.COMMON_OWNERSHIP_TYPE) // Совместная собственность
                            denominator = xPersons.Count;

                        if (Right_Data_numerator != null && Right_Data_denominator != null)
                        {
                            numerator = Convert.ToInt32(Right_Data_numerator.InnerText);
                            denominator = Convert.ToInt32(Right_Data_denominator.InnerText);
                            tmp.Fraction = new Fraction(numerator, denominator);
                        }
                        else
                        {
                            tmp.Fraction = new Fraction(numerator, denominator);
                            tmp.FullFraction = "Доли не обработаны";
                        }

                        Persons.Add(tmp);
                    }

                    if (xMunicipality.Count > 0 && xResident.Count == 0)
                    {
                        foreach (XmlElement node in xMunicipality)
                        {
                            XmlNode Name = node.SelectSingleNode("x:name", _nsManager);

                            XmlMunicipality tmp = new XmlMunicipality
                            {
                                RegType = Right_Data_code?.InnerText,
                                RegName = Right_Data_number?.InnerText,
                                RegDate = DateTime.ParseExact(Date, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                                FullName = Name.InnerText,
                            };

                            // Доли собственности
                            int numerator = 1;
                            int denominator = 1;

                            if (Right_Data_code.Value == XmlParserFactory.COMMON_OWNERSHIP_TYPE) // Совместная собственность
                                denominator = xPersons.Count;

                            if (Right_Data_numerator != null && Right_Data_denominator != null)
                            {
                                numerator = Convert.ToInt32(Right_Data_numerator.InnerText);
                                denominator = Convert.ToInt32(Right_Data_denominator.InnerText);
                                tmp.Fraction = new Fraction(numerator, denominator);
                            }
                            else
                            {
                                tmp.Fraction = new Fraction(numerator, denominator);
                                tmp.FullFraction = "Доли не обработаны";
                            }

                            Municipality.Add(tmp);
                        }
                    }

                    if (xResident.Count > 0)
                    {
                        foreach (XmlNode node in xResident)
                        {
                            XmlNode Name = node.SelectSingleNode("x:name", _nsManager);
                            XmlNode Inn = node.SelectSingleNode("x:inn", _nsManager);

                            XmlResident tmp = new XmlResident
                            {
                                RegType = Right_Data_code?.InnerText,
                                RegName = Right_Data_number?.InnerText,
                                RegDate = DateTime.ParseExact(Date, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                                FullName = Name.InnerText,
                                INN = Inn != null ? Inn.InnerText : null,
                            };

                            // Доли собственности
                            int numerator = 1;
                            int denominator = 1;

                            if (Right_Data_code.Value == XmlParserFactory.COMMON_OWNERSHIP_TYPE) // Совместная собственность
                                denominator = xPersons.Count;

                            if (Right_Data_numerator != null && Right_Data_denominator != null)
                            {
                                numerator = Convert.ToInt32(Right_Data_numerator.InnerText);
                                denominator = Convert.ToInt32(Right_Data_denominator.InnerText);
                                tmp.Fraction = new Fraction(numerator, denominator);
                            }
                            else
                            {
                                tmp.Fraction = new Fraction(numerator, denominator);
                                tmp.FullFraction = "Доли не обработаны";
                            }

                            Resident.Add(tmp);
                        }
                    }
                }

                SetBuildingInfo();
            }

            internal virtual void SetBuildingInfo()
            {
                XmlElement buildingXml = (XmlElement)_document.DocumentElement.SelectSingleNode("//x:build_record", _nsManager);
                if (buildingXml != null)
                {
                    BuildingInfo = new XmlBuildingInfo();

                    XmlNode objCadastralNum = buildingXml.SelectSingleNode("x:object/x:common_data/x:cad_number", _nsManager);
                    if (objCadastralNum != null)
                        BuildingInfo.CadastralNum = objCadastralNum.InnerText;

                    XmlNode objType = buildingXml.SelectSingleNode("x:object/x:common_data/x:type/x:code", _nsManager);
                    if (objType != null)
                        BuildingInfo.Type = objType.InnerText;

                    XmlNode area = buildingXml.SelectSingleNode("x:params/x:area", _nsManager);
                    if (area != null)
                        BuildingInfo.Area = area.InnerText;

                    XmlNode addr = buildingXml.SelectSingleNode("x:params", _nsManager);
                    if (addr != null)
                    {

                    }

                }
                XmlElement buildingXml_new = (XmlElement)_document.DocumentElement.SelectSingleNode("//x:room_record", _nsManager);
                if (buildingXml_new != null)
                {
                    BuildingInfo = new XmlBuildingInfo();

                    XmlNode objCadastralNum = buildingXml_new.SelectSingleNode("x:object/x:common_data/x:cad_number", _nsManager);
                    if (objCadastralNum != null)
                        BuildingInfo.CadastralNum = objCadastralNum.InnerText;

                    XmlNode objType = buildingXml_new.SelectSingleNode("x:object/x:common_data/x:type/x:code", _nsManager);
                    if (objType != null)
                        BuildingInfo.Type = objType.InnerText;

                    XmlNode area = buildingXml_new.SelectSingleNode("x:params/x:area", _nsManager);
                    if (area != null)
                        BuildingInfo.Area = area.InnerText;

                    XmlNode addr = buildingXml_new.SelectSingleNode("x:address_room/x:address/x:address/address_fias", _nsManager);
                    if (addr != null)
                    {
                        XmlNode name_city = addr.SelectSingleNode("x:level_settlement/x:city/x:name_city", _nsManager);
                        if (name_city != null)
                        {
                            BuildingInfo.City = name_city.InnerText;
                        }
                        XmlNode type_city = addr.SelectSingleNode("x:level_settlement/x:city/x:type_city", _nsManager);
                        if (type_city != null)
                        {
                            BuildingInfo.CityType = type_city.InnerText;
                        }
                        XmlNode name_street = addr.SelectSingleNode("x:detailed_level/x:street/x:name_street", _nsManager);
                        if (name_street != null)
                        {
                            BuildingInfo.Street = name_street.InnerText;
                        }
                        XmlNode type_street = addr.SelectSingleNode("x:detailed_level/x:street/x:type_street", _nsManager);
                        if (type_street != null)
                        {
                            BuildingInfo.StreetType = type_street.InnerText;
                        }
                        XmlNode name_home = addr.SelectSingleNode("x:detailed_level/x:level1/x:name_level1", _nsManager);
                        if (name_home != null)
                        {
                            BuildingInfo.Home = name_home.InnerText;
                        }
                        XmlNode type_home = addr.SelectSingleNode("x:detailed_level/x:level1/x:type_level1", _nsManager);
                        if (type_home != null)
                        {
                            BuildingInfo.HomeType = type_home.InnerText;
                        }
                        XmlNode name_flat = addr.SelectSingleNode("x:detailed_level/x:apartment/x:name_apartment", _nsManager);
                        if (name_flat != null)
                        {
                            BuildingInfo.Flat = name_flat.InnerText;
                        }
                        XmlNode type_flat = addr.SelectSingleNode("x:detailed_level/x:apartment/x:type_apartment", _nsManager);
                        if (type_flat != null)
                        {
                            BuildingInfo.FlatType = type_flat.InnerText;
                        }
                    }

                }
            }
        }
    }
}
