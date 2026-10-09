using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MuchHttp.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(VisualizationAccessCodeFixProvider)), Shared]
public sealed class VisualizationAccessCodeFixProvider : CodeFixProvider
{
    private const string Title = "Remove forbidden visualization call";

    public override ImmutableArray<string> FixableDiagnosticIds => [VisualizationAccessAnalyzer.DiagnosticId];

    public override FixAllProvider? GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null || semanticModel is null)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            var nodeToRemove = FindRemovableNode(root, semanticModel, diagnostic, context.CancellationToken);
            if (nodeToRemove is null)
                continue;

            context.RegisterCodeFix(
                CodeAction.Create(
                    Title,
                    cancellationToken => RemoveNodeAsync(context.Document, root, nodeToRemove, cancellationToken),
                    equivalenceKey: nameof(VisualizationAccessCodeFixProvider)),
                diagnostic);
        }
    }

    private static SyntaxNode? FindRemovableNode(
        SyntaxNode root, SemanticModel semanticModel, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var span = diagnostic.Location.SourceSpan;
        var invocation = root.FindNode(span, getInnermostNodeForTie: true)
            .FirstAncestorOrSelf<InvocationExpressionSyntax>();

        if (invocation?.Parent is not ExpressionStatementSyntax statement ||
            !IsStandaloneCall(invocation, span) ||
            statement.ContainsDiagnostics ||
            statement.ContainsDirectives ||
            !IsVisualizationCall(invocation, semanticModel, cancellationToken))
            return null;

        return GetRemovalTarget(statement);
    }

    private static bool IsStandaloneCall(InvocationExpressionSyntax invocation, TextSpan diagnosticSpan) =>
        invocation.Expression.Span.Contains(diagnosticSpan) &&
        !invocation.Ancestors().OfType<ArgumentSyntax>().Any() &&
        !invocation.Expression.DescendantNodes().OfType<InvocationExpressionSyntax>().Any();

    private static bool IsVisualizationCall(
        InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken) =>
        semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method &&
        VisualizationAccessAnalyzer.IsInRestrictedNamespace(method);

    private static SyntaxNode? GetRemovalTarget(ExpressionStatementSyntax statement) =>
        statement.Parent switch
        {
            BlockSyntax => statement,
            SwitchSectionSyntax => statement,
            GlobalStatementSyntax globalStatement => globalStatement,
            _ => null
        };

    private static Task<Document> RemoveNodeAsync(
        Document document, SyntaxNode root, SyntaxNode nodeToRemove, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var updatedRoot = root.RemoveNode(nodeToRemove, SyntaxRemoveOptions.KeepExteriorTrivia)
                          ?? throw new InvalidOperationException("Removing a statement must preserve the syntax root.");
        return Task.FromResult(document.WithSyntaxRoot(updatedRoot));
    }
}