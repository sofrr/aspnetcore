﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.RoutePattern;

namespace Microsoft.AspNetCore.App.Analyzers.Infrastructure;

internal sealed class RouteUsageModel
{
    public RoutePatternTree RoutePattern { get; init; } = default!;
    public RoutePatternUsageContext UsageContext { get; init; }
}
