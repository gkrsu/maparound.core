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
using System.Globalization;
using System.Xml;
using System.Collections.Generic;
using MapAround.Mapping;

namespace MapAround.ThematicLayer
{
    /// <summary>
    ///  Thematic Layer extension
    /// </summary>
    public class ThematicLayerExtension:FeatureLayerExtension
    {
       

        /// <summary>
        /// Represent feature style
        /// </summary>
        public class FeatureStyle
        {
           /// <summary/>
           
           public PointStyle PointStyle = new PointStyle();
           /// <summary/>
           public PolygonStyle PolygonStyle = new PolygonStyle();
           /// <summary/>
           public  PolylineStyle PolylineStyle = new PolylineStyle();
           /// <summary/>
           public  TitleStyle TitleStyle = new TitleStyle();
        }


        IDictionary<string, FeatureStyle> _rules = new Dictionary<string, FeatureStyle>();
        

       

        private int attribute_index = -1;

        
        private string attriibute_name = string.Empty;

        /// <summary>
        /// Get and set attribute index
        /// </summary>
        public  int Attribute_index
        {
            get { return attribute_index; }
            set { attribute_index = value; }
        }

        /// <summary>
        /// Get and set attribute name
        /// </summary>
        public  string Attriibute_name
        {
            get { return attriibute_name; }
            set { attriibute_name = value; }
        }
        /// <summary>
        /// Default constructor 
        /// </summary>
        /// <param name="layer"></param>
        public ThematicLayerExtension(FeatureLayer layer) : base(layer)
        {
            
        }

        #region Overrides of FeatureLayerExtension

        /// <summary>
        /// Registry extension in FeatureLayer
        /// </summary>
        /// <param name="layer"></param>
        public override void RegistryExtension(FeatureLayer layer)
        {
            layer.BeforePointRender += layer_BeforePointRender;
            layer.BeforePolygonRender += layer_BeforePolygonRender;
            layer.BeforePolylineRender += layer_BeforePolylineRender;
        }

        

        /// <summary>
        /// UnRegistry extension in FeatureLayer
        /// </summary>
        /// <param name="layer"></param>
        public override void UnRegistryExtension(FeatureLayer layer)
        {
            layer.BeforePointRender-= layer_BeforePointRender;
            layer.BeforePolygonRender -= layer_BeforePolylineRender;
            layer.BeforePolylineRender -= layer_BeforePolylineRender;
        }

        /// <summary>
        /// Save internal state in XmlNode
        /// </summary>
        /// <param name="node"></param>
        public override void processFromXml(XmlNode node)
        {
            if (node == null) throw new ArgumentNullException("node");

            if (node.Attributes["useExtension"]!=null)
            {
                UseExtension = (node.Attributes["useExtension"].Value == "1");
            }
            if (node.Attributes["attribyte_name"] != null)
            {
                attriibute_name = node.Attributes["attribyte_name"].Value;
            }

            if (node.Attributes["attribyte_index"]!=null)
            {
                attribute_index = int.Parse(node.Attributes["attribyte_index"].Value, CultureInfo.InvariantCulture);
            }
            if (string.IsNullOrEmpty(attriibute_name) && (attribute_index == -1)) throw new FormatException();

            XmlNode rules_node = tryGetNodeByName(node.ChildNodes, "rules");
            if (rules_node!=null)
            {
                processRules(rules_node);
            }
        }


        private void addRule(XmlElement rules_element)
        {

            foreach (var rule in _rules.Keys)
            {
                XmlElement rule_element = rules_element.OwnerDocument.CreateElement("rule");
                rules_element.AppendChild(rule_element);
                addAttribute(rule_element,"value",rule);
                var style = _rules[rule];
                addPointStyleElement(style.PointStyle, rules_element.OwnerDocument,rule_element );
                addPolygonStyleElement( style.PolygonStyle, rules_element.OwnerDocument,rule_element );
                addPolylineStyleElement(style.PolylineStyle, rules_element.OwnerDocument,rule_element );
                addTitleStyleElement(style.TitleStyle, rules_element.OwnerDocument, rule_element);

            }
        }

        private  void processRules(XmlNode rules)
        {
            foreach (XmlNode node in rules.ChildNodes)
            {
                if (node.Name=="rule")
                {
                    string value = node.Attributes["value"].Value;
                    FeatureStyle fs = new FeatureStyle();
                    processPointStyle(node,fs.PointStyle);
                    processPolygonStyle(node, fs.PolygonStyle);
                    processPolylineStyle(node,fs.PolylineStyle);
                    processTitleStyle(node, fs.TitleStyle);
                    _rules.Add(value,fs);
                }
            }
        }

        /// <summary>
        /// Restore internal state in XmlEllement
        /// </summary>
        /// <param name="element"></param>
        public override void addToXml(XmlElement element)
        {
            addAttribute(element, "useExtension", (UseExtension)?"1":"0");
            addAttribute(element, "attribyte_name", attriibute_name);
            addAttribute(element, "attribyte_index", attribute_index.ToString(CultureInfo.InvariantCulture));
            XmlElement rules_element =  element.OwnerDocument.CreateElement("rules");
            element.AppendChild(rules_element);
            addRule(rules_element);
        }

        #endregion
        private  int GetIndex(Feature f)
        {
            int index = -1;
            foreach (var atr in f.Layer.FeatureAttributeNames)
            {
                index++;
                if (atr == attriibute_name) return index;
            }
            return -1;
        }
        private  FeatureStyle GetStyle(Feature f)
        {
            if (attribute_index == -1) attribute_index = GetIndex(f);
            string value = f.Attributes[attribute_index].ToString();
            if (_rules.ContainsKey(value))
            {
                return _rules[value];
            }
            else
            {

                return new FeatureStyle()
                           {
                               PolygonStyle = f.Layer.PolygonStyle,
                               PointStyle = f.Layer.PointStyle,
                               PolylineStyle = f.Layer.PolylineStyle,
                               TitleStyle = f.Layer.TitleStyle
                           };
            }
        }
    #region render_extend
        void layer_BeforePolylineRender(object sender, FeatureRenderEventArgs e)
        {
            if (_useExtesion)
            {
                FeatureStyle fs = GetStyle(e.Feature);
                e.Feature.PolylineStyle = fs.PolylineStyle;
                e.Feature.TitleStyle = fs.TitleStyle;
            }

        }

        void layer_BeforePolygonRender(object sender, FeatureRenderEventArgs e)
        {
            if (_useExtesion)
            {
                FeatureStyle fs = GetStyle(e.Feature);
                e.Feature.PolygonStyle = fs.PolygonStyle;
                e.Feature.TitleStyle = fs.TitleStyle;
            }
        }

        void layer_BeforePointRender(object sender, FeatureRenderEventArgs e)
        {
            if (_useExtesion)
            {
                FeatureStyle fs = GetStyle(e.Feature);
                e.Feature.PointStyle = fs.PointStyle;
                e.Feature.TitleStyle = fs.TitleStyle;
            }
        }
   #endregion

        /// <summary>
        /// Get theme reules
        /// </summary>
        public  IDictionary<string,FeatureStyle> ThemeRules
        {
            get { return _rules; }
        }
    }
}