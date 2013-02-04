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
** Файл: Fills.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Назначение: Классы заливок.
**
=============================================================================*/

namespace MapAround.Rendering
{
    using System;

    /// <summary>
    /// Базовый класс заливки.
    /// </summary>
    public abstract class FillBase
    {
        /// <summary>
        /// Реализация должна вычислять цвет пикселя.
        /// </summary>
        /// <param name="x">Координата x</param>
        /// <param name="y">Координата y</param>
        /// <returns>Цвет пикселя</returns>
        public abstract Int32 GetPixelColor(int x, int y);
    }

    /// <summary>
    /// Сплошная заливка.
    /// </summary>
    public class SolidFill : FillBase
    {
        Int32 _color = 0;

        /// <summary>
        /// Получает или устанавливает цвет.
        /// </summary>
        public Int32 Color
        {
            get { return _color; }
        }

        /// <summary>
        /// Вычисляет цвет пикселя.
        /// </summary>
        /// <param name="x">Координата x</param>
        /// <param name="y">Координата y</param>
        /// <returns>Цвет пикселя</returns>
        public override Int32 GetPixelColor(int x, int y)
        {
            return _color;
        }

        /// <summary>
        /// Создает экземпляр SoldFill.
        /// </summary>
        /// <param name="color">Цвет</param>
        public SolidFill(Int32 color)
        {
            _color = color;
        }
    }

    /// <summary>
    /// Шаблонная заливка
    /// </summary>
    public class PatternFill : FillBase
    {
        private Int32[,] _pattern;
        private int _originX;
        private int _originY;

        /// <summary>
        /// Получает или устанавливает шаблон заливки.
        /// </summary>
        public Int32[,] Pattern
        {
            get { return _pattern; }
            set { _pattern = value; }
        }

        /// <summary>
        /// Вычисляет цвет пикселя.
        /// </summary>
        /// <param name="x">Координата x</param>
        /// <param name="y">Координата y</param>
        /// <returns>Цвет пикселя</returns>
        public override Int32 GetPixelColor(int x, int y)
        {
            int ix = Math.Abs(y - _originY) % _pattern.GetLength(0);
            int iy = Math.Abs(x - _originX) % _pattern.GetLength(1);
            return _pattern[ix, iy];
        }

        /// <summary>
        /// Создает экземпляр PatternFill.
        /// </summary>
        /// <param name="pattern">Таблица значений пиекселй шаблона</param>
        /// <param name="originX">Координата x начала отсчета</param>
        /// <param name="originY">Координата y начала отсчета</param>
        public PatternFill(Int32[,] pattern, int originX, int originY)
        {
            if (pattern.GetLength(0) == 0 || pattern.GetLength(1) == 0)
                throw new ArgumentException("pattern");

            _pattern = pattern;
            _originX = originX;
            _originY = originY;
        }
    }

    /// <summary>
    /// Штриховая заливка.
    /// </summary>
    public class HatchFill : FillBase
    {
        private bool[,] _pattern;
        private int _originX;
        private int _originY;
        private Int32 _color1;
        private Int32 _color2;

        /// <summary>
        /// Получает или устанавливает таблицу штриховки.
        /// </summary>
        public bool[,] Pattern
        {
            get { return _pattern; }
            set { _pattern = value; }
        }

        /// <summary>
        /// Вычисляет цвет пикселя
        /// </summary>
        /// <param name="x">Координата x пикселя</param>
        /// <param name="y">Координата y пикселя</param>
        /// <returns>Цвет пикселя</returns>
        public override Int32 GetPixelColor(int x, int y)
        {
            int ix = Math.Abs(y - _originY) % _pattern.GetLength(0);
            int iy = Math.Abs(x - _originX) % _pattern.GetLength(1);
            return _pattern[ix, iy] ? _color1 : _color2;
        }

        /// <summary>
        /// Создает экземпляр HatchFill.
        /// </summary>
        /// <param name="pattern">Таблица штриховки</param>
        /// <param name="color1">Цвет 1</param>
        /// <param name="color2">Цвет 2</param>
        /// <param name="originX">Координата x начала отсчета</param>
        /// <param name="originY">Координата y начала отсчета</param>
        public HatchFill(bool[,] pattern, Int32 color1, Int32 color2, int originX, int originY)
        {
            if (pattern.GetLength(0) == 0 || pattern.GetLength(1) == 0)
                throw new ArgumentException("pattern");

            _pattern = pattern;
            _originX = originX;
            _originY = originY;
            _color1 = color1;
            _color2 = color2;
        }
    }
}