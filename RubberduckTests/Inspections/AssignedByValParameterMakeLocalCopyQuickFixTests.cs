using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using Rubberduck.Inspections;
using Rubberduck.Inspections.QuickFixes;
using Moq;
using Rubberduck.VBEditor.SafeComWrappers.Abstract;
using RubberduckTests.Mocks;
using Rubberduck.UI.Refactorings;
using System.Windows.Forms;
using Rubberduck.Parsing.Inspections.Abstract;
using Rubberduck.Parsing.VBA;

namespace RubberduckTests.Inspections
{
    [TestClass]
    public class AssignedByValParameterMakeLocalCopyQuickFixTests
    {

        [TestMethod]
        [TestCategory("Inspections")]
        public void AssignedByValParameter_LocalVariableAssignment()
        {
            var inputCode =
@"Public Sub Foo(ByVal arg1 As String)
    Let arg1 = ""test""
End Sub";

            var expectedCode =
@"Public Sub Foo(ByVal arg1 As String)
Dim localArg1 As String
localArg1 = arg1
    Let localArg1 = ""test""
End Sub";

            var quickFixResult = ApplyLocalVariableQuickFixToCodeFragment(inputCode);
            Assert.AreEqual(expectedCode, quickFixResult);

            //weaponized formatting
            inputCode =
@"Sub DoSomething(_
    ByVal foo As Long, _
    ByRef _
        bar, _
    ByRef barbecue _
                    )
    foo = 4
    bar = barbecue * _
                bar + foo / barbecue
End Sub
";

            expectedCode =
@"Sub DoSomething(_
    ByVal foo As Long, _
    ByRef _
        bar, _
    ByRef barbecue _
                    )
Dim localFoo As Long
localFoo = foo
    localFoo = 4
    bar = barbecue * _
                bar + localFoo / barbecue
End Sub
";
            quickFixResult = ApplyLocalVariableQuickFixToCodeFragment(inputCode);
            Assert.AreEqual(expectedCode, quickFixResult);
        }

        [TestMethod]
        [TestCategory("Inspections")]
        public void AssignedByValParameter_LocalVariableAssignment_ComputedNameAvoidsCollision()
        {
            var inputCode =
@"
Public Sub Foo(ByVal arg1 As String)
    Dim fooVar, _
        localArg1 As Long
    Let arg1 = ""test""
End Sub"
;

            var expectedCode =
@"
Public Sub Foo(ByVal arg1 As String)
Dim localArg12 As String
localArg12 = arg1
    Dim fooVar, _
        localArg1 As Long
    Let localArg12 = ""test""
End Sub"
;
            var quickFixResult = ApplyLocalVariableQuickFixToCodeFragment(inputCode);
            Assert.AreEqual(expectedCode, quickFixResult);

        }

        [TestMethod]
        [TestCategory("Inspections")]
        public void AssignedByValParameter_LocalVariableAssignment_NameInUseOtherSub()
        {
            //Make sure the modified code stays within the specific method under repair
            var inputCode =
@"
Public Function Bar2(ByVal arg2 As String) As String
    Dim arg1 As String
    Let arg1 = ""Test1""
    Bar2 = arg1
End Function

Public Sub Foo(ByVal arg1 As String)
    Let arg1 = ""test""
End Sub

'VerifyNoChangeBelowThisLine
Public Sub Bar(ByVal arg2 As String)
    Dim arg1 As String
    Let arg1 = ""Test2""
End Sub"
;
            var expectedFragment =
@"
Public Sub Bar(ByVal arg2 As String)
    Dim arg1 As String
    Let arg1 = ""Test2""
End Sub"
;
            string[] splitToken = { "'VerifyNoChangeBelowThisLine" };
            var quickFixResult = ApplyLocalVariableQuickFixToCodeFragment(inputCode);
            var evaluatedFragment = quickFixResult.Split(splitToken, System.StringSplitOptions.None)[1];
            Assert.AreEqual(expectedFragment, evaluatedFragment);
        }

        [TestMethod]
        [TestCategory("Inspections")]
        public void AssignedByValParameter_LocalVariableAssignment_NameInUseOtherProperty()
        {
            //Make sure the modified code stays within the specific method under repair
            var inputCode =
@"
Option Explicit
Private mBar as Long
Public Property Let Foo(ByVal bar As Long)
    bar = 42
    bar = bar * 2
    mBar = bar
End Property

Public Property Get Foo() As Long
    Dim bar as Long
    bar = 12
    Foo = mBar
End Property

'VerifyNoChangeBelowThisLine
Public Function bar() As Long
    Dim localBar As Long
    localBar = 7
    bar = localBar
End Function
";
            string[] splitToken = { "'VerifyNoChangeBelowThisLine" };
            var expectedCode = inputCode.Split(splitToken, System.StringSplitOptions.None)[1];

            var quickFixResult = ApplyLocalVariableQuickFixToCodeFragment(inputCode);
            var evaluatedResult = quickFixResult.Split(splitToken, System.StringSplitOptions.None)[1];
            Assert.AreEqual(expectedCode, evaluatedResult);

        }

        //Addresses issue #2873 : AssignedByValParameter quick fix needs to use `Set` for reference types.
        [TestMethod]
        [TestCategory("Inspections")]
        public void AssignedByValParameter_LocalVariableAssignment_UsesSet()
        {
            var inputCode =
@"
Public Sub Foo(FirstArg As Long, ByVal arg1 As Range)
    arg1 = Range(""A1: C4"")
End Sub"
;

            var expectedCode =
@"
Public Sub Foo(FirstArg As Long, ByVal arg1 As Range)
Dim localArg1 As Range
Set localArg1 = arg1
    localArg1 = Range(""A1: C4"")
End Sub"
;
            var quickFixResult = ApplyLocalVariableQuickFixToCodeFragment(inputCode);
            Assert.AreEqual(expectedCode, quickFixResult);

        }

        private string ApplyLocalVariableQuickFixToCodeFragment(string inputCode, string userEnteredName = "")
        {
            var vbe = BuildMockVBE(inputCode);

            var mockDialogFactory = BuildMockDialogFactory(userEnteredName);

            RubberduckParserState state;
            var inspectionResults = GetInspectionResults(vbe.Object, mockDialogFactory.Object, out state);
            var result = inspectionResults.FirstOrDefault();
            if (result == null)
            {
                Assert.Inconclusive("Inspection yielded no results.");
            }

            result.QuickFixes.Single(s => s is AssignedByValParameterMakeLocalCopyQuickFix).Fix();
            return state.GetRewriter(vbe.Object.ActiveVBProject.VBComponents[0]).GetText();
        }

        private Mock<IVBE> BuildMockVBE(string inputCode)
        {
            IVBComponent component;
            return MockVbeBuilder.BuildFromSingleStandardModule(inputCode, out component);
        }

        private IEnumerable<IInspectionResult> GetInspectionResults(IVBE vbe, IAssignedByValParameterQuickFixDialogFactory mockDialogFactory, out RubberduckParserState state)
        {
            state = MockParser.CreateAndParse(vbe);

            var inspection = new AssignedByValParameterInspection(state, mockDialogFactory);
            return inspection.GetInspectionResults();
        }

        private Mock<IAssignedByValParameterQuickFixDialogFactory> BuildMockDialogFactory(string userEnteredName)
        {
            var mockDialog = new Mock<IAssignedByValParameterQuickFixDialog>();

            mockDialog.SetupAllProperties();

            if (userEnteredName.Length > 0)
            {
                mockDialog.SetupGet(m => m.NewName).Returns(() => userEnteredName);
            }
            mockDialog.SetupGet(m => m.DialogResult).Returns(() => DialogResult.OK);

            var mockDialogFactory = new Mock<IAssignedByValParameterQuickFixDialogFactory>();
            mockDialogFactory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>())).Returns(mockDialog.Object);

            return mockDialogFactory;
        }

        private string GetModuleContent(IVBE vbe, string componentName = "")
        {
            var project = vbe.VBProjects[0];
            var module = componentName.Length > 0
                ? project.VBComponents[componentName].CodeModule : project.VBComponents[0].CodeModule;
            return module.Content();
        }
    }
}
