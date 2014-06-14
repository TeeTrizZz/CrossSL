using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrossSL.Meta;
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
    internal abstract partial class ShaderVisitor : IAstVisitor<StringBuilder>
    {
        protected DecompilerContext DecContext;
        protected AstNode TopNode;

        protected internal StringBuilder Result { get; protected set; }

        protected internal Collection<MethodDefinition> RefMethods { get; protected set; }
        protected internal Collection<VariableDesc> RefVariables { get; protected set; }

        internal static ShaderVisitor GetTranslator(ShaderTarget target, AstNode methodBody, DecompilerContext decContext)
        {
            switch (target.Envr)
            {
                case xSLEnvironment.OpenGL:
                    switch ((xSLTarget.GLSL)target.VersionID)
                    {
                        case xSLTarget.GLSL.V110:
                            return new GLSLVisitor110(methodBody, decContext);

                        default:
                            return new GLSLVisitor(methodBody, decContext);
                    }

                case xSLEnvironment.OpenGLES:
                    return new GLSLVisitor110(methodBody, decContext);

                case xSLEnvironment.OpenGLMix:
                    return new GLSLVisitor110(methodBody, decContext);
            }

            return null;
        }

        internal ShaderVisitor(AstNode methodBody, DecompilerContext decContext)
        {
            DecContext = decContext;
            TopNode = methodBody;

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

        internal void Translate()
        {
            Result = new StringBuilder().Block(TopNode.AcceptVisitor(this));
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

        protected StringBuilder JoinArgs(ICollection<Expression> args)
        {
            var accArgs = args.Select(arg => arg.AcceptVisitor(this).ToString());
            return new StringBuilder(String.Join(", ", accArgs));
        }

        protected string MapDataTypeIfValid(AstType node, Type type)
        {
            if (type != null && xSLTypeMapping.Types.ContainsKey(type))
                return xSLTypeMapping.Types[type];

            var instr = GetInstructionFromStmt(node.GetParent<Statement>());
            Helper.Error("Type '" + type + "' is not supported", instr);

            return null;
        }

        public StringBuilder VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitCastExpression(CastExpression castExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitCheckedExpression(CheckedExpression checkedExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitConditionalExpression(ConditionalExpression conditionalExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitIndexerExpression(IndexerExpression indexerExpression)
        {
            throw new NotImplementedException();
        }

    
        public StringBuilder VisitIsExpression(IsExpression isExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitLambdaExpression(LambdaExpression lambdaExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitNamedExpression(NamedExpression namedExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitAnonymousTypeCreateExpression(
            AnonymousTypeCreateExpression anonymousTypeCreateExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitSizeOfExpression(SizeOfExpression sizeOfExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitStackAllocExpression(StackAllocExpression stackAllocExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitTypeOfExpression(TypeOfExpression typeOfExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitUncheckedExpression(UncheckedExpression uncheckedExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitEmptyExpression(EmptyExpression emptyExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQueryExpression(QueryExpression queryExpression)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQueryFromClause(QueryFromClause queryFromClause)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQueryLetClause(QueryLetClause queryLetClause)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQueryWhereClause(QueryWhereClause queryWhereClause)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQueryJoinClause(QueryJoinClause queryJoinClause)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQueryOrderClause(QueryOrderClause queryOrderClause)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQueryOrdering(QueryOrdering queryOrdering)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQuerySelectClause(QuerySelectClause querySelectClause)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitQueryGroupClause(QueryGroupClause queryGroupClause)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitAttribute(Attribute attribute)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitAttributeSection(AttributeSection attributeSection)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitTypeDeclaration(TypeDeclaration typeDeclaration)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitUsingAliasDeclaration(UsingAliasDeclaration usingAliasDeclaration)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitUsingDeclaration(UsingDeclaration usingDeclaration)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitBreakStatement(BreakStatement breakStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitCheckedStatement(CheckedStatement checkedStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitContinueStatement(ContinueStatement continueStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitDoWhileStatement(DoWhileStatement doWhileStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitEmptyStatement(EmptyStatement emptyStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitFixedStatement(FixedStatement fixedStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitForeachStatement(ForeachStatement foreachStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitForStatement(ForStatement forStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitGotoDefaultStatement(GotoDefaultStatement gotoDefaultStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitGotoStatement(GotoStatement gotoStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitLabelStatement(LabelStatement labelStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitLockStatement(LockStatement lockStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitSwitchStatement(SwitchStatement switchStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitSwitchSection(SwitchSection switchSection)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitCaseLabel(CaseLabel caseLabel)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitThrowStatement(ThrowStatement throwStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitTryCatchStatement(TryCatchStatement tryCatchStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitCatchClause(CatchClause catchClause)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitUncheckedStatement(UncheckedStatement uncheckedStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitUnsafeStatement(UnsafeStatement unsafeStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitUsingStatement(UsingStatement usingStatement)
        {
            throw new NotImplementedException();
        }
 
        public StringBuilder VisitWhileStatement(WhileStatement whileStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitYieldReturnStatement(YieldReturnStatement yieldReturnStatement)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitAccessor(Accessor accessor)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitConstructorInitializer(ConstructorInitializer constructorInitializer)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitEventDeclaration(EventDeclaration eventDeclaration)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitCustomEventDeclaration(CustomEventDeclaration customEventDeclaration)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitMethodDeclaration(MethodDeclaration methodDeclaration)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitFixedVariableInitializer(FixedVariableInitializer fixedVariableInitializer)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitSyntaxTree(SyntaxTree syntaxTree)
        {
            throw new NotImplementedException();
        }


        public StringBuilder VisitMemberType(MemberType memberType)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitComposedType(ComposedType composedType)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitArraySpecifier(ArraySpecifier arraySpecifier)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitComment(Comment comment)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitNewLine(NewLineNode newLineNode)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitWhitespace(WhitespaceNode whitespaceNode)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitText(TextNode textNode)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitPreProcessorDirective(PreProcessorDirective preProcessorDirective)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitDocumentationReference(DocumentationReference documentationReference)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitConstraint(Constraint constraint)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitCSharpTokenNode(CSharpTokenNode cSharpTokenNode)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitIdentifier(Identifier identifier)
        {
            throw new NotImplementedException();
        }

        public StringBuilder VisitPatternPlaceholder(AstNode placeholder, Pattern pattern)
        {
            throw new NotImplementedException();
        }
    }
}