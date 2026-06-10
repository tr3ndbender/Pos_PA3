
using AbcRobotCore;

namespace Roboter
{
    internal class BlockExpression : Expression
    {
        private Programm _programm = new Programm();
        internal override void Parse(List<Token> tokens)
        {
            if (tokens.Count > 0)
            {
                if (tokens[0].Type == Token.TokenType.OpenBracket)
                {
                    tokens.RemoveAt(0);
                    _programm.Parse(tokens);
                    if(tokens.Count > 0)
                    {
                        if (tokens[0].Type == Token.TokenType.CloseBracket)
                        {
                            tokens.RemoveAt(0);
                        }
                        else
                        {
                            //Fehler
                            Errors.Add("Unexpected Token Type" + tokens[0].Type + ", expected Direction");
                        }
                    }
                    else
                    {
                        //Fehler
                        Errors.Add("Unexpected end of BlockExpression, expected }");
                    }
                }
                else
                {
                    //Fehler
                    Errors.Add("Unexpected Token Type " + tokens[0].Type + ", expected Direction");
                }
            }
            else
            {
                //Fehler
                Errors.Add("Unexpected end of BlockExpression, expected {");
            }
        }

        internal override void Run (RobotField robot)
        {
            _programm.Run(robot);
        }
    }
}