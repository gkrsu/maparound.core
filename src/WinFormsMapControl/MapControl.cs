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
** File: MapControl.cs
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Windows Forms MapControl
**
=============================================================================*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MapAround.UI.WinForms
{
    using MapAround.Mapping;
    using MapAround.Geometry;
    using MapAround.CoordinateSystems.Transformations;

    /// <summary>
    /// Represents a Windows Forms Control that displays a map 
    /// and provides a way for manipulations with map and related 
    /// objects.
    /// </summary>
    public partial class MapControl : Control, ISupportInitialize
    {
        /// <summary>
        /// Enumerates a possible dragging modes.
        /// </summary>
        public enum DraggingMode : int
        { 
            /// <summary>
            /// Dragging is off.
            /// </summary>
            None = 0,
            /// <summary>
            /// Performs pannig a map while 
            /// user drags a mouse.
            /// </summary>
            Pan = 1,
            /// <summary>
            /// Performs drawing a zooming box while 
            /// user drags a mouse.
            /// </summary>
            Zoom = 2
        }

        int _oldWidth = 0;
        int _oldHeight = 0;

        bool _alignmentWhileZooming = true;
        bool _mouseWheelZooming = true;
        int _zoomPercent = 60;
        DraggingMode _draggingMode = DraggingMode.Pan;

        bool _animation = false;
        bool _animated = false;
        private int _animationTime = 400;

        private double _mainAnimationRelativeDuration = 0.8;
        private RectangleF _currentRectangle = new Rectangle(0, 0, 0, 0);
        private Bitmap _asyncMapImage = null;
        private double _opacity = 0;

        private System.Windows.Forms.Timer _wheelTimer = new System.Windows.Forms.Timer();
        private int _deltaPercent = 0;
        private Point _mouseLocation = new Point();

        private ColorMatrix _opacityColorMatrix  = new ColorMatrix(
                        new float[][] { 
                                   new float[] {1,  0,  0,  0, 0},
                                   new float[] {0,  1,  0,  0, 0},
                                   new float[] {0,  0,  1,  0, 0},
                                   new float[] {0,  0,  0,  1, 0},
                                   new float[] {0,  0,  0, 0,  1}});

        int _dragThreshold = 1;
        private int _selectionMargin = 3;

        private int _mouseDownX = 0;
        private int _mouseDownY = 0;
        private bool _isMapDragging = false;
        private bool _isFeatureNodeDragging = false;
        private bool _dragStartedFlag = false;
        private int _offsetX = 0;
        private int _offsetY = 0;

        private int _startAnimationOffsetX = 0;
        private int _startAnimationOffsetY = 0;

        private int[] _mouseX = new int[4]{-1, 0, 0, 0};
        private int[] _mouseY = new int[4]{-1, 0, 0, 0};

        private DateTime[] _mouseTime = new DateTime[4];

        private DateTime _lastMouseTime = DateTime.Now;

        private Color _selectionRectangleColor = Color.FromKnownColor(KnownColor.Highlight);

        private Bitmap _bitmap = null;
        private Map _map = null;
        private BoundingRectangle _viewBox = new BoundingRectangle();
        
        private void measureMouseMovementParameters(System.Windows.Forms.MouseEventArgs e)
        {
            if (_mouseX[0] == -1)
            {
                _mouseX[0] = e.X; _mouseX[1] = e.X;
                _mouseX[2] = e.X; _mouseX[3] = e.X;
                _mouseY[0] = e.Y; _mouseY[1] = e.Y;
                _mouseY[2] = e.Y; _mouseY[3] = e.Y;

                _mouseTime =
                    new DateTime[4] {DateTime.Now, 
                                     DateTime.Now,
                                     DateTime.Now,
                                     DateTime.Now};
            }

            DateTime dt = DateTime.Now;
            if ((dt - _mouseTime[3]).TotalMilliseconds > 30)
            {
                for (int i = 2; i >= 0; i--)
                {
                    _mouseTime[i + 1] = _mouseTime[i];
                    _mouseX[i + 1] = _mouseX[i];
                    _mouseY[i + 1] = _mouseY[i];
                }
                _mouseX[0] = e.X;
                _mouseY[0] = e.Y;
                _mouseTime[0] = dt;
            }
        }

        private void mouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (_isFeatureNodeDragging)
            {
                if (_editor.ActivePatchIndex != -1)
                {
                    updateActiveNodePosition(e.X, e.Y, false);
                    this.Refresh();
                }
                return;
            }

            if (_isMapDragging)
            {
                _offsetX = e.X - _mouseDownX;
                _offsetY = e.Y - _mouseDownY;

                measureMouseMovementParameters(e);

                if (_draggingMode != DraggingMode.None)
                {
                    if (_dragStartedFlag == false && (Math.Abs(_offsetX) > _dragThreshold || Math.Abs(_offsetY) > _dragThreshold))
                    {
                        _dragStartedFlag = true;
                        if (MapDragStarted != null)
                            MapDragStarted(this, new EventArgs());
                    }
                }
                this.Refresh();
            }
        }

        private void updateActiveNodePosition(int mouseX, int mouseY, bool commitIntoUndoList)
        {
            if(Editor.ActivePatchIndex == -1)
                return;

            ICoordinate mc = ClientToMap(new Point(mouseX, mouseY));

            _editor.UpdateActiveNodePosition(mc, commitIntoUndoList);
        }

        private ICoordinate getMouseSpeed(DateTime satrtTime, DateTime endtime, Point startPoint, Point endPoint)
        {
            double milliseconds = (endtime - satrtTime).TotalMilliseconds;

            if (milliseconds == 0)
                return PlanimetryEnvironment.NewCoordinate(0, 0);

            return PlanimetryEnvironment.NewCoordinate((startPoint.X - endPoint.X) / milliseconds,
                              (startPoint.Y - endPoint.Y) / milliseconds);
        }

        private ICoordinate getAvgMouseSpeed()
        {
            double vxAvg, vyAvg;

            ICoordinate v1 = getMouseSpeed(_mouseTime[3],
                                      _mouseTime[2],
                                      new Point(_mouseX[3], _mouseY[3]),
                                      new Point(_mouseX[2], _mouseY[2]));

            ICoordinate v2 = getMouseSpeed(_mouseTime[2],
                                      _mouseTime[1],
                                      new Point(_mouseX[2], _mouseY[2]),
                                      new Point(_mouseX[1], _mouseY[1]));

            ICoordinate v3 = getMouseSpeed(_mouseTime[1],
                                      _mouseTime[0],
                                      new Point(_mouseX[1], _mouseY[1]),
                                      new Point(_mouseX[0], _mouseY[0]));

            vxAvg = 0.35 *
               -(v1.X * (_mouseTime[2] - _mouseTime[3]).TotalMilliseconds +
                 v2.X * (_mouseTime[1] - _mouseTime[2]).TotalMilliseconds +
                 v3.X * (_mouseTime[0] - _mouseTime[1]).TotalMilliseconds) /
                 (_mouseTime[0] - _mouseTime[3]).TotalMilliseconds;

            vyAvg = 0.35 *
               -(v1.Y * (_mouseTime[2] - _mouseTime[3]).TotalMilliseconds +
                 v2.Y * (_mouseTime[1] - _mouseTime[2]).TotalMilliseconds +
                 v3.Y * (_mouseTime[0] - _mouseTime[1]).TotalMilliseconds) /
                 (_mouseTime[0] - _mouseTime[3]).TotalMilliseconds;

            return PlanimetryEnvironment.NewCoordinate(vxAvg, vyAvg);
        }

        private void mouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (_isFeatureNodeDragging)
            {
                _isFeatureNodeDragging = false;

                    if (_editor.ActivePatchIndex != -1)
                        updateActiveNodePosition(e.X, e.Y, true);

                return;
            }

            if (_isMapDragging)
                if (e.Button == MouseButtons.Left)
                {
                    _isMapDragging = false;
                    if (_dragStartedFlag)
                    {
                        if (_draggingMode == DraggingMode.Pan)
                        {
                            if (Animation)
                            {
                                if ((DateTime.Now - _mouseTime[0]).TotalMilliseconds > 200)
                                {
                                    Pan(_offsetX, _offsetY);
                                }
                                else
                                {
                                    
                                    _startAnimationOffsetX = _offsetX;
                                    _startAnimationOffsetY = _offsetY;

                                    ICoordinate avgSpeed = getAvgMouseSpeed();

                                    animatedPan(_offsetX + (int)(avgSpeed.X * AnimationTime * _mainAnimationRelativeDuration),
                                                _offsetY + (int)(avgSpeed.Y * AnimationTime * _mainAnimationRelativeDuration));
                                }
                            }
                            else
                                Pan(_offsetX, _offsetY);
                        }
                        if (_draggingMode == DraggingMode.Zoom)
                        {
                            Point upperLeft = new Point(Math.Min(_mouseDownX, _mouseDownX + _offsetX),
                                                        Math.Min(_mouseDownY, _mouseDownY + _offsetY));

                            ICoordinate p1 = ClientToMap(upperLeft);
                            ICoordinate p2 = ClientToMap(new Point(upperLeft.X + Math.Abs(_offsetX), 
                                                              upperLeft.Y + Math.Abs(_offsetY)));

                            if(SelectionRectangleDefined != null)
                            {
                                BoundingRectangle r = 
                                new BoundingRectangle(Math.Min(p1.X, p2.X), 
                                                             Math.Min(p1.Y, p2.Y),
                                                             Math.Max(p1.X, p2.X),
                                                             Math.Max(p1.Y, p2.Y));
                                SelectionRectangleDefined(this, new ViewBoxEventArgs(r));
                            }
                            this.Refresh();
                        }
                        _dragStartedFlag = false;
                        if (MapDragFinished != null)
                            MapDragFinished(this, new EventArgs());
                    }
                    else
                        this.Refresh();

                    _offsetX = 0;
                    _offsetY = 0;
                }
        }

        private void mouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (_map == null || _viewBox == null)
                return;

            if (_animated || _viewBox.IsEmpty())
                return;

            if (e.Clicks == 1 && _editor != null)
            {
                if (tryActivateFeatureNode(e.X, e.Y))
                {
                    _isFeatureNodeDragging = true;
                    this.Refresh();
                    return;
                }
            }

            if (e.Clicks == 2 && _editor != null)
            {
                if (tryActivateFeatureNode(e.X, e.Y))
                {
                    _editor.RemoveActiveNode();
                    this.Refresh();
                    return;
                }
            }

            if (_draggingMode != DraggingMode.None && e.Clicks == 1 && e.Button == MouseButtons.Left)
            {
                _mouseDownX = e.X;
                _mouseDownY = e.Y;
                _isMapDragging = true;
            }
        }

        private bool tryActivateFeatureNode(int mouseX, int mouseY)
        {
            if (_editor == null)
                return false;

            if (_editor.ResultingGeometry == null)
                return false;

            double nodeSize =
                PlanimetryAlgorithms.Distance(
                ClientToMap(new Point(mouseX, mouseY)),
                ClientToMap(new Point(mouseX - _editor.NodeSize, mouseY)));

            return _editor.TryActivateNodeAt(ClientToMap(new Point(mouseX, mouseY)), nodeSize);
        }

        private void mouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (_mouseWheelZooming)
            {
                if (!Animation)
                {
                    ChangeZoom(e.Delta / 120 * _zoomPercent, e.X, e.Y);
                }
                else
                {
                    _mouseLocation = e.Location;
                    if (!_wheelTimer.Enabled)
                        _wheelTimer.Start();

                    _deltaPercent += e.Delta / 120 * _zoomPercent;
                }
            }
        }

        private void wheelTimerTick(object sender, EventArgs e)
        {
            _wheelTimer.Stop();
            MouseWheel -= mouseWheel;
            try
            {
                ChangeZoom(_deltaPercent, _mouseLocation.X, _mouseLocation.Y);
            }
            finally
            {
                // events from the mouse wheel, obtained during an animation effect, shall be treated
                Application.DoEvents();
                MouseWheel += mouseWheel;
            }
            _deltaPercent = 0;
        }

        private void sizeChanged(object sender, EventArgs e)
        {
            if (_oldWidth == 0)
                _oldWidth = Width;
            if (_oldHeight == 0)
                _oldHeight = Height;

            int dx = Width - _oldWidth;
            int dy = Height - _oldHeight;

            if ((dx != 0 || dy != 0) && Width != 0 && Height != 0)
                if (!_viewBox.IsEmpty())
                {
                    BoundingRectangle viewBox = new BoundingRectangle(_viewBox.MinX,
                                                               _viewBox.MinY - dy * _viewBox.Height / _oldHeight,
                                                               _viewBox.MaxX + dx * _viewBox.Width / _oldWidth,
                                                               _viewBox.MaxY);
                    SetViewBox(viewBox);
                }

            _oldWidth = Width;
            _oldHeight = Height;
        }

        private void fitViewBoxSize(BoundingRectangle viewBox)
        {
            if (viewBox == null)
                return;

            if (viewBox.IsEmpty())
                return;

            double dx = 0, dy = 0;

            if (Width / Height > viewBox.Width / viewBox.Height)
                dy = -(viewBox.Height - (double)Height / Width * viewBox.Width);
            else
                dx = -(viewBox.Width - (double)Width / Height * viewBox.Height);

            viewBox.SetBounds(viewBox.MinX, viewBox.MinY,
                                             viewBox.MaxX + dx, viewBox.MaxY + dy);
        }

        private void drawDragging(PaintEventArgs pe)
        {
            if (_draggingMode == DraggingMode.Pan)
            {
                Rectangle rect = new Rectangle(pe.ClipRectangle.Left + _offsetX, pe.ClipRectangle.Top + _offsetY,
                                               pe.ClipRectangle.Right, pe.ClipRectangle.Bottom);

                pe.Graphics.DrawImageUnscaledAndClipped(_bitmap, rect);

                if (_editor != null)
                    _editor.DrawEditingGeometry(pe.Graphics, this.MapToClient, _offsetX, _offsetY);
            }
            if (_draggingMode == DraggingMode.Zoom)
            {
                pe.Graphics.DrawImageUnscaledAndClipped(_bitmap, pe.ClipRectangle);
                Point upperLeft = new Point(Math.Min(_mouseDownX, _mouseDownX + _offsetX),
                                            Math.Min(_mouseDownY, _mouseDownY + _offsetY));

                Rectangle r = new Rectangle(upperLeft,
                                            new Size(new Point(Math.Abs(_offsetX), Math.Abs(_offsetY))));

                using (Brush b = new SolidBrush(Color.FromArgb(30, _selectionRectangleColor)))
                    pe.Graphics.FillRectangle(b, r);
                using (Pen p = new Pen(Color.FromArgb(80, _selectionRectangleColor)))
                    pe.Graphics.DrawRectangle(p, r);
            }
        }

        //private Contour _editObject = 
        //    //null;
        //    new Contour(new ICoordinate[] { 
        //        PlanimetryEnvironment.NewCoordinate(10, 10),
        //        PlanimetryEnvironment.NewCoordinate(10, 100),
        //        PlanimetryEnvironment.NewCoordinate(100, 10)
        //    });

        private GeometryEditor _editor = null;

        /// <summary>
        /// Gets or sets an editor of geometry.
        /// Set this value if you want to enable geometry editing.
        /// </summary>
        public GeometryEditor Editor
        {
            get { return _editor; }
            set { _editor = value; }
        }

        private void drawGeneral(PaintEventArgs pe)
        {
            Rectangle r = new Rectangle(new Point(_startAnimationOffsetX, _startAnimationOffsetY),
                                        new Size(new Point(pe.ClipRectangle.Left + pe.ClipRectangle.Width,
                           pe.ClipRectangle.Top + pe.ClipRectangle.Height)));

            pe.Graphics.DrawImageUnscaledAndClipped(_bitmap, r);

            if(_editor != null)
                _editor.DrawEditingGeometry(pe.Graphics, this.MapToClient, 0, 0);
        }

        private void drawAnimated(PaintEventArgs pe)
        {
            if (_currentRectangle.Width <= 1e9 && _currentRectangle.Width >= -1e9 &&
                _currentRectangle.Height <= 1e9 && _currentRectangle.Height >= -1e9 &&
                _currentRectangle.Left <= 1e9 && _currentRectangle.Left >= -1e9 &&
                _currentRectangle.Top <= 1e9 && _currentRectangle.Top >= -1e9)
            {
                if (_opacity == 0)
                {
                    pe.Graphics.DrawImage(_bitmap, _currentRectangle);
                }
                else
                {
                    Rectangle r = new Rectangle(new Point(0, 0),
                                                new Size(new Point(pe.ClipRectangle.Left + pe.ClipRectangle.Width,
                                                                   pe.ClipRectangle.Top + pe.ClipRectangle.Height)));

                    pe.Graphics.DrawImageUnscaledAndClipped(_asyncMapImage, r);

                    if (_opacity > 0)
                        _opacityColorMatrix.Matrix33 = (float)_opacity;
                    else
                        _opacityColorMatrix.Matrix33 = 0;

                    using (ImageAttributes imageAttributes = new ImageAttributes())
                    {
                        imageAttributes.SetColorMatrix(_opacityColorMatrix,
                                                       ColorMatrixFlag.Default,
                                                       ColorAdjustType.Bitmap);

                        pe.Graphics.DrawImage(_bitmap,
                                              new Rectangle((int)_currentRectangle.Left, (int)_currentRectangle.Top,
                                                            (int)_currentRectangle.Width, (int)_currentRectangle.Height),
                                              0, 0, _bitmap.Width, _bitmap.Height,
                                              GraphicsUnit.Pixel, imageAttributes);
                    }
                }
            }
        }

        /// <summary>
        /// Draws a background of control.
        /// </summary>
        /// <param name="pe">Provides data for the System.Windows.Forms.Control.Paint event</param>
        protected override void OnPaintBackground(PaintEventArgs pe)
        {
            using(Brush b = new SolidBrush(BackColor))
                pe.Graphics.FillRectangle(b, pe.ClipRectangle);
        }

        /// <summary>
        /// Draws a control.
        /// </summary>
        /// <param name="pe">Provides data for the System.Windows.Forms.Control.Paint event</param>
        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            if (_bitmap != null)
            {
                if (_isMapDragging)
                    drawDragging(pe);
                else
                {
                    if (_animated)
                        drawAnimated(pe);
                    else
                        drawGeneral(pe);
                }
            }
        }

        private Rectangle mapViewBoxToClientRectangle(BoundingRectangle r)
        {
            Point p1 = MapToClient(r.Min);
            Point p2 = MapToClient(r.Max);

            return new Rectangle(new Point(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y)),
                                 new Size(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y)));
        }

        private Thread beginMapDrawing()
        {
            Thread result = new Thread(mapDrawWorker);
            result.Priority = ThreadPriority.BelowNormal;
            return result;
        }

        private void mapDrawWorker()
        {
            _asyncMapImage = _map.Render(Width, Height, _viewBox);
        }

        private void playFade()
        { 
            _animated = true;
            try
            {

                TimeSpan animationInterval = 
                    new TimeSpan(0, 0, 0, 0, (int)Math.Round(_animationTime * (1 - _mainAnimationRelativeDuration)));
                DateTime begin = DateTime.Now;
                DateTime end = begin.Add(animationInterval);
                DateTime now = DateTime.Now;

                while (now < end)
                {
                    double t = (double)(now - begin).Ticks / (double)animationInterval.Ticks;
                    if (t > 0)
                    {
                        _opacity = -t;
                        this.Refresh();
                    }
                    now = DateTime.Now;
                }
            }
            finally
            {
                _opacity = 0;
                _animated = false;
            }
        }

        private void play(Rectangle startRectangle, Rectangle endRectangle)
        {
            _animated = true;
            try
            {
                double xScaleFactor = (double)startRectangle.Width / (double)endRectangle.Width;
                double yScaleFactor = (double)startRectangle.Height / (double)endRectangle.Height;

                endRectangle = new Rectangle(
                    new Point((int)(-endRectangle.X * xScaleFactor),
                              (int)(-endRectangle.Y * yScaleFactor)),
                    new Size((int)(startRectangle.Width * xScaleFactor),
                             (int)(startRectangle.Height * yScaleFactor))
                                );

                TimeSpan animationInterval = new TimeSpan(0, 0, 0, 0, (int)Math.Round(_animationTime * _mainAnimationRelativeDuration));
                DateTime begin = DateTime.Now;
                DateTime end = begin.Add(animationInterval);

                float dTop = startRectangle.Top - endRectangle.Top;
                float dLeft = startRectangle.Left - endRectangle.Left;
                float dRight = startRectangle.Right - endRectangle.Right;
                float dBottom = startRectangle.Bottom - endRectangle.Bottom;
                DateTime now = DateTime.Now;

                while (now < end)
                {
                    double t = (double)(now - begin).Ticks / (double)animationInterval.Ticks;
                    if (t > 0)
                    {
                        double factor = Math.Pow(t, 0.25);

                        float newLeft = (int)Math.Round(startRectangle.Left - dLeft * factor);
                        float newTop = (int)Math.Round(startRectangle.Top - dTop * factor);
                        float newRight = (int)Math.Round(startRectangle.Right - dRight * factor);
                        float newBottom = (int)Math.Round(startRectangle.Bottom - dBottom * factor);

                        _currentRectangle = new RectangleF(newLeft, newTop, newRight - newLeft, newBottom - newTop);

                        this.Refresh();
                    }
                    now = DateTime.Now;
                }
            }
            finally
            {
                _animated = false;
            }

            return;
        }

        #region Events

        /// <summary>
        /// Raises when view box is changed.
        /// </summary>
        [Description("Occurs when view box is changed.")]
        public event EventHandler<ViewBoxEventArgs> ViewBoxChanged = null;

        /// <summary>
        /// Raises before map rendering.
        /// </summary>
        [Description("Occurs before map rendering.")]
        public event EventHandler<ViewBoxEventArgs> BeforeMapRender = null;

        /// <summary>
        /// Raises when user initialize map image dragging.
        /// </summary>
        [Description("Occurs when user initialize map image dragging.")]
        public event EventHandler<EventArgs> MapDragStarted = null;

        /// <summary>
        /// Raises when user has finished dragging map image.
        /// </summary>
        [Description("Occurs when user has finished dragging map image.")]
        public event EventHandler<EventArgs> MapDragFinished = null;

        /// <summary>
        /// Raises after user has finished \"drawing\" selection rectangle.
        /// </summary>
        [Description("Occurs after user has finished \"drawing\" selection rectangle.")]
        public event EventHandler<ViewBoxEventArgs> SelectionRectangleDefined = null;

        #endregion 

        #region Public methods

        /// <summary>
        /// Initializes a new instance of MapAround.UI.WinForms.MapControl.
        /// </summary>
        public MapControl()
        {
            InitializeComponent();
            _oldWidth = Width;
            _oldHeight = Height;
            SizeChanged += sizeChanged;
            MouseWheel += mouseWheel;
            MouseDown += mouseDown;
            MouseUp += mouseUp;
            MouseMove += mouseMove;
            DoubleBuffered = true;
            
            _wheelTimer.Interval = 80;
            _wheelTimer.Tick += wheelTimerTick;
        }

        /// <summary>
        /// Sets a new visible area of map.
        /// </summary>
        /// <param name="viewBox">Bounding rectangle defining an area of view</param>
        public void SetViewBox(BoundingRectangle viewBox)
        {
            SetViewBox(viewBox, false, false, false);
        }

        private void setViewBoxWithAnimation(Rectangle begin, Rectangle end)
        {
            // handlers to perform better in the mainstream
            if (BeforeMapRender != null)
                BeforeMapRender(this, new ViewBoxEventArgs(_viewBox));

            // block input events to the animation
            bool oldFocused = Focused;
            try
            {
                // thread to start generating a map image
                Thread thread = beginMapDrawing();
                thread.Priority = ThreadPriority.BelowNormal;
                thread.Start();
                // start the animation effect in the main stream
                play(begin, end);
                // waiting for the completion of the process of generating a map image
                thread.Join();

                // new image is generated, run the animation effect "manifestations" of the image
                if (begin.Width != end.Width ||
                   begin.Height != end.Height)
                    playFade();

                _startAnimationOffsetX = 0;
                _startAnimationOffsetY = 0;

                // replace the image
                if (_bitmap != null)
                    _bitmap.Dispose();

                _bitmap = _asyncMapImage;
                _asyncMapImage = null;

                // update appearance
                Refresh();
            }
            finally
            {
                if (oldFocused)
                    Focus();
            }
        }

        private bool whetherViewBoxChanged(BoundingRectangle newViewBox)
        {
            if (newViewBox.IsEmpty() ^ _viewBox.IsEmpty())
                return true;

            if (!newViewBox.IsEmpty() && !_viewBox.IsEmpty())
            {
                if (newViewBox.MinX != _viewBox.MinX || newViewBox.MaxX != _viewBox.MaxX ||
                    newViewBox.MinY != _viewBox.MinY || newViewBox.MaxY != _viewBox.MaxY)
                    return true;
            }

            return false;
        }

        private void calcAnimationRectangles(BoundingRectangle oldViewBox, BoundingRectangle viewBox, out Rectangle begin, out Rectangle end)
        {
            begin = new Rectangle();
            end = new Rectangle();

            if (!oldViewBox.IsEmpty())
            {
                begin = mapViewBoxToClientRectangle(oldViewBox);
                if (_startAnimationOffsetX != 0 || _startAnimationOffsetY != 0)
                    begin = new Rectangle(new Point(_startAnimationOffsetX, _startAnimationOffsetY), begin.Size);
            }

            if (!viewBox.IsEmpty() && !_viewBox.IsEmpty())
            {
                end = mapViewBoxToClientRectangle(viewBox);
            }
        }

        /// <summary>
        /// Sets a new visible area of map.
        /// </summary>
        /// <param name="viewBox">Bounding rectangle defining an area of view</param>
        /// <param name="forceRedraw">A value indicating whether a map image should be rendered 
        /// even if the viewing area is unchanged</param>
        /// <param name="forceSizeFitting">A value indicating whether a view box sizes should be corrected
        /// to fit map control sizes</param>
        /// <param name="playAnimation">A value indicating whether an animation effect is played</param>
        public void SetViewBox(BoundingRectangle viewBox, bool forceRedraw, bool forceSizeFitting, bool playAnimation)
        {
            if (viewBox == null)
                throw new ArgumentNullException("viewBox");

            if (viewBox.IsEmpty())
            {
                if (_bitmap != null)
                {
                    _bitmap.Dispose();
                    _bitmap = null;
                }
                this.Refresh();
            }

            bool viewBoxChanged = whetherViewBoxChanged(viewBox);

            if (forceSizeFitting)
                fitViewBoxSize(viewBox);
            
            BoundingRectangle oldViewBox = _viewBox;
            Rectangle begin, end;

            calcAnimationRectangles(oldViewBox, viewBox, out begin, out end);

            _viewBox = viewBox;

            if (_map != null && !viewBox.IsEmpty())
                if (viewBoxChanged || forceRedraw)
                {
                    if (_animation && playAnimation && !oldViewBox.IsEmpty())
                        // requires animations play
                        setViewBoxWithAnimation(begin, end);
                    else
                    {
                        if (_bitmap != null)
                        {
                            _bitmap.Dispose();
                            _bitmap = null;
                        }

                        RedrawMap();
                    }
                }

            if (viewBoxChanged && ViewBoxChanged != null)
                ViewBoxChanged(this, new ViewBoxEventArgs(_viewBox));
        }

        /// <summary>
        /// Redraws a map image.
        /// </summary>
        public void RedrawMap()
        {
            if (_viewBox != null)
            {
                if (!_viewBox.IsEmpty())
                {
                    if (BeforeMapRender != null)
                        BeforeMapRender(this, new ViewBoxEventArgs(_viewBox));

                    _bitmap = _map.Render(Width, Height, _viewBox);
                    this.Refresh();
                }
            }
        }

        /// <summary>
        /// Gets a viewing area of map.
        /// </summary>
        /// <returns>A bounding rectangle defining the viewing area</returns>
        public BoundingRectangle GetViewBox()
        {
            return (BoundingRectangle)_viewBox.Clone();
        }

        /// <summary>
        /// Changes a zoom.
        /// </summary>
        /// <param name="deltaPercent">A value (percents) by which a zoom change</param>
        /// <param name="mouseX">An X coordinate of the mouse</param>
        /// <param name="mouseY">A Y coordinate of the mouse</param>
        public void ChangeZoom(int deltaPercent, int mouseX, int mouseY)
        {
            if (_map == null || _viewBox == null)
                return;

            if (deltaPercent != 0)
            {
                if (_viewBox.IsEmpty())
                    return;

                double delta = (double)deltaPercent / 100;
                if (delta > 0) 
                    delta *= 2;

                if (!_alignmentWhileZooming)
                {
                    mouseX = Width / 2;
                    mouseY = Height / 2;
                }

                ICoordinate node = ClientToMap(new Point(mouseX, mouseY));

                double leftWidth = node.X - _viewBox.MinX;
                double rightWidth = _viewBox.MaxX - node.X;
                double bottomHeight = node.Y - _viewBox.MinY;
                double topHeight = _viewBox.MaxY - node.Y;

                double factor = delta > 0 ? 1 - delta / (2 + 2 * delta) : 1 - delta / 2;

                BoundingRectangle viewbox =
                    new BoundingRectangle(node.X - leftWidth * factor,
                                          node.Y - bottomHeight * factor,
                                          node.X + rightWidth * factor,
                                          node.Y + topHeight * factor);

                SetViewBox(viewbox, true, false, true);
            }
        }

        /// <summary>
        /// Zooms in.
        /// </summary>
        /// <param name="p">A mouse position</param>
        public void ZoomIn(Point p)
        {
            ChangeZoom(_zoomPercent, p.X, p.Y);
        }

        /// <summary>
        /// Zooms out.
        /// </summary>
        /// <param name="p">A mouse position</param>
        public void ZoomOut(Point p)
        {
            ChangeZoom(-_zoomPercent, p.X, p.Y);
        }

        /// <summary>
        /// Zooms in.
        /// </summary>
        public void ZoomIn()
        {
            ChangeZoom(_zoomPercent, Width / 2, Height / 2);
        }

        /// <summary>
        /// Zooms out.
        /// </summary>
        public void ZoomOut()
        {
            ChangeZoom(-_zoomPercent, Width / 2, Height / 2);
        }

        /// <summary>
        /// Pans a map.
        /// </summary>
        /// <param name="horizontalShift">Horizontal shift in pixels</param>
        /// <param name="verticalShift">Vertical shift in pixels</param>
        public void Pan(int horizontalShift, int verticalShift)
        {
            if (_map == null || _viewBox == null)
                return;

            if (!_viewBox.IsEmpty())
            {
                double xFactor = _viewBox.Width / Width;
                double yFactor = _viewBox.Height / Height;
                BoundingRectangle viewBox = new BoundingRectangle(_viewBox.MinX - horizontalShift * xFactor,
                                                           _viewBox.MinY + verticalShift * yFactor,
                                                           _viewBox.MaxX - horizontalShift * xFactor,
                                                           _viewBox.MaxY + verticalShift * yFactor);

                SetViewBox(viewBox);
            }
        }

        /// <summary>
        /// Pans a map.
        /// </summary>
        /// <param name="horizontalShift">Horizontal shift in pixels</param>
        /// <param name="verticalShift">Horizontal shift in pixels</param>
        /// <param name="forceRedraw">A value indicating whether a map image should be rendered 
        /// even if the viewing area is unchanged</param>
        /// <param name="playAnimation">A value indicating whether an animation effect is played</param>
        public void Pan(int horizontalShift, int verticalShift, bool forceRedraw, bool playAnimation)
        {
            if (_map == null || _viewBox == null)
                return;

            if (!_viewBox.IsEmpty())
            {
                double xFactor = _viewBox.Width / Width;
                double yFactor = _viewBox.Height / Height;
                BoundingRectangle viewBox = new BoundingRectangle(_viewBox.MinX - horizontalShift * xFactor,
                                                           _viewBox.MinY + verticalShift * yFactor,
                                                           _viewBox.MaxX - horizontalShift * xFactor,
                                                           _viewBox.MaxY + verticalShift * yFactor);

                SetViewBox(viewBox, forceRedraw, false, playAnimation);
            }
        }

        private void animatedPan(int horizontalShift, int verticalShift)
        {
            if (_map == null || _viewBox == null)
                return;

            if (!_viewBox.IsEmpty())
            {
                double xFactor = _viewBox.Width / Width;
                double yFactor = _viewBox.Height / Height;

                BoundingRectangle viewBox = new BoundingRectangle(_viewBox.MinX - horizontalShift * xFactor,
                                           _viewBox.MinY + verticalShift * yFactor,
                                           _viewBox.MaxX - horizontalShift * xFactor,
                                           _viewBox.MaxY + verticalShift * yFactor);

                SetViewBox(viewBox, true, false, true);
            }
        }

        private ICoordinate prepareForSelect(Point position)
        {
            BoundingRectangle mapCoordsViewBox =
                _map.MapViewBoxFromPresentationViewBox(_viewBox);

            double x = _viewBox.Width / Width * position.X + _viewBox.MinX;
            double y = _viewBox.MaxY - _viewBox.Height / Height * position.Y;

            // calculate sampling error of point and linear features
            _map.SelectionPointRadius =
                _selectionMargin * _viewBox.Width / Width;

            ICoordinate result = PlanimetryEnvironment.NewCoordinate(x, y);
            if (_map.OnTheFlyTransform != null)
            {
                ICoordinate delta = PlanimetryEnvironment.NewCoordinate(result.X + _map.SelectionPointRadius, result.Y);
                IMathTransform inverseTransform = _map.OnTheFlyTransform.Inverse();

                delta = PlanimetryEnvironment.NewCoordinate(inverseTransform.Transform(delta.Values()));

                _map.SelectionPointRadius =
                    PlanimetryAlgorithms.Distance(PlanimetryEnvironment.NewCoordinate(inverseTransform.Transform(result.Values())), delta);
            }

            return result;
        }

        /// <summary>
        /// Finds a topmost feature of the map at specified point.
        /// </summary>
        /// <param name="position">A point at which to find feature</param>
        /// <returns>A finded feature</returns>
        public Feature GetFeatureAtPosition(Point position)
        {
            if (_viewBox == null || _map == null)
                return null;
            if (_viewBox.IsEmpty())
                return null;

            Feature feature = null;

            ICoordinate point = prepareForSelect(position);

            _map.SelectTopObject(point, Width / _viewBox.Width, out feature);

            return feature;
        }

        /// <summary>
        /// Finds features of the map at specified point.
        /// </summary>
        /// <param name="position">A point at which to find feature</param>
        /// <returns>A list containing finded feature</returns>
        public IList<Feature> GetFeaturesAtPosition(Point position)
        {
            if (_viewBox == null || _map == null)
                return new List<Feature>();
            if (_viewBox.IsEmpty())
                return new List<Feature>();

            ICoordinate point = prepareForSelect(position);

            IList<Feature> result = null;
            _map.SelectObjects(point, Width / _viewBox.Width, out result);

            return result;
        }


        /// <summary>
        /// Transforms screen coordinates to map coordinates.
        /// </summary>
        /// <param name="point">A point on the screen</param>
        /// <returns>A point on the map</returns>
        public ICoordinate ClientToMap(Point point)
        {
            if (_viewBox == null)
                throw new InvalidOperationException("Undefined view box");

            return PlanimetryEnvironment.NewCoordinate(_viewBox.Width / Width * point.X + _viewBox.MinX, _viewBox.MaxY - _viewBox.Height / Height * point.Y);
        }

        /// <summary>
        /// Transforms map coordinates to screen coordinates.
        /// </summary>
        /// <param name="point">A point on the map</param>
        /// <returns>A point on the screen</returns>
        public Point MapToClient(ICoordinate point)
        {
            double scaleFactor = Math.Min(Width / _viewBox.Width, Height / _viewBox.Height);

            int resultX = (int)Math.Round((point.X - _viewBox.MinX) * scaleFactor);
            int resultY = (int)Math.Round((_viewBox.MaxY - point.Y) * scaleFactor);

            return new Point(resultX, resultY);
        }

        #endregion 

        #region Public properties

        /// <summary>
        /// A map which displays by this control.
        /// </summary>
        [Browsable(false)]
        public Map Map
        {
            get { return _map; }
            set 
            { 
                _map = value;
                RedrawMap();
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates wether an animation effect 
        /// will be played when the scale has changed.
        /// <remarks>
        /// When the animation is enabled the rendering of map image (and the querying spatial data) 
        /// is executed in a separate thread. Should take this into account when using spatial 
        /// data providers or handling a map events.
        /// </remarks>
        /// </summary>
        [Category("Behavior")]
        [Description("Gets or sets a value that indicates wether animation effect will be played when the scale has changed.")]
        public bool Animation
        {
            get { return _animation; }
            set { _animation = value; }
        }

        /// <summary>
        /// Gets or sets a tolerance (in pixels) of feature selection.
        /// </summary>
        [Category("Behavior")]
        [Description("Gets or sets a tolerance (in pixels) of feature selection.")]
        public int SelectionMargin
        {
            get { return _selectionMargin; }
            set { _selectionMargin = value; }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether map should be aligned to mouse pointer.
        /// </summary>
        [Category("Behavior")]
        [Description("Value that indicates whether map should be aligned to mouse pointer.")]
        public bool AlignmentWhileZooming
        {
            get { return _alignmentWhileZooming; }
            set { _alignmentWhileZooming = value; }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether mouse wheel zooming is enabled.
        /// </summary>
        [Category("Behavior")]
        [Description("Value that indicates whether mouse wheel zooming is enabled.")]
        public bool MouseWheelZooming
        {
            get { return _mouseWheelZooming; }
            set { _mouseWheelZooming = value; }
        }

        /// <summary>
        /// Gets or sets a value (percent) of zoom change. This value is used by ZoomIn and ZoomOut methods.
        /// </summary>
        [Category("Behavior")]
        [Description("Value (percent) of zoom change. This value is used by ZoomIn and ZoomOut methods.")]
        public int ZoomPercent
        {
            get { return _zoomPercent; }
            set { _zoomPercent = value; }
        }

        /// <summary>
        /// Gets or sets a mode of mouse dragging.
        /// </summary>
        [Category("Behavior")]
        [Description("Mode of mouse dragging.")]
        public DraggingMode DragMode
        {
            get { return _draggingMode; }
            set { _draggingMode = value; }
        }

        /// <summary>
        /// Gets or sets a minimum number of pixels on which user must shift the map to start new image rendering.
        /// </summary>
        [Category("Behavior")]
        [Description("Minimum number of pixels on which user must shift the map to start new image rendering.")]
        public int DragThreshold
        {
            get { return _dragThreshold; }
            set { _dragThreshold = value; }
        }

        /// <summary>
        /// Gets or sets a color of selection rectangle.
        /// </summary>
        [Category("Appearance")]
        [Description("Color of selection rectangle.")]
        public Color SelectionRectangleColor
        {
            get { return _selectionRectangleColor; }
            set { _selectionRectangleColor = value; }
        }

        /// <summary>
        /// Gets or sets a Value (in milliseconds) of the animation effect duration.
        /// </summary>
        [Category("Behavior")]
        [Description("Value (in milliseconds) of the animation effect duration.")]
        public int AnimationTime
        {
            get { return _animationTime; }
            set { _animationTime = value; }
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether 
        /// a mouse dragging is currently performing.
        /// </summary>
        [Browsable(false)]
        public bool IsDragging
        {
            get { return _dragStartedFlag; }
            set { _dragStartedFlag = value; }
        }


        #endregion

        #region ISupportInitialize Members

        /// <summary>
        /// Signals the beginning of a block of initialization.
        /// </summary>
        public void BeginInit()
        {
        }

        /// <summary>
        /// Signals the end of the initialization block.
        /// </summary>
        public void EndInit()
        {
        }

        #endregion
    }

    /// <summary>
    /// The MapAround.UI.WinForms namespace contains classes and interfaces 
    /// for developing Windows Forms user interface containing a map widgets.
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Provides data for the ViewBoxChanged, BeforeMapRender, SelectionRectangleDefined events.
    /// </summary>
    public class ViewBoxEventArgs : EventArgs
    {
        private BoundingRectangle _viewBox;

        /// <summary>
        /// Gets a bounding rectangle defining a viewing area of map.
        /// </summary>
        public BoundingRectangle ViewBox
        {
            get { return _viewBox; }
        }

        /// <summary>
        /// initializes a new instance of MapAround.UI.WinForms.ViewBoxEventArgs. 
        /// </summary>
        /// <param name="viewBox">A bounding rectangle defining a viewing area of map</param>
        public ViewBoxEventArgs(BoundingRectangle viewBox)
        {
            _viewBox = viewBox;
        }
    }
}
