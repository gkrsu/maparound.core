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
** Файл: Scaning.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Назначение: Классы и интерфейсы для горизонтального сканирования.
** (код выглядит странно, оптимизация производительности заставила
**  писать так)
**
=============================================================================*/

namespace MapAround.Rendering
{
    using System;

    using MapAround.Geometry;
    using System.Collections.Generic;

    /// <summary>
    /// Интерфейс объекта поддерживающего горизонтальное сканирование.
    /// </summary>
    public interface IScannable : ICloneable
    {
        /// <summary>
        /// Уведомляет объект о начале процесса сканирования.
        /// </summary>
        /// <param name="minX">Минимальная координата X области сканирования</param>
        /// <param name="maxX">Максимальная координата X области сканирования</param>
        /// <param name="minY">Минимальная координата Y области сканирования</param>
        /// <param name="maxY">Максимальная координата Y области сканирования</param>
        /// <param name="orientation">Направление сканирования</param>
        void InitScaning(int minX, int maxX, int minY, int maxY, Orientation orientation);

        /// <summary>
        /// Ограничивающий прямоугольник объекта.
        /// </summary>
        BoundingRectangle BoundingBox { get; }

        /// <summary>
        /// Вычисляет пересечения границ объекта с горизонтальным отрезком.
        /// </summary>
        /// <param name="scanY">Координата Y горизонтального отрезка</param>
        /// <param name="intersections">Пересечения</param>
        void ComputeHorizontalIntersections(float scanY, out float[] intersections);

        /// <summary>
        /// Вычисляет пересечения границ объекта с вертикальным отрезком.
        /// </summary>
        /// <param name="scanX">Координата X вертикального отрезка</param>
        /// <param name="intersections">Пересечения</param>
        void ComputeVerticalIntersections(float scanX, out float[] intersections);
    }

    /// <summary>
    /// Режим заполнения внутренних областей.
    /// Тип аналогичен System.Drawing.Drawing2D.FillMode
    /// </summary>
    public enum InteriorFillMode : byte
    {
        /// <summary>
        /// Режим заполнения нечетных областей.
        /// </summary>
        Alternate = 0,
        /// <summary>
        /// Сплошной режим заполнения.
        /// </summary>
        Winding = 1
    }

    /// <summary>
    /// Генератор последовательностей пикселей
    /// для векторных данных.
    /// </summary>
    public class SpanGenerator 
    {
        private byte _subPixelLevel = 4;
        private float _scanStep = 1 / 4f;
        private float _antiAliasingGamma;

        private double[] _alphaTable = new double[255];

        /// <summary>
        /// Гамма антиалиасинга.
        /// </summary>
        public float AntiAliasingGamma
        {
            get 
            { 
                return _antiAliasingGamma; 
            }
            set 
            {
                for (int i = 0; i < _alphaTable.Length; i++)
                    _alphaTable[i] = Math.Pow(i / 255f, _antiAliasingGamma);
                _antiAliasingGamma = value; 
            }
        }

        /// <summary>
        /// Субпиксельный уровень.
        /// </summary>
        public byte SubPixelLevel
        {
            get { return _subPixelLevel; }
            set
            {
                _subPixelLevel = value;
                _scanStep = 1 / value;
            }
        }

        /// <summary>
        /// Возвращает горизонтальные последовательности пикселей для
        /// геометрической фигуры.
        /// </summary>
        /// <param name="sourceGeometry"></param>
        /// <param name="fill">Заливка</param>
        /// <param name="minX"></param>
        /// <param name="minY"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <returns>Список горизонтальных последовательностей символов</returns>
        public IList<PixelSpan> GetHorizontalSpans(IScannable sourceGeometry, int minX, int minY, int maxX, int maxY, FillBase fill)
        {
            List<PixelSpan> spans = new List<PixelSpan>();

            BoundingRectangle br = sourceGeometry.BoundingBox;

            float startY = (int)Math.Max((float)br.MinY - 1, minY);
            float endY = Math.Min((float)br.MaxY + 1, maxY);
            float startX = Math.Max((float)br.MinX, minX);
            float endX = Math.Min((float)br.MaxX + 1, maxX);

            sourceGeometry.InitScaning(minX, maxX, (int)startY, (int)endY, Orientation.Horizontal);

            List<float[]> pixelScanIntersections = new List<float[]>();
            for (float scanY = startY; scanY < endY; scanY++)
            {
                pixelScanIntersections.Clear();
                for (byte i = 0; i < _subPixelLevel; i++)
                {
                    float[] intersections;
                    sourceGeometry.ComputeHorizontalIntersections(scanY + _scanStep * i, out intersections);
                    pixelScanIntersections.Add(intersections);
                }
                addSpans(Orientation.Horizontal, spans, pixelScanIntersections, (int)scanY, fill);
            }

            return spans;
        }

        /// <summary>
        /// Возвращает вертикальные последовательности пикселей для
        /// геометрической фигуры.
        /// </summary>
        /// <param name="sourceGeometry"></param>
        /// <param name="fill">Заливка</param>
        /// <param name="minX"></param>
        /// <param name="minY"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <returns>Список вертикальных последовательностей символов</returns>
        public IList<PixelSpan> GetVerticalSpans(IScannable sourceGeometry, int minX, int minY, int maxX, int maxY, FillBase fill)
        {
            List<PixelSpan> spans = new List<PixelSpan>();

            BoundingRectangle br = sourceGeometry.BoundingBox;

            float startY = Math.Max((float)br.MinY, minY);
            float endY = Math.Min((float)br.MaxY + 1, maxY);
            float startX = (int)Math.Max((float)br.MinX - 1, minX);
            float endX = Math.Min((float)br.MaxX + 1, maxX);

            sourceGeometry.InitScaning((int)startX, (int)endX, minY, maxY, Orientation.Vertical);

            List<float[]> pixelScanIntersections = new List<float[]>();
            for (float scanX = startX; scanX < endX; scanX++)
            {
                pixelScanIntersections.Clear();
                for (byte i = 0; i < _subPixelLevel; i++)
                {
                    float[] intersections;
                    sourceGeometry.ComputeVerticalIntersections(scanX + _scanStep * i, out intersections);
                    pixelScanIntersections.Add(intersections);
                }
                addSpans(Orientation.Vertical, spans, pixelScanIntersections, (int)scanX, fill);
            }

            return spans;
        }

        private void addSpans(Orientation orientation, List<PixelSpan> spans, List<float[]> pixelScanIntersections, int scanPosition, FillBase fill)
        {
            if (pixelScanIntersections.Count == 0)
                return;

            float min = float.MaxValue;
            float max = float.MinValue;

            foreach (float[] subPixelSpan in pixelScanIntersections)
                for (int i = 0; i < subPixelSpan.Length; i++)
                {
                    float spsi = subPixelSpan[i];
                    if (spsi < min)
                        min = spsi;
                    if (spsi > max)
                        max = spsi;
                }

            if (max <= min) return;

            float[] pixelCoverage = new float[(int)(max + 2) - (int)min];

            int shift = (int)min;

            // вычисление покрытия пикселей
            foreach (float[] subPixelSpan in pixelScanIntersections)
            {
                int subPixelSpanLength = subPixelSpan.Length; 
                if (subPixelSpanLength > 0)
                {
                    for (int k = 0; k < subPixelSpanLength; k += 2)
                    {
                        float spanStart = subPixelSpan[k];
                        float spanEnd = subPixelSpan[k + 1];

                        // длина пересечения сканирующего отрезка с объектом меньше единицы
                        // и это пересечение не пересекает границы пикселей
                        if ((int)spanEnd == (int)spanStart ||
                            (int)spanEnd == (int)spanStart + 1)
                            if (spanEnd - spanStart < 1)
                            {
                                pixelCoverage[(int)spanStart - shift] += spanEnd - spanStart;
                                continue;
                            }

                        // пиксели, пересеченные сканирующим отрезком "насквозь"
                        float hv = spanEnd - shift - 1;
                        for (int pixelIndex = (int)spanStart + 1 - shift; pixelIndex < hv; pixelIndex++)
                            pixelCoverage[pixelIndex]++;

                        // пиксель, в котором начался сканирующий отрезок
                        pixelCoverage[(int)spanStart - shift] += 1 - (spanStart - (int)spanStart);

                        // пиксель, в котором закончился сканирующий отрезок
                        if (hv == (int)hv)
                            pixelCoverage[(int)hv] += 1;
                        else
                            pixelCoverage[(int)hv + 1] += spanEnd - (int)spanEnd;
                    }
                }
            }

            // вычисление горизонтальных последовательностей символов
            int spanStartIndex = 0;
            bool spanStarted = false;
            int coverageArrayHiIndex = pixelCoverage.Length - 1;
            float alphaStep = 255f / _subPixelLevel;
            for (int i = 0; i <= coverageArrayHiIndex; i++)
            {
                if (!spanStarted)
                {
                    if (pixelCoverage[i] > 0)
                    {
                        spanStartIndex = i;
                        spanStarted = true;
                    }
                }
                else
                {
                    if (pixelCoverage[i] == 0 || i == coverageArrayHiIndex)
                    {
                        Int32[] pixelValues = new int[i - spanStartIndex];
                        int f = 0;
                        switch(orientation)
                        {
                            case Orientation.Horizontal:
                                for (int k = spanStartIndex; k < i; k++)
                                {
                                    double coverage = pixelCoverage[k];
                                    if (coverage > _subPixelLevel)
                                        coverage = _subPixelLevel;

                                    double antiAliasingAlpha = _alphaTable[(int)(coverage / _subPixelLevel * 254f)];

                                    Int32 color = fill.GetPixelColor((int)min + f + spanStartIndex, scanPosition);
                                    byte fillAlpha = (byte)(color >> 24 & 0xFF);
                                    if (fillAlpha == 0)
                                        pixelValues[f] = (byte)(antiAliasingAlpha * 255f) << 24 | color;
                                    else
                                        pixelValues[f] = (byte)(antiAliasingAlpha * fillAlpha) << 24 | (color & 0x00FFFFFF);

                                    f++;
                                }
                                spans.Add(new PixelSpan(Orientation.Horizontal, (int)min + spanStartIndex, scanPosition, pixelValues));
                                break;

                            case Orientation.Vertical:
                                for (int k = spanStartIndex; k < i; k++)
                                {
                                    double coverage = pixelCoverage[k];
                                    if (coverage > _subPixelLevel)
                                        coverage = _subPixelLevel;

                                    double antiAliasingAlpha = _alphaTable[(int)(coverage / _subPixelLevel * 254f)];

                                    Int32 color = fill.GetPixelColor(scanPosition, (int)min + f + spanStartIndex);
                                    byte fillAlpha = (byte)(color >> 24 & 0xFF);
                                    if (fillAlpha == 0)
                                        pixelValues[f] = (byte)(antiAliasingAlpha * 255f) << 24 | color;
                                    else
                                        pixelValues[f] = (byte)(antiAliasingAlpha * fillAlpha) << 24 | (color & 0x00FFFFFF);

                                    f++;
                                }
                                spans.Add(new PixelSpan(Orientation.Vertical, scanPosition, (int)min + spanStartIndex, pixelValues));
                                break;
                        }
                        spanStarted = false;
                    }
                }
            }
        }

        /// <summary>
        /// Создает экземпляр SpanGenerator.
        /// </summary>
        public SpanGenerator()
        {
            AntiAliasingGamma = 1f;
        }
    }
}