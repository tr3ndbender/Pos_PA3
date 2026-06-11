using Painter;
using System.Collections.Generic;

namespace Austrian_Painter
{
    internal abstract class Expression
    {
        // Sammelt alle Fehler aus Parsen und Ausführung
        internal static List<string> Errors { get; set; } = new List<string>();

        internal abstract void Parse(List<Token> tokens);

        internal virtual void Run(PainterControl painter) { }
    }
}
