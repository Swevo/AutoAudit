using Microsoft.CodeAnalysis;

namespace AutoAudit;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor ClassMustBePartial = new(
        id: "AUDIT001",
        title: "Class must be partial",
        messageFormat: "Class '{0}' must be declared as partial to use [Auditable]",
        category: "AutoAudit",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
