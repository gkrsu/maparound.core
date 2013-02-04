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
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Drawing.Imaging;
using System.Drawing;
using MapAround.Mapping;
using MapAround.Geometry;
using MapAround.Serialization;
using MapAround.CoordinateSystems;
using MapAround.CoordinateSystems.Transformations;
using MapAround.Web.Wms;

namespace MapAround.Web.Wmts
{
    /// <summary>
    /// Generates an XML containing WMTS capabilities.
    /// </summary>
    public class WmtsCapabilities
    {
        private const string wmtsNamespaceURI = "http://www.opengis.net/wmts/1.0"; 
        private const string xlinkNamespaceURI = "http://www.w3.org/1999/xlink";

        /// <summary>
        /// Generates an XML containing WMTS capabilities.
        /// </summary>
        /// <param name="map">A MapAround.Mapping.Map instance</param>        
        /// <param name="serviceDescription"></param>
        /// <returns>A System.Xml.XmlDocument instance containing WMTS capabilities in compliance to the WMS standard</returns>
        public static XmlDocument GetCapabilities(Map map,
                                                  WmtsServiceDescription serviceDescription)
        {
            XmlDocument capabilities = new XmlDocument();

            capabilities.InsertBefore(capabilities.CreateXmlDeclaration("1.0", "UTF-8", string.Empty),
                                      capabilities.DocumentElement);

            XmlNode rootNode = capabilities.CreateNode(XmlNodeType.Element, "WMTS_Capabilities", wmtsNamespaceURI);
            rootNode.Attributes.Append(createAttribute("version", "1.0.0", capabilities));

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

        private static XmlNode GenerateServiceNode(ref WmtsServiceDescription serviceDescription,
                                                   XmlDocument capabilities)
        {
            XmlElement serviceNode = capabilities.CreateElement("Service", wmtsNamespaceURI);
            serviceNode.AppendChild(createElement("Name", "OGC:WMTS", capabilities, false, wmtsNamespaceURI));
            serviceNode.AppendChild(createElement("Title", serviceDescription.Title, capabilities, false,
                                                  wmtsNamespaceURI));
            if (!String.IsNullOrEmpty(serviceDescription.Abstract))
                serviceNode.AppendChild(createElement("Abstract", serviceDescription.Abstract, capabilities, false,
                                                      wmtsNamespaceURI));

            if (serviceDescription.Keywords != null && serviceDescription.Keywords.Length > 0)
            {
                XmlElement KeywordListNode = capabilities.CreateElement("KeywordList", wmtsNamespaceURI);
                foreach (string keyword in serviceDescription.Keywords)
                    KeywordListNode.AppendChild(createElement("Keyword", keyword, capabilities, false, wmtsNamespaceURI));
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
                                                      wmtsNamespaceURI));
            if (serviceDescription.AccessConstraints != null && serviceDescription.AccessConstraints != string.Empty)
                serviceNode.AppendChild(createElement("AccessConstraints", serviceDescription.AccessConstraints,
                                                      capabilities, false, wmtsNamespaceURI));
            if (serviceDescription.LayerLimit > 0)
                serviceNode.AppendChild(createElement("LayerLimit", serviceDescription.LayerLimit.ToString(CultureInfo.InvariantCulture),
                                                      capabilities, false, wmtsNamespaceURI));
            if (serviceDescription.MaxWidth > 0)
                serviceNode.AppendChild(createElement("MaxWidth", serviceDescription.MaxWidth.ToString(CultureInfo.InvariantCulture), capabilities,
                                                      false, wmtsNamespaceURI));
            if (serviceDescription.MaxHeight > 0)
                serviceNode.AppendChild(createElement("MaxHeight", serviceDescription.MaxHeight.ToString(CultureInfo.InvariantCulture), capabilities,
                                                      false, wmtsNamespaceURI));
            return serviceNode;
        }

        private static XmlNode GenerateCapabilityNode(Map map, WmtsServiceDescription serviceDescription, XmlDocument capabilities)
        {
            string OnlineResource = string.Empty; // !!!!!!!!!!!! Доработать!

            XmlNode CapabilityNode = capabilities.CreateNode(XmlNodeType.Element, "Capability", wmtsNamespaceURI);
            XmlNode RequestNode = capabilities.CreateNode(XmlNodeType.Element, "Request", wmtsNamespaceURI);
            XmlNode GetCapabilitiesNode = capabilities.CreateNode(XmlNodeType.Element, "GetCapabilities",
                                                                  wmtsNamespaceURI);

            GetCapabilitiesNode.AppendChild(createElement("Format", "text/xml", capabilities, false, wmtsNamespaceURI));
            GetCapabilitiesNode.AppendChild(GenerateDCPTypeNode(capabilities, OnlineResource));
            RequestNode.AppendChild(GetCapabilitiesNode);

            XmlNode getMapNode = capabilities.CreateNode(XmlNodeType.Element, "GetTile", wmtsNamespaceURI);

            // поддерживаемые форматы изображений
            foreach (ImageCodecInfo encoder in ImageCodecInfo.GetImageEncoders())
                getMapNode.AppendChild(createElement("Format", encoder.MimeType, capabilities, false, wmtsNamespaceURI));

            getMapNode.AppendChild(GenerateDCPTypeNode(capabilities, OnlineResource));

            RequestNode.AppendChild(getMapNode);
            CapabilityNode.AppendChild(RequestNode);
            XmlElement exceptionNode = capabilities.CreateElement("Exception", wmtsNamespaceURI);
            exceptionNode.AppendChild(createElement("Format", "text/xml", capabilities, false, wmtsNamespaceURI));
            CapabilityNode.AppendChild(exceptionNode); //Add supported exception types

            // список слоев
            XmlNode layerRootNode = capabilities.CreateNode(XmlNodeType.Element, "Layer", wmtsNamespaceURI);
            layerRootNode.AppendChild(createElement("Title", "MapAround", capabilities, false, wmtsNamespaceURI));

            string srs = "EPSG:-1";
            if (!string.IsNullOrEmpty(map.CoodrinateSystemWKT))
            {
                ICoordinateSystem coordinateSystem =
                    (ICoordinateSystem)CoordinateSystemWktDeserializer.Parse(map.CoodrinateSystemWKT);

                srs = coordinateSystem.Authority + ":" + coordinateSystem.AuthorityCode;
            }

            layerRootNode.AppendChild(createElement("SRS", srs, capabilities, false,
                                                    wmtsNamespaceURI));

            if (serviceDescription.Tile != null &&
                !serviceDescription.Tile.IsEmpty())
            {
                layerRootNode.AppendChild(GenerateTileElement(serviceDescription.Tile, srs, capabilities));
            }

            foreach (LayerBase layer in map.Layers)
                layerRootNode.AppendChild(getWmtsLayerNode(serviceDescription, layer, capabilities));

            CapabilityNode.AppendChild(layerRootNode);

            return CapabilityNode;
        }

        private static XmlNode GenerateDCPTypeNode(XmlDocument capabilities, string onlineResource)
        {
            XmlNode dcpType = capabilities.CreateNode(XmlNodeType.Element, "DCPType", wmtsNamespaceURI);
            XmlNode httpType = capabilities.CreateNode(XmlNodeType.Element, "HTTP", wmtsNamespaceURI);
            XmlElement resource = GenerateOnlineResourceElement(capabilities, onlineResource);

            XmlNode getNode = capabilities.CreateNode(XmlNodeType.Element, "Get", wmtsNamespaceURI);
            XmlNode postNode = capabilities.CreateNode(XmlNodeType.Element, "Post", wmtsNamespaceURI);
            getNode.AppendChild(resource.Clone());
            postNode.AppendChild(resource);
            httpType.AppendChild(getNode);
            httpType.AppendChild(postNode);
            dcpType.AppendChild(httpType);
            return dcpType;
        }

        private static XmlElement GenerateOnlineResourceElement(XmlDocument capabilities, string onlineResource)
        {
            XmlElement resource = capabilities.CreateElement("OnlineResource", wmtsNamespaceURI);
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
            XmlElement infoNode = capabilities.CreateElement("ContactInformation", wmtsNamespaceURI);

            XmlElement cpp = capabilities.CreateElement("ContactPersonPrimary", wmtsNamespaceURI);
            if (info.PersonPrimary.Person != null && info.PersonPrimary.Person != String.Empty)
                cpp.AppendChild(createElement("ContactPerson", info.PersonPrimary.Person, capabilities, false,
                                              wmtsNamespaceURI));
            if (info.PersonPrimary.Organisation != null && info.PersonPrimary.Organisation != String.Empty)
                cpp.AppendChild(createElement("ContactOrganization", info.PersonPrimary.Organisation, capabilities,
                                              false, wmtsNamespaceURI));
            if (cpp.HasChildNodes)
                infoNode.AppendChild(cpp);

            if (info.Position != null && info.Position != string.Empty)
                infoNode.AppendChild(createElement("ContactPosition", info.Position, capabilities, false,
                                                   wmtsNamespaceURI));

            XmlElement ca = capabilities.CreateElement("ContactAddress", wmtsNamespaceURI);
            if (info.Address.AddressType != null && info.Address.AddressType != string.Empty)
                ca.AppendChild(createElement("AddressType", info.Address.AddressType, capabilities, false,
                                             wmtsNamespaceURI));
            if (info.Address.Address != null && info.Address.Address != string.Empty)
                ca.AppendChild(createElement("Address", info.Address.Address, capabilities, false, wmtsNamespaceURI));
            if (info.Address.City != null && info.Address.City != string.Empty)
                ca.AppendChild(createElement("City", info.Address.City, capabilities, false, wmtsNamespaceURI));
            if (info.Address.StateOrProvince != null && info.Address.StateOrProvince != string.Empty)
                ca.AppendChild(createElement("StateOrProvince", info.Address.StateOrProvince, capabilities, false,
                                             wmtsNamespaceURI));
            if (info.Address.PostCode != null && info.Address.PostCode != string.Empty)
                ca.AppendChild(createElement("PostCode", info.Address.PostCode, capabilities, false, wmtsNamespaceURI));
            if (info.Address.Country != null && info.Address.Country != string.Empty)
                ca.AppendChild(createElement("Country", info.Address.Country, capabilities, false, wmtsNamespaceURI));
            if (ca.HasChildNodes)
                infoNode.AppendChild(ca);

            if (info.VoiceTelephone != null && info.VoiceTelephone != string.Empty)
                infoNode.AppendChild(createElement("ContactVoiceTelephone", info.VoiceTelephone, capabilities, false,
                                                   wmtsNamespaceURI));
            if (info.FacsimileTelephone != null && info.FacsimileTelephone != string.Empty)
                infoNode.AppendChild(createElement("ContactFacsimileTelephone", info.FacsimileTelephone, capabilities,
                                                   false, wmtsNamespaceURI));
            if (info.ElectronicMailAddress != null && info.ElectronicMailAddress != string.Empty)
                infoNode.AppendChild(createElement("ContactElectronicMailAddress", info.ElectronicMailAddress,
                                                   capabilities, false, wmtsNamespaceURI));

            return infoNode;
        }

        private static XmlNode getWmtsLayerNode(WmtsServiceDescription serviceDescription, LayerBase layer, XmlDocument doc)
        {
            XmlNode layerNode = doc.CreateNode(XmlNodeType.Element, "Layer", wmtsNamespaceURI);
            layerNode.AppendChild(createElement("Name", layer.Alias, doc, false, wmtsNamespaceURI));
            layerNode.AppendChild(createElement("Title", layer.Title, doc, false, wmtsNamespaceURI));
            layerNode.Attributes.Append(doc.CreateAttribute("queryable")).Value = 
                layer is FeatureLayer && (layer as FeatureLayer).FeaturesSelectable ? "1" : "0";

            layerNode.AppendChild(GenerateTileMatrixSet(serviceDescription, doc));

            return layerNode;
        }

        private static XmlElement GenerateTileElement(Tile tile, string srs, XmlDocument doc)
        {
            XmlElement xmlTile = doc.CreateElement("Tile", wmtsNamespaceURI);
            xmlTile.Attributes.Append(createAttribute("TileRow", tile.Row.ToString(CultureInfo.InvariantCulture), doc));
            xmlTile.Attributes.Append(createAttribute("TileColumn", tile.Col.ToString(CultureInfo.InvariantCulture), doc));
            xmlTile.Attributes.Append(createAttribute("TileWidth", tile.Width.ToString(CultureInfo.InvariantCulture), doc));
            xmlTile.Attributes.Append(createAttribute("TileHeight", tile.Height.ToString(CultureInfo.InvariantCulture), doc));
            xmlTile.Attributes.Append(createAttribute("SRS", srs, doc));
            return xmlTile;
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

        private static XmlNode GenerateTileMatrixSet(WmtsServiceDescription serviceDescription, XmlDocument capabilities)
        {
            XmlNode tileMatrixSetLink = capabilities.CreateNode(XmlNodeType.Element, "TileMatrixSetLink", wmtsNamespaceURI);
            
            XmlNode tileMatrixSet = capabilities.CreateNode(XmlNodeType.Element, "TileMatrixSet", wmtsNamespaceURI);

            tileMatrixSet.AppendChild(createElement("Identifier", "EPSG:3857", capabilities, false, wmtsNamespaceURI));
            tileMatrixSet.AppendChild(createElement("WellKnownScaleSet", "EPSG", capabilities, false, wmtsNamespaceURI));

            for (int i = 0; i < serviceDescription.ZoomLevel.Count; i++)
            {
                tileMatrixSet.AppendChild(GenerateTileMatrixInfo(serviceDescription, capabilities, i));
            }

            tileMatrixSetLink.AppendChild(tileMatrixSet);

            return tileMatrixSetLink;
        }

        private static XmlNode GenerateTileMatrixInfo(WmtsServiceDescription serviceDescription, XmlDocument capabilities, int i)
        {
            XmlNode matrixInfo = capabilities.CreateNode(XmlNodeType.Element, "TileMatrix", wmtsNamespaceURI);

            matrixInfo.AppendChild(createElement("ows:Identifier", serviceDescription.ZoomLevel.Keys.ToList()[i].ToString(), capabilities, false, wmtsNamespaceURI));
            matrixInfo.AppendChild(createElement("ScaleDenominator", serviceDescription.GetScaleDenominator(i).ToString(), capabilities, false, wmtsNamespaceURI));
            matrixInfo.AppendChild(createElement("TopLeftCorner", "-180 90", capabilities, false, wmtsNamespaceURI));
            matrixInfo.AppendChild(createElement("TileWidth", "256", capabilities, false, wmtsNamespaceURI));
            matrixInfo.AppendChild(createElement("TileHeight", "256", capabilities, false, wmtsNamespaceURI));
            matrixInfo.AppendChild(createElement("MatrixWidth", Math.Pow(2, i).ToString(), capabilities, false, wmtsNamespaceURI));
            matrixInfo.AppendChild(createElement("MatrixHeight", Math.Pow(2, i).ToString(), capabilities, false, wmtsNamespaceURI));

            return matrixInfo;
        }
    }
}