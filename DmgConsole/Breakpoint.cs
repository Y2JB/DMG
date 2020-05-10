using System;
using System.Collections.Generic;
using System.Text;

namespace DmgConsole
{
    public class Breakpoint
    {
        public ushort Address { get; set; }
        public ConditionalExpression Expression { get; set; }

        public Breakpoint(ushort address)
        {
            Address = address;
        }
        
        public Breakpoint(ushort address, ConditionalExpression expr)
        {
            Address = address;
            Expression = expr;
        }

        public bool ShouldBreak(ushort pc)
        {
            if(pc == Address)
            {
                if(Expression == null)
                {
                    return true;
                }
                return Expression.Evaluate();
            }
            return false;
        }


        public override string ToString()
        {
            string str = String.Format("Breakpoint {0:X4}", Address);
            if(Expression != null)
            {
                str = String.Format("{0} - {1}", str, Expression.ToString());
            }
            return str;
        }
    }
}
