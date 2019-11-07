﻿using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using SqlAnalyzer.Net.Parsers;

namespace SqlAnalyzer.Net.Rules
{
    public static class ParametersMatchingRule
    {
        public const string DiagnosticId = "SQL002";

        public const string MessageFormatCsharpArgumentNotFound = "Argument not found for SQL variable '{0}'";

        public const string MessageFormatSqlVariableNotFound = "SQL variable not found for argument '{0}'";

        private const string Category = "API Guidance";

        private const string Title = "SQL parameters mismatch";

        internal static readonly DiagnosticDescriptor CsharpArgumentNotFoundRule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormatCsharpArgumentNotFound,
            Category,
            DiagnosticSeverity.Warning,
            true,
            Title,
            "https://github.com/olsh/sql-analyzer-net#sql002-sql-parameters-mismatch");

        internal static readonly DiagnosticDescriptor SqlParameterNotFoundRule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormatSqlVariableNotFound,
            Category,
            DiagnosticSeverity.Warning,
            true,
            Title);

        public static void TryReportDiagnostics(
            string sqlText,
            ICollection<string> sharpParameters,
            Location location,
            SyntaxNodeAnalysisContext context)
        {
            if (string.IsNullOrEmpty(sqlText))
            {
                return;
            }

            if (sharpParameters == null)
            {
                return;
            }

            const char SqlVariableDeclarationSymbol = '@';
            var sqlVariables = SqlParser.FindParameters(sqlText).Select(v => v.Trim(SqlVariableDeclarationSymbol)).ToList();
            sharpParameters = sharpParameters.Select(p => p.Trim(SqlVariableDeclarationSymbol)).ToList();

            foreach (var notFoundArgument in sqlVariables.Except(
                sharpParameters,
                StringComparer.InvariantCultureIgnoreCase))
            {
                context.ReportDiagnostic(Diagnostic.Create(CsharpArgumentNotFoundRule, location, notFoundArgument));
            }

            foreach (var notFoundVariable in sharpParameters.Except(
                sqlVariables,
                StringComparer.InvariantCultureIgnoreCase))
            {
                context.ReportDiagnostic(Diagnostic.Create(SqlParameterNotFoundRule, location, notFoundVariable));
            }
        }
    }
}
