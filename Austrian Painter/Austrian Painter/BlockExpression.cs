using Painter;
using System.Collections.Generic;

namespace Austrian_Painter
{
    // Ein Block { ... } fasst mehrere Anweisungen zusammen
    internal class BlockExpression : Expression
    {
        private Programm programm = new Programm();

        internal override void Parse(List<Token> tokens)
        {
            if (tokens.Count > 0)
            {
                if (tokens[0].Type == Token.TokenType.OpenBracket)
                {
                    tokens.RemoveAt(0);          // { entfernen
                    programm.Parse(tokens);      // Inhalt parsen (stoppt bei })

                    if (tokens.Count > 0)
                    {
                        if (tokens[0].Type == Token.TokenType.CloseBracket)
                        {
                            tokens.RemoveAt(0);  // } entfernen
                        }
                        else
                        {
                            Errors.Add("Unexpected Token Type " + tokens[0].Type + ", expected }");
                        }
                    }
                    else
                    {
                        Errors.Add("Unexpected end of BlockExpression, expected }");
                    }
                }
                else
                {
                    Errors.Add("Unexpected Token Type " + tokens[0].Type + ", expected {");
                }
            }
            else
            {
                Errors.Add("Unexpected end of BlockExpression, expected {");
            }
        }

        internal override void Run(PainterControl painter)
        {
            programm.Run(painter);
        }
    }
}
