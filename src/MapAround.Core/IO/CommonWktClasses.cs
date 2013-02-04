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
** File: CommonWktClasses.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Common classes for handling wkt representations of objects
**
=============================================================================*/

namespace MapAround.IO
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.IO;

    /// <summary>
    /// The MapAround.IO contains interfaces and classes 
    /// defining I/O operations with spatial data.
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Enumerates possible token types in
    /// well-known text representation.
    /// </summary>
    internal enum TokenType
    {
        /// <summary>
        /// A word lexem.
        /// </summary>
        Word,
        /// <summary>
        /// A number lexem.
        /// </summary>
        Number,
        /// <summary>
        /// An end of line.
        /// </summary>
        Eol,
        /// <summary>
        /// An end of file (stream).
        /// Конец входного потока.
        /// </summary>
        Eof,
        /// <summary>
        /// A space.
        /// </summary>
        Whitespace,
        /// <summary>
        /// A symbol.
        /// </summary>
        Symbol
    }

    ///<summary>
    /// Reads an input stream and constructs tokens from the data read.
    ///</summary>
    internal class StreamTokenizer
    {
        TokenType _currentTokenType;
        TextReader _reader;
        string _currentToken;
        bool _ignoreWhitespace = false;
        int _lineNumber = 1;
        int _colNumber = 1;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of MapAround.IO.StreamTokenizer
        /// </summary>
        /// <param name="reader">A System>IO.TextReader instance</param>
        /// <param name="ignoreWhitespace">A value indicating whether the whitespace symbols are ignored while tokenizing</param>
        public StreamTokenizer(TextReader reader, bool ignoreWhitespace)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            _reader = reader;
            _ignoreWhitespace = ignoreWhitespace;
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets a number of current line.
        /// </summary>
        public int LineNumber
        {
            get
            { return _lineNumber; }
        }
        /// <summary>
        /// Gets a number of symbol in current line.
        /// </summary>
        public int Column
        {
            get
            { return _colNumber; }
        }


        #endregion

        #region Methods

        /// <summary>
        /// Parses a numeric value.
        /// </summary>
        /// <returns>Parsed value</returns>
        public double GetNumericValue()
        {
            string number = this.GetStringValue();
            if (this.GetTokenType() == TokenType.Number)
                return double.Parse(number, CultureInfo.InvariantCulture.NumberFormat);
            throw new InvalidDataException(String.Format(CultureInfo.InvariantCulture.NumberFormat, "Lexem '{0}' is not a number. Line {1} position {2}.",
                number, this.LineNumber, this.Column));
        }
        /// <summary>
        /// Parses a word value.
        /// </summary>
        /// <returns>Parsed value</returns>
        public string GetStringValue()
        {
            return _currentToken;
        }

        /// <summary>
        /// Gets a type of current token.
        /// </summary>
        /// <returns>A type of current token</returns>
        public TokenType GetTokenType()
        {
            return _currentTokenType;
        }

        /// <summary>
        /// Gets a next token.
        /// </summary>
        /// <param name="ignoreWhitespace">A value indicating whether the whitespace symbols are ignored</param>
        /// <returns>A next token</returns>
        public TokenType NextToken(bool ignoreWhitespace)
        {
            TokenType nextTokenType;
            if (ignoreWhitespace)
            {
                nextTokenType = NextNonWhitespaceToken();
            }
            else
            {
                nextTokenType = NextTokenAny();
            }
            return nextTokenType;
        }

        /// <summary>
        /// Gets a next token.
        /// </summary>
        /// <returns>A next token</returns>
        public TokenType NextToken()
        {
            return NextToken(_ignoreWhitespace);
        }

        private TokenType NextTokenAny()
        {
            TokenType nextTokenType = TokenType.Eof;
            char[] chars = new char[1];
            _currentToken = "";
            _currentTokenType = TokenType.Eof;
            int finished = _reader.Read(chars, 0, 1);

            bool isNumber = false;
            bool isWord = false;
            byte[] ba = null;
#if Silverlight
			Encoding AE = System.Text.Encoding.Default;
#else
            ASCIIEncoding AE = new ASCIIEncoding();
#endif
            char[] ascii = null;
            Char currentCharacter;
            Char nextCharacter;
            while (finished != 0)
            {
                // convert int to char
                ba = new Byte[] { (byte)_reader.Peek() };

                ascii = AE.GetChars(ba);

                currentCharacter = chars[0];
                nextCharacter = ascii[0];
                _currentTokenType = GetType(currentCharacter);
                nextTokenType = GetType(nextCharacter);

                // handling of words with _
                if (isWord && currentCharacter == '_')
                {
                    _currentTokenType = TokenType.Word;
                }
                // handing of words ending in numbers
                if (isWord && _currentTokenType == TokenType.Number)
                {
                    _currentTokenType = TokenType.Word;
                }

                if (_currentTokenType == TokenType.Word && nextCharacter == '_')
                {
                    //enable words with _ inbetween
                    nextTokenType = TokenType.Word;
                    isWord = true;
                }
                if (_currentTokenType == TokenType.Word && nextTokenType == TokenType.Number)
                {
                    //enable words ending with numbers
                    nextTokenType = TokenType.Word;
                    isWord = true;
                }

                // handle negative numbers
                if (currentCharacter == '-' && nextTokenType == TokenType.Number && isNumber == false)
                {
                    _currentTokenType = TokenType.Number;
                    nextTokenType = TokenType.Number;
                    //isNumber = true;
                }

                // this handles numbers with a decimal point
                if (isNumber && nextTokenType == TokenType.Number && currentCharacter == '.')
                {
                    _currentTokenType = TokenType.Number;
                }
                if (_currentTokenType == TokenType.Number && nextCharacter == '.' && isNumber == false)
                {
                    nextTokenType = TokenType.Number;
                    isNumber = true;
                }

                _colNumber++;
                if (_currentTokenType == TokenType.Eol)
                {
                    _lineNumber++;
                    _colNumber = 1;
                }

                _currentToken = _currentToken + currentCharacter;
                //if (_currentTokenType==TokenType.Word && nextCharacter=='_')
                //{
                // enable words with _ inbetween
                //	finished = _reader.Read(chars,0,1);
                //}
                if (_currentTokenType != nextTokenType)
                {
                    finished = 0;
                }
                else if (_currentTokenType == TokenType.Symbol && currentCharacter != '-')
                {
                    finished = 0;
                }
                else
                {
                    finished = _reader.Read(chars, 0, 1);
                }
            }
            return _currentTokenType;
        }

        /// <summary>
        /// Cumputes a token type by its firets character.
        /// </summary>
        /// <param name="character">Символ</param>
        /// <returns>A token type</returns>
        private static TokenType GetType(char character)
        {
            if (Char.IsDigit(character))
            {
                return TokenType.Number;
            }
            else if (Char.IsLetter(character))
            {
                return TokenType.Word;
            }
            else if (character == '\n')
            {
                return TokenType.Eol;
            }
            else if (Char.IsWhiteSpace(character) || Char.IsControl(character))
            {
                return TokenType.Whitespace;
            }
            else //(Char.IsSymbol(character))
            {
                return TokenType.Symbol;
            }

        }

        /// <summary>
        /// Gets the next token which is different from TokenType.Whitespace.
        /// </summary>
        /// <returns>The next token which is different from TokenType.Whitespace</returns>
        private TokenType NextNonWhitespaceToken()
        {

            TokenType tokentype = this.NextTokenAny();
            while (tokentype == TokenType.Whitespace || tokentype == TokenType.Eol)
            {
                tokentype = this.NextTokenAny();
            }

            return tokentype;
        }
        #endregion

    }

    ///<summary>
    ///Reads an input stream containing well-known text of object 
    ///and constructs wkt-tokens from the data read.
    ///</summary>
    internal class WktStreamTokenizer : StreamTokenizer
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of MapAround.IO.WktStreamTokenizer 
        /// </summary>
        /// <param name="reader">A System.IO.TextReader instance</param>
        public WktStreamTokenizer(TextReader reader)
            : base(reader, true)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
        }
        #endregion

        #region Methods

        /// <summary>
        /// Reads a token and checks whether it corresponds the specified token. 
        /// </summary>
        /// <param name="expectedToken">An expected token</param>
        internal void ReadToken(string expectedToken)
        {
            this.NextToken();
            if (this.GetStringValue() != expectedToken)
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture.NumberFormat, "Expected ('{3}'), but goes '{0}'. Line {1}, position {2}.", this.GetStringValue(), this.LineNumber, this.Column, expectedToken));
        }

        /// <summary>
        /// Reads a quoted string.
        /// </summary>
        /// <remarks>
        /// All the whitespace tokens between quotes are ignored.
        /// </remarks>
        public string ReadDoubleQuotedWord()
        {
            string word = string.Empty;
            ReadToken("\"");
            NextToken(false);
            while (GetStringValue() != "\"")
            {
                word = word + this.GetStringValue();
                NextToken(false);
            }
            return word;
        }

        #endregion

    }
}