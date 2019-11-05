using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using SqlAnalyzer.Net.Extensions;
using SqlAnalyzer.Net.Parsers;
using SqlAnalyzer.Net.Walkers;

namespace SqlAnalyzer.Net
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DapperParametersMatchingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SQL002";

        public const string MessageFormatCsharpArgumentNotFound = "Argument not found for SQL variable '{0}'";

        public const string MessageFormatSqlVariableNotFound = "SQL variable not found for argument '{0}'";

        private const string Category = "API Guidance";

        private static readonly string Title = "SQL parameters mismatch";

        private static readonly DiagnosticDescriptor CsharpArgumentNotFoundRule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormatCsharpArgumentNotFound,
            Category,
            DiagnosticSeverity.Warning,
            true,
            Title,
            "https://github.com/olsh/sql-analyzer-net#sql002-sql-parameters-mismatch");

        private static readonly DiagnosticDescriptor SqlParameterNotFoundRule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormatSqlVariableNotFound,
            Category,
            DiagnosticSeverity.Warning,
            true,
            Title);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(CsharpArgumentNotFoundRule, SqlParameterNotFoundRule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }

        private static ICollection<string> FindParameters(SyntaxNodeAnalysisContext context, ArgumentSyntax argument)
        {
            var symbol = context.SemanticModel.GetSymbolInfo(argument.Expression).Symbol;
            if (symbol == null)
            {
                return null;
            }

            if (symbol is IMethodSymbol methodSymbol)
            {
                return methodSymbol.Parameters.Select(p => p.Name.Trim('@')).ToList();
            }

            if (symbol is ILocalSymbol localSymbol && localSymbol.IsDapperDynamicParameter(context.SemanticModel))
            {
                var methodDeclarationSyntax = argument
                    .Expression
                    .AncestorsAndSelf()
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault();
                if (methodDeclarationSyntax == null)
                {
                    return null;
                }

                var dapperAddInvocationExpressionWalker = new DapperAddInvocationExpressionWalker(symbol.Name);
                dapperAddInvocationExpressionWalker.Visit(methodDeclarationSyntax.Body);
                if (!dapperAddInvocationExpressionWalker.IsAllParametersStatic)
                {
                    return null;
                }

                return dapperAddInvocationExpressionWalker.SqlParameters.Select(p => p.Trim('@')).ToList();
            }

            return null;
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

        private void ReportDiagnostics(
            SyntaxNodeAnalysisContext context,
            InvocationExpressionSyntax invocationExpressionSyntax)
        {
            ICollection<string> sqlVariables = null;
            ICollection<string> sharpParameters = null;
            foreach (var argument in invocationExpressionSyntax.ArgumentList.Arguments)
            {
                var parameter = argument.DetermineParameter(context.SemanticModel);
                if (string.Equals(parameter.Name, "sql"))
                {
                    var sourceText = argument.TryGetArgumentStringValue(context.SemanticModel);

                    // If SQL code is not constant, return
                    if (sourceText == null)
                    {
                        return;
                    }

                    sqlVariables = SqlParser.FindParameters(sourceText);

                    continue;
                }

                if (string.Equals(parameter.Name, "param"))
                {
                    sharpParameters = FindParameters(context, argument);
                }
            }

            if (sharpParameters == null || sqlVariables == null)
            {
                return;
            }

            foreach (var notFoundArgument in sqlVariables.Except(
                sharpParameters,
                StringComparer.InvariantCultureIgnoreCase))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        CsharpArgumentNotFoundRule,
                        invocationExpressionSyntax.GetLocation(),
                        notFoundArgument));
            }

            foreach (var notFoundVariable in sharpParameters.Except(
                sqlVariables,
                StringComparer.InvariantCultureIgnoreCase))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        SqlParameterNotFoundRule,
                        invocationExpressionSyntax.GetLocation(),
                        notFoundVariable));
            }
        }
    }
}
