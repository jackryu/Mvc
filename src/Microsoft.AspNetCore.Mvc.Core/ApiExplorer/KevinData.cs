// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    /// <summary>
    /// Metadata associated with an action method inferred via conventions or ambient values.
    /// </summary>
    public sealed class KevinData
    {
        /// <summary>
        /// Gets or sets the <see cref="IApiResponseMetadataProvider"/> instances inferred from a convention.
        /// </summary>
        public IReadOnlyList<IApiResponseMetadataProvider> ResponseMetadataProviders { get; set; }

        /// <summary>
        /// Gets or sets the error <see cref="Type"/> 
        /// </summary>
        public Type DefaultErrorType { get; set; }

        internal static KevinData Read(
            MethodInfo method,
            ApiConventionTypeAttribute[] apiConventionAttributes)
        {
            var responseMetadataProviders = GetResponseMetadataProvidersFromConvention(method, apiConventionAttributes);
            var errorType = GetDefaultErrorType(method);

            return new KevinData
            {
                ResponseMetadataProviders = responseMetadataProviders,
                DefaultErrorType = errorType,
            };
        }

        private static Type GetDefaultErrorType(MethodInfo method)
        {
            var errorTypeAttribute =
                method.GetCustomAttribute<ApiErrorTypeAttribute>(inherit: true) ??
                method.DeclaringType.GetCustomAttribute<ApiErrorTypeAttribute>(inherit: true) ??
                method.DeclaringType.Assembly.GetCustomAttribute<ApiErrorTypeAttribute>();

            return errorTypeAttribute?.Type ?? typeof(ProblemDetails);
        }

        private static IReadOnlyList<IApiResponseMetadataProvider> GetResponseMetadataProvidersFromConvention(
            MethodInfo method,
            ApiConventionTypeAttribute[] apiConventionAttributes)
        {
            var apiConventionMethodAttribute = method.GetCustomAttribute<ApiConventionMethodAttribute>(inherit: true);
            var conventionMethod = apiConventionMethodAttribute?.Method;
            if (conventionMethod == null)
            {
                conventionMethod = MatchConventionMethod(method, apiConventionAttributes);
            }

            if (conventionMethod != null)
            {
                var metadataProviders = conventionMethod.GetCustomAttributes(inherit: false)
                    .OfType<IApiResponseMetadataProvider>()
                    .ToArray();

                return metadataProviders;
            }

            return Array.Empty<IApiResponseMetadataProvider>();
        }

        private static MethodInfo MatchConventionMethod(MethodInfo method, ApiConventionTypeAttribute[] apiConventionAttributes)
        {
            foreach (var attribute in apiConventionAttributes)
            {
                var conventionMethods = attribute.ConventionType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                foreach (var conventionMethod in conventionMethods)
                {
                    if (ApiConventionMatcher.IsMatch(method, conventionMethod))
                    {
                        return conventionMethod;
                    }
                }
            }

            return null;
        }
    }
}
