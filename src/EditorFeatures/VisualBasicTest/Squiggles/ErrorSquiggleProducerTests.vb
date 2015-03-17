' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Squiggles
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics.RemoveUnnecessaryImports
Imports Microsoft.VisualStudio.Text.Adornments
Imports Microsoft.VisualStudio.Text.Tagging

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Squiggles
    Public Class ErrorSquiggleProducerTests
        Inherits AbstractSquiggleProducerTests

        Private Function ProduceSquiggles(ParamArray lines As String()) As IEnumerable(Of ITagSpan(Of IErrorTag))
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(lines)
                Return GetErrorSpans(workspace)
            End Using
        End Function

        Private Function ProduceSquiggles(analyzerMap As ImmutableDictionary(Of String, ImmutableArray(Of DiagnosticAnalyzer)), ParamArray lines As String()) As IEnumerable(Of ITagSpan(Of IErrorTag))
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(lines)
                Return GetErrorSpans(workspace, analyzerMap)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Sub ErrorTagGeneratedForSimpleError()
            ' Make sure we have errors from the tree
            Dim spans = ProduceSquiggles("^")
            Assert.Equal(1, spans.Count())

            Dim firstSpan = spans.First()
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.Tag.ErrorType)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Sub ArgOutOfRangeExceptionBug_904382()
            Dim spans = ProduceSquiggles("Class C1", "Sub Foo(", "End Class")

            'If the following line does not throw an exception then the test passes.
            Dim count = spans.Count
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Sub ErrorDoesNotCrashPastEOF()
            Dim spans = ProduceSquiggles("Class C1",
                                         "    Sub Foo()",
                                         "        Dim x = <xml>",
                                         "    End Sub",
                                         "End Class")
            Assert.Equal(5, spans.Count())
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Sub SemanticError()
            Dim spans = ProduceSquiggles(
"Class C1",
"    Sub Foo(b as Bar)",
"    End Sub",
"End Class")
            Assert.Equal(1, spans.Count())

            Dim firstSpan = spans.First()
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.Tag.ErrorType)
            Assert.Contains("Bar", DirectCast(firstSpan.Tag.ToolTipContent, String), StringComparison.Ordinal)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Sub SuggestionTagsForUnnecessaryCode()

            Dim analyzerMap = ImmutableDictionary.CreateBuilder(Of String, ImmutableArray(Of DiagnosticAnalyzer))
            analyzerMap.Add(LanguageNames.VisualBasic,
                    ImmutableArray.Create(Of DiagnosticAnalyzer)(
                        New VisualBasicSimplifyTypeNamesDiagnosticAnalyzer(),
                        New VisualBasicRemoveUnnecessaryImportsDiagnosticAnalyzer()))

            Dim spans = ProduceSquiggles(analyzerMap.ToImmutable(),
"Imports System.Collections ' Unused import.
Class C1
    Sub Foo()
        Dim x as System.Int32 = 2 ' Simplify type name.
        x = x + 1
    End Sub
End Class")
            spans = spans.OrderBy(Function(s) s.Span.Span.Start)

            Assert.Equal(2, spans.Count())
            Dim first = spans.First()
            Dim second = spans.Last()

            Assert.Equal(PredefinedErrorTypeNames.Suggestion, first.Tag.ErrorType)
            Assert.Equal(VBFeaturesResources.RemoveUnnecessaryImportsDiagnosticTitle, first.Tag.ToolTipContent)
            Assert.Equal(PredefinedErrorTypeNames.Suggestion, second.Tag.ErrorType)
            Assert.Equal(WorkspacesResources.NameCanBeSimplified, second.Tag.ToolTipContent)
        End Sub
    End Class
End Namespace
