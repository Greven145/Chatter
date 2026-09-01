using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Chatter.CQRS.DependencyInjection
{
    public static class TypeExtensions
    {
        public static bool IsGenericTypeWithNonGenericTypeParameters(this Type type)
            => type.IsGenericType && type.GetGenericArguments().All(a => !a.IsGenericParameter);

        [RequiresUnreferencedCode("Reflects over the implemented-interfaces closure of a runtime-supplied Type, which trimming cannot statically analyze.")]
        public static bool IsImplementingOpenGenericTypeWithMatchingTypeParameter(this Type type, Type openGenericTypeToMatch, Type typeParameterTypeToMatch)
            => type.GetImplementedInterfacesThatMatchOpenGenericType(openGenericTypeToMatch)
                .HasMatchingGenericArgumentType(typeParameterTypeToMatch);

        [RequiresUnreferencedCode("Reflects over the implemented-interfaces closure of a runtime-supplied Type, which trimming cannot statically analyze.")]
        public static IEnumerable<Type> GetImplementedInterfacesThatMatchOpenGenericType(this Type type, Type openGenericType)
            => type.GetTypeInfo().ImplementedInterfaces
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericType);

        [RequiresUnreferencedCode("Reflects over the implemented-interfaces closure of a runtime-supplied Type, which trimming cannot statically analyze.")]
        public static bool HasMatchingGenericArgumentType(this IEnumerable<Type> types, Type typeParameterTypeToMatch)
            => types.Any(t => t.GetImplementedInterfacesOfSingleGenericTypeArgument().Any(gat => gat == typeParameterTypeToMatch));

        [RequiresUnreferencedCode("Reflects over the implemented-interfaces closure of a runtime-supplied Type, which trimming cannot statically analyze.")]
        public static IEnumerable<Type> GetImplementedInterfacesOfSingleGenericTypeArgument(this Type type)
            => type.GetGenericArguments().SingleOrDefault()?.GetTypeInfo().ImplementedInterfaces ?? new Type[] { };
    }
}
