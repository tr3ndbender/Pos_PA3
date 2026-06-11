using Painter;
using System.Collections.Generic;

namespace Austrian_Painter
{
    // COLOR Red
    internal class ColorExpression : Expression
    {
        private Token color;

        internal override void Parse(List<Token> tokens)
        {
            if (tokens.Count > 0)
            {
                if (tokens[0].Type == Token.TokenType.Color)
                {
                    color = tokens[0];
                    tokens.RemoveAt(0);
                }
                else
                {
                    Errors.Add("Unexpected Token Type " + tokens[0].Type + ", expected Color");
                }
            }
            else
            {
                Errors.Add("Unexpected end of ColorExpression, expected Color");
            }
        }

        internal override void Run(PainterControl painter)
        {
            painter.ChangeColor(color.Value);
        }
    }
}
