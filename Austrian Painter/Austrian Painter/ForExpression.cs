using Painter;
using System.Collections.Generic;

namespace Austrian_Painter
{
    // FOR 6 { ... }   -> entspricht dem REPEAT beim Roboter
    internal class ForExpression : Expression
    {
        private int count;
        private Expression block = new BlockExpression();

        internal override void Parse(List<Token> tokens)
        {
            if (tokens.Count > 0)
            {
                if (tokens[0].Type == Token.TokenType.Number)
                {
                    count = int.Parse(tokens[0].Value);
                    tokens.RemoveAt(0);
                    block.Parse(tokens);
                }
                else
                {
                    Errors.Add("Unexpected Token Type " + tokens[0].Type + ", expected Number");
                }
            }
            else
            {
                Errors.Add("Unexpected end of ForExpression, expected Number");
            }
        }

        internal override void Run(PainterControl painter)
        {
            for (int i = 0; i < count; i++)
            {
                block.Run(painter);
            }
        }
    }
}
