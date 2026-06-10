using AbcRobotCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roboter
{
    internal class MoveExpression : Expression
    {
        private Token direction;
        internal override void Parse(List<Token> tokens)
        {
            if(tokens.Count > 0)
            {
                if (tokens[0].Type == Token.TokenType.Direction)
                {
                    direction = tokens[0];
                    tokens.RemoveAt(0);
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
                Errors.Add("Unexpected end of MoveExpression, expected Direction");
            }
        }

        internal override void Run (RobotField robot)
        {
            bool couldMove = false;
            switch (direction.Value)
            {
                case "LEFT":
                    couldMove = robot.Move(RobotField.Direction.Left);
                    break;
                case "RIGHT":
                    couldMove = robot.Move(RobotField.Direction.Right);
                    break;
                case "UP":
                    couldMove = robot.Move(RobotField.Direction.Up);
                    break;
                case "DOWN":
                    couldMove = robot.Move(RobotField.Direction.Down);
                    break;
            }

            if (!couldMove)
            {
                Errors.Add("MoveExpression: Could not move " + direction.Value);
            }
        }
    }
}
