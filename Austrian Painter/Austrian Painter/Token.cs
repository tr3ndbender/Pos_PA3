using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Austrian_Painter
{
    internal class Token
    {
        public enum TokenType { Keyword, Number, Direction, Color, OpenBracket, CloseBracket, Error }
        public string Value { get; set; }
        public TokenType Type { get; set; } = TokenType.Error;

    }
}
