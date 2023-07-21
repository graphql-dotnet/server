// source: https://github.com/dotnet/aspnetcore/blob/main/src/Shared/SecurityHelper/SecurityHelper.cs
// permalink: https://github.com/dotnet/aspnetcore/blob/8b2fd3f7a3b3e18afc6f63c4a494cc733dcced64/src/Shared/SecurityHelper/SecurityHelper.cs
// retrieved: 2023-07-05

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*

The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

namespace GraphQL.Server.Transports.AspNetCore;

/// <summary>
/// Helper code used when implementing authentication middleware
/// </summary>
internal static class SecurityHelper
{
    /// <summary>
    /// Add all ClaimsIdentities from an additional ClaimPrincipal to the ClaimsPrincipal
    /// Merges a new claims principal, placing all new identities first, and eliminating
    /// any empty unauthenticated identities from context.User
    /// </summary>
    /// <param name="existingPrincipal">The <see cref="ClaimsPrincipal"/> containing existing <see cref="ClaimsIdentity"/>.</param>
    /// <param name="additionalPrincipal">The <see cref="ClaimsPrincipal"/> containing <see cref="ClaimsIdentity"/> to be added.</param>
    public static ClaimsPrincipal MergeUserPrincipal(ClaimsPrincipal? existingPrincipal, ClaimsPrincipal? additionalPrincipal)
    {
        // For the first principal, just use the new principal rather than copying it
        if (existingPrincipal == null && additionalPrincipal != null)
        {
            return additionalPrincipal;
        }

        var newPrincipal = new ClaimsPrincipal();

        // New principal identities go first
        if (additionalPrincipal != null)
        {
            newPrincipal.AddIdentities(additionalPrincipal.Identities);
        }

        // Then add any existing non empty or authenticated identities
        if (existingPrincipal != null)
        {
            newPrincipal.AddIdentities(existingPrincipal.Identities.Where(i => i.IsAuthenticated || i.Claims.Any()));
        }
        return newPrincipal;
    }
}
