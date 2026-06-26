using AutoAudit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AutoAudit.Tests;

/// <summary>Validates generator output via <see cref="CSharpGeneratorDriver"/> without
/// requiring EF Core references in the compilation.</summary>
public class GeneratorDriverTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GeneratorDriverRunResult RunGenerator(string source)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(AuditGenerator).Assembly.Location),
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver
            .Create(new AuditGenerator())
            .RunGenerators(compilation);

        return driver.GetRunResult();
    }

    private static string? GetEntitySource(GeneratorDriverRunResult result, string typeName) =>
        result.GeneratedTrees
            .Select(t => t.ToString())
            .FirstOrDefault(s => s.Contains($"partial class {typeName}"));

    // ── Static sources always emitted ─────────────────────────────────────────

    [Fact]
    public void AttributeSource_AlwaysEmitted()
    {
        var result = RunGenerator("// empty");
        result.GeneratedTrees.Any(t => t.ToString().Contains("class AuditableAttribute"))
            .Should().BeTrue();
    }

    [Fact]
    public void InterfaceSource_AlwaysEmitted()
    {
        var result = RunGenerator("// empty");
        result.GeneratedTrees.Any(t => t.ToString().Contains("interface IAuditableEntity"))
            .Should().BeTrue();
    }

    [Fact]
    public void InterceptorSource_AlwaysEmitted()
    {
        var result = RunGenerator("// empty");
        result.GeneratedTrees.Any(t => t.ToString().Contains("class AuditInterceptor"))
            .Should().BeTrue();
    }

    [Fact]
    public void ExtensionsSource_AlwaysEmitted()
    {
        var result = RunGenerator("// empty");
        result.GeneratedTrees.Any(t => t.ToString().Contains("AuditInterceptorExtensions"))
            .Should().BeTrue();
    }

    [Fact]
    public void EmptyInput_EmitsExactlyThreeStaticSources()
    {
        var result = RunGenerator("// empty");
        result.GeneratedTrees.Should().HaveCount(3);
    }

    // ── Entity generation ──────────────────────────────────────────────────────

    [Fact]
    public void AuditableClass_EmitsPartialClass()
    {
        var result = RunGenerator("""
            using AutoAudit;
            namespace App;
            [Auditable]
            public partial class Order { }
            """);

        var src = GetEntitySource(result, "Order");
        src.Should().NotBeNull();
        src.Should().Contain("partial class Order");
    }

    [Fact]
    public void AuditableClass_EmitsIAuditableEntityInterface()
    {
        var result = RunGenerator("""
            using AutoAudit;
            namespace App;
            [Auditable]
            public partial class Order { }
            """);

        var src = GetEntitySource(result, "Order");
        src.Should().Contain("IAuditableEntity");
    }

    [Fact]
    public void AuditableClass_EmitsCreatedAtProperty()
    {
        var result = RunGenerator("""
            using AutoAudit;
            namespace App;
            [Auditable]
            public partial class Order { }
            """);

        var src = GetEntitySource(result, "Order");
        src.Should().Contain("CreatedAt");
        src.Should().Contain("DateTimeOffset");
    }

    [Fact]
    public void AuditableClass_EmitsUpdatedAtProperty()
    {
        var result = RunGenerator("""
            using AutoAudit;
            namespace App;
            [Auditable]
            public partial class Order { }
            """);

        var src = GetEntitySource(result, "Order");
        src.Should().Contain("UpdatedAt");
        src.Should().Contain("DateTimeOffset");
    }

    [Fact]
    public void AuditableClass_EmitsCreatedByProperty()
    {
        var result = RunGenerator("""
            using AutoAudit;
            namespace App;
            [Auditable]
            public partial class Order { }
            """);

        var src = GetEntitySource(result, "Order");
        src.Should().Contain("CreatedBy");
        src.Should().Contain("string?");
    }

    [Fact]
    public void AuditableClass_EmitsUpdatedByProperty()
    {
        var result = RunGenerator("""
            using AutoAudit;
            namespace App;
            [Auditable]
            public partial class Order { }
            """);

        var src = GetEntitySource(result, "Order");
        src.Should().Contain("UpdatedBy");
        src.Should().Contain("string?");
    }

    [Fact]
    public void AuditableClass_NamespaceIsPreserved()
    {
        var result = RunGenerator("""
            using AutoAudit;
            namespace My.Company.Domain;
            [Auditable]
            public partial class Invoice { }
            """);

        var src = GetEntitySource(result, "Invoice");
        src.Should().Contain("namespace My.Company.Domain");
    }

    [Fact]
    public void TwoAuditableClasses_BothGenerated()
    {
        var result = RunGenerator("""
            using AutoAudit;
            namespace App;
            [Auditable] public partial class Order { }
            [Auditable] public partial class Customer { }
            """);

        result.GeneratedTrees.Should().HaveCount(5); // 3 static + 2 entities
        GetEntitySource(result, "Order").Should().NotBeNull();
        GetEntitySource(result, "Customer").Should().NotBeNull();
    }

    // ── AUDIT001 diagnostic ────────────────────────────────────────────────────

    [Fact]
    public void NonPartialClass_ReportsAUDIT001()
    {
        var result = RunGenerator("""
            using AutoAudit;
            namespace App;
            [Auditable]
            public class Order { }
            """);

        result.Diagnostics.Should().ContainSingle(d => d.Id == "AUDIT001");
    }

    [Fact]
    public void NonPartialClass_DoesNotEmitEntitySource()
    {
        var result = RunGenerator("""
            using AutoAudit;
            namespace App;
            [Auditable]
            public class Order { }
            """);

        GetEntitySource(result, "Order").Should().BeNull();
    }

    [Fact]
    public void PartialClass_DoesNotReportAUDIT001()
    {
        var result = RunGenerator("""
            using AutoAudit;
            namespace App;
            [Auditable]
            public partial class Order { }
            """);

        result.Diagnostics.Should().NotContain(d => d.Id == "AUDIT001");
    }

    [Fact]
    public void AUDIT001_MessageContainsClassName()
    {
        var result = RunGenerator("""
            using AutoAudit;
            namespace App;
            [Auditable]
            public class MyEntity { }
            """);

        var diag = result.Diagnostics.FirstOrDefault(d => d.Id == "AUDIT001");
        diag.Should().NotBeNull();
        diag!.GetMessage().Should().Contain("MyEntity");
    }

    // ── Interceptor source content ─────────────────────────────────────────────

    [Fact]
    public void InterceptorSource_ContainsSavingChangesOverride()
    {
        var result = RunGenerator("// empty");
        var src = result.GeneratedTrees
            .Select(t => t.ToString())
            .First(s => s.Contains("AuditInterceptor"));

        src.Should().Contain("SavingChanges");
        src.Should().Contain("SavingChangesAsync");
    }

    [Fact]
    public void InterceptorSource_ContainsApplyAuditMethod()
    {
        var result = RunGenerator("// empty");
        var src = result.GeneratedTrees
            .Select(t => t.ToString())
            .First(s => s.Contains("AuditInterceptor"));

        src.Should().Contain("ApplyAudit");
        src.Should().Contain("IAuditableEntity");
    }

    [Fact]
    public void InterceptorSource_ContainsAddAuditInterceptorExtension()
    {
        var result = RunGenerator("// empty");
        var src = result.GeneratedTrees
            .Select(t => t.ToString())
            .First(s => s.Contains("AuditInterceptorExtensions"));

        src.Should().Contain("AddAuditInterceptor");
        src.Should().Contain("DbContextOptionsBuilder");
    }
}
