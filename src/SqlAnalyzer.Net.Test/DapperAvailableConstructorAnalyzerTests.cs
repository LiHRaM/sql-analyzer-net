using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlAnalyzer.Net.Test.Helpers;

namespace SqlAnalyzer.Net.Test
{
    [TestClass]
    public class DapperAvailableConstructorAnalyzerTests : DiagnosticVerifier
    {
        protected override string TestDataFolder => "DapperAvailableConstructorAnalyzer";

        [TestMethod]
        public void InlineNoValidConstructor_AnalyzerTriggered()
        {
            var code = ReadTestData("NoAvailableConstructor.cs");

            var expected = new DiagnosticResult
            {
                Id = DapperAvailableConstructorsAnalyzer.DiagnosticId,
                Message = string.Format(DapperAvailableConstructorsAnalyzer.MessageFormatCsharpArgumentNotFound, "SqlAnalyzer.Net.Test.Dto"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 34) }
            };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void AvailableImplicitConstructorAnalyzer_NotTriggered()
        {
            var code = ReadTestData("AvailableImplicitConstructor.cs");

            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void AvailableExplicitConstructorAnalyzer_NotTriggered()
        {
            var code = ReadTestData("AvailableExplicitConstructor.cs");

            VerifyCSharpDiagnostic(code);
        }


        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new DapperAvailableConstructorsAnalyzer();
        }
    }
}
