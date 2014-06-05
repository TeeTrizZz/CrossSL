using System;
using System.Text;

namespace CrossSL
{
    public static class ExtensionMethods
    {
        public static StringBuilder NewLine(this StringBuilder value)
        {
            return value.Append(Environment.NewLine);
        }

        public static StringBuilder Open(this StringBuilder value)
        {
            return value.Append("{");
        }

        public static StringBuilder Close(this StringBuilder value)
        {
            return value.Append("}");
        }

        public static StringBuilder Space(this StringBuilder value)
        {
            return value.Append(" ");
        }

        public static StringBuilder Semicolon(this StringBuilder value)
        {
            return value.Append(";");
        }

        public static StringBuilder Assign(this StringBuilder value)
        {
            return value.Append(" = ");
        }

        public static StringBuilder Assign(this StringBuilder value, String op)
        {
            return value.Append(" " + op + "= ");
        }

        public static StringBuilder Intend(this StringBuilder value, int level)
        {
            for (var i = 0; i < level; i++)
                value.Append("\t");

            return value;
        }
    }
}
