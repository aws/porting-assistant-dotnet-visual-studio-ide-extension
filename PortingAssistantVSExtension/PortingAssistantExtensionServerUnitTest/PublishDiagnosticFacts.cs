
using Xunit.Abstractions;
using Xunit;

namespace PortingAssistantExtensionServerUnitTest
{
    public class PublishDiagnosticFacts : AbstractLanguageServerTestBase
    {
        public PublishDiagnosticFacts(ITestOutputHelper testOutput) : base(testOutput)
        {
        }

        //Copy from omnisharp roslyn
        //[Theory]
        //[InlineData(true)]
        //[InlineData(false)]
        //public async Task CodeCheckSpecifiedFileOnly(bool roslynAnalyzersEnabled)
        //{
        //    await ReadyHost(roslynAnalyzersEnabled);
        //    var testFile = new TestFile("a.cs", "class C { int n = true; }");
        //    AddFilesToWorkspace(testFile);
        //    await OpenFile(testFile.FileName);

        //    await WaitForDiagnostics();

        //    var quickFixes = GetDiagnostics("a.cs");
        //    Assert.Contains(quickFixes, x => x.Code == "CS0029");
        //}

    }
}
