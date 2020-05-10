using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DmgConsole
{
    public class ConditionalExpression
    {
        enum EqualityCheck
        {
            Equal,
            NotEqual,

            Invalid
        }

        ushort lhs, rhs;
        EqualityCheck equalitycheck;


        public ConditionalExpression(string[] terms)
        {
            if (terms.Length != 4)
            {
                throw new ArgumentException("ConditionalExpression arguments wrong. Form must be 'if <x> <==> <y>");
            }

            if (terms[0].Equals("if", StringComparison.OrdinalIgnoreCase) == false) throw new ArgumentException("missing if");

            if (ParseUShortParameter(terms[1], out lhs) == false ||
                ParseUShortParameter(terms[3], out rhs) == false)
            {
                throw new ArgumentException("ConditionalExpression arguments: params incorrect");
            }

            if (ParseEqualityParameter(terms[2], out equalitycheck) == false)
            {
                throw new ArgumentException("ConditionalExpression arguments: Invalid equality check");
            }

        }


        public bool Evaluate()
        {
            switch (equalitycheck)
            {
                case EqualityCheck.Equal:
                    return (lhs == rhs);

                case EqualityCheck.NotEqual:
                    return (lhs != rhs);
            }
            return false;
        }


        // TODO: This should be comomn code 
        bool ParseUShortParameter(string p, out ushort value)
        {
            if (ushort.TryParse(p, out value) == false)
            {
                // Is it hex?
                if (p.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
                {
                    p = p.Substring(2);
                }
                return ushort.TryParse(p, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out value);
            }
            return true;
        }


        bool ParseEqualityParameter(string p, out EqualityCheck value)
        {
            if (p.Equals("=="))
            {
                value = EqualityCheck.Equal;
                return true;
            }

            if (p.Equals("!="))
            {
                value = EqualityCheck.NotEqual;
                return true;
            }

            value = EqualityCheck.Invalid;
            return false;
        }


        public override string ToString()
        {
            return String.Format("{0} {1} {2}", lhs, equalitycheck.ToString(), rhs);
        }
    }
}
