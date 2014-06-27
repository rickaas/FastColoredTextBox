using System;
using System.Collections.Generic;
using System.Text;
using FastColoredTextBoxNS.EventArgDefs;

namespace FastColoredTextBoxNS.CommandImpl
{
    /// <summary>
    /// Fired by keyboard shortcuts
    /// </summary>
    public class FCTBActionHandler
    {
        private FastColoredTextBox textbox;

        public FCTBActionHandler(FastColoredTextBox textbox)
        {
            this.textbox = textbox;
        }

        public void DoAction(FCTBAction action)
        {
            switch (action)
            {
                case FCTBAction.ZoomIn:
                    this.textbox.ChangeFontSize(2);
                    break;
                case FCTBAction.ZoomOut:
                    this.textbox.ChangeFontSize(-2);
                    break;
                case FCTBAction.ZoomNormal:
                    this.textbox.RestoreFontSize();
                    break;
                case FCTBAction.ScrollDown:
                    this.textbox.DoScrollVertical(1, -1);
                    break;

                case FCTBAction.ScrollUp:
                    this.textbox.DoScrollVertical(1, 1);
                    break;

                case FCTBAction.GoToDialog:
                    EditorCommands.ShowGoToDialog(this.textbox);
                    break;

                case FCTBAction.FindDialog:
                    EditorCommands.ShowFindDialog(this.textbox);
                    break;

                case FCTBAction.FindChar:
                    this.textbox.findCharMode = true;
                    break;

                case FCTBAction.FindNext:
                    if (this.textbox.findForm == null || this.textbox.findForm.FindTextBox.Text == "")
                        EditorCommands.ShowFindDialog(this.textbox);
                    else
                        this.textbox.findForm.FindNext(this.textbox.findForm.FindTextBox.Text);
                    break;
                case FCTBAction.FindPrevious:
                    if (this.textbox.findForm == null || this.textbox.findForm.FindTextBox.Text == "")
                        EditorCommands.ShowFindDialog(this.textbox);
                    else
                        this.textbox.findForm.FindPrevious(this.textbox.findForm.FindTextBox.Text);
                    break;

                case FCTBAction.ReplaceDialog:
                    EditorCommands.ShowReplaceDialog(this.textbox);
                    break;

                case FCTBAction.Copy:
                    EditorCommands.Copy(this.textbox);
                    break;

                case FCTBAction.CommentSelected:
                    EditorCommands.CommentSelected(this.textbox);
                    break;

                case FCTBAction.Cut:
                    if (!this.textbox.Selection.ReadOnly)
                        EditorCommands.Cut(this.textbox);
                    break;

                case FCTBAction.Paste:
                    if (!this.textbox.Selection.ReadOnly)
                        EditorCommands.Paste(this.textbox);
                    break;

                case FCTBAction.SelectAll:
                    this.textbox.Selection.SelectAll();
                    break;

                case FCTBAction.Undo:
                    if (!this.textbox.ReadOnly)
                        this.textbox.Undo();
                    break;

                case FCTBAction.Redo:
                    if (!this.textbox.ReadOnly)
                        this.textbox.Redo();
                    break;

                case FCTBAction.LowerCase:
                    if (!this.textbox.Selection.ReadOnly)
                        EditorCommands.LowerCase(this.textbox);
                    break;

                case FCTBAction.UpperCase:
                    if (!this.textbox.Selection.ReadOnly)
                        EditorCommands.UpperCase(this.textbox);
                    break;

                case FCTBAction.IndentDecrease:
                    if (!this.textbox.Selection.ReadOnly)
                        this.textbox.DecreaseIndent();
                    break;

                case FCTBAction.IndentIncrease:
                    if (!this.textbox.Selection.ReadOnly)
                        this.textbox.IncreaseIndent();
                    break;

                case FCTBAction.AutoIndentSelection:
                    if (!this.textbox.Selection.ReadOnly)
                        this.textbox.DoAutoIndent();
                    break;

                case FCTBAction.NavigateBackward:
                    this.textbox.NavigateBackward();
                    break;

                case FCTBAction.NavigateForward:
                    this.textbox.NavigateForward();
                    break;

                case FCTBAction.UnbookmarkLine:
                    BookmarkCommands.UnbookmarkLine(this.textbox, this.textbox.Selection.Start.iLine);
                    break;

                case FCTBAction.BookmarkLine:
                    BookmarkCommands.BookmarkLine(this.textbox, this.textbox.Selection.Start.iLine);
                    break;

                case FCTBAction.GoNextBookmark:
                    BookmarkCommands.GotoNextBookmark(this.textbox, this.textbox.Selection.Start.iLine);
                    break;

                case FCTBAction.GoPrevBookmark:
                    BookmarkCommands.GotoPrevBookmark(this.textbox, this.textbox.Selection.Start.iLine);
                    break;

                case FCTBAction.ClearWordLeft:
                    if (this.textbox.OnKeyPressing('\b')) //KeyPress event processed key
                        break;
                    if (!this.textbox.Selection.ReadOnly)
                    {
                        if (!this.textbox.Selection.IsEmpty)
                            this.textbox.ClearSelected();
                        this.textbox.Selection.GoWordLeft(true);
                        if (!this.textbox.Selection.ReadOnly)
                            this.textbox.ClearSelected();
                    }
                    this.textbox.OnKeyPressed('\b');
                    break;

                case FCTBAction.ReplaceMode:
                    if (!this.textbox.ReadOnly)
                        this.textbox.isReplaceMode = !this.textbox.isReplaceMode;
                    break;

                case FCTBAction.DeleteCharRight:
                    if (!this.textbox.Selection.ReadOnly)
                    {
                        if (this.textbox.OnKeyPressing((char)0xff)) //KeyPress event processed key
                            break;
                        if (!this.textbox.Selection.IsEmpty)
                            this.textbox.ClearSelected();
                        else
                        {
                            //if line contains only spaces then delete line
                            if (this.textbox.TextSource[this.textbox.Selection.Start.iLine].StartSpacesCount == this.textbox.TextSource[this.textbox.Selection.Start.iLine].GetDisplayWidth(this.textbox.TabLength))
                                this.textbox.RemoveSpacesAfterCaret();

                            if (!this.textbox.Selection.IsReadOnlyRightChar())
                                if (this.textbox.Selection.GoRightThroughFolded())
                                {
                                    int iLine = this.textbox.Selection.Start.iLine;

                                    this.textbox.InsertChar('\b');

                                    //if removed \n then trim spaces
                                    if (iLine != this.textbox.Selection.Start.iLine && this.textbox.AutoIndent)
                                        if (this.textbox.Selection.Start.iChar > 0)
                                            this.textbox.RemoveSpacesAfterCaret();
                                }
                        }
                        this.textbox.OnKeyPressed((char)0xff);
                    }
                    break;

                case FCTBAction.ClearWordRight:
                    if (this.textbox.OnKeyPressing((char)0xff)) //KeyPress event processed key
                        break;
                    if (!this.textbox.Selection.ReadOnly)
                    {
                        if (!this.textbox.Selection.IsEmpty)
                            this.textbox.ClearSelected();
                        this.textbox.Selection.GoWordRight(true);
                        if (!this.textbox.Selection.ReadOnly)
                            this.textbox.ClearSelected();
                    }
                    this.textbox.OnKeyPressed((char)0xff);
                    break;

                case FCTBAction.GoWordLeft:
                    this.textbox.Selection.GoWordLeft(false);
                    break;

                case FCTBAction.GoWordLeftWithSelection:
                    this.textbox.Selection.GoWordLeft(true);
                    break;

                case FCTBAction.GoLeft:
                    this.textbox.Selection.GoLeft(false);
                    break;

                case FCTBAction.GoLeftWithSelection:
                    this.textbox.Selection.GoLeft(true);
                    break;

                case FCTBAction.GoLeft_ColumnSelectionMode:
                    this.textbox.CheckAndChangeSelectionType();
                    if (this.textbox.Selection.ColumnSelectionMode)
                        this.textbox.Selection.GoLeft_ColumnSelectionMode();
                    this.textbox.Invalidate();
                    break;

                case FCTBAction.GoWordRight:
                    this.textbox.Selection.GoWordRight(false);
                    break;

                case FCTBAction.GoWordRightWithSelection:
                    this.textbox.Selection.GoWordRight(true);
                    break;

                case FCTBAction.GoRight:
                    this.textbox.Selection.GoRight(false);
                    break;

                case FCTBAction.GoRightWithSelection:
                    this.textbox.Selection.GoRight(true);
                    break;

                case FCTBAction.GoRight_ColumnSelectionMode:
                    this.textbox.CheckAndChangeSelectionType();
                    if (this.textbox.Selection.ColumnSelectionMode)
                        this.textbox.Selection.GoRight_ColumnSelectionMode();
                    this.textbox.Invalidate();
                    break;

                case FCTBAction.GoUp:
                    this.textbox.Selection.GoUp(false);
                    this.textbox.ScrollLeft();
                    break;

                case FCTBAction.GoUpWithSelection:
                    this.textbox.Selection.GoUp(true);
                    this.textbox.ScrollLeft();
                    break;

                case FCTBAction.GoUp_ColumnSelectionMode:
                    this.textbox.CheckAndChangeSelectionType();
                    if (this.textbox.Selection.ColumnSelectionMode)
                        this.textbox.Selection.GoUp_ColumnSelectionMode();
                    this.textbox.Invalidate();
                    break;

                case FCTBAction.MoveSelectedLinesUp:
                    if (!this.textbox.Selection.ColumnSelectionMode)
                        MoveCommands.MoveSelectedLinesUp(this.textbox);
                    break;

                case FCTBAction.GoDown:
                    this.textbox.Selection.GoDown(false);
                    this.textbox.ScrollLeft();
                    break;

                case FCTBAction.GoDownWithSelection:
                    this.textbox.Selection.GoDown(true);
                    this.textbox.ScrollLeft();
                    break;

                case FCTBAction.GoDown_ColumnSelectionMode:
                    this.textbox.CheckAndChangeSelectionType();
                    if (this.textbox.Selection.ColumnSelectionMode)
                        this.textbox.Selection.GoDown_ColumnSelectionMode();
                    this.textbox.Invalidate();
                    break;

                case FCTBAction.MoveSelectedLinesDown:
                    if (!this.textbox.Selection.ColumnSelectionMode)
                        MoveCommands.MoveSelectedLinesDown(this.textbox);
                    break;
                case FCTBAction.GoPageUp:
                    this.textbox.Selection.GoPageUp(false);
                    this.textbox.ScrollLeft();
                    break;

                case FCTBAction.GoPageUpWithSelection:
                    this.textbox.Selection.GoPageUp(true);
                    this.textbox.ScrollLeft();
                    break;

                case FCTBAction.GoPageDown:
                    this.textbox.Selection.GoPageDown(false);
                    this.textbox.ScrollLeft();
                    break;

                case FCTBAction.GoPageDownWithSelection:
                    this.textbox.Selection.GoPageDown(true);
                    this.textbox.ScrollLeft();
                    break;

                case FCTBAction.GoFirstLine:
                    this.textbox.Selection.GoFirst(false);
                    break;

                case FCTBAction.GoFirstLineWithSelection:
                    this.textbox.Selection.GoFirst(true);
                    break;

                case FCTBAction.GoHome:
                    this.textbox.GoHome(false);
                    this.textbox.ScrollLeft();
                    break;

                case FCTBAction.GoHomeWithSelection:
                    this.textbox.GoHome(true);
                    this.textbox.ScrollLeft();
                    break;

                case FCTBAction.GoLastLine:
                    this.textbox.Selection.GoLast(false);
                    break;

                case FCTBAction.GoLastLineWithSelection:
                    this.textbox.Selection.GoLast(true);
                    break;

                case FCTBAction.GoEnd:
                    this.textbox.Selection.GoEnd(false);
                    break;

                case FCTBAction.GoEndWithSelection:
                    this.textbox.Selection.GoEnd(true);
                    break;

                case FCTBAction.ClearHints:
                    this.textbox.Hints.Clear();
                    if (this.textbox.MacrosManager != null)
                        this.textbox.MacrosManager.IsRecording = false;
                    break;

                case FCTBAction.MacroRecord:
                    if (this.textbox.MacrosManager != null)
                    {
                        if (this.textbox.MacrosManager.AllowMacroRecordingByUser)
                            this.textbox.MacrosManager.IsRecording = !this.textbox.MacrosManager.IsRecording;
                        if (this.textbox.MacrosManager.IsRecording)
                            this.textbox.MacrosManager.ClearMacros();
                    }
                    break;

                case FCTBAction.MacroExecute:
                    if (this.textbox.MacrosManager != null)
                    {
                        this.textbox.MacrosManager.IsRecording = false;
                        this.textbox.MacrosManager.ExecuteMacros();
                    }
                    break;
                case FCTBAction.CustomAction1:
                case FCTBAction.CustomAction2:
                case FCTBAction.CustomAction3:
                case FCTBAction.CustomAction4:
                case FCTBAction.CustomAction5:
                case FCTBAction.CustomAction6:
                case FCTBAction.CustomAction7:
                case FCTBAction.CustomAction8:
                case FCTBAction.CustomAction9:
                case FCTBAction.CustomAction10:
                case FCTBAction.CustomAction11:
                case FCTBAction.CustomAction12:
                case FCTBAction.CustomAction13:
                case FCTBAction.CustomAction14:
                case FCTBAction.CustomAction15:
                case FCTBAction.CustomAction16:
                case FCTBAction.CustomAction17:
                case FCTBAction.CustomAction18:
                case FCTBAction.CustomAction19:
                case FCTBAction.CustomAction20:
                    this.textbox.OnCustomAction(new CustomActionEventArgs(action));
                    break;
            }
        }
    }
}
