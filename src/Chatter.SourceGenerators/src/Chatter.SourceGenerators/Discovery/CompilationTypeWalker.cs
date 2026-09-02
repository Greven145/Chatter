using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Chatter.SourceGenerators.Discovery
{
    internal static class CompilationTypeWalker
    {
        public static IEnumerable<INamedTypeSymbol> FindAccessibleCandidateTypes(
            Compilation compilation,
            string requiredReferencedAssemblySimpleName)
        {
            foreach (var assembly in RelevantAssemblies(compilation, requiredReferencedAssemblySimpleName))
            {
                foreach (var candidate in AllNamedTypes(assembly.GlobalNamespace))
                {
                    if (candidate.TypeKind != TypeKind.Class
                        || candidate.IsAbstract
                        || candidate.IsGenericType)
                    {
                        continue;
                    }

                    if (!compilation.IsSymbolAccessibleWithin(candidate, compilation.Assembly))
                    {
                        continue;
                    }

                    yield return candidate;
                }
            }
        }

        public static bool TryGetClosedInterface(INamedTypeSymbol candidate, INamedTypeSymbol openGenericInterface, out INamedTypeSymbol closedInterface)
        {
            foreach (var iface in candidate.AllInterfaces)
            {
                if (iface.IsGenericType && SymbolEqualityComparer.Default.Equals(iface.ConstructedFrom, openGenericInterface))
                {
                    closedInterface = iface;
                    return true;
                }
            }

            closedInterface = null!;
            return false;
        }

        public static bool Implements(INamedTypeSymbol candidate, INamedTypeSymbol targetInterface)
            => candidate.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, targetInterface));

        // FullyQualifiedFormat already emits its own "global::" prefix and never renders a predefined-type
        // keyword alias (e.g. "string") — prepending "global::" to the default ToDisplayString() output
        // breaks for any type whose alias exists (global::string is not valid C#).
        public static string FullyQualifiedName(ITypeSymbol type)
            => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        private static IEnumerable<IAssemblySymbol> RelevantAssemblies(Compilation compilation, string requiredReferencedAssemblySimpleName)
        {
            yield return compilation.Assembly;

            foreach (var reference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly
                    && !SymbolEqualityComparer.Default.Equals(assembly, compilation.Assembly)
                    && ReferencesAssembly(assembly, requiredReferencedAssemblySimpleName))
                {
                    yield return assembly;
                }
            }
        }

        // First-hop only: an assembly whose declared references don't literally name the required
        // assembly is skipped without walking its namespace tree. This is what keeps the walk off
        // the BCL and unrelated third-party packages pulled in transitively.
        private static bool ReferencesAssembly(IAssemblySymbol assembly, string simpleName)
            => assembly.Identity.Name == simpleName
               || assembly.Modules.Any(m => m.ReferencedAssemblies.Any(id => id.Name == simpleName));

        private static IEnumerable<INamedTypeSymbol> AllNamedTypes(INamespaceSymbol root)
        {
            foreach (var member in root.GetMembers())
            {
                if (member is INamespaceSymbol ns)
                {
                    foreach (var t in AllNamedTypes(ns))
                    {
                        yield return t;
                    }
                }
                else if (member is INamedTypeSymbol type)
                {
                    yield return type;

                    foreach (var nested in AllNestedTypes(type))
                    {
                        yield return nested;
                    }
                }
            }
        }

        private static IEnumerable<INamedTypeSymbol> AllNestedTypes(INamedTypeSymbol type)
        {
            foreach (var nested in type.GetTypeMembers())
            {
                yield return nested;

                foreach (var deeper in AllNestedTypes(nested))
                {
                    yield return deeper;
                }
            }
        }
    }
}
