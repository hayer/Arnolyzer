﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Arnolyzer.Analyzers.Settings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SuccincT.Options;

namespace Arnolyzer.Analyzers
{
    internal static class CommonFunctions
    {
        public static string SeverityType(this DiagnosticSeverity severity) => Enum.GetName(typeof(DiagnosticSeverity), severity);

        public static bool IsEnabledByDefault(this DefaultState state) => state == DefaultState.EnabledByDefault;

        public static bool SkipSymbolAnalysis(ISymbol symbol,
                                               SettingsHandler settingsHandler,
                                               IEnumerable<Type> suppressionAttributes)
        {
            var settings =
                settingsHandler.GetArnolyzerSettingsForProject(GetFilePathForSymbol(symbol));
            return AutoGenerated(symbol) ||
                   HasIgnoreRuleAttribute(symbol, suppressionAttributes) ||
                   IgnoredFile(symbol, settings);
        }

        private static bool AutoGenerated(ISymbol symbol) =>
            SyntaxRootContainsAutoGeneratedComment(symbol.DeclaringSyntaxReferences[0].SyntaxTree.GetRoot());

        public static bool AutoGenerated(SyntaxNode node) => 
            SyntaxRootContainsAutoGeneratedComment(node.SyntaxTree.GetRoot());

        private static bool HasIgnoreRuleAttribute(ISymbol symbol, IEnumerable<Type> attributes) => 
            symbol.GetAttributes()
                  .Any(s => attributes.TryFirst(t => MatchAttributeName(t, s.AttributeClass.Name)).HasValue);

        public static bool PropertyHasIgnoreRuleAttribute(PropertyDeclarationSyntax property,
                                                          IEnumerable<Type> attributes) =>
                property.AttributeLists
                    .SelectMany(l => l.Attributes, (l, a) => a.Name.GetText().ToString())
                    .Any(name => attributes.TryFirst(t => MatchAttributeName(t, name)).HasValue);

        private static bool IgnoredFile(ISymbol symbol, SettingsDetails settings) => 
            SyntaxTreeIsInIgoredFile(symbol.DeclaringSyntaxReferences[0].SyntaxTree, settings);

        public static bool IgnoredFile(SyntaxNode node, SettingsDetails settings) => 
            SyntaxTreeIsInIgoredFile(node.SyntaxTree, settings);

        public static bool NodeIsTypeDeclaration(SyntaxNode node)
        {
            var kind = node?.Kind();
            return kind == SyntaxKind.ClassDeclaration ||
                   kind == SyntaxKind.InterfaceDeclaration ||
                   kind == SyntaxKind.StructDeclaration ||
                   kind == SyntaxKind.EnumDeclaration;
        }
        private static string GetFilePathForSymbol(ISymbol symbol) => symbol.Locations[0].SourceTree.FilePath;

        private static bool SyntaxRootContainsAutoGeneratedComment(SyntaxNode syntaxRoot)
        {
            return syntaxRoot.ChildNodes()
                             .Where(n => n.HasLeadingTrivia)
                             .Any(node => node.GetLeadingTrivia().Any(t => t.ToString().Contains("<auto-generated>")));
        }

        private static bool MatchAttributeName(Type attributeType, string name) =>
            attributeType.Name.Replace("Attribute", "") == name || attributeType.Name == name;

        private static bool SyntaxTreeIsInIgoredFile(SyntaxTree syntaxTree, SettingsDetails settings) => 
            settings.IgnorePathsRegex != "" && Regex.Match(syntaxTree.FilePath, settings.IgnorePathsRegex).Success;
    }
}