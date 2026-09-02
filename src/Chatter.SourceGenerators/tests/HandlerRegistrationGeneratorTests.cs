using System.Linq;
using Chatter.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Chatter.SourceGenerators.Tests;

public class HandlerRegistrationGeneratorTests
{
    // AppDomain.CurrentDomain.GetAssemblies() only returns assemblies already loaded into the process,
    // and modern .NET loads referenced assemblies lazily — Chatter.CQRS may not be loaded yet even
    // though this project references it. TRUSTED_PLATFORM_ASSEMBLIES gives the full BCL set reliably;
    // referencing Chatter.CQRS's own ICommand type directly guarantees that assembly is loaded too.
    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .Append(MetadataReference.CreateFromFile(typeof(Chatter.CQRS.Commands.ICommand).Assembly.Location))
        .ToArray();

    private static (Compilation Compilation, string? Generated) Run(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new HandlerRegistrationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var result = driver.GetRunResult();
        var generated = result.Results
            .SelectMany(r => r.GeneratedSources)
            .FirstOrDefault(s => s.HintName == "GeneratedHandlerRegistration.g.cs");

        return (outputCompilation, generated.SourceText?.ToString());
    }

    [Fact]
    public void PublicCommandHandler_IsDiscoveredAndRegistered()
    {
        var (_, generated) = Run("""
            using Chatter.CQRS;
            using Chatter.CQRS.Commands;
            using Chatter.CQRS.Context;
            using System.Threading.Tasks;

            namespace TestNamespace;

            public sealed class PublicCommand : ICommand { }

            public sealed class PublicCommandHandler : IMessageHandler<PublicCommand>
            {
                public Task Handle(PublicCommand message, IMessageHandlerContext context) => Task.CompletedTask;
            }
            """);

        Assert.NotNull(generated);
        Assert.Contains("AddCommandHandler<global::TestNamespace.PublicCommand, global::TestNamespace.PublicCommandHandler>", generated);
    }

    [Fact]
    public void InternalCommandHandlerWithoutInternalsVisibleTo_IsSilentlySkipped()
    {
        // Simulates the exact wall the spike found: a type visible to the generator's symbol walk
        // (declared internal, no InternalsVisibleTo to the compiling assembly) but not legally
        // referenceable from generated code in a different assembly. Discovery must not turn into a
        // broken emit.
        const string librarySource = """
            using Chatter.CQRS;
            using Chatter.CQRS.Commands;
            using Chatter.CQRS.Context;
            using System.Threading.Tasks;

            namespace LibraryNamespace;

            internal sealed class InternalCommand : ICommand { }

            internal sealed class InternalCommandHandler : IMessageHandler<InternalCommand>
            {
                public Task Handle(InternalCommand message, IMessageHandlerContext context) => Task.CompletedTask;
            }
            """;

        var libraryTree = CSharpSyntaxTree.ParseText(librarySource);
        var libraryCompilation = CSharpCompilation.Create(
            "LibraryAssembly",
            [libraryTree],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var libraryStream = new MemoryStream();
        var emitResult = libraryCompilation.Emit(libraryStream);
        Assert.True(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics));
        libraryStream.Position = 0;

        var hostReferences = References.Append(MetadataReference.CreateFromStream(libraryStream)).ToArray();
        var hostTree = CSharpSyntaxTree.ParseText("namespace HostNamespace;");
        var hostCompilation = CSharpCompilation.Create(
            "HostAssembly",
            [hostTree],
            hostReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new HandlerRegistrationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(hostCompilation, out var outputCompilation, out _);

        var diagnostics = outputCompilation.GetDiagnostics();
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        var result = driver.GetRunResult();
        var generated = result.Results
            .SelectMany(r => r.GeneratedSources)
            .FirstOrDefault(s => s.HintName == "GeneratedHandlerRegistration.g.cs");

        Assert.DoesNotContain("InternalCommandHandler", generated.SourceText?.ToString() ?? string.Empty);
    }
}
