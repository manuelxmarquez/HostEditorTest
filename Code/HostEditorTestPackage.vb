Imports System
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Task = System.Threading.Tasks.Task

<PackageRegistration(UseManagedResourcesOnly:=True, AllowsBackgroundLoading:=True)>
<Guid(Constants.PackageGuid)>
<ProvideEditorExtension(GetType(HostEditorTestFactory), HostEditorTestFactory.FileExtensionWithPeriod, &H40)>
<ProvideEditorLogicalView(GetType(HostEditorTestFactory), LogicalViewID.Designer)>
<ProvideXmlEditorChooserDesignerView("Host Document Editor", HostEditorTestFactory.FileExtension, LogicalViewID.Designer, &H60, MatchExtensionAndNamespace:=False,
                                     CodeLogicalViewEditor:=GetType(HostEditorTestFactory),
                                     DesignerLogicalViewEditor:=GetType(HostEditorTestFactory),
                                     TextLogicalViewEditor:=GetType(HostEditorTestFactory),
                                     DebuggingLogicalViewEditor:=GetType(HostEditorTestFactory))>
Friend NotInheritable Class HostEditorTestPackage : Inherits AsyncPackage

    Protected Overrides Async Function InitializeAsync(cancellationToken As CancellationToken, progress As IProgress(Of ServiceProgressData)) As Task
        Await MyBase.InitializeAsync(cancellationToken, progress)

        Me.RegisterEditorFactory(New HostEditorTestFactory)
    End Function

End Class
