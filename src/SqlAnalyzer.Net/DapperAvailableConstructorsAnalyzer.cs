using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SqlAnalyzer.Net.Extensions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;

namespace SqlAnalyzer.Net
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DapperAvailableConstructorsAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(CsharpNoConstructorFoundRule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }

        public const string DiagnosticId = "SQL006";
        public const string MessageFormatCsharpArgumentNotFound = "No viable constructor found for type '{0}'";
        private const string Category = "API Guidance";
        private const string Title = "SQL parameters mismatch";

        static readonly DiagnosticDescriptor CsharpNoConstructorFoundRule =
            new DiagnosticDescriptor(
                DiagnosticId,
                Title,
                MessageFormatCsharpArgumentNotFound,
                Category,
                DiagnosticSeverity.Error,
                true,
                Title
            );


        private static ICollection<TypeSyntax> FindTypeArguments(MemberAccessExpressionSyntax memberAccessExpressionSyntax)
        {
            var collection = new Collection<TypeSyntax>();
            foreach (var genericName in memberAccessExpressionSyntax.ChildNodes().OfType<GenericNameSyntax>())
            {
                foreach (TypeSyntax item in genericName.TypeArgumentList.Arguments.Where(arg => arg.IsKind(SyntaxKind.IdentifierName)))
                {
                    collection.Add(item);
                }
            }
            return collection;
        }

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocationExpressionSyntax = (InvocationExpressionSyntax)context.Node;

            if (!invocationExpressionSyntax.IsDapperInlineSqlMethod(context.SemanticModel))
            {
                return;
            }

            ReportDiagnostics(context, invocationExpressionSyntax);
        }

        private void ReportDiagnostics(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationExpressionSyntax)
        {
            if (!(invocationExpressionSyntax.Expression is MemberAccessExpressionSyntax expression))
            {
                return;
            }

            var typeArgs = FindTypeArguments(expression);
            foreach (TypeSyntax item in typeArgs)
            {
                var type = context.SemanticModel.GetTypeInfo(item).Type;
                var members = type.GetMembers().Where(member => member.DeclaredAccessibility == Accessibility.Public);
                var properties = members.OfType<IPropertySymbol>();
                var ctors = members.OfType<IMethodSymbol>().Where(method => method.MethodKind == MethodKind.Constructor);

                var anyValidCtor = ctors.Any(ctor =>
                    ctor.Parameters.Length == 0 ||
                    properties.All(prop =>
                        ctor.Parameters.Any(param =>
                            param.Name == prop.Name &&
                            param.Type == prop.Type
                        )
                    )
                );

                if (!anyValidCtor)
                {
                    var diagnostic = Diagnostic.Create(CsharpNoConstructorFoundRule, item.GetLocation(), type);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
