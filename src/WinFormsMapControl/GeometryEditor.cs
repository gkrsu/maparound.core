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
** File: GeometryEditor.cs
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Editing of geometries
**
=============================================================================*/


namespace MapAround.UI.WinForms
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Linq;

    using MapAround.Geometry;

    /// <summary>
    /// Represents a geometry editor.
    /// </summary>
    public class GeometryEditor
    {
        private IGeometry _geometry = null;
        private int _nodeSize = 4;
        private int _activeNodeNumber = -1;
        private int _activePatchIndex = -1;
        private int _activeCoordinateIndex = -1;

        private Snapper _snapper = null;

        private bool _antialias = false;

        private Color _fillColor = Color.FromArgb(70, Color.Blue);
        private Color _inactivePatchColor = Color.FromArgb(100, Color.Blue);

        private Color _primaryNodeColor = Color.FromArgb(150, Color.Blue);
        private Color _secondaryNodeColor = Color.FromArgb(70, Color.Blue);

        private Color _activePatchColor = Color.FromArgb(200, Color.Red);
        private Color _activeNodeColor = Color.Red;

        private ICoordinate oldActiveNodeCoordinate = null;

        private History _history = new History();

        #region history
        
        /// <summary>
        /// Undo edit action.
        /// </summary>
        public void Undo()
        {
            this.ActiveNodeNumber = -1;
            _geometry = _history.Undo(_geometry);
        }

        /// <summary>
        /// Redo edit action.
        /// </summary>
        public void Redo()
        {
            this.ActiveNodeNumber = -1;
            _geometry = _history.Redo(_geometry);
        }

        /// <summary>
        /// Gets a value indicating whether an undoing is possible.
        /// </summary>
        public bool CanUndo
        {
            get { return _history.CanUndo; }
        }

        /// <summary>
        /// Gets a value indicating whether an redoing is possible.
        /// </summary>
        public bool CanRedo
        {
            get { return _history.CanRedo; }
        }

        /// <summary>
        /// Clears the history of edit operations.
        /// </summary>
        public void ClearHistory()
        {
            _history.Clear();
        }

        /// <summary>
        /// Gets a number of actions in the history.
        /// </summary>
        public int HistoryEntryCount
        {
            get
            { return _history.Count; }
        }

        /// <summary>
        /// Gets a number of actions in history that can be undone.
        /// </summary>
        public int UndoCount
        {
            get
            {
                return _history.UndoCount;
            }
        }

        /// <summary>
        /// Gets a number of actions in history that can be redone.
        /// </summary>
        public int RedoCount
        {
            get
            {
                return _history.RedoCount;
            }
        }

        #endregion

        #region Drawing

        /// <summary>
        /// Draws editing geometry.
        /// </summary>
        /// <param name="g">A System.Drawing.Graphics instance where to draw the geometry</param>
        /// <param name="mapToClient">A method performing map-to-screen conversion</param>
        /// <param name="offsetX">An X offset of geometry in pixels</param>
        /// <param name="offsetY">A Y offset of geometry in pixels</param>
        public void DrawEditingGeometry(Graphics g, MapToClientDelegate mapToClient, int offsetX, int offsetY)
        {
            if (_geometry == null)
                return;

            Point[] points;
            if (_antialias)
                g.SmoothingMode = SmoothingMode.AntiAlias;
            else
                g.SmoothingMode = SmoothingMode.HighSpeed;

            using (GraphicsPath path = new GraphicsPath(FillMode.Alternate))
            {
                if (_geometry is Polygon)
                {
                    path.StartFigure();
                    Polygon p = (Polygon)_geometry;
                    foreach (Contour c in p.Contours)
                    {
                        points = new Point[c.CoordinateCount];
                        for (int i = 0; i < c.CoordinateCount; i++)
                        {
                            points[i] = mapToClient(c.Vertices[i]);
                            points[i].X += offsetX;
                            points[i].Y += offsetY;
                        }
                        if (points.Length > 2)
                            path.AddPolygon(points);
                        else if (points.Length == 2)
                            path.AddLines(points);
                    }
                    path.CloseFigure();

                    using (Brush b = new SolidBrush(_fillColor))
                        g.FillPath(b, path);
                }

                if (_geometry is Polyline)
                {
                    Polyline p = (Polyline)_geometry;
                    foreach (LinePath pt in p.Paths)
                    {
                        path.StartFigure();
                        points = new Point[pt.CoordinateCount];
                        for (int i = 0; i < pt.CoordinateCount; i++)
                        {
                            points[i] = mapToClient(pt.Vertices[i]);
                            points[i].X += offsetX;
                            points[i].Y += offsetY;
                        }
                        if (points.Length > 1)
                            path.AddLines(points);
                    }
                }

                using (Pen p = new Pen(_inactivePatchColor))
                    g.DrawPath(p, path);

                if (_activePatchIndex >= 0)
                {
                    using (GraphicsPath activePath = new GraphicsPath(FillMode.Alternate))
                    {
                        activePath.StartFigure();
                        if (_geometry is Polygon)
                        {
                            Polygon p = (Polygon)_geometry;
                            Contour c = p.Contours[_activePatchIndex];

                            points = new Point[c.CoordinateCount];
                            for (int i = 0; i < c.CoordinateCount; i++)
                            {
                                points[i] = mapToClient(c.Vertices[i]);
                                points[i].X += offsetX;
                                points[i].Y += offsetY;
                            }
                            if (points.Length > 2)
                                activePath.AddPolygon(points);
                            else if (points.Length == 2)
                                activePath.AddLines(points);
                            activePath.CloseFigure();
                        }

                        if (_geometry is Polyline)
                        {
                            Polyline p = (Polyline)_geometry;
                            LinePath pt = p.Paths[_activePatchIndex];
                            points = new Point[pt.CoordinateCount];
                            for (int i = 0; i < pt.CoordinateCount; i++)
                            {
                                points[i] = mapToClient(pt.Vertices[i]);
                                points[i].X += offsetX;
                                points[i].Y += offsetY;
                            }
                            if (points.Length > 1)
                                activePath.AddLines(points);
                        }
                        using (Pen p = new Pen(_activePatchColor))
                            g.DrawPath(p, activePath);
                    }
                }

                drawNodes(GetSecondaryNodes(), g, mapToClient, _secondaryNodeColor, offsetX, offsetY);
                drawNodes(_geometry.ExtractCoordinates(), g, mapToClient, _primaryNodeColor, offsetX, offsetY);

                if (_activeCoordinateIndex >= 0 && _activePatchIndex >= 0)
                    drawActiveNode(g, mapToClient, _activeNodeColor, offsetX, offsetY);
            }
        }

        private void drawActiveNode(Graphics g, MapToClientDelegate MapToClient, Color color, int offsetX, int offsetY)
        {
            ICoordinate c = null;
            if (_geometry is Polyline)
                c = ((Polyline)_geometry).Paths[_activePatchIndex].Vertices[_activeCoordinateIndex];

            if (_geometry is Polygon)
                c = ((Polygon)_geometry).Contours[_activePatchIndex].Vertices[_activeCoordinateIndex];

            drawNodes(new ICoordinate[] { c }, g, MapToClient, color, offsetX, offsetY);
        }

        private void drawNodes(IEnumerable<ICoordinate> nodes, Graphics g, MapToClientDelegate MapToClient, Color color, int offsetX, int offsetY)
        {
            if (nodes.Count() <= 0)
                return;

            using (Pen p = new Pen(color))
            {
                Point[] points = new Point[nodes.Count()];

                int i = 0;
                foreach (ICoordinate c in nodes)
                {
                    points[i] = MapToClient(c);
                    points[i].X += offsetX;
                    points[i].Y += offsetY;
                    i++;
                }

                Rectangle[] rectangles = new Rectangle[points.Length];

                int ns = this.NodeSize;
                for (i = 0; i < points.Length; i++)
                    rectangles[i] = new Rectangle((int)points[i].X - ns,
                        (int)points[i].Y - ns, ns * 2, ns * 2);

                using (Brush b = new SolidBrush(color))
                {
                    g.FillRectangles(b, rectangles);
                }
                g.DrawRectangles(p, rectangles);
            }
        }

        #endregion

        private void recalcActiveElements()
        {
            _activeCoordinateIndex = -1;
            _activePatchIndex = -1;

            if (_activeNodeNumber == -1) return;

            int k = 0;
            if (_geometry is Polyline)
            {
                Polyline polyline = (Polyline)_geometry;
                for (int i = 0; i < polyline.Paths.Count; i++)
                {
                    if (k + polyline.Paths[i].CoordinateCount > this.ActiveNodeNumber)
                    {
                        for (int j = 0; j < polyline.Paths[i].CoordinateCount; j++)
                        {
                            if (k == this.ActiveNodeNumber)
                            {
                                _activePatchIndex = i;
                                _activeCoordinateIndex = j;
                                return;
                            }
                            k++;
                        }
                    }
                    else
                        k += polyline.Paths[i].CoordinateCount;
                }
            }

            if (_geometry is Polygon)
            {
                Polygon polygon = (Polygon)_geometry;
                for (int i = 0; i < polygon.Contours.Count; i++)
                {
                    if (k + polygon.Contours[i].CoordinateCount > this.ActiveNodeNumber)
                    {
                        for (int j = 0; j < polygon.Contours[i].CoordinateCount; j++)
                        {
                            if (k == this.ActiveNodeNumber)
                            {
                                _activePatchIndex = i;
                                _activeCoordinateIndex = j;
                                return;
                            }
                            k++;
                        }
                    }
                    else
                        k += polygon.Contours[i].CoordinateCount;
                }
            }
        }

        /// <summary>
        /// Gets secondaty nodes of the editing feature.
        /// Secondary nodes are nodes placed at the middle points of 
        /// the feature segments.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ICoordinate> GetSecondaryNodes()
        {
            List<ICoordinate> result = new List<ICoordinate>();
            if (_geometry is Polyline)
            {
                Polyline p = (Polyline)_geometry;
                foreach (LinePath path in p.Paths)
                {
                    for (int i = 0; i < path.Vertices.Count - 1; i++)
                    {
                        result.Add(PlanimetryEnvironment.NewCoordinate(
                            (path.Vertices[i].X + path.Vertices[i + 1].X) / 2,
                            (path.Vertices[i].Y + path.Vertices[i + 1].Y) / 2));
                    }
                }
            }

            if (_geometry is Polygon)
            {
                Polygon p = (Polygon)_geometry;
                foreach (Contour c in p.Contours)
                {
                    for (int i = 0; i < c.Vertices.Count; i++)
                    {
                        int j = i == c.Vertices.Count - 1 ? 0 : i + 1;
                        result.Add(PlanimetryEnvironment.NewCoordinate(
                            (c.Vertices[i].X + c.Vertices[j].X) / 2,
                            (c.Vertices[i].Y + c.Vertices[j].Y) / 2));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Tries to add a new node into editing geometry.
        /// </summary>
        /// <param name="coordinate">An instance of the ICoordinate to add</param>
        public bool TryAddNode(ICoordinate coordinate)
        {
            if (_geometry is Polyline)
            {
                Polyline p = (Polyline)_geometry;
                int k = 0;
                foreach (LinePath path in p.Paths)
                {
                    for (int i = 0; i < path.Vertices.Count - 1; i++)
                    {
                        ICoordinate c =
                        PlanimetryEnvironment.NewCoordinate(
                            (path.Vertices[i].X + path.Vertices[i + 1].X) / 2,
                            (path.Vertices[i].Y + path.Vertices[i + 1].Y) / 2);

                        if (c.Equals(coordinate))
                        {
                            insertNodeAt(k, i + 1, coordinate);
                            return true;
                        }
                    }
                    k++;
                }
            }

            if (_geometry is Polygon)
            {
                Polygon p = (Polygon)_geometry;
                int k = 0;
                foreach (Contour contour in p.Contours)
                {
                    for (int i = 0; i < contour.Vertices.Count; i++)
                    {
                        int j = i == contour.Vertices.Count - 1 ? 0 : i + 1;

                        ICoordinate c =
                        PlanimetryEnvironment.NewCoordinate(
                            (contour.Vertices[i].X + contour.Vertices[j].X) / 2,
                            (contour.Vertices[i].Y + contour.Vertices[j].Y) / 2);

                        if (c.Equals(coordinate))
                        {
                            insertNodeAt(k, i + 1, coordinate);
                            return true;
                        }
                    }
                    k++;
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to activate a node at the specified position.
        /// </summary>
        /// <param name="coordinate">A coordinate of node to be activated</param>
        /// <param name="nodeSize">A size of node</param>
        /// <returns>true if the node be activated, false otherwise</returns>
        public bool TryActivateNodeAt(ICoordinate coordinate, double nodeSize)
        {
            IEnumerable<ICoordinate> coordinates = _geometry.ExtractCoordinates();

            int i = 0;
            foreach (ICoordinate coord in coordinates)
            {
                BoundingRectangle br =
                    new BoundingRectangle(coord.X - nodeSize,
                        coord.Y - nodeSize,
                        coord.X + nodeSize,
                        coord.Y + nodeSize);

                if (br.ContainsPoint(coordinate))
                {
                    this.ActiveNodeNumber = i;

                    if (_geometry is Polygon)
                        oldActiveNodeCoordinate = (ICoordinate)(_geometry as Polygon).Contours[_activePatchIndex].Vertices[_activeCoordinateIndex].Clone();

                    if (_geometry is Polyline)
                        oldActiveNodeCoordinate = (ICoordinate)(_geometry as Polyline).Paths[_activePatchIndex].Vertices[_activeCoordinateIndex].Clone();

                    return true;
                }
                i++;
            }

            coordinates = this.GetSecondaryNodes();
            foreach (ICoordinate coord in coordinates)
            {
                BoundingRectangle br =
                    new BoundingRectangle(coord.X - nodeSize,
                        coord.Y - nodeSize,
                        coord.X + nodeSize,
                        coord.Y + nodeSize);

                if (br.ContainsPoint(coordinate))
                {
                    this.TryAddNode(coord);
                    return this.TryActivateNodeAt(coordinate, nodeSize);
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the position of the active node with the specified value
        /// </summary>
        /// <param name="coordinate">New position of the active node</param>
        /// <param name="commitIntoUndoList">A value indicating whether current update should be commited into undo list</param>
        /// <returns>true if the position has been updated, false otherwise</returns>
        public bool UpdateActiveNodePosition(ICoordinate coordinate, bool commitIntoUndoList)
        {
            if (ActivePatchIndex == -1)
                return false;

            if (commitIntoUndoList)
            {
                EditAction forwardAction;
                EditAction backwardAction;
                EditActionParameters parameters = new EditActionParameters()
                {
                    OldGeometry = _geometry,
                    PatchIndex = _activePatchIndex,
                    NodeIndex = _activeCoordinateIndex,
                    NodePosition = (ICoordinate)coordinate.Clone()
                };

                // update active node with the old value
                // this is necessary for properly generation of the inverse action 
                UpdateActiveNodePosition(oldActiveNodeCoordinate, false);

                forwardAction = new EditAction(EditActionType.SetNodePosition, parameters, out backwardAction);
                HistoryEntry pair = new HistoryEntry(forwardAction, backwardAction);
                _geometry = _history.DoAction(pair, _geometry);

                oldActiveNodeCoordinate = (ICoordinate)coordinate.Clone();
            }
            else
            {
                if (_snapper != null && _snapper.SnapMethod != null)
                    coordinate = _snapper.SnapMethod(coordinate);

                if (_geometry is Polyline)
                {
                    Polyline polyline = (Polyline)_geometry;

                    polyline.Paths[_activePatchIndex].Vertices[_activeCoordinateIndex].X = coordinate.X;
                    polyline.Paths[_activePatchIndex].Vertices[_activeCoordinateIndex].Y = coordinate.Y;
                }

                if (_geometry is Polygon)
                {
                    Polygon polygon = (Polygon)_geometry;

                    polygon.Contours[_activePatchIndex].Vertices[_activeCoordinateIndex].X = coordinate.X;
                    polygon.Contours[_activePatchIndex].Vertices[_activeCoordinateIndex].Y = coordinate.Y;
                }
            }

            return true;
        }

        /// <summary>
        /// Removes the active node.
        /// </summary>
        public void RemoveActiveNode()
        {
            if (_activePatchIndex >= 0 && _activeCoordinateIndex >= 0)
            {
                removeNodeAt(_activePatchIndex, _activeCoordinateIndex);
                _activePatchIndex = -1;
                _activeCoordinateIndex = -1;
            }
        }

        /// <summary>
        /// Removes specified coordinate patch.
        /// </summary>
        /// <param name="patchIndex">An zero-based index of patch</param>
        public void RemovePatch(int patchIndex)
        {
            if (_geometry is Polygon)
                if ((_geometry as Polygon).Contours.Count <= patchIndex)
                    throw new ArgumentOutOfRangeException("Patch index exceeds the number of patches", "patchIndex");

            if (_geometry is Polyline)
                if ((_geometry as Polyline).Paths.Count <= patchIndex)
                    throw new ArgumentOutOfRangeException("Patch index exceeds the number of patches", "patchIndex");

            EditAction forwardAction;
            EditAction backwardAction;
            EditActionParameters parameters = new EditActionParameters()
            {
                OldGeometry = _geometry,
                PatchIndex = patchIndex,
            };

            forwardAction = new EditAction(EditActionType.DeletePatch, parameters, out backwardAction);
            HistoryEntry pair = new HistoryEntry(forwardAction, backwardAction);
            _geometry = _history.DoAction(pair, _geometry);

            _activePatchIndex = -1;
            _activeCoordinateIndex = -1;
        }

        /// <summary>
        /// Removes the active patch.
        /// </summary>
        public void RemoveActivePatch()
        {
            if (_activePatchIndex >= 0)
                RemovePatch(_activePatchIndex);
        }

        /// <summary>
        /// Inserts a new coordinate patch into the editing geometry.
        /// </summary>
        /// <param name="patchIndex">A zero-based index of patch</param>
        /// <param name="coordinates">A coordinates of inserted patch</param>
        public void InsertPatch(int patchIndex, IEnumerable<ICoordinate> coordinates)
        {
            if (_geometry is Polygon)
                if ((_geometry as Polygon).Contours.Count < patchIndex)
                    throw new ArgumentOutOfRangeException("Patch index exceeds the number of patches", "patchIndex");

            if (_geometry is Polyline)
                if ((_geometry as Polyline).Paths.Count < patchIndex)
                    throw new ArgumentOutOfRangeException("Patch index exceeds the number of patches", "patchIndex");

            EditAction forwardAction;
            EditAction backwardAction;
            EditActionParameters parameters = new EditActionParameters()
            {
                OldGeometry = _geometry,
                PatchIndex = patchIndex,
                PatchCoordinates = coordinates
            };

            forwardAction = new EditAction(EditActionType.InsertPatch, parameters, out backwardAction);
            HistoryEntry pair = new HistoryEntry(forwardAction, backwardAction);
            _geometry = _history.DoAction(pair, _geometry);

            _activePatchIndex = -1;
            _activeCoordinateIndex = -1;
        }

        /// <summary>
        /// Replaces currently editing geometry with the other one.
        /// </summary>
        /// <param name="newGeometry">A new instance of IGeometry</param>
        public void ReplaceGeometry(IGeometry newGeometry)
        {
            if (newGeometry == null || newGeometry is Polygon || newGeometry is Polyline)
            {
                EditAction forwardAction;
                EditAction backwardAction;
                EditActionParameters parameters = new EditActionParameters()
                {
                    OldGeometry = _geometry,
                    NewGeometry = newGeometry
                };

                forwardAction = new EditAction(EditActionType.ReplaceGeometry, parameters, out backwardAction);
                HistoryEntry pair = new HistoryEntry(forwardAction, backwardAction);
                _geometry = _history.DoAction(pair, _geometry);

                //_geometry = newGeometry;
                ActiveNodeNumber = -1;
            }
            else
                throw new InvalidCastException("Allows only Polygons and Polylines");
        }

        private void insertNodeAt(int patchIndex, int coordinateIndex, ICoordinate coordinate)
        {
            EditAction forwardAction;
            EditAction backwardAction;
            EditActionParameters parameters = new EditActionParameters()
            {
                OldGeometry = _geometry,
                PatchIndex = patchIndex,
                NodeIndex = coordinateIndex,
                NodePosition = (ICoordinate)coordinate.Clone()
            };

            forwardAction = new EditAction(EditActionType.InsertNode, parameters, out backwardAction);
            HistoryEntry pair = new HistoryEntry(forwardAction, backwardAction);
            _geometry = _history.DoAction(pair, _geometry);
        }

        private void removeNodeAt(int patchIndex, int coordinateIndex)
        {
            EditAction forwardAction;
            EditAction backwardAction;
            EditActionParameters parameters = new EditActionParameters()
            {
                OldGeometry = _geometry,
                PatchIndex = patchIndex,
                NodeIndex = coordinateIndex,
            };

            forwardAction = new EditAction(EditActionType.DeleteNode, parameters, out backwardAction);
            HistoryEntry pair = new HistoryEntry(forwardAction, backwardAction);
            _geometry = _history.DoAction(pair, _geometry);
        }

        /// <summary>
        /// Gets an index of the last edited coordinate in patch. 
        /// </summary>
        public int ActiveCoordinateIndex
        {
            get { return _activeCoordinateIndex; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether 
        /// an antialias is used to render editing feature.
        /// </summary>
        public bool Antialias
        {
            get { return _antialias; }
            set { _antialias = value; }
        }

        /// <summary>
        /// Gets an index of the last edited patch. 
        /// The patch is a line path or a contour containing geometry.
        /// </summary>
        public int ActivePatchIndex
        {
            get { return _activePatchIndex; }
        }

        internal int ActiveNodeNumber
        {
            get { return _activeNodeNumber; }
            set
            {
                _activeNodeNumber = value;
                recalcActiveElements();
            }
        }

        #region Colors

        /// <summary>
        /// Gets or sets a color which is used to draw 
        /// an active node of editing feature.
        /// </summary>
        public Color ActiveNodeColor
        {
            get { return _activeNodeColor; }
            set { _activeNodeColor = value; }
        }

        /// <summary>
        /// Gets or sets a color which is used to draw 
        /// an active patch (contour or line path) of editing feature.
        /// </summary>
        public Color ActivePatchColor
        {
            get { return _activePatchColor; }
            set { _activePatchColor = value; }
        }

        /// <summary>
        /// Gets or sets a color which is used to draw 
        /// secondary nodes of editing feature.
        /// </summary>
        public Color SecondaryNodeColor
        {
            get { return _secondaryNodeColor; }
            set { _secondaryNodeColor = value; }
        }

        /// <summary>
        /// Gets or sets a color which is used to draw 
        /// primary nodes of editing feature.
        /// </summary>
        public Color PrimaryNodeColor
        {
            get { return _primaryNodeColor; }
            set { _primaryNodeColor = value; }
        }

        /// <summary>
        /// Gets or sets a color which is used to draw 
        /// inactive patch (contour or line path) of editing feature.
        /// </summary>
        public Color InactivePatchColor
        {
            get { return _inactivePatchColor; }
            set { _inactivePatchColor = value; }
        }

        /// <summary>
        /// Gets or sets a color which is used to draw 
        /// an interior of polygon.
        /// </summary>
        public Color FillColor
        {
            get { return _fillColor; }
            set { _fillColor = value; }
        }

        #endregion

        /// <summary>
        /// Gets or sets a size of the feature nodes in pixels.
        /// </summary>
        public int NodeSize
        {
            get { return _nodeSize; }
            set { _nodeSize = value; }
        }

        /// <summary>
        /// Gets or sets an object that snaps the editing coordinates.
        /// </summary>
        public Snapper Snapper
        {
            get { return _snapper; }
            set { _snapper = value; }
        }

        /// <summary>
        /// Gets geometry representing the result of editing operations. 
        /// Changes in the resulting instance will not be reflected in the editor.
        /// </summary>
        public IGeometry ResultingGeometry
        {
            get
            {
                if (_geometry == null)
                    return null;

                return (IGeometry)_geometry.Clone(); 
            }
        }
    }

    internal enum EditActionType
    {
        InsertNode,     // PatchIndex, NodeIndex, NodePosition
        DeleteNode,     // OldGeometry, PatchIndex, NodeIndex
        SetNodePosition,// OldGeometry, PatchIndex, NodeIndex, NodePosition
        InsertPatch,    // PatchIndex, PatchCoordinates
        DeletePatch,    // OldGeometry, PatchIndex
        ReplaceGeometry // OldGeometry, NewGeometry
    }

    internal struct EditActionParameters
    {
        // applies to MoveNode, DeletePatch, DeleteNode and ReplaceGeometry actions
        public IGeometry OldGeometry;

        // applies to ReplaceGeometry action
        public IGeometry NewGeometry;

        // applies to InsertPatch action
        public IEnumerable<ICoordinate> PatchCoordinates;

        // applies to DeletePatch, InsertPatch, 
        // MoveNode, DeleteNode and InsertNode actions
        public int PatchIndex;

        // applies to MoveNode, DeleteNode and InsertNode actions
        public int NodeIndex;

        // applies to MoveNode and InsertNode actions
        public ICoordinate NodePosition;
    }

    internal class EditAction
    {
        private EditActionType _actionType;
        private EditActionParameters _parameters;

        public IGeometry Do(IGeometry geometry)
        {
            IGeometry result = geometry;

            if (_actionType == EditActionType.SetNodePosition)
            {
                if (geometry is Polyline)
                {
                    (geometry as Polyline).Paths[_parameters.PatchIndex].Vertices[_parameters.NodeIndex].X = _parameters.NodePosition.X;
                    (geometry as Polyline).Paths[_parameters.PatchIndex].Vertices[_parameters.NodeIndex].Y = _parameters.NodePosition.Y;
                }

                if (geometry is Polygon)
                {
                    (geometry as Polygon).Contours[_parameters.PatchIndex].Vertices[_parameters.NodeIndex].X = _parameters.NodePosition.X;
                    (geometry as Polygon).Contours[_parameters.PatchIndex].Vertices[_parameters.NodeIndex].Y = _parameters.NodePosition.Y;
                }
            }
            else if (_actionType == EditActionType.InsertNode)
            {
                if (geometry is Polyline)
                    (geometry as Polyline).Paths[_parameters.PatchIndex].Vertices.Insert(_parameters.NodeIndex, _parameters.NodePosition);

                if (geometry is Polygon)
                    (geometry as Polygon).Contours[_parameters.PatchIndex].Vertices.Insert(_parameters.NodeIndex, _parameters.NodePosition);
            }
            else if (_actionType == EditActionType.DeleteNode)
            {
                if (geometry is Polyline)
                    (geometry as Polyline).Paths[_parameters.PatchIndex].Vertices.RemoveAt(_parameters.NodeIndex);

                if (geometry is Polygon)
                    (geometry as Polygon).Contours[_parameters.PatchIndex].Vertices.RemoveAt(_parameters.NodeIndex);
            }
            else if (_actionType == EditActionType.InsertPatch)
            {
                if (geometry is Polyline)
                {
                    LinePath path = new LinePath(_parameters.PatchCoordinates);
                    (geometry as Polyline).Paths.Insert(_parameters.PatchIndex, path);
                }

                if (geometry is Polygon)
                {
                    Contour contour = new Contour(_parameters.PatchCoordinates);
                    (geometry as Polygon).Contours .Insert(_parameters.PatchIndex, contour);
                }

            }
            else if (_actionType == EditActionType.DeletePatch)
            {
                if (geometry is Polyline)
                    (geometry as Polyline).Paths.RemoveAt(_parameters.PatchIndex);

                if (geometry is Polygon)
                    (geometry as Polygon).Contours.RemoveAt(_parameters.PatchIndex);
            }
            else if (_actionType == EditActionType.ReplaceGeometry)
            {
                result = _parameters.NewGeometry;
            }

            return result;
        }

        private EditAction(EditActionType actionType, EditActionParameters parameters)
        {
            _actionType = actionType;
            _parameters = parameters;
        }

        private static EditActionType getInvertedAction(EditActionType forwardActionType)
        {
            switch (forwardActionType)
            {
                case EditActionType.SetNodePosition: return EditActionType.SetNodePosition;
                case EditActionType.ReplaceGeometry: return EditActionType.ReplaceGeometry;
                case EditActionType.InsertNode: return EditActionType.DeleteNode;
                case EditActionType.DeleteNode: return EditActionType.InsertNode;
                case EditActionType.InsertPatch: return EditActionType.DeletePatch;
                case EditActionType.DeletePatch: return EditActionType.InsertPatch;
            }

            throw new ArgumentException("Unsupported value", "forwardActionType");
        }

        public EditAction(EditActionType actionType, EditActionParameters parameters, out EditAction inverseAction)
        {
            _actionType = actionType;
            _parameters = parameters;

            EditActionParameters invertedParameters = new EditActionParameters();

            inverseAction = null;

            if (actionType == EditActionType.SetNodePosition)
            {
                invertedParameters.PatchIndex = parameters.PatchIndex;
                invertedParameters.NodeIndex = parameters.NodeIndex;

                ICoordinate oldPosition = null;
                if (_parameters.OldGeometry is Polyline)
                    oldPosition = (ICoordinate)(_parameters.OldGeometry as Polyline).Paths[parameters.PatchIndex].Vertices[_parameters.NodeIndex].Clone();

                if (_parameters.OldGeometry is Polygon)
                    oldPosition = (ICoordinate)(_parameters.OldGeometry as Polygon).Contours[parameters.PatchIndex].Vertices[_parameters.NodeIndex].Clone();

                invertedParameters.NodePosition = oldPosition;
                
            }
            else if (actionType == EditActionType.InsertNode)
            {
                invertedParameters.PatchIndex = parameters.PatchIndex;
                invertedParameters.NodeIndex = parameters.NodeIndex;
            }
            else if (actionType == EditActionType.DeleteNode)
            {
                invertedParameters.PatchIndex = parameters.PatchIndex;
                invertedParameters.NodeIndex = parameters.NodeIndex;

                ICoordinate oldPosition = null;
                if (_parameters.OldGeometry is Polyline)
                    oldPosition = (ICoordinate)(_parameters.OldGeometry as Polyline).Paths[parameters.PatchIndex].Vertices[_parameters.NodeIndex].Clone();

                if (_parameters.OldGeometry is Polygon)
                    oldPosition = (ICoordinate)(_parameters.OldGeometry as Polygon).Contours[parameters.PatchIndex].Vertices[_parameters.NodeIndex].Clone();

                invertedParameters.NodePosition = oldPosition;
            }
            else if (actionType == EditActionType.InsertPatch)
            {
                invertedParameters.PatchIndex = parameters.PatchIndex;
            }
            else if (actionType == EditActionType.DeletePatch)
            {
                invertedParameters.PatchIndex = parameters.PatchIndex;

                List<ICoordinate> patchCoordinates = new List<ICoordinate>();
                if (_parameters.OldGeometry is Polyline)
                    foreach (ICoordinate c in (_parameters.OldGeometry as Polyline).Paths[parameters.PatchIndex].Vertices)
                        patchCoordinates.Add((ICoordinate)c.Clone());

                if (_parameters.OldGeometry is Polygon)
                    foreach (ICoordinate c in (_parameters.OldGeometry as Polygon).Contours[parameters.PatchIndex].Vertices)
                        patchCoordinates.Add((ICoordinate)c.Clone());

                invertedParameters.PatchCoordinates = patchCoordinates;
            }
            else if (actionType == EditActionType.ReplaceGeometry)
            {
                invertedParameters.NewGeometry = parameters.OldGeometry;
            }

            _parameters.OldGeometry = null;
            inverseAction = new EditAction(getInvertedAction(actionType), invertedParameters);
        }
    }

    internal class HistoryEntry
    {
        private EditAction _forwardAction;
        private EditAction _backwardAction;

        public IGeometry Undo(IGeometry geometry)
        {
            return _backwardAction.Do(geometry);
        }

        public IGeometry Redo(IGeometry geometry)
        {
            return _forwardAction.Do(geometry);
        }

        public HistoryEntry(EditAction forwardAction, EditAction backwardAction)
        {
            this._forwardAction = forwardAction;
            this._backwardAction = backwardAction;
        }
    }

    /// <summary>
    /// Represents a history of edit actions.
    /// </summary>
    internal class History
    {
        private LinkedList<HistoryEntry> _entries = new LinkedList<HistoryEntry>();
        private LinkedListNode<HistoryEntry> _currentEntry = null;

        /// <summary>
        /// Gets a value indicating whether an undo is possible.
        /// </summary>
        public bool CanUndo
        {
            get 
            { return _currentEntry != null; }
        }

        /// <summary>
        /// Gets a value indicating whether an redo is possible.
        /// </summary>
        public bool CanRedo
        {
            get
            {
                if (_currentEntry == null)
                    return _entries.First != null;

                return _currentEntry.Next != null; 
            }
        }

        /// <summary>
        /// Undo an action.
        /// </summary>
        /// <param name="g">A MapAround.Geometry.IGeometry instance for undoing an action</param>
        /// <returns>A resulting geometry</returns>
        public IGeometry Undo(IGeometry g)
        {
            if (!CanUndo) return g;

            IGeometry result = _currentEntry.Value.Undo(g);
            _currentEntry = _currentEntry.Previous;
            return result;
        }

        /// <summary>
        /// Redo an action.
        /// </summary>
        /// <param name="g">A MapAround.Geometry.IGeometry instance for redoing an action</param>
        /// <returns>A resulting geometry</returns>
        public IGeometry Redo(IGeometry g)
        {
            if (!CanRedo) return g;

            _currentEntry = _currentEntry == null ? _entries.First : _currentEntry.Next;
            IGeometry result = _currentEntry.Value.Redo(g);
            return result;
        }

        /// <summary>
        /// Clears history.
        /// </summary>
        public void Clear()
        {
            _currentEntry = null;
            _entries.Clear();
        }

        /// <summary>
        /// Gets a number of actions in history.
        /// </summary>
        public int Count
        {
            get 
            { return _entries.Count; } 
        }

        /// <summary>
        /// Gets a number of actions that can be undone.
        /// </summary>
        public int UndoCount
        {
            get
            {
                int result = 0;
                LinkedListNode<HistoryEntry> node = _currentEntry;
                while (node != null)
                {
                    result++;
                    node = node.Previous;
                }
                return result; 
            }
        }

        /// <summary>
        /// Gets a number of actions that can be redone.
        /// </summary>
        public int RedoCount
        {
            get
            {
                if (_currentEntry == null)
                    return 0;

                int result = 0;
                LinkedListNode<HistoryEntry> node = _currentEntry;
                while (node.Next != null)
                {
                    result++;
                    node = node.Next;
                }
                return result;
            }
        }

        internal IGeometry DoAction(HistoryEntry entry, IGeometry g)
        {
            if (_currentEntry != null && _currentEntry.Next != null)
            {
                while(_entries.Last != _currentEntry)
                    _entries.RemoveLast();
            }

            if (_currentEntry == null && _entries.Count > 0)
                _entries.Clear();

            _entries.AddLast(entry);
            _currentEntry = _entries.Last;

            return entry.Redo(g);
        }
    }

    /// <summary>
    /// Defines a method that is used by editor to perform 
    /// map-to-screen coordinate transformation.
    /// </summary>
    /// <param name="c">A coordinate on the map</param>
    /// <returns>A point on the screen</returns>
    public delegate Point MapToClientDelegate(ICoordinate c);

    /// <summary>
    /// Enumerates a types of snapping.
    /// </summary>
    public enum SnapType
    { 
        /// <summary>
        /// No snapping applied
        /// </summary>
        None,

        /// <summary>
        /// The regular grid is used to snap coordinates.
        /// </summary>
        Grid,

        /// <summary>
        /// Points list is used to snap coordinates.
        /// </summary>
        Points,

        /// <summary>
        /// Segments list is used to snap coordinates.
        /// </summary>
        Segments,

        /// <summary>
        /// Segments list and points list is used to snap coordinates.
        /// </summary>
        PointsAndSegments,

        /// <summary>
        /// Custom method is used to snap coordinates.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Defines a method that is used by editor to snap coordinates.
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public delegate ICoordinate SnapMethodDelegate(ICoordinate c);

    /// <summary>
    /// Contains an information about snapping.
    /// </summary>
    public class Snapper
    {
        private SnapType _snapType;
        private double _distance;

        private ICoordinate _gridOrigin;
        private double _gridCellSize;

        private List<ICoordinate> _points = new List<ICoordinate>();
        private List<Segment> _segments = new List<Segment>();

        private SnapMethodDelegate _snapMethod;

        private ICoordinate emptySnapMethod(ICoordinate c)
        {
            return c;
        }

        private ICoordinate gridSnapMethod(ICoordinate c)
        {
            return c;
        }

        private ICoordinate pointsSnapMethod(ICoordinate c)
        {
            return c;
        }

        private ICoordinate segmentsSnapMethod(ICoordinate c)
        {
            return c;
        }

        private ICoordinate pointsAndSegmentsSnapMethod(ICoordinate c)
        {
            return c;
        }

        /// <summary>
        /// Gets or sets a snap distance. This value should 
        /// be used by SnapMethod when it has any meaning.
        /// </summary>
        public double Distance
        {
            get { return _distance; }
            set { _distance = value; }
        }

        /// <summary>
        /// Gets a method that is used to snap coordinates.
        /// </summary>
        public SnapMethodDelegate SnapMethod
        {
            get { return _snapMethod; }
        }

        /// <summary>
        /// Gets a snap type.
        /// </summary>
        public SnapType SnapType
        {
            get { return _snapType; }
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.UI.WinForms.SnapInfo
        /// which describes a no snapping abilities.
        /// </summary>
        public Snapper()
        {
            _snapType = SnapType.None;
            _snapMethod = emptySnapMethod;
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.UI.WinForms.SnapInfo
        /// which describes a snapping to the regular grid.
        /// </summary>
        public Snapper(ICoordinate gridOrigin, double gridCellSize)
        {
            _snapType = SnapType.Grid;
            _snapMethod = this.gridSnapMethod;

            _gridCellSize = gridCellSize;
            _gridOrigin = gridOrigin;
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.UI.WinForms.SnapInfo
        /// which describes a snapping to the points.
        /// </summary>
        public Snapper(IEnumerable<ICoordinate> points)
        {
            _snapType = SnapType.Points;
            _snapMethod = this.pointsSnapMethod;

            foreach(ICoordinate c in points)
                _points.Add((ICoordinate)c.Clone());
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.UI.WinForms.SnapInfo
        /// which describes a snapping to the segments.
        /// </summary>
        public Snapper(IEnumerable<Segment> segments)
        {
            _snapType = SnapType.Segments;
            _snapMethod = this.segmentsSnapMethod;

            foreach (Segment s in segments)
                _segments.Add((Segment)s.Clone());
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.UI.WinForms.SnapInfo
        /// which describes a snapping to the points and the segments.
        /// </summary>
        public Snapper(IEnumerable<ICoordinate> points, IEnumerable<Segment> segments)
        {
            _snapType = SnapType.PointsAndSegments;
            _snapMethod = this.pointsAndSegmentsSnapMethod;

            foreach (ICoordinate c in points)
                _points.Add((ICoordinate)c.Clone());

            foreach (Segment s in segments)
                _segments.Add((Segment)s.Clone());
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.UI.WinForms.SnapInfo
        /// which describes a snapping by the custom method.
        /// </summary>
        public Snapper(IEnumerable<ICoordinate> points, SnapMethodDelegate snapMethod)
        {
            _snapType = SnapType.PointsAndSegments;
            _snapMethod = snapMethod;
        }
    }
}