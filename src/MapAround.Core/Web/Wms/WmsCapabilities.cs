//  MapAround - .NET tools for developing web and desktop mapping applications 

//  Copyright (coffee) 2009-2012 OOO "GKR"
//  This program is free software; you can redistribute it and/or 
//  modify it under the terms of the GNU General Public License 
//   as published by the Free Software Foundation; either version 3 
//  of the License, or (at your option) any later version. 
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program; If not, see <http://www.gnu.org/licenses/>



using System;
using System.Drawing.Imaging;
using System.Globalization;
using System.Xml;
using MapAround.CoordinateSystems;
using MapAround.Geometry;
using MapAround.Mapping;
using MapAround.Serialization;

namespace MapAround.Web.Wms
{
    /// <summary>
    /// Generates an XML containing WMS capabilities.
    /// </summary>
    public class WmsCapabilities
    {
        private const string wmsNamespaceURI = "http://www.opengis.net/wms";
        private const string xlinkNamespaceURI = "http://www.w3.org/1999/xlink";

        /// <summary>
        /// Generates an XML containing WMS capabilities.
        /// </summary>
        /// <param name="map">A MapAround.Mapping.Map instance</param>
        /// <param name="serviceDescription">A MapAround.Web.Wms.WmsServiceDescription instance</param>
        /// <returns>A System.Xml.XmlDocument instance containing WMS capabilities in compliance to the WMS standard</returns>
        public static XmlDocument GetCapabilities(Map map,
                                                  WmsServiceDescription serviceDescription)
        {
            XmlDocument capabilities = new XmlDocument();

            capabilities.InsertBefore(capabilities.CreateXmlDeclaration("1.0", "UTF-8", string.Empty),
                                      capabilities.DocumentElement);

            XmlNode rootNode = capabilities.CreateNode(XmlNodeType.Element, "WMS_Capabilities", wmsNamespaceURI);
            rootNode.Attributes.Append(createAttribute("version", "1.1.1", capabilities));

            XmlAttribute attr = capabilities.CreateAttribute("xmlns", "xsi", "http://www.w3.org/2000/xmlns/");
            attr.InnerText = "http://www.w3.org/2001/XMLSchema-instance";
            rootNode.Attributes.Append(attr);

            rootNode.Attributes.Append(createAttribute("xmlns:xlink", xlinkNamespaceURI, capabilities));
            XmlAttribute attr2 = capabilities.CreateAttribute("xsi", "schemaLocation",
                                                              "http://www.w3.org/2001/XMLSchema-instance");

            rootNode.AppendChild(GenerateServiceNode(ref serviceDescription, capabilities));

            rootNode.AppendChild(GenerateCapabilityNode(map, serviceDescription, capabilities));

            capabilities.AppendChild(rootNode);

            return capabilities;
        }

        private static XmlNode GenerateServiceNode(ref WmsServiceDescription serviceDescription,
                                                   XmlDocument capabilities)
        {
            XmlElement serviceNode = capabilities.CreateElement("Service", wmsNamespaceURI);
            serviceNode.AppendChild(createElement("Name", "OGC:WMS", capabilities, false, wmsNamespaceURI));
            serviceNode.AppendChild(createElement("Title", serviceDescription.Title, capabilities, false,
                                                  wmsNamespaceURI));
            if (!String.IsNullOrEmpty(serviceDescription.Abstract))
                serviceNode.AppendChild(createElement("Abstract", serviceDescription.Abstract, capabilities, false,
                                                      wmsNamespaceURI));

            if (serviceDescription.Keywords != null && serviceDescription.Keywords.Length > 0)
            {
                XmlElement KeywordListNode = capabilities.CreateElement("KeywordList", wmsNamespaceURI);
                foreach (string keyword in serviceDescription.Keywords)
                    KeywordListNode.AppendChild(createElement("Keyword", keyword, capabilities, false, wmsNamespaceURI));
                serviceNode.AppendChild(KeywordListNode);
            }

            // ссылка
            XmlElement onlineResourceNode = GenerateOnlineResourceElement(capabilities,
                                                                          serviceDescription.OnlineResource);
            serviceNode.AppendChild(onlineResourceNode);

            // контактная информация
            XmlElement contactInfoNode = GenerateContactInfoElement(capabilities, serviceDescription.ContactInformation);
            if (contactInfoNode.HasChildNodes)
                serviceNode.AppendChild(contactInfoNode);

            if (serviceDescription.Fees != null && serviceDescription.Fees != string.Empty)
                serviceNode.AppendChild(createElement("Fees", serviceDescription.Fees, capabilities, false,
                                                      wmsNamespaceURI));
            if (serviceDescription.AccessConstraints != null && serviceDescription.AccessConstraints != string.Empty)
                serviceNode.AppendChild(createElement("AccessConstraints", serviceDescription.AccessConstraints,
                                                      capabilities, false, wmsNamespaceURI));
            if (serviceDescription.LayerLimit > 0)
                serviceNode.AppendChild(createElement("LayerLimit", serviceDescription.LayerLimit.ToString(CultureInfo.InvariantCulture),
                                                      capabilities, false, wmsNamespaceURI));
            if (serviceDescription.MaxWidth > 0)
                serviceNode.AppendChild(createElement("MaxWidth", serviceDescription.MaxWidth.ToString(CultureInfo.InvariantCulture), capabilities,
                                                      false, wmsNamespaceURI));
            if (serviceDescription.MaxHeight > 0)
                serviceNode.AppendChild(createElement("MaxHeight", serviceDescription.MaxHeight.ToString(CultureInfo.InvariantCulture), capabilities,
                                                      false, wmsNamespaceURI));
            return serviceNode;
        }

        private static XmlNode GenerateCapabilityNode(Map map, WmsServiceDescription serviceDescription, XmlDocument capabilities)
        {
            string OnlineResource = string.Empty; // !!!!!!!!!!!! Доработать!

            XmlNode CapabilityNode = capabilities.CreateNode(XmlNodeType.Element, "Capability", wmsNamespaceURI);
            XmlNode RequestNode = capabilities.CreateNode(XmlNodeType.Element, "Request", wmsNamespaceURI);
            XmlNode GetCapabilitiesNode = capabilities.CreateNode(XmlNodeType.Element, "GetCapabilities",
                                                                  wmsNamespaceURI);

            GetCapabilitiesNode.AppendChild(createElement("Format", "text/xml", capabilities, false, wmsNamespaceURI));
            GetCapabilitiesNode.AppendChild(GenerateDCPTypeNode(capabilities, OnlineResource));
            RequestNode.AppendChild(GetCapabilitiesNode);

            XmlNode getMapNode = capabilities.CreateNode(XmlNodeType.Element, "GetMap", wmsNamespaceURI);

            // поддерживаемые форматы изображений
            foreach (ImageCodecInfo encoder in ImageCodecInfo.GetImageEncoders())
                getMapNode.AppendChild(createElement("Format", encoder.MimeType, capabilities, false, wmsNamespaceURI));

            getMapNode.AppendChild(GenerateDCPTypeNode(capabilities, OnlineResource));

            RequestNode.AppendChild(getMapNode);
            CapabilityNode.AppendChild(RequestNode);
            XmlElement exceptionNode = capabilities.CreateElement("Exception", wmsNamespaceURI);
            exceptionNode.AppendChild(createElement("Format", "text/xml", capabilities, false, wmsNamespaceURI));
            CapabilityNode.AppendChild(exceptionNode); //Add supported exception types

            // список слоев
            XmlNode layerRootNode = capabilities.CreateNode(XmlNodeType.Element, "Layer", wmsNamespaceURI);
            layerRootNode.AppendChild(createElement("Title", "MapAround", capabilities, false, wmsNamespaceURI));

            string srs = "EPSG:-1";
            if (!string.IsNullOrEmpty(map.CoodrinateSystemWKT))
            {
                ICoordinateSystem coordinateSystem =
                    (ICoordinateSystem)CoordinateSystemWktDeserializer.Parse(map.CoodrinateSystemWKT);

                srs = coordinateSystem.Authority + ":" + coordinateSystem.AuthorityCode;
            }

            layerRootNode.AppendChild(createElement("SRS", srs, capabilities, false,
                                                    wmsNamespaceURI));

            if (serviceDescription.BoundingBox != null &&
                !serviceDescription.BoundingBox.IsEmpty())
            {
                layerRootNode.AppendChild(generateBoundingBoxElement(serviceDescription.BoundingBox, srs, capabilities));
            }

            foreach (LayerBase layer in map.Layers)
                layerRootNode.AppendChild(getWmsLayerNode(layer, capabilities));

            CapabilityNode.AppendChild(layerRootNode);

            return CapabilityNode;
        }

        private static XmlNode GenerateDCPTypeNode(XmlDocument capabilities, string onlineResource)
        {
            XmlNode dcpType = capabilities.CreateNode(XmlNodeType.Element, "DCPType", wmsNamespaceURI);
            XmlNode httpType = capabilities.CreateNode(XmlNodeType.Element, "HTTP", wmsNamespaceURI);
            XmlElement resource = GenerateOnlineResourceElement(capabilities, onlineResource);

            XmlNode getNode = capabilities.CreateNode(XmlNodeType.Element, "Get", wmsNamespaceURI);
            XmlNode postNode = capabilities.CreateNode(XmlNodeType.Element, "Post", wmsNamespaceURI);
            getNode.AppendChild(resource.Clone());
            postNode.AppendChild(resource);
            httpType.AppendChild(getNode);
            httpType.AppendChild(postNode);
            dcpType.AppendChild(httpType);
            return dcpType;
        }

        private static XmlElement GenerateOnlineResourceElement(XmlDocument capabilities, string onlineResource)
        {
            XmlElement resource = capabilities.CreateElement("OnlineResource", wmsNamespaceURI);
            XmlAttribute attrType = capabilities.CreateAttribute("xlink", "type", xlinkNamespaceURI);
            attrType.Value = "simple";
            resource.Attributes.Append(attrType);
            XmlAttribute href = capabilities.CreateAttribute("xlink", "href", xlinkNamespaceURI);
            href.Value = onlineResource;
            resource.Attributes.Append(href);
            XmlAttribute xmlns = capabilities.CreateAttribute("xmlns:xlink");
            xmlns.Value = xlinkNamespaceURI;
            resource.Attributes.Append(xmlns);
            return resource;
        }

        private static XmlElement GenerateContactInfoElement(XmlDocument capabilities, WmsContactInformation info)
        {
            XmlElement infoNode = capabilities.CreateElement("ContactInformation", wmsNamespaceURI);

            XmlElement cpp = capabilities.CreateElement("ContactPersonPrimary", wmsNamespaceURI);
            if (info.PersonPrimary.Person != null && info.PersonPrimary.Person != String.Empty)
                cpp.AppendChild(createElement("ContactPerson", info.PersonPrimary.Person, capabilities, false,
                                              wmsNamespaceURI));
            if (info.PersonPrimary.Organisation != null && info.PersonPrimary.Organisation != String.Empty)
                cpp.AppendChild(createElement("ContactOrganization", info.PersonPrimary.Organisation, capabilities,
                                              false, wmsNamespaceURI));
            if (cpp.HasChildNodes)
                infoNode.AppendChild(cpp);

            if (info.Position != null && info.Position != string.Empty)
                infoNode.AppendChild(createElement("ContactPosition", info.Position, capabilities, false,
                                                   wmsNamespaceURI));

            XmlElement ca = capabilities.CreateElement("ContactAddress", wmsNamespaceURI);
            if (info.Address.AddressType != null && info.Address.AddressType != string.Empty)
                ca.AppendChild(createElement("AddressType", info.Address.AddressType, capabilities, false,
                                             wmsNamespaceURI));
            if (info.Address.Address != null && info.Address.Address != string.Empty)
                ca.AppendChild(createElement("Address", info.Address.Address, capabilities, false, wmsNamespaceURI));
            if (info.Address.City != null && info.Address.City != string.Empty)
                ca.AppendChild(createElement("City", info.Address.City, capabilities, false, wmsNamespaceURI));
            if (info.Address.StateOrProvince != null && info.Address.StateOrProvince != string.Empty)
                ca.AppendChild(createElement("StateOrProvince", info.Address.StateOrProvince, capabilities, false,
                                             wmsNamespaceURI));
            if (info.Address.PostCode != null && info.Address.PostCode != string.Empty)
                ca.AppendChild(createElement("PostCode", info.Address.PostCode, capabilities, false, wmsNamespaceURI));
            if (info.Address.Country != null && info.Address.Country != string.Empty)
                ca.AppendChild(createElement("Country", info.Address.Country, capabilities, false, wmsNamespaceURI));
            if (ca.HasChildNodes)
                infoNode.AppendChild(ca);

            if (info.VoiceTelephone != null && info.VoiceTelephone != string.Empty)
                infoNode.AppendChild(createElement("ContactVoiceTelephone", info.VoiceTelephone, capabilities, false,
                                                   wmsNamespaceURI));
            if (info.FacsimileTelephone != null && info.FacsimileTelephone != string.Empty)
                infoNode.AppendChild(createElement("ContactFacsimileTelephone", info.FacsimileTelephone, capabilities,
                                                   false, wmsNamespaceURI));
            if (info.ElectronicMailAddress != null && info.ElectronicMailAddress != string.Empty)
                infoNode.AppendChild(createElement("ContactElectronicMailAddress", info.ElectronicMailAddress,
                                                   capabilities, false, wmsNamespaceURI));

            return infoNode;
        }

        private static XmlNode getWmsLayerNode(LayerBase layer, XmlDocument doc)
        {
            XmlNode layerNode = doc.CreateNode(XmlNodeType.Element, "Layer", wmsNamespaceURI);
            layerNode.AppendChild(createElement("Name", layer.Alias, doc, false, wmsNamespaceURI));
            layerNode.AppendChild(createElement("Title", layer.Title, doc, false, wmsNamespaceURI));
            layerNode.Attributes.Append(doc.CreateAttribute("queryable")).Value = 
                layer is FeatureLayer && (layer as FeatureLayer).FeaturesSelectable ? "1" : "0";

            return layerNode;
        }

        private static XmlElement generateBoundingBoxElement(BoundingRectangle bbox, string srs, XmlDocument doc)
        {
            XmlElement xmlBbox = doc.CreateElement("BoundingBox", wmsNamespaceURI);
            xmlBbox.Attributes.Append(createAttribute("minx", bbox.MinX.ToString(CultureInfo.InvariantCulture), doc));
            xmlBbox.Attributes.Append(createAttribute("miny", bbox.MinY.ToString(CultureInfo.InvariantCulture), doc));
            xmlBbox.Attributes.Append(createAttribute("maxx", bbox.MaxX.ToString(CultureInfo.InvariantCulture), doc));
            xmlBbox.Attributes.Append(createAttribute("maxy", bbox.MaxY.ToString(CultureInfo.InvariantCulture), doc));
            xmlBbox.Attributes.Append(createAttribute("SRS", srs, doc));
            return xmlBbox;
        }

        private static XmlAttribute createAttribute(string name, string value, XmlDocument doc)
        {
            XmlAttribute attr = doc.CreateAttribute(name);
            attr.Value = value;
            return attr;
        }

        private static XmlNode createElement(string name, string value, XmlDocument doc, bool IsXml, string namespaceURI)
        {
            XmlNode node = doc.CreateNode(XmlNodeType.Element, name, namespaceURI);
            if (IsXml)
                node.InnerXml = value;
            else
                node.InnerText = value;
            return node;
        }

        internal static XmlDocument CreateXml()
        {
            XmlDocument capabilities = new XmlDocument();
            return capabilities;
        }
    }

  
}