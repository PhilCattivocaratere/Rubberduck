using Interop.SldWorks.Types;
using Rubberduck.VBEditor.SafeComWrappers.VBA.Abstract;

namespace Rubberduck.VBEditor.VBEHost
{
    public class SolidWorksApp : HostApplicationBase<Interop.SldWorks.Extensibility.Application>
    {
        public SolidWorksApp() : base("SolidWorks") { }
        public SolidWorksApp(IVBE vbe) : base(vbe, "SolidWorks") { }
		
        public override void Run(QualifiedMemberName qualifiedMemberName)
        {
            var projectFileName = qualifiedMemberName.QualifiedModuleName.Project.FileName;
            var moduleName = qualifiedMemberName.QualifiedModuleName.ComponentName;
            var memberName = qualifiedMemberName.MemberName;

            if (Application != null)
            {
                SldWorks runner = (SldWorks)Application.SldWorks;
                runner.RunMacro(projectFileName, moduleName, memberName);
            }
        }
    }
}
