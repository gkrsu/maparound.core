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
** Назначение: Растеризация.
**
=============================================================================*/

namespace MapAround.Rendering
{
    using System;
    using System.Drawing;

    /// <summary>
    /// Класс для выполнения низкоуровневых операций (на уровне пикселей 
    /// и их последовательностей) над буфером изображения.
    /// </summary>
    public class RasterData
    {
        private Int32[] _buffer;
        private int _width;
        private int _height;

        /// <summary>
        /// Высота изображения в пикселях.
        /// </summary>
        public int Height
        {
            get { return _height; }
            set { _height = value; }
        }

        /// <summary>
        /// Ширина изображения в пикселях.
        /// </summary>
        public int Width
        {
            get { return _width; }
            set { _width = value; }
        }

        /// <summary>
        /// Массив чисел представляющих пиксели растра.
        /// </summary>
        public Int32[] Buffer
        {
            get { return _buffer; }
            set { _buffer = value; }
        }

        /// <summary>
        /// Устанавливает значения пикселя.
        /// </summary>
        /// <param name="x">Координата х</param>
        /// <param name="y">Координата y</param>
        /// <param name="r">Значение красного канала</param>
        /// <param name="g">Значение зеленого канала</param>
        /// <param name="b">Значение синего канала</param>
        /// <param name="a">Значение альфа-канала</param>
        public void SetPixel(int x, int y, byte r, byte g, byte b, byte a)
        {
            int shift = _width * y + x;
            _buffer[shift] =
                (int)a << 24 |         
                (int)r << 16 |         
                (int)g << 8 |         
                (int)b;
        }

        /// <summary>
        /// Устанавливает значения пикселя.
        /// </summary>
        /// <param name="x">Координата х</param>
        /// <param name="y">Координата y</param>
        /// <param name="pixelData">Данные пикселя в формате 32bppArgb</param>
        public void SetPixel(int x, int y, Int32 pixelData)
        {
            int shift = _width * y + x;
            _buffer[shift] = pixelData;
        }

        /// <summary>
        /// Смешивает значение пикселя.
        /// </summary>
        /// <param name="x">Координата x</param>
        /// <param name="y">Координата y</param>
        /// <param name="pixelData">Данные пикселя в формате 32bppArgb</param>
        public void BlendPixel(int x, int y, Int32 pixelData)
        {
            byte alpha = (byte)(pixelData >> 24 & 0xFF);
            if (alpha == 0)
                return;

            int shift = _width * y + x;
            if (alpha == 255)
            {
                _buffer[shift] = pixelData;
                return;
            }

            Int32 oldPixelData = _buffer[shift];
            byte oldAlpha = (byte)((oldPixelData >> 24 & 0xFF));

            byte newAlpha = (byte)(alpha + oldAlpha - alpha * oldAlpha / 255f);

            float a1 = alpha / 255f;
            float a0 = (1 - a1) * oldAlpha / 255f;
            a0 = a0 / (a1 + a0);
            a1 = 1 - a0;

            Int32 b1 = newAlpha << 24;
            Int32 b2 = (byte)((oldPixelData >> 16 & 0xFF) * a0 + (pixelData >> 16 & 0xFF) * a1) << 16;
            Int32 b3 = (byte)((oldPixelData >> 8 & 0xFF) * a0 + (pixelData >> 8 & 0xFF) * a1) << 8;
            Int32 b4 = (byte)((oldPixelData & 0xFF) * a0 + (pixelData & 0xFF) * a1);

            _buffer[shift++] = b1 | b2 | b3 | b4;
        }

        /// <summary>
        /// Записывает в буфер последовательность пикселей.
        /// </summary>
        /// <param name="span">Последовательность пикселей</param>
        public void SetSpan(PixelSpan span)
        {
            int shift = _width * span.Y + span.X;
            Int32[] pixels = span.PixelValues;
            int length = pixels.Length;
            int shiftIncrement = 1;
            if (span.Orientation == Orientation.Vertical)
                shiftIncrement = _width;

            for (int i = 0; i < length; i++)
            {
                _buffer[shift] = pixels[i];
                shift += shiftIncrement;
            }
        }

        /// <summary>
        /// Смешивает последовательность пикселей.
        /// </summary>
        /// <param name="span">Последовательность пикселей</param>
        public void BlendSpan(PixelSpan span)
        {
            int shift = _width * span.Y + span.X;
            Int32[] spanPixels = span.PixelValues;
            int length = spanPixels.Length;
            int shiftIncrement = 1;

            if (span.Orientation == Orientation.Vertical)
                shiftIncrement = _width;

            for (int i = 0; i < length; i++)
            {
                Int32 pixelData = spanPixels[i];

                byte alpha = (byte)(pixelData >> 24 & 0xFF);

                if (alpha == 255)
                {
                    _buffer[shift] = pixelData;
                    shift += shiftIncrement;
                    continue;
                }

                // целесообразность проверки под вопросом
                if (alpha == 0)
                {
                    shift += shiftIncrement;
                    continue;
                }

                Int32 oldPixelData = _buffer[shift];
                byte oldAlpha = (byte)((oldPixelData >> 24 & 0xFF));

                if (oldAlpha == 0)
                {
                    _buffer[shift] = pixelData;
                    shift += shiftIncrement;
                    continue;
                }

                byte newAlpha = (byte)(alpha + oldAlpha - alpha * oldAlpha / 255f);

                float a1 = alpha / 255f;
                float a0 = (1 - a1) * oldAlpha / 255f;
                a0 = a0 / (a1 + a0);
                a1 = 1 - a0;

                Int32 b1 = newAlpha << 24;
                Int32 b2 = (byte)((oldPixelData >> 16 & 0xFF) * a0 + (pixelData >> 16 & 0xFF) * a1) << 16;
                Int32 b3 = (byte)((oldPixelData >> 8 & 0xFF) * a0 + (pixelData >> 8 & 0xFF) * a1) << 8;
                Int32 b4 = (byte)((oldPixelData & 0xFF) * a0 + (pixelData & 0xFF) * a1);

                _buffer[shift] = b1 | b2 | b3 | b4;
                shift += shiftIncrement;
            }
        }

        /// <summary>
        /// Устанавливает значения всех пикселей в pixelData.
        /// </summary>
        /// <param name="pixelData">Значение пикселя</param>
        public void Clear(Int32 pixelData)
        {
            for (int i = 0; i < _buffer.Length; i++)
                _buffer[i] = pixelData;
        }

        /// <summary>
        /// Формирует объект Bitmap
        /// </summary>
        /// <returns>Объект Bitmap</returns>
        public Bitmap GetBitmap()
        {
            Bitmap bmp = new Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            System.Drawing.Imaging.BitmapData bmpData =
                    bmp.LockBits(new Rectangle(0, 0, _width, _height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                System.Runtime.InteropServices.Marshal.Copy(_buffer, 0, bmpData.Scan0, bmp.Width * bmp.Height);
            }
            finally
            {
                bmp.UnlockBits(bmpData);
            }

            return bmp;
        }

        /// <summary>
        /// Создает ScanlineRenderer и копирует в него изображение из Bitmap.
        /// </summary>
        /// <param name="bitmap">Объект Bitmap</param>
        /// <returns>Экземпляр RasterData</returns>
        public static RasterData FromBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");

            if (bitmap.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                throw new ArgumentException("Illegal pixel format. Should be 32bppArgb", "bitmap");
            RasterData renderer = new RasterData(bitmap.Width, bitmap.Height);

            System.Drawing.Imaging.BitmapData bmpData =
                    bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, renderer._buffer, 0, bitmap.Width * bitmap.Height);
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }
            return null;
        }

        /// <summary>
        /// Создает экземпляр RasterData.
        /// </summary>
        /// <param name="width">Ширина изображения в пикселях</param>
        /// <param name="height">Высота изображения в пикселях</param>
        public RasterData(int width, int height)
        {
            _width = width;
            _height = height;
            _buffer = new Int32[width * height];
        }
    }

    /// <summary>
    /// Ориентация.
    /// <remarks>
    /// Элементы этого перечисления используются для задания ориентации
    /// последовательностей пикселей и для определения направления сканирования.
    /// </remarks>
    /// </summary>
    public enum Orientation
    { 
        /// <summary>
        /// Горизонтальная ориентация.
        /// </summary>
        Horizontal,
        /// <summary>
        /// Вертикальная ориентация.
        /// </summary>
        Vertical
    }

    /// <summary>
    /// Горизонтальная или вертикальная последовательность пикселей.
    /// </summary>
    public class PixelSpan
    {
        Int32[] _pixelValues;
        private int _x;
        private int _y;

        private Orientation _orientation;

        /// <summary>
        /// Ориентация последовательности пикселей.
        /// </summary>
        public Orientation Orientation
        {
            get { return _orientation; }
        }

        /// <summary>
        /// Получает координату x самого левого пикселя в последовательности.
        /// </summary>
        public int X
        {
            get { return _x; }
        }

        /// <summary>
        /// Получает координату y последовательности пикселей.
        /// </summary>
        public int Y
        {
            get { return _y; }
        }

        /// <summary>
        /// Получает или устанавливает массив значений пикселей.
        /// </summary>
        public Int32[] PixelValues
        {
            get { return _pixelValues; }
            set { _pixelValues = value; }
        }

        /// <summary>
        /// Создает экземпляр PixelSpan.
        /// </summary>
        /// <param name="orientation">Ориентация последовательности пикселей</param>
        /// <param name="x">Координата x самого левого пикселя в последовательности</param>
        /// <param name="y">Координата y последовательности</param>
        /// <param name="pixelValues">Значения пикселей</param>
        public PixelSpan(Orientation orientation, int x, int y, Int32[] pixelValues)
        {
            _orientation = orientation;
            _pixelValues = pixelValues;
            _x = x;
            _y = y;
        }

        /// <summary>
        /// Генерирует однородную последовательность пикселей.
        /// </summary>
        /// <param name="orientation">Ориентация последовательности пикселей</param>
        /// <param name="x">Координата x самого левого пикселя в последовательности</param>
        /// <param name="y">Координата y последовательности</param>
        /// <param name="pixelData">Значение пикселей</param>
        /// <param name="length">Длина последовательности</param>
        /// <returns>Однородная последовательность пикселей</returns>
        public static PixelSpan GetSolid(Orientation orientation, int x, int y, Int32 pixelData, int length)
        {
            Int32[] pixelValues = new Int32[length];
            for (int i = 0; i < length; i++)
                pixelValues[i] = pixelData;

            return new PixelSpan(orientation, x, y, pixelValues);
        }
    }
}