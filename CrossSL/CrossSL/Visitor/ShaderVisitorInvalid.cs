using System.Text;
using ICSharpCode.NRefactory.CSharp;

namespace CrossSL
{
    internal abstract partial class ShaderVisitor
    {
        public StringBuilder VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpr)
        {
            var instr = GetInstructionFromStmt(anonymousMethodExpr.GetParent<Statement>());
            Helper.Error("Anonymous method expressions are not supported", instr);
            return new StringBuilder();
        }

        public StringBuilder VisitUndocumentedExpression(UndocumentedExpression undocumentedExpr)
        {
            var instr = GetInstructionFromStmt(undocumentedExpr.GetParent<Statement>());
            Helper.Error("Non-standard language extensions are not supported", instr);
            return new StringBuilder();
        }

        public StringBuilder VisitAsExpression(AsExpression asExpr)
        {
            var instr = GetInstructionFromStmt(asExpr.GetParent<Statement>());
            Helper.Error("Type casts with keyword 'as' are not supported", instr);
            return new StringBuilder();
        }
    }
}
