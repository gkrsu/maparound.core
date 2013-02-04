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
** File: FeatureAppearance.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Classes that describes feature appearance on the map
**
=============================================================================*/

namespace MapAround.Mapping
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Drawing;
    using System.Drawing.Drawing2D;

    /// <summary>
    /// Defines a polygon rendering style.
    /// </summary>
    [Serializable]
    public class PolygonStyle : ICloneable
    {
        private Color _borderColor = Color.Black;
        private float _borderWidth = 1;
        private DashStyle _borderDashStyle = DashStyle.Solid;
        private DashCap _borderDashCap = DashCap.Flat;
        private bool _polygonBorderVisible = true;

        private HatchStyle _hatchStyle = HatchStyle.Percent50;
        private Bitmap _originalBrushPattern = null;
        private Bitmap _brushPattern = null;
        private Color _fillForeColor = Color.White;
        private Color _fillBackColor = Color.White;

        private bool _useHatch = false;
        private BuiltInFillPatterns _fillPattern = BuiltInFillPatterns.None;



        private void brushPatternParamsChanged()
        {
            if (_originalBrushPattern == null)
            {
                _brushPattern = null;
                return;
            }

            if (_brushPattern != null)
                _brushPattern.Dispose();

            _brushPattern = new Bitmap(_originalBrushPattern.Width, _originalBrushPattern.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            for(int i = 0; i < _brushPattern.Width; i++)
                for (int j = 0; j < _brushPattern.Height; j++)
                {
                    Color c = _originalBrushPattern.GetPixel(i, j);

                    Color blendedColor = 
                        Color.FromArgb(
                            RenderingUtils.BlendPixels(_fillBackColor.ToArgb(), Color.FromArgb(255 - c.R, _fillForeColor).ToArgb()));

                    _brushPattern.SetPixel(i, j, blendedColor);
                }
        }

        private void checkBrushPattern(Bitmap bmp)
        {
            if (bmp == null)
                return;

            if (bmp.Width > 32 || bmp.Height > 32)
                throw new InvalidOperationException("Fill pattern is too big.");

            for (int i = 0; i < bmp.Width; i++)
                for (int j = 0; j < bmp.Height; j++)
                {
                    Color c = bmp.GetPixel(i, j);
                    if (c.R != c.G || c.B != c.G)
                        throw new InvalidOperationException("Fill pattern should be grayscale");
                }
        }

        /// <summary>
        /// 
        /// </summary>
        internal Bitmap FillPatternInternal
        {
            get { return _brushPattern; }
            set 
            {
                checkBrushPattern(value);
                _originalBrushPattern = value;
                brushPatternParamsChanged();
            }
        }

        /// <summary>
        /// Gets or sets a fill pattern.
        /// This value has a priority over the UseHatch 
        /// and HatchStyle properties.
        /// </summary>
        public BuiltInFillPatterns FillPattern
        {
            get { return _fillPattern; }
            set
            {
                _fillPattern = value;
                if (value == BuiltInFillPatterns.None)
                    FillPatternInternal = null;
                else
                {
                    lock (BuiltInFillPatternsAcccessor.SyncRoot)
                        FillPatternInternal = BuiltInFillPatternsAcccessor.Bitmaps[(int)value];
                }
            }
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public object Clone()
        {
            PolygonStyle ps = (PolygonStyle)MemberwiseClone();

            if (_brushPattern != null)
            {
                _brushPattern = (Bitmap)_brushPattern.Clone();
                _originalBrushPattern = (Bitmap)_originalBrushPattern.Clone();
                ps.brushPatternParamsChanged();
            }

            return ps;
        }

        /// <summary>
        /// Gets or sets a polygon border color.
        /// </summary>
        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; }
        }

        /// <summary>
        /// Gets or sets a foreground color 
        /// of the polygon interior.
        /// </summary>
        public Color FillForeColor
        {
            get { return _fillForeColor; }
            set 
            { 
                _fillForeColor = value;
                lock (HatchFillPatternsAcccessor.SyncRoot)
                    brushPatternParamsChanged();
            }
        }

        /// <summary>
        /// Gets or sets a background color 
        /// of the polygon interior.
        /// </summary>
        public Color FillBackColor
        {
            get { return _fillBackColor; }
            set 
            {
                _fillBackColor = value;
                lock (HatchFillPatternsAcccessor.SyncRoot)
                    brushPatternParamsChanged();
            }
        }

        /// <summary>
        /// Gets or sets a with of the 
        /// polygon border.
        /// </summary>
        public float BorderWidth
        {
            get { return _borderWidth; }
            set { _borderWidth = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether
        /// a polygon border is visible.
        /// </summary>
        public bool BorderVisible
        {
            get { return _polygonBorderVisible; }
            set { _polygonBorderVisible = value; }
        }

        /// <summary>
        /// Gets or sets a dash cap 
        /// of the polygon border.
        /// </summary>
        public DashCap BorderDashCap
        {
            get { return _borderDashCap; }
            set { _borderDashCap = value; }
        }

        /// <summary>
        /// Gets or sets a filling 
        /// hatch style.
        /// </summary>
        public HatchStyle HatchStyle
        {
            get { return _hatchStyle; }
            set 
            { 
                _hatchStyle = value;

                lock (HatchFillPatternsAcccessor.SyncRoot)
                    FillPatternInternal = HatchFillPatternsAcccessor.Bitmaps[(int)HatchStyle];
            }
        }

        /// <summary>
        /// Gets of sets a value indicating 
        /// whether a hatch will be used
        /// for filling.
        /// </summary>
        public bool UseHatch
        {
            get { return _useHatch; }
            set 
            {
                if (!value)
                    FillPattern = FillPattern;
                else
                {
                    lock (HatchFillPatternsAcccessor.SyncRoot)
                        FillPatternInternal = HatchFillPatternsAcccessor.Bitmaps[(int)HatchStyle];
                }

                _useHatch = value;
            }
        }

        /// <summary>
        /// Gets or sets a dash style 
        /// of the border.
        /// </summary>
        public DashStyle BorderDashStyle
        {
            get { return _borderDashStyle; }
            set { _borderDashStyle = value; }
        }

        /// <summary/>                
        public Brush GetBrush()
        {
            Brush b;
            if (_brushPattern != null)
            {
                b = new TextureBrush(_brushPattern);
            }
            else if (_useHatch)
            {

                //b = new HatchBrush(_hatchStyle, _fillForeColor, _fillBackColor);

                if (_brushPattern == null)
                    this.HatchStyle = this.HatchStyle;

                b = new TextureBrush(_brushPattern);

            }
            else
                b = new SolidBrush(_fillForeColor);

            return b;
        }
        /// <summary/>
        public Pen GetPen()
        {
            Pen p = new Pen(_borderColor, _borderWidth);
            p.DashStyle = _borderDashStyle;
            p.DashCap = _borderDashCap;
            return p;
        }
    }

    /// <summary>
    /// Defines a polyline rendering style.
    /// </summary>
    [Serializable]
    public class PolylineStyle : ICloneable
    {
        private float _width = 1;

        private bool _useAnnexLine = false;

        private Color _color = Color.Black;
        private DashStyle _dashStyle = DashStyle.Solid;
        private DashCap _dashCap = DashCap.Flat;
        private float[] _dashPattern = null;

        private bool _isCompound = false;
        private float[] _compound = null;

        private Color _annexColor = Color.Black;
        private DashStyle _annexDashStyle = DashStyle.Solid;
        private DashCap _annexDashCap = DashCap.Flat;
        private float[] _annexDashPattern = null;

        private bool _isAnnexCompound = false;
        private float[] _annexCompound = null;

        private bool _useOutline = false;
        private int _outlineWidth = 1;
        private Color _outlineColor = Color.Black;

        /// <summary>
        /// Gets or sets a value indicating wheter
        /// an annex line is rendered.
        /// </summary>
        public bool UseAnnexLine
        {
            get { return _useAnnexLine; }
            set { _useAnnexLine = value; }
        }

        /// <summary>
        /// Gets or sets a compound
        /// of the annex line.
        /// </summary>
        public float[] AnnexCompound
        {
            get { return _annexCompound; }
            set { _annexCompound = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether 
        /// the annex line has a compound.
        /// </summary>
        public bool IsAnnexCompound
        {
            get { return _isAnnexCompound; }
            set { _isAnnexCompound = value; }
        }

        /// <summary>
        /// Gets or sets a dash pattern 
        /// of the annex line.
        /// </summary>
        public float[] AnnexDashPattern
        {
            get { return _annexDashPattern; }
            set { _annexDashPattern = value; }
        }

        /// <summary>
        /// Gets or sets a dash cap 
        /// of the annex line.
        /// </summary>
        public DashCap AnnexDashCap
        {
            get { return _annexDashCap; }
            set { _annexDashCap = value; }
        }

        /// <summary>
        /// Gets or sets a color of the annex line.
        /// </summary>
        public Color AnnexColor
        {
            get { return _annexColor; }
            set { _annexColor = value; }
        }

        /// <summary>
        /// Gets or sets a dash style 
        /// of the annex line.
        /// </summary>
        public DashStyle AnnexDashStyle
        {
            get { return _annexDashStyle; }
            set { _annexDashStyle = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether 
        /// the main line has a compound.
        /// </summary>
        public bool IsCompound
        {
            get { return _isCompound; }
            set { _isCompound = value; }
        }

        /// <summary>
        /// Gets or sets a dash pattern 
        /// of the main line.
        /// </summary>
        public float[] Compound
        {
            get { return _compound; }
            set { _compound = value; }
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Gets or sets a dash pattern 
        /// of the main line.        
        /// </summary>
        public float[] DashPattern
        {
            get { return _dashPattern; }
            set { _dashPattern = value; }
        }

        /// <summary>
        /// Gets or sets an outline color.
        /// </summary>
        public Color OutlineColor
        {
            get { return _outlineColor; }
            set { _outlineColor = value; }
        }

        /// <summary>
        /// Gets or sets an outline width in pixels.
        /// </summary>
        public int OutlineWidth
        {
            get { return _outlineWidth; }
            set { _outlineWidth = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating 
        /// whether an outline is visible.
        /// </summary>
        public bool UseOutline
        {
            get { return _useOutline; }
            set { _useOutline = value; }
        }

        /// <summary>
        /// Gets or sets a dash cap 
        /// of the main line.
        /// </summary>
        public DashCap DashCap
        {
            get { return _dashCap; }
            set { _dashCap = value; }
        }

        /// <summary>
        /// Gets or seta a color ot the main line.
        /// </summary>
        public Color Color
        {
            get { return _color; }
            set { _color = value; }
        }

        /// <summary>
        /// Gets or sets a width in pixels.
        /// </summary>
        public float Width
        {
            get { return _width; }
            set { _width = value; }
        }

        /// <summary>
        /// Gets or sets a dash style 
        /// of the main line.
        /// </summary>
        public DashStyle DashStyle
        {
            get { return _dashStyle; }
            set { _dashStyle = value; }
        }
        /// <summary/>
        public Pen GetPen()
        {
            Pen p = new Pen(_color, _width);
            p.DashStyle = _dashStyle;
            p.DashCap = _dashCap;
            p.MiterLimit = 3f;
            p.LineJoin = LineJoin.MiterClipped;
            if (p.DashStyle == DashStyle.Custom)
                if (_dashPattern != null)
                {
                    p.DashPattern = _dashPattern;
                }
                else
                    p.DashStyle = DashStyle.Solid;

            if (_isCompound && _compound != null)
                p.CompoundArray = _compound;

            return p;
        }
        /// <summary/>
        public Pen GetAnnexPen()
        {
            if (!_useAnnexLine)
                return null;

            Pen p = new Pen(_annexColor, _width);
            p.DashStyle = _annexDashStyle;
            p.DashCap = _annexDashCap;
            p.MiterLimit = 3f;
            p.LineJoin = LineJoin.MiterClipped;
            if (p.DashStyle == DashStyle.Custom)
                if (_annexDashPattern != null)
                {
                    p.DashPattern = _annexDashPattern;
                }
                else
                    p.DashStyle = DashStyle.Solid;

            if (_isAnnexCompound && _annexCompound != null)
                p.CompoundArray = _annexCompound;

            return p;
        }
        /// <summary/>
        public Pen GetOutlinePen()
        {
            Pen p = new Pen(_outlineColor, _width + _outlineWidth * 2);
            p.MiterLimit = 3f;
            p.LineJoin = LineJoin.MiterClipped;
            return p;
        }
    }

    /// <summary>
    /// Defines a title rendering style.
    /// </summary>
    [Serializable]
    public class TitleStyle : ICloneable
    {
        private bool _titlesVisible = false;
        private bool _leadAlong = false;
        private string _fontFamily = "Microsoft Sans Serif";
        private float _fontSize = 8;
        private FontStyle _fontStyle = FontStyle.Regular;
        private bool _useOutline = false;
        private int _outlineSize = 2;
        private double _visibleScale = 10;
        private int _renderPriority = 0;
        private Color _color = Color.Black;

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Gets or sets a value indicating whether
        /// a hallo-like outline is visible.
        /// </summary>
        public bool UseOutline
        {
            get { return _useOutline; }
            set { _useOutline = value; }
        }

        /// <summary>
        /// Gets or sets a size inpixels 
        /// of the a hallo-like outline.
        /// </summary>
        public int OutlineSize
        {
            get { return _outlineSize; }
            set { _outlineSize = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating
        /// whether a titles will lead
        /// along features when possible.
        /// </summary>
        public bool LeadAlong
        {
            get { return _leadAlong; }
            set { _leadAlong = value; }
        }

        /// <summary>
        /// Gets or sets a render ptiority value.
        /// Titles with greater priority are rendered first.
        /// </summary>
        public int RenderPriority
        {
            get { return _renderPriority; }
            set { _renderPriority = value; }
        }

        /// <summary>
        /// Gets or sets a font family which is 
        /// used to render the title.
        /// </summary>
        public string FontFamily
        {
            get { return _fontFamily; }
            set { _fontFamily = value; }
        }

        /// <summary>
        /// Gets or sets a size of font.
        /// </summary>
        public float FontSize
        {
            get { return _fontSize; }
            set { _fontSize = value; }
        }

        /// <summary>
        /// Gets or sets a style of font.
        /// </summary>
        public FontStyle FontStyle
        {
            get { return _fontStyle; }
            set { _fontStyle = value; }
        }

        /// <summary>
        /// Gets or sets a color.
        /// </summary>
        public Color Color
        {
            get { return _color; }
            set { _color = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating 
        /// whether a title is visible.
        /// </summary>
        public bool TitlesVisible
        {
            get { return _titlesVisible; }
            set { _titlesVisible = value; }
        }

        /// <summary>
        /// Gets or sets a maximum scale (pixels in map unit) 
        /// at which the title should be visible.
        /// </summary>
        public double VisibleScale
        {
            get { return _visibleScale; }
            set { _visibleScale = value; }
        }

        internal Font GetFont()
        {
            Font f = new Font(_fontFamily, _fontSize, _fontStyle, GraphicsUnit.Pixel);
            return f;
        }
    }

    /// <summary>
    /// Defines a types of point 
    /// feature display.
    /// </summary>
    public enum PointDisplayKind : int 
    {
        /// <summary>
        /// Symbol.
        /// <para>
        /// Point feature is displayd as a symbol.
        /// </para>
        /// </summary>
        Symbol = 0,
        /// <summary>
        /// Image.
        /// <para>
        /// Point feature is displayd 
        /// as a raster image.
        /// </para>
        /// </summary>
        Image = 1
    }

    /// <summary>
    /// Defines a point rendering style.
    /// </summary>
    [Serializable]
    public class PointStyle : ICloneable
    {
        private int _size = 24;
        private char _symbol = 'A';
        private Color _color = Color.Black;
        private System.Drawing.ContentAlignment _contentAlignment = ContentAlignment.MiddleCenter;
        
        private Bitmap _image;
        private string _fontName = string.Empty;

        private PointDisplayKind _displayKind = PointDisplayKind.Symbol;

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public object Clone()
        {
            PointStyle ps = (PointStyle)MemberwiseClone();

            if (_image != null)
                ps._image = (Bitmap)_image.Clone();

            if (!string.IsNullOrEmpty(_fontName))
                ps._fontName = _fontName;
            ps._contentAlignment = _contentAlignment;

            return ps;
        }

        /// <summary>
        /// Gets or sets an object defining 
        /// a display type of point object.
        /// </summary>
        public PointDisplayKind DisplayKind
        {
            get { return _displayKind; }
            set { _displayKind = value; }
        }

        /// <summary>
        /// Gets or sets o font family which is used 
        /// to display a symbol of point feature.
        /// </summary>
        public string FontName
        {
            get { return _fontName; }
            set { _fontName = value; }
        }

        /// <summary>
        /// Gets or sets an image which is displayed 
        /// at the point feature position.
        /// </summary>
        public Bitmap Image
        {
            get { return _image; }
            set { _image = value; }
        }

        /// <summary>
        /// Gets or sets a symbol which is displayed 
        /// at the point feature position.
        /// </summary>
        public char Symbol
        {
            get { return _symbol; }
            set { _symbol = value; }
        }

        /// <summary>
        /// Gets or sets a syze of vector symbol.
        /// </summary>
        public int Size
        {
            get { return _size; }
            set { _size = value; }
        }

        /// <summary>
        /// Gets or sets a color of vector symbol.
        /// </summary>
        public Color Color
        {
            get { return _color; }
            set { _color = value; }
        }
        
        /// <summary>
        /// Location relative to the center of the object.
        /// </summary>
        public ContentAlignment ContentAlignment
        {
            get { return _contentAlignment; }
            set { _contentAlignment = value; }

        }
    }

    /// <summary>
    /// Represents an object defining the settings 
    /// of automatic displaying titles.
    /// </summary>
    [Serializable]
    public class AutoTitleSettings : ICloneable
    {
        private string _attributeName = string.Empty;
        private int _attributeIndex = -1;

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Gets or sets an index ot the non-spatial attribute
        /// which value is used to displaying a title.
        /// </summary>
        public int AttributeIndex
        {
            get { return _attributeIndex; }
            set { _attributeIndex = value; }
        }

        /// <summary>
        /// Gets or sets a name ot the non-spatial attribute
        /// which value is used to displaying a title.
        /// </summary>
        public string AttributeName
        {
            get { return _attributeName; }
            set { _attributeName = value; }
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Mapping.AutoTitleSettings.
        /// </summary>
        public AutoTitleSettings(string attributeName, int attributeIndex)
        {
            _attributeName = attributeName;
            _attributeIndex = attributeIndex;
        }
    }


    #region BuiltIn Fill Patterns.

    /// <summary>
    /// Represents a builtin fill patterns.
    /// </summary>
    public enum BuiltInFillPatterns : int
    { 
        /// <summary>
        /// Missing pattern.
        /// </summary>
        None = -1,
        /// <summary>
        /// Pattern 1.
        /// </summary>
        Pattern1 = 0,
        /// <summary>
        /// Pattern 2.
        /// </summary>
        Pattern2 = 1,
        /// <summary>
        /// Pattern 3.
        /// </summary>
        Pattern3 = 2,
        /// <summary>
        /// Pattern 4.
        /// </summary>
        Pattern4 = 3,
        /// <summary>
        /// Pattern 5.
        /// </summary>
        Pattern5 = 4,
        /// <summary>
        /// Pattern 6.
        /// </summary>
        Pattern6 = 5,
        /// <summary>
        /// Pattern 7.
        /// </summary>
        Pattern7 = 6,
        /// <summary>
        /// Pattern 8.
        /// </summary>
        Pattern8 = 7,
        /// <summary>
        /// Pattern 9.
        /// </summary>
        Pattern9 = 8,
        /// <summary>
        /// Pattern 10.
        /// </summary>
        Pattern10 = 9,
        /// <summary>
        /// Pattern 11.
        /// </summary>
        Pattern11 = 10,
        /// <summary>
        /// Pattern 12.
        /// </summary>
        Pattern12 = 11,
        /// <summary>
        /// Pattern 13.
        /// </summary>
        Pattern13 = 12,
        /// <summary>
        /// Pattern 14.
        /// </summary>
        Pattern14 = 13,
        /// <summary>
        /// Pattern 15.
        /// </summary>
        Pattern15 = 14,
        /// <summary>
        /// Pattern 16.
        /// </summary>
        Pattern16 = 15,
        /// <summary>
        /// Pattern 17.
        /// </summary>
        Pattern17 = 16,
        /// <summary>
        /// Pattern 18.
        /// </summary>
        Pattern18 = 17,
        /// <summary>
        /// Pattern 19.
        /// </summary>
        Pattern19 = 18,
        /// <summary>
        /// Pattern 20.
        /// </summary>
        Pattern20 = 19,
        /// <summary>
        /// Pattern 21.
        /// </summary>
        Pattern21 = 20,
        /// <summary>
        /// Pattern 22.
        /// </summary>
        Pattern22 = 21,
        /// <summary>
        /// Pattern 23.
        /// </summary>
        Pattern23 = 22,
        /// <summary>
        /// Pattern 24.
        /// </summary>
        Pattern24 = 23,
        /// <summary>
        /// Pattern 25.
        /// </summary>
        Pattern25 = 24,
        /// <summary>
        /// Minimal pattern.
        /// </summary>
        Min = Pattern1,
        /// <summary>
        /// Maximal pattern.
        /// </summary>
        Max = Pattern25
    }

    /// <summary>
    /// Provides an access to built-in patterns.
    /// </summary>
    internal static class BuiltInFillPatternsAcccessor
    {
        private static List<Bitmap> _bitmaps;

        public static object SyncRoot = new object();

        /// <summary>
        /// Gets a collecion containing a bitmaps 
        /// of builtin patterns.
        /// </summary>
        public static ReadOnlyCollection<Bitmap> Bitmaps
        { 
            get{return _bitmaps.AsReadOnly();}
        }

        static BuiltInFillPatternsAcccessor()
        {
            _bitmaps = new List<Bitmap>(
                new Bitmap[] {
                    MapAround.Properties.Resources.brush1,
                    MapAround.Properties.Resources.brush2,
                    MapAround.Properties.Resources.brush3,
                    MapAround.Properties.Resources.brush4,
                    MapAround.Properties.Resources.brush5,
                    MapAround.Properties.Resources.brush6,
                    MapAround.Properties.Resources.brush7,
                    MapAround.Properties.Resources.brush8,
                    MapAround.Properties.Resources.brush9,
                    MapAround.Properties.Resources.brush10,
                    MapAround.Properties.Resources.brush11,
                    MapAround.Properties.Resources.brush12,
                    MapAround.Properties.Resources.brush13,
                    MapAround.Properties.Resources.brush14,
                    MapAround.Properties.Resources.brush15,
                    MapAround.Properties.Resources.brush16,
                    MapAround.Properties.Resources.brush17,
                    MapAround.Properties.Resources.brush18,
                    MapAround.Properties.Resources.brush19,
                    MapAround.Properties.Resources.brush20,
                    MapAround.Properties.Resources.brush21,
                    MapAround.Properties.Resources.brush22,
                    MapAround.Properties.Resources.brush23,
                    MapAround.Properties.Resources.brush24,
                    MapAround.Properties.Resources.brush25,
                });
        }
    }

    #endregion

    #region Hatch Patterns (workaround a mono bug)

    /// <summary>
    /// Provides an access to patterns coincides to the hatch styles.
    /// This allows to workaround a mono bug (incorrect opacity handling 
    /// in hatch styles).
    /// </summary>
    internal static class HatchFillPatternsAcccessor
    {
        private static List<Bitmap> _bitmaps;

        public static object SyncRoot = new object();

        /// <summary>
        /// Gets a collecion containing a bitmaps 
        /// of hatch patterns.
        /// </summary>
        public static ReadOnlyCollection<Bitmap> Bitmaps
        {
            get { return _bitmaps.AsReadOnly(); }
        }

        static HatchFillPatternsAcccessor()
        {
            _bitmaps = new List<Bitmap>();
            
            Array values = Enum.GetValues(typeof(HatchStyle));
            for (int i = 0; i < 53; i++)
            {
                Bitmap bmp = new Bitmap(8, 8);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.HighSpeed;
                    g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                    g.Clear(Color.White);
                    using (Brush brush = new HatchBrush((HatchStyle)i, Color.Black, Color.White))
                    {
                        g.FillRectangle(brush, 0, 0, 8, 8);
                    }
                }

                _bitmaps.Add(bmp);
            }
        }
    }

    #endregion
}