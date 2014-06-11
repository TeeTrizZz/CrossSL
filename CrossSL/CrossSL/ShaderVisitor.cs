using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Attribute = ICSharpCode.NRefactory.CSharp.Attribute;

namespace CrossSL
{
    internal abstract class ShaderVisitor : IAstVisitor<int, StringBuilder>
    {
        protected DecompilerContext DecContext;

        public StringBuilder Result { get; protected set; }

        public Collection<MethodDefinition> RefMethods { get; protected set; }
        public Collection<VariableDesc> RefVariables { get; protected set; } 

        internal ShaderVisitor(AstNode methodBody, DecompilerContext decContext)
        {
            DecContext = decContext;

            // replaces every "x = Plus(x, y)" by "x += y", etc.
            var transform1 = (IAstTransform) new ReplaceMethodCallsWithOperators(decContext);
            transform1.Run(methodBody);

            // replaces every "!(x == 5)" by "(x != 5)"
            var transform2 = (IAstTransform) new PushNegation();
            transform2.Run(methodBody);

            // replaces every "var x; x = 5;" by "var x = 5;"
            var transform3 = (IAstTransform) new DeclareVariables(decContext);
            transform3.Run(methodBody);

            RefMethods = new Collection<MethodDefinition>();
            RefVariables = new Collection<VariableDesc>();
        }

        protected T GetAnnotations<T>(Statement stmt) where T : class
        {
            var typeRoleMap = new Dictionary<Type, int>
            {
                {typeof (ExpressionStatement), 0},
                {typeof (VariableDeclarationStatement), 1},
                {typeof (IfElseStatement), 2}
            };

            switch (typeRoleMap[stmt.GetType()])
            {
                case 0:
                    return stmt.GetChildByRole(Roles.Expression).Annotation<T>();
                case 1:
                    return stmt.GetChildByRole(Roles.Variable).Annotation<T>();
                case 2:
                    return stmt.GetChildByRole(Roles.Condition).Annotation<T>();
                default:
                    throw new ArgumentException("Statement type " + stmt.GetType() + " not supported.");
            }
        }

        protected Instruction GetInstructionFromStmt(Statement stmt)
        {
            if (stmt == null) return null;

            var ilRange = GetAnnotations<List<ILRange>>(stmt).First();
            var instructions = DecContext.CurrentMethod.Body.Instructions;
            return instructions.First(il => il.Offset == ilRange.From);
        }

        protected int GetLineNmbrFromStmt(Statement stmt)
        {
            var seqPoint = GetInstructionFromStmt(stmt).SequencePoint;
            return (seqPoint == null) ? 0 : seqPoint.StartLine;
        }

        protected StringBuilder JoinArgs(ICollection<Expression> args)
        {
            var accArgs = args.Select(arg => arg.AcceptVisitor(this, 0).ToString());
            return new StringBuilder(String.Join(", ", accArgs));
        }

        protected string MapDataTypeIfValid(AstType node, Type type)
        {
            if (type != null && xSLDataType.Types.ContainsKey(type))
                return xSLDataType.Types[type];

            var instr = GetInstructionFromStmt(node.GetParent<Statement>());
            Helper.Error("Type '" + type + "' is not supported", instr);

            return null;
        }

        public StringBuilder VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression,
            int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression,
            int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitAsExpression(AsExpression asExpression, int data)
        {
            throw new NotImplementedException();
        }

        public abstract StringBuilder VisitAssignmentExpression(AssignmentExpression assignmentExpr, int data);

        public abstract StringBuilder VisitBaseReferenceExpression(BaseReferenceExpression baseRefExpr, int data);

        public abstract StringBuilder VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOpExpr, int data);

        public StringBuilder VisitCastExpression(CastExpression castExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitCheckedExpression(CheckedExpression checkedExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitConditionalExpression(ConditionalExpression conditionalExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression, int data)
        {
            throw new NotImplementedException();
        }

        public abstract StringBuilder VisitDirectionExpression(DirectionExpression directionExpr, int data);
        
        public abstract StringBuilder VisitIdentifierExpression(IdentifierExpression identifierExpr, int data);

        public StringBuilder VisitIndexerExpression(IndexerExpression indexerExpression, int data)
        {
            throw new NotImplementedException();
        }

        public abstract StringBuilder VisitInvocationExpression(InvocationExpression invocationExpr, int data);

        public StringBuilder VisitIsExpression(IsExpression isExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitLambdaExpression(LambdaExpression lambdaExpression, int data)
        {
            throw new NotImplementedException();
        }

        public abstract StringBuilder VisitMemberReferenceExpression(MemberReferenceExpression memberRefExpr, int data);

        public StringBuilder VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitNamedExpression(NamedExpression namedExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression, int data)
        {
            throw new NotImplementedException();
        }

        public abstract StringBuilder VisitObjectCreateExpression(ObjectCreateExpression objCreateExpr, int data);

        public StringBuilder VisitAnonymousTypeCreateExpression(
            AnonymousTypeCreateExpression anonymousTypeCreateExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression,
            int data)
        {
            throw new NotImplementedException();
        }

        public abstract StringBuilder VisitPrimitiveExpression(PrimitiveExpression primitiveExpr, int data);

        public StringBuilder VisitSizeOfExpression(SizeOfExpression sizeOfExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitStackAllocExpression(StackAllocExpression stackAllocExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitTypeOfExpression(TypeOfExpression typeOfExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression, int data)
        {
            throw new NotImplementedException();
        }

        public abstract StringBuilder VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOpExpr, int data);

        public StringBuilder VisitUncheckedExpression(UncheckedExpression uncheckedExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitEmptyExpression(EmptyExpression emptyExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQueryExpression(QueryExpression queryExpression, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQueryFromClause(QueryFromClause queryFromClause, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQueryLetClause(QueryLetClause queryLetClause, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQueryWhereClause(QueryWhereClause queryWhereClause, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQueryJoinClause(QueryJoinClause queryJoinClause, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQueryOrderClause(QueryOrderClause queryOrderClause, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQueryOrdering(QueryOrdering queryOrdering, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQuerySelectClause(QuerySelectClause querySelectClause, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQueryGroupClause(QueryGroupClause queryGroupClause, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitAttribute(Attribute attribute, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitAttributeSection(AttributeSection attributeSection, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitTypeDeclaration(TypeDeclaration typeDeclaration, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitUsingAliasDeclaration(UsingAliasDeclaration usingAliasDeclaration, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitUsingDeclaration(UsingDeclaration usingDeclaration, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration, int data)
        {
            throw new NotImplementedException();
        }

        public abstract StringBuilder VisitBlockStatement(BlockStatement blockStatement, int data);

        public StringBuilder VisitBreakStatement(BreakStatement breakStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitCheckedStatement(CheckedStatement checkedStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitContinueStatement(ContinueStatement continueStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitDoWhileStatement(DoWhileStatement doWhileStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitEmptyStatement(EmptyStatement emptyStatement, int data)
        {
            throw new NotImplementedException();
        }

        public abstract StringBuilder VisitExpressionStatement(ExpressionStatement exprStmt, int data);

        public StringBuilder VisitFixedStatement(FixedStatement fixedStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitForeachStatement(ForeachStatement foreachStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitForStatement(ForStatement forStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitGotoDefaultStatement(GotoDefaultStatement gotoDefaultStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitGotoStatement(GotoStatement gotoStatement, int data)
        {
            throw new NotImplementedException();
        }

        public abstract StringBuilder VisitIfElseStatement(IfElseStatement ifElseStmt, int data);

        public StringBuilder VisitLabelStatement(LabelStatement labelStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitLockStatement(LockStatement lockStatement, int data)
        {
            throw new NotImplementedException();
        }

        public abstract StringBuilder VisitReturnStatement(ReturnStatement returnStmt, int data);

        public StringBuilder VisitSwitchStatement(SwitchStatement switchStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitSwitchSection(SwitchSection switchSection, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitCaseLabel(CaseLabel caseLabel, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitThrowStatement(ThrowStatement throwStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitTryCatchStatement(TryCatchStatement tryCatchStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitCatchClause(CatchClause catchClause, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitUncheckedStatement(UncheckedStatement uncheckedStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitUnsafeStatement(UnsafeStatement unsafeStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitUsingStatement(UsingStatement usingStatement, int data)
        {
            throw new NotImplementedException();
        }

        public abstract StringBuilder VisitVariableDeclarationStatement(
            VariableDeclarationStatement varDeclStmt, int data);

        public StringBuilder VisitWhileStatement(WhileStatement whileStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitYieldReturnStatement(YieldReturnStatement yieldReturnStatement, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitAccessor(Accessor accessor, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitConstructorInitializer(ConstructorInitializer constructorInitializer, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitEventDeclaration(EventDeclaration eventDeclaration, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitCustomEventDeclaration(CustomEventDeclaration customEventDeclaration, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitFieldDeclaration(FieldDeclaration fieldDeclaration, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitMethodDeclaration(MethodDeclaration methodDeclaration, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitParameterDeclaration(ParameterDeclaration parameterDeclaration, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, int data)
        {
            throw new NotImplementedException();
        }

        public abstract StringBuilder VisitVariableInitializer(VariableInitializer variableInitializer, int data);

        public StringBuilder VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitFixedVariableInitializer(FixedVariableInitializer fixedVariableInitializer, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitSyntaxTree(SyntaxTree syntaxTree, int data)
        {
            throw new NotImplementedException();
        }

        public abstract StringBuilder VisitSimpleType(SimpleType simpleType, int data);

        public StringBuilder VisitMemberType(MemberType memberType, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitComposedType(ComposedType composedType, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitArraySpecifier(ArraySpecifier arraySpecifier, int data)
        {
            throw new NotImplementedException();
        }

        public abstract StringBuilder VisitPrimitiveType(PrimitiveType primitiveType, int data);

        public StringBuilder VisitComment(Comment comment, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitNewLine(NewLineNode newLineNode, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitWhitespace(WhitespaceNode whitespaceNode, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitText(TextNode textNode, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitPreProcessorDirective(PreProcessorDirective preProcessorDirective, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitDocumentationReference(DocumentationReference documentationReference, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitConstraint(Constraint constraint, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitCSharpTokenNode(CSharpTokenNode cSharpTokenNode, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitIdentifier(Identifier identifier, int data)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitPatternPlaceholder(AstNode placeholder, Pattern pattern, int data)
        {
            throw new NotImplementedException();
        }
    }
}