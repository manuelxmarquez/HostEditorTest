Imports System
Imports System.Runtime.InteropServices
Imports System.Windows
Imports System.Windows.Controls
Imports Microsoft.VisualStudio
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.Editor
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.TextManager.Interop

<Guid(Constants.HostEditorTestFactoryGuidString)>
Friend Class HostEditorTestFactory : Implements IVsEditorFactory, IDisposable

#Region "Constants"

    Friend Const FileExtension = "test"

    Friend Const FileExtensionWithPeriod = "." & FileExtension

#End Region

#Region "Variables"

    Private mOleServiceProvider As Microsoft.VisualStudio.OLE.Interop.IServiceProvider

    Private mServiceProvider As ServiceProvider

#End Region

#Region "Constructors"

    Friend Sub New()
    End Sub

#End Region

#Region "Destructors"

    Private mIsDisposed As Boolean

    Public ReadOnly Property IsDisposed As Boolean
        Get
            Return Me.mIsDisposed
        End Get
    End Property

    Protected Overrides Sub Finalize()
        Me.Dispose(False)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Me.Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

    Protected Sub Dispose(disposing As Boolean)
        If Me.mIsDisposed Then Return

        Me.mIsDisposed = True

        If disposing Then
            Me.mServiceProvider?.Dispose()

            Me.mServiceProvider = Nothing
        End If
    End Sub

#End Region

    Public Function SetSite(psp As Microsoft.VisualStudio.OLE.Interop.IServiceProvider) As Integer Implements IVsEditorFactory.SetSite
        Me.mOleServiceProvider = psp
        Me.mServiceProvider = New ServiceProvider(psp)

        Return VSConstants.S_OK
    End Function

    Public Function MapLogicalView(ByRef rguidLogicalView As Guid, ByRef pbstrPhysicalView As String) As Integer Implements IVsEditorFactory.MapLogicalView
        pbstrPhysicalView = Nothing

        If rguidLogicalView = VSConstants.LOGVIEWID_Primary OrElse rguidLogicalView = VSConstants.LOGVIEWID_Code OrElse
                rguidLogicalView = VSConstants.LOGVIEWID_Debugging OrElse rguidLogicalView = VSConstants.LOGVIEWID_TextView OrElse
                rguidLogicalView = VSConstants.LOGVIEWID_Designer Then Return VSConstants.S_OK

        Return VSConstants.E_NOTIMPL
    End Function

    Public Function CreateEditorInstance(grfCreateDoc As UInteger, pszMkDocument As String, pszPhysicalView As String, pvHier As IVsHierarchy, itemid As UInteger, punkDocDataExisting As IntPtr, ByRef ppunkDocView As IntPtr, ByRef ppunkDocData As IntPtr, ByRef pbstrEditorCaption As String, ByRef pguidCmdUI As Guid, ByRef pgrfCDW As Integer) As Integer Implements IVsEditorFactory.CreateEditorInstance
        ppunkDocView = IntPtr.Zero
        ppunkDocData = IntPtr.Zero
        pguidCmdUI = Constants.HostEditorTestFactoryGuid
        pgrfCDW = 0
        pbstrEditorCaption = Nothing

        If (grfCreateDoc And (VSConstants.CEF_OPENFILE Or VSConstants.CEF_SILENT)) = 0 Then Return VSConstants.E_INVALIDARG

        Dim textBuffer As IVsTextBuffer

        If punkDocDataExisting = IntPtr.Zero Then
            Dim invisibleEditorManager = CType(Me.mServiceProvider.GetService(GetType(SVsInvisibleEditorManager)), IVsInvisibleEditorManager)

            Dim invisibleEditor As IVsInvisibleEditor = Nothing
            ErrorHandler.ThrowOnFailure(invisibleEditorManager.RegisterInvisibleEditor(pszMkDocument, Nothing, CUInt(_EDITORREGFLAGS.RIEF_ENABLECACHING), Nothing, invisibleEditor))

            Dim docDataPointer = IntPtr.Zero
            ErrorHandler.ThrowOnFailure(invisibleEditor.GetDocData(1, GetType(IVsTextBuffer).GUID, docDataPointer))

            textBuffer = CType(Marshal.GetObjectForIUnknown(docDataPointer), IVsTextBuffer)
        Else
            textBuffer = TryCast(Marshal.GetObjectForIUnknown(punkDocDataExisting), IVsTextBuffer)

            If textBuffer Is Nothing Then ErrorHandler.ThrowOnFailure(VSConstants.VS_E_INCOMPATIBLEDOCDATA)
        End If

        ErrorHandler.ThrowOnFailure(textBuffer.SetLanguageServiceID(Constants.XmlLanguageServiceGuid))

        Dim componentModel = CType(Me.mServiceProvider.GetService(GetType(SComponentModel)), IComponentModel)

        Dim editorAdapterFactory = componentModel.GetService(Of IVsEditorAdaptersFactoryService)

        Dim codeWindow = editorAdapterFactory.CreateVsCodeWindowAdapter(Me.mOleServiceProvider)

        ' Disable the splitter which causes a crash
        Dim initView = {New INITVIEW}
        ErrorHandler.ThrowOnFailure(CType(codeWindow, IVsCodeWindowEx).Initialize(CUInt(_codewindowbehaviorflags.CWB_DISABLESPLITTER), VSUSERCONTEXTATTRIBUTEUSAGE.VSUC_Usage_Filter, "", "", 0, initView))

        ErrorHandler.ThrowOnFailure(codeWindow.SetBuffer(CType(textBuffer, IVsTextLines)))

        Dim textView As IVsTextView = Nothing
        ErrorHandler.ThrowOnFailure(codeWindow.GetPrimaryView(textView))

        Dim textViewHost = editorAdapterFactory.GetWpfTextViewHost(textView)

        If textViewHost Is Nothing Then Return VSConstants.VS_E_INCOMPATIBLEDOCDATA

        RemoveParentControl(textViewHost.HostControl)

        Dim editor As New HostEditorTestPane(codeWindow, textViewHost)
        ppunkDocView = Marshal.GetIUnknownForObject(editor)
        ppunkDocData = Marshal.GetIUnknownForObject(textBuffer)
        pbstrEditorCaption = ""

        Marshal.Release(ppunkDocView)
        Marshal.Release(ppunkDocData)

        Return VSConstants.S_OK
    End Function

    Public Function Close() As Integer Implements IVsEditorFactory.Close
        Return VSConstants.S_OK
    End Function


    Private Shared Sub RemoveParentControl(control As FrameworkElement)
        If control.Parent Is Nothing Then Return

        Dim parentPanel = TryCast(control.Parent, Panel)
        If parentPanel IsNot Nothing Then parentPanel.Children.Remove(control)

        Dim parentContentControl = TryCast(control.Parent, ContentControl)
        If parentContentControl IsNot Nothing AndAlso parentContentControl.Content Is control Then parentContentControl.Content = Nothing

        Dim parentDecorator = TryCast(control.Parent, Decorator)
        If parentDecorator IsNot Nothing AndAlso parentDecorator.Child Is control Then parentDecorator.Child = Nothing
    End Sub

End Class
