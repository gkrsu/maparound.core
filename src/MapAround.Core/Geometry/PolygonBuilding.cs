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
** File: PolygonBuilding.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Polygonizing of linear data
**
=============================================================================*/

namespace MapAround.Geometry
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// Poligonizes a linework.
    /// </summary>
    public static class PolygonBuilder
    {
        [ThreadStatic]
        private static List<LinePath> _sourcePaths;

        private static object _dangleLabelTag = new object();
        private static object _cutLabelTag = new object();

        private static void init(IEnumerable<LinePath> paths)
        {
            _sourcePaths = new List<LinePath>();
            foreach (LinePath path in paths)
                _sourcePaths.Add((LinePath)path.Clone());
        }

        private static PlanarGraphEdge getSingleEnabledEdge(PlanarGraphNode node)
        {
            int enabledEdgesCount = 0;
            PlanarGraphEdge enabledEdge = null;
            foreach (PlanarGraphEdge edge in node.IncidentEdges)
                if (edge.Enabled)
                {
                    enabledEdgesCount++;
                    enabledEdge = edge;
                }

            return enabledEdgesCount == 1 ? enabledEdge : null;
        }

        private static void markDangles(PlanarGraph graph)
        {
            Stack<PlanarGraphNode> nodeStack = new Stack<PlanarGraphNode>();

            // find sites with a single incident to and put them on the stack
            foreach (PlanarGraphNode node in graph.Nodes)
                if (node.IncidentEdges.Count == 1)
                    nodeStack.Push(node);

            while (nodeStack.Count > 0)
            {
                PlanarGraphNode node = nodeStack.Pop();
                node.Enabled = false;

                PlanarGraphEdge enabledEdge = getSingleEnabledEdge(node);

                // there was one available edge
                if (enabledEdge != null)
                {
                    enabledEdge.Enabled = false;
                    enabledEdge.Label.Tag = _dangleLabelTag;
                    if (enabledEdge.Node1 == node)
                    {
                        if (getSingleEnabledEdge(enabledEdge.Node2) != null)
                            nodeStack.Push(enabledEdge.Node2);
                    }
                    else
                    {
                        if (getSingleEnabledEdge(enabledEdge.Node1) != null)
                            nodeStack.Push(enabledEdge.Node1);
                    }
                }
            }
        }

        private static void getCutsAndDangles(PlanarGraph graph,
            out IList<Segment> dangles)
        {
            dangles = new List<Segment>();

            foreach (PlanarGraphEdge edge in graph.Edges)
            {
                if (edge.Label.Tag == _dangleLabelTag)
                    dangles.Add(new Segment(edge.Node1.Point, edge.Node2.Point));
            }
        }

        private static void build(
            out IList<Polygon> result,
            out IList<Segment> dangles,
            out IList<Segment> cuts)
        {
            Polyline polyline = new Polyline();
            polyline.Paths = _sourcePaths;

            PlanarGraph graph = PlanarGraph.Build(polyline, null);
            graph.SetElementsEnabledState(true);

            markDangles(graph);
            getCutsAndDangles(graph, out dangles);

            result = graph.BuildFaces(out cuts);
        }

        /// <summary>
        /// Builds polygons from the linework.
        /// </summary>
        /// <param name="sourcePaths">Enumerator of the source line paths</param>
        /// <param name="result">A resulting polygons</param>
        /// <param name="dangles">A list containing dangles</param>
        /// <param name="cuts">A list containing cuts</param>
        public static void BuildPolygons(IEnumerable<LinePath> sourcePaths, 
            out IList<Polygon> result, 
            out IList<Segment> dangles,
            out IList<Segment> cuts)
        {
            try
            {
                init(sourcePaths);
                build(out result, out dangles, out cuts);
            }
            finally
            {
                _sourcePaths = null;
            }
        }
    }
}