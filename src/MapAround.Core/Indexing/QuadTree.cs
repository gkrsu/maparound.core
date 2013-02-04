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
** File: QuadTree.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Quad tree.
**
=============================================================================*/

namespace MapAround.Indexing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using MapAround.Mapping;
    using MapAround.Geometry;

    /// <summary>
    /// Represents a Quad tree.
    /// This spatial index is effective for uniform distribution of objects 
    /// of a small size compared to the indexed space.
    /// </summary>
    [Serializable]
    public class QuadTree : ISpatialIndex
    {
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>    
        public object Clone()
        {
            QuadTree result = new QuadTree(this.IndexedSpace);

            result._root = (QuadTreeNode)this._root.Clone();

            result._maxDepth = this._maxDepth;
            result._depth = this._maxDepth;
            result._nodeCount = this._nodeCount;

            result._boxSquareThreshold = this._boxSquareThreshold;
            result._minObjectCount = this._minObjectCount;
            result._nodeCount = this._nodeCount;

            return result;
        }

        /// <summary>
        /// Represents positions of the quad tree node.
        /// </summary>
        internal enum QuadTreeNodePosition
        {
            Undefined,
            LeftUp,
            RightUp,
            RightDown,
            LeftDown
        }

        private BoundingRectangle _indexedSpace = null;
        private int _nodeCount = 0;
        private double _boxSquareThreshold = 10;
        private int _maxDepth = 12;
        private int _depth = 0;
        private int _minObjectCount = 200;

        /// <summary>
        /// Gets a depth of this QuadTree.
        /// </summary>
        public int Depth
        {
            get { return _depth; }
        }

        /// <summary>
        /// Gets a number of nodes of QuadTree.
        /// </summary>
        public int NodeCount
        {
            get { return _nodeCount; }
        }

        /// <summary>
        /// Gets or sets a value defining a maximum depth of this QuadTree.
        /// </summary>
        public int MaxDepth
        {
            get { return _maxDepth; }
            set { _maxDepth = value; }
        }

        /// <summary>
        /// Gets or sets a threshold 
        /// value of a cell area.
        /// </summary>
        public double BoxSquareThreshold
        {
            get { return _boxSquareThreshold; }
            set { _boxSquareThreshold = value; }
        }

        /// <summary>
        /// Gets a bounding rectangle defining the extent of spatial index.
        /// </summary>
        public BoundingRectangle IndexedSpace
        {
            get { return _indexedSpace; }
        }

        /// <summary>
        /// Gets or sets a value defining 
        /// minimum object number in a cell.
        /// </summary>
        public int MinObjectCount
        {
            get { return _minObjectCount; }
            set { _minObjectCount = value; }
        }

        /// <summary>
        /// Represents a QuadTree node.
        /// </summary>
        [Serializable]
        private class QuadTreeNode : ICloneable
        {
            /// <summary>
            /// Creates a new object that is a copy of the current instance.
            /// </summary>
            /// <returns>A new object that is a copy of this instance</returns> 
            public object Clone()
            {
                QuadTreeNode result = new QuadTreeNode(this._owner, this._fullRectangle);
                result._position = this._position;
                result._parent = this._parent;

                foreach (IIndexable s in _objects)
                    result._objects.Add((IIndexable)s.Clone());

                if (HasChildren)
                {
                    if (_leftUpChild != null)
                        result._leftUpChild = (QuadTreeNode)this._leftUpChild.Clone();

                    if (_rightUpChild != null)
                        result._rightUpChild = (QuadTreeNode)this._rightUpChild.Clone();

                    if (_rightDownChild != null)
                        result._rightDownChild = (QuadTreeNode)this._rightDownChild.Clone();

                    if (_leftDownChild != null)
                        result._leftDownChild = (QuadTreeNode)this._leftDownChild.Clone();
                }

                return result;
            }

            private QuadTreeNode _parent = null;
            private BoundingRectangle _geometriesRectangle = null;
            private BoundingRectangle _fullRectangle = null;

            private QuadTree _owner = null;

            private List<IIndexable> _objects = new List<IIndexable>();

            private QuadTreeNodePosition _position = QuadTreeNodePosition.Undefined;

            private QuadTreeNode _leftUpChild = null;
            private QuadTreeNode _rightUpChild = null;
            private QuadTreeNode _rightDownChild = null;
            private QuadTreeNode _leftDownChild = null;

            /// <summary>
            /// Gets a value indicating whether this node has child nodes.
            /// </summary>
            internal bool HasChildren
            {
                get
                {
                    return _leftUpChild != null ||
                           _rightUpChild != null ||
                           _rightDownChild != null ||
                           _leftDownChild != null;
                }
            }

            private void splitAndRebuild(int branchChild)
            {
                List<IIndexable> tempObjects = _objects;
                _objects = new List<IIndexable>();
                _geometriesRectangle = null;

                ICoordinate center = this._fullRectangle.Center();

                foreach (IIndexable obj in tempObjects)
                {
                    if (!canAddingToChildCell(obj.BoundingRectangle))
                        forceInsert(obj);
                    else
                        insertIntoChild(obj, branchChild);
                }
            }

            private void insertIntoChild(IIndexable obj, int branchDepth)
            {
                ICoordinate center = this._fullRectangle.Center();

                if (center.X >= obj.BoundingRectangle.MaxX && center.Y <= obj.BoundingRectangle.MinY)
                {
                    if (_leftUpChild == null)
                        _leftUpChild =
                            new QuadTreeNode(_owner, new BoundingRectangle(_fullRectangle.MinX, center.Y, center.X, _fullRectangle.MaxY));
                    _leftUpChild.Insert(obj, branchDepth + 1);
                }

                if (center.X <= obj.BoundingRectangle.MinX && center.Y <= obj.BoundingRectangle.MinY)
                {
                    if (_rightUpChild == null)
                        _rightUpChild =
                            new QuadTreeNode(_owner, new BoundingRectangle(center, _fullRectangle.Max));
                    _rightUpChild.Insert(obj, branchDepth + 1);
                }

                if (center.X <= obj.BoundingRectangle.MinX && center.Y >= obj.BoundingRectangle.MaxY)
                {
                    if (_rightDownChild == null)
                        _rightDownChild =
                            new QuadTreeNode(_owner, new BoundingRectangle(center.X, _fullRectangle.MinY, _fullRectangle.MaxX, center.Y));
                    _rightDownChild.Insert(obj, branchDepth + 1);
                }

                if (center.X >= obj.BoundingRectangle.MaxX && center.Y >= obj.BoundingRectangle.MaxY)
                {
                    if (_leftDownChild == null)
                        _leftDownChild =
                            new QuadTreeNode(_owner, new BoundingRectangle(_fullRectangle.Min, center));
                    _leftDownChild.Insert(obj, branchDepth + 1);
                }
            }

            private void forceInsert(IIndexable obj)
            {
                _objects.Add(obj);

                if (_geometriesRectangle == null)
                    _geometriesRectangle = (BoundingRectangle)obj.BoundingRectangle.Clone();
                else
                    _geometriesRectangle.Join(obj.BoundingRectangle);
            }

            private bool canAddingToChildCell(BoundingRectangle rectangle)
            {
                ICoordinate center = _fullRectangle.Center();
                if (rectangle.ContainsPoint(center))
                    return false;

                if (rectangle.MinY > center.Y)
                {
                    if (rectangle.MinX < center.X && rectangle.MaxX > center.X)
                        return false;

                    return true;
                }

                if (rectangle.MinX > center.X)
                {
                    if (rectangle.MinY < center.Y && rectangle.MaxY > center.Y)
                        return false;

                    return true;
                }

                if (rectangle.MaxY < center.Y)
                {
                    if (rectangle.MinX < center.X && rectangle.MaxX > center.X)
                        return false;

                    return true;
                }

                if (rectangle.MaxX < center.X)
                {
                    if (rectangle.MinY < center.Y && rectangle.MaxY > center.Y)
                        return false;

                    return true;
                }

                return false;
            }

            internal void Insert(IIndexable obj, int branchDepth)
            {
                if (!_fullRectangle.ContainsRectangle(obj.BoundingRectangle))
                    throw new ArgumentException("Bounding rectangle of the indexed object exceeds the node bounding rectangle", "obj");

                // возможно глубина текущей ветки 
                // уже больше глубины индекса
                if (branchDepth > _owner.Depth)
                    _owner._depth++;

                if (HasChildren)
                {
                    if (!canAddingToChildCell(obj.BoundingRectangle))
                        forceInsert(obj);
                    else
                        insertIntoChild(obj, branchDepth);
                }
                else
                {
                    if (_owner._minObjectCount < _objects.Count)
                    {
                        forceInsert(obj);
                    }
                    else
                    {
                        forceInsert(obj);

                        // ячейку можно разбивать, если ее площадь превосходит
                        // порог площади ячейки и текущая глубина ветви меньше предельной
                        if (_owner.BoxSquareThreshold < _fullRectangle.Width * _fullRectangle.Height &&
                            _owner.MaxDepth > branchDepth)
                            splitAndRebuild(branchDepth);
                    }
                }
            }

            /// <summary>
            /// Removes an object from this node 
            /// or from one of child nodes.
            /// </summary>
            internal bool Remove(IIndexable obj)
            {
                if (HasChildren)
                {
                    if (_leftUpChild != null)
                        if (_leftUpChild._fullRectangle.ContainsRectangle(obj.BoundingRectangle))
                            return _leftUpChild.Remove(obj);

                    if (_rightUpChild != null)
                        if (_rightUpChild._fullRectangle.ContainsRectangle(obj.BoundingRectangle))
                            return _rightUpChild.Remove(obj);

                    if (_rightDownChild != null)
                        if (_rightDownChild._fullRectangle.ContainsRectangle(obj.BoundingRectangle))
                            return _rightDownChild.Remove(obj);

                    if (_leftDownChild != null)
                        if (_leftDownChild._fullRectangle.ContainsRectangle(obj.BoundingRectangle))
                            return _leftDownChild.Remove(obj);
                }

                if (_objects.Contains(obj))
                {
                    _objects.Remove(obj);

                    if (this._geometriesRectangle.MinX == obj.BoundingRectangle.MinX ||
                        this._geometriesRectangle.MinY == obj.BoundingRectangle.MinY ||
                        this._geometriesRectangle.MaxX == obj.BoundingRectangle.MaxX ||
                        this._geometriesRectangle.MaxY == obj.BoundingRectangle.MaxY)
                        refreshGeometriesRectangle();

                    if (_objects.Count == 0)
                    {
                        // нужно удалить саму ячейку
                    }

                    return true;
                }

                return false;
            }

            private void refreshGeometriesRectangle()
            {
                if (this._objects.Count == 0)
                    return;

                this._geometriesRectangle = new BoundingRectangle();
                foreach (IIndexable obj in _objects)
                    this._geometriesRectangle.Join(obj.BoundingRectangle);
            }

            internal QuadTreeNode(QuadTree owner, BoundingRectangle boundingRectangle)
            {
                _owner = owner;
                _owner._nodeCount++;

                _fullRectangle = (BoundingRectangle)boundingRectangle.Clone();
                _position = QuadTreeNodePosition.Undefined;
            }

            internal QuadTreeNode(QuadTree owner, QuadTreeNode parent, QuadTreeNodePosition position)
            {
                _owner = owner;
                _owner._nodeCount++;

                _parent = parent;
                _position = position;
            }

            /// <summary>
            /// Builds an index for a specified objects.
            /// </summary>
            internal void Build<T>(List<T> objects)
                where T : IIndexable
            {
                foreach (T obj in objects)
                    this.Insert(obj, 1);
            }

            /// <summary>
            /// Removes all child nodes.
            /// </summary>
            internal void ClearChildren()
            {
                if (_leftUpChild != null)
                {
                    _leftUpChild.ClearChildren();
                    _owner._nodeCount--;
                }

                if (_rightUpChild != null)
                {
                    _rightUpChild.ClearChildren();
                    _owner._nodeCount--;
                }

                if (_leftDownChild != null)
                {
                    _leftDownChild.ClearChildren();
                    _owner._nodeCount--;
                }

                if (_rightDownChild != null)
                {
                    _rightDownChild.ClearChildren();
                    _owner._nodeCount--;
                }

                _leftUpChild = null;
                _rightUpChild = null;
                _leftDownChild = null;
                _rightDownChild = null;
            }

            internal void addAllObjectsRecursively<T>(IList<T> objects)
                where T : IIndexable
            {
                foreach (IIndexable obj in _objects)
                    objects.Add((T)obj);

                if (_leftUpChild != null)
                    _leftUpChild.addAllObjectsRecursively(objects);

                if (_rightUpChild != null)
                    _rightUpChild.addAllObjectsRecursively(objects);

                if (_rightDownChild != null)
                    _rightDownChild.addAllObjectsRecursively(objects);

                if (_leftDownChild != null)
                    _leftDownChild.addAllObjectsRecursively(objects);
            }

            internal void QueryObjectsInRectangle<T>(BoundingRectangle box, IList<T> objects)
                where T : IIndexable
            {
                // если прямоугольник ячейки не пересекается
                // с запрашиваемым прямоугольником - считать нечего
                if (!box.Intersects(_fullRectangle))
                    return;

                // если прямоугольник ячейки целиком содержится в 
                // запрашиваемом прямоугольнике - добавляем все объекты рекурсивно
                if (box.ContainsRectangle(this._fullRectangle))
                {
                    addAllObjectsRecursively(objects);
                    return;
                }

                // если прямоугольник объектов ячейки целиком содержится в 
                // запрашиваемом прямоугольнике - добавляем все объекты этой ячейки
                if (_geometriesRectangle != null && box.ContainsRectangle(_geometriesRectangle))
                {
                    foreach (IIndexable obj in _objects)
                        objects.Add((T)obj);
                }
                else
                {
                    // иначе выполняем проверки на пересечение 
                    // прямоугольников для каждого объекта
                    foreach (IIndexable obj in _objects)
                        if (box.Intersects(obj.BoundingRectangle))
                            objects.Add((T)obj);
                }

                // если дочерние узлы отсутствуют, 
                // дальше считать нечего
                if (!HasChildren)
                    return;

                // разбираемся с детьми
                if (_leftUpChild != null)
                    _leftUpChild.QueryObjectsInRectangle(box, objects);

                if (_rightUpChild != null)
                    _rightUpChild.QueryObjectsInRectangle(box, objects);

                if (_rightDownChild != null)
                    _rightDownChild.QueryObjectsInRectangle(box, objects);

                if (_leftDownChild != null)
                    _leftDownChild.QueryObjectsInRectangle(box, objects);
            }

            /// <summary>
            /// Adds objects, which bounding rectangles contains 
            /// specified point, to the list.
            /// </summary>
            internal void QueryObjectsContainingPoint<T>(ICoordinate point, IList<T> objects)
                where T : IIndexable
            {
                if (_fullRectangle.ContainsPoint(point))
                {
                    foreach (T obj in _objects)
                        if (obj.BoundingRectangle.ContainsPoint(point))
                            objects.Add(obj);

                    if (!HasChildren)
                        return;

                    if (_leftUpChild != null)
                        _leftUpChild.QueryObjectsContainingPoint(point, objects);

                    if (_rightUpChild != null)
                        _rightUpChild.QueryObjectsContainingPoint(point, objects);

                    if (_rightDownChild != null)
                        _rightDownChild.QueryObjectsContainingPoint(point, objects);

                    if (_leftDownChild != null)
                        _leftDownChild.QueryObjectsContainingPoint(point, objects);
                }
            }
        }


        private QuadTreeNode _root = null;

        private BoundingRectangle getMinBoundingQuad(BoundingRectangle rectangle)
        {
            ICoordinate center = rectangle.Center();
            double maxHalfSize = Math.Max(rectangle.Width, rectangle.Height) / 2;

            BoundingRectangle result =
                new BoundingRectangle(center.X - maxHalfSize, center.Y - maxHalfSize,
                                      center.X + maxHalfSize, center.Y + maxHalfSize);

            result.Join(rectangle);
            return result;
        }

        /// <summary>
        /// Строит индекс для списка объектов.
        /// </summary>
        /// <param name="objects">Перечислитель объектов, для которых требуется построить индекс</param>
        public void Build<T>(IEnumerable<T> objects)
            where T : IIndexable
        {
            Clear();

            List<T> list = new List<T>();
            foreach (T obj in objects)
                if (IndexedSpace.ContainsRectangle(obj.BoundingRectangle))
                    list.Add(obj);
                else
                    throw new ArgumentException("At least one object goes beyond the indexed space", "objects");

            _root.Build(list);
        }

        /// <summary>
        /// Removes all child nodes.
        /// </summary>
        public void Clear()
        {
            _root.ClearChildren();
        }

        /// <summary>
        /// Inserts an object into index.
        /// </summary>
        /// <param name="obj">An object to insert</param>
        public void Insert<T>(T obj)
            where T : IIndexable
        {
            if (!IndexedSpace.ContainsRectangle(obj.BoundingRectangle))
                throw new ArgumentException("Bounding rectangle of the object outside the indexed space. Need to rebuild index.", "obj");

            _root.Insert(obj, 1);
        }

        /// <summary>
        /// Adds objects, which bounding rectangles intersect 
        /// specified rectangle, to the list.
        /// </summary>
        /// <param name="box">A bounding rectangle defining queryable area</param>
        /// <param name="objects">A list for adding objects</param>
        public void QueryObjectsInRectangle<T>(BoundingRectangle box, IList<T> objects)
            where T : IIndexable
        {
            if (box.IsEmpty())
                return;

            _root.QueryObjectsInRectangle(box, objects);
        }

        /// <summary>
        /// Adds objects, which bounding rectangles contains 
        /// specified point, to the list.
        /// </summary>
        /// <param name="point">A point</param>
        /// <param name="objects">A list for adding objects</param>
        public void QueryObjectsContainingPoint<T>(ICoordinate point, IList<T> objects)
            where T : IIndexable
        {
            _root.QueryObjectsContainingPoint(point, objects);
        }

        /// <summary>
        /// Removes an object from index.
        /// </summary>
        /// <param name="obj">An object to remove</param>
        /// <returns>true, if the object was removed, false otherwise</returns>
        public bool Remove<T>(T obj)
            where T : IIndexable
        {
            return _root.Remove(obj);
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Indexing.QuadTree.
        /// </summary>
        /// <param name="indexedSpace">A bounding rectangle defining the extent of spatial index</param>
        public QuadTree(BoundingRectangle indexedSpace)
        {
            if (indexedSpace == null)
                throw new ArgumentNullException("indexedSpace");

            if (indexedSpace.IsEmpty())
                throw new ArgumentException("Indexed space should not be empty", "indexedSpace");

            _indexedSpace = indexedSpace;
            _root = new QuadTreeNode(this, getMinBoundingQuad(indexedSpace));
        }
    }
}