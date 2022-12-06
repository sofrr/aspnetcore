// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Threading;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure.VirtualChars;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.RoutePattern;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.App.Analyzers.Infrastructure;

internal sealed class RoutePatterns
{
    private static readonly BoundedCacheWithFactory<Compilation, RoutePatterns> LazyRoutePatternsCache = new();
    
    public static RoutePatterns GetOrCreate(Compilation compilation) =>
        LazyRoutePatternsCache.GetOrCreateValue(compilation, static c => new RoutePatterns(c));
    
    private readonly ConcurrentDictionary<SyntaxToken, RoutePatternTree> _lazyRoutePatterns;
    private readonly Compilation _compilation;

    private RoutePatterns(Compilation compilation)
    {
        _lazyRoutePatterns = new();
        _compilation = compilation;
    }

    public RoutePatternTree Get(SyntaxToken syntaxToken, CancellationToken cancellationToken)
    {
        var tree = _lazyRoutePatterns[syntaxToken];
        if (tree is not null)
        {
            return tree;
        }
        
        // Symbol hasn't been added to the cache yet.
        // Resolve symbol from name, cache, and return.
        return GetAndCache(syntaxToken, cancellationToken);
    }
    
    private RoutePatternTree GetAndCache(SyntaxToken syntaxToken, CancellationToken cancellationToken)
    {
        return _lazyRoutePatterns.GetOrAdd(syntaxToken, token =>
        {
            var wellKnownTypes = WellKnownTypes.GetOrCreate(_compilation);
            var usageContext = RoutePatternUsageDetector.BuildContext(
                token,
                _compilation.GetSemanticModel(syntaxToken.SyntaxTree),
                wellKnownTypes,
                cancellationToken);

            var virtualChars = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
            var tree = RoutePatternParser.TryParse(virtualChars, supportTokenReplacement: usageContext.IsMvcAttribute);
            return tree!;
        });
    }
}
