// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

public partial class RouteHandlerAnalyzer : DiagnosticAnalyzer
{
    private static void DetectMismatchedParameterOptionality(
        in OperationAnalysisContext context,
        RouteUsageModel routeUsage,
        IMethodSymbol methodSymbol)
    {
        var allDeclarations = methodSymbol.GetAllMethodSymbolsOfPartialParts();
        foreach (var method in allDeclarations)
        {
            var parametersInArguments = method.Parameters;
            foreach (var parameter in parametersInArguments)
            {
                var paramName = parameter.Name;
                //  If this is not the methpd parameter associated with the route
                // parameter then continue looking for it in the list

                if (!routeUsage.RoutePattern.TryGetRouteParameter(paramName, out var routeParameter))
                {
                    continue;
                }

                var argumentIsOptional = parameter.IsOptional || parameter.NullableAnnotation != NullableAnnotation.NotAnnotated;
                if (!argumentIsOptional && routeParameter.IsOptional)
                {
                    var location = parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation();
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.DetectMismatchedParameterOptionality,
                        location,
                        paramName));
                }
            }
        }
    }

    internal ref struct RouteTokenEnumerator
    {
        private ReadOnlySpan<char> _routeTemplate;

        public RouteTokenEnumerator(string routeTemplateString)
        {
            _routeTemplate = routeTemplateString.AsSpan();
            CurrentName = default;
            CurrentQualifiers = default;
        }

        public ReadOnlySpan<char> CurrentName { get; private set; }
        public ReadOnlySpan<char> CurrentQualifiers { get; private set; }

        public bool MoveNext()
        {
            if (_routeTemplate.IsEmpty)
            {
                return false;
            }

        findStartBrace:
            var startIndex = _routeTemplate.IndexOf('{');
            if (startIndex == -1)
            {
                return false;
            }

            if (startIndex < _routeTemplate.Length - 1 && _routeTemplate[startIndex + 1] == '{')
            {
                // Escaped sequence
                _routeTemplate = _routeTemplate.Slice(startIndex + 1);
                goto findStartBrace;
            }

            var tokenStart = startIndex + 1;

        findEndBrace:
            var endIndex = IndexOf(_routeTemplate, tokenStart, '}');
            if (endIndex == -1)
            {
                return false;
            }
            if (endIndex < _routeTemplate.Length - 1 && _routeTemplate[endIndex + 1] == '}')
            {
                tokenStart = endIndex + 2;
                goto findEndBrace;
            }

            var token = _routeTemplate.Slice(startIndex + 1, endIndex - startIndex - 1);
            var qualifier = token.IndexOfAny(new[] { ':', '=', '?' });
            CurrentName = qualifier == -1 ? token : token.Slice(0, qualifier);
            CurrentQualifiers = qualifier == -1 ? null : token.Slice(qualifier);

            _routeTemplate = _routeTemplate.Slice(endIndex + 1);
            return true;
        }
    }

    private static int IndexOf(ReadOnlySpan<char> span, int startIndex, char c)
    {
        for (var i = startIndex; i < span.Length; i++)
        {
            if (span[i] == c)
            {
                return i;
            }
        }

        return -1;
    }
}
