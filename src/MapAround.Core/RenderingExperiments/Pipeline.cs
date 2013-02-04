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
** Файл: LowLevelRendering.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Назначение: Эксперименты по "ручному" рендерингу.
**
=============================================================================*/

namespace MapAround.Rendering
{
    using System.Collections.Generic;
    using System.Threading;
    using System;

    using MapAround.Geometry;

    /// <summary>
    /// Пространство имен MapAround.Rendering содержит классы, 
    /// выполняющие функции генерирования растров по векторным 
    /// данным. Это экспериментальное пространство имен для 
    /// исследования возможности замены GDI.
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Степень распараллеливания процедур генерирования изображений.
    /// </summary>
    public enum ParallelizationLevel
    { 
        /// <summary>
        /// Распараллеливание не выполняется.
        /// </summary>
        Single,
        /// <summary>
        /// Процедуры выполняются в двух потоках.
        /// </summary>
        Duo,
        /// <summary>
        /// Процедуры выполняются в четырех потоках.
        /// </summary>
        Quad
    }

    /// <summary>
    /// Конвейер рендеринга.
    /// </summary>
    public class RenderingPipeline
    {
        private SpanGenerator _spanGenerator;
        private RasterData _rasterData;
        private ParallelizationLevel _parallelizationLevel = ParallelizationLevel.Single;

        private Thread _thread1 = null;
        private Thread _thread2 = null;
        private Thread _thread3 = null;
        private Thread _thread4 = null;

        private List<IScannable> _objectPool = new List<IScannable>();
        private List<FillBase> _fillPool = new List<FillBase>();

        private int _poolSize = 100;
        private bool _unlimitedPool = false;

        private class ThreadStartData
        {
            public int MinX;
            public int MinY;
            public int MaxX;
            public int MaxY;
        }

        private void beginRenderObjectPool(object startData)
        {
            ThreadStartData tsd = (ThreadStartData)startData;
            int f = 0;
            foreach (IScannable obj in _objectPool)
            {
                BoundingRectangle br = obj.BoundingBox;
                if (obj.BoundingBox.Intersects(new BoundingRectangle(tsd.MinX, tsd.MinY, tsd.MaxX, tsd.MaxY)))
                {
                    IScannable newObj = (IScannable)obj.Clone();

                    IList<PixelSpan> spans = null;
                    if (br.Width >= br.Height)
                        spans = _spanGenerator.GetHorizontalSpans(newObj, tsd.MinX, tsd.MinY, tsd.MaxX, tsd.MaxY, _fillPool[f]);
                    else
                        spans = _spanGenerator.GetVerticalSpans(newObj, tsd.MinX, tsd.MinY, tsd.MaxX, tsd.MaxY, _fillPool[f]);
                    foreach (PixelSpan ps in spans)
                        _rasterData.BlendSpan(ps);
                }
                f++;
            }
        }

        private void renderSingleThread(IScannable obj, FillBase fill)
        {
            IList<PixelSpan> spans = null;
            if (obj.BoundingBox.Width >= obj.BoundingBox.Height)
                spans = _spanGenerator.GetHorizontalSpans(obj, 0, 0, _rasterData.Width, _rasterData.Height, fill);
            else
                spans = _spanGenerator.GetVerticalSpans(obj, 0, 0, _rasterData.Width, _rasterData.Height, fill);

            foreach (PixelSpan ps in spans)
                _rasterData.BlendSpan(ps);
        }

        private void renderMultipleThreads(IScannable obj, FillBase fill)
        {
            _objectPool.Add(obj);
            _fillPool.Add(fill);

            if (!_unlimitedPool && _objectPool.Count == _poolSize)
                Flush();
        }

        /// <summary>
        /// Количество объектов в пуле.
        /// <remarks>
        /// Большие значения увеличивают расход памяти и уменьшают время выполнения.
        /// </remarks>
        /// </summary>
        public int PoolSize
        {
            get { return _poolSize; }
            set { _poolSize = value; }
        }

        private void wait()
        {
            if (_thread1 != null)
                _thread1.Join();

            if (_thread2 != null)
                _thread2.Join();

            if (_thread3 != null)
                _thread3.Join();

            if (_thread4 != null)
                _thread4.Join();

            _objectPool.Clear();
            _fillPool.Clear();
        }

        /// <summary>
        /// Возвращает управление после завершения всех невыполненных заданий.
        /// </summary>
        public void Flush()
        {
            switch (_parallelizationLevel)
            { 
                case ParallelizationLevel.Single:
                    break;
                case ParallelizationLevel.Duo:
                    _thread1 = new Thread(beginRenderObjectPool);
                    _thread1.Start(new ThreadStartData() { MinX = 0, MinY = 0, MaxX = _rasterData.Width, MaxY = _rasterData.Height / 2});

                    _thread2 = new Thread(beginRenderObjectPool);
                    _thread2.Start(new ThreadStartData() { MinX = 0, MinY = _rasterData.Height / 2, MaxX = _rasterData.Width, MaxY = _rasterData.Height });
                    break;
                case ParallelizationLevel.Quad:
                    _thread1 = new Thread(beginRenderObjectPool);
                    _thread1.Start(new ThreadStartData() { MinX = 0, MinY = 0, MaxX = _rasterData.Width / 2, MaxY = _rasterData.Height / 2 });

                    _thread2 = new Thread(beginRenderObjectPool);
                    _thread2.Start(new ThreadStartData() { MinX = _rasterData.Width / 2, MinY = 0, MaxX = _rasterData.Width, MaxY = _rasterData.Height / 2});

                    _thread3 = new Thread(beginRenderObjectPool);
                    _thread3.Start(new ThreadStartData() { MinX = 0, MinY = _rasterData.Height / 2, MaxX = _rasterData.Width / 2, MaxY = _rasterData.Height});

                    _thread4 = new Thread(beginRenderObjectPool);
                    _thread4.Start(new ThreadStartData() { MinX = _rasterData.Width / 2, MinY = _rasterData.Height / 2, MaxX = _rasterData.Width, MaxY = _rasterData.Height });
                    break;
            }
            wait();
        }

        /// <summary>
        /// Выводит объект.
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <param name="fill">Заливка</param>
        public void Render(IScannable obj, FillBase fill)
        {
            switch(_parallelizationLevel)
            {
                case ParallelizationLevel.Single:
                    renderSingleThread(obj, fill);
                    break;
                case ParallelizationLevel.Duo:
                case ParallelizationLevel.Quad:
                    renderMultipleThreads(obj, fill);
                    break;
            }
        }

        /// <summary>
        /// Получает или устанавливает значение указывающее будет ли пакет
        /// заданий рендеринга ограничен значением PoolSize.
        /// </summary>
        public bool UnlimitedPool
        {
            get { return _unlimitedPool; }
            set { _unlimitedPool = value; }
        }

        /// <summary>
        /// Создает экземпляр RenderingPipeline.
        /// </summary>
        /// <param name="rasterData">Растр</param>
        /// <param name="spanGenerator">Генератор последовательностей пикселей</param>
        /// <param name="parallelizationLevel">Степень распараллеливания</param>
        public RenderingPipeline(RasterData rasterData, 
            SpanGenerator spanGenerator, 
            ParallelizationLevel parallelizationLevel)
        {
            _rasterData = rasterData;
            _spanGenerator = spanGenerator;
            _parallelizationLevel = parallelizationLevel;
        }
    }
}