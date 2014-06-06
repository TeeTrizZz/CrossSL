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

        public static StringBuilder Space(this StringBuilder value)
        {
            return value.Append(" ");
        }

        public static StringBuilder Dot(this StringBuilder value)
        {
            return value.Append(".");
        }

        public static StringBuilder OBraces(this StringBuilder value)
        {
            return value.Append("{");
        }

        public static StringBuilder CBraces(this StringBuilder value)
        {
            return value.Append("}");
        }

        public static StringBuilder OParent(this StringBuilder value)
        {
            return value.Append("(");
        }

        public static StringBuilder CParent(this StringBuilder value)
        {
            return value.Append(")");
        }

        public static StringBuilder Semicolon(this StringBuilder value)
        {
            return value.Append(";");
        }

        public static StringBuilder Assign(this StringBuilder value, String op = "")
        {
            return value.Append(" " + op + "= ");
        }

        public static StringBuilder Intend(this StringBuilder value, int level = 1)
        {
            for (var i = 0; i < level; i++)
                value.Append("\t");

            return value;
        }

        public static StringBuilder Method(this StringBuilder value, string name, params string[] args)
        {
            value.Append(name).Append("(");
            value.Append(String.Join(", ", args));
            return value.Append(")");
        }
    }
}