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



﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Windows.Threading;

namespace WpfMapControl
{
    using MapAround.Mapping;
    using MapAround.Geometry;
    using MapAround.CoordinateSystems.Transformations;

    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class MapControl : UserControl
    {
        /// <summary>
        /// Enumerates a possible dragging modes.
        /// </summary>
        public enum DraggingMode
        {
            /// <summary>
            /// Dragging is off.
            /// </summary>
            None = 0,
            /// <summary>
            /// Performs panning a map while user drags a mouse.
            /// </summary>
            Pan = 1,
            /// <summary>
            /// Performs drawing a zooming box while user drags a mouse.
            /// </summary>
            Zoom = 2
        }

        #region Variables

        DraggingMode _draggingMode = DraggingMode.Pan;      // Current DragMode

        private BitmapSource _bitmap;
        private BitmapSource _asyncMapImage;
        private System.Drawing.Bitmap _asyncBitmapMapImage;

        private DateTime[] _mouseTime = new DateTime[4];
        private Map _map;
        private Point _mouseLocation;
        private Color _selectionRectangleColor = Color.FromRgb(255, 00, 00);
        private FormatConvertedBitmap _opocityColorMatrix = new FormatConvertedBitmap();
        private DispatcherTimer _wheelTimer = new DispatcherTimer();
        private BoundingRectangle _viewBox = new BoundingRectangle();
        private Rect _currentRectangle;

        private double _startAnimationOffsetX;
        private double _startAnimationOffsetY;
        private double _animationTime = 400;
        private double _mainAnimationRelativeDuration = 0.8;
        private double _opacity;

        private double _oldHeight;                          // Last save height
        private double _oldWidth;                           // Last save width
        // Offset from the point of a drag
        private double _offsetX;                            
        private double _offsetY;                            

        private double _mouseDownX;
        private double _mouseDownY;

        private double[] _mouseX = new double[4] { -1, 0, 0, 0 };
        private double[] _mouseY = new double[4] { -1, 0, 0, 0 };

        private bool _animation;                            // On / Off animation effect
        private bool _animated;                             // On / Off Animation
        private bool _isMapDragging;                        // Moving / Not moving map
        private bool _dragStartedFlag;                      // Flag of the beginning of drag
        private bool _mouseWheelZooming = true;
        private bool _alignmentWhileZooming = true;

        private int _dragThreshold = 1;                     // Rerendinga threshold in pixels
        private int _zoomPercent = 60;
        private int _deltaPercent;
        private int _selectionMargin = 3;

        #endregion

        #region Override Events

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isMapDragging)
            {
                Point p = e.GetPosition(sender as IInputElement);

                _offsetX = p.X - _mouseDownX;
                _offsetY = p.Y - _mouseDownY;

                MeasureMouseMovementParameters(p);

                if (_draggingMode != DraggingMode.None)
                {
                    if (_dragStartedFlag == false && (Math.Abs(_offsetX) > _dragThreshold || Math.Abs(_offsetY) > _dragThreshold))
                    {
                        _dragStartedFlag = true;

                        if (MapDragStarted != null)
                        {
                            MapDragStarted(this, new EventArgs());
                        }
                    }
                }
                this.InvalidateVisual();
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isMapDragging)
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

                                ICoordinate avgSpeed = GetAvgMouseSpeed();

                                AnimatedPan(
                                    (_offsetX + (avgSpeed.X * AnimationTime * _mainAnimationRelativeDuration)),
                                    (_offsetY + (avgSpeed.Y * AnimationTime * _mainAnimationRelativeDuration)));
                            }
                        }
                        else
                        {
                            Pan(_offsetX, _offsetY);
                        }
                    }
                    if (_draggingMode == DraggingMode.Zoom)
                    {
                        if (SelectionRectangleDefined != null)
                        {
                            Point upperLeft = new Point(Math.Min(_mouseDownX, _mouseDownX + _offsetX),
                                                        Math.Min(_mouseDownY, _mouseDownY + _offsetY));

                            ICoordinate p1 = ClientToMap(upperLeft);
                            ICoordinate p2 = ClientToMap(new Point(upperLeft.X + Math.Abs(_offsetX),
                                                                   upperLeft.Y + Math.Abs(_offsetY)));

                            BoundingRectangle r = new BoundingRectangle(Math.Min(p1.X, p2.X),
                                                                        Math.Min(p1.Y, p2.Y),
                                                                        Math.Max(p1.X, p2.X),
                                                                        Math.Max(p1.Y, p2.Y));
                            SelectionRectangleDefined(this, new ViewBoxEventArgs(r));
                        }
                        this.InvalidateVisual();
                    }

                    _dragStartedFlag = false;

                    if (MapDragFinished != null)
                    {
                        MapDragFinished(this, new EventArgs());
                    }
                }
                else
                {
                    this.InvalidateVisual();
                }

                _offsetX = 0;
                _offsetY = 0;

            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_map == null || _viewBox == null)
                return;

            if (_animated || _viewBox.IsEmpty())
                return;

            if (_draggingMode != DraggingMode.None)
            {
                Point p = e.GetPosition(sender as IInputElement);
                _mouseDownX = p.X;
                _mouseDownY = p.Y;
                _isMapDragging = true;
            }
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_mouseWheelZooming)
            {
                var thisElement = sender as IInputElement;

                if (!Animation)
                {
                    Point p = e.GetPosition(thisElement);
                    ChangeZoom(e.Delta / 120 * _zoomPercent, p.X, p.Y);
                }
                else
                {
                    _mouseLocation.X = e.GetPosition(thisElement).X;
                    _mouseLocation.Y = e.GetPosition(thisElement).Y;

                    if (!_wheelTimer.IsEnabled)
                        _wheelTimer.Start();

                    _deltaPercent += e.Delta / 120 * _zoomPercent;
                }
            }
        }

        private void OnSizeChanged(object sender, EventArgs e)
        {
            double dx = (ActualWidth - _oldWidth);
            double dy = (ActualHeight - _oldHeight);

            if ((dx != 0 || dy != 0) && ActualWidth != 0 && ActualHeight != 0)
            {
                if (!_viewBox.IsEmpty())
                {
                    BoundingRectangle viewBox = new BoundingRectangle(
                        _viewBox.MinX,
                        _viewBox.MinY - dy * _viewBox.Height / _oldHeight,
                        _viewBox.MaxX + dx * _viewBox.Width / _oldWidth,
                        _viewBox.MaxY);
                    SetViewBox(viewBox);
                }
            }

            _oldWidth = ActualWidth;
            _oldHeight = ActualHeight;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (_bitmap != null)
            {
                if (_isMapDragging)
                    DrawDragging(drawingContext);
                else
                {
                    if (_animated)
                    {
                        DrawAnimated(drawingContext);
                    }
                    else
                        DrawGeneral(drawingContext);
                }
            }
        }

        private void WheelTimerTick(object sender, EventArgs e)
        {
            _wheelTimer.Stop();

            MouseWheel -= OnMouseWheel;

            try
            {
                ChangeZoom(_deltaPercent, _mouseLocation.X, _mouseLocation.Y);
            }
            finally
            {
                // Wheel Events wont be treated received during the play animation effect
                Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { }));
                //Application.DoEvents();
                MouseWheel += OnMouseWheel;
            }
            _deltaPercent = 0;
        }

        #endregion

        #region Private method

        /// <summary>
        /// Measurement of the parameters you move the mouse
        /// </summary>
        /// <param name="p">Mouse position</param>
        private void MeasureMouseMovementParameters(Point p)
        {
            if (_mouseX[0] == -1)
            {
                _mouseX[0] = p.X; _mouseX[1] = p.X;
                _mouseX[2] = p.X; _mouseX[3] = p.X;
                _mouseY[0] = p.Y; _mouseY[1] = p.Y;
                _mouseY[2] = p.Y; _mouseY[3] = p.Y;

                _mouseTime = new DateTime[4]
                                 {
                                     DateTime.Now,
                                     DateTime.Now,
                                     DateTime.Now,
                                     DateTime.Now
                                 };
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

                _mouseX[0] = p.X;
                _mouseY[0] = p.Y;
                _mouseTime[0] = dt;
            }
        }

        private ICoordinate GetMouseSpeed(DateTime satrtTime, DateTime endtime, Point startPoint, Point endPoint)
        {
            double milliseconds = (endtime - satrtTime).TotalMilliseconds;

            if (milliseconds == 0)
                return PlanimetryEnvironment.NewCoordinate(0, 0);

            return PlanimetryEnvironment.NewCoordinate((startPoint.X - endPoint.X) / milliseconds,
                                                       (startPoint.Y - endPoint.Y) / milliseconds);
        }

        private ICoordinate GetAvgMouseSpeed()
        {
            ICoordinate v1 = GetMouseSpeed(_mouseTime[3],
                                           _mouseTime[2],
                                           new Point(_mouseX[3], _mouseY[3]),
                                           new Point(_mouseX[2], _mouseY[2]));

            ICoordinate v2 = GetMouseSpeed(_mouseTime[2],
                                           _mouseTime[1],
                                           new Point(_mouseX[2], _mouseY[2]),
                                           new Point(_mouseX[1], _mouseY[1]));

            ICoordinate v3 = GetMouseSpeed(_mouseTime[1],
                                           _mouseTime[0],
                                           new Point(_mouseX[1], _mouseY[1]),
                                           new Point(_mouseX[0], _mouseY[0]));

            double vxAvg = 0.35 *
                           -(v1.X * (_mouseTime[2] - _mouseTime[3]).TotalMilliseconds +
                             v2.X * (_mouseTime[1] - _mouseTime[2]).TotalMilliseconds +
                             v3.X * (_mouseTime[0] - _mouseTime[1]).TotalMilliseconds) /
                           (_mouseTime[0] - _mouseTime[3]).TotalMilliseconds;

            double vyAvg = 0.35 *
                           -(v1.Y * (_mouseTime[2] - _mouseTime[3]).TotalMilliseconds +
                             v2.Y * (_mouseTime[1] - _mouseTime[2]).TotalMilliseconds +
                             v3.Y * (_mouseTime[0] - _mouseTime[1]).TotalMilliseconds) /
                           (_mouseTime[0] - _mouseTime[3]).TotalMilliseconds;

            return PlanimetryEnvironment.NewCoordinate(vxAvg, vyAvg);
        }

        private void DrawDragging(DrawingContext dc)
        {
            if (_draggingMode == DraggingMode.Pan)
            {
                Rect rect = new Rect(_offsetX, _offsetY, ActualWidth, ActualHeight);
                dc.DrawImage(_bitmap, rect);
            }
            if (_draggingMode == DraggingMode.Zoom)
            {
                Rect rect = new Rect(0, 0, ActualWidth, ActualHeight);
                dc.DrawImage(_bitmap, rect);

                Point upperLeft = new Point(Math.Min(_mouseDownX, _mouseDownX + _offsetX),
                                            Math.Min(_mouseDownY, _mouseDownY + _offsetY));

                Rect r = new Rect(upperLeft, new Size(Math.Abs(_offsetX), Math.Abs(_offsetY)));

                SolidColorBrush brush = new SolidColorBrush(new Color
                {
                    A = 30,
                    B = _selectionRectangleColor.B,
                    G = _selectionRectangleColor.G,
                    R = _selectionRectangleColor.R
                });

                Pen pen = new Pen(new SolidColorBrush(new Color
                {
                    A = 80,
                    B = _selectionRectangleColor.B,
                    G = _selectionRectangleColor.G,
                    R = _selectionRectangleColor.R,
                }), 1);

                dc.DrawRectangle(brush, pen, r);
            }
        }

        private void DrawAnimated(DrawingContext dc)
        {
            if (_currentRectangle.Width <= 1e9 && _currentRectangle.Width >= -1e9 &&
                _currentRectangle.Height <= 1e9 && _currentRectangle.Height >= -1e9 &&
                _currentRectangle.Left <= 1e9 && _currentRectangle.Left >= -1e9 &&
                _currentRectangle.Top <= 1e9 && _currentRectangle.Top >= -1e9)
            {
                if (_opacity == 0)
                {
                    dc.DrawImage(_bitmap, _currentRectangle);
                }
                else
                {
                    Rect r = new Rect(new Point(0, 0), new Size(this.ActualWidth, this.ActualHeight));

                    dc.DrawImage(_asyncMapImage, r);

                    _opocityColorMatrix.BeginInit();
                    _opocityColorMatrix.Source = _bitmap;

                    if (_opacity > 0)
                    {
                        _opocityColorMatrix.AlphaThreshold = _opacity;
                    }
                    else
                    {
                        _opocityColorMatrix.AlphaThreshold = 0;
                    }

                    _opocityColorMatrix.DestinationFormat = PixelFormats.Default;

                    _opocityColorMatrix.EndInit();
                    dc.DrawImage(_opocityColorMatrix, r);

                }
            }
            _animated = false;
        }

        /// <summary> 
        /// The main mode is called when rendering при _animated = false;
        /// </summary>
        /// <param name="dc">DrawingContext</param>
        private void DrawGeneral(DrawingContext dc)
        {
            Rect r = new Rect(new Point(_startAnimationOffsetX, _startAnimationOffsetY),
                              new Size(this.ActualWidth, this.ActualHeight));

            dc.DrawImage(_bitmap, r);
        }

        /// <summary>
        /// Fit Size ViewBox
        /// </summary>
        /// <param name="viewBox"></param>
        private void FitViewBoxSize(BoundingRectangle viewBox)
        {
            if (viewBox == null)
                return;

            if (viewBox.IsEmpty())
                return;

            double dx = 0;
            double dy = 0;

            if (ActualWidth / ActualHeight > viewBox.Width / viewBox.Height)
                dy = -(viewBox.Height - ActualHeight / ActualWidth * viewBox.Width);
            else
                dx = -(viewBox.Width - ActualWidth / ActualHeight * viewBox.Height);

            // Sets a bounds of all shapes in file
            viewBox.SetBounds(viewBox.MinX, viewBox.MinY, viewBox.MaxX + dx, viewBox.MaxY + dy);
        }

        /// <summary>
        /// Set ViewBox with animation
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        private void SetViewBoxWithAnimation(Rect begin, Rect end)
        {
            // Event Handlers to perorm better in the main stream
            if (BeforeMapRender != null)
                BeforeMapRender(this, new ViewBoxEventArgs(_viewBox));

            // Block event Input to the animation
            bool oldFocused = Focusable;

            try
            {
                // Start thread generating image 
                Thread thread = BeginMapDrawing();
                thread.Start();
                // Start effect animation in main thread
                Play(begin, end);
                // Waiting for the completion of the process of genrating the map image
                thread.Join();

                _asyncMapImage = ConvertBitmap(_asyncBitmapMapImage);

                // New image generate, play effect "showing" image
                if (begin.Width != end.Width || begin.Height != end.Height)
                    PlayFade();

                _startAnimationOffsetX = 0;
                _startAnimationOffsetY = 0;

                _bitmap = _asyncMapImage;
                //_asyncMapImage = null;

                this.InvalidateVisual();
            }
            finally
            {
                if (oldFocused)
                    Focus();
            }
        }

        /// <summary>
        /// Playing a manifestation of the effect of image
        /// </summary>
        private void PlayFade()
        {
            _animated = true;
            try
            {

                TimeSpan animationInterval = new TimeSpan(0, 0, 0, 0,
                                                          (int)Math.Round(_animationTime * (1 - _mainAnimationRelativeDuration)));
                DateTime begin = DateTime.Now;
                DateTime end = begin.Add(animationInterval);
                DateTime now = DateTime.Now;

                while (now < end)
                {
                    double t = (double)(now - begin).Ticks / (double)animationInterval.Ticks;
                    if (t > 0)
                    {
                        _opacity = -t;

                        this.InvalidateVisual();
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

        /// <summary>
        /// Reproduction shift Rectangle
        /// </summary>
        /// <param name="startRectangle">The initial position Rectangle</param>
        /// <param name="endRectangle">The final position Rectangle</param>
        private void Play(Rect startRectangle, Rect endRectangle)
        {
            _animated = true;
            try
            {
                double xScaleFactor = startRectangle.Width / endRectangle.Width;
                double yScaleFactor = startRectangle.Height / endRectangle.Height;

                endRectangle = new Rect(new Point((-endRectangle.X * xScaleFactor), (-endRectangle.Y * yScaleFactor)),
                                        new Size((startRectangle.Height * yScaleFactor),
                                                 (startRectangle.Width * xScaleFactor)));

                TimeSpan animationInterval = new TimeSpan(0, 0, 0, 0,
                                                          (int)Math.Round(_animationTime * _mainAnimationRelativeDuration));
                DateTime begin = DateTime.Now;
                DateTime end = begin.Add(animationInterval);

                double dTop = startRectangle.Top - endRectangle.Top;
                double dLeft = startRectangle.Left - endRectangle.Left;
                double dRight = startRectangle.Right - endRectangle.Right;
                double dBottom = startRectangle.Bottom - endRectangle.Bottom;

                DateTime now = DateTime.Now;



                while (now < end)
                {
                    double t = (double)(now - begin).Ticks / (double)animationInterval.Ticks;
                    if (t > 0)
                    {
                        double factor = Math.Pow(t, 0.25);

                        double newLeft = Math.Round(startRectangle.Left - dLeft * factor);
                        double newTop = Math.Round(startRectangle.Top - dTop * factor);
                        double newRight = Math.Round(startRectangle.Right - dRight * factor);
                        double newBottom = Math.Round(startRectangle.Bottom - dBottom * factor);

                        _currentRectangle = new Rect(newLeft, newTop, newRight - newLeft, newBottom - newTop);

                        UpdateLayout();
                        this.InvalidateVisual();
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

        /// <summary>
        /// Start drawing map
        /// </summary>
        /// <returns>Returns the thread does rendering</returns>
        private Thread BeginMapDrawing()
        {
            Thread result = new Thread(MapDrawWorker);
            result.Priority = ThreadPriority.BelowNormal;
            return result;
        }

        /// <summary>
        /// Render Map
        /// </summary>
        private void MapDrawWorker()
        {
            _asyncBitmapMapImage = _map.Render((int)ActualWidth, (int)ActualHeight, _viewBox);
        }

        /// <summary>
        ///Calculation of the animation Rectangle
        /// </summary>
        /// <param name="oldViewBox"></param>
        /// <param name="viewBox"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        private void CalcAnimationRectangles(BoundingRectangle oldViewBox, BoundingRectangle viewBox, out Rect begin, out Rect end)
        {
            begin = new Rect();
            end = new Rect();

            if (!oldViewBox.IsEmpty())
            {
                begin = MapViewBoxToClientRectangle(oldViewBox);

                if (_startAnimationOffsetX != 0 || _startAnimationOffsetY != 0)
                {
                    begin = new Rect(new Point(_startAnimationOffsetX, _startAnimationOffsetY), begin.Size);
                }
            }

            if (!viewBox.IsEmpty() && !_viewBox.IsEmpty())
                end = MapViewBoxToClientRectangle(viewBox);
        }

        /// <summary>
        /// Translated from the card in the client rectangle ViewBox
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        private Rect MapViewBoxToClientRectangle(BoundingRectangle r)
        {
            Point p1 = MapToClient(r.Min);
            Point p2 = MapToClient(r.Max);

            return new Rect(new Point(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y)),
                            new Size(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y)));

        }

        /// <summary>
        /// Has been modified ViewBox
        /// </summary>
        /// <param name="newViewBox">The new state ViewBox</param>
        /// <returns></returns>
        private bool WhetherViewBoxChanged(BoundingRectangle newViewBox)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="horizontalShift"></param>
        /// <param name="verticalShift"></param>
        private void AnimatedPan(double horizontalShift, double verticalShift)
        {
            if (_map == null || _viewBox == null)
                return;

            if (!_viewBox.IsEmpty())
            {
                double xFactor = _viewBox.Width / ActualWidth;
                double yFactor = _viewBox.Height / ActualHeight;

                BoundingRectangle viewBox = new BoundingRectangle(_viewBox.MinX - horizontalShift * xFactor,
                                                                  _viewBox.MinY + verticalShift * yFactor,
                                                                  _viewBox.MaxX - horizontalShift * xFactor,
                                                                  _viewBox.MaxY + verticalShift * yFactor);

                SetViewBox(viewBox, true, false, true);
            }
        }

        private ICoordinate PrepareForSelect(Point position)
        {
            //BoundingRectangle mapCoordsViewBox = _map.MapViewBoxFromPresentationViewBox(_viewBox);

            double x = _viewBox.Width / ActualWidth * position.X + _viewBox.MinX;
            double y = _viewBox.MaxY - _viewBox.Height / ActualHeight * position.Y;

            // Caculate the error of selecting the point and line objects
            _map.SelectionPointRadius = _selectionMargin * _viewBox.Width / ActualWidth;

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
        /// Converter Bitmap в BitmapSource
        /// </summary>
        /// <param name="gdiPlusBitmap">Bitmap Image</param>
        /// <returns>BitmapSource</returns>
        private static BitmapSource ConvertBitmap(System.Drawing.Bitmap gdiPlusBitmap)
        {
            using (gdiPlusBitmap)
            {
                IntPtr hBitmap = gdiPlusBitmap.GetHbitmap();

                return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty,
                                                             BitmapSizeOptions.FromEmptyOptions());
            }
        }

        #endregion

        #region Public method

        /// <summary>
        /// Initializes a new instance of MapAround.UI.WPF.MapControl
        /// </summary>
        public MapControl()
        {
            InitializeComponent();

            _oldHeight = this.ActualHeight;
            _oldWidth = this.ActualWidth;

            MouseLeftButtonUp += OnMouseLeftButtonUp;
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseWheel += OnMouseWheel;
            MouseMove += OnMouseMove;
            SizeChanged += OnSizeChanged;

            _wheelTimer.Interval = new TimeSpan(80);
            _wheelTimer.Tick += WheelTimerTick;
        }

        /// <summary>
        /// Sets a new visible area of map.
        /// </summary>
        /// <param name="viewBox">Bounding rectangle defining an area of view</param>
        public void SetViewBox(BoundingRectangle viewBox)
        {
            SetViewBox(viewBox, false, false, false);
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
                    _bitmap = null;
                this.InvalidateVisual();
            }

            bool viewBoxChanged = WhetherViewBoxChanged(viewBox);

            if (forceSizeFitting)
                FitViewBoxSize(viewBox);

            BoundingRectangle oldViewBox = _viewBox;
            Rect begin;
            Rect end;

            CalcAnimationRectangles(oldViewBox, viewBox, out begin, out end);

            _viewBox = viewBox;

            if (_map != null && !viewBox.IsEmpty())
                if (viewBoxChanged || forceRedraw)
                {
                    if (_animation && playAnimation && !oldViewBox.IsEmpty())
                        // Need playing animation
                        // Требуется проигрывание анимации
                        SetViewBoxWithAnimation(begin, end);
                    else
                    {
                        if (_bitmap != null)
                            _bitmap = null;

                        RedrawMap();
                    }
                }

            if (viewBoxChanged && ViewBoxChanged != null)
                ViewBoxChanged(this, new ViewBoxEventArgs(_viewBox));
        }

        /// <summary>
        /// Transforms map coordinates to screen coordinates.
        /// </summary>
        /// <param name="point">A point on the map</param>
        /// <returns>A point on the screen</returns>
        public Point MapToClient(ICoordinate point)
        {
            double scaleFactor = Math.Min(ActualWidth / _viewBox.Width, ActualHeight / _viewBox.Height);

            double resultX = Math.Round((point.X - _viewBox.MinX) * scaleFactor);
            double resultY = Math.Round((_viewBox.MaxY - point.Y) * scaleFactor);

            return new Point(resultX, resultY);
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

                    _bitmap = ConvertBitmap(_map.Render((int)ActualWidth, (int)ActualHeight, _viewBox));
                    this.InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Changes a zoom.
        /// </summary>
        /// <param name="deltaPercent">A value (percents) by which a zoom change</param>
        /// <param name="mouseX">An X coordinate of the mouse</param>
        /// <param name="mouseY">A Y coordinate of the mouse</param>
        public void ChangeZoom(int deltaPercent, double mouseX, double mouseY)
        {
            if (_map == null || _viewBox == null)
                return;

            if (deltaPercent != 0)
            {
                if (_viewBox.IsEmpty())
                    return;

                double delta = deltaPercent / 100.0;
                if (delta > 0)
                    delta *= 2;

                if (!_alignmentWhileZooming)
                {
                    mouseX = ActualWidth / 2;
                    mouseY = ActualHeight / 2;
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
            ChangeZoom(_zoomPercent, ActualWidth / 2, ActualHeight / 2);
        }

        /// <summary>
        /// Zooms out.
        /// </summary>
        public void ZoomOut()
        {
            ChangeZoom(-_zoomPercent, ActualWidth / 2, ActualHeight / 2);
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

            ICoordinate point = PrepareForSelect(position);

            _map.SelectTopObject(point, ActualWidth / _viewBox.Width, out feature);

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

            ICoordinate point = PrepareForSelect(position);

            IList<Feature> result;
            _map.SelectObjects(point, ActualWidth / _viewBox.Width, out result);

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

            return PlanimetryEnvironment.NewCoordinate(_viewBox.Width / ActualWidth * point.X + _viewBox.MinX,
                                                       _viewBox.MaxY - _viewBox.Height / ActualHeight * point.Y);
        }

        /// <summary>
        /// Pans a map.
        /// </summary>
        /// <param name="horizontalShift">Horizontal shift in pixels</param>
        /// <param name="verticalShift">Vertical shift in pixels</param>
        public void Pan(double horizontalShift, double verticalShift)
        {
            if (_map == null || _viewBox == null)
                return;

            if (!_viewBox.IsEmpty())
            {
                double xFactor = _viewBox.Width / ActualWidth;
                double yFactor = _viewBox.Height / ActualHeight;
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
        public void Pan(double horizontalShift, double verticalShift, bool forceRedraw, bool playAnimation)
        {
            if (_map == null || _viewBox == null)
                return;

            if (!_viewBox.IsEmpty())
            {
                double xFactor = _viewBox.Width / ActualWidth;
                double yFactor = _viewBox.Height / ActualHeight;
                BoundingRectangle viewBox = new BoundingRectangle(_viewBox.MinX - horizontalShift * xFactor,
                                                                  _viewBox.MinY + verticalShift * yFactor,
                                                                  _viewBox.MaxX - horizontalShift * xFactor,
                                                                  _viewBox.MaxY + verticalShift * yFactor);

                SetViewBox(viewBox, forceRedraw, false, playAnimation);
            }
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
        /// Gets or sets a Value (in milliseconds) of the animation effect duration.
        /// </summary>
        [Category("Behavior")]
        [Description("Value (in milliseconds) of the animation effect duration.")]
        public double AnimationTime
        {
            get { return _animationTime; }
            set { _animationTime = value; }
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
        /// Gets or sets a value (percent) of zoom change. 
        /// This value is used by ZoomIn and ZoomOut methods.
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
        /// Gets or sets a minimum number of pixels on which user 
        /// must shift the map to start new image rendering.
        /// </summary>
        [Category("Behavior")]
        [Description("Minimum number of pixels on which user must shift the map to start new image rendering.")]
        public int DragThreshold
        {
            get { return _dragThreshold; }
            set { _dragThreshold = value; }
        }

        #endregion

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

        /// <summary>
        /// The MapAround.UI.WPF namespace contains classes and interfaces 
        /// for developing WPF user interface containing a map widgets.
        /// </summary>
        internal class NamespaceDoc
        {
        }

        /// <summary>
        /// Provides data for the ViewBoxChanged, BeforeMapRender, SelectionRectangleDefined events.
        /// </summary>
        public class ViewBoxEventArgs : EventArgs
        {
            private readonly BoundingRectangle _viewBox;

            /// <summary>
            /// Gets a bounding rectangle defining a viewing area of map.
            /// </summary>
            public BoundingRectangle ViewBox
            {
                get { return _viewBox; }
            }

            /// <summary>
            /// Initializes a new instance of MapAround.UI.WinForms.ViewBoxEventArgs
            /// </summary>
            /// <param name="viewBox">A bounding rectangle defining a viewing area of map
            /// </param>
            public ViewBoxEventArgs(BoundingRectangle viewBox)
            {
                _viewBox = viewBox;
            }
        }
    }
}
