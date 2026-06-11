using Painter;
using System.Collections.Generic;

namespace Austrian_Painter
{
    // DRAW 250
    internal class DrawExpression : Expression
    {
        private int length;

        internal override void Parse(List<Token> tokens)
        {
            if (tokens.Count > 0)
            {
                if (tokens[0].Type == Token.TokenType.Number)
                {
                    length = int.Parse(tokens[0].Value);
                    tokens.RemoveAt(0);
                }
                else
                {
                    Errors.Add("Unexpected Token Type " + tokens[0].Type + ", expected Number");
                }
            }
            else
            {
                Errors.Add("Unexpected end of DrawExpression, expected Number");
            }
        }

        internal override void Run(PainterControl painter)
        {
            painter.Draw(length);
        }
    }
}
