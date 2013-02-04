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
** File: Rendering.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Classes and interfaces for feature rendering
**
=============================================================================*/

namespace MapAround.Mapping
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Drawing;
    using System.Drawing.Drawing2D;

    using MapAround.Geometry;

    /// <summary>
    /// Provides access to members of object that draws features on the map.
    /// MapAround.Mapping.Map.FeatureRenderer can be assigned with implementing objects.
    /// </summary>
    public interface IFeatureRenderer
    {
        /// <summary>
        /// Gets or sets a mask color of selected feature.
        /// </summary>
        Color SelectionColor
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether 
        /// the subpixel details of features will be 
        /// rendered.
        /// <remarks>
        /// Filtering of small details performs every time the map renders
        /// and may take some time, which depends on the selected scale 
        /// (pixel size). This option is useful only if the time of filtering
        /// and the time of rendering filtered features is less than the time 
        /// of rendering non-filtered features.
        /// </remarks>
        /// </summary>
        bool ReduceSubpixelDetails
        {
            get;
            set;
        }

        /// <summary>
        /// Draws a point feature.
        /// </summary>
        /// <param name="feature">A point feature to draw</param>
        /// <param name="g">A System.Drawing.Graphics instance that represents a surface for drawing a feature</param>
        /// <param name="style">An object defining point style</param>
        /// <param name="titleStyle">An object defining title style</param>
        /// <param name="viewBox">A bounding rectangle dfefining the drawing area</param>
        /// <param name="titleVisible">A value indicating whether a title is visible. This value overrides titleStyle.TitlesVisible.</param>
        /// <param name="scaleFactor">A number of pixels per map unit</param>
        void DrawPoint(Feature feature, Graphics g, PointStyle style, TitleStyle titleStyle, BoundingRectangle viewBox, bool titleVisible, double scaleFactor);

        /// <summary>
        /// Draws a polyline feature.
        /// </summary>
        /// <param name="feature">A polyline feature to draw</param>
        /// <param name="g">A System.Drawing.Graphics instance that represents a surface for drawing a feature</param>
        /// <param name="style">An object defining polyline style</param>
        /// <param name="titleStyle">An object defining title style</param>
        /// <param name="viewBox">A bounding rectangle dfefining the drawing area</param>
        /// <param name="titleVisible">A value indicating whether a title is visible. This value overrides titleStyle.TitlesVisible.</param>
        /// <param name="scaleFactor">A number of pixels per map unit</param>
        void DrawPolyline(Feature feature, Graphics g, PolylineStyle style, TitleStyle titleStyle, BoundingRectangle viewBox, bool titleVisible, double scaleFactor);

        /// <summary>
        /// Draws a polygon feature.
        /// </summary>
        /// <param name="feature">A polygon feature to draw</param>
        /// <param name="g">A System.Drawing.Graphics instance that represents a surface for drawing a feature</param>
        /// <param name="style">An object defining polygon style</param>
        /// <param name="titleStyle">An object defining title style</param>
        /// <param name="viewBox">A bounding rectangle dfefining the drawing area</param>
        /// <param name="titleVisible">A value indicating whether a title is visible. This value overrides titleStyle.TitlesVisible.</param>
        /// <param name="scaleFactor">A number of pixels per map unit</param>
        void DrawPolygon(Feature feature, Graphics g, PolygonStyle style, TitleStyle titleStyle, BoundingRectangle viewBox, bool titleVisible, double scaleFactor);

        /// <summary>
        /// Draws selected features.
        /// </summary>
        /// <param name="g">A System.Drawing.Graphics instance that represents a surface for drawing features</param>
        /// <param name="viewBox">A bounding rectangle dfefining the drawing area</param>
        /// <param name="scaleFactor">A number of pixels per map unit</param>
        void FlushSelectedFeatures(Graphics g, BoundingRectangle viewBox, double scaleFactor);

        /// <summary>
        /// Draws titles of features.
        /// </summary>
        /// <param name="g">A System.Drawing.Graphics instance that represents a surface for drawing features</param>
        /// <param name="viewBox">A bounding rectangle dfefining the drawing area</param>
        /// <param name="scaleFactor">A number of pixels per map unit</param>
        void FlushTitles(Graphics g, BoundingRectangle viewBox, double scaleFactor);
    }

    /// <summary>
    /// Implements the MapAround.Mapping.IFeatureRenderer interface.
    /// </summary>
    internal class DefaultFeatureRenderer : IFeatureRenderer
    {
        private static readonly string _symbolsDefaultFontName = "GeographicSymbols";

        //private Color _selectionColor = Color.FromArgb(220, 90, 90);
        private Color _selectionColor = Color.FromArgb(0, 0, 255);
        private bool _selectionColorChanged = true;

        private Bitmap _selectionTexture = null;

        private readonly double _labelRotationDeltaMax = 0.7;
        private readonly double _labelRotationDeltaMin = 0;

        private static Color _titleOutlineColor = Color.FromArgb(170, 255, 255, 255);
        private static StringFormat _titleStringFormat;
        private static StringFormat _symbolStringFormat;
        private bool _isSelectionRendering = false;

        private void createSelectionTexture()
        {
            lock (HatchFillPatternsAcccessor.SyncRoot)
            {
                Bitmap originalPattern = HatchFillPatternsAcccessor.Bitmaps[(int)HatchStyle.Percent50];

                if (_selectionTexture != null)
                    _selectionTexture.Dispose();

                _selectionTexture = new Bitmap(originalPattern.Width, originalPattern.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                for (int i = 0; i < _selectionTexture.Width; i++)
                    for (int j = 0; j < _selectionTexture.Height; j++)
                    {
                        Color c = originalPattern.GetPixel(i, j);

                        Color blendedColor =
                            Color.FromArgb(
                                RenderingUtils.BlendPixels(Color.Transparent.ToArgb(), Color.FromArgb((255 - c.R) / 2, _selectionColor).ToArgb()));

                        _selectionTexture.SetPixel(i, j, blendedColor);
                    }
            }

            _selectionColorChanged = false;
        }

        public DefaultFeatureRenderer()
        {
            _titleStringFormat = new StringFormat(StringFormat.GenericTypographic);
            _titleStringFormat.Alignment = StringAlignment.Center;
            _titleStringFormat.LineAlignment = StringAlignment.Center;

            _symbolStringFormat = new StringFormat(StringFormat.GenericTypographic);
            _symbolStringFormat.Alignment = StringAlignment.Center;
            _symbolStringFormat.LineAlignment = StringAlignment.Near;
        }

        /// <summary>
        /// Buffer element labels.
        /// </summary>
        [Serializable]
        public class TitleBufferElement
        {
            private TitleStyle _style;
            private string _title;
            private Segment _box;
            private bool _hasRendered = false;
            private bool _isSimple = true;
            private FollowingTitle _followingTitle;
            private int _titleCount;

            public int Number
            {
                get { return _titleCount; }
            }

            /// <summary>
            /// Gets label located along the object.
            /// </summary>
            public FollowingTitle FollowingTitle
            {
                get { return _followingTitle; }
            }

            /// <summary>
            /// Gets a value indicating whether the label is simple.
            /// If false, then the inscription should be placed along the object.
            /// </summary>
            public bool IsSimple
            {
                get { return _isSimple; }
                set { _isSimple = value; }
            }

            /// <summary>
            /// Gets a value indicating whether the legend is displayed on the map.
            /// </summary>
            public bool HasRendered
            {
                get { return _hasRendered; }
                set { _hasRendered = value; }
            }

            /// <summary>
            /// Gets the value of the segment defining the lower-left and 
            /// upper-right corner of the bounding rectangle of the image caption.
            /// </summary>
            public Segment Box
            {
                get { return _box; }
            }

            /// <summary>
            /// Gets the style of the inscription.
            /// </summary>
            public TitleStyle Style
            {
                get { return _style; }
            }

            /// <summary>
            /// Gets the row labels. Only used to display simple labels.
            /// </summary>
            public string Title
            {
                get { return _title; }
            }

            private bool intersectsWithComplexTitle(TitleBufferElement complexOne)
            {
                if (!PlanimetryAlgorithms.AreRectanglesIntersect(_box, complexOne._box)) 
                    return false;

                Segment[] segments = new Segment[4]
                {
                    new Segment(_box.V1, PlanimetryEnvironment.NewCoordinate(_box.V1.X, _box.V2.Y)),
                    new Segment(PlanimetryEnvironment.NewCoordinate(_box.V1.X, _box.V2.Y), _box.V2),
                    new Segment(_box.V2, PlanimetryEnvironment.NewCoordinate(_box.V2.X, _box.V1.Y)),
                    new Segment(PlanimetryEnvironment.NewCoordinate(_box.V2.X, _box.V1.Y), _box.V1)
                };

                ICoordinate pointStub = null;
                Segment segmentStub = new Segment();

                foreach (Contour c in complexOne._followingTitle.EnvelopePolygon.Contours)
                {
                    Segment segment1 = new Segment(c.Vertices[0], c.Vertices[1]);
                    Segment segment2 = new Segment(c.Vertices[2], c.Vertices[3]);
                    for(int i = 0; i < 4; i++)
                        if (PlanimetryAlgorithms.SegmentsIntersection(segment1, segments[i], out pointStub, out segmentStub) != Dimension.None ||
                            PlanimetryAlgorithms.SegmentsIntersection(segment2, segments[i], out pointStub, out segmentStub) != Dimension.None)
                            return true;
                }

                return false;
            }

            private static bool contoursIntersect(Contour c1, Contour c2)
            {
                if (!c1.GetBoundingRectangle().Intersects(c2.GetBoundingRectangle()))
                    return false;

                ICoordinate pointStub = null;
                Segment segmentStub = new Segment();

                Segment segment1 = new Segment(c1.Vertices[0], c1.Vertices[1]);
                Segment segment2 = new Segment(c1.Vertices[2], c1.Vertices[3]);
                Segment segment3 = new Segment(c2.Vertices[0], c2.Vertices[1]);
                Segment segment4 = new Segment(c2.Vertices[3], c2.Vertices[3]);

                if (PlanimetryAlgorithms.SegmentsIntersection(segment1, segment3, out pointStub, out segmentStub) != Dimension.None ||
                    PlanimetryAlgorithms.SegmentsIntersection(segment1, segment4, out pointStub, out segmentStub) != Dimension.None ||
                    PlanimetryAlgorithms.SegmentsIntersection(segment2, segment3, out pointStub, out segmentStub) != Dimension.None ||
                    PlanimetryAlgorithms.SegmentsIntersection(segment2, segment4, out pointStub, out segmentStub) != Dimension.None)
                    return true;

                return false;
            }

            private static bool complexTitlesIntersect(TitleBufferElement element1, TitleBufferElement element2)
            {
                if (!PlanimetryAlgorithms.AreRectanglesIntersect(element1.Box, element2.Box))
                    return false;

                foreach (Contour c1 in element1.FollowingTitle.EnvelopePolygon.Contours)
                    foreach (Contour c2 in element1.FollowingTitle.EnvelopePolygon.Contours)
                        if (contoursIntersect(c1, c2))
                            return true;

                return false;
            }

            public bool IntersectsWith(TitleBufferElement element)
            {
                if (_isSimple && element.IsSimple)
                    return PlanimetryAlgorithms.AreRectanglesIntersect(_box, element._box);
                else 
                if (!_isSimple && !element.IsSimple)
                {
                    return complexTitlesIntersect(this, element);
                }
                else
                if (!_isSimple && element.IsSimple)
                    return element.intersectsWithComplexTitle(this);
                else
                    return intersectsWithComplexTitle(element);
            }

            /// <summary>
            /// Creates an instance of the appropriate TitleBufferElement simple inscription.
            /// </summary>
            public TitleBufferElement(TitleStyle style, Segment box, string title, int number)
            {
                _style = style;
                _title = title;
                _box = box;
                _titleCount = number;
            }

            /// <summary>
            /// Creates a copy of an inscription TitleBufferElement located along the object.
            /// </summary>
            public TitleBufferElement(FollowingTitle followingTitle, TitleStyle style, int number)
            {
                _isSimple = false;
                _style = style;
                _box = new Segment(followingTitle.GetBoundingBox().Min, followingTitle.GetBoundingBox().Max);
                _followingTitle = followingTitle;
                _titleCount = number;
            }
        }
       
        /// <summary>
        /// Element buffer polygons.
        /// </summary>
        protected class PolygonBufferElement
        {
            private Feature _polygon;
            private PolygonStyle _style;
            private TitleStyle _titleStyle;
            private bool _titleVisible;

            public bool TitleVisible
            {
                get { return _titleVisible; }
            }

            public Feature Polygon
            {
                get { return _polygon; }
            }

            public PolygonStyle Style
            {
                get { return _style; }
            }

            public TitleStyle TitleStyle
            {
                get { return _titleStyle; }
            }

            public PolygonBufferElement(Feature polygon, PolygonStyle style, TitleStyle titleStyle, bool titleVisible)
            {
                _style = style;
                _titleStyle = titleStyle;
                _polygon = polygon;
                _titleVisible = titleVisible;
            }
        }

        /// <summary>
        /// The inscription is along the object.
        /// </summary>
        [Serializable]
        public class FollowingTitle
        {
            List<FollowingTitleElement> _elements = new List<FollowingTitleElement>();
            BoundingRectangle _boundingBox = new BoundingRectangle();
            Polygon _envelopePolygon;
            bool _boundingBoxChanged = true;

            public Polygon EnvelopePolygon
            {
                get { return _envelopePolygon; }
                set { _envelopePolygon = value; }
            }

            public ReadOnlyCollection<FollowingTitleElement> Elements
            {
                get { return _elements.AsReadOnly(); }
            }

            public BoundingRectangle GetBoundingBox()
            {
                if (_boundingBoxChanged)
                {
                    _boundingBox = _envelopePolygon.GetBoundingRectangle();
                    _boundingBoxChanged = false;
                }

                return _boundingBox;
            }

            public void AddElement(FollowingTitleElement element)
            {
                Contour c = new Contour();

                foreach (PointF p in element.Corners)
                    c.Vertices.Add(PlanimetryEnvironment.NewCoordinate((float)p.X, (float)p.Y));

                if (_envelopePolygon == null)
                    _envelopePolygon = new Polygon();

                _envelopePolygon.Contours.Add(c);
                _elements.Add(element);
                _boundingBoxChanged = true;
            }
        }

        /// <summary>
        ///Rectilinear element labels placed along the object.
        /// </summary>
         [Serializable]
        public class FollowingTitleElement
        {
            private PointF _translationPoint;
            private PointF _titleOrigin;
            private float _rotationAngle;
            private string _substring;
            private PointF[] _corners;

            public PointF[] Corners
            {
                get { return _corners; }
                set { _corners = value; }
            }

            public float RotationAngle
            {
                get { return _rotationAngle; }
                set { _rotationAngle = value; }
            }

            public PointF TranslationPoint
            {
                get { return _translationPoint; }
                set { _translationPoint = value; }
            }

            public PointF TitleOrigin
            {
                get { return _titleOrigin; }
                set { _titleOrigin = value; }
            }

            public string Substring
            {
                get { return _substring; }
                set { _substring = value; }
            }

            public FollowingTitleElement(PointF translationPoint,
                                         float rotationAngle,
                                         PointF titleOrigin,
                                         string substring,
                                         PointF v1,
                                         PointF v2,
                                         PointF v3,
                                         PointF v4)
            {
                _translationPoint = translationPoint;
                _rotationAngle = rotationAngle;
                _titleOrigin = titleOrigin;
                _substring = substring;
                _corners = new PointF[] { v1, v2, v3, v4 };
            }
        }

        /// <summary>
         /// Buffer element polylines.
        /// </summary>
        
        protected class PolylineBufferElement
        {
            private Feature _polyline;
            private PolylineStyle _style;
            private TitleStyle _titleStyle;
            private bool _titleVisible;

            public bool TitleVisible
            {
                get { return _titleVisible; }
            }

            public Feature Polyline
            {
                get { return _polyline; }
            }

            public PolylineStyle Style
            {
                get { return _style; }
            }

            public TitleStyle TitleStyle
            {
                get { return _titleStyle; }
            }

            public PolylineBufferElement(Feature polyline, PolylineStyle style, TitleStyle titleStyle, bool titleVisible)
            {
                _style = style;
                _titleStyle = titleStyle;
                _polyline = polyline;
                _titleVisible = titleVisible;
            }
        }

        /// <summary>
        /// Buffer element points.
        /// </summary>
        protected class PointBufferElement
        {
            private Feature _point;
            private PointStyle _style;
            private TitleStyle _titleStyle;
            private bool _titleVisible;

            public bool TitleVisible
            {
                get { return _titleVisible; }
            }

            public Feature Polyline
            {
                get { return _point; }
            }

            public PointStyle Style
            {
                get { return _style; }
            }

            public TitleStyle TitleStyle
            {
                get { return _titleStyle; }
            }

            public PointBufferElement(Feature point, PointStyle style, TitleStyle titleStyle, bool titleVisible)
            {
                _style = style;
                _titleStyle = titleStyle;
                _point = point;
                _titleVisible = titleVisible;
            }
        }

        protected List<TitleBufferElement> _titleBuffer = new List<TitleBufferElement>();
        protected List<PolygonBufferElement> _selectedPolygons = new List<PolygonBufferElement>();
        protected List<PolylineBufferElement> _selectedPolylines = new List<PolylineBufferElement>();
        protected List<PointBufferElement> _selectedPoints = new List<PointBufferElement>();
        protected int _titleCount = 0;

        #region IFeatureRenderer members

        /// <summary>
        /// Gets or sets a mask color of selected feature.
        /// </summary>
        public Color SelectionColor
        {
            get { return _selectionColor; }
            set 
            {
                if (!_selectionColor.Equals(value))
                    _selectionColorChanged = true;

                _selectionColor = value; 
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether 
        /// the subpixel details of features will be 
        /// rendered.
        /// <remarks>
        /// Filtering of small details performs every time the map renders
        /// and may take some time, which depends on the selected scale 
        /// (pixel size). This option is useful only if the time of filtering
        /// and the time of rendering filtered features is less than the time 
        /// of rendering non-filtered features.
        /// </remarks>
        /// </summary>
        public bool ReduceSubpixelDetails
        {
            get { return _reduceSubpixelDetails; }
            set { _reduceSubpixelDetails = value; }
        }

        void IFeatureRenderer.DrawPoint(Feature feature, Graphics g, PointStyle style, TitleStyle titleStyle, BoundingRectangle viewBox, bool titleVisible, double scaleFactor)
        {
            DrawPoint(feature, g, style, titleStyle, viewBox, titleVisible, scaleFactor);
        }

   

        void IFeatureRenderer.DrawPolyline(Feature feature, Graphics g, PolylineStyle style, TitleStyle titleStyle, BoundingRectangle viewBox, bool titleVisible, double scaleFactor)
        {
            DrawPolyline(feature, g, style, titleStyle, viewBox, titleVisible, scaleFactor);
        }

  

        private bool _reduceSubpixelDetails = false;

        #region Experimenting in pseudo-3D rendering

        private void drawPrismPolygon(Feature feature, Graphics g, BoundingRectangle viewBox, double scaleFactor)
        {
            using (Pen pen = new Pen(Color.FromArgb(255, Color.White)))
            {
                using (GraphicsPath path = new GraphicsPath(FillMode.Alternate))
                {
                    path.StartFigure();

                    float x = (float)((feature.Polygon.Contours[0].Vertices[0].X - viewBox.MinX) * scaleFactor);
                    float y = (float)((viewBox.MaxY - feature.Polygon.Contours[0].Vertices[0].Y) * scaleFactor);

                    g.RenderingOrigin = new Point((int)Math.Round(x), (int)Math.Round(y));

                    PointF[][] points = new PointF[feature.Polygon.Contours.Count][];
                    int contourIndex = 0;

                    foreach (Contour c in feature.Polygon.Contours)
                    {
                        points[contourIndex] = new PointF[c.Vertices.Count];

                        for (int j = 0; j < c.Vertices.Count; j++)
                        {
                            points[contourIndex][j].X = (float)((c.Vertices[j].X - viewBox.MinX) * scaleFactor);
                            points[contourIndex][j].Y = (float)((viewBox.MaxY - c.Vertices[j].Y) * scaleFactor);
                        }

                        if (points[contourIndex].Length > 2)
                            path.AddPolygon(points[contourIndex]);

                        contourIndex++;
                    }

                    path.CloseFigure();

                    int prismHeight = 4; //(int)(scaleFactor / 10000.0);

                    // boundary of the lower base of the prism
                    g.DrawPath(pen, path);

                    // Fill the lower base of the prism
                    using (Brush b = new SolidBrush(Color.FromArgb(50, 225, 225, 225)))
                    {
                        g.FillPath(b, path);
                    }

                    // border vertical faces
                    foreach (PointF[] contour in points)
                        for (int i = 0; i < contour.Length; i++)
                            g.DrawLine(pen, contour[i], new PointF(contour[i].X, contour[i].Y - prismHeight));

                    //Fill vertical faces
                    using (Brush b = new SolidBrush(Color.FromArgb(50, 60, 60, 60)))
                    {
                        List<Segment> segments = new List<Segment>();
                        List<bool> isForeSegment = new List<bool>();

                        foreach (PointF[] contour in points)
                            for (int i = 0; i < contour.Length; i++)
                            {
                                int j = i < contour.Length - 1 ? i + 1 : 0;
                                if (contour[i].X != contour[i].Y)
                                {
                                    segments.Add(new Segment(contour[i].X, contour[i].Y, contour[j].X, contour[j].Y));
                                    isForeSegment.Add(true);
                                }
                            }

                        for(int i = 0 ; i < segments.Count; i++)
                        {
                            double cX = segments[i].Center().X;
                            double cY = segments[i].Center().Y;

                            for (int j = 0; j < segments.Count; j++)
                            {
                                if (i != j)
                                { 
                                    double v1x = segments[j].V1.X;
                                    double v2x = segments[j].V2.X;
                                    double v1y = segments[j].V1.Y;
                                    double v2y = segments[j].V2.Y;

                                    if(v1x > v2x)
                                    {
                                        double temp = v1x;
                                        v1x = v2x;
                                        v2x = temp;
                                    }

                                    if (v1x < cX && v2x >= cX)
                                    {
                                        double crossY = v1y + (cX - v1x) / (v2x - v1x) * (v2y - v1y);
                                        if(crossY > cY)
                                            isForeSegment[i] = !isForeSegment[i];
                                    }
                                }
                            }
                        }

                        for (int i = 0; i < segments.Count; i++)
                        {
                            if (isForeSegment[i])
                                g.FillPolygon(b, new PointF[] 
                                {
                                    new PointF((float)segments[i].V1.X, (float)segments[i].V1.Y),
                                    new PointF((float)segments[i].V2.X, (float)segments[i].V2.Y),
                                    new PointF((float)segments[i].V2.X, (float)segments[i].V2.Y - prismHeight),
                                    new PointF((float)segments[i].V1.X, (float)segments[i].V1.Y - prismHeight)
                                });
                        }
                    }

                    // Fill the upper base of the prism
                    using (Brush b = new SolidBrush(Color.FromArgb(200, Color.FromArgb(245, 245, 245))))
                    {
                        path.Transform(new Matrix(1, 0, 0, 1, 0, -prismHeight));
                        g.FillPath(b, path);
                    }

                    // boundary of the upper base of the prism
                    g.DrawPath(pen, path);
                }
            }
        }

        #endregion

        void IFeatureRenderer.DrawPolygon(Feature feature, Graphics g, PolygonStyle style, TitleStyle titleStyle, BoundingRectangle viewBox, bool titleVisible, double scaleFactor)
        {
            DrawPolygon(feature, g, style, titleStyle, viewBox, titleVisible, scaleFactor);
        }

     

        void IFeatureRenderer.FlushSelectedFeatures(Graphics g, BoundingRectangle viewBox, double scaleFactor)
        {
            FlushSelectedFeatures(g, viewBox, scaleFactor);
        }

        

        void IFeatureRenderer.FlushTitles(Graphics g, BoundingRectangle viewBox, double scaleFactor)
        {
            FlushTitles(g, viewBox, scaleFactor);
        }

        #region Interface methods for normal overlap

        /// <summary>
        /// Draw a point.
        /// </summary>        
        protected virtual void DrawPoint(Feature feature, Graphics g, PointStyle style, TitleStyle titleStyle,
                                         BoundingRectangle viewBox, bool titleVisible, double scaleFactor)
        {
            if (!_isSelectionRendering && feature.Selected)
            {
                PointBufferElement element = new PointBufferElement(feature, style, titleStyle, titleVisible);
                _selectedPoints.Add(element);
                return;
            }

            string _fontName = string.IsNullOrEmpty(style.FontName) ? _symbolsDefaultFontName : style.FontName;
            using (Font f = new Font(_fontName, style.Size, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                using (SolidBrush fontBrush = new SolidBrush(style.Color))
                {
                    SizeF size;
                    SizeF offset;
                    if (style.DisplayKind == PointDisplayKind.Symbol)
                        // character size
                        size = g.MeasureString(style.Symbol.ToString(), f, new PointF(0, 0), _symbolStringFormat);
                    else
                    {
                        // image size
                        if (style.Image != null)
                            size = new SizeF(style.Image.Width, style.Image.Height);
                        else
                            size = new SizeF(1, 1);
                    }
                    //Offset relative to the center point
                    offset = new SizeF(size.Width/2,size.Height/2);

                    switch (style.ContentAlignment)
                    {
                        case ContentAlignment.TopLeft: offset = new SizeF(0, 0); break;
                        case ContentAlignment.TopCenter: offset = new SizeF(size.Width / 2,0); break;
                        case ContentAlignment.TopRight: offset = new SizeF(size.Width, 0); break;



                        case ContentAlignment.BottomLeft: offset = new SizeF(0, size.Height ); break;
                        case ContentAlignment.BottomCenter: offset = new SizeF(size.Width / 2, size.Height); break;
                        case ContentAlignment.BottomRight: offset = new SizeF(size.Width, size.Height); break;



                        case ContentAlignment.MiddleLeft: offset = new SizeF(0, size.Height / 2); break;
                        case ContentAlignment.MiddleCenter: offset = new SizeF(size.Width / 2, size.Height / 2); break;
                        case ContentAlignment.MiddleRight: offset = new SizeF(size.Width, size.Height / 2); break;
    
                        default:
                            throw new NotSupportedException();

                    }
                    IEnumerable<ICoordinate> targetPoints = null;
                    if (feature.FeatureType == FeatureType.Point)
                        targetPoints = new ICoordinate[] {feature.Point.Coordinate};
                    else
                        targetPoints = feature.MultiPoint.Points;

                    foreach (ICoordinate targetPoint in targetPoints)
                    {
                        if (style.DisplayKind == PointDisplayKind.Symbol)
                        {
                            // symbol
                            using (GraphicsPath path = new GraphicsPath())
                            {
                                path.AddString(style.Symbol.ToString(),
                                               f.FontFamily,
                                               (int) f.Style,
                                               f.Size,
                                               new PointF((float) ((targetPoint.X - viewBox.MinX)*scaleFactor-offset.Width),
                                                          (float)
                                                          ((viewBox.MaxY - targetPoint.Y)*scaleFactor -offset.Height)),
                                               _symbolStringFormat);

                                g.FillPath(fontBrush, path);
                            }
                        }
                        else
                        {
                            // image
                            if (style.Image != null)
                                g.DrawImageUnscaled(style.Image,
                                                    new Point(
                                                        (int)
                                                        Math.Round(((targetPoint.X - viewBox.MinX)*scaleFactor -
                                                                    offset.Width)),
                                                        (int)
                                                        Math.Round(((viewBox.MaxY - targetPoint.Y)*scaleFactor -
                                                                    offset.Height))));
                        }

                        if (feature.Selected)
                        {
                            // Frame selected object
                            using (Pen p = new Pen(_selectionColor, 2))
                                g.DrawRectangle(p,
                                                (float)((targetPoint.X - viewBox.MinX) * scaleFactor - offset.Width + 1),
                                                (float)((viewBox.MaxY - targetPoint.Y) * scaleFactor - offset.Height + 1),
                                                size.Width - 2, size.Height - 2);

                            using (Brush b = new SolidBrush(Color.FromArgb(50, _selectionColor)))
                                g.FillRectangle(b, (float)((targetPoint.X - viewBox.MinX) * scaleFactor - offset.Width),
                                                (float)((viewBox.MaxY - targetPoint.Y) * scaleFactor - offset.Height),
                                                size.Width, size.Height);
                        }
                    }

                    // inscription
                    if (!string.IsNullOrEmpty(feature.Title) && titleVisible)
                    {
                        if (feature.FeatureType == FeatureType.Point)
                        {
                            //Location signs point object can not be determined only by coordinates,
                            //without knowing the size of the image of a point object. 
                            //Therefore, the ordinate of a point object is displaced by half the size of the symbol.
                            Feature shp = new Feature(FeatureType.Point);
                            shp.Point = new PointD(feature.Point.X, feature.Point.Y + size.Height/2/scaleFactor);
                            shp.Title = feature.Title;
                            addTitleBufferElement(g, shp, titleStyle, viewBox, scaleFactor);
                        }
                        if (feature.FeatureType == FeatureType.MultiPoint)
                            addTitleBufferElement(g, feature, titleStyle, viewBox, scaleFactor);
                    }
                }
            }
        }

        /// <summary>
        /// Drawing the line.
        /// </summary>
        protected virtual void DrawPolyline(Feature feature, Graphics g, PolylineStyle style, TitleStyle titleStyle,
                                            BoundingRectangle viewBox, bool titleVisible, double scaleFactor)
        {
            if (!_isSelectionRendering && feature.Selected)
            {
                PolylineBufferElement element = new PolylineBufferElement(feature, style, titleStyle, titleVisible);
                _selectedPolylines.Add(element);
                return;
            }

            double pixelsize = 1/scaleFactor;

            if (_reduceSubpixelDetails)
            {
                if (feature.BoundingRectangle.Width < pixelsize && feature.BoundingRectangle.Height < pixelsize)
                    return;

                Polyline p1 = (Polyline) feature.Polyline.Clone();
                p1.Weed(pixelsize);
                Feature tempFeature = new Feature(FeatureType.Polyline);
                tempFeature.Title = feature.Title;
                tempFeature.Selected = feature.Selected;
                tempFeature.Polyline = p1;
                feature = tempFeature;

                if (feature.Polyline.Paths.Count == 0)
                    return;
            }

            using (Pen pen = style.GetPen())
            {
                if (Math.Min(viewBox.Width/(feature.BoundingRectangle.Width),
                             viewBox.Height/(feature.BoundingRectangle.Height)) < 2)
                    drawPolylineWithIntersectCalculation(g, feature, style, viewBox, scaleFactor);
                else
                    drawPolylineSimple(g, feature, style, viewBox, scaleFactor);

                // inscription
                if (!string.IsNullOrEmpty(feature.Title) && titleVisible)
                    addTitleBufferElement(g, feature, titleStyle, viewBox, scaleFactor);
            }
        }

        /// <summary>
        /// Draw a polygon.
        /// </summary>
        protected virtual void DrawPolygon(Feature feature, Graphics g, PolygonStyle style, TitleStyle titleStyle,
                                           BoundingRectangle viewBox, bool titleVisible, double scaleFactor)
        {
            if (feature.Polygon.Contours.Count == 0)
                return;

            //if (feature.Layer != null && feature.Layer.Title == "adp.TAB")
            //{
            //    drawPrismPolygon(feature, g, viewBox, scaleFactor);
            //    // inscription
            //    if (!string.IsNullOrEmpty(feature.Title) && titleVisible)
            //        addTitleBufferElement(g, feature, titleStyle, viewBox, scaleFactor);
            //    return;
            //}

            if (!_isSelectionRendering && feature.Selected)
            {
                PolygonBufferElement element = new PolygonBufferElement(feature, style, titleStyle, titleVisible);
                _selectedPolygons.Add(element);
                return;
            }

            double pixelsize = 1/scaleFactor;

            if (_reduceSubpixelDetails)
            {
                if (feature.BoundingRectangle.Width < pixelsize && feature.BoundingRectangle.Height < pixelsize)
                    return;

                Polygon tempPolygon = (Polygon) feature.Polygon.Clone();
                tempPolygon.Weed(pixelsize);
                Feature tempFeature = new Feature(FeatureType.Polygon);
                tempFeature.Title = feature.Title;
                tempFeature.Selected = feature.Selected;
                tempFeature.Polygon = tempPolygon;
                feature = tempFeature;

                if (feature.Polygon.Contours.Count == 0)
                    return;
            }

            using (Pen pen = style.GetPen())
            {
                using (GraphicsPath path = new GraphicsPath(FillMode.Alternate))
                {
                    path.StartFigure();

                    float x = (float) ((feature.Polygon.Contours[0].Vertices[0].X - viewBox.MinX)*scaleFactor);
                    float y = (float) ((viewBox.MaxY - feature.Polygon.Contours[0].Vertices[0].Y)*scaleFactor);

                    g.RenderingOrigin = new Point((int) Math.Round(x), (int) Math.Round(y));

                    foreach (Contour c in feature.Polygon.Contours)
                    {
                        // there is no point in trying to draw the contours of the degenerate
                        if (c.Vertices.Count <= 2)
                            continue;

                        PointF[] points = new PointF[c.Vertices.Count];

                        for (int j = 0; j < c.Vertices.Count; j++)
                        {
                            points[j].X = (float) ((c.Vertices[j].X - viewBox.MinX)*scaleFactor);
                            points[j].Y = (float) ((viewBox.MaxY - c.Vertices[j].Y)*scaleFactor);
                        }

                        if (points.Length > 2)
                            path.AddPolygon(points);
                    }

                    path.CloseFigure();

                    // Fill polygon
                    using (Brush b = style.GetBrush())
                    {
                        if (style.FillPatternInternal != null)
                        {
                            int w = style.FillPatternInternal.Width;
                            int h = style.FillPatternInternal.Height;
                            ((TextureBrush) b).TranslateTransform(g.RenderingOrigin.X%w, g.RenderingOrigin.Y%h);
                        }
                        g.FillPath(b, path);
                    }

                    if (feature.Selected)
                    {
                        //Fills the selected polygon

                        //Color color = Color.FromArgb(50, _selectionColor);
                        //using (Brush b = new HatchBrush(HatchStyle.Percent70, color, Color.Transparent))

                        if (_selectionColorChanged || _selectionTexture == null)
                            createSelectionTexture();

                        using (Brush b = new TextureBrush(_selectionTexture))
                        {
                            ((TextureBrush) b).TranslateTransform(g.RenderingOrigin.X%8, g.RenderingOrigin.Y%8);
                            g.FillPath(b, path);
                        }

                        // boundary of the selected polygons
                        using (Pen p = new Pen(_selectionColor, style.BorderWidth + 3))
                        {
                            p.LineJoin = LineJoin.Bevel;
                            g.DrawPath(p, path);
                        }
                    }

                    //boundary of the landfill
                    if (style.BorderVisible)
                        g.DrawPath(pen, path);
                }

                // inscription
                if (!string.IsNullOrEmpty(feature.Title) && titleVisible)
                    addTitleBufferElement(g, feature, titleStyle, viewBox, scaleFactor);
            }
        }

        /// <summary>
        /// Rendering selected objects.
        /// </summary>

        protected virtual void FlushSelectedFeatures(Graphics g, BoundingRectangle viewBox, double scaleFactor)
        {
            _isSelectionRendering = true;
            try
            {
                foreach (PolygonBufferElement element in _selectedPolygons)
                    (this as IFeatureRenderer).DrawPolygon(element.Polygon,
                                                           g,
                                                           element.Style,
                                                           element.TitleStyle,
                                                           viewBox,
                                                           element.TitleVisible,
                                                           scaleFactor);

                foreach (PolylineBufferElement element in _selectedPolylines)
                    (this as IFeatureRenderer).DrawPolyline(element.Polyline,
                                                            g,
                                                            element.Style,
                                                            element.TitleStyle,
                                                            viewBox,
                                                            element.TitleVisible,
                                                            scaleFactor);

                foreach (PointBufferElement element in _selectedPoints)
                    (this as IFeatureRenderer).DrawPoint(element.Polyline,
                                                         g,
                                                         element.Style,
                                                         element.TitleStyle,
                                                         viewBox,
                                                         element.TitleVisible,
                                                         scaleFactor);
            }
            finally
            {
                _isSelectionRendering = false;

                _selectedPolygons.Clear();
                _selectedPolylines.Clear();
                _selectedPoints.Clear();
            }
        }

        /// <summary>
        /// Drawing object names.
        /// </summary>        
        protected virtual void FlushTitles(Graphics g, BoundingRectangle viewBox, double scaleFactor)
        {
            // sort an array of labels according to the priority
            Comparison<TitleBufferElement> delegateInstance =
                delegate(TitleBufferElement arg1, TitleBufferElement arg2)
                    {
                        if (arg1 == null) return -1;
                        if (arg2 == null) return 1;
                        if (arg1 == arg2) return 0;

                        if (arg1.Style.RenderPriority == arg2.Style.RenderPriority)
                            return arg1.Number > arg2.Number ? 1 : -1;

                        return arg1.Style.RenderPriority > arg2.Style.RenderPriority ? 1 : -1;
                    };

            _titleBuffer.Sort(delegateInstance);

            for (int i = _titleBuffer.Count - 1; i >= 0; i--)
            {
                bool mustRender = true;

                // need to check whether or not the label overlaps with previously written inscriptions
                for (int j = _titleBuffer.Count - 1; j > i; j--)
                    if (_titleBuffer[j].HasRendered)
                    {
                        if (_titleBuffer[i].IntersectsWith(_titleBuffer[j]))
                        {
                            mustRender = false;
                            break;
                        }
                    }
                if (mustRender)
                {
                    drawTitle(g, _titleBuffer[i], scaleFactor, viewBox);
                    _titleBuffer[i].HasRendered = true;
                }
            }

            _titleBuffer.Clear();
            _titleCount = 0;
        }

        #endregion


        #endregion

        #region Helper methods rendering annotations

        private static void drawTitle(Graphics g, TitleBufferElement title, double scaleFactor, BoundingRectangle viewBox)
        {
            using (Font f = title.Style.GetFont())
                using (SolidBrush fontBrush = new SolidBrush(title.Style.Color))
                {
                    SizeF size = g.MeasureString(title.Title, f, new PointF(0, 0), _titleStringFormat);
                    ICoordinate originPoint = PlanimetryEnvironment.NewCoordinate((title.Box.V1.X + title.Box.V2.X) / 2, title.Box.V2.Y);

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        if (title.IsSimple)
                        {
                            path.AddString(title.Title,
                                           f.FontFamily,
                                           (int)f.Style,
                                           f.Size,
                                           new PointF((float)((originPoint.X - viewBox.MinX) * scaleFactor),
                                                      (float)((viewBox.MaxY - originPoint.Y) * scaleFactor)),
                                           _titleStringFormat);

                            if (title.Style.UseOutline)
                                using (Pen pen = new Pen(_titleOutlineColor, title.Style.OutlineSize))
                                {
                                    pen.MiterLimit = 1;
                                    g.DrawPath(pen, path);
                                }
                            g.FillPath(fontBrush, path);
                        }
                        else
                        {
                            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                            if (title.Style.UseOutline)
                                using (Pen pen = new Pen(_titleOutlineColor, title.Style.OutlineSize))
                                {
                                    pen.MiterLimit = 1;
                                    foreach (FollowingTitleElement element in title.FollowingTitle.Elements)
                                    {
                                        g.TranslateTransform(element.TranslationPoint.X, element.TranslationPoint.Y);
                                        g.RotateTransform(element.RotationAngle);

                                        path.Reset();
                                        path.AddString(element.Substring, f.FontFamily, (int)f.Style, f.Size, element.TitleOrigin, StringFormat.GenericTypographic);
                                        g.DrawPath(pen, path);
                                        g.ResetTransform();
                                    }
                                }

                            using (Brush b = new SolidBrush(title.Style.Color))
                                foreach (FollowingTitleElement element in title.FollowingTitle.Elements)
                                {
                                    g.TranslateTransform(element.TranslationPoint.X, element.TranslationPoint.Y);
                                    g.RotateTransform(element.RotationAngle);

                                    path.Reset();
                                    path.AddString(element.Substring, f.FontFamily, (int)f.Style, f.Size, element.TitleOrigin, StringFormat.GenericTypographic);
                                    g.FillPath(fontBrush, path);
                                    g.ResetTransform();
                                }
                        }
                    }
                }
        }

        private void addTitleBufferElement(Graphics g, Feature feature, TitleStyle titleStyle, BoundingRectangle viewBox, double scaleFactor)
        {
            ICoordinate targetCoordinate = PlanimetryEnvironment.NewCoordinate(0, 0);
            Segment s;

            SizeF size;
            using (Font f = titleStyle.GetFont())
                size = g.MeasureString(feature.Title, f, new PointF(0, 0), _titleStringFormat);

            switch (feature.FeatureType)
            {
                case FeatureType.Polyline:
                    if (!titleStyle.LeadAlong)
                    {
                        foreach (LinePath path in feature.Polyline.Paths)
                        {
                            if (path.Vertices.Count > 2)
                                targetCoordinate = path.Vertices[path.Vertices.Count / 2 - 1];
                            else
                            {
                                s = new Segment(path.Vertices[0].X, path.Vertices[0].Y,
                                                path.Vertices[1].X, path.Vertices[1].Y);

                                targetCoordinate = s.Center();
                            }
                            addTitleBufferElement(titleStyle, targetCoordinate, size, scaleFactor, feature);
                        }
                    }
                    else
                    {
                        int i = 0;
                        foreach (LinePath path in feature.Polyline.Paths)
                        {
                            FollowingTitle followingTitle = 
                                getFollowingTitle(g, path, feature.PolylinePartLengths[i], feature.Title, titleStyle, viewBox, scaleFactor);
                            if (followingTitle != null)
                                _titleBuffer.Add(new TitleBufferElement(followingTitle, titleStyle, _titleCount++));
                            i++;
                        }
                    }
                    return;
                case FeatureType.Polygon:
                    //if (feature.Polygon.Contours.Count > 0)
                    //    targetPoint = feature.Polygon.Contours[0].RibsCentroid();
                    //else
                    //    return;
                    //break;

                    if (feature.Polygon.Contours.Count > 0)
                        try
                        {
                            targetCoordinate = feature.Polygon.PointOnSurface();
                        }
                        catch(InvalidOperationException)
                        {
                            //interior point of the polygon for some reason (usually singular) can not be found
                            return;
                        }
                    else
                        return;
                    break;
                    
                case FeatureType.Point:
                    targetCoordinate = feature.Point.Coordinate;
                    //targetPoint.Y += size.Height / scaleFactor / 2;
                    break;
                case FeatureType.MultiPoint:
                    if (titleStyle.LeadAlong)
                    {
                        foreach (ICoordinate p in feature.MultiPoint.Points)
                        {
                            targetCoordinate = p;
                            targetCoordinate.Y += size.Height / scaleFactor / 2;
                            addTitleBufferElement(titleStyle, targetCoordinate, size, scaleFactor, feature);
                        }
                        return;
                    }
                    else
                        targetCoordinate = PlanimetryAlgorithms.GetCentroid(feature.MultiPoint.Points);
                    break;
            }

            addTitleBufferElement(titleStyle, targetCoordinate, size, scaleFactor, feature);
        }

        private void addTitleBufferElement(TitleStyle titleStyle, ICoordinate targetPoint, SizeF size, double scaleFactor, Feature feature)
        {
            _titleBuffer.Add(new TitleBufferElement(titleStyle,
                                        new Segment(targetPoint.X - size.Width / scaleFactor / 2,
                                                    targetPoint.Y - size.Height / scaleFactor / 2,
                                                    targetPoint.X + size.Width / scaleFactor / 2,
                                                    targetPoint.Y + size.Height / scaleFactor / 2),
                                        feature.Title,
                                        _titleCount++));
        }

        #region Helper methods rendering annotations along the lines of

        private FollowingTitle getFollowingTitle(Graphics g, LinePath part, double length, string label, TitleStyle titleStyle, BoundingRectangle viewBox, double scaleFactor)
        {
            StringFormat format = StringFormat.GenericTypographic;
            SizeF sizeF;
            PointF zeroPoint = new PointF(0, 0);

            using (Font f = titleStyle.GetFont())
            {
                sizeF = g.MeasureString(label, f, zeroPoint, format);

                // label length must be less than the length of the line
                if (sizeF.Width / scaleFactor < length)
                {
                    LinePath tempPart = new LinePath();
                    foreach (ICoordinate p in part.Vertices)
                        tempPart.Vertices.Add(p);

                    int vertexNumber = 0;
                    ICoordinate centerPoint = getDistantPoint(tempPart.Vertices, length / 2, out vertexNumber);

                    // if the point of the proposed mid-label misses the display area of ​​the map, the inscription does not appear
                    if (!viewBox.ContainsPoint(centerPoint))
                        return null;

                    // simplify the line
                    tempPart.Weed(sizeF.Height / scaleFactor / 2);

                    //get the center point of the simplified line
                    centerPoint = getDistantPoint(tempPart.Vertices, length / 2, out vertexNumber);

                    List<double> leftPointsRotationDeltas = new List<double>();
                    List<double> rightPointsRotationDeltas = new List<double>();

                    // coordinates of points on the left of the middle of the inscription
                    IList<ICoordinate> leftPoints =
                        getLeftPoints(tempPart.Vertices,
                                      centerPoint,
                                      sizeF.Width / 2 / scaleFactor,
                                      vertexNumber,
                                      sizeF.Height / 2 / scaleFactor,
                                      leftPointsRotationDeltas);

                    // coordinates of the points to the right of the middle of the inscription
                    IList<ICoordinate> rightPoints =
                        getRightPoints(tempPart.Vertices,
                                       centerPoint,
                                       sizeF.Width / 2 / scaleFactor,
                                       vertexNumber,
                                       sizeF.Height / 2 / scaleFactor,
                                       rightPointsRotationDeltas);

                    //coordinates of the vertices of the broken line, which will be located along the inscription
                    List<ICoordinate> points = leftPoints.ToList();
                    points.AddRange(rightPoints);

                    // shifts of the inscriptions associated with break lines
                    List<double> rotationDeltas = leftPointsRotationDeltas;
                    rotationDeltas.AddRange(rightPointsRotationDeltas);

                    for (int i = 0; i < points.Count; i++)
                        points[i] = PlanimetryEnvironment.NewCoordinate((points[i].X - viewBox.MinX) * scaleFactor,
                                               (viewBox.MaxY - points[i].Y) * scaleFactor);

                    for (int i = 0; i < rotationDeltas.Count; i++)
                        rotationDeltas[i] = rotationDeltas[i] * scaleFactor;

                    //determine the direction of following labels (direct or reverse)
                    double forwardWeight = 0;
                    double backwardWeight = 0;

                    for (int i = 1; i < points.Count; i++)
                    {
                        Segment s = new Segment(PlanimetryEnvironment.NewCoordinate(points[i].X, points[i].Y),
                                                PlanimetryEnvironment.NewCoordinate(points[i - 1].X, points[i - 1].Y));
                        int quadNumber = pointQuadrantNumber(PlanimetryEnvironment.NewCoordinate(s.V1.X - s.V2.X, s.V1.Y - s.V2.Y));
                        if (quadNumber == 1 || quadNumber == 4)
                            forwardWeight += s.Length();
                        else
                            backwardWeight += s.Length();
                    }

                    if (backwardWeight > forwardWeight)
                    {
                        points.Reverse();
                        rotationDeltas.Reverse();
                    }

                    // inscriptions along the route should not be a large number of points
                    if (label.Length > points.Count - 2)
                    {
                        List<int> subStringLengths = new List<int>();
                        List<double> deltas = new List<double>();

                        LinePath p1 = new LinePath(points.ToArray());
                        double l = p1.Length();

                        // partition of the inscription on the straight parts, the calculation of displacement
                        int startIndex = 0;
                        for (int i = 1; i < points.Count; i++)
                        {
                            double currentDistance = PlanimetryAlgorithms.Distance(points[i - 1], points[i]);

                            if (deltas.Count > 0)
                                if (deltas[deltas.Count - 1] < currentDistance)
                                    currentDistance -= deltas[deltas.Count - 1];

                            //subtract the offset associated with line breaks
                            currentDistance -= rotationDeltas[i - 1];
                            if (i < rotationDeltas.Count)
                                currentDistance -= rotationDeltas[i];

                            int currentLength = (int)(currentDistance / l * label.Length);

                            if (startIndex + currentLength > label.Length)
                                currentLength = label.Length - startIndex;

                            subStringLengths.Add(currentLength > 0 ? currentLength : 0);

                            string subString;
                            if (subStringLengths[i - 1] > 0)
                                subString = label.Substring(startIndex, subStringLengths[i - 1]);
                            else
                                subString = string.Empty;

                            float width1, width2, width3;
                            width1 = width2 = width3 = g.MeasureString(subString, f, zeroPoint, format).Width;

                            if (!string.IsNullOrEmpty(subString))
                            {
                                if (subStringLengths[i - 1] > 1)
                                {
                                    width2 = g.MeasureString(label.Substring(startIndex, subStringLengths[i - 1] - 1), f, zeroPoint, format).Width;
                                    if (Math.Abs(width2 - currentDistance) < Math.Abs(width1 - currentDistance))
                                    {
                                        subStringLengths[i - 1] = subStringLengths[i - 1] - 1;
                                        width1 = width2;
                                    }
                                }

                                if (label.Length > subStringLengths[i - 1] + startIndex)
                                {
                                    width3 = g.MeasureString(label.Substring(startIndex, subStringLengths[i - 1] + 1), f, zeroPoint, format).Width;
                                    if (Math.Abs(width3 - currentDistance) < Math.Abs(width1 - currentDistance) &&
                                        Math.Abs(width3 - currentDistance) < sizeF.Width / label.Length / 6)
                                    {
                                        subStringLengths[i - 1] = subStringLengths[i - 1] + 1;
                                        width1 = width3;
                                    }
                                }
                            }

                            deltas.Add(0.5 * (width1 - currentDistance));
                            if (currentLength > 0)
                                startIndex += currentLength;
                        }

                        int sum = 0;
                        int maxZeroLengths = 0;
                        int zeroLengthsCount = 0;
                        foreach (int k in subStringLengths)
                        {
                            if (k <= 0)
                            {
                                zeroLengthsCount++;
                                if (maxZeroLengths < zeroLengthsCount)
                                    maxZeroLengths = zeroLengthsCount;
                            }
                            else
                                zeroLengthsCount = 0;
                            sum += k;
                        }

                        if (maxZeroLengths > 1)
                            return null;

                        int lastIndex = subStringLengths.Count() - 1;
                        if (lastIndex >= 0)
                        {
                            subStringLengths[lastIndex] += label.Length - sum;
                            if (subStringLengths[lastIndex] < 0)
                            {
                                subStringLengths[lastIndex - 1] -= subStringLengths[lastIndex];
                                subStringLengths[lastIndex] = 0;
                            }
                        }

                        FollowingTitle followingTitle = new FollowingTitle();

                        startIndex = 0;
                        double? previousAngle = null;
                        for (int i = 0; i < subStringLengths.Count(); i++)
                        {
                            if (subStringLengths[i] <= 0)
                                continue;

                            if (startIndex + subStringLengths[i] > label.Length)
                                return null;

                            SizeF size;
                            size = g.MeasureString(label.Substring(startIndex, subStringLengths[i]), f, zeroPoint, format);

                            int x0 = (i == 0 ? 0 : (int)Math.Round(deltas[i - 1] + rotationDeltas[i - 1]));

                            PointF[] v = new PointF[4];
                            v[0].X = x0;
                            v[0].Y = -size.Height / 2;

                            v[1].X = size.Width + x0;
                            v[1].Y = -size.Height / 2;

                            v[2].X = size.Width + x0;
                            v[2].Y = size.Height / 2;

                            v[3].X = x0;
                            v[3].Y = size.Height / 2;

                            float angle = (float)(180 / Math.PI * getAngle(PlanimetryEnvironment.NewCoordinate(Math.Abs(points[i].X * 2), points[i].Y),
                                                                           points[i],
                                                                           points[i + 1],
                                                  false));

                            if (previousAngle != null)
                            {
                                double angleDelta = Math.Abs(angle - previousAngle.Value);
                                if (angleDelta > 45 && 360 - angleDelta > 45)
                                    return null;
                            }

                            previousAngle = angle;

                            g.TranslateTransform((float)points[i].X, (float)points[i].Y);
                            g.RotateTransform(-(angle % 360));

                            g.Transform.TransformPoints(v);
                            for (int j = 0; j < 4; j++)
                            {
                                v[j].X = (float)(v[j].X / scaleFactor + viewBox.MinX);
                                v[j].Y = (float)(viewBox.MaxY - v[j].Y / scaleFactor);
                            }

                            FollowingTitleElement element =
                                new FollowingTitleElement(new PointF((float)points[i].X, (float)points[i].Y),
                                                          -(angle % 360),
                                                          new PointF(x0, -size.Height / 2),
                                                          label.Substring(startIndex, subStringLengths[i]),
                                                          v[0], v[1], v[2], v[3]);

                            startIndex += subStringLengths[i];

                            followingTitle.AddElement(element);

                            g.ResetTransform();
                        }
                        return followingTitle.EnvelopePolygon == null ? null : followingTitle;
                    }
                }
            }
            return null;
        }

        private ICoordinate getDistantPoint(IList<ICoordinate> points, double targetDistance, out int vertexNumber)
        {
            ICoordinate result = PlanimetryEnvironment.NewCoordinate(0, 0);
            vertexNumber = -1;

            if (points.Count < 1)
                return result;

            double currentDistance = 0;
            ICoordinate previousPoint = points[0];
            vertexNumber = 0;

            foreach (ICoordinate point in points)
            {
                double segmentLength = PlanimetryAlgorithms.Distance(previousPoint, point);
                if (segmentLength + currentDistance > targetDistance)
                {
                    double remainderDistance = targetDistance - currentDistance;
                    double l = remainderDistance / (segmentLength - remainderDistance);
                    result.X = (previousPoint.X + l * point.X) / (1 + l);
                    result.Y = (previousPoint.Y + l * point.Y) / (1 + l);
                    return result;
                }
                currentDistance += segmentLength;
                previousPoint = point;
                vertexNumber++;
            }

            return result;
        }

        private IList<ICoordinate> getLeftPoints(IList<ICoordinate> points, ICoordinate firstPoint, double targetDistance, int vertexNumber, double halfFontHeight, List<double> rotationDeltas)
        {
            List<ICoordinate> result = new List<ICoordinate>();

            ICoordinate previousPoint = firstPoint;
            ICoordinate prePreviousPoint = firstPoint;
            double currentDistance = 0;
            for (int i = vertexNumber - 1; i >= 0; i--)
            {
                double segmentLength = PlanimetryAlgorithms.Distance(previousPoint, points[i]);
                if (segmentLength + currentDistance > targetDistance)
                {
                    double remainderDistance = targetDistance - currentDistance;
                    double l = remainderDistance / (segmentLength - remainderDistance);
                    ICoordinate lastPoint = PlanimetryEnvironment.NewCoordinate(
                                    (previousPoint.X + l * points[i].X) / (1 + l),
                                    (previousPoint.Y + l * points[i].Y) / (1 + l));
                    result.Add(lastPoint);
                    rotationDeltas.Add(0);
                    if (prePreviousPoint != previousPoint && previousPoint != lastPoint)
                    {
                        double angle = Math.PI - getAngle(prePreviousPoint, previousPoint, lastPoint, false);
                        double delta = halfFontHeight * Math.Tan(Math.Abs(angle)) * (angle < 0 ? _labelRotationDeltaMax : _labelRotationDeltaMin);
                        rotationDeltas[rotationDeltas.Count - 2] = delta;
                    }
                    break;
                }
                result.Add(points[i]);
                rotationDeltas.Add(0);

                if (prePreviousPoint != previousPoint && previousPoint != points[i])
                {
                    double angle = Math.PI - getAngle(prePreviousPoint, previousPoint, points[i], false);
                    double delta = halfFontHeight * Math.Tan(Math.Abs(angle)) * (angle < 0 ? _labelRotationDeltaMax : _labelRotationDeltaMin);
                    rotationDeltas[rotationDeltas.Count - 2] = delta;
                    currentDistance -= delta * 2;
                }

                currentDistance += segmentLength;
                prePreviousPoint = previousPoint;
                previousPoint = points[i];
            }
            result.Reverse();
            rotationDeltas.Reverse();
            return result;
        }

        private IList<ICoordinate> getRightPoints(IList<ICoordinate> points, ICoordinate firstPoint, double targetDistance, int vertexNumber, double halfFontHeight, List<double> rotationDeltas)
        {
            List<ICoordinate> result = new List<ICoordinate>();

            ICoordinate previousPoint = firstPoint;
            ICoordinate prePreviousPoint = firstPoint;
            double currentDistance = 0;
            for (int i = vertexNumber; i < points.Count; i++)
            {
                double segmentLength = PlanimetryAlgorithms.Distance(previousPoint, points[i]);
                if (segmentLength + currentDistance > targetDistance)
                {
                    double remainderDistance = targetDistance - currentDistance;
                    double l = remainderDistance / (segmentLength - remainderDistance);
                    ICoordinate lastPoint = PlanimetryEnvironment.NewCoordinate(
                                    (previousPoint.X + l * points[i].X) / (1 + l),
                                    (previousPoint.Y + l * points[i].Y) / (1 + l));
                    result.Add(lastPoint);
                    if (prePreviousPoint != previousPoint && previousPoint != lastPoint)
                    {
                        double angle = Math.PI - getAngle(prePreviousPoint, previousPoint, lastPoint, false);
                        double delta = halfFontHeight * Math.Tan(Math.Abs(angle)) * (angle > 0 ? _labelRotationDeltaMax : _labelRotationDeltaMin);
                        if (rotationDeltas.Count > 2)
                            rotationDeltas[rotationDeltas.Count - 2] = delta;
                    }
                    break;
                }
                result.Add(points[i]);
                rotationDeltas.Add(0);

                if (prePreviousPoint != previousPoint && previousPoint != points[i])
                {
                    double angle = Math.PI - getAngle(prePreviousPoint, previousPoint, points[i], false);
                    double delta = halfFontHeight * Math.Tan(Math.Abs(angle)) * (angle > 0 ? _labelRotationDeltaMax : _labelRotationDeltaMin);
                    if (rotationDeltas.Count > 2)
                        rotationDeltas[rotationDeltas.Count - 2] = delta;
                    currentDistance -= delta * 2;
                }
                currentDistance += segmentLength;
                prePreviousPoint = previousPoint;
                previousPoint = points[i];
            }
            return result;
        }

        private double getAngle(ICoordinate p1, ICoordinate p2, ICoordinate p3, bool counterClockwise)
        {
            p1 = (ICoordinate)p1.Clone();
            p2 = (ICoordinate)p2.Clone();
            p3 = (ICoordinate)p3.Clone();

            // translating the origin
            p1.X -= p2.X; p1.Y -= p2.Y;
            p3.X -= p2.X; p3.Y -= p2.Y;

            double alpha = p1.X != 0 ? Math.Atan(Math.Abs(p1.Y / p1.X)) : 0.5 * Math.PI;
            double betta = p3.X != 0 ? Math.Atan(Math.Abs(p3.Y / p3.X)) : 0.5 * Math.PI;

            alpha = translateAngleQuadrant(alpha, pointQuadrantNumber(p1));
            betta = translateAngleQuadrant(betta, pointQuadrantNumber(p3));

            if (counterClockwise)
                return alpha < betta ? (betta - alpha) : (2 * Math.PI - alpha + betta);
            else
                return alpha > betta ? (alpha - betta) : (2 * Math.PI - betta + alpha);
        }

        private static double translateAngleQuadrant(double angle, int quadrantNumber)
        {
            switch (quadrantNumber)
            {
                case 1: { return angle; }
                case 2: { return Math.PI - angle; }
                case 3: { return Math.PI + angle; }
                case 4: { return 2 * Math.PI - angle; }
            }

            return angle;
        }

        private static int pointQuadrantNumber(ICoordinate p)
        {
            if (p.X > 0)
            {
                if (p.Y > 0) return 1; else return 4;
            }
            else
            {
                if (p.Y > 0) return 2; else return 3;
            }
        }

        #endregion

        #endregion

        #region Helper methods drawing polylines

        private static void drawVisiblePolylinePart(Graphics g, List<ICoordinate> path, Pen pen, BoundingRectangle viewBox, double scaleFactor)
        {
            PointF[] points = new PointF[path.Count];

            for (int k = 0; k < path.Count; k++)
            {
                points[k].X = (float)((path[k].X - viewBox.MinX) * scaleFactor);
                points[k].Y = (float)((viewBox.MaxY - path[k].Y) * scaleFactor);
            }

            // polyline
            g.DrawLines(pen, points);
        }

        private static List<ICoordinate> getCrossPoints(ICoordinate vertex1, ICoordinate vertex2, BoundingRectangle viewBox)
        {
            List<ICoordinate> result = new List<ICoordinate>();
            Segment currentSegment = new Segment(vertex1.X,
                                                 vertex1.Y,
                                                 vertex2.X,
                                                 vertex2.Y);

            addCrossPointToList(new Segment(viewBox.MinX, viewBox.MinY, viewBox.MinX, viewBox.MaxY),
              currentSegment, result);

            addCrossPointToList(new Segment(viewBox.MinX, viewBox.MaxY, viewBox.MaxX, viewBox.MaxY),
                          currentSegment, result);

            addCrossPointToList(new Segment(viewBox.MaxX, viewBox.MaxY, viewBox.MaxX, viewBox.MinY),
                          currentSegment, result);

            addCrossPointToList(new Segment(viewBox.MaxX, viewBox.MinY, viewBox.MinX, viewBox.MinY),
                          currentSegment, result);

            return result;
        }

        private static void addCrossPointToList(Segment s1, Segment s2, List<ICoordinate> list)
        {
            Segment stub = new Segment();
            ICoordinate crossPoint = PlanimetryEnvironment.NewCoordinate(0, 0);

            Dimension crossKind =
                PlanimetryAlgorithms.SegmentsIntersection(s1, s2, out crossPoint, out stub);
            if (crossKind == Dimension.Zero)
                list.Add(crossPoint);
        }

        private static bool getCrossPoint(Segment s1, Segment s2, ref ICoordinate crossPoint)
        {
            Segment stub = new Segment();

            Dimension crossKind =
                PlanimetryAlgorithms.SegmentsIntersection(s1, s2, out crossPoint, out stub);
            if (crossKind == Dimension.Zero)
                return true;

            return false;
        }

        private static float getRelativeDashSize(Pen pen)
        {
            float relativeDashSize = 1;
            switch (pen.DashStyle)
            {
                case DashStyle.Dot:
                    relativeDashSize = 2;
                    break;

                case DashStyle.Dash:
                    relativeDashSize = 4;
                    break;

                case DashStyle.DashDot:
                    relativeDashSize = 6;
                    break;

                case DashStyle.DashDotDot:
                    relativeDashSize = 8;
                    break;

                case DashStyle.Custom:
                    relativeDashSize = 0;
                    foreach (float d in pen.DashPattern)
                        relativeDashSize += d;
                    break;
            }

            return relativeDashSize;
        }

        private static void drawVisiblePolylinePartWithStyleDetection(Graphics g, List<ICoordinate> path, PolylineStyle style, Pen defaultPen, Pen annexPen, bool selected, BoundingRectangle viewBox, double scaleFactor, double lengthFromBegining)
        {
            // Necessary to calculate the displacement pattern of the pen before drawing a polyline, or generated images will not be used for cross-linking.
            // Pattern in the zero offset polyline vertex set equal to zero.
            // Knowing the length of the zero-vertex to the portion of the polyline, you can calculate the shift pattern at the beginning of the portion.

            float relativeDashSize = getRelativeDashSize(defaultPen);
            float dashSize = style.Width * relativeDashSize;
            float dashOffset = (float)((lengthFromBegining * scaleFactor) % dashSize) / style.Width;
            defaultPen.DashOffset = dashOffset;

            if (style.UseAnnexLine)
            {
                relativeDashSize = getRelativeDashSize(annexPen);
                dashSize = style.Width * relativeDashSize;
                dashOffset = (float)((lengthFromBegining * scaleFactor) % dashSize) / style.Width;
                annexPen.DashOffset = dashOffset;
            }

            // main polyline
            drawVisiblePolylinePart(g, path, defaultPen, viewBox, scaleFactor);
            if (style.UseAnnexLine)
            {
                // More polyline
                drawVisiblePolylinePart(g, path, annexPen, viewBox, scaleFactor);
            }
        }

        private static double getPathLength(List<ICoordinate> path)
        {
            LinePath tempPart = new LinePath();
            tempPart.Vertices = path;
            return tempPart.Length();
        }

        private void drawPolylineWithIntersectCalculation(Graphics g, Feature feature, PolylineStyle style, BoundingRectangle viewBox, double scaleFactor)
        {
            //selection and rims do not contain dashes, so it is better to paint the entire
            if (style.UseOutline || feature.Selected)
            {
                foreach (LinePath path in feature.Polyline.Paths)
                {
                    PointF[] points = new PointF[path.Vertices.Count];

                    for (int j = 0; j < path.Vertices.Count; j++)
                    {
                        points[j].X = (float)((path.Vertices[j].X - viewBox.MinX) * scaleFactor);
                        points[j].Y = (float)((viewBox.MaxY - path.Vertices[j].Y) * scaleFactor);
                    }

                    if (style.UseOutline || feature.Selected)
                        drawLinePathSelectionAndOutline(g, feature.Selected, points, style);
                }
            }

            List<ICoordinate> currentPath = new List<ICoordinate>();

            using (Pen pen = style.GetPen())
            {
                using(Pen annexPen = style.GetAnnexPen())
                foreach (LinePath path in feature.Polyline.Paths)
                {
                    if (path.Vertices.Count < 2)
                        continue;

                    currentPath.Clear();
                    double currentLength = 0;
                    bool isInternalPath = viewBox.ContainsPoint(path.Vertices[0]);
                    ICoordinate previousPoint = path.Vertices[0];
                    IList<ICoordinate> vertices = path.Vertices;

                    for (int j = 0; j < path.Vertices.Count; j++)
                    {
                        if (isInternalPath) // the inside of the polyline
                        {
                            if (viewBox.ContainsPoint(vertices[j])) //stay inside
                            {
                                currentPath.Add(vertices[j]);
                                continue;
                            }
                            else // go outside
                            {
                                // add a point of intersection
                                List<ICoordinate> crossPoints = getCrossPoints(vertices[j], vertices[j - 1], viewBox);
                                currentPath.Add(crossPoints[0]);

                                //draw
                                drawVisiblePolylinePartWithStyleDetection(g, currentPath, style, pen, annexPen, feature.Selected, viewBox, scaleFactor, currentLength);

                                // are counting the length of a past
                                currentLength += getPathLength(currentPath);

                                // initialize the array outside of the points, polylines and continue execution
                                currentPath.Clear();
                                currentPath.Add(crossPoints[0]);
                                currentPath.Add(vertices[j]);
                                isInternalPath = false;
                                continue;
                            }
                        }
                        else //the outer part of the polyline
                        {
                            if (viewBox.ContainsPoint(vertices[j])) // go inside
                            {
                                //add a point of intersection
                                List<ICoordinate> crossPoints = getCrossPoints(vertices[j], vertices[j - 1], viewBox);
                                currentPath.Add(crossPoints[0]);

                                //are counting the length of a past
                                currentLength += getPathLength(currentPath);

                                // initialize the array of points inside the polyline and continue execution
                                currentPath.Clear();
                                currentPath.Add(crossPoints[0]);
                                currentPath.Add(vertices[j]);
                                isInternalPath = true;
                                continue;
                            }
                            else // cross twice, or remain outside
                            {
                                // look for the point of intersection
                                if (j > 0)
                                {
                                    List<ICoordinate> crossPoints = getCrossPoints(vertices[j], vertices[j - 1], viewBox);
                                    if (crossPoints.Count == 0) // remained outside
                                    {
                                        currentPath.Add(vertices[j]);
                                        continue;
                                    }
                                    if (crossPoints.Count == 2) // crossed 2 times
                                    {
                                        //determine which of the points of intersection must be added to the current path
                                        double d0 = PlanimetryAlgorithms.Distance(crossPoints[0], vertices[j - 1]);
                                        double d1 = PlanimetryAlgorithms.Distance(crossPoints[1], vertices[j - 1]);
                                        if (d0 < d1)
                                            currentPath.Add(crossPoints[0]);
                                        else
                                            currentPath.Add(crossPoints[1]);

                                        // are counting the length of a past
                                        currentLength += getPathLength(currentPath);

                                        currentPath.Clear();

                                        currentPath.Add(crossPoints[d0 < d1 ? 0 : 1]);
                                        currentPath.Add(crossPoints[d0 < d1 ? 1 : 0]);

                                        //draw a segment
                                        drawVisiblePolylinePartWithStyleDetection(g, currentPath, style, pen, annexPen, feature.Selected, viewBox, scaleFactor, currentLength);
                                        // consider the length
                                        currentLength += PlanimetryAlgorithms.Distance(crossPoints[0], crossPoints[1]);

                                        // initialize the external part of the polyline
                                        currentPath.Clear();
                                        if (d0 < d1)
                                            currentPath.Add(crossPoints[1]);
                                        else
                                            currentPath.Add(crossPoints[0]);
                                        currentPath.Add(vertices[j]);
                                        isInternalPath = false;
                                        continue;
                                    }
                                }
                                else // 1st point you just need to add to the list
                                {
                                    currentPath.Add(vertices[j]);
                                    continue;
                                }
                            }
                        }
                    }

                    //Draw a polyline part, if the internal
                    if (isInternalPath)
                        drawVisiblePolylinePartWithStyleDetection(g, currentPath, style, pen, annexPen, feature.Selected, viewBox, scaleFactor, currentLength);
                }
            }
        }

        private void drawLinePathSelectionAndOutline(Graphics g, bool selected, PointF[] pathPoints, PolylineStyle style)
        {
            if (selected)
            {
                // allocation
                float w = style.Width + (style.UseOutline ? style.OutlineWidth * 2 + 2 : 3);
                using (Pen p = new Pen(_selectionColor, w))
                {
                    p.MiterLimit = 3;
                    g.DrawLines(p, pathPoints);
                }
            }
            else
            {
                // Stroke
                if (style.UseOutline)
                    using (Pen p = style.GetOutlinePen())
                    {
                        p.MiterLimit = 3;
                        g.DrawLines(p, pathPoints);
                    }
            }
        }
        
        private void drawPolylineSimple(Graphics g, Feature feature, PolylineStyle style, BoundingRectangle viewBox, double scaleFactor)
        {
            foreach (LinePath path in feature.Polyline.Paths)
            {
                if (path.Vertices.Count < 2)
                    continue;

                PointF[] points = new PointF[path.Vertices.Count];

                for (int j = 0; j < path.Vertices.Count; j++)
                {
                    points[j].X = (float)((path.Vertices[j].X - viewBox.MinX) * scaleFactor);
                    points[j].Y = (float)((viewBox.MaxY - path.Vertices[j].Y) * scaleFactor);
                }

                //selection and Stroke
                if (style.UseOutline || feature.Selected)
                    drawLinePathSelectionAndOutline(g, feature.Selected, points, style);

                using (Pen pen = style.GetPen())
                {
                    pen.DashOffset = 0;

                    //main polyline
                    g.DrawLines(pen, points);
                    if (style.UseAnnexLine)
                        using (Pen annexPen = style.GetAnnexPen())
                        {
                            // additional line
                            g.DrawLines(annexPen, points);
                        }
                }
            }
        }

        #endregion
    }

    internal static class RenderingUtils
    {
        /// <summary>
        /// Blends two pixels.
        /// </summary>
        /// <param name="pixelData1">Source pixel data (in 32bppArgb format)</param>
        /// <param name="pixelData2">Pixel data to blend with (in 32bppArgb format)</param>
        public static Int32 BlendPixels(Int32 pixelData1, Int32 pixelData2)
        {
            byte alpha = (byte)(pixelData2 >> 24 & 0xFF);
            if (alpha == 0) return pixelData1;

            if (alpha == 255) return pixelData2;

            Int32 oldPixelData = pixelData1;
            byte oldAlpha = (byte)((oldPixelData >> 24 & 0xFF));

            byte newAlpha = (byte)(alpha + oldAlpha - alpha * oldAlpha / 255f);

            float a1 = alpha / 255f;
            float a0 = (1 - a1) * oldAlpha / 255f;
            a0 = a0 / (a1 + a0);
            a1 = 1 - a0;

            Int32 b1 = newAlpha << 24;
            Int32 b2 = (byte)((oldPixelData >> 16 & 0xFF) * a0 + (pixelData2 >> 16 & 0xFF) * a1) << 16;
            Int32 b3 = (byte)((oldPixelData >> 8 & 0xFF) * a0 + (pixelData2 >> 8 & 0xFF) * a1) << 8;
            Int32 b4 = (byte)((oldPixelData & 0xFF) * a0 + (pixelData2 & 0xFF) * a1);

            return b1 | b2 | b3 | b4;
        }
    }
}