Imports HostEditorTest.Views
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.TextManager.Interop
Imports System
Imports Microsoft.VisualStudio.OLE.Interop
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio

Friend NotInheritable Class HostEditorTestPane : Inherits WindowPane : Implements IOleCommandTarget

#Region "Constants"

    Private Const WM_KEYFIRST = &H100
    Private Const WM_KEYLAST = &H109

#End Region

#Region "Variables"

    Private mTextViewHost As IWpfTextViewHost

    Private mControl As EditorControl

    Private mFilterKeys As IVsFilterKeys2

#End Region

#Region "Constructors"

    Friend Sub New(codeWindow As IVsCodeWindow, textViewHost As IWpfTextViewHost)
        Me.mTextViewHost = textViewHost

        Me.EditorCommandTarget = CType(codeWindow, IOleCommandTarget)

        ' Create the control
        Me.mControl = New EditorControl

        Me.mControl.Editor.Content = Me.mTextViewHost.HostControl

        Me.Content = Me.mControl

        AddHandler Me.mTextViewHost.TextView.TextBuffer.PostChanged, AddressOf Me.TextPostChanged
    End Sub

#End Region

#Region "Properties"

    Protected ReadOnly Property EditorCommandTarget As IOleCommandTarget

#End Region

    Protected Overrides Function PreProcessMessage(ByRef m As Windows.Forms.Message) As Boolean
        If m.Msg >= WM_KEYFIRST AndAlso m.Msg <= WM_KEYLAST Then
            Dim oleMsg = New MSG With {.hwnd = m.HWnd, .lParam = m.LParam, .wParam = m.WParam, .message = CUInt(m.Msg)}

            If Me.mFilterKeys Is Nothing Then Me.mFilterKeys = Me.GetService(Of SVsFilterKeys, IVsFilterKeys2)

            Return Me.mFilterKeys.TranslateAcceleratorEx({oleMsg}, CUInt(__VSTRANSACCELEXFLAGS.VSTAEXF_UseTextEditorKBScope), 0, Array.Empty(Of Guid)(), Nothing, Nothing, Nothing, Nothing) = VSConstants.S_OK
        End If

        Return MyBase.PreProcessMessage(m)
    End Function

    Public Function Exec(ByRef pguidCmdGroup As Guid, nCmdID As UInteger, nCmdexecopt As UInteger, pvaIn As IntPtr, pvaOut As IntPtr) As Integer Implements IOleCommandTarget.Exec
        If Me.EditorCommandTarget IsNot Nothing Then Return Me.EditorCommandTarget.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut)

        Return OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED
    End Function

    Public Function QueryStatus(ByRef pguidCmdGroup As Guid, cCmds As UInteger, prgCmds As OLECMD(), pCmdText As IntPtr) As Integer Implements IOleCommandTarget.QueryStatus
        If Me.EditorCommandTarget IsNot Nothing Then Return Me.EditorCommandTarget.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText)

        Return OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED
    End Function


    Private Sub TextPostChanged(sender As Object, e As EventArgs)
        Me.mControl.TextBox.Text = Me.mTextViewHost.TextView.TextSnapshot.GetText
    End Sub

End Class
