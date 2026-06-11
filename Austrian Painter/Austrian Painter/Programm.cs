using Painter;
using System.Collections.Generic;

namespace Austrian_Painter
{
    internal class Programm : Expression
    {
        private List<Expression> expressions = new();

        internal override void Parse(List<Token> tokens)
        {
            while (tokens.Count > 0 && tokens[0].Type != Token.TokenType.CloseBracket)
            {
                Token token = tokens[0];
                if (token.Type == Token.TokenType.Keyword)
                {
                    Expression expression = null;
                    switch (token.Value)
                    {
                        case "TURN":
                            expression = new TurnExpression();
                            break;

                        case "FOR":
                            expression = new ForExpression();
                            break;

                        case "DRAW":
                            expression = new DrawExpression();
                            break;
                        case "COLOR":
                            expression = new ColorExpression();
                            break;
                    }
                    if (expression == null)
                    {
                        //Fehler
                        Errors.Add("Programm: Unexpected Keyword " + token.Value);
                    }
                    else
                    {
                        tokens.RemoveAt(0);
                        expression.Parse(tokens);
                        expressions.Add(expression);
                    }
                }
                else
                {
                    //Fehler
                    Errors.Add("Unexpected Token Type " + token.Type);
                    tokens.RemoveAt(0);
                }
            }
        }

        internal override void Run(PainterControl painter)
        {
            foreach (Expression e in expressions)
            {
                e.Run(painter);
            }
        }

    }
}
