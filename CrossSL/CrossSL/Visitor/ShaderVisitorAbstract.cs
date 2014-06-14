using System.Text;
using ICSharpCode.NRefactory.CSharp;

namespace CrossSL
{
    internal abstract partial class ShaderVisitor
    {
        public abstract StringBuilder VisitAssignmentExpression(AssignmentExpression assignmentExpr);

        public abstract StringBuilder VisitBaseReferenceExpression(BaseReferenceExpression baseRefExpr);

        public abstract StringBuilder VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOpExpr);

        public abstract StringBuilder VisitDirectionExpression(DirectionExpression directionExpr);

        public abstract StringBuilder VisitIdentifierExpression(IdentifierExpression identifierExpr);

        public abstract StringBuilder VisitInvocationExpression(InvocationExpression invocationExpr);

        public abstract StringBuilder VisitMemberReferenceExpression(MemberReferenceExpression memberRefExpr);

        public abstract StringBuilder VisitObjectCreateExpression(ObjectCreateExpression objCreateExpr);

        public abstract StringBuilder VisitPrimitiveExpression(PrimitiveExpression primitiveExpr);

        public abstract StringBuilder VisitTypeReferenceExpression(TypeReferenceExpression typeRefExpr);

        public abstract StringBuilder VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOpExpr);

        public abstract StringBuilder VisitBlockStatement(BlockStatement blockStatement);

        public abstract StringBuilder VisitExpressionStatement(ExpressionStatement exprStmt);

        public abstract StringBuilder VisitIfElseStatement(IfElseStatement ifElseStmt);

        public abstract StringBuilder VisitReturnStatement(ReturnStatement returnStmt);

        public abstract StringBuilder VisitVariableDeclarationStatement(VariableDeclarationStatement varDeclStmt);

        public abstract StringBuilder VisitVariableInitializer(VariableInitializer variableInitializer);

        public abstract StringBuilder VisitSimpleType(SimpleType simpleType);

        public abstract StringBuilder VisitPrimitiveType(PrimitiveType primitiveType);
    }
}