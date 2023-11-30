using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using System.Collections.Immutable;

namespace BusinessCentral.LinterCop.Design
{
    [DiagnosticAnalyzer]
    public class Rule0039PageRunTableMismatch : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create<DiagnosticDescriptor>(DiagnosticDescriptors.Rule0039ArgumentDifferentTypeThenExpected);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(new Action<OperationAnalysisContext>(this.AnalyzeRunPageArguments), OperationKind.InvocationExpression);
            context.RegisterOperationAction(new Action<OperationAnalysisContext>(this.AnalyzeSetRecordArgument), OperationKind.InvocationExpression);
        }

        private void AnalyzeRunPageArguments(OperationAnalysisContext ctx)
        {
            if (ctx.ContainingSymbol.GetContainingObjectTypeSymbol().IsObsoletePending || ctx.ContainingSymbol.GetContainingObjectTypeSymbol().IsObsoleteRemoved) return;
            if (ctx.ContainingSymbol.IsObsoletePending || ctx.ContainingSymbol.IsObsoleteRemoved) return;

            IInvocationExpression operation = (IInvocationExpression)ctx.Operation;
            if (operation.TargetMethod.MethodKind != MethodKind.BuiltInMethod) return;

            if (operation.TargetMethod.ContainingType.GetTypeSymbol().GetNavTypeKindSafe() != NavTypeKind.Page) return;
            string[] procedureNames = { "Run", "RunModal" };
            if (!procedureNames.Contains(operation.TargetMethod.Name)) return;
            if (operation.Arguments.Count() < 2) return;

            if (operation.Arguments[0].Syntax.Kind != SyntaxKind.OptionAccessExpression) return;
            if (operation.Arguments[1].Syntax.Kind != SyntaxKind.IdentifierName || operation.Arguments[1].Value.Kind != OperationKind.ConversionExpression) return;

            IApplicationObjectTypeSymbol applicationObjectTypeSymbol = ((IApplicationObjectAccess)operation.Arguments[0].Value).ApplicationObjectTypeSymbol;
            if (applicationObjectTypeSymbol.GetNavTypeKindSafe() != NavTypeKind.Page) return;
            ITableTypeSymbol pageSourceTable = ((IPageTypeSymbol)applicationObjectTypeSymbol.GetTypeSymbol()).RelatedTable;
            if (pageSourceTable == null) return;

            IOperation operand = ((IConversionExpression)operation.Arguments[1].Value).Operand;
            ITableTypeSymbol recordArgument = ((IRecordTypeSymbol)operand.GetSymbol().GetTypeSymbol()).BaseTable;

            if (!AreTheSameNavObjects(recordArgument, pageSourceTable))
                ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.Rule0039ArgumentDifferentTypeThenExpected, ctx.Operation.Syntax.GetLocation(), new object[] { 2, operand.GetSymbol().GetTypeSymbol().ToString(), pageSourceTable.GetNavTypeKindSafe() + " \"" + pageSourceTable.Name + "\"" }));
        }

        private void AnalyzeSetRecordArgument(OperationAnalysisContext ctx)
        {
            if (ctx.ContainingSymbol.GetContainingObjectTypeSymbol().IsObsoletePending || ctx.ContainingSymbol.GetContainingObjectTypeSymbol().IsObsoleteRemoved) return;
            if (ctx.ContainingSymbol.IsObsoletePending || ctx.ContainingSymbol.IsObsoleteRemoved) return;

            IInvocationExpression operation = (IInvocationExpression)ctx.Operation;
            if (operation.TargetMethod.MethodKind != MethodKind.BuiltInMethod) return;

            if (operation.TargetMethod.ContainingType.GetTypeSymbol().GetNavTypeKindSafe() != NavTypeKind.Page) return;
            string[] procedureNames = { "GetRecord", "SetRecord", "SetSelectionFilter", "SetTableView" };
            if (!procedureNames.Contains(operation.TargetMethod.Name)) return;
            if (operation.Arguments.Count() != 1) return;

            if (operation.Arguments[0].Syntax.Kind != SyntaxKind.IdentifierName || operation.Arguments[0].Value.Kind != OperationKind.ConversionExpression) return;

            IOperation pageReference = ctx.Operation.DescendantsAndSelf().Where(x => x.GetSymbol() != null)
                                                        .Where(x => x.Type.GetNavTypeKindSafe() == NavTypeKind.Page)
                                                        .First();
            IVariableSymbol variableSymbol = (IVariableSymbol)pageReference.GetSymbol().OriginalDefinition;
            IPageTypeSymbol pageTypeSymbol = (IPageTypeSymbol)variableSymbol.GetTypeSymbol().OriginalDefinition;
            ITableTypeSymbol pageSourceTable = pageTypeSymbol.RelatedTable;

            IOperation operand = ((IConversionExpression)operation.Arguments[0].Value).Operand;
            ITableTypeSymbol recordArgument = ((IRecordTypeSymbol)operand.GetSymbol().GetTypeSymbol()).BaseTable;

            if (!AreTheSameNavObjects(recordArgument, pageSourceTable))
                ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.Rule0039ArgumentDifferentTypeThenExpected, ctx.Operation.Syntax.GetLocation(), new object[] { 1, operand.GetSymbol().GetTypeSymbol().ToString(), pageSourceTable.GetNavTypeKindSafe() + " \"" + pageSourceTable.Name + "\"" }));
        }

        private static bool AreTheSameNavObjects(ITableTypeSymbol left, ITableTypeSymbol right)
        {
            if (left.GetNavTypeKindSafe() != right.GetNavTypeKindSafe()) return false;
            if (((INamespaceSymbol)left.ContainingSymbol).QualifiedName != ((INamespaceSymbol)right.ContainingSymbol).QualifiedName) return false;
            if (left.Name != right.Name) return false;
            return true;
        }
    }
}