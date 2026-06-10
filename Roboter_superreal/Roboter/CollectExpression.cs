
using AbcRobotCore;

namespace Roboter
{
    internal class CollectExpression : Expression
    {
        internal override void Parse(List<Token> tokens)
        {
            
        }

        internal override void Run(RobotField robot)
        {
            string letter = robot.Collect();
            if (letter == null || letter == "")
            {
                Errors.Add("CollectExpression: No item to collect");
            }
        }
    }
}