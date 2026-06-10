
using AbcRobotCore;

namespace Roboter
{
    internal class RepeatExpression : Expression
    {
        private int _count;
        private Expression _block = new BlockExpression();
        internal override void Parse(List<Token> tokens)
        {
            if (tokens.Count > 0)
            {
                if (tokens[0].Type == Token.TokenType.Number)
                {
                    _count = int.Parse(tokens[0].Value);
                    tokens.RemoveAt(0);
                    _block.Parse(tokens);
                }
                else
                {
                    //Fehler
                    Errors.Add("Unexpected Token Type " + tokens[0].Type + ", expected Number");
                }
            }
            else
            {
                //Fehler
                Errors.Add("Unexpected end of RepeatExpression, expected Number");
            }
        }

        internal override void Run(RobotField robot)
        {
            for (int i  = 0; i < _count; i++)
            {
                _block.Run(robot);
            }
        }
    }
}