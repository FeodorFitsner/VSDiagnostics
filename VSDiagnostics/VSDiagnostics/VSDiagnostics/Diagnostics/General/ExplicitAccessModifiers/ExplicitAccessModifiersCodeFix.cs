﻿using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace VSDiagnostics.Diagnostics.General.ExplicitAccessModifiers
{
    [ExportCodeFixProvider(nameof(ExplicitAccessModifiersCodeFix), LanguageNames.CSharp), Shared]
    public class ExplicitAccessModifiersCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(ExplicitAccessModifiersAnalyzer.Rule.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var statement = root.FindNode(diagnosticSpan);

            var semanticModel = context.Document.GetSemanticModelAsync().Result;
            var symbol = semanticModel.GetDeclaredSymbol(statement);
            var accessibility = symbol?.DeclaredAccessibility ?? Accessibility.Private;

            context.RegisterCodeFix(
                CodeAction.Create(VSDiagnosticsResources.ExplicitAccessModifiersCodeFixTitle,
                    x => AddModifier(context.Document, root, statement, accessibility),
                    ExplicitAccessModifiersAnalyzer.Rule.Id), diagnostic);
        }

        private Task<Solution> AddModifier(Document document, SyntaxNode root, SyntaxNode statement,
            Accessibility accessibility)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var newStatement = generator.WithAccessibility(statement, accessibility);

            var newRoot = root.ReplaceNode(statement, newStatement);
            return Task.FromResult(document.WithSyntaxRoot(newRoot).Project.Solution);
        }
    }
}