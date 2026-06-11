using Painter;
using System.Collections.Generic;

namespace Austrian_Painter
{
    // TURN LEFT 45   /   TURN RIGHT 90
    internal class TurnExpression : Expression
    {
        private Token direction;
        private int angle;

        internal override void Parse(List<Token> tokens)
        {
            // 1. die Richtung (LEFT / RIGHT)
            if (tokens.Count > 0)
            {
                if (tokens[0].Type == Token.TokenType.Direction)
                {
                    direction = tokens[0];
                    tokens.RemoveAt(0);
                }
                else
                {
                    Errors.Add("Unexpected Token Type " + tokens[0].Type + ", expected Direction");
                    return;
                }
            }
            else
            {
                Errors.Add("Unexpected end of TurnExpression, expected Direction");
                return;
            }

            // 2. der Winkel (eine Zahl)
            if (tokens.Count > 0)
            {
                if (tokens[0].Type == Token.TokenType.Number)
                {
                    angle = int.Parse(tokens[0].Value);
                    tokens.RemoveAt(0);
                }
                else
                {
                    Errors.Add("Unexpected Token Type " + tokens[0].Type + ", expected Number");
                }
            }
            else
            {
                Errors.Add("Unexpected end of TurnExpression, expected Number");
            }
        }

        internal override void Run(PainterControl painter)
        {
            // RIGHT = im Uhrzeigersinn (positiv), LEFT = gegen den Uhrzeigersinn (negativ)
            if (direction.Value == "RIGHT")
            {
                painter.Rotate(angle);
            }
            else
            {
                painter.Rotate(-angle);
            }
        }
    }
}
