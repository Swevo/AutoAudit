using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoAudit;

/// <summary>Roslyn incremental source generator that emits audit fields for [Auditable] classes.</summary>
[Generator]
public sealed class AuditGenerator : IIncrementalGenerator
{
    private const string AttributeFqn = "AutoAudit.AuditableAttribute";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("AutoAudit.AuditableAttribute.g.cs", Emitter.AttributeSource);
            ctx.AddSource("AutoAudit.IAuditableEntity.g.cs", Emitter.InterfaceSource);
            ctx.AddSource("AutoAudit.AuditInterceptor.g.cs", Emitter.InterceptorSource);
        });

        var typeModels = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeFqn,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => Transform(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        context.RegisterSourceOutput(typeModels, static (ctx, model) =>
            ctx.AddSource(
                $"{model.Namespace}.{model.TypeName}.Audit.g.cs",
                Emitter.Emit(model)));

        var diagnostics = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeFqn,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => GetDiagnostic(ctx))
            .Where(static d => d is not null)
            .Select(static (d, _) => d!);

        context.RegisterSourceOutput(diagnostics, static (ctx, diag) =>
            ctx.ReportDiagnostic(diag));
    }

    private static TypeModel? Transform(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol symbol)
            return null;

        if (!IsPartial(ctx.TargetNode))
            return null;

        return new TypeModel(
            symbol.Name,
            symbol.ContainingNamespace.ToDisplayString());
    }

    private static Diagnostic? GetDiagnostic(GeneratorAttributeSyntaxContext ctx)
    {
        if (IsPartial(ctx.TargetNode))
            return null;

        return Diagnostic.Create(
            Diagnostics.ClassMustBePartial,
            ctx.TargetNode.GetLocation(),
            (ctx.TargetSymbol as INamedTypeSymbol)?.Name ?? "?");
    }

    private static bool IsPartial(SyntaxNode node) =>
        node is ClassDeclarationSyntax cls &&
        cls.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
}
