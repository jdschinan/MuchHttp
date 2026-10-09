using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MuchHttp.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class VisualizationAccessAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MUCH0001";

    private const string RestrictedNamespace = "MuchHttp.Visualization";
    private const string AllowedFile = "Program.cs";

    // The rule: what the build error looks like.
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Access to restricted namespace",
        messageFormat: "'{0}' belongs to " + RestrictedNamespace + " and may only be used in " + AllowedFile,
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];


    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        // Call AnalyzeNameNode for every identifier name and generic name in the code.
        context.RegisterSyntaxNodeAction(AnalyzeNameNode, SyntaxKind.IdentifierName, SyntaxKind.GenericName);
    }

    private static void AnalyzeNameNode(SyntaxNodeAnalysisContext context)
    {
        // 1. Program.cs is allowed to use the namespace.
        if (CurrentNodeLivesInProgramCs(context))
            return;

        // 2. The namespace is allowed to use itself.
        if (IsInRestrictedNamespace(context.ContainingSymbol))
            return;

        // 3. Ask the compiler what this name refers to.
        var symbol = context.SemanticModel.GetSymbolInfo(context.Node, context.CancellationToken).Symbol;
        if (symbol is null)
            return;

        // 4. If it lives in the restricted namespace: report an error.
        if (IsInRestrictedNamespace(symbol))
            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), symbol.ToDisplayString()));
    }

    private static bool CurrentNodeLivesInProgramCs(SyntaxNodeAnalysisContext context) =>
        Path.GetFileName(context.Node.SyntaxTree.FilePath) == AllowedFile;

    internal static bool IsInRestrictedNamespace(ISymbol? symbol)
    {
        var symbolNamespace = symbol as INamespaceSymbol ?? symbol?.ContainingNamespace;
        return symbolNamespace?.ToDisplayString() == RestrictedNamespace;
    }
}