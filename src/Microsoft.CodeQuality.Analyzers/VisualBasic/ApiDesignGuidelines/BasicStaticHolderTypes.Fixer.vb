﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Analyzer.Utilities
Imports Microsoft.ApiDesignGuidelines.Analyzers
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.ApiDesignGuidelines.VisualBasic.Analyzers

    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Public Class BasicStaticHolderTypesFixer
        Inherits CodeFixProvider

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                ' TODO: Re-implement the VB fix by turning the Class into a Module.
                ' For now, leave this fixer in place but don't declare it to fix anything.
                '
                ' This is tracked by https://github.com/dotnet/roslyn/issues/3546.
                '
                ' Return ImmutableArray.Create(CA1052DiagnosticAnalyzer.DiagnosticId)
                Return ImmutableArray(Of String).Empty
            End Get
        End Property

        Public NotOverridable Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim document = context.Document
            Dim span = context.Span
            Dim cancellationToken = context.CancellationToken

            cancellationToken.ThrowIfCancellationRequested()
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim classStatement = root.FindToken(span.Start).Parent?.FirstAncestorOrSelf(Of ClassStatementSyntax)
            If classStatement IsNot Nothing Then
                Dim title As String = String.Format(MicrosoftApiDesignGuidelinesAnalyzersResources.MakeClassStatic, classStatement.Identifier.Text)
                Dim fix = New MyCodeAction(title, Async Function(ct) Await AddNotInheritableKeyword(document, root, classStatement).ConfigureAwait(False))
                context.RegisterCodeFix(fix, context.Diagnostics)
            End If
        End Function

        Private Function AddNotInheritableKeyword(document As Document, root As SyntaxNode, classStatement As ClassStatementSyntax) As Task(Of Document)
            Dim notInheritableKeyword = SyntaxFactory.Token(SyntaxKind.NotInheritableKeyword).WithAdditionalAnnotations(Formatter.Annotation)
            Dim newClassStatement = classStatement.AddModifiers(notInheritableKeyword)
            Dim newRoot = root.ReplaceNode(classStatement, newClassStatement)
            Return Task.FromResult(document.WithSyntaxRoot(newRoot))
        End Function

        Private Class MyCodeAction
            Inherits DocumentChangeAction

            Public Overrides ReadOnly Property EquivalenceKey As String
                Get
                    Return NameOf(BasicStaticHolderTypesFixer)
                End Get
            End Property

            Public Sub New(title As String, createChangedDocument As Func(Of CancellationToken, Task(Of Document)))
                MyBase.New(title, createChangedDocument)
            End Sub
        End Class
    End Class
End Namespace
