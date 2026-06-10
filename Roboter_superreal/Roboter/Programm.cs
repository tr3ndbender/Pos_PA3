using AbcRobotCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roboter
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
                        case "MOVE":
                            expression = new MoveExpression();
                            break;

                        case "REPEAT":
                            expression = new RepeatExpression();
                            break;

                        case "COLLECT":
                            expression = new CollectExpression();
                            break;
                    }
                    if(expression == null)
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

        internal override void Run(RobotField robot)
        {
            foreach (Expression e in expressions)
            {
                e.Run(robot);
            }
        }

    }
}
