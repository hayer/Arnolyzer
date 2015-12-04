using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Arnolyzer.RuleExceptionAttributes;
using Arnolyzer.SyntacticAnalyzers.Factories;
using Arnolyzer.SyntacticAnalyzers.Settings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using static Arnolyzer.SyntacticAnalyzers.CommonFunctions;

namespace Arnolyzer.SyntacticAnalyzers.SHOFAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StaticMethodMustHaveAtLeastOneParameterAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "StaticMethodMustHaveAtLeastOneParameter";

        private static readonly LocalizableString _title =
            LocalizableStringFactory.LocalizableResourceString(nameof(Resources.StaticMethodMustHaveAtLeastOneParameterTitle));
        private static readonly LocalizableString _messageFormat =
            LocalizableStringFactory.LocalizableResourceString(nameof(Resources.StaticMethodMustHaveAtLeastOneParameterMessageFormat));
        private static readonly LocalizableString _description =
            LocalizableStringFactory.LocalizableResourceString(nameof(Resources.StaticMethodMustHaveAtLeastOneParameterDescription));

        private static readonly DiagnosticDescriptor _rule =
            DiagnosticDescriptorFactory.EnabledByDefaultErrorDescriptor(AnalyzerCategories.ShofAnalyzers,
                                                                        DiagnosticId,
                                                                        _title,
                                                                        _messageFormat,
                                                                        _description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(CompilationStart);
        }

        [HasSideEffects]
        private static void CompilationStart(CompilationStartAnalysisContext context)
        {
            context.Options.InitialiseArnolyzerSettings();
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
        }

        [MutatesParameter]
        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var methodSymbol = (IMethodSymbol)context.Symbol;

            if (!AutoGenerated(methodSymbol) &&
                !HasIgnoreRuleAttribute(methodSymbol, new List<Type> { typeof(HasSideEffectsAttribute) }) &&
                methodSymbol.IsStatic &&
                methodSymbol.Parameters.IsEmpty &&
                methodSymbol.MethodKind != MethodKind.PropertyGet)
            {
                context.ReportDiagnostic(Diagnostic.Create(_rule, methodSymbol.Locations[0], methodSymbol.Name));
            }
        }
    }
}