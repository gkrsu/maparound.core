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



﻿/*===========================================================================
** 
** File: Legend.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Descriptions: Classes and interfaces for creating map legends
**
=============================================================================*/

namespace MapAround.UI
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Drawing;

    using MapAround.Mapping;

    /// <summary>
    /// The MapAround.UI namespace contains interfaces and classes
    /// which may used in the user interface implementations.
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Represents a legent element.
    /// </summary>
    public class LegendElement
    {
        private Image _image;
        private string _label;

        /// <summary>
        /// Gets or sets a label.
        /// </summary>
        public string Label
        {
            get { return _label; }
            set { _label = value; }
        }

        /// <summary>
        /// Gets or sets an image.
        /// </summary>
        public Image Image
        {
            get { return _image; }
            set { _image = value; }
        }

        /// <summary>
        /// Initializes a new instance of MapAround.UI.LegendElement.
        /// </summary>
        /// <param name="image">A System.Drawing.Image instance containing the image of element</param>
        /// <param name="label">A label of the element</param>
        public LegendElement(Image image, string label)
        {
            _image = image;
            _label = label;
        }
    }

    /// <summary>
    /// Base class for legend classes.
    /// <para>
    /// Contains common methods for managing elements.
    /// </para>
    /// </summary>
    public abstract class LegendBase
    {
        private Collection<LegendElement> _elements = new Collection<LegendElement>();
        private string _caption = string.Empty;

        /// <summary>
        /// Checks an element.
        /// <para>
        /// Implementations should throw an exception if an element
        /// may not be added.
        /// </para>
        /// </summary>
        /// <param name="element">An element to check</param>
        protected abstract void CheckNewElement(LegendElement element);

        /// <summary>
        /// Checks a legend caption.
        /// <para>
        /// Implementations should throw an exception 
        /// if a caption value is impossible.
        /// </para>
        /// </summary>
        /// <param name="caption">A caption value to check</param>
        protected abstract void CheckCaption(string caption);

        /// <summary>
        /// Gets or sets a caption value.
        /// </summary>
        public string Caption
        {
            get { return _caption; }
            set 
            {
                CheckCaption(value);
                _caption = value; 
            }
        }

        /// <summary>
        /// Gets a collection of elements.
        /// </summary>
        public ReadOnlyCollection<LegendElement> Elements
        {
            get 
            { 
                return new ReadOnlyCollection<LegendElement>(_elements); 
            }
        }

        /// <summary>
        /// Adds an element to this legend.
        /// </summary>
        /// <param name="element">An element to add</param>
        public void AddElement(LegendElement element)
        {
            if (!_elements.Contains(element))
                _elements.Add(element);
        }

        /// <summary>
        /// Interts an element to this legend.
        /// </summary>
        /// <param name="element">An element to insert</param>
        /// <param name="index">A zero-based index at which insert the element</param>
        public void InsertElement(LegendElement element, int index)
        {
            if (!_elements.Contains(element))
                _elements.Insert(index, element);
        }

        /// <summary>
        /// Creates legend elements for the specified layer and adds it
        /// to this legend.
        /// </summary>
        /// <param name="layer">A layer which is used to generate elements</param>
        /// <param name="imagesWidth">A value specifying a width of images of the elements</param>
        /// <param name="imagesHeight">A value specifying a height of images of the elements</param>
        public void AddElementsForLayer(LayerBase layer, int imagesWidth, int imagesHeight)
        {
            FeatureLayer l = layer as FeatureLayer;
            if (l == null) return;

            if (l.LegendSettings != null)
            {
                if ((l.LegendSettings.DisplayPointSample ||
                   l.LegendSettings.DisplayPolylineSample ||
                   l.LegendSettings.DisplayPolygonSample) &&
                   l.FeatureRenderer == null)
                    throw new InvalidOperationException("Unable to add items for the layer. Undefined feature renderer.");

                if (l.LegendSettings.DisplayPointSample)
                {
                    Bitmap bmp = new Bitmap(imagesWidth, imagesHeight);
                    l.RenderFeaturesSample(bmp, true, false, false);
                    LegendElement element = new LegendElement(bmp, l.LegendSettings.PointSampleTitle);
                    CheckNewElement(element);
                    AddElement(element);
                }

                if (l.LegendSettings.DisplayPolylineSample)
                {
                    Bitmap bmp = new Bitmap(imagesWidth, imagesHeight);
                    l.RenderFeaturesSample(bmp, false, true, false);
                    LegendElement element = new LegendElement(bmp, l.LegendSettings.PolylineSampleTitle);
                    CheckNewElement(element);
                    AddElement(element);
                }

                if (l.LegendSettings.DisplayPolygonSample)
                {
                    Bitmap bmp = new Bitmap(imagesWidth, imagesHeight);
                    l.RenderFeaturesSample(bmp, false, false, true);
                    LegendElement element = new LegendElement(bmp, l.LegendSettings.PolygonSampleTitle);
                    CheckNewElement(element);
                    AddElement(element);
                }
            }
        }

        /// <summary>
        /// Builds a legend for a layers of a map.
        /// </summary>
        public void BuildLegendForMap(Map map, int imagesWidth, int imagesHeight)
        {
            Caption = map.Title;

            for (int i = map.Layers.Count - 1; i >= 0; i--)
                AddElementsForLayer(map.Layers[i], imagesWidth, imagesHeight);
        }

        /// <summary>
        /// Removes all elements from this legend.
        /// </summary>
        public void ClearElements()
        {
            _elements.Clear();
        }
    }

    ///// <summary>
    ///// HTML-легенда цифровой карты.
    ///// </summary>
    //public class HTMLLegend : LegendBase
    //{
    //    /// <summary>
    //    /// </summary>
    //    /// <param name="element">Проверяемый элемент</param>
    //    protected override void CheckNewElement(LegendElement element)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    /// <summary>
    //    /// </summary>
    //    /// <param name="caption">Проверяемый заголовок</param>
    //    protected override void CheckCaption(string caption)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    /// <summary>
    /// Represents a legend which appears as a simple image.
    /// </summary>
    public class ImageLegend : LegendBase
    {
        private Font _elementFont = new Font("arial", 8);
        private Font _captionFont = new Font("arial", 12);
        private Color _elementLabelsColor = Color.Black;
        private Color _captionColor = Color.Black;
        private Color _backgroundColor = Color.White;
        private int _width = 0;
        private int _height = 0;
        private int _margin = 5;
        private int _elementHorizontalSpacing = 5;
        private int _elementsVerticalSpacing = 5;

        private bool checkElementWidth(LegendElement element)
        {
            int width = element.Image.Width + _margin * 2 + _elementHorizontalSpacing;

            Bitmap bmp = new Bitmap(1, 1);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                SizeF labelSize = g.MeasureString(element.Label, _elementFont);
                width += (int)labelSize.Width;
                _width = Math.Max(width, _width);
            }

            return  true;
        }

        private bool checkElementHeight(LegendElement element)
        {
            int currentHeight = _margin * 2 + (int)getCaptionSize().Height + _elementsVerticalSpacing;

            foreach (LegendElement el in Elements)
            {
                int height = Math.Max(el.Image.Height, (int)getLabelStrSize(el.Label).Height);
                currentHeight += height + _elementsVerticalSpacing;
            }

            currentHeight += Math.Max(element.Image.Height, (int)getLabelStrSize(element.Label).Height);

            if(currentHeight > _height)
                _height = currentHeight;

            return true;
        }

        private SizeF getLabelStrSize(string label)
        {
            Bitmap bmp = new Bitmap(1, 1);

            using (Graphics g = Graphics.FromImage(bmp))
                return g.MeasureString(label, _elementFont);
        }

        private SizeF getCaptionSize()
        {
            Bitmap bmp = new Bitmap(1, 1);

            using (Graphics g = Graphics.FromImage(bmp))
                return g.MeasureString(Caption, _captionFont);
        }

        /// <summary>
        /// Checks an element.
        /// </summary>
        /// <param name="element">An element to check</param>
        protected override void CheckNewElement(LegendElement element)
        {
            if (!checkElementWidth(element) || !checkElementHeight(element))
                throw new ArgumentException("Unable to add element to the legend", "element");    
        }

        /// <summary>
        /// Checks a legend caption.
        /// <para>
        /// Implementations should throw an exception 
        /// if a caption value is impossible.
        /// </para>
        /// </summary>
        /// <param name="caption">A caption value to check</param>
        protected override void CheckCaption(string caption)
        {
            Bitmap bmp = new Bitmap(1, 1);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                SizeF captionSize = g.MeasureString(caption, _captionFont);
                _width = Math.Max((int)captionSize.Width + _margin * 2, _width);
                _height = Math.Max((int)captionSize.Height + _margin * 2, _height);
            }
        }

        /// <summary>
        /// Gets a width of a legend image in pixels.
        /// </summary>
        public int Width
        {
            get { return _width; }
        }

        /// <summary>
        /// Gets a height of a legend image in pixels.
        /// </summary>
        public int Height
        {
            get { return _height; }
        }

        /// <summary>
        /// Gets or sets an indent of drawing area 
        /// from the edges of the image in pixels.
        /// </summary>
        public int Margin
        {
            get { return _margin; }
            set { _margin = value; }
        }

        /// <summary>
        /// Gets or sets a horizontal spacing between elements in pixels.
        /// </summary>
        public int ElementHorizontalSpacing
        {
            get { return _elementHorizontalSpacing; }
            set { _elementHorizontalSpacing = value; }
        }

        /// <summary>
        /// Gets or sets a vertical spacing between elements in pixels.
        /// </summary>
        public int ElementsVerticalSpacing
        {
            get { return _elementsVerticalSpacing; }
            set { _elementsVerticalSpacing = value; }
        }

        /// <summary>
        /// Gets or sets a font of element labels.
        /// </summary>
        public Font ElementFont
        {
            get { return _elementFont; }
            set { _elementFont = value; }
        }

        /// <summary>
        /// Gets or sets a font used to 
        /// display a caption of this legend.
        /// </summary>
        public Font CaptionFont
        {
            get { return _captionFont; }
            set { _captionFont = value; }
        }

        /// <summary>
        /// Gets or sets a color of element labels.
        /// </summary>
        public Color ElementLabelsColor
        {
            get { return _elementLabelsColor; }
            set { _elementLabelsColor = value; }
        }

        /// <summary>
        /// Gets or sets a color of caption.
        /// </summary>
        public Color CaptionColor
        {
            get { return _captionColor; }
            set { _captionColor = value; }
        }

        /// <summary>
        /// Gets or sets a background color of this legeng.
        /// </summary>
        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set { _backgroundColor = value; }
        }

        private int drawElement(Graphics g, LegendElement element, int currentHeight)
        {
            SizeF labelSize = g.MeasureString(element.Label, _elementFont);

            int imageHeightSpacing = 0;
            if (element.Image.Height < labelSize.Height)
                imageHeightSpacing = (int)labelSize.Height / 2 - element.Image.Height / 2;
            g.DrawImageUnscaled(element.Image, new Point(_margin, currentHeight + imageHeightSpacing));

            int labelHeightSpacing = 0;
            if (element.Image.Height > labelSize.Height)
                labelHeightSpacing = element.Image.Height / 2 - (int)labelSize.Height / 2;
            g.DrawString(element.Label, _elementFont, new SolidBrush(_elementLabelsColor),
                new Point(_margin + element.Image.Width + _elementHorizontalSpacing, currentHeight + labelHeightSpacing));

            return Math.Max(element.Image.Height, (int)labelSize.Height);
        }

        /// <summary>
        /// Generates a legend bitmap.
        /// </summary>
        public Bitmap DrawLegend()
        {
            Bitmap bmp = new Bitmap(_width, _height);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                // фон
                g.Clear(_backgroundColor);

                // заголовок
                SizeF captionSize = g.MeasureString(Caption, _captionFont);
                g.DrawString(Caption, _captionFont, new SolidBrush(_captionColor), new PointF(_width / 2 - captionSize.Width / 2, _margin));

                int currentHieght = _margin + (int)captionSize.Height + _elementsVerticalSpacing;

                // элементы
                foreach (LegendElement el in Elements)
                {
                    currentHieght += drawElement(g, el, currentHieght) + _elementsVerticalSpacing;
                }
            }

            return bmp;
        }
    }
}