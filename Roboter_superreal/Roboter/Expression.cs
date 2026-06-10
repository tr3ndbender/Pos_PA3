using AbcRobotCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roboter
{
    internal abstract class Expression
    {
        internal static List<string> Errors {  get; set; } = new List<string>();
        internal abstract void Parse(List<Token> tokens);

        internal virtual void Run(RobotField robot) { } 
    }
}
