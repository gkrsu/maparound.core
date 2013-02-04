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
** File: KDTree.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: kD-tree.
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
    /// Represents a kD-tree.
    /// This spatial index is optimized for fast discarding the empty space.
    /// </summary>
    [Serializable]
    public class KDTree : ISpatialIndex
    {
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>    
        public object Clone()
        {
            KDTree result = new KDTree(this.IndexedSpace);

            result._root = (KDTreeNode)this._root.Clone();

            result._maxDepth = this._maxDepth;
            result._depth = this._maxDepth;
            result._nodeCount = this._nodeCount;

            result._boxSquareThreshold = this._boxSquareThreshold;
            result._minObjectCount = this._minObjectCount;
            result._nodeCount = this._nodeCount;

            return result;
        }

        /// <summary>
        /// Represents a kD-tree node.
        /// </summary>
        [Serializable]
        internal class KDTreeNode : ICloneable
        {
            /// <summary>
            /// Creates a new object that is a copy of the current instance.
            /// </summary>
            /// <returns>A new object that is a copy of this instance</returns> 
            public object Clone()
            {
                KDTreeNode result = new KDTreeNode(this.BoundingBox, this._parent);

                foreach (IIndexable s in _objects)
                    result._objects.Add((IIndexable)s.Clone());

                if (_child0 != null)
                    result._child0 = (KDTreeNode)this._child0.Clone();
                if (_child1 != null)
                    result._child1 = (KDTreeNode)this._child1.Clone();

                return result;
            }

            private KDTreeNode _child0 = null;
            private KDTreeNode _child1 = null;

            private BoundingRectangle _boundingBox;

            private KDTree _parent = null;
            private List<IIndexable> _objects = new List<IIndexable>();

            private enum axis { X, Y };

            private void splitX(BoundingRectangle box, out BoundingRectangle box1, out BoundingRectangle box2)
            {
                // разбиваем вдоль оси X
                box1 = new BoundingRectangle(box.MinX, box.MinY, box.MaxX, box.Center().Y);
                box2 = new BoundingRectangle(box.MinX, box.Center().Y, box.MaxX, box.MaxY);
            }

            private void splitY(BoundingRectangle box, out BoundingRectangle box1, out BoundingRectangle box2)
            {
                // разбиваем вдоль оси Y
                box1 = new BoundingRectangle(box.MinX, box.MinY, box.Center().X, box.MaxY);
                box2 = new BoundingRectangle(box.Center().X, box.MinY, box.MaxX, box.MaxY);
            }

            private axis maxAxis(BoundingRectangle box)
            {
                return (box.Width > box.Height) ? axis.X : axis.Y;
            }

            private void splitMaxAxis(BoundingRectangle box, out BoundingRectangle box1, out BoundingRectangle box2)
            {
                if (maxAxis(box) == axis.X)
                    splitY(box, out box1, out box2);
                else
                    splitX(box, out box1, out box2);
            }

            private void splitMinAxis(BoundingRectangle box, out BoundingRectangle box1, out BoundingRectangle box2)
            {
                if (maxAxis(box) == axis.Y)
                    splitY(box, out box1, out box2);
                else
                    splitX(box, out box1, out box2);
            }

            /// <summary>
            /// Gets or sets a boundeing rectangle.
            /// </summary>
            internal BoundingRectangle BoundingBox
            {
                get { return _boundingBox; }
                set { _boundingBox = value; }
            }

            /// <summary>
            /// Gets or sets first child node.
            /// </summary>
            internal KDTreeNode Child0
            {
                get { return _child0; }
                set { _child0 = value; }
            }

            /// <summary>
            /// Gets or sets second child node.
            /// </summary>
            internal KDTreeNode Child1
            {
                get { return _child1; }
                set { _child1 = value; }
            }

            /// <summary>
            /// Gets or sets a list containing 
            /// the objects of this node.
            /// </summary>
            internal List<IIndexable> Objects
            {
                get { return _objects; }
                set { _objects = value; }
            }

            /// <summary>
            /// Removes an object from this node or from
            /// one of its child nodes.
            /// </summary>
            internal bool Remove(IIndexable obj)
            {
                if (_child0 != null)
                {
                    if (_child0._boundingBox.ContainsRectangle(obj.BoundingRectangle))
                        return _child0.Remove(obj);

                    if (_child1._boundingBox.ContainsRectangle(obj.BoundingRectangle))
                        return _child1.Remove(obj);
                }

                if (_objects.Contains(obj))
                {
                    _objects.Remove(obj);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Adds an object to this node 
            /// or one of its childs.
            /// </summary>
            internal void Insert(IIndexable obj, int branchDepth)
            {
                // возможно глубина текущей ветки 
                // уже больше глубины индекса
                if (branchDepth > _parent.Depth)
                    _parent._depth++;

                // возможно мы достигли: 
                // 1. максимально допустимой глубины индекса 
                // 2. порогового значения площади ячейки узла
                // при этом создавать дочерние узлы уже нельзя
                if (_parent.Depth >= _parent.MaxDepth ||
                    _parent._boxSquareThreshold > _boundingBox.Width * _boundingBox.Height)
                {
                    _objects.Add(obj);
                    return;
                }

                BoundingRectangle box0, box1;
                bool hasChilds = _child0 != null && _child1 != null;

                // усли у узла еще нет дочерних узлов, вычисляем их ограничивающие прямоугольники,
                // иначе берем их из дочерних узлов
                if (!hasChilds)
                    splitMaxAxis(_boundingBox, out box0, out box1);
                else
                {
                    box0 = _child0._boundingBox;
                    box1 = _child1._boundingBox;
                }

                bool f0 = box0.Intersects(obj.BoundingRectangle);
                bool f1 = box1.Intersects(obj.BoundingRectangle);

                if (f0 && f1)
                {
                    // дочерние узлы уже были созданы, 
                    // вариантов разбиения больше нет
                    if (hasChilds)
                    {
                        _objects.Add(obj);
                        return;
                    }

                    // возможно при разбиении вдоль другой оси ситуация не столь плачевна
                    splitMinAxis(_boundingBox, out box0, out box1);

                    f0 = box0.Intersects(obj.BoundingRectangle);
                    f1 = box1.Intersects(obj.BoundingRectangle);
                    if (f0 && f1)
                    {
                        // добавляемый объект пересекает оба ограничивающих прямоугольника в обоих
                        // вариантах разбиения, значит мы не можем добавить его в дочерние узлы, 
                        // добавляем в текущий узел
                        _objects.Add(obj);
                        return;
                    }
                }

                // если дочерние узлы еще не были созданы, создаем их
                if (_child0 == null && _child1 == null)
                {
                    _child0 = new KDTreeNode(box0, _parent);
                    _child1 = new KDTreeNode(box1, _parent);
                }

                if (f0)
                {
                    _child0.Insert(obj, branchDepth + 1);
                    return;
                }

                if (f1)
                {
                    _child1.Insert(obj, branchDepth + 1);
                    return;
                }

#if DEBUG
                Debug.Assert(false, "This statement should never be executed");
#endif
            }

            internal void addAllObjectsRecursively<T>(IList<T> objects)
                where T : IIndexable
            {
                foreach (T obj in _objects)
                    objects.Add(obj);

                if (_child0 != null && _child1 != null)
                {
                    _child0.addAllObjectsRecursively(objects);
                    _child1.addAllObjectsRecursively(objects);
                }
            }

            /// <summary>
            /// Adds objects, which bounding rectangles intersect 
            /// specified rectangle, to the list.
            /// </summary>
            /// <param name="box">A bounding rectangle defining queryable area</param>
            /// <param name="objects">A list for adding objects</param>
            internal void QueryObjectsInRectangle<T>(BoundingRectangle box, IList<T> objects)
                where T : IIndexable
            {
                // возможно ячейка узла лежит внутри запрашиваемой области,
                // в этом случае мы должны добавить все объекты дочерних узлов
                // без выполнения проверок на пересечения
                if (box.ContainsRectangle(BoundingBox))
                {
                    addAllObjectsRecursively(objects);
                    return;
                }

                // предпринимаем действия по добавлению объектов в список только
                // в том случае, если ограничивающие прямоугольники пересеклись
                if (box.Intersects(BoundingBox))
                {
                    foreach (T obj in _objects)
                        if (box.Intersects(obj.BoundingRectangle))
                            objects.Add(obj);

                    if (_child0 != null && _child1 != null)
                    {
                        _child0.QueryObjectsInRectangle(box, objects);
                        _child1.QueryObjectsInRectangle(box, objects);
                    }
                }
            }

            /// <summary>
            /// Adds objects, which bounding rectangles contains 
            /// specified point, to the list.
            /// </summary>
            /// <param name="point">A point</param>
            /// <param name="objects">A list for adding objects</param>
            internal void QueryObjectsContainingPoint<T>(ICoordinate point, IList<T> objects)
                where T : IIndexable
            {
                if (BoundingBox.ContainsPoint(point))
                {
                    foreach (T obj in _objects)
                        if (obj.BoundingRectangle.ContainsPoint(point))
                            objects.Add(obj);

                    if (_child0 != null && _child1 != null)
                    {
                        _child0.QueryObjectsContainingPoint(point, objects);
                        _child1.QueryObjectsContainingPoint(point, objects);
                    }
                }
            }

            /// <summary>
            /// Removes all child nodes.
            /// </summary>
            internal void ClearChildren()
            {
                if (_child0 != null && _child1 != null)
                {
                    _child0.ClearChildren();
                    _child1.ClearChildren();
                    _parent._nodeCount -= 2;
                }

                _child0 = null;
                _child1 = null;
            }

            /// <summary>
            /// Computes the cost of horizontal split.
            /// </summary>
            private double getSplitCostHorizontal<T>(List<T> objects, ref BoundingRectangle box0, ref BoundingRectangle box1)
                where T : IIndexable
            {
                double area0 = box0.Width * box0.Height;
                double area1 = box1.Width * box1.Height;

                int box0ObjectCount = 0, box1ObjectCount = 0;
                foreach (T obj in objects)
                    if (obj.BoundingRectangle.MaxY <= box0.MaxY)
                        box0ObjectCount++;
                    else
                    {
                        if (obj.BoundingRectangle.MinY >= box1.MinY)
                            box1ObjectCount++;
                    }

                return area0 * box0ObjectCount + area1 * box1ObjectCount;
            }

            /// <summary>
            /// Computes the cost of vertical split.
            /// </summary>
            private double getSplitCostVertical<T>(List<T> objects, BoundingRectangle box0, BoundingRectangle box1)
                where T : IIndexable
            {
                double area0 = box0.Width * box0.Height;
                double area1 = box1.Width * box1.Height;

                int box0ObjectCount = 0, box1ObjectCount = 0;
                foreach (T obj in objects)
                    if (obj.BoundingRectangle.MaxX <= box0.MaxX)
                        box0ObjectCount++;
                    else
                    {
                        if (obj.BoundingRectangle.MinX >= box1.MinX)
                            box1ObjectCount++;
                    }

                return area0 * box0ObjectCount + area1 * box1ObjectCount;
            }

            private void splitX(BoundingRectangle box, double x, out BoundingRectangle box0, out BoundingRectangle box1)
            {
                box0 = new BoundingRectangle(box.MinX, box.MinY, x, box.MaxY);
                box1 = new BoundingRectangle(x, box.MinY, box.MaxX, box.MaxY);
            }

            private void splitY(BoundingRectangle box, double y, out BoundingRectangle box0, out BoundingRectangle box1)
            {
                //box0 = new BoundingRectangle(box.MinX, box.MinY, box.MaxY, y);
                box0 = new BoundingRectangle(box.MinX, box.MinY, box.MaxX, y);
                box1 = new BoundingRectangle(box.MinX, y, box.MaxX, box.MaxY);
            }

            /// <summary>
            /// Builds an index for the specified objects.
            /// </summary>
            internal void BuildUnbalanced<T>(List<T> objects, int currentDepth)
                where T : IIndexable
            {
                if (currentDepth > _parent.Depth)
                    _parent._depth = currentDepth;

                // Возможно мы достигли: 
                // 1. максимально допустимой глубины индекса 
                // 2. порогового значения площади ячейки узла
                // или кол-во объектов переданных для индексирования
                // не превысило минимального.
                // При этом создавать дочерние узлы уже нельзя
                if (_parent.Depth >= _parent.MaxDepth ||
                    _parent._boxSquareThreshold > _boundingBox.Width * _boundingBox.Height ||
                    _parent.MinObjectCount >= objects.Count)
                {
                    foreach (T obj in objects)
                        _objects.Add(obj);

                    objects.Clear();
                    return;
                }

                List<IIndexable> objects0 = new List<IIndexable>();
                List<IIndexable> objects1 = new List<IIndexable>();

                double minCost = double.MaxValue;

                BoundingRectangle box0, box1;
                BoundingRectangle minCostBox0 = _parent._indexedSpace,
                                  minCostBox1 = _parent._indexedSpace;

                #region Вычисление разбиения с минимальной стоимостью: стоимость = кол-во объекты * площадь

                for (int i = 1; i < KDTree.DividingGridRowCount; i++)
                {
                    double x = _boundingBox.MinX + _boundingBox.Width / KDTree.DividingGridRowCount * i;
                    splitX(_boundingBox, x, out box0, out box1);

                    double cost = getSplitCostVertical(objects, box0, box1);

                    if (cost < minCost)
                    {
                        minCost = cost;
                        minCostBox0 = box0;
                        minCostBox1 = box1;
                    }

                    double y = _boundingBox.MinY + _boundingBox.Height / KDTree.DividingGridRowCount * i;

                    splitY(_boundingBox, y, out box0, out box1);

                    cost = getSplitCostHorizontal(objects, ref box0, ref box1);

                    if (cost < minCost)
                    {
                        minCost = cost;
                        minCostBox0 = box0;
                        minCostBox1 = box1;
                    }
                }

                #endregion

                // разбиваем исходный список на 3 множества:
                // objects0 - объекты индексируемые в дочернем узле _child0
                // objects1 - объекты индексируемые в дочернем узле _child1
                // _objects - объекты этого узла
                foreach (T obj in objects)
                {
                    if (minCostBox0.ContainsRectangle(obj.BoundingRectangle))
                        objects0.Add(obj);
                    else
                    {
                        if (minCostBox1.ContainsRectangle(obj.BoundingRectangle))
                            objects1.Add(obj);
                        else
                            _objects.Add(obj);
                    }
                }

                objects.Clear();

                if (objects0.Count > 0 || objects1.Count > 0)
                {
                    _child0 = new KDTreeNode(minCostBox0, _parent);
                    _child1 = new KDTreeNode(minCostBox1, _parent);
                    _child0.BuildUnbalanced(objects0, currentDepth + 1);
                    _child1.BuildUnbalanced(objects1, currentDepth + 1);
                }
            }

            internal KDTreeNode(BoundingRectangle boundingBox, KDTree parent)
            {
                _boundingBox = boundingBox;
                _parent = parent;
                _parent._nodeCount++;
            }
        }

        private BoundingRectangle _indexedSpace;
        private KDTreeNode _root;
        private int _maxDepth = 10;
        private int _depth = 0;
        private int _nodeCount = 0;

        private double _boxSquareThreshold = 10;
        private int _minObjectCount = 4;

        /// <summary>
        /// The number of the possible divisions of cell.
        /// </summary>
        public static readonly int DividingGridRowCount = 6;

        /// <summary>
        /// Gets or sets a value defining 
        /// minimum object number in a cell.
        /// <para>
        /// This value is used only by Build(...) methods.
        /// Ignored in other cases.
        /// </para>
        /// </summary>
        public int MinObjectCount
        {
            get { return _minObjectCount; }
            set { _minObjectCount = value; }
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
        /// Gets or sets a value defining a maximum depth of this kD-tree.
        /// </summary>
        public int MaxDepth
        {
            get { return _maxDepth; }
            set { _maxDepth = value; }
        }

        /// <summary>
        /// Gets a depth of this kD-tree.
        /// </summary>
        public int Depth
        {
            get { return _depth; }
        }

        /// <summary>
        /// Gets a number of nodes of kD-tree.
        /// </summary>
        public int NodeCount
        {
            get { return _nodeCount; }
        }

        /// <summary>
        /// Gets a bounding rectangle defining the extent of spatial index.
        /// </summary>
        public BoundingRectangle IndexedSpace
        {
            get
            { return _indexedSpace; }
        }

        /// <summary>
        /// Inserts an object into index.
        /// </summary>
        /// <param name="obj">An object to insert</param>
        public void Insert<T>(T obj)
            where T : IIndexable
        {
            if (!IndexedSpace.ContainsRectangle(obj.BoundingRectangle))
                throw new ArgumentException("Bounding rectangle of the object is outside of the indexed space. Need to rebuild index.", "obj");

            _root.Insert(obj, 1);
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
        /// Removes all objects from index.
        /// </summary>
        public void Clear()
        {
            _root.ClearChildren();
        }

        /// <summary>
        /// Built an index for a specified objects.
        /// <para>
        /// This method creates a more optimal partitioning of the space 
        /// than the one which is obtained by sequentially adding objects 
        /// to the index. The resulting tree is unbalanced.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// The algorithm is optimized for fast discarding the empty space. 
        /// The criterion for the optimal partition is the minimum of 
        /// (number_of_objects_in_1st_area * square_of_1st_area + number_of_objects_in_2nd_area * square_of_2nd_area). 
        /// The number of possible partitions defined into <see cref="KDTree.DividingGridRowCount"/>.
        /// </para>
        /// </remarks>
        /// <param name="objects">Enumerator of objects for indexing</param>
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

            _root.BuildUnbalanced(list, 1);
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
        /// Initializes a new instance of MapAround.Indexing.KDTree.
        /// </summary>
        /// <param name="indexedSpace">A bounding rectangle defining the extent of spatial index</param>
        public KDTree(BoundingRectangle indexedSpace)
        {
            _indexedSpace = indexedSpace;
            _root = new KDTreeNode(indexedSpace, this);
        }
    }
}