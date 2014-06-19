//
//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE.
//
//  License: GNU Lesser General Public License (LGPLv3)
//
//  Email: pavel_torgashov@ukr.net.
//
//  Copyright (C) Pavel Torgashov, 2011-2014. 

//#define debug


// -------------------------------------------------------------------------------
// By default the FastColoredTextbox supports no more 16 styles at the same time.
// This restriction saves memory.
// However, you can to compile FCTB with 32 styles supporting.
// Uncomment following definition if you need 32 styles instead of 16:
// #define Styles32

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using FastColoredTextBoxNS.Bookmarking;
using FastColoredTextBoxNS.CommandImpl;
using FastColoredTextBoxNS.EventArgDefs;
using Microsoft.Win32;
using Timer = System.Windows.Forms.Timer;

namespace FastColoredTextBoxNS
{
    /// <summary>
    /// Fast colored textbox
    /// </summary>
    public partial class FastColoredTextBox : UserControl, ISupportInitialize
    {
        internal const int MIN_LEFT_INDENT = 8;
        internal const int MAX_BRACKET_SEARCH_ITERATIONS = 2000;
        private const int MAX_LINES_FOR_FOLDING = 3000;
        private const int MIN_LINES_FOR_ACCURACY = 100000;

        private const int WM_IME_SETCONTEXT = 0x0281;
        private const int WM_HSCROLL = 0x114;
        private const int WM_VSCROLL = 0x115;
        private const int SB_ENDSCROLL = 0x8;

        public readonly List<LineInfo> LineInfos = new List<LineInfo>();
        private readonly Range selection;

        private readonly Timer delayedEventsTimer = new Timer();
        private readonly Timer delayedTextChangedTimer = new Timer();
        private readonly Timer tooltipDelayTimer = new Timer();

        // updated by Rendering.DrawLines(...) and this.AddVisualMarker(...)
        internal readonly List<VisualMarker> visibleMarkers = new List<VisualMarker>();

        /// <summary>
        /// TextHeight is calculated in RecalcMaxLineLength.
        /// It loops over all LineInfos sums the values of (lineInfo.WordWrapStringsCount*charHeight + lineInfo.bottomPadding);
        /// </summary>
        public int TextHeight { get; private set; }

        internal bool allowInsertRemoveLines = true;

        // drawing
        private Brush backBrush;

        // colors
        private Color changedLineColor;
        private Color currentLineColor;
        private Color foldingIndicatorColor;
        private Color indentBackColor;
        private Color lineNumberColor;
        private Color paddingBackColor;
        private Color selectionColor;
        private Color serviceLinesColor;


        // references to other objects
        internal TextSource lines;
        public BaseBookmarks Bookmarks { get; private set; }
        internal Hints hints;
        private Language language;
        private FastColoredTextBox sourceTextBox;


        
        
        private int charHeight;
        
        private Cursor defaultCursor;
        private Range delayedTextChangedRange;
        private string descriptionFile;


        
        internal readonly Dictionary<int, int> foldingPairs = new Dictionary<int, int>();

        private bool handledChar;
        private bool highlightFoldingIndicator;
        
        
        private bool isChanged;
        private bool isLineSelect;
        public bool isReplaceMode;
        

        private Keys lastModifiers;
        private Point lastMouseCoord;

        // used by Navigate, NavigateForward and NavigateBackward
        private DateTime lastNavigatedDateTime;

        internal Range leftBracketPosition;
        internal Range leftBracketPosition2;

        private int leftPadding;
        private int lineInterval;

        // Start value of first line number.
        internal uint lineNumberStartValue;

        private int lineSelectFrom;

        

        private IntPtr m_hImc;
        private int maxLineLength;
        private bool mouseIsDrag;
        private bool mouseIsDragDrop;

        /// <summary>
        /// When false only a single line is allowed.
        /// When true the textbox as scrollbars and allows multiple lines
        /// </summary>
        private bool multiline;

        private bool scrollBars;

        /// <summary>
        /// Set to true when recalc of the position of lines is needed.
        /// </summary>
        internal bool needRecalc;

        private bool needRecalcWordWrap;

        /// <summary>
        /// Point struct is abused to indicate the interval
        /// X = From Line
        /// Y = To Line
        /// </summary>
        private Point needRecalcWordWrapInterval;
        internal bool needRecalcFoldingLines;

        private bool needRiseSelectionChangedDelayed;
        private bool needRiseTextChangedDelayed;
        private bool needRiseVisibleRangeChangedDelayed;

        private int preferredLineWidth;
        internal Range rightBracketPosition;
        internal Range rightBracketPosition2;

        


        private bool showFoldingLines;
        private bool showLineNumbers;

        // Start line index of current highlighted folding area. Return -1 if start of area is not found.
        private int startFoldingLine = -1;
        // End line index of current highlighted folding area. Return -1 if end of area is not found.
        private int endFoldingLine = -1;

        // keeps track of BeginUpdate()
        private int updating;

        private Range updatingRange;
        internal Range visibleRange;

        private bool wordWrap;

        private WordWrapMode wordWrapMode = WordWrapMode.WordWrapControlWidth;

        private int reservedCountOfLineNumberChars = 1;


        // cache location of caret for drawing purposes, see Rendering.DrawCaret(...)
        internal Rectangle prevCaretRect;

        private Size localAutoScrollMinSize;

        private readonly FCTBActionHandler FCTBActionHandler;

        /// <summary>
        /// Constructor
        /// </summary>
        public FastColoredTextBox()
        {
            //register type provider
            TypeDescriptionProvider prov = TypeDescriptor.GetProvider(GetType());
            object theProvider =
                prov.GetType().GetField("Provider", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(prov);
            if (theProvider.GetType() != typeof (FCTBDescriptionProvider))
                TypeDescriptor.AddProvider(new FCTBDescriptionProvider(GetType()), GetType());

            //drawing optimization
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);

            // init child objects
            this.SyntaxHighlighter = new SyntaxHighlighter();
            this.Bookmarks = new Bookmarks(this);
            this.macrosManager = new MacrosManager(this);
            this.HotkeysMapping = new HotkeysMapping();
            this.HotkeysMapping.InitDefault();
            this.hints = new Hints(this);
            this.FCTBActionHandler = new FCTBActionHandler(this);



            // init data
            needRecalc = true;
            needRecalcFoldingLines = true;

            lastNavigatedDateTime = DateTime.Now;

            this.FoldedBlocks = new Dictionary<int, int>();

            //create one line
            InitTextSource(CreateTextSource());
            if (lines.Count == 0)
                lines.InsertLine(0, lines.CreateLine());
            selection = new Range(this) { Start = new Place(0, 0) };

            language = Language.Custom;

            this.DefaultEolFormat = EolFormat.CRLF;

            // init appearance properties

            //append monospace font
            //Font = new Font("Consolas", 9.75f, FontStyle.Regular, GraphicsUnit.Point);
            Font = new Font(FontFamily.GenericMonospace, 9.75f);

            //default settings
            Cursor = Cursors.IBeam;

            // color scheme
            BackColor = Color.White;
            LineNumberColor = Color.Teal;
            IndentBackColor = Color.WhiteSmoke;
            ServiceLinesColor = Color.Silver;
            FoldingIndicatorColor = Color.Green;
            CurrentLineColor = Color.Transparent;
            ChangedLineColor = Color.Transparent;
            SelectionColor = Color.Blue;

            HighlightFoldingIndicator = true;
            ShowLineNumbers = true;
            TabLength = 4;

            FoldedBlockStyle = new FoldedBlockStyle(Brushes.Gray, null, FontStyle.Regular);
            BracketsStyle = new MarkerStyle(new SolidBrush(Color.FromArgb(80, Color.Lime)));
            BracketsStyle2 = new MarkerStyle(new SolidBrush(Color.FromArgb(60, Color.Red)));

            DelayedEventsInterval = 100;
            DelayedTextChangedInterval = 100;

            AllowSeveralTextStyleDrawing = false;

            LeftBracket = '\x0';
            RightBracket = '\x0';
            LeftBracket2 = '\x0';
            RightBracket2 = '\x0';

            PreferredLineWidth = 0;

            AutoIndent = true;
            AutoIndentExistingLines = true;

            CommentPrefix = "//";

            lineNumberStartValue = 1;

            multiline = true;
            scrollBars = true;
            AcceptsTab = true;
            AcceptsReturn = true;
            caretVisible = true;
            CaretColor = Color.Black;
            WideCaret = false;

            Paddings = new Padding(0, 0, 0, 0);
            PaddingBackColor = Color.Transparent;
            DisabledColor = Color.FromArgb(100, 180, 180, 180);

            

            AllowDrop = true;

            FindEndOfFoldingBlockStrategy = FindEndOfFoldingBlockStrategy.Strategy1;
            VirtualSpace = false;
            
            BookmarkColor = Color.PowderBlue;

            ToolTip = new ToolTip();
            tooltipDelayTimer.Interval = 500;
            
            SelectionHighlightingForLineBreaksEnabled = true;
            textAreaBorder = TextAreaBorderType.None;
            textAreaBorderColor = Color.Black;



            WordWrapAutoIndent = true;
            
            AutoCompleteBrackets = true;
            //
            base.AutoScroll = true;

            delayedEventsTimer.Tick += DelayedEventsTimerTick; // fire OnSelectionChangedDelayed and OnVisibleRangeChangedDelayed
            delayedTextChangedTimer.Tick += DelayedTextChangedTimerTick; // OnTextChangedDelayed
            tooltipDelayTimer.Tick += TooltipDelayTimerTick; // show tooltip
            middleClickScrollingTimer.Tick += middleClickScrollingTimer_Tick;

            
        }

        // =============================
        // data structures
        // =============================

        /// <summary>
        /// Contains UniqueId of start lines of folded blocks
        /// </summary>
        /// <remarks>This dictionary remembers folding state of blocks.
        /// It is needed to restore child folding after user collapsed/expanded top-level folding block.</remarks>
        [Browsable(false)]
        public Dictionary<int, int> FoldedBlocks { get; private set; }

        private readonly MacrosManager macrosManager;

        /// <summary>
        /// MacrosManager records, stores and executes the macroses
        /// </summary>
        [Browsable(false)]
        public MacrosManager MacrosManager { get { return macrosManager; } }

        /// <summary>
        /// Collection of Hints.
        /// This is temporary buffer for currently displayed hints.
        /// </summary>
        /// <remarks>You can asynchronously add, remove and clear hints. Appropriate hints will be shown or hidden from the screen.</remarks>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
         EditorBrowsable(EditorBrowsableState.Never)]
        public Hints Hints
        {
            get { return hints; }
        }

        /// <summary>
        /// Text was changed
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsChanged
        {
            get { return isChanged; }
            set
            {
                if (!value)
                    //clear line's IsChanged property
                    lines.ClearIsChanged();

                isChanged = value;
            }
        }

        /// <summary>
        /// Text version
        /// </summary>
        /// <remarks>This counter is incremented each time changes the text</remarks>
        [Browsable(false)]
        public int TextVersion { get; private set; }

        /// <summary>
        /// Rectangle where located text
        /// </summary>
        [Browsable(false)]
        public Rectangle TextAreaRect
        {
            get
            {
                int rightPaddingStartX = LeftIndent + maxLineLength * CharWidth + Paddings.Left + 1;
                rightPaddingStartX = Math.Max(ClientSize.Width - Paddings.Right, rightPaddingStartX);
                int bottomPaddingStartY = this.TextHeight + this.Paddings.Top;
                bottomPaddingStartY = Math.Max(ClientSize.Height - Paddings.Bottom, bottomPaddingStartY);
                var top = Math.Max(0, Paddings.Top - 1) - VerticalScroll.Value;
                var left = LeftIndent - HorizontalScroll.Value - 2 + Math.Max(0, Paddings.Left - 1);
                var rect = Rectangle.FromLTRB(left, top, rightPaddingStartX - HorizontalScroll.Value, bottomPaddingStartY - VerticalScroll.Value);
                return rect;
            }
        }

        /// <summary>
        /// Styles
        /// </summary>
        [Browsable(false)]
        public Style[] Styles
        {
            get { return lines.Styles; }
        }

        /// <summary>
        /// TextSource
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TextSource TextSource
        {
            get { return lines; }
            set { InitTextSource(value); }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HasSourceTextBox
        {
            get { return SourceTextBox != null; }
        }

        /// <summary>
        /// The source of the text.
        /// Allows to get text from other FastColoredTextBox.
        /// </summary>
        [Browsable(true)]
        [DefaultValue(null)]
        [Description("Allows to get text from other FastColoredTextBox.")]
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FastColoredTextBox SourceTextBox
        {
            get { return sourceTextBox; }
            set
            {
                if (value == sourceTextBox)
                    return;

                sourceTextBox = value;

                if (sourceTextBox == null)
                {
                    InitTextSource(CreateTextSource());
                    lines.InsertLine(0, TextSource.CreateLine());
                    IsChanged = false;
                }
                else
                {
                    InitTextSource(SourceTextBox.TextSource);
                    isChanged = false;
                }
                Invalidate();
            }
        }

        /// <summary>
        /// Position of left highlighted bracket.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Range LeftBracketPosition
        {
            get { return leftBracketPosition; }
        }

        /// <summary>
        /// Position of right highlighted bracket.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Range RightBracketPosition
        {
            get { return rightBracketPosition; }
        }

        /// <summary>
        /// Position of left highlighted alternative bracket.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Range LeftBracketPosition2
        {
            get { return leftBracketPosition2; }
        }

        /// <summary>
        /// Position of right highlighted alternative bracket.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Range RightBracketPosition2
        {
            get { return rightBracketPosition2; }
        }

        /// <summary>
        /// Start line index of current highlighted folding area. Return -1 if start of area is not found.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int StartFoldingLine
        {
            get { return startFoldingLine; }
        }

        /// <summary>
        /// End line index of current highlighted folding area. Return -1 if end of area is not found.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int EndFoldingLine
        {
            get { return endFoldingLine; }
        }

        [Browsable(false)]
        public IFindForm findForm { get; private set; }

        [Browsable(false)]
        public ReplaceForm replaceForm { get; private set; }

        /// <summary>
        /// Count of lines
        /// </summary>
        [Browsable(false)]
        public int LinesCount
        {
            get { return lines.Count; }
        }

        /// <summary>
        /// Gets or sets char and styleId for given place
        /// This property does not fire OnTextChanged event
        /// </summary>
        public Char this[Place place]
        {
            get { return lines[place.iLine][place.iChar]; }
            set { lines[place.iLine][place.iChar] = value; }
        }

        /// <summary>
        /// Gets Line
        /// </summary>
        public Line this[int iLine]
        {
            get { return lines[iLine]; }
        }

        /// <summary>
        /// Returns current visible range of text
        /// </summary>
        [Browsable(false)]
        public Range VisibleRange
        {
            get
            {
                if (visibleRange != null)
                    return visibleRange;
                return RangeUtil.GetRange(this,
                    PointToPlace(new Point(LeftIndent, 0)),
                    PointToPlace(new Point(ClientSize.Width, ClientSize.Height))
                    );
            }
        }

        /// <summary>
        /// Current selection range
        /// </summary>
        [Browsable(false)]
        public Range Selection
        {
            get { return selection; }
            set
            {
                selection.BeginUpdate();
                selection.Start = value.Start;
                selection.End = value.End;
                selection.EndUpdate();
                Invalidate();
            }
        }

        /// <summary>
        /// Text of current selection
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedText
        {
            get { return Selection.Text; }
            set { InsertText(value); }
        }

        /// <summary>
        /// Start position of selection
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectionStart
        {
            get { return Math.Min(TextSourceUtil.PlaceToPosition(this.lines, Selection.Start), TextSourceUtil.PlaceToPosition(this.lines, Selection.End)); }
            set { Selection.Start = TextSourceUtil.PositionToPlace(this.lines, value); }
        }

        /// <summary>
        /// Length of selected text
        /// </summary>
        [Browsable(false)]
        [DefaultValue(0)]
        public int SelectionLength
        {
            get { return Math.Abs(TextSourceUtil.PlaceToPosition(this.lines, Selection.Start) - TextSourceUtil.PlaceToPosition(this.lines, Selection.End)); }
            set
            {
                if (value > 0)
                    Selection.End = TextSourceUtil.PositionToPlace(this.lines, SelectionStart + value);
            }
        }

        /// <summary>
        /// Is undo enabled?
        /// </summary>
        [Browsable(false)]
        public bool UndoEnabled
        {
            get { return lines.Manager.UndoEnabled; }
        }

        /// <summary>
        /// Is redo enabled?
        /// </summary>
        [Browsable(false)]
        public bool RedoEnabled
        {
            get { return lines.Manager.RedoEnabled; }
        }

        public int LeftIndentLine
        {
            get { return LeftIndent - MIN_LEFT_INDENT / 2 - 3; }
        }

        /// <summary>
        /// Range of all text
        /// </summary>
        [Browsable(false)]
        public Range Range
        {
            get { return new Range(this, new Place(0, 0), new Place(lines[lines.Count - 1].Count, lines.Count - 1)); }
        }

        /// <summary>
        /// Text of control
        /// </summary>
        [Browsable(true)]
        [Localizable(true)]
        [Editor(
            "System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
            , typeof(UITypeEditor))]
        [SettingsBindable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Text of the control.")]
        [Bindable(true)]
        public override string Text
        {
            get
            {
                if (LinesCount == 0)
                    return "";
                var sel = new Range(this);
                sel.SelectAll();
                return sel.Text;
            }

            set
            {
                if (value == Text && value != "")
                    return;

                SetAsCurrentTB();

                Selection.ColumnSelectionMode = false;

                Selection.BeginUpdate();
                try
                {
                    Selection.SelectAll();
                    this.DefaultEolFormat = EolFormat.None;
                    InsertText(value);
                    GoHome();
                    TryDeriveEolFormat();
                }
                finally
                {
                    Selection.EndUpdate();
                }
            }
        }

        // =============================
        // appearance and behavior properties
        // =============================
        #region appearance and behavior properties

        /// <summary>
        /// Defines the default end-of-line format: LN, CR/LN, CR.
        /// This is used when pressing Enter.
        /// </summary>
        public EolFormat DefaultEolFormat { get; set; }

        private char[] autoCompleteBracketsList = { '(', ')', '{', '}', '[', ']', '"', '"', '\'', '\'' };

        public char[] AutoCompleteBracketsList
        {
            get { return autoCompleteBracketsList; }
            set { autoCompleteBracketsList = value; }
        }

        /// <summary>
        /// AutoComplete brackets
        /// </summary>
        [DefaultValue(true)]
        [Description("AutoComplete brackets.")]
        public bool AutoCompleteBrackets { get; set; }

        /// <summary>
        /// Strategy of search of brackets to highlighting
        /// </summary>
        [DefaultValue(typeof(BracketsHighlightStrategy), "Strategy1")]
        [Description("Strategy of search of brackets to highlighting.")]
        public BracketsHighlightStrategy BracketsHighlightStrategy { get; set; }
        
        /// <summary>
        /// Automatically shifts secondary wordwrap lines on the shift amount of the first line
        /// </summary>
        [DefaultValue(true)]
        [Description("Automatically shifts secondary wordwrap lines on the shift amount of the first line.")]
        public bool WordWrapAutoIndent { get; set; }

        /// <summary>
        /// Indent of secondary wordwrap lines (in chars)
        /// </summary>
        [DefaultValue(0)]
        [Description("Indent of secondary wordwrap lines (in chars).")]
        public int WordWrapIndent { get; set; }

        /// <summary>
        /// Allows drag and drop
        /// </summary>
        [DefaultValue(true)]
        [Description("Allows drag and drop")]
        public override bool AllowDrop
        {
            get { return base.AllowDrop; }
            set { base.AllowDrop = value; }
        }

        /// <summary>
        /// Delay (ms) of ToolTip
        /// </summary>
        [Browsable(true)]
        [DefaultValue(500)]
        [Description("Delay(ms) of ToolTip.")]
        public int ToolTipDelay
        {
            get { return tooltipDelayTimer.Interval; }
            set { tooltipDelayTimer.Interval = value; }
        }

        /// <summary>
        /// ToolTip component
        /// </summary>
        [Browsable(true)]
        [Description("ToolTip component.")]
        public ToolTip ToolTip { get; set; }

        /// <summary>
        /// Color of bookmarks
        /// </summary>
        [Browsable(true)]
        [DefaultValue(typeof (Color), "PowderBlue")]
        [Description("Color of bookmarks.")]
        public Color BookmarkColor { get; set; }

        /// <summary>
        /// Enables virtual spaces
        /// </summary>
        [DefaultValue(false)]
        [Description("Enables virtual spaces.")]
        public bool VirtualSpace { get; set; }

        /// <summary>
        /// Strategy of search of end of folding block
        /// </summary>
        [DefaultValue(FindEndOfFoldingBlockStrategy.Strategy1)]
        [Description("Strategy of search of end of folding block.")]
        public FindEndOfFoldingBlockStrategy FindEndOfFoldingBlockStrategy { get; set; }

        /// <summary>
        /// Indicates if tab characters are accepted as input
        /// </summary>
        [DefaultValue(true)]
        [Description("Indicates if tab characters are accepted as input.")]
        public bool AcceptsTab { get; set; }

        /// <summary>
        /// Indicates if return characters are accepted as input
        /// </summary>
        [DefaultValue(true)]
        [Description("Indicates if return characters are accepted as input.")]
        public bool AcceptsReturn { get; set; }

        /// <summary>
        /// Indicates if tabs are converted to spaces.
        /// Use TabLength to set the Spaces count for a tab.
        /// </summary>
        [DefaultValue(false)]
        [Description("Indicates if tabs are converted to spaces.")]
        public bool ConvertTabToSpaces { get; set; }


        private bool caretVisible;

        /// <summary>
        /// Shows or hides the caret
        /// </summary>
        [DefaultValue(true)]
        [Description("Shows or hides the caret")]
        public bool CaretVisible
        {
            get { return caretVisible; }
            set
            {
                caretVisible = value;
                Invalidate();
            }
        }

        Color textAreaBorderColor;

        /// <summary>
        /// Color of border of text area
        /// </summary>
        [DefaultValue(typeof(Color), "Black")]
        [Description("Color of border of text area")]
        public Color TextAreaBorderColor
        {
            get { return textAreaBorderColor; }
            set
            {
                textAreaBorderColor = value;
                Invalidate();
            }
        }

        TextAreaBorderType textAreaBorder;
        /// <summary>
        /// Type of border of text area
        /// </summary>
        [DefaultValue(typeof(TextAreaBorderType), "None")]
        [Description("Type of border of text area")]
        public TextAreaBorderType TextAreaBorder
        {
            get { return textAreaBorder; }
            set
            {
                textAreaBorder = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Background color for current line
        /// </summary>
        [DefaultValue(typeof (Color), "Transparent")]
        [Description("Background color for current line. Set to Color.Transparent to hide current line highlighting")]
        public Color CurrentLineColor
        {
            get { return currentLineColor; }
            set
            {
                currentLineColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Background color for highlighting of changed lines
        /// </summary>
        [DefaultValue(typeof (Color), "Transparent")]
        [Description("Background color for highlighting of changed lines. Set to Color.Transparent to hide changed line highlighting")]
        public Color ChangedLineColor
        {
            get { return changedLineColor; }
            set
            {
                changedLineColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Fore color (default style color)
        /// </summary>
        public override Color ForeColor
        {
            get { return base.ForeColor; }
            set
            {
                base.ForeColor = value;
                lines.InitDefaultStyle();
                Invalidate();
            }
        }

        /// <summary>
        /// Height of char in pixels (includes LineInterval)
        /// </summary>
        [Browsable(false)]
        public int CharHeight
        {
            get { return charHeight; }
            set
            {
                charHeight = value;
                NeedRecalc();
                OnCharSizeChanged();
            }
        }

        /// <summary>
        /// Interval between lines (in pixels)
        /// </summary>
        [Description("Interval between lines in pixels")]
        [DefaultValue(0)]
        public int LineInterval
        {
            get { return lineInterval; }
            set
            {
                lineInterval = value;
                SetFont(Font);
                Invalidate();
            }
        }

        /// <summary>
        /// Width of char in pixels
        /// </summary>
        [Browsable(false)]
        public int CharWidth { get; set; }

        /// <summary>
        /// Spaces count for tab
        /// </summary>
        [DefaultValue(4)]
        [Description("Spaces count for tab")]
        public int TabLength { get; set; }

        /// <summary>
        /// Read only
        /// </summary>
        [DefaultValue(false)]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Shows line numbers.
        /// </summary>
        [DefaultValue(true)]
        [Description("Shows line numbers.")]
        public bool ShowLineNumbers
        {
            get { return showLineNumbers; }
            set
            {
                showLineNumbers = value;
                NeedRecalc();
                Invalidate();
            }
        }

        /// <summary>
        /// Shows vertical lines between folding start line and folding end line.
        /// </summary>
        [DefaultValue(false)]
        [Description("Shows vertical lines between folding start line and folding end line.")]
        public bool ShowFoldingLines
        {
            get { return showFoldingLines; }
            set
            {
                showFoldingLines = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Color of line numbers.
        /// </summary>
        [DefaultValue(typeof (Color), "Teal")]
        [Description("Color of line numbers.")]
        public Color LineNumberColor
        {
            get { return lineNumberColor; }
            set
            {
                lineNumberColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Start value of first line number.
        /// </summary>
        [DefaultValue(typeof (uint), "1")]
        [Description("Start value of first line number.")]
        public uint LineNumberStartValue
        {
            get { return lineNumberStartValue; }
            set
            {
                lineNumberStartValue = value;
                needRecalc = true;
                Invalidate();
            }
        }

        /// <summary>
        /// Background color of indent area
        /// </summary>
        [DefaultValue(typeof(Color), "WhiteSmoke")]
        [Description("Background color of indent area")]
        public Color IndentBackColor
        {
            get { return indentBackColor; }
            set
            {
                indentBackColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Background color of padding area
        /// </summary>
        [DefaultValue(typeof (Color), "Transparent")]
        [Description("Background color of padding area")]
        public Color PaddingBackColor
        {
            get { return paddingBackColor; }
            set
            {
                paddingBackColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Color of disabled component
        /// </summary>
        [DefaultValue(typeof (Color), "100;180;180;180")]
        [Description("Color of disabled component")]
        public Color DisabledColor { get; set; }

        /// <summary>
        /// Color of caret
        /// </summary>
        [DefaultValue(typeof (Color), "Black")]
        [Description("Color of caret.")]
        public Color CaretColor { get; set; }

        /// <summary>
        /// Wide caret
        /// </summary>
        [DefaultValue(false)]
        [Description("Wide caret.")]
        public bool WideCaret { get; set; }

        /// <summary>
        /// Color of service lines (folding lines, borders of blocks etc.)
        /// </summary>
        [DefaultValue(typeof (Color), "Silver")]
        [Description("Color of service lines (folding lines, borders of blocks etc.)")]
        public Color ServiceLinesColor
        {
            get { return serviceLinesColor; }
            set
            {
                serviceLinesColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Padings of text area
        /// </summary>
        [Browsable(true)]
        [Description("Paddings of text area.")]
        public Padding Paddings { get; set; }

        //hide parent padding
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
         EditorBrowsable(EditorBrowsableState.Never)]
        public new Padding Padding
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        //hide RTL
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
         EditorBrowsable(EditorBrowsableState.Never)]
        public new bool RightToLeft
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Color of folding area indicator
        /// </summary>
        [DefaultValue(typeof (Color), "Green")]
        [Description("Color of folding area indicator.")]
        public Color FoldingIndicatorColor
        {
            get { return foldingIndicatorColor; }
            set
            {
                foldingIndicatorColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Enables folding indicator (left vertical line between folding bounds)
        /// </summary>
        [DefaultValue(true)]
        [Description("Enables folding indicator (left vertical line between folding bounds)")]
        public bool HighlightFoldingIndicator
        {
            get { return highlightFoldingIndicator; }
            set
            {
                highlightFoldingIndicator = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Left distance to text beginning
        /// </summary>
        [Browsable(false)]
        [Description("Left distance to text beginning.")]
        public int LeftIndent { get; private set; }

        /// <summary>
        /// Left padding in pixels
        /// </summary>
        [DefaultValue(0)]
        [Description("Width of left service area (in pixels)")]
        public int LeftPadding
        {
            get { return leftPadding; }
            set
            {
                leftPadding = value;
                Invalidate();
            }
        }

        /// <summary>
        /// This property draws vertical line after defined char position.
        /// Set to 0 for disable drawing of vertical line.
        /// </summary>
        [DefaultValue(0)]
        [Description("This property draws vertical line after defined char position. Set to 0 for disable drawing of vertical line.")]
        public int PreferredLineWidth
        {
            get { return preferredLineWidth; }
            set
            {
                preferredLineWidth = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Hotkeys. Do not use this property in your code, use HotkeysMapping property.
        /// </summary>
        [Description("Here you can change hotkeys for FastColoredTextBox.")]
        [Editor(typeof(HotkeysEditor), typeof(UITypeEditor))]
        [DefaultValue("Tab=IndentIncrease, Escape=ClearHints, PgUp=GoPageUp, PgDn=GoPageDown, End=GoEnd, Home=GoHome, Left=GoLeft, Up=GoUp, Right=GoRight, Down=GoDown, Ins=ReplaceMode, Del=DeleteCharRight, F3=FindNext, Shift+Tab=IndentDecrease, Shift+PgUp=GoPageUpWithSelection, Shift+PgDn=GoPageDownWithSelection, Shift+End=GoEndWithSelection, Shift+Home=GoHomeWithSelection, Shift+Left=GoLeftWithSelection, Shift+Up=GoUpWithSelection, Shift+Right=GoRightWithSelection, Shift+Down=GoDownWithSelection, Shift+Ins=Paste, Shift+Del=Cut, Ctrl+Back=ClearWordLeft, Ctrl+Space=AutocompleteMenu, Ctrl+End=GoLastLine, Ctrl+Home=GoFirstLine, Ctrl+Left=GoWordLeft, Ctrl+Up=ScrollUp, Ctrl+Right=GoWordRight, Ctrl+Down=ScrollDown, Ctrl+Ins=Copy, Ctrl+Del=ClearWordRight, Ctrl+0=ZoomNormal, Ctrl+A=SelectAll, Ctrl+B=BookmarkLine, Ctrl+C=Copy, Ctrl+E=MacroExecute, Ctrl+F=FindDialog, Ctrl+G=GoToDialog, Ctrl+H=ReplaceDialog, Ctrl+M=MacroRecord, Ctrl+N=GoNextBookmark, Ctrl+R=Redo, Ctrl+U=UpperCase, Ctrl+V=Paste, Ctrl+X=Cut, Ctrl+Z=Undo, Ctrl+Add=ZoomIn, Ctrl+Subtract=ZoomOut, Ctrl+OemMinus=NavigateBackward, Ctrl+Shift+End=GoLastLineWithSelection, Ctrl+Shift+Home=GoFirstLineWithSelection, Ctrl+Shift+Left=GoWordLeftWithSelection, Ctrl+Shift+Right=GoWordRightWithSelection, Ctrl+Shift+B=UnbookmarkLine, Ctrl+Shift+C=CommentSelected, Ctrl+Shift+N=GoPrevBookmark, Ctrl+Shift+U=LowerCase, Ctrl+Shift+OemMinus=NavigateForward, Alt+Back=Undo, Alt+Up=MoveSelectedLinesUp, Alt+Down=MoveSelectedLinesDown, Alt+F=FindChar, Alt+Shift+Left=GoLeft_ColumnSelectionMode, Alt+Shift+Up=GoUp_ColumnSelectionMode, Alt+Shift+Right=GoRight_ColumnSelectionMode, Alt+Shift+Down=GoDown_ColumnSelectionMode")]
        public string Hotkeys { 
            get { return HotkeysMapping.ToString(); }
            set { HotkeysMapping = HotkeysMapping.Parse(value); }
        }

        /// <summary>
        /// Hotkeys mapping
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public HotkeysMapping HotkeysMapping{ get; set;}

        /// <summary>
        /// Default text style
        /// This style is using when no one other TextStyle is not defined in Char.style
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TextStyle DefaultStyle
        {
            get { return lines.DefaultStyle; }
            set { lines.DefaultStyle = value; }
        }

        /// <summary>
        /// Style for rendering Selection area
        /// </summary>
        [Browsable(false)]
        public SelectionStyle SelectionStyle { get; set; }

        /// <summary>
        /// Style for rendering EOL characters.
        /// The EOL character is defined per Line.
        /// </summary>
        [Browsable(false)]
        public Style EndOfLineStyle { get; set; }

        /// <summary>
        /// Style for folded block rendering
        /// </summary>
        [Browsable(false)]
        public TextStyle FoldedBlockStyle { get; set; }

        /// <summary>
        /// Style for brackets highlighting
        /// </summary>
        [Browsable(false)]
        public MarkerStyle BracketsStyle { get; set; }

        /// <summary>
        /// Style for alternative brackets highlighting
        /// </summary>
        [Browsable(false)]
        public MarkerStyle BracketsStyle2 { get; set; }

        /// <summary>
        /// Opening bracket for brackets highlighting.
        /// Set to '\x0' for disable brackets highlighting.
        /// </summary>
        [DefaultValue('\x0')]
        [Description("Opening bracket for brackets highlighting. Set to '\\x0' for disable brackets highlighting.")]
        public char LeftBracket { get; set; }

        /// <summary>
        /// Closing bracket for brackets highlighting.
        /// Set to '\x0' for disable brackets highlighting.
        /// </summary>
        [DefaultValue('\x0')]
        [Description("Closing bracket for brackets highlighting. Set to '\\x0' for disable brackets highlighting.")]
        public char RightBracket { get; set; }

        /// <summary>
        /// Alternative opening bracket for brackets highlighting.
        /// Set to '\x0' for disable brackets highlighting.
        /// </summary>
        [DefaultValue('\x0')]
        [Description("Alternative opening bracket for brackets highlighting. Set to '\\x0' for disable brackets highlighting.")]
        public char LeftBracket2 { get; set; }

        /// <summary>
        /// Alternative closing bracket for brackets highlighting.
        /// Set to '\x0' for disable brackets highlighting.
        /// </summary>
        [DefaultValue('\x0')]
        [Description("Alternative closing bracket for brackets highlighting. Set to '\\x0' for disable brackets highlighting.")]
        public char RightBracket2 { get; set; }

        /// <summary>
        /// Comment line prefix.
        /// </summary>
        [DefaultValue("//")]
        [Description("Comment line prefix.")]
        public string CommentPrefix { get; set; }

        /// <summary>
        /// This property specifies which part of the text will be highlighted as you type (by built-in highlighter).
        /// </summary>
        /// <remarks>When a user enters text, a component refreshes highlighting (because the text was changed).
        /// This property specifies exactly which section of the text will be re-highlighted.
        /// This can be useful to highlight multi-line comments, for example.</remarks>
        [DefaultValue(typeof (HighlightingRangeType), "ChangedRange")]
        [Description("This property specifies which part of the text will be highlighted as you type.")]
        public HighlightingRangeType HighlightingRangeType { get; set; }

        /// <summary>
        /// Is keyboard in replace mode (wide caret) ?
        /// </summary>
        [Browsable(false)]
        public bool IsReplaceMode
        {
            get
            {
                return isReplaceMode && 
                       Selection.IsEmpty &&
                       (!Selection.ColumnSelectionMode) &&
                       Selection.Start.iChar < lines[Selection.Start.iLine].Count;
            }
            set { isReplaceMode = value; }
        }

        /// <summary>
        /// Allows text rendering several styles same time.
        /// </summary>
        [Browsable(true)]
        [DefaultValue(false)]
        [Description("Allows text rendering several styles same time.")]
        public bool AllowSeveralTextStyleDrawing { get; set; }

        /// <summary>
        /// Allows to record macros.
        /// </summary>
        [Browsable(true)]
        [DefaultValue(true)]
        [Description("Allows to record macros.")]
        public bool AllowMacroRecording 
        { 
            get { return macrosManager.AllowMacroRecordingByUser; }
            set { macrosManager.AllowMacroRecordingByUser = value; }
        }

        /// <summary>
        /// Allows AutoIndent. Inserts spaces before new line.
        /// </summary>
        [DefaultValue(true)]
        [Description("Allows auto indent. Inserts spaces before line chars.")]
        public bool AutoIndent { get; set; }

        /// <summary>
        /// Does autoindenting in existing lines. It works only if AutoIndent is True.
        /// </summary>
        [DefaultValue(true)]
        [Description("Does autoindenting in existing lines. It works only if AutoIndent is True.")]
        public bool AutoIndentExistingLines { get; set; }

        /// <summary>
        /// Minimal delay(ms) for delayed events (except TextChangedDelayed).
        /// </summary>
        [Browsable(true)]
        [DefaultValue(100)]
        [Description("Minimal delay(ms) for delayed events (except TextChangedDelayed).")]
        public int DelayedEventsInterval
        {
            get { return delayedEventsTimer.Interval; }
            set { delayedEventsTimer.Interval = value; }
        }

        /// <summary>
        /// Minimal delay(ms) for TextChangedDelayed event.
        /// </summary>
        [Browsable(true)]
        [DefaultValue(100)]
        [Description("Minimal delay(ms) for TextChangedDelayed event.")]
        public int DelayedTextChangedInterval
        {
            get { return delayedTextChangedTimer.Interval; }
            set { delayedTextChangedTimer.Interval = value; }
        }

        /// <summary>
        /// Language for highlighting by built-in highlighter.
        /// </summary>
        [Browsable(true)]
        [DefaultValue(typeof (Language), "Custom")]
        [Description("Language for highlighting by built-in highlighter.")]
        public Language Language
        {
            get { return language; }
            set
            {
                language = value;
                if (SyntaxHighlighter != null)
                    SyntaxHighlighter.InitStyleSchema(language);
                Invalidate();
            }
        }

        /// <summary>
        /// Syntax Highlighter
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SyntaxHighlighter SyntaxHighlighter { get; set; }

        /// <summary>
        /// XML file with description of syntax highlighting.
        /// This property works only with Language == Language.Custom.
        /// </summary>
        [Browsable(true)]
        [DefaultValue(null)]
        [Editor(typeof (FileNameEditor), typeof (UITypeEditor))]
        [Description(
            "XML file with description of syntax highlighting. This property works only with Language == Language.Custom."
            )]
        public string DescriptionFile
        {
            get { return descriptionFile; }
            set
            {
                descriptionFile = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Background color.
        /// It is used if BackBrush is null.
        /// </summary>
        [DefaultValue(typeof (Color), "White")]
        [Description("Background color.")]
        public override Color BackColor
        {
            get { return base.BackColor; }
            set { base.BackColor = value; }
        }

        /// <summary>
        /// Background brush.
        /// If Null then BackColor is used.
        /// </summary>
        [Browsable(false)]
        public Brush BackBrush
        {
            get { return backBrush; }
            set
            {
                backBrush = value;
                Invalidate();
            }
        }

        [Browsable(true)]
        [DefaultValue(true)]
        [Description("Scollbars visibility.")]
        public bool ShowScrollBars
        {
            get { return scrollBars; }
            set
            {
                if (value == scrollBars) return;
                scrollBars = value;
                needRecalc = true;
                Invalidate();
            }
        }

        /// <summary>
        /// Multiline.
        /// Setting to true enables scrollbars
        /// </summary>
        [Browsable(true)]
        [DefaultValue(true)]
        [Description("Multiline mode.")]
        public bool Multiline
        {
            get { return multiline; }
            set
            {
                if (multiline == value) return;
                multiline = value;
                needRecalc = true;
                if (multiline)
                {
                    base.AutoScroll = true;
                    ShowScrollBars = true;
                }
                else
                {
                    base.AutoScroll = false;
                    ShowScrollBars = false;
                    if (lines.Count > 1)
                        lines.RemoveLine(1, lines.Count - 1);
                    lines.Manager.ClearHistory();
                }
                Invalidate();
            }
        }

        /// <summary>
        /// WordWrap.
        /// When changing to true it recalculates the line positions
        /// </summary>
        [Browsable(true)]
        [DefaultValue(false)]
        [Description("WordWrap.")]
        public bool WordWrap
        {
            get { return wordWrap; }
            set
            {
                if (wordWrap == value) return;
                wordWrap = value;
                if (wordWrap)
                    Selection.ColumnSelectionMode = false;
                NeedRecalc(false, true);
                //RecalcWordWrap(0, LinesCount - 1);
                Invalidate();
            }
        }

        /// <summary>
        /// WordWrap mode.
        /// </summary>
        [Browsable(true)]
        [DefaultValue(typeof (WordWrapMode), "WordWrapControlWidth")]
        [Description("WordWrap mode.")]
        public WordWrapMode WordWrapMode
        {
            get { return wordWrapMode; }
            set
            {
                if (wordWrapMode == value) return;
                wordWrapMode = value;
                NeedRecalc(false, true);
                //RecalcWordWrap(0, LinesCount - 1);
                Invalidate();
            }
        }

        private bool selectionHighlightingForLineBreaksEnabled;
        /// <summary>
        /// If <c>true</c> then line breaks included into the selection will be selected too.
        /// Then line breaks will be shown as selected blank character.
        /// </summary>
        [DefaultValue(true)]
        [Description("If enabled then line ends included into the selection will be selected too. " +
            "Then line ends will be shown as selected blank character.")]
        public bool SelectionHighlightingForLineBreaksEnabled
        {
            get { return selectionHighlightingForLineBreaksEnabled; }
            set
            {
                selectionHighlightingForLineBreaksEnabled = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Do not change this property
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override bool AutoScroll
        {
            get { return base.AutoScroll; }
            set { ; }
        }

        /// <summary>
        /// Font
        /// </summary>
        /// <remarks>Use only monospaced font</remarks>
        [DefaultValue(typeof (Font), "Courier New, 9.75")]
        public override Font Font
        {
            get { return BaseFont; }
            set {
                originalFont = (Font)value.Clone();
                SetFont(value);
            }
        }


        Font baseFont;
        /// <summary>
        /// Font
        /// </summary>
        /// <remarks>Use only monospaced font</remarks>
        [DefaultValue(typeof(Font), "Courier New, 9.75")]
        private Font BaseFont
        {
            get { return baseFont; }
            set
            {
                baseFont = value;
            }
        }

        private void SetFont(Font newFont)
        {
            BaseFont = newFont;
            //check monospace font
            SizeF sizeM = CharHelper.GetCharSize(BaseFont, 'M');
            SizeF sizeDot = CharHelper.GetCharSize(BaseFont, '.');
            if (sizeM != sizeDot)
                BaseFont = new Font("Courier New", BaseFont.SizeInPoints, FontStyle.Regular, GraphicsUnit.Point);
            //calc size of character width and character height
            SizeF size = CharHelper.GetCharSize(BaseFont, 'M');
            CharWidth = (int) Math.Round(size.Width*1f /*0.85*/) - 1 /*0*/;
            CharHeight = lineInterval + (int) Math.Round(size.Height*1f /*0.9*/) - 1 /*0*/;
            //
            //if (wordWrap)
            //    RecalcWordWrap(0, Lines.Count - 1);
            NeedRecalc(false, wordWrap);
            //
            Invalidate();
        }

        public new Size AutoScrollMinSize
        {
            set
            {
                if (scrollBars)
                {
                    if (!base.AutoScroll)
                        base.AutoScroll = true;
                    Size newSize = value;
                    if (WordWrap && WordWrapMode != FastColoredTextBoxNS.WordWrapMode.Custom)
                    {
                        int maxWidth = GetMaxLineWordWrapedWidth();
                        newSize = new Size(Math.Min(newSize.Width, maxWidth), newSize.Height);
                    }
                    base.AutoScrollMinSize = newSize;
                }
                else
                {
                    if (base.AutoScroll)
                        base.AutoScroll = false;
                    base.AutoScrollMinSize = new Size(0, 0);
                    VerticalScroll.Visible = false;
                    HorizontalScroll.Visible = false;
                    VerticalScroll.Maximum = Math.Max(0, value.Height - ClientSize.Height);
                    HorizontalScroll.Maximum = Math.Max(0, value.Width - ClientSize.Width);
                    localAutoScrollMinSize = value;
                }
            }

            get
            {
                if (scrollBars)
                    return base.AutoScrollMinSize;
                else
                    //return new Size(HorizontalScroll.Maximum, VerticalScroll.Maximum);
                    return localAutoScrollMinSize;
            }
        }

        /// <summary>
        /// Indicates that IME is allowed (for CJK language entering)
        /// </summary>
        [Browsable(false)]
        public bool ImeAllowed
        {
            get
            {
                return ImeMode != ImeMode.Disable &&
                       ImeMode != ImeMode.Off &&
                       ImeMode != ImeMode.NoControl;
            }
        }

        /// <summary>
        /// Color of selected area
        /// </summary>
        [DefaultValue(typeof (Color), "Blue")]
        [Description("Color of selected area.")]
        public virtual Color SelectionColor
        {
            get { return selectionColor; }
            set
            {
                selectionColor = value;
                if (selectionColor.A == 255)
                    selectionColor = Color.FromArgb(60, selectionColor);
                SelectionStyle = new SelectionStyle(new SolidBrush(selectionColor));
                Invalidate();
            }
        }

        public override Cursor Cursor
        {
            get { return base.Cursor; }
            set
            {
                defaultCursor = value;
                base.Cursor = value;
            }
        }

        /// <summary>
        /// Reserved space for line number characters.
        /// If smaller than needed (e. g. line count >= 10 and this value set to 1) this value will have no impact.
        /// If you want to reserve space, e. g. for line numbers >= 10 or >= 100 than you can set this value to 2 or 3 or higher.
        /// </summary>
        [DefaultValue(1)]
        [Description(
            "Reserved space for line number characters. If smaller than needed (e. g. line count >= 10 and " +
            "this value set to 1) this value will have no impact. If you want to reserve space, e. g. for line " +
            "numbers >= 10 or >= 100, than you can set this value to 2 or 3 or higher.")]
        public int ReservedCountOfLineNumberChars
        {
            get { return reservedCountOfLineNumberChars; }
            set
            {
                reservedCountOfLineNumberChars = value;
                NeedRecalc();
                Invalidate();
            }
        }

        #endregion

        // ========================================
        // ========================================
        // ========================================

        #region events

        /// <summary>
        /// Occurs when mouse is moving over text and tooltip is needed.
        /// The event handler should set the properties in ToolTipNeededEventArgs.
        /// </summary>
        [Browsable(true)]
        [Description("Occurs when mouse is moving over text and tooltip is needed.")]
        public event EventHandler<ToolTipNeededEventArgs> ToolTipNeeded;

        private void TooltipDelayTimerTick(object sender, EventArgs e)
        {
            tooltipDelayTimer.Stop();
            OnToolTip();
        }

        protected virtual void OnToolTip()
        {
            if (ToolTip == null)
                return;
            if (ToolTipNeeded == null)
                return;

            //get place under mouse
            Place place = PointToPlace(lastMouseCoord);

            //check distance
            Point p = PlaceToPoint(place);
            if (Math.Abs(p.X - lastMouseCoord.X) > CharWidth*2 ||
                Math.Abs(p.Y - lastMouseCoord.Y) > CharHeight*2)
                return;
            //get word under mouse
            var r = new Range(this, place, place);
            string hoveredWord = r.GetFragment("[a-zA-Z]").Text;
            //event handler
            var ea = new ToolTipNeededEventArgs(place, hoveredWord);
            ToolTipNeeded(this, ea);

            if (ea.ToolTipText != null)
            {
                //show tooltip
                ToolTip.ToolTipTitle = ea.ToolTipTitle;
                ToolTip.ToolTipIcon = ea.ToolTipIcon;
                //ToolTip.SetToolTip(this, ea.ToolTipText);
                ToolTip.Show(ea.ToolTipText, this, new Point(lastMouseCoord.X, lastMouseCoord.Y + CharHeight));
            }
        }

        /// <summary>
        /// Occurs when VisibleRange is changed
        /// </summary>
        public virtual void OnVisibleRangeChanged()
        {
            needRecalcFoldingLines = true;

            needRiseVisibleRangeChangedDelayed = true;
            ResetTimer(delayedEventsTimer);
            if (VisibleRangeChanged != null)
                VisibleRangeChanged(this, new EventArgs());
        }

        /// <summary>
        /// Invalidates the entire surface of the control and causes the control to be redrawn.
        /// This method is thread safe and does not require Invoke.
        /// </summary>
        public new void Invalidate()
        {
            if (InvokeRequired)
                BeginInvoke(new MethodInvoker(Invalidate));
            else
                base.Invalidate();
        }

        protected virtual void OnCharSizeChanged()
        {
            // update scroll distance
            this.VerticalScroll.SmallChange = this.CharHeight;
            this.VerticalScroll.LargeChange = 10*this.CharHeight;
            this.HorizontalScroll.SmallChange = CharWidth;
        }

        /// <summary>
        /// HintClick event.
        /// It occurs if user click on the hint.
        /// </summary>
        [Browsable(true)]
        [Description("It occurs if user click on the hint.")]
        public event EventHandler<HintClickEventArgs> HintClick;

        /// <summary>
        /// Occurs when user click on the hint
        /// </summary>
        /// <param name="hint"></param>
        internal virtual void OnHintClick(Hint hint)
        {
            if (HintClick != null)
                HintClick(this, new HintClickEventArgs(hint));
        }

        /// <summary>
        /// TextChanged event.
        /// It occurs after insert, delete, clear, undo and redo operations.
        /// </summary>
        [Browsable(true)]
        [Description("It occurs after insert, delete, clear, undo and redo operations.")]
        public new event EventHandler<TextChangedEventArgs> TextChanged;

        /// <summary>
        /// Fake event for correct data binding
        /// </summary>
        [Browsable(false)]
        internal event EventHandler BindingTextChanged;

        /// <summary>
        /// Occurs when user paste text from clipboard.
        /// Pasting can be cancelled by setting TextChangingEventArgs.Cancel.
        /// Or the inserted text can be changed via TextChangingEventArgs.InsertingText.
        /// </summary>
        [Description("Occurs when user paste text from clipboard")]
        public event EventHandler<TextChangingEventArgs> Pasting;

        /// <summary>
        /// Returns the result of the Pasting EventHandler because pasting could be cancelled.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal TextChangingEventArgs OnPasting(string text)
        {
            var args = new TextChangingEventArgs()
                {
                    Cancel = false,
                    InsertingText = text,
                };

            if (Pasting != null)
            {
                Pasting(this, args);
            }
            return args;
        }

        /// <summary>
        /// TextChanging event.
        /// It occurs before insert, delete, clear, undo and redo operations.
        /// </summary>
        [Browsable(true)]
        [Description("It occurs before insert, delete, clear, undo and redo operations.")]
        public event EventHandler<TextChangingEventArgs> TextChanging;

        /// <summary>
        /// SelectionChanged event.
        /// It occurs after changing of selection.
        /// </summary>
        [Browsable(true)]
        [Description("It occurs after changing of selection.")]
        public event EventHandler SelectionChanged;

        /// <summary>
        /// VisibleRangeChanged event.
        /// It occurs after changing of visible range.
        /// </summary>
        [Browsable(true)]
        [Description("It occurs after changing of visible range.")]
        public event EventHandler VisibleRangeChanged;

        /// <summary>
        /// TextChangedDelayed event. 
        /// It occurs after insert, delete, clear, undo and redo operations. 
        /// This event occurs with a delay relative to TextChanged, and fires only once.
        /// </summary>
        [Browsable(true)]
        [Description(
            "It occurs after insert, delete, clear, undo and redo operations. This event occurs with a delay relative to TextChanged, and fires only once."
            )]
        public event EventHandler<TextChangedEventArgs> TextChangedDelayed;

        /// <summary>
        /// SelectionChangedDelayed event.
        /// It occurs after changing of selection.
        /// This event occurs with a delay relative to SelectionChanged, and fires only once.
        /// </summary>
        [Browsable(true)]
        [Description(
            "It occurs after changing of selection. This event occurs with a delay relative to SelectionChanged, and fires only once."
            )]
        public event EventHandler SelectionChangedDelayed;

        /// <summary>
        /// VisibleRangeChangedDelayed event.
        /// It occurs after changing of visible range.
        /// This event occurs with a delay relative to VisibleRangeChanged, and fires only once.
        /// </summary>
        [Browsable(true)]
        [Description(
            "It occurs after changing of visible range. This event occurs with a delay relative to VisibleRangeChanged, and fires only once."
            )]
        public event EventHandler VisibleRangeChangedDelayed;

        /// <summary>
        /// It occurs when user click on VisualMarker.
        /// </summary>
        [Browsable(true)]
        [Description("It occurs when user click on VisualMarker.")]
        public event EventHandler<VisualMarkerEventArgs> VisualMarkerClick;

        /// <summary>
        /// It occurs when visible char is entering (alphabetic, digit, punctuation, DEL, BACKSPACE)
        /// </summary>
        /// <remarks>Set Handle to True for cancel key</remarks>
        [Browsable(true)]
        [Description("It occurs when visible char is entering (alphabetic, digit, punctuation, DEL, BACKSPACE).")]
        public event KeyPressEventHandler KeyPressing;

        /// <summary>
        /// It occurs when visible char is entered (alphabetic, digit, punctuation, DEL, BACKSPACE)
        /// </summary>
        [Browsable(true)]
        [Description("It occurs when visible char is entered (alphabetic, digit, punctuation, DEL, BACKSPACE).")]
        public event KeyPressEventHandler KeyPressed;

        /// <summary>
        /// It occurs when calculates AutoIndent for new line
        /// </summary>
        [Browsable(true)]
        [Description("It occurs when calculates AutoIndent for new line.")]
        public event EventHandler<AutoIndentEventArgs> AutoIndentNeeded;

        /// <summary>
        /// It occurs when line background is painting
        /// </summary>
        [Browsable(true)]
        [Description("It occurs when line background is painting.")]
        public event EventHandler<PaintLineEventArgs> PaintLine;

        /// <summary>
        /// Occurs when line was inserted/added
        /// </summary>
        [Browsable(true)]
        [Description("Occurs when line was inserted/added.")]
        public event EventHandler<LineInsertedEventArgs> LineInserted;

        /// <summary>
        /// Occurs when line was removed
        /// </summary>
        [Browsable(true)]
        [Description("Occurs when line was removed.")]
        public event EventHandler<LineRemovedEventArgs> LineRemoved;

        /// <summary>
        /// Occurs when current highlighted folding area is changed.
        /// Current folding area see in StartFoldingLine and EndFoldingLine.
        /// </summary>
        /// <remarks></remarks>
        [Browsable(true)]
        [Description("Occurs when current highlighted folding area is changed.")]
        public event EventHandler<EventArgs> FoldingHighlightChanged;

        /// <summary>
        /// Occurs when undo/redo stack is changed
        /// </summary>
        /// <remarks></remarks>
        [Browsable(true)]
        [Description("Occurs when undo/redo stack is changed.")]
        public event EventHandler<EventArgs> UndoRedoStateChanged;

        /// <summary>
        /// Occurs when component was zoomed
        /// </summary>
        [Browsable(true)]
        [Description("Occurs when component was zoomed.")]
        public event EventHandler ZoomChanged;


        /// <summary>
        /// Occurs when user pressed key, that specified as CustomAction
        /// </summary>
        [Browsable(true)]
        [Description("Occurs when user pressed key, that specified as CustomAction.")]
        public event EventHandler<CustomActionEventArgs> CustomAction;

        /// <summary>
        /// Occurs when scroolbars are updated
        /// </summary>
        [Browsable(true)]
        [Description("Occurs when scroolbars are updated.")]
        public event EventHandler ScrollbarsUpdated;

        /// <summary>
        /// Occurs when custom wordwrap is needed
        /// </summary>
        [Browsable(true)]
        [Description("Occurs when custom wordwrap is needed.")]
        public event EventHandler<WordWrapNeededEventArgs> WordWrapNeeded;

        #endregion


        internal TextSource CreateTextSource()
        {
            return new TextSource(this);
        }

        private void SetAsCurrentTB()
        {
            TextSource.CurrentTB = this;
        }

        /// <summary>
        /// Replaces the current TextSource with the given TextSource
        /// </summary>
        /// <param name="ts"></param>
        internal void InitTextSource(TextSource ts)
        {
            if (this.lines != null)
            {
                // FIXME: Shouldn't this be removing the handlers from this.lines?
                ts.LineInserted -= ts_LineInserted;
                ts.LineRemoved -= ts_LineRemoved;
                ts.TextChanged -= ts_TextChanged;
                ts.RecalcNeeded -= ts_RecalcNeeded;
                ts.RecalcWordWrap -= ts_RecalcWordWrap;
                ts.TextChanging -= ts_TextChanging;

                lines.Dispose();
            }

            LineInfos.Clear();
            this.Hints.Clear();
            if (this.Bookmarks != null)
                this.Bookmarks.Clear();

            lines = ts;

            if (ts != null)
            {
                ts.LineInserted += ts_LineInserted;
                ts.LineRemoved += ts_LineRemoved;
                ts.TextChanged += ts_TextChanged;
                ts.RecalcNeeded += ts_RecalcNeeded;
                ts.RecalcWordWrap += ts_RecalcWordWrap;
                ts.TextChanging += ts_TextChanging;

                while (LineInfos.Count < ts.Count)
                    LineInfos.Add(new LineInfo(-1));
            }

            isChanged = false;
            needRecalc = true;
        }

        #region TextSource event handlers

        private void ts_RecalcWordWrap(object sender, TextSource.TextSourceTextChangedEventArgs e)
        {
            RecalcWordWrap(e.iFromLine, e.iToLine);
        }

        private void ts_TextChanging(object sender, TextChangingEventArgs e)
        {
            if (TextSource.CurrentTB == this)
            {
                string text = e.InsertingText;
                OnTextChanging(ref text);
                e.InsertingText = text;
            }
        }

        private void ts_RecalcNeeded(object sender, TextSource.TextSourceTextChangedEventArgs e)
        {
            if (e.iFromLine == e.iToLine && !WordWrap && lines.Count > MIN_LINES_FOR_ACCURACY)
                RecalcScrollByOneLine(e.iFromLine);
            else
                needRecalc = true;
        }

        private void ts_TextChanged(object sender, TextSource.TextSourceTextChangedEventArgs e)
        {
            if (e.iFromLine == e.iToLine && !WordWrap)
                RecalcScrollByOneLine(e.iFromLine);
            else
                needRecalc = true;

            Invalidate();
            if (TextSource.CurrentTB == this)
                OnTextChanged(e.iFromLine, e.iToLine);
        }

        private void ts_LineRemoved(object sender, LineRemovedEventArgs e)
        {
            LineInfos.RemoveRange(e.Index, e.Count);
            OnLineRemoved(e.Index, e.Count, e.RemovedLineUniqueIds);
        }

        private void ts_LineInserted(object sender, LineInsertedEventArgs e)
        {
            VisibleState newState = VisibleState.Visible;
            if (e.Index >= 0 && e.Index < LineInfos.Count && LineInfos[e.Index].VisibleState == VisibleState.Hidden)
                newState = VisibleState.Hidden;

            var temp = new List<LineInfo>(e.Count);
            for (int i = 0; i < e.Count; i++)
                temp.Add(new LineInfo(-1) {VisibleState = newState});
            LineInfos.InsertRange(e.Index, temp);

            OnLineInserted(e.Index, e.Count);
        }

        #endregion

        /// <summary>
        /// Tries to derive the end-of-line format from the first 'maxlines' lines and sets the DefaultEolFormat
        /// </summary>
        private void TryDeriveEolFormat(int maxlines = 10)
        {
            int crCount = 0, lfCount = 0, crlfCount = 0;

            for (int i = 0; i < lines.Count && i < maxlines; i++)
            {
                switch (lines[i].EolFormat)
                {
                    case EolFormat.CR:
                        crCount++;
                        break;
                    case EolFormat.CRLF:
                        crlfCount++;
                        break;
                    case EolFormat.LF:
                        lfCount++;
                        break;
                }
            }
            if (lfCount > crlfCount && lfCount > crCount)
            {
                DefaultEolFormat = EolFormat.LF;
            }
            else if (crCount > crlfCount && crCount > lfCount)
            {
                DefaultEolFormat = EolFormat.CR;
            }
            else
            {
                DefaultEolFormat = EolFormat.CRLF;
            }
        }

        /// <summary>
        /// Converts all line endings to the new format and sets the DefaultEolFormat to the newFormat
        /// </summary>
        /// <param name="newFormat"></param>
        public void ConvertEolFormat(EolFormat newFormat)
        {
            foreach (var line in lines)
            {
                if (line.EolFormat != EolFormat.None)
                {
                    line.EolFormat = newFormat;
                }
            }
            DefaultEolFormat = newFormat;
            this.Invalidate();
        }

        /// <summary>
        /// Call this method if the recalc of the position of lines is needed.
        /// This will schedule a recalc
        /// </summary>
        public void NeedRecalc(bool forced = false, bool wordWrapRecalc = false)
        {
            needRecalc = true;

            if (wordWrapRecalc)
            {
                // Point struct is abused to indicate the interval
                needRecalcWordWrapInterval = new Point(0, this.LinesCount - 1);
                needRecalcWordWrap = true;
            }

            if (forced)
                Recalc();
        }

        /// <summary>
        /// Navigates forward (by Line.LastVisit property)
        /// </summary>
        public bool NavigateForward()
        {
            DateTime min = DateTime.Now;
            int iLine = -1;
            for (int i = 0; i < this.LinesCount; i++)
            {
                if (this.lines.IsLineLoaded(i))
                {
                    if (this.lines[i].LastVisit > this.lastNavigatedDateTime && this.lines[i].LastVisit < min)
                    {
                        min = this.lines[i].LastVisit;
                        iLine = i;
                    }
                }
            }

            if (iLine >= 0)
            {
                this.Navigate(iLine);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Navigates backward (by Line.LastVisit property)
        /// </summary>
        public bool NavigateBackward()
        {
            var max = new DateTime();
            int iLine = -1;
            for (int i = 0; i < LinesCount; i++)
            {
                if (lines.IsLineLoaded(i))
                {
                    if (lines[i].LastVisit < lastNavigatedDateTime && lines[i].LastVisit > max)
                    {
                        max = lines[i].LastVisit;
                        iLine = i;
                    }
                }
            }

            if (iLine >= 0)
            {
                this.Navigate(iLine);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Navigates to defined line, without Line.LastVisit reseting
        /// </summary>
        public void Navigate(int iLine)
        {
            if (iLine >= this.LinesCount) return;
            this.lastNavigatedDateTime = this.lines[iLine].LastVisit;
            this.Selection.Start = new Place(0, iLine);
            this.DoSelectionVisible();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            m_hImc = NativeMethods.ImmGetContext(Handle);
        }

        private void DelayedTextChangedTimerTick(object sender, EventArgs e)
        {
            delayedTextChangedTimer.Enabled = false;
            if (needRiseTextChangedDelayed)
            {
                needRiseTextChangedDelayed = false;
                if (delayedTextChangedRange == null)
                    return;
                delayedTextChangedRange = Range.GetIntersectionWith(delayedTextChangedRange);
                delayedTextChangedRange.Expand();
                OnTextChangedDelayed(delayedTextChangedRange);
                delayedTextChangedRange = null;
            }
        }

        private void DelayedEventsTimerTick(object sender, EventArgs e)
        {
            delayedEventsTimer.Enabled = false;
            if (needRiseSelectionChangedDelayed)
            {
                needRiseSelectionChangedDelayed = false;
                OnSelectionChangedDelayed();
            }
            if (needRiseVisibleRangeChangedDelayed)
            {
                needRiseVisibleRangeChangedDelayed = false;
                OnVisibleRangeChangedDelayed();
            }
        }

        public virtual void OnTextChangedDelayed(Range changedRange)
        {
            if (TextChangedDelayed != null)
                TextChangedDelayed(this, new TextChangedEventArgs(changedRange));
        }

        public virtual void OnSelectionChangedDelayed()
        {
            RecalcScrollByOneLine(Selection.Start.iLine);
            //highlight brackets
            ClearBracketsPositions();
            if (LeftBracket != '\x0' && RightBracket != '\x0')
                Highlighting.HighlightBrackets(this, this.BracketsHighlightStrategy, LeftBracket, RightBracket, ref leftBracketPosition, ref rightBracketPosition);
            if (LeftBracket2 != '\x0' && RightBracket2 != '\x0')
                Highlighting.HighlightBrackets(this, this.BracketsHighlightStrategy, LeftBracket2, RightBracket2, ref leftBracketPosition2, ref rightBracketPosition2);
            //remember last visit time
            if (Selection.IsEmpty && Selection.Start.iLine < LinesCount)
            {
                if (lastNavigatedDateTime != lines[Selection.Start.iLine].LastVisit)
                {
                    lines[Selection.Start.iLine].LastVisit = DateTime.Now;
                    lastNavigatedDateTime = lines[Selection.Start.iLine].LastVisit;
                }
            }

            if (SelectionChangedDelayed != null)
                SelectionChangedDelayed(this, new EventArgs());
        }

        public virtual void OnVisibleRangeChangedDelayed()
        {
            if (VisibleRangeChangedDelayed != null)
                VisibleRangeChangedDelayed(this, new EventArgs());
        }

        // RL: I don't know why, but appearently the timers have to be started after the handle is created.
        Dictionary<Timer, Timer> timersToReset = new Dictionary<Timer, Timer>();

        private void ResetTimer(Timer timer)
        {
            timer.Stop();
            if (IsHandleCreated)
                timer.Start();
            else
                timersToReset[timer] = timer;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            foreach (var timer in new List<Timer>(timersToReset.Keys))
                ResetTimer(timer);
            timersToReset.Clear();

            OnScrollbarsUpdated();
        }

        #region Style methods

        /// <summary>
        /// Add new style
        /// </summary>
        /// <returns>Layer index of this style</returns>
        public int AddStyle(Style style)
        {
            if (style == null) return -1;

            int i = GetStyleIndex(style);
            if (i >= 0)
                return i;

            for (i = Styles.Length - 1; i >= 0; i--)
                if (Styles[i] != null)
                    break;

            i++;
            if (i >= Styles.Length)
                throw new Exception("Maximum count of Styles is exceeded.");

            Styles[i] = style;
            return i;
        }

        /// <summary>
        /// Clear buffer of styles
        /// </summary>
        public void ClearStylesBuffer()
        {
            for (int i = 0; i < Styles.Length; i++)
                Styles[i] = null;
        }

        /// <summary>
        /// Clear style of all text
        /// </summary>
        public void ClearStyle(StyleIndex styleIndex)
        {
            foreach (Line line in lines)
                line.ClearStyle(styleIndex);

            for (int i = 0; i < LineInfos.Count; i++)
                SetVisibleState(i, VisibleState.Visible);

            Invalidate();
        }

        /// <summary>
        /// Returns index of the style in Styles
        /// -1 otherwise
        /// </summary>
        /// <param name="style"></param>
        /// <returns>Index of the style in Styles</returns>
        public int GetStyleIndex(Style style)
        {
            return Array.IndexOf(Styles, style);
        }

        /// <summary>
        /// Returns StyleIndex mask of given styles
        /// </summary>
        /// <param name="styles"></param>
        /// <returns>StyleIndex mask of given styles</returns>
        public StyleIndex GetStyleIndexMask(Style[] styles)
        {
            StyleIndex mask = StyleIndex.None;
            foreach (Style style in styles)
            {
                int i = GetStyleIndex(style);
                if (i >= 0)
                    mask |= Range.ToStyleIndex(i);
            }

            return mask;
        }

        internal int GetOrSetStyleLayerIndex(Style style)
        {
            int i = GetStyleIndex(style);
            if (i < 0)
                i = AddStyle(style);
            return i;
        }

        #endregion

        /// <summary>
        /// Gets length of given line
        /// </summary>
        /// <param name="iLine">Line index</param>
        /// <returns>Length of line</returns>
        public int GetLineLength(int iLine)
        {
            if (iLine < 0 || iLine >= lines.Count)
                throw new ArgumentOutOfRangeException("iLine", "Line index out of range");

            return lines[iLine].Count;
        }

        /// <summary>
        /// Get range of line
        /// </summary>
        /// <param name="iLine">Line index</param>
        public Range GetLine(int iLine)
        {
            if (iLine < 0 || iLine >= lines.Count)
                throw new ArgumentOutOfRangeException("iLine", "Line index out of range");

            var sel = new Range(this);
            sel.Start = new Place(0, iLine);
            sel.End = new Place(lines[iLine].Count, iLine);
            return sel;
        }

        /// <summary>
        /// Select all chars of text
        /// </summary>
        public void SelectAll()
        {
            Selection.SelectAll();
        }

        /// <summary>
        /// Move caret to end of text
        /// </summary>
        public void GoEnd()
        {
            if (lines.Count > 0)
                Selection.Start = new Place(lines[lines.Count - 1].Count, lines.Count - 1);
            else
                Selection.Start = new Place(0, 0);

            DoCaretVisible();
        }

        /// <summary>
        /// Move caret to first position
        /// </summary>
        public void GoHome()
        {
            Selection.Start = new Place(0, 0);

            DoCaretVisible();
            //VerticalScroll.Value = 0;
            //HorizontalScroll.Value = 0;
        }

        public void GoHome(bool shift)
        {
            Selection.BeginUpdate();
            try
            {
                int iLine = Selection.Start.iLine;
                int spaces = this[iLine].StartSpacesCount;
                if (Selection.Start.iChar <= spaces)
                    Selection.GoHome(shift);
                else
                {
                    Selection.GoHome(shift);
                    for (int i = 0; i < spaces; i++)
                        Selection.GoRight(shift);
                }
            }
            finally
            {
                Selection.EndUpdate();
            }
        }

        /// <summary>
        /// Clear text, styles, history, caches
        /// </summary>
        public void Clear()
        {
            Selection.BeginUpdate();
            try
            {
                Selection.SelectAll();
                ClearSelected();
                lines.Manager.ClearHistory();
                Invalidate();
            }
            finally
            {
                Selection.EndUpdate();
            }
        }

        /// <summary>
        /// Clears undo and redo stacks
        /// </summary>
        public void ClearUndo()
        {
            lines.Manager.ClearHistory();
        }

        #region Insert/Append Text

        /// <summary>
        /// Insert text into current selected position
        /// </summary>
        public virtual void InsertText(string text)
        {
            InsertText(text, true);
        }

        /// <summary>
        /// Insert text into current selected position
        /// </summary>
        /// <param name="text"></param>
        public virtual void InsertText(string text, bool jumpToCaret)
        {
            if (text == null)
                return;
            // Keep EOL format
            //if (text == "\r")
            //    text = "\n";

            lines.Manager.BeginAutoUndoCommands();
            try
            {
                if (!Selection.IsEmpty)
                    lines.Manager.ExecuteCommand(new ClearSelectedCommand(TextSource));

                //insert virtual spaces
                if(this.TextSource.Count > 0)
                if (Selection.IsEmpty && Selection.Start.iChar > GetLineLength(Selection.Start.iLine) && VirtualSpace)
                    InsertVirtualSpaces();

                lines.Manager.ExecuteCommand(new InsertTextCommand(TextSource, text));
                if (updating <= 0 && jumpToCaret)
                    DoCaretVisible();
            }
            finally
            {
                lines.Manager.EndAutoUndoCommands();
            }
            //
            Invalidate();
        }

        /// <summary>
        /// Insert text into current selection position (with predefined style)
        /// </summary>
        /// <param name="text"></param>
        public virtual void InsertText(string text, Style style)
        {
            InsertText(text, style, true);
        }

        /// <summary>
        /// Insert text into current selection position (with predefined style)
        /// </summary>
        public virtual void InsertText(string text, Style style, bool jumpToCaret)
        {
            if (text == null)
                return;

            //remember last caret position
            Place last = Selection.Start;
            //insert text
            InsertText(text, jumpToCaret);
            //get range
            var range = new Range(this, last, Selection.Start);
            //set style for range
            range.SetStyle(style);
        }

        /// <summary>
        /// Append string to end of the Text
        /// </summary>
        public virtual void AppendText(string text)
        {
            AppendText(text, null);
        }

        /// <summary>
        /// Append string to end of the Text
        /// </summary>
        public virtual void AppendText(string text, Style style)
        {
            if (text == null)
                return;

            Selection.ColumnSelectionMode = false;

            Place oldStart = Selection.Start;
            Place oldEnd = Selection.End;

            Selection.BeginUpdate();
            lines.Manager.BeginAutoUndoCommands();
            try
            {
                if (lines.Count > 0)
                    Selection.Start = new Place(lines[lines.Count - 1].Count, lines.Count - 1);
                else
                    Selection.Start = new Place(0, 0);

                //remember last caret position
                Place last = Selection.Start;

                lines.Manager.ExecuteCommand(new InsertTextCommand(TextSource, text));

                if (style != null)
                    new Range(this, last, Selection.Start).SetStyle(style);
            }
            finally
            {
                lines.Manager.EndAutoUndoCommands();
                Selection.Start = oldStart;
                Selection.End = oldEnd;
                Selection.EndUpdate();
            }
            //
            Invalidate();
        }

        public void InsertChar(char c)
        {
            lines.Manager.BeginAutoUndoCommands();
            try
            {
                if (!Selection.IsEmpty)
                    lines.Manager.ExecuteCommand(new ClearSelectedCommand(TextSource));

                //insert virtual spaces
                if (Selection.IsEmpty && Selection.Start.iChar > GetLineLength(Selection.Start.iLine) && VirtualSpace)
                    InsertVirtualSpaces();

                //insert char
                lines.Manager.ExecuteCommand(new InsertCharCommand(TextSource, c));
            }
            finally
            {
                lines.Manager.EndAutoUndoCommands();
            }

            Invalidate();
        }

        private void InsertVirtualSpaces()
        {
            int lineLength = GetLineLength(Selection.Start.iLine);
            int count = Selection.Start.iChar - lineLength;
            Selection.BeginUpdate();
            try
            {
                Selection.Start = new Place(lineLength, Selection.Start.iLine);
                lines.Manager.ExecuteCommand(new InsertTextCommand(TextSource, new string(' ', count)));
            }
            finally
            {
                Selection.EndUpdate();
            }
        }

        #endregion

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HSCROLL || m.Msg == WM_VSCROLL)
                if (m.WParam.ToInt32() != SB_ENDSCROLL)
                    Invalidate();
            
            base.WndProc(ref m);

            if (ImeAllowed)
                if (m.Msg == WM_IME_SETCONTEXT && m.WParam.ToInt32() == 1)
                {
                    NativeMethods.ImmAssociateContext(Handle, m_hImc);
                }
        }

        /// <summary>
        /// Deletes selected chars
        /// </summary>
        public void ClearSelected()
        {
            if (!Selection.IsEmpty)
            {
                lines.Manager.ExecuteCommand(new ClearSelectedCommand(TextSource));
                Invalidate();
            }
        }

        /// <summary>
        /// Deletes current line(s)
        /// </summary>
        public void ClearCurrentLine()
        {
            Selection.Expand();

            lines.Manager.ExecuteCommand(new ClearSelectedCommand(TextSource));
            if (Selection.Start.iLine == 0)
                if (!Selection.GoRightThroughFolded()) return;
            if (Selection.Start.iLine > 0)
                lines.Manager.ExecuteCommand(new InsertCharCommand(TextSource, '\b')); //backspace
            Invalidate();
        }

        internal void Recalc()
        {
            if (!needRecalc)
                return;

#if debug
            var sw = Stopwatch.StartNew();
#endif

            needRecalc = false;
            //calc min left indent
            LeftIndent = LeftPadding;
            long maxLineNumber = LinesCount + lineNumberStartValue - 1;
            int charsForLineNumber = 2 + (maxLineNumber > 0 ? (int) Math.Log10(maxLineNumber) : 0);

            // If there are reserved character for line numbers: correct this
            if (this.ReservedCountOfLineNumberChars + 1 > charsForLineNumber)
                charsForLineNumber = this.ReservedCountOfLineNumberChars + 1;

            if (Created)
            {
                if (ShowLineNumbers)
                    LeftIndent += charsForLineNumber*CharWidth + MIN_LEFT_INDENT + 1;

                //calc wordwrapping
                if (needRecalcWordWrap)
                {
                    RecalcWordWrap(needRecalcWordWrapInterval.X, needRecalcWordWrapInterval.Y);
                    needRecalcWordWrap = false;
                }
            }
            else
                needRecalc = true;

            //calc max line length and count of wordWrapLines
            this.maxLineLength = RecalcMaxLineLength();

            //adjust AutoScrollMinSize
            int minWidth;
            CalcMinAutosizeWidth(out minWidth, ref this.maxLineLength);
            
            AutoScrollMinSize = new Size(minWidth, this.TextHeight + Paddings.Top + Paddings.Bottom);
            UpdateScrollbars();
#if debug
            sw.Stop();
            Console.WriteLine("Recalc: " + sw.ElapsedMilliseconds);
#endif
        }

        private void CalcMinAutosizeWidth(out int minWidth, ref int _maxLineLength)
        {
            //adjust AutoScrollMinSize
            minWidth = LeftIndent + _maxLineLength*CharWidth + 2 + Paddings.Left + Paddings.Right;
            if (wordWrap)
                switch (WordWrapMode)
                {
                    case WordWrapMode.WordWrapControlWidth:
                    case WordWrapMode.CharWrapControlWidth:
                        _maxLineLength = Math.Min(_maxLineLength,
                                                 (ClientSize.Width - LeftIndent - Paddings.Left - Paddings.Right)/
                                                 CharWidth);
                        minWidth = 0;
                        break;
                    case WordWrapMode.WordWrapPreferredWidth:
                    case WordWrapMode.CharWrapPreferredWidth:
                        _maxLineLength = Math.Min(_maxLineLength, PreferredLineWidth);
                        minWidth = LeftIndent + PreferredLineWidth*CharWidth + 2 + Paddings.Left + Paddings.Right;
                        break;
                }
        }

        private void RecalcScrollByOneLine(int iLine)
        {
            if (iLine >= lines.Count)
                return;

            int _maxLineLength = lines[iLine].Count;
            if (this.maxLineLength < _maxLineLength && !WordWrap)
                this.maxLineLength = _maxLineLength;

            int minWidth;
            CalcMinAutosizeWidth(out minWidth, ref _maxLineLength);

            if (AutoScrollMinSize.Width < minWidth)
                AutoScrollMinSize = new Size(minWidth, AutoScrollMinSize.Height);
        }

        private int RecalcMaxLineLength()
        {
            int currentMaxLineLength = 0;

            // first line start after the top padding
            int currentHeight = this.Paddings.Top;

            for (int i = 0; i < lines.Count; i++)
            {
                int lineLength = this.lines.GetLineLength(i);
                LineInfo lineInfo = this.LineInfos[i];
                if (lineLength > currentMaxLineLength && lineInfo.VisibleState == VisibleState.Visible)
                {
                    // found a line that is longer
                    currentMaxLineLength = lineLength;
                }
                lineInfo.startY = currentHeight;

                currentHeight += lineInfo.WordWrapStringsCount * this.CharHeight + lineInfo.bottomPadding;
                
                this.LineInfos[i] = lineInfo;
            }

            // substract the padding so we get the actual height of the lines
            this.TextHeight = currentHeight - this.Paddings.Top;

            return currentMaxLineLength;
        }

        private int GetMaxLineWordWrapedWidth()
        {
            if (wordWrap)
                switch (wordWrapMode)
                {
                    case WordWrapMode.WordWrapControlWidth:
                    case WordWrapMode.CharWrapControlWidth:
                        return ClientSize.Width;
                    case WordWrapMode.WordWrapPreferredWidth:
                    case WordWrapMode.CharWrapPreferredWidth:
                        return LeftIndent + PreferredLineWidth*CharWidth + 2 + Paddings.Left + Paddings.Right;
                }

            return int.MaxValue;
        }

        private void RecalcWordWrap(int fromLine, int toLine)
        {
            int maxCharsPerLine = 0;
            bool charWrap = false;

            toLine = Math.Min(this.LinesCount - 1, toLine);

            switch (this.WordWrapMode)
            {
                case WordWrapMode.WordWrapControlWidth:
                    maxCharsPerLine = (this.ClientSize.Width - this.LeftIndent - this.Paddings.Left - this.Paddings.Right) / this.CharWidth;
                    break;
                case WordWrapMode.CharWrapControlWidth:
                    maxCharsPerLine = (this.ClientSize.Width - this.LeftIndent - this.Paddings.Left - this.Paddings.Right) / this.CharWidth;
                    charWrap = true;
                    break;
                case WordWrapMode.WordWrapPreferredWidth:
                    maxCharsPerLine = this.PreferredLineWidth;
                    break;
                case WordWrapMode.CharWrapPreferredWidth:
                    maxCharsPerLine = this.PreferredLineWidth;
                    charWrap = true;
                    break;
            }

            for (int iLine = fromLine; iLine <= toLine; iLine++)
            {
                if (this.lines.IsLineLoaded(iLine))
                {
                    if (!this.WordWrap)
                    {
                        LineInfos[iLine].CutOffPositions.Clear();
                    }
                    else
                    {
                        LineInfo li = LineInfos[iLine];

                        li.wordWrapIndent = WordWrapAutoIndent
                                                ? lines[iLine].StartSpacesCount + WordWrapIndent
                                                : WordWrapIndent;

                        if (WordWrapMode == WordWrapMode.Custom)
                        {
                            if (WordWrapNeeded != null)
                            {
                                WordWrapNeeded(this,
                                               new WordWrapNeededEventArgs(li.CutOffPositions, ImeAllowed, lines[iLine]));
                            }
                        }
                        else
                        {
                            CalcCutOffs(li.CutOffPositions, maxCharsPerLine, maxCharsPerLine - li.wordWrapIndent,
                                        ImeAllowed, charWrap, lines[iLine]);
                        }

                        LineInfos[iLine] = li;
                    }
                }
            }
            needRecalc = true;
        }

        /// <summary>
        /// Calculates wordwrap cutoffs
        /// </summary>
        public static void CalcCutOffs(List<int> cutOffPositions, int maxCharsPerLine, int maxCharsPerSecondaryLine, bool allowIME, bool charWrap, Line line)
        {
            if (maxCharsPerSecondaryLine < 1) maxCharsPerSecondaryLine = 1;
            if (maxCharsPerLine < 1) maxCharsPerLine = 1;

            int segmentLength = 0;
            int cutOff = 0;
            cutOffPositions.Clear();

            for (int i = 0; i < line.Count - 1; i++)
            {
                char c = line[i].c;
                if (charWrap)
                {
                    //char wrapping
                    cutOff = i + 1;
                }
                else
                {
                    //word wrapping
                    if (allowIME && CharHelper.IsCJKLetter(c)) //in CJK languages cutoff can be in any letter
                    {
                        cutOff = i;
                    }
                    else
                    {
                        if (!char.IsLetterOrDigit(c) && c != '_' && c != '\'')
                        {
                            cutOff = Math.Min(i + 1, line.Count - 1);
                        }
                    }
                }

                segmentLength++;

                if (segmentLength == maxCharsPerLine)
                {
                    if (cutOff == 0 ||
                        (cutOffPositions.Count > 0 && cutOff == cutOffPositions[cutOffPositions.Count - 1]))
                    {
                        cutOff = i + 1;
                    }
                    cutOffPositions.Add(cutOff);
                    segmentLength = 1 + i - cutOff;
                    maxCharsPerLine = maxCharsPerSecondaryLine;
                }
            }
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            if (WordWrap)
            {
                //RecalcWordWrap(0, lines.Count - 1);
                NeedRecalc(false, true);
                Invalidate();
            }
            OnVisibleRangeChanged();
            UpdateScrollbars();
        }

        #region scrolling

        /// <summary>
        /// When alignByLines is true, align by line height scrolling vertically
        /// </summary>
        /// <param name="se"></param>
        /// <param name="alignByLines"></param>
        public void OnScroll(ScrollEventArgs se, bool alignByLines)
        {
            if (se.ScrollOrientation == ScrollOrientation.VerticalScroll)
            {
                //align by line height
                int newValue = se.NewValue;
                if (alignByLines)
                    newValue = (int)(Math.Ceiling(1d * newValue / CharHeight) * CharHeight);
                //
                VerticalScroll.Value = Math.Max(VerticalScroll.Minimum, Math.Min(VerticalScroll.Maximum, newValue));
            }
            if (se.ScrollOrientation == ScrollOrientation.HorizontalScroll)
                HorizontalScroll.Value = Math.Max(HorizontalScroll.Minimum, Math.Min(HorizontalScroll.Maximum, se.NewValue));

            UpdateScrollbars();

            Invalidate();
            //
            base.OnScroll(se);
            OnVisibleRangeChanged();
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            OnScroll(se, true);
        }

        /// <summary>
        /// Scroll control for display defined rectangle
        /// </summary>
        /// <param name="rect"></param>
        private void DoVisibleRectangle(Rectangle rect)
        {
            int oldV = VerticalScroll.Value;
            int v = VerticalScroll.Value;
            int h = HorizontalScroll.Value;

            if (rect.Bottom > ClientRectangle.Height)
                v += rect.Bottom - ClientRectangle.Height;
            else if (rect.Top < 0)
                v += rect.Top;

            if (rect.Right > ClientRectangle.Width)
                h += rect.Right - ClientRectangle.Width;
            else if (rect.Left < LeftIndent)
                h += rect.Left - LeftIndent;
            //
            if (!Multiline)
                v = 0;
            //
            v = Math.Max(VerticalScroll.Minimum, v); // was 0
            h = Math.Max(HorizontalScroll.Minimum, h); // was 0
            //
            try
            {
                if (VerticalScroll.Visible || !ShowScrollBars)
                    VerticalScroll.Value = Math.Min(v, VerticalScroll.Maximum);
                if (HorizontalScroll.Visible || !ShowScrollBars)
                    HorizontalScroll.Value = Math.Min(h, HorizontalScroll.Maximum);
            }
            catch (ArgumentOutOfRangeException)
            {
                ;
            }

            UpdateScrollbars();
            //
            if (oldV != VerticalScroll.Value)
                OnVisibleRangeChanged();
        }

        /// <summary>
        /// Updates scrollbar position after Value changed
        /// </summary>
        public void UpdateScrollbars()
        {
            if (ShowScrollBars)
            {
                //some magic for update scrolls
                base.AutoScrollMinSize -= new Size(1, 0);
                base.AutoScrollMinSize += new Size(1, 0);

            }
            else
                AutoScrollMinSize = AutoScrollMinSize;

            if(IsHandleCreated)
                BeginInvoke((MethodInvoker)OnScrollbarsUpdated);
        }

        protected virtual void OnScrollbarsUpdated()
        {           
            if (ScrollbarsUpdated != null)
                ScrollbarsUpdated(this, EventArgs.Empty);
        }

        /// <summary>
        /// Scroll control for display caret
        /// </summary>
        public void DoCaretVisible()
        {
            Invalidate();
            Recalc();
            Point car = PlaceToPoint(Selection.Start);
            car.Offset(-CharWidth, 0);
            DoVisibleRectangle(new Rectangle(car, new Size(2*CharWidth, 2*CharHeight)));
        }

        /// <summary>
        /// Scroll control left
        /// </summary>
        public void ScrollLeft()
        {
            Invalidate();
            HorizontalScroll.Value = 0;
            AutoScrollMinSize -= new Size(1, 0);
            AutoScrollMinSize += new Size(1, 0);
        }

        /// <summary>
        /// Scroll control for display selection area
        /// </summary>
        public void DoSelectionVisible()
        {
            if (LineInfos[Selection.End.iLine].VisibleState != VisibleState.Visible)
                ExpandBlock(Selection.End.iLine);

            if (LineInfos[Selection.Start.iLine].VisibleState != VisibleState.Visible)
                ExpandBlock(Selection.Start.iLine);

            Recalc();
            DoVisibleRectangle(new Rectangle(PlaceToPoint(new Place(0, Selection.End.iLine)),
                                             new Size(2*CharWidth, 2*CharHeight)));

            Point car = PlaceToPoint(Selection.Start);
            Point car2 = PlaceToPoint(Selection.End);
            car.Offset(-CharWidth, -ClientSize.Height/2);
            DoVisibleRectangle(new Rectangle(car, new Size(Math.Abs(car2.X - car.X), ClientSize.Height)));
            //Math.Abs(car2.Y-car.Y) + 2 * CharHeight

            Invalidate();
        }

        /// <summary>
        /// Scroll control for display given range
        /// </summary>
        public void DoRangeVisible(Range range)
        {
            DoRangeVisible(range, false);
        }

        /// <summary>
        /// Scroll control for display given range
        /// </summary>
        public void DoRangeVisible(Range range, bool tryToCentre)
        {
            range = range.Clone();
            range.Normalize();
            range.End = new Place(range.End.iChar,
                                  Math.Min(range.End.iLine, range.Start.iLine + ClientSize.Height/CharHeight));

            if (LineInfos[range.End.iLine].VisibleState != VisibleState.Visible)
                ExpandBlock(range.End.iLine);

            if (LineInfos[range.Start.iLine].VisibleState != VisibleState.Visible)
                ExpandBlock(range.Start.iLine);

            Recalc();
            int h = (1 + range.End.iLine - range.Start.iLine)*CharHeight;
            Point p = PlaceToPoint(new Place(0, range.Start.iLine));
            if (tryToCentre)
            {
                p.Offset(0, -ClientSize.Height/2);
                h = ClientSize.Height;
            }
            DoVisibleRectangle(new Rectangle(p, new Size(2*CharWidth, h)));

            Invalidate();
        }

        #endregion

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.KeyCode == Keys.ShiftKey)
                lastModifiers &= ~Keys.Shift;
            if (e.KeyCode == Keys.Alt)
                lastModifiers &= ~Keys.Alt;
            if (e.KeyCode == Keys.ControlKey)
                lastModifiers &= ~Keys.Control;
        }

        /// <summary>
        /// When true the next OnKeyPressing event will be ignored and handled by this.FindChar(...)
        /// </summary>
        public bool findCharMode;

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (middleClickScrollingActivated)
                return;

            base.OnKeyDown(e);

            if (Focused)//??? 
                lastModifiers = e.Modifiers;

            handledChar = false;

            if (e.Handled)
            {
                handledChar = true;
                return;
            }

            if (ProcessKey(e.KeyData))
                return;

            e.Handled = true;

            DoCaretVisible();
            Invalidate();
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if ((keyData & Keys.Alt) > 0)
            {
                if (HotkeysMapping.ContainsKey(keyData))
                {
                    ProcessKey(keyData);
                    return true;
                }
            }

            return base.ProcessDialogKey(keyData);
        }

        /// <summary>
        /// Process control keys
        /// </summary>
        public virtual bool ProcessKey(Keys keyData)
        {
           KeyEventArgs a = new KeyEventArgs(keyData);

            if(a.KeyCode == Keys.Tab && !AcceptsTab)
                 return false;


            if (macrosManager != null)
            if (!HotkeysMapping.ContainsKey(keyData) || (HotkeysMapping[keyData] != FCTBAction.MacroExecute && HotkeysMapping[keyData] != FCTBAction.MacroRecord))
                macrosManager.ProcessKey(keyData);


            if (HotkeysMapping.ContainsKey(keyData))
            {
                var act = HotkeysMapping[keyData];
                this.FCTBActionHandler.DoAction(act);
                if (HotkeysMapping.ScrollActions.ContainsKey(act))
                    return true;
            }
            else
            {
                /*  !!!!
                //space
                if (a.KeyCode == Keys.Space && (a.Modifiers == Keys.None || a.Modifiers == Keys.Shift))
                {
                    if (OnKeyPressing(' ')) //KeyPress event processed key
                        return false;

                    if (Selection.ReadOnly) return false;

                    if (!Selection.IsEmpty)
                        ClearSelected();

                    //replace mode? select forward char
                    if (IsReplaceMode)
                    {
                        Selection.GoRight(true);
                        Selection.Inverse();
                        if (Selection.ReadOnly) return false;
                    }

                    InsertChar(' ');
                    OnKeyPressed(' ');
                    return false;
                }

                //backspace
                if (a.KeyCode == Keys.Back && (a.Modifiers == Keys.None || a.Modifiers == Keys.Shift))
                {
                    if (OnKeyPressing('\b')) //KeyPress event processed key
                        return false;

                    if (Selection.ReadOnly) return false;

                    if (!Selection.IsEmpty)
                        ClearSelected();
                    else
                        if (!Selection.IsReadOnlyLeftChar()) //is not left char readonly?
                            InsertChar('\b');

                    OnKeyPressed('\b');
                    return false;
                }*/

                //
                if (a.KeyCode == Keys.Alt)
                    return true;

                if ((a.Modifiers & Keys.Control) != 0)
                    return true;

                if ((a.Modifiers & Keys.Alt) != 0)
                {
                    if ((MouseButtons & MouseButtons.Left) != 0)
                        CheckAndChangeSelectionType();
                    return true;
                }

                if (a.KeyCode == Keys.ShiftKey)
                    return true;
            }

            return false;
        }



        public virtual void OnCustomAction(CustomActionEventArgs e)
        {
            if (CustomAction != null)
                CustomAction(this, e);
        }

        Font originalFont;

        public void RestoreFontSize()
        {
            Zoom = 100;
        }

        /*
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                bool proc = ProcessKeyPress('\r');
                if (proc)
                {
                    base.OnKeyDown(new KeyEventArgs(Keys.Enter));
                    return true;
                }
            }
            
            return base.ProcessCmdKey(ref msg, keyData);
        }*/

        public void OnKeyPressing(KeyPressEventArgs args)
        {
            if (KeyPressing != null)
                KeyPressing(this, args);
        }

        public bool OnKeyPressing(char c)
        {
            if (findCharMode)
            {
                // suppress OnKeyPressing event
                findCharMode = false;
                FindChar(c);
                return true;
            }
            var args = new KeyPressEventArgs(c);
            OnKeyPressing(args);
            return args.Handled;
        }

        public void OnKeyPressed(char c)
        {
            var args = new KeyPressEventArgs(c);
            if (KeyPressed != null)
                KeyPressed(this, args);
        }

        protected override bool ProcessMnemonic(char charCode)
        {
            if (middleClickScrollingActivated)
                return false;

            if (Focused)
                return ProcessKey(charCode, lastModifiers) || base.ProcessMnemonic(charCode);
            else
                return false;
        }

        const int WM_CHAR = 0x102;

        protected override bool ProcessKeyMessage(ref Message m)
        {
            if (m.Msg == WM_CHAR)
                ProcessMnemonic(Convert.ToChar(m.WParam.ToInt32()));

            return base.ProcessKeyMessage(ref m);
        }

        /// <summary>
        /// Process "real" keys (no control)
        /// </summary>
        public virtual bool ProcessKey(char c, Keys modifiers)
        {
            if (handledChar)
                return true;

            if (macrosManager != null)
                macrosManager.ProcessKey(c, modifiers);
            /*  !!!!
            if (c == ' ')
                return true;*/

            //backspace
            if (c == '\b' && (modifiers == Keys.None || modifiers == Keys.Shift || (modifiers & Keys.Alt) != 0))
            {
                if (ReadOnly || !Enabled)
                    return false;

                if (OnKeyPressing(c))
                    return true;

                if (Selection.ReadOnly)
                    return false;

                if (!Selection.IsEmpty)
                    ClearSelected();
                else
                    if (!Selection.IsReadOnlyLeftChar()) //is not left char readonly?
                        InsertChar('\b');

                OnKeyPressed('\b');
                return true;
            }
 
            /* !!!!
            if (c == '\b' && (modifiers & Keys.Alt) != 0)
                return true;*/

            if (char.IsControl(c) && c != '\r' && c != '\t')
                return false;

            if (ReadOnly || !Enabled)
                return false;


            if (modifiers != Keys.None &&
                modifiers != Keys.Shift &&
                modifiers != (Keys.Control | Keys.Alt) && //ALT+CTRL is special chars (AltGr)
                modifiers != (Keys.Shift | Keys.Control | Keys.Alt) && //SHIFT + ALT + CTRL is special chars (AltGr)
                (modifiers != (Keys.Alt) || char.IsLetterOrDigit(c)) //may be ALT+LetterOrDigit is mnemonic code
                )
                return false; //do not process Ctrl+? and Alt+? keys

            char sourceC = c;
            if (OnKeyPressing(sourceC)) //KeyPress event processed key
                return true;

            //
            if (Selection.ReadOnly)
                return false;
            //
            if (c == '\r' && !AcceptsReturn)
                return false;

            //is not tab?
            if (c != '\t')
            {
                string cStr = c.ToString(); // for EOL characters we need a string
                //replace \r on \n
                if (c == '\r')
                {
                    c = '\n';
                    switch (DefaultEolFormat)
                    {
                        case EolFormat.CRLF:
                            cStr = "\r\n";
                            break;
                        case EolFormat.LF:
                            cStr = "\n";
                            break;
                    }
                }
                //replace mode? select forward char
                if (IsReplaceMode)
                {
                    Selection.GoRight(true);
                    Selection.Inverse();
                }
                //insert char
                if (!Selection.ReadOnly)
                {
                    if (!DoAutocompleteBrackets(c))
                    {
                        InsertText(cStr);
                    }
                }

                //do autoindent
                if (c == '\n' || AutoIndentExistingLines)
                    DoAutoIndentIfNeed();
            }

            DoCaretVisible();
            Invalidate();

            OnKeyPressed(sourceC);

            return true;
        }

        /// <summary>
        /// Returns true when bracket completion was performed.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private bool DoAutocompleteBrackets(char c)
        {
            if (AutoCompleteBrackets)
            {
                if (!Selection.ColumnSelectionMode)
                {
                    for (int i = 1; i < autoCompleteBracketsList.Length; i += 2)
                    {
                        if (c == autoCompleteBracketsList[i] && c == Selection.CharAfterStart)
                        {
                            Selection.GoRight();
                            return true;
                        }
                    }
                }

                for (int i = 0; i < autoCompleteBracketsList.Length; i += 2)
                {
                    if (c == autoCompleteBracketsList[i])
                    {
                        InsertBrackets(autoCompleteBracketsList[i], autoCompleteBracketsList[i + 1]);
                        return true;
                    }
                }
            }
            return false;
        }

        private bool InsertBrackets(char left, char right)
        {
            if (Selection.ColumnSelectionMode)
            {
                var range = Selection.Clone();
                range.Normalize();
                Selection.BeginUpdate();
                BeginAutoUndo();
                Selection = new Range(this, range.Start.iChar, range.Start.iLine, range.Start.iChar, range.End.iLine) { ColumnSelectionMode = true };
                InsertChar(left);
                Selection = new Range(this, range.End.iChar + 1, range.Start.iLine, range.End.iChar + 1, range.End.iLine) { ColumnSelectionMode = true };
                InsertChar(right);
                if (range.IsEmpty)
                    Selection = new Range(this, range.End.iChar + 1, range.Start.iLine, range.End.iChar + 1, range.End.iLine) { ColumnSelectionMode = true };
                EndAutoUndo();
                Selection.EndUpdate();
            }
            else if (Selection.IsEmpty)
            {
                InsertText(left + "" + right);
                Selection.GoLeft();
            }
            else
            {
                InsertText(left + SelectedText + right);
            }

            return true;
        }

        /// <summary>
        /// Finds given char after current caret position, moves the caret to found pos.
        /// </summary>
        /// <param name="c"></param>
        protected virtual void FindChar(char c)
        {
            if (c == '\r')
                c = '\n';

            var r = Selection.Clone();
            while (r.GoRight())
            {
                if (r.CharBeforeStart == c)
                {
                    Selection = r;
                    DoCaretVisible();
                    return;
                }
            }
        }

        private void DoAutoIndentIfNeed()
        {
            if (Selection.ColumnSelectionMode)
                return;
            if (AutoIndent)
            {
                DoCaretVisible();
                int needSpaces = CalcAutoIndent(Selection.Start.iLine);
                if (this[Selection.Start.iLine].AutoIndentSpacesNeededCount != needSpaces)
                {
                    DoAutoIndent(Selection.Start.iLine);
                    this[Selection.Start.iLine].AutoIndentSpacesNeededCount = needSpaces;
                }
            }
        }

        public void RemoveSpacesAfterCaret()
        {
            if (!Selection.IsEmpty)
                return;
            Place end = Selection.Start;
            while (Selection.CharAfterStart == ' ')
                Selection.GoRight(true);
            ClearSelected();
        }

        /// <summary>
        /// Inserts autoindent's spaces in the line
        /// </summary>
        public virtual void DoAutoIndent(int iLine)
        {
            if (Selection.ColumnSelectionMode)
                return;
            Place oldStart = Selection.Start;
            //
            int needSpaces = CalcAutoIndent(iLine);
            //
            int spaces = lines[iLine].StartSpacesCount;
            int needToInsert = needSpaces - spaces;
            if (needToInsert < 0)
                needToInsert = -Math.Min(-needToInsert, spaces);
            //insert start spaces
            if (needToInsert == 0)
                return;
            Selection.Start = new Place(0, iLine);
            if (needToInsert > 0)
                InsertText(new String(' ', needToInsert));
            else
            {
                Selection.Start = new Place(0, iLine);
                Selection.End = new Place(-needToInsert, iLine);
                ClearSelected();
            }

            Selection.Start = new Place(Math.Min(lines[iLine].Count, Math.Max(0, oldStart.iChar + needToInsert)), iLine);
        }

        /// <summary>
        /// Returns needed start space count for the line
        /// </summary>
        public virtual int CalcAutoIndent(int iLine)
        {
            if (iLine < 0 || iLine >= LinesCount) return 0;


            EventHandler<AutoIndentEventArgs> calculator = AutoIndentNeeded;
            if (calculator == null)
                if (Language != Language.Custom && SyntaxHighlighter != null)
                    calculator = SyntaxHighlighter.AutoIndentNeeded;
                else
                    calculator = CalcAutoIndentShiftByCodeFolding;

            int needSpaces = 0;

            var stack = new Stack<AutoIndentEventArgs>();
            //calc indent for previous lines, find stable line
            int i;
            for (i = iLine - 1; i >= 0; i--)
            {
                var args = new AutoIndentEventArgs(i, lines[i].Text, i > 0 ? lines[i - 1].Text : "", TabLength, 0);
                calculator(this, args);
                stack.Push(args);
                if (args.Shift == 0 && args.AbsoluteIndentation == 0 && args.LineText.Trim() != "")
                    break;
            }
            int indent = lines[i >= 0 ? i : 0].StartSpacesCount;
            while (stack.Count != 0)
            {
                var arg = stack.Pop();
                if (arg.AbsoluteIndentation != 0)
                    indent = arg.AbsoluteIndentation + arg.ShiftNextLines;
                else
                    indent += arg.ShiftNextLines;
            }
            //clalc shift for current line
            var a = new AutoIndentEventArgs(iLine, lines[iLine].Text, iLine > 0 ? lines[iLine - 1].Text : "", TabLength, indent);
            calculator(this, a);
            needSpaces = a.AbsoluteIndentation + a.Shift;

            return needSpaces;
        }

        internal virtual void CalcAutoIndentShiftByCodeFolding(object sender, AutoIndentEventArgs args)
        {
            //inset TAB after start folding marker
            if (string.IsNullOrEmpty(lines[args.iLine].FoldingEndMarker) &&
                !string.IsNullOrEmpty(lines[args.iLine].FoldingStartMarker))
            {
                args.ShiftNextLines = TabLength;
                return;
            }
            //remove TAB before end folding marker
            if (!string.IsNullOrEmpty(lines[args.iLine].FoldingEndMarker) &&
                string.IsNullOrEmpty(lines[args.iLine].FoldingStartMarker))
            {
                args.Shift = -TabLength;
                args.ShiftNextLines = -TabLength;
                return;
            }
        }


        private int GetMinStartSpacesCount(int fromLine, int toLine)
        {
            if (fromLine > toLine)
                return 0;

            int result = int.MaxValue;
            for (int i = fromLine; i <= toLine; i++)
            {
                int count = lines[i].StartSpacesCount;
                if (count < result)
                    result = count;
            }

            return result;
        }

        /// <summary>
        /// Undo last operation
        /// </summary>
        public virtual void Undo()
        {
            lines.Manager.Undo();
            DoCaretVisible();
            Invalidate();
        }

        /// <summary>
        /// Redo
        /// </summary>
        public virtual void Redo()
        {
            lines.Manager.Redo();
            DoCaretVisible();
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Tab && !AcceptsTab)
                return false;
            if (keyData == Keys.Enter && !AcceptsReturn)
                return false;

            if ((keyData & Keys.Alt) == Keys.None)
            {
                Keys keys = keyData & Keys.KeyCode;
                if (keys == Keys.Return)
                    return true;
            }

            if ((keyData & Keys.Alt) != Keys.Alt)
            {
                switch ((keyData & Keys.KeyCode))
                {
                    case Keys.Prior:
                    case Keys.Next:
                    case Keys.End:
                    case Keys.Home:
                    case Keys.Left:
                    case Keys.Right:
                    case Keys.Up:
                    case Keys.Down:
                        return true;

                    case Keys.Escape:
                        return false;

                    case Keys.Tab:
                        return (keyData & Keys.Control) == Keys.None;
                }
            }

            return base.IsInputKey(keyData);
        }


        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (BackBrush == null)
                base.OnPaintBackground(e);
            else
                e.Graphics.FillRectangle(BackBrush, ClientRectangle);
        }

        /// <summary>
        /// Draw control
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (needRecalc)
                Recalc();

            if (needRecalcFoldingLines)
                RecalcFoldingLines();
#if debug
            var sw = Stopwatch.StartNew();
#endif
            visibleMarkers.Clear();
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            //
            var servicePen = new Pen(ServiceLinesColor);
            
            Brush indentBrush = new SolidBrush(IndentBackColor);
            Brush paddingBrush = new SolidBrush(PaddingBackColor);
            
            //draw padding area
            Rendering.RenderPadding(e.Graphics, this, paddingBrush);
            
            var textAreaRect = TextAreaRect;

            //
            int leftTextIndent = Math.Max(LeftIndent, LeftIndent + Paddings.Left - HorizontalScroll.Value);
            int textWidth = textAreaRect.Width;
            //draw indent area
            e.Graphics.FillRectangle(indentBrush, 0, 0, LeftIndentLine, ClientSize.Height);
            if (LeftIndent > MIN_LEFT_INDENT)
            {
                e.Graphics.DrawLine(servicePen, LeftIndentLine, 0, LeftIndentLine, ClientSize.Height);
            }
            //draw preferred line width
            if (PreferredLineWidth > 0)
            {
                e.Graphics.DrawLine(servicePen,
                                    new Point(
                                        LeftIndent + Paddings.Left + PreferredLineWidth*CharWidth -
                                        HorizontalScroll.Value + 1, textAreaRect.Top + 1),
                                    new Point(
                                        LeftIndent + Paddings.Left + PreferredLineWidth*CharWidth -
                                        HorizontalScroll.Value + 1, textAreaRect.Bottom - 1));
            }
            //draw text area border
            Rendering.DrawTextAreaBorder(e.Graphics, this);
            //

            int endLine = Rendering.DrawLines(e, this, servicePen);

            // y-coordinate to line index
            int startLine = this.YtoLineIndex(this.VerticalScroll.Value);

            //draw folding lines
            if (ShowFoldingLines)
                Rendering.DrawFoldingLines(e, this, startLine, endLine);

            //draw column selection
            if (Selection.ColumnSelectionMode)
            {
                if (SelectionStyle.BackgroundBrush is SolidBrush)
                {
                    Color color = ((SolidBrush) SelectionStyle.BackgroundBrush).Color;
                    Point p1 = PlaceToPoint(Selection.Start);
                    Point p2 = PlaceToPoint(Selection.End);
                    using (var pen = new Pen(color))
                    {
                        e.Graphics.DrawRectangle(pen,
                                                 Rectangle.FromLTRB(Math.Min(p1.X, p2.X) - 1, Math.Min(p1.Y, p2.Y),
                                                                    Math.Max(p1.X, p2.X),
                                                                    Math.Max(p1.Y, p2.Y) + CharHeight));
                    }
                }
            }
            //draw brackets highlighting
            Rendering.DrawBracketsHighlighting(e.Graphics, this);

            //
            e.Graphics.SmoothingMode = SmoothingMode.None;
            //draw folding indicator
            if ((startFoldingLine >= 0 || endFoldingLine >= 0) && Selection.Start == Selection.End)
            {
                if (endFoldingLine < LineInfos.Count)
                {
                    //folding indicator
                    int startFoldingY = (startFoldingLine >= 0 ? LineInfos[startFoldingLine].startY : 0) -
                                        VerticalScroll.Value + CharHeight/2;
                    int endFoldingY = (endFoldingLine >= 0
                                           ? LineInfos[endFoldingLine].startY +
                                             (LineInfos[endFoldingLine].WordWrapStringsCount - 1)*CharHeight
                                           : TextHeight + CharHeight) - VerticalScroll.Value + CharHeight;

                    using (var indicatorPen = new Pen(Color.FromArgb(100, FoldingIndicatorColor), 4))
                    {
                        e.Graphics.DrawLine(indicatorPen, LeftIndent - 5, startFoldingY, LeftIndent - 5, endFoldingY);
                    }
                }
            }
            //draw hint's brackets
            Rendering.PaintHintBrackets(e.Graphics, this);
            //draw markers
            foreach (VisualMarker m in this.visibleMarkers)
                m.Draw(e.Graphics, servicePen);
            //draw caret
            Rendering.DrawCaret(e.Graphics, this);

            //draw disabled mask
            if (!Enabled)
                using (var brush = new SolidBrush(DisabledColor))
                    e.Graphics.FillRectangle(brush, ClientRectangle);

            if (MacrosManager.IsRecording)
                Rendering.DrawRecordingHint(e.Graphics, this);

            if (middleClickScrollingActivated)
                DrawMiddleClickScrolling(e.Graphics);

            //dispose resources
            servicePen.Dispose();
            
            indentBrush.Dispose();
            
            paddingBrush.Dispose();
            //
#if debug
            sw.Stop();
            Console.WriteLine("OnPaint: "+ sw.ElapsedMilliseconds);
#endif
            //
            base.OnPaint(e);
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            mouseIsDrag = false;
            mouseIsDragDrop = false;
            draggedRange = null;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            isLineSelect = false;

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (mouseIsDragDrop)
                    OnMouseClickText(e);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (middleClickScrollingActivated)
            {
                DeactivateMiddleClickScrollingMode();
                mouseIsDrag = false;
                if(e.Button == System.Windows.Forms.MouseButtons.Middle)
                    RestoreScrollsAfterMiddleClickScrollingMode();
                return;
            }

            MacrosManager.IsRecording = false;

            Select();
            ActiveControl = null;

            if (e.Button == MouseButtons.Left)
            {
                VisualMarker marker = FindVisualMarkerForPoint(e.Location);
                //click on marker
                if (marker != null)
                {
                    mouseIsDrag = false;
                    mouseIsDragDrop = false;
                    draggedRange = null;
                    OnMarkerClick(e, marker);
                    return;
                }

                mouseIsDrag = true;
                mouseIsDragDrop = false;
                draggedRange = null;
                isLineSelect = (e.Location.X < LeftIndentLine);

                if (!isLineSelect)
                {
                    var p = PointToPlace(e.Location);

                    if (e.Clicks == 2)
                    {
                        mouseIsDrag = false;
                        mouseIsDragDrop = false;
                        draggedRange = null;

                        SelectWord(p);
                        return;
                    }

                    if (Selection.IsEmpty || !Selection.Contains(p) || this[p.iLine].Count <= p.iChar || ReadOnly)
                        OnMouseClickText(e);
                    else
                    {
                        mouseIsDragDrop = true;
                        mouseIsDrag = false;
                    }
                }
                else
                {
                    CheckAndChangeSelectionType();

                    Selection.BeginUpdate();
                    //select whole line
                    int iLine = PointToPlaceSimple(e.Location).iLine;
                    lineSelectFrom = iLine;
                    Selection.Start = new Place(0, iLine);
                    Selection.End = new Place(GetLineLength(iLine), iLine);
                    Selection.EndUpdate();
                    Invalidate();
                }
            }
            else
            if (e.Button == MouseButtons.Middle)
            {
                ActivateMiddleClickScrollingMode(e);
            }
        }

        private void OnMouseClickText(MouseEventArgs e)
        {
            //click on text
            Place oldEnd = Selection.End;
            Selection.BeginUpdate();

            if (Selection.ColumnSelectionMode)
            {
                Selection.Start = PointToPlaceSimple(e.Location);
                Selection.ColumnSelectionMode = true;
            }
            else
            {
                if (VirtualSpace)
                    Selection.Start = PointToPlaceSimple(e.Location);
                else
                    Selection.Start = PointToPlace(e.Location);
            }

            if ((lastModifiers & Keys.Shift) != 0)
                Selection.End = oldEnd;

            CheckAndChangeSelectionType();

            Selection.EndUpdate();
            Invalidate();
            return;
        }

        /// <summary>
        /// When ALT is pressed and wordwrap is off => ColumnSelectionMode = true.
        /// Else ColumnSelectionMode = false.
        /// </summary>
        public void CheckAndChangeSelectionType()
        {
            //change selection type to ColumnSelectionMode
            if ((ModifierKeys & Keys.Alt) != 0 && !WordWrap)
            {
                Selection.ColumnSelectionMode = true;
            }
            else
            //change selection type to Range
            {
                Selection.ColumnSelectionMode = false;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            Invalidate();

            if (lastModifiers == Keys.Control)
            {
                ChangeFontSize(2 * Math.Sign(e.Delta));
                ((HandledMouseEventArgs)e).Handled = true;
            }
            else
            if (VerticalScroll.Visible || !ShowScrollBars)
            {
                //base.OnMouseWheel(e);

                // Determine scoll offset
                int mouseWheelScrollLinesSetting = WindowsRegistry.GetControlPanelWheelScrollLinesValue();

                DoScrollVertical(mouseWheelScrollLinesSetting, e.Delta);

                ((HandledMouseEventArgs)e).Handled = true;
            }

            DeactivateMiddleClickScrollingMode();
        }

        public void DoScrollVertical(int countLines, int direction)
        {
            int numberOfVisibleLines = ClientSize.Height / CharHeight;

            int offset;
            if ((countLines == -1) || (countLines > numberOfVisibleLines))
                offset = CharHeight*numberOfVisibleLines;
            else
                offset = CharHeight*countLines;

            var newScrollPos = VerticalScroll.Value - Math.Sign(direction) * offset;

            var ea = new ScrollEventArgs(direction > 0 ? ScrollEventType.SmallDecrement : ScrollEventType.SmallIncrement,
                                         VerticalScroll.Value,
                                         newScrollPos,
                                         ScrollOrientation.VerticalScroll);

            OnScroll(ea);
        }

        /// <summary>
        /// Changes font size by zooming
        /// </summary>
        /// <param name="step"></param>
        public void ChangeFontSize(int step)
        {
            var points = Font.SizeInPoints;
            using (var gr = Graphics.FromHwnd(Handle))
            {
                var dpi = gr.DpiY;
                var newPoints = points + step * 72f / dpi;
                if (newPoints < 1f) return;
                var k = newPoints / originalFont.SizeInPoints;
                Zoom = (int)(100 * k);
            }
        }

        #region Zoom

        private int zoom = 100;

        /// <summary>
        /// Zooming (in percentages).
        /// Setting the zoom will execute the zoom and call ZoomChanged
        /// </summary>
        [Browsable(false)]
        public int Zoom 
        {
            get { return zoom; }
            set {
                zoom = value;
                DoZoom(zoom / 100f);
                OnZoomChanged();
            }
        }

        protected virtual void OnZoomChanged()
        {
            if (ZoomChanged != null)
                ZoomChanged(this, EventArgs.Empty);
        }

        private void DoZoom(float koeff)
        {
            //remmber first displayed line
            var iLine = YtoLineIndex(VerticalScroll.Value);
            //
            var points = originalFont.SizeInPoints;
            points *= koeff;

            if (points < 1f || points > 300f) return;

            var oldFont = Font;
            SetFont(new Font(Font.FontFamily, points, Font.Style, GraphicsUnit.Point));
            oldFont.Dispose();

            NeedRecalc(true);

            //restore first displayed line
            if (iLine < LinesCount)
                VerticalScroll.Value = Math.Min(VerticalScroll.Maximum, LineInfos[iLine].startY);
            UpdateScrollbars();
            //
            Invalidate();
            OnVisibleRangeChanged();
        }

        #endregion

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            CancelToolTip();
        }

        /// <summary>
        /// What did we drag
        /// </summary>
        internal Range draggedRange;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (middleClickScrollingActivated)
                return;

            if (lastMouseCoord != e.Location)
            {
                //restart tooltip timer because mouse position changed
                CancelToolTip();
                tooltipDelayTimer.Start();
            }
            lastMouseCoord = e.Location;

            if (e.Button == MouseButtons.Left && mouseIsDragDrop)
            {
                draggedRange = Selection.Clone();
                DoDragDrop(SelectedText, DragDropEffects.Copy);
                draggedRange = null;
                return;
            }

            if (e.Button == MouseButtons.Left && mouseIsDrag)
            {
                Place place;
                if (Selection.ColumnSelectionMode || VirtualSpace)
                    place = PointToPlaceSimple(e.Location);
                else
                    place = PointToPlace(e.Location);

                if (isLineSelect)
                {
                    Selection.BeginUpdate();

                    int iLine = place.iLine;
                    if (iLine < lineSelectFrom)
                    {
                        Selection.Start = new Place(0, iLine);
                        Selection.End = new Place(GetLineLength(lineSelectFrom), lineSelectFrom);
                    }
                    else
                    {
                        Selection.Start = new Place(GetLineLength(iLine), iLine);
                        Selection.End = new Place(0, lineSelectFrom);
                    }

                    Selection.EndUpdate();
                    DoCaretVisible();
                    HorizontalScroll.Value = 0;
                    UpdateScrollbars();
                    Invalidate();
                }
                else if (place != Selection.Start)
                {
                    Place oldEnd = Selection.End;
                    Selection.BeginUpdate();
                    if (Selection.ColumnSelectionMode)
                    {
                        Selection.Start = place;
                        Selection.ColumnSelectionMode = true;
                    }
                    else
                        Selection.Start = place;
                    Selection.End = oldEnd;
                    Selection.EndUpdate();
                    DoCaretVisible();
                    Invalidate();
                    return;
                }
            }

            VisualMarker marker = FindVisualMarkerForPoint(e.Location);
            if (marker != null)
                base.Cursor = marker.Cursor;
            else
            {
                if (e.Location.X < LeftIndentLine || isLineSelect)
                    base.Cursor = Cursors.Arrow;
                else
                    base.Cursor = defaultCursor;
            }
        }

        private void CancelToolTip()
        {
            tooltipDelayTimer.Stop();
            if (ToolTip != null && !string.IsNullOrEmpty(ToolTip.GetToolTip(this)))
            {
                ToolTip.Hide(this);
                ToolTip.SetToolTip(this, null);
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            var m = FindVisualMarkerForPoint(e.Location);
            if (m != null)
                OnMarkerDoubleClick(m);
        }

        private void SelectWord(Place p)
        {
            int fromX = p.iChar;
            int toX = p.iChar;

            for (int i = p.iChar; i < lines[p.iLine].Count; i++)
            {
                char c = lines[p.iLine][i].c;
                if (char.IsLetterOrDigit(c) || c == '_')
                    toX = i + 1;
                else
                    break;
            }

            for (int i = p.iChar - 1; i >= 0; i--)
            {
                char c = lines[p.iLine][i].c;
                if (char.IsLetterOrDigit(c) || c == '_')
                    fromX = i;
                else
                    break;
            }

            Selection = new Range(this, toX, p.iLine, fromX, p.iLine);
        }

        public int YtoLineIndex(int y)
        {
            int i = LineInfos.BinarySearch(new LineInfo(-10), new LineYComparer(y));
            i = i < 0 ? -i - 2 : i;
            if (i < 0) return 0;
            if (i > lines.Count - 1) return lines.Count - 1;
            return i;
        }

        /// <summary>
        /// Gets nearest line and char position from coordinates
        /// FIXME: Take care of wordwrapping and real TABs
        /// </summary>
        /// <param name="point">Point</param>
        /// <returns>Line and char position</returns>
        public Place PointToPlace(Point point)
        {
            #if debug
            var sw = Stopwatch.StartNew();
            #endif
            point.Offset(HorizontalScroll.Value, VerticalScroll.Value);
            point.Offset(-LeftIndent - Paddings.Left, 0);
            int iLine = YtoLineIndex(point.Y);
            if (iLine < 0)
                return Place.Empty;

            int y = 0;

            for (; iLine < lines.Count; iLine++)
            {
                y = LineInfos[iLine].startY + LineInfos[iLine].WordWrapStringsCount*CharHeight;
                if (y > point.Y && LineInfos[iLine].VisibleState == VisibleState.Visible)
                    break;
            }
            if (iLine >= lines.Count)
                iLine = lines.Count - 1;
            if (LineInfos[iLine].VisibleState != VisibleState.Visible)
                iLine = FindPrevVisibleLine(iLine);
            //
            int iWordWrapLine = LineInfos[iLine].WordWrapStringsCount;
            do
            {
                iWordWrapLine--;
                y -= CharHeight;
            } while (y > point.Y);
            if (iWordWrapLine < 0) iWordWrapLine = 0;
            //
            int start = LineInfos[iLine].GetWordWrapStringStartPosition(iWordWrapLine);
            int finish = LineInfos[iLine].GetWordWrapStringFinishPosition(iWordWrapLine, lines[iLine]);

            int x;
            // var x = (int) Math.Round((float) point.X/CharWidth);
            if (this.ConvertTabToSpaces)
            {
                // each character has a fixed width
                x = (int)Math.Round((float)point.X / CharWidth); 
            }
            else
            {
                // we need to correct the charwidth width the tablength
                x = TextSizeCalculator.CharIndexAtPoint(lines[iLine].Text, this.TabLength, this.CharWidth, point.X);
            }

            if (iWordWrapLine > 0)
                x -= LineInfos[iLine].wordWrapIndent;


            x = x < 0 ? start : start + x;
            if (x > finish)
                x = finish + 1;
            if (x > lines[iLine].Count)
                x = lines[iLine].Count;

#if debug
            Console.WriteLine("PointToPlace: " + sw.ElapsedMilliseconds);
#endif

            return new Place(x, iLine);
        }

        private Place PointToPlaceSimple(Point point)
        {
            point.Offset(HorizontalScroll.Value, VerticalScroll.Value);
            point.Offset(-LeftIndent - Paddings.Left, 0);
            int iLine = YtoLineIndex(point.Y);
            int x;
            if (this.ConvertTabToSpaces)
            {
                // each character has a fixed width
                x = (int)Math.Round((float)point.X / CharWidth);
            }
            else
            {
                // we need to correct the charwidth width the tablength
                x = TextSizeCalculator.CharIndexAtPoint(lines[iLine].Text, this.TabLength, this.CharWidth, point.X);
            }
            if (x < 0) x = 0;
            return new Place(x, iLine);
        }

        /// <summary>
        /// Gets nearest absolute text position for given point
        /// </summary>
        /// <param name="point">Point</param>
        /// <returns>Position</returns>
        public int PointToPosition(Point point)
        {
            return TextSourceUtil.PlaceToPosition(this.lines, PointToPlace(point));
        }

        /// <summary>
        /// Fires TextChanging event
        /// </summary>
        public virtual void OnTextChanging(ref string text)
        {
            ClearBracketsPositions();

            if (TextChanging != null)
            {
                var args = new TextChangingEventArgs {InsertingText = text};
                TextChanging(this, args);
                text = args.InsertingText;
                if (args.Cancel)
                    text = string.Empty;
            }
        }

        public virtual void OnTextChanging()
        {
            string temp = null;
            OnTextChanging(ref temp);
        }

        /// <summary>
        /// Fires TextChanged event
        /// </summary>
        public virtual void OnTextChanged()
        {
            var r = new Range(this);
            r.SelectAll();
            OnTextChanged(new TextChangedEventArgs(r));
        }

        /// <summary>
        /// Fires TextChanged event
        /// </summary>
        public virtual void OnTextChanged(int fromLine, int toLine)
        {
            var r = new Range(this);
            r.Start = new Place(0, Math.Min(fromLine, toLine));
            r.End = new Place(lines[Math.Max(fromLine, toLine)].Count, Math.Max(fromLine, toLine));
            OnTextChanged(new TextChangedEventArgs(r));
        }

        /// <summary>
        /// Fires TextChanged event
        /// </summary>
        public virtual void OnTextChanged(Range r)
        {
            OnTextChanged(new TextChangedEventArgs(r));
        }

        /// <summary>
        /// Call this method before multiple text changing.
        /// OnTextChanged will be called when all BeginUpdate() calls are ended with EndUpdate().
        /// Every BeginUpdate(). should have a corresponding EndUpdate().
        /// </summary>
        public void BeginUpdate()
        {
            if (updating == 0)
                updatingRange = null;
            updating++;
        }

        /// <summary>
        /// Call this method after multiple text changing.
        /// Every BeginUpdate(). should have a corresponding EndUpdate().
        /// </summary>
        public void EndUpdate()
        {
            updating--;

            if (updating == 0 && updatingRange != null)
            {
                updatingRange.Expand();
                OnTextChanged(updatingRange);
            }
        }


        /// <summary>
        /// Fires TextChanged event
        /// </summary>
        protected virtual void OnTextChanged(TextChangedEventArgs args)
        {
            //
            args.ChangedRange.Normalize();
            //
            if (updating > 0)
            {
                if (updatingRange == null)
                    updatingRange = args.ChangedRange.Clone();
                else
                {
                    if (updatingRange.Start.iLine > args.ChangedRange.Start.iLine)
                        updatingRange.Start = new Place(0, args.ChangedRange.Start.iLine);
                    if (updatingRange.End.iLine < args.ChangedRange.End.iLine)
                        updatingRange.End = new Place(lines[args.ChangedRange.End.iLine].Count,
                                                      args.ChangedRange.End.iLine);
                    updatingRange = updatingRange.GetIntersectionWith(Range);
                }
                return;
            }
            //
#if debug
            var sw = Stopwatch.StartNew();
            #endif
            CancelToolTip();
            this.Hints.Clear();
            IsChanged = true;
            TextVersion++;
            MarkLinesAsChanged(args.ChangedRange);
            ClearFoldingState(args.ChangedRange);
            //
            if (wordWrap)
                RecalcWordWrap(args.ChangedRange.Start.iLine, args.ChangedRange.End.iLine);
            //
            base.OnTextChanged(args);

            //dalayed event stuffs
            if (delayedTextChangedRange == null)
                delayedTextChangedRange = args.ChangedRange.Clone();
            else
                delayedTextChangedRange = delayedTextChangedRange.GetUnionWith(args.ChangedRange);

            needRiseTextChangedDelayed = true;
            ResetTimer(delayedTextChangedTimer);
            //
            OnSyntaxHighlight(args);
            //
            if (TextChanged != null)
                TextChanged(this, args);
            //
            if (BindingTextChanged != null)
                BindingTextChanged(this, EventArgs.Empty);
            //
            base.OnTextChanged(EventArgs.Empty);
            //
#if debug
            Console.WriteLine("OnTextChanged: " + sw.ElapsedMilliseconds);
#endif

            OnVisibleRangeChanged();
        }

        /// <summary>
        /// Clears folding state for range of text
        /// </summary>
        private void ClearFoldingState(Range range)
        {
            for (int iLine = range.Start.iLine; iLine <= range.End.iLine; iLine++)
                if (iLine >= 0 && iLine < lines.Count)
                    FoldedBlocks.Remove(this[iLine].UniqueId);
        }


        private void MarkLinesAsChanged(Range range)
        {
            for (int iLine = range.Start.iLine; iLine <= range.End.iLine; iLine++)
                if (iLine >= 0 && iLine < lines.Count)
                    lines[iLine].IsChanged = true;
        }

        /// <summary>
        /// Fires SelectionChanged event
        /// </summary>
        public virtual void OnSelectionChanged()
        {
#if debug
            var sw = Stopwatch.StartNew();
            #endif
            //find folding markers for highlighting
            if (HighlightFoldingIndicator)
                HighlightFoldings();
            //
            needRiseSelectionChangedDelayed = true;
            ResetTimer(delayedEventsTimer);

            if (SelectionChanged != null)
                SelectionChanged(this, new EventArgs());

#if debug
            Console.WriteLine("OnSelectionChanged: "+ sw.ElapsedMilliseconds);
#endif
        }

        //find folding markers for highlighting
        private void HighlightFoldings()
        {
            if (LinesCount == 0)
                return;
            //
            int prevStartFoldingLine = startFoldingLine;
            int prevEndFoldingLine = endFoldingLine;
            //
            startFoldingLine = -1;
            endFoldingLine = -1;
            int counter = 0;
            for (int i = Selection.Start.iLine; i >= Math.Max(Selection.Start.iLine - MAX_LINES_FOR_FOLDING, 0); i--)
            {
                bool hasStartMarker = lines.LineHasFoldingStartMarker(i);
                bool hasEndMarker = lines.LineHasFoldingEndMarker(i);

                if (hasEndMarker && hasStartMarker)
                    continue;

                if (hasStartMarker)
                {
                    counter--;
                    if (counter == -1) //found start folding
                    {
                        startFoldingLine = i;
                        break;
                    }
                }
                if (hasEndMarker && i != Selection.Start.iLine)
                    counter++;
            }
            if (startFoldingLine >= 0)
            {
                //find end of block
                endFoldingLine = Folding.FindEndOfFoldingBlock(this.lines, startFoldingLine, MAX_LINES_FOR_FOLDING, this.FindEndOfFoldingBlockStrategy);
                if (endFoldingLine == startFoldingLine)
                    endFoldingLine = -1;
            }

            if (startFoldingLine != prevStartFoldingLine || endFoldingLine != prevEndFoldingLine)
                OnFoldingHighlightChanged();
        }

        protected virtual void OnFoldingHighlightChanged()
        {
            if (FoldingHighlightChanged != null)
                FoldingHighlightChanged(this, EventArgs.Empty);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            SetAsCurrentTB();
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            lastModifiers = Keys.None;
            DeactivateMiddleClickScrollingMode();
            base.OnLostFocus(e);
            Invalidate();
        }





        /// <summary>
        /// Gets absolute char position from char position
        /// </summary>
        public Point PositionToPoint(int pos)
        {
            return PlaceToPoint(TextSourceUtil.PositionToPlace(this.lines, pos));
        }

        /// <summary>
        /// Gets point for given line and char position
        /// </summary>
        /// <param name="place">Line and char position</param>
        /// <returns>Coordiantes</returns>
        public Point PlaceToPoint(Place place)
        {
            if (place.iLine >= LineInfos.Count)
                return new Point();
            int y = LineInfos[place.iLine].startY;
            //
            int iWordWrapIndex = LineInfos[place.iLine].GetWordWrapStringIndex(place.iChar);
            y += iWordWrapIndex*CharHeight;

            string offsetChars = this.lines[place.iLine].Text.Substring(0, place.iChar);
            int offset = TextSizeCalculator.TextWidth(offsetChars, this.TabLength);
            int x = (offset - LineInfos[place.iLine].GetWordWrapStringStartPosition(iWordWrapIndex)) * CharWidth;
            //int x = (place.iChar - LineInfos[place.iLine].GetWordWrapStringStartPosition(iWordWrapIndex))*CharWidth;

            if(iWordWrapIndex > 0 )
                x += LineInfos[place.iLine].wordWrapIndent * CharWidth;

            //
            y = y - VerticalScroll.Value;
            x = LeftIndent + Paddings.Left + x - HorizontalScroll.Value;

            return new Point(x, y);
        }

        /// <summary>
        /// Get text of given line
        /// </summary>
        /// <param name="iLine">Line index</param>
        /// <returns>Text</returns>
        public string GetLineText(int iLine)
        {
            if (iLine < 0 || iLine >= lines.Count)
                throw new ArgumentOutOfRangeException("iLine", "Line index out of range");
            var sb = new StringBuilder(lines[iLine].Count);
            foreach (Char c in lines[iLine])
                sb.Append(c.c);
            return sb.ToString();
        }

        /// <summary>
        /// Exapnds folded block
        /// </summary>
        /// <param name="iLine">Start line</param>
        public virtual void ExpandFoldedBlock(int iLine)
        {
            if (iLine < 0 || iLine >= lines.Count)
                throw new ArgumentOutOfRangeException("iLine", "Line index out of range");
            //find all hidden lines afetr iLine
            int end = iLine;
            for (; end < LinesCount - 1; end++)
            {
                if (LineInfos[end + 1].VisibleState != VisibleState.Hidden)
                    break;
            }

            ExpandBlock(iLine, end);

            FoldedBlocks.Remove(this[iLine].UniqueId);//remove folded state for this line
            AdjustFolding();
        }

        /// <summary>
        /// Collapse folding blocks using FoldedBlocks dictionary.
        /// </summary>
        public virtual void AdjustFolding()
        {
            //collapse folded blocks
            for (int iLine = 0; iLine < LinesCount; iLine++)
                if (LineInfos[iLine].VisibleState == VisibleState.Visible)
                    if (FoldedBlocks.ContainsKey(this[iLine].UniqueId))
                        CollapseFoldingBlock(iLine);
        }


        /// <summary>
        /// Expand collapsed block
        /// </summary>
        public virtual void ExpandBlock(int fromLine, int toLine)
        {
            int from = Math.Min(fromLine, toLine);
            int to = Math.Max(fromLine, toLine);
            for (int i = from; i <= to; i++)
                SetVisibleState(i, VisibleState.Visible);
            needRecalc = true;

            Invalidate();
            OnVisibleRangeChanged();
        }

        /// <summary>
        /// Expand collapsed block
        /// </summary>
        /// <param name="iLine">Any line inside collapsed block</param>
        public void ExpandBlock(int iLine)
        {
            if (LineInfos[iLine].VisibleState == VisibleState.Visible)
                return;

            for (int i = iLine; i < LinesCount; i++)
                if (LineInfos[i].VisibleState == VisibleState.Visible)
                    break;
                else
                {
                    SetVisibleState(i, VisibleState.Visible);
                    needRecalc = true;
                }

            for (int i = iLine - 1; i >= 0; i--)
                if (LineInfos[i].VisibleState == VisibleState.Visible)
                    break;
                else
                {
                    SetVisibleState(i, VisibleState.Visible);
                    needRecalc = true;
                }

            Invalidate();
            OnVisibleRangeChanged();
        }

        /// <summary>
        /// Collapses all folding blocks
        /// </summary>
        public virtual void CollapseAllFoldingBlocks()
        {
            for (int i = 0; i < LinesCount; i++)
                if (lines.LineHasFoldingStartMarker(i))
                {
                    int iFinish = FindEndOfFoldingBlock(i);
                    if (iFinish >= 0)
                    {
                        CollapseBlock(i, iFinish);
                        i = iFinish;
                    }
                }

            OnVisibleRangeChanged();
            UpdateScrollbars();
        }

        /// <summary>
        /// Exapnds all folded blocks
        /// </summary>
        /// <param name="iLine"></param>
        public virtual void ExpandAllFoldingBlocks()
        {
            for (int i = 0; i < LinesCount; i++)
                SetVisibleState(i, VisibleState.Visible);

            FoldedBlocks.Clear();

            OnVisibleRangeChanged();
            Invalidate();
            UpdateScrollbars();
        }

        /// <summary>
        /// Collapses folding block
        /// </summary>
        /// <param name="iLine">Start folding line</param>
        public virtual void CollapseFoldingBlock(int iLine)
        {
            if (iLine < 0 || iLine >= lines.Count)
                throw new ArgumentOutOfRangeException("iLine", "Line index out of range");
            if (string.IsNullOrEmpty(lines[iLine].FoldingStartMarker))
                throw new ArgumentOutOfRangeException("iLine", "This line is not folding start line");
            //find end of block
            int i = FindEndOfFoldingBlock(iLine);
            //collapse
            if (i >= 0)
            {
                CollapseBlock(iLine, i);
                var id = this[iLine].UniqueId;
                FoldedBlocks[id] = id; //add folded state for line
            }
        }

        private int FindEndOfFoldingBlock(int iStartLine)
        {
            return Folding.FindEndOfFoldingBlock(lines, iStartLine, int.MaxValue, this.FindEndOfFoldingBlockStrategy);
        }

        /// <summary>
        /// Start foilding marker for the line
        /// </summary>
        public string GetLineFoldingStartMarker(int iLine)
        {
            if (lines.LineHasFoldingStartMarker(iLine))
                return lines[iLine].FoldingStartMarker;
            return null;
        }

        /// <summary>
        /// End foilding marker for the line
        /// </summary>
        public string GetLineFoldingEndMarker(int iLine)
        {
            if (lines.LineHasFoldingEndMarker(iLine))
                return lines[iLine].FoldingEndMarker;
            return null;
        }

        internal virtual void RecalcFoldingLines()
        {
            if (!needRecalcFoldingLines)
                return;
            needRecalcFoldingLines = false;
            if (!ShowFoldingLines)
                return;

            foldingPairs.Clear();
            //
            Range range = VisibleRange;
            int startLine = Math.Max(range.Start.iLine - MAX_LINES_FOR_FOLDING, 0);
            int endLine = Math.Min(range.End.iLine + MAX_LINES_FOR_FOLDING, Math.Max(range.End.iLine, LinesCount - 1));
            var stack = new Stack<int>();
            for (int i = startLine; i <= endLine; i++)
            {
                bool hasStartMarker = lines.LineHasFoldingStartMarker(i);
                bool hasEndMarker = lines.LineHasFoldingEndMarker(i);

                if (hasEndMarker && hasStartMarker)
                    continue;

                if (hasStartMarker)
                {
                    stack.Push(i);
                }
                if (hasEndMarker)
                {
                    string m = lines[i].FoldingEndMarker;
                    while (stack.Count > 0)
                    {
                        int iStartLine = stack.Pop();
                        foldingPairs[iStartLine] = i;
                        if (m == lines[iStartLine].FoldingStartMarker)
                            break;
                    }
                }
            }

            while (stack.Count > 0)
                foldingPairs[stack.Pop()] = endLine + 1;
        }

        /// <summary>
        /// Collapse text block
        /// </summary>
        public virtual void CollapseBlock(int fromLine, int toLine)
        {
            int from = Math.Min(fromLine, toLine);
            int to = Math.Max(fromLine, toLine);
            if (from == to)
                return;

            //find first non empty line
            for (; from <= to; from++)
            {
                if (GetLineText(from).Trim().Length > 0)
                {
                    //hide lines
                    for (int i = from + 1; i <= to; i++)
                        SetVisibleState(i, VisibleState.Hidden);
                    SetVisibleState(from, VisibleState.StartOfHiddenBlock);
                    Invalidate();
                    break;
                }
            }
            //Move caret outside
            from = Math.Min(fromLine, toLine);
            to = Math.Max(fromLine, toLine);
            int newLine = FindNextVisibleLine(to);
            if (newLine == to)
                newLine = FindPrevVisibleLine(from);
            Selection.Start = new Place(0, newLine);
            //
            needRecalc = true;
            Invalidate();
            OnVisibleRangeChanged();
        }


        internal int FindNextVisibleLine(int iLine)
        {
            if (iLine >= lines.Count - 1) return iLine;
            int old = iLine;
            do
                iLine++; while (iLine < lines.Count - 1 && LineInfos[iLine].VisibleState != VisibleState.Visible);

            if (LineInfos[iLine].VisibleState != VisibleState.Visible)
                return old;
            else
                return iLine;
        }


        internal int FindPrevVisibleLine(int iLine)
        {
            if (iLine <= 0) return iLine;
            int old = iLine;
            do
                iLine--; while (iLine > 0 && LineInfos[iLine].VisibleState != VisibleState.Visible);

            if (LineInfos[iLine].VisibleState != VisibleState.Visible)
                return old;
            else
                return iLine;
        }

        private VisualMarker FindVisualMarkerForPoint(Point p)
        {
            foreach (VisualMarker m in visibleMarkers)
                if (m.rectangle.Contains(p))
                    return m;
            return null;
        }

        /// <summary>
        /// Insert TAB into front of seletcted lines.
        /// </summary>
        public void IncreaseIndent()
        {
            if (Selection.Start == Selection.End)
            {
                if (!Selection.ReadOnly)
                {
                    if (this.ConvertTabToSpaces)
                    {
                        //insert tab as spaces
                        int spaces = TabLength - (Selection.Start.iChar % TabLength);
                        //replace mode? select forward chars
                        if (IsReplaceMode)
                        {
                            for (int i = 0; i < spaces; i++)
                                Selection.GoRight(true);
                            Selection.Inverse();
                        }

                        InsertText(new String(' ', spaces));
                    }
                    else
                    {
                        if (IsReplaceMode)
                        {
                            Selection.GoRight(true);
                            Selection.Inverse();
                        }
                        InsertText("\t");
                    }
                }
                return;
            }

            bool carretAtEnd = (Selection.Start > Selection.End) && !Selection.ColumnSelectionMode;

            int startChar = 0; // Only move selection when in 'ColumnSelectionMode'
            if (Selection.ColumnSelectionMode)
                startChar = Math.Min(Selection.End.iChar, Selection.Start.iChar);

            BeginUpdate();
            Selection.BeginUpdate();
            lines.Manager.BeginAutoUndoCommands();

            var old = Selection.Clone();
            lines.Manager.ExecuteCommand(new SelectCommand(TextSource));//remember selection

            //
            Selection.Normalize();
            Range currentSelection = this.Selection.Clone();
            int from = Selection.Start.iLine;
            int to = Selection.End.iLine;

            if (!Selection.ColumnSelectionMode)
                if (Selection.End.iChar == 0) to--;

            for (int i = from; i <= to; i++)
            {
                if (lines[i].Count == 0) continue;
                Selection.Start = new Place(startChar, i);
                lines.Manager.ExecuteCommand(new InsertTextCommand(TextSource, new String(' ', TabLength)));
            }

            // Restore selection
            if (Selection.ColumnSelectionMode == false)
            {
                int newSelectionStartCharacterIndex = currentSelection.Start.iChar + this.TabLength;
                int newSelectionEndCharacterIndex = currentSelection.End.iChar + (currentSelection.End.iLine == to?this.TabLength : 0);
                this.Selection.Start = new Place(newSelectionStartCharacterIndex, currentSelection.Start.iLine);
                this.Selection.End = new Place(newSelectionEndCharacterIndex, currentSelection.End.iLine);
            }
            else
            {
                Selection = old;
            }
            lines.Manager.EndAutoUndoCommands();

            if (carretAtEnd)
                Selection.Inverse();

            needRecalc = true;
            Selection.EndUpdate();
            EndUpdate();
            Invalidate();
        }

        /// <summary>
        /// Remove TAB from front of seletcted lines.
        /// </summary>
        public void DecreaseIndent()
        {
            if (Selection.Start.iLine == Selection.End.iLine)
            {
                DecreaseIndentOfSingleLine();
                return;
            }

            int startCharIndex = 0;
            if (Selection.ColumnSelectionMode)
                startCharIndex = Math.Min(Selection.End.iChar, Selection.Start.iChar);

            BeginUpdate();
            Selection.BeginUpdate();
            lines.Manager.BeginAutoUndoCommands();
            var old = Selection.Clone();
            lines.Manager.ExecuteCommand(new SelectCommand(TextSource));//remember selection

            // Remember current selection infos
            Range currentSelection = this.Selection.Clone();
            Selection.Normalize();
            int from = Selection.Start.iLine;
            int to = Selection.End.iLine;

            if (!Selection.ColumnSelectionMode)
                if (Selection.End.iChar == 0) to--;

            int numberOfDeletedWhitespacesOfFirstLine = 0;
            int numberOfDeletetWhitespacesOfLastLine = 0;

            for (int i = from; i <= to; i++)
            {
                if (startCharIndex > lines[i].Count)
                    continue;
                // Select first characters from the line
                int endIndex = Math.Min(this.lines[i].Count, startCharIndex + this.TabLength);
                string wasteText = this.lines[i].Text.Substring(startCharIndex, endIndex-startCharIndex);

                // Only select the first whitespace characters
                endIndex = Math.Min(endIndex, startCharIndex + wasteText.Length - wasteText.TrimStart().Length);

                // Select the characters to remove
                this.Selection = new Range(this, new Place(startCharIndex, i), new Place(endIndex, i));

                // Remember characters to remove for first and last line
                int numberOfWhitespacesToRemove = endIndex - startCharIndex;
                if (i == currentSelection.Start.iLine)
                {
                    numberOfDeletedWhitespacesOfFirstLine = numberOfWhitespacesToRemove;
                }
                if (i == currentSelection.End.iLine)
                {
                    numberOfDeletetWhitespacesOfLastLine = numberOfWhitespacesToRemove;
                }

                // Remove marked/selected whitespace characters
                if(!Selection.IsEmpty)
                    this.ClearSelected();
            }

            // Restore selection
            if (Selection.ColumnSelectionMode == false)
            {
                int newSelectionStartCharacterIndex = Math.Max(0, currentSelection.Start.iChar - numberOfDeletedWhitespacesOfFirstLine);
                int newSelectionEndCharacterIndex = Math.Max(0, currentSelection.End.iChar - numberOfDeletetWhitespacesOfLastLine);
                this.Selection.Start = new Place(newSelectionStartCharacterIndex, currentSelection.Start.iLine);
                this.Selection.End = new Place(newSelectionEndCharacterIndex, currentSelection.End.iLine);
            }
            else
            {
                Selection = old;
            }
            lines.Manager.EndAutoUndoCommands();

            needRecalc = true;
            Selection.EndUpdate();
            EndUpdate();
            Invalidate();
        }

        /*
        private void DecreaseIndentOfSingleLine()
        {
            var old = Selection.Clone();
            var r = new Range(this, 0, Selection.Start.iLine, Selection.FromX, Selection.Start.iLine);
            foreach (var range in r.GetRanges("\\s{1,"+TabLength+"}", RegexOptions.RightToLeft))
            {
                lines.Manager.BeginAutoUndoCommands();
                lines.Manager.ExecuteCommand(new SelectCommand(TextSource));
                Selection = range;
                ClearSelected();
                lines.Manager.EndAutoUndoCommands();
                var rangeLength = range.End.iChar - range.Start.iChar;
                Selection = new Range(this, old.Start.iChar - rangeLength, old.Start.iLine, old.End.iChar - rangeLength, old.End.iLine);
                break;
            }
        }*/

        /// <summary>
        /// Remove TAB in front of the caret ot the selected line.
        /// </summary>
        private void DecreaseIndentOfSingleLine()
        {
            if (this.Selection.Start.iLine != this.Selection.End.iLine)
                return;

            // Remeber current selection infos
            Range currentSelection = this.Selection.Clone();
            int currentLineIndex = this.Selection.Start.iLine;
            int currentLeftSelectionStartIndex = Math.Min(this.Selection.Start.iChar, this.Selection.End.iChar);

            // Determine number of whitespaces to remove
            string lineText = this.lines[currentLineIndex].Text;
            Match whitespacesLeftOfSelectionStartMatch = new Regex(@"\s*", RegexOptions.RightToLeft).Match(lineText, currentLeftSelectionStartIndex);
            int leftOffset = whitespacesLeftOfSelectionStartMatch.Index;
            int countOfWhitespaces = whitespacesLeftOfSelectionStartMatch.Length;
            int numberOfCharactersToRemove = 0;
            if (countOfWhitespaces > 0)
            {
                int remainder = (this.TabLength > 0)
                    ? currentLeftSelectionStartIndex % this.TabLength
                    : 0;
                numberOfCharactersToRemove = (remainder != 0)
                    ? Math.Min(remainder, countOfWhitespaces)
                    : Math.Min(this.TabLength, countOfWhitespaces);
            }

            // Remove whitespaces if available
            if (numberOfCharactersToRemove > 0)
            {
                // Start selection update
                this.BeginUpdate();
                this.Selection.BeginUpdate();
                lines.Manager.BeginAutoUndoCommands();
                lines.Manager.ExecuteCommand(new SelectCommand(TextSource));//remember selection

                // Remove whitespaces
                this.Selection.Start = new Place(leftOffset, currentLineIndex);
                this.Selection.End = new Place(leftOffset + numberOfCharactersToRemove, currentLineIndex);
                ClearSelected();

                // Restore selection
                int newSelectionStartCharacterIndex = currentSelection.Start.iChar - numberOfCharactersToRemove;
                int newSelectionEndCharacterIndex = currentSelection.End.iChar - numberOfCharactersToRemove;
                this.Selection.Start = new Place(newSelectionStartCharacterIndex, currentLineIndex);
                this.Selection.End = new Place(newSelectionEndCharacterIndex, currentLineIndex);

                lines.Manager.ExecuteCommand(new SelectCommand(TextSource));//remember selection
                // End selection update
                lines.Manager.EndAutoUndoCommands();
                this.Selection.EndUpdate();
                this.EndUpdate();
            }

            Invalidate();
        }


        /// <summary>
        /// Insert autoindents into selected lines
        /// </summary>
        public void DoAutoIndent()
        {
            if (Selection.ColumnSelectionMode)
                return;
            Range r = Selection.Clone();
            r.Normalize();
            //
            BeginUpdate();
            Selection.BeginUpdate();
            lines.Manager.BeginAutoUndoCommands();
            //
            //when the range ends on the first position of a line and the endline isn't the same as the start line
            // then that line won't be included in the auto indent block
            if (r.End.iChar == 0 && r.End.iLine != r.Start.iLine)
            {
                r.End = new Place(lines[r.End.iLine - 1].Count, r.End.iLine - 1);
            }
            for (int i = r.Start.iLine; i <= r.End.iLine; i++)
                DoAutoIndent(i);
            //
            lines.Manager.EndAutoUndoCommands();
            Selection.Start = r.Start;
            Selection.End = r.End;
            Selection.Expand();
            //
            Selection.EndUpdate();
            EndUpdate();
        }

        /// <summary>
        /// Insert prefix into front of seletcted lines
        /// </summary>
        public void InsertLinePrefix(string prefix)
        {
            Range old = Selection.Clone();
            int from = Math.Min(Selection.Start.iLine, Selection.End.iLine);
            int to = Math.Max(Selection.Start.iLine, Selection.End.iLine);
            BeginUpdate();
            Selection.BeginUpdate();
            lines.Manager.BeginAutoUndoCommands();
            lines.Manager.ExecuteCommand(new SelectCommand(TextSource));
            int spaces = GetMinStartSpacesCount(from, to);
            for (int i = from; i <= to; i++)
            {
                Selection.Start = new Place(spaces, i);
                lines.Manager.ExecuteCommand(new InsertTextCommand(TextSource, prefix));
            }
            Selection.Start = new Place(0, from);
            Selection.End = new Place(lines[to].Count, to);
            needRecalc = true;
            lines.Manager.EndAutoUndoCommands();
            Selection.EndUpdate();
            EndUpdate();
            Invalidate();
        }

        /// <summary>
        /// Remove prefix from front of selected lines
        /// This method ignores forward spaces of the line
        /// </summary>
        public void RemoveLinePrefix(string prefix)
        {
            Range old = Selection.Clone();
            int from = Math.Min(Selection.Start.iLine, Selection.End.iLine);
            int to = Math.Max(Selection.Start.iLine, Selection.End.iLine);
            BeginUpdate();
            Selection.BeginUpdate();
            lines.Manager.BeginAutoUndoCommands();
            lines.Manager.ExecuteCommand(new SelectCommand(TextSource));
            for (int i = from; i <= to; i++)
            {
                string text = lines[i].Text;
                string trimmedText = text.TrimStart();
                if (trimmedText.StartsWith(prefix))
                {
                    int spaces = text.Length - trimmedText.Length;
                    Selection.Start = new Place(spaces, i);
                    Selection.End = new Place(spaces + prefix.Length, i);
                    ClearSelected();
                }
            }
            Selection.Start = new Place(0, from);
            Selection.End = new Place(lines[to].Count, to);
            needRecalc = true;
            lines.Manager.EndAutoUndoCommands();
            Selection.EndUpdate();
            EndUpdate();
        }

        /// <summary>
        /// Begins AutoUndo block.
        /// All changes of text between BeginAutoUndo() and EndAutoUndo() will be canceled in one operation Undo.
        /// </summary>
        public void BeginAutoUndo()
        {
            lines.Manager.BeginAutoUndoCommands();
        }

        /// <summary>
        /// Ends AutoUndo block.
        /// All changes of text between BeginAutoUndo() and EndAutoUndo() will be canceled in one operation Undo.
        /// </summary>
        public void EndAutoUndo()
        {
            lines.Manager.EndAutoUndoCommands();
        }

        public virtual void OnVisualMarkerClick(MouseEventArgs args, StyleVisualMarker marker)
        {
            if (VisualMarkerClick != null)
                VisualMarkerClick(this, new VisualMarkerEventArgs(marker.Style, marker, args));
            marker.Style.OnVisualMarkerClick(this, new VisualMarkerEventArgs(marker.Style, marker, args));
        }

        protected virtual void OnMarkerClick(MouseEventArgs args, VisualMarker marker)
        {
            if (marker is StyleVisualMarker)
            {
                OnVisualMarkerClick(args, marker as StyleVisualMarker);
                return;
            }
            if (marker is CollapseFoldingMarker)
            {
                CollapseFoldingBlock((marker as CollapseFoldingMarker).iLine);
                return;
            }

            if (marker is ExpandFoldingMarker)
            {
                ExpandFoldedBlock((marker as ExpandFoldingMarker).iLine);
                return;
            }

            if (marker is FoldedAreaMarker)
            {
                //select folded block
                int iStart = (marker as FoldedAreaMarker).iLine;
                int iEnd = FindEndOfFoldingBlock(iStart);
                if (iEnd < 0)
                    return;
                Selection.BeginUpdate();
                Selection.Start = new Place(0, iStart);
                Selection.End = new Place(lines[iEnd].Count, iEnd);
                Selection.EndUpdate();
                Invalidate();
                return;
            }
        }

        protected virtual void OnMarkerDoubleClick(VisualMarker marker)
        {
            if (marker is FoldedAreaMarker)
            {
                ExpandFoldedBlock((marker as FoldedAreaMarker).iLine);
                Invalidate();
                return;
            }
        }

        private void ClearBracketsPositions()
        {
            leftBracketPosition = null;
            rightBracketPosition = null;
            leftBracketPosition2 = null;
            rightBracketPosition2 = null;
        }



        public virtual void OnSyntaxHighlight(TextChangedEventArgs args)
        {
            #if debug
            Stopwatch sw = Stopwatch.StartNew();
            #endif

            Range range;

            switch (this.HighlightingRangeType)
            {
                case HighlightingRangeType.VisibleRange:
                    range = this.VisibleRange.GetUnionWith(args.ChangedRange);
                    break;
                case HighlightingRangeType.AllTextRange:
                    range = this.Range;
                    break;
                default:
                    range = args.ChangedRange;
                    break;
            }

            if (this.SyntaxHighlighter != null)
            {
                if (Language == Language.Custom && !string.IsNullOrEmpty(DescriptionFile))
                    SyntaxHighlighter.HighlightSyntax(DescriptionFile, range);
                else
                    SyntaxHighlighter.HighlightSyntax(Language, range);
            }

#if debug
            Console.WriteLine("OnSyntaxHighlight: "+ sw.ElapsedMilliseconds);
#endif
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // FastColoredTextBox
            // 
            Name = "FastColoredTextBox";
            ResumeLayout(false);
        }

        internal void CallVisibleRangeHandlers()
        {
            //call handlers for VisibleRange
            if (VisibleRangeChanged != null)
                VisibleRangeChanged(this, new EventArgs());
            if (VisibleRangeChangedDelayed != null)
                VisibleRangeChangedDelayed(this, new EventArgs());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (SyntaxHighlighter != null)
                    SyntaxHighlighter.Dispose();
                delayedEventsTimer.Dispose();
                delayedTextChangedTimer.Dispose();
                middleClickScrollingTimer.Dispose();

                if (findForm != null)
                    findForm.Dispose();

                if (replaceForm != null)
                    replaceForm.Dispose();
                /*
                if (Font != null)
                    Font.Dispose();

                if (originalFont != null)
                    originalFont.Dispose();*/

                if (TextSource != null)
                    TextSource.Dispose();

                if (ToolTip != null)
                    ToolTip.Dispose();
            }
        }

        internal virtual void OnPaintLine(PaintLineEventArgs e)
        {
            if (PaintLine != null)
                PaintLine(this, e);
        }

        internal void OnLineInserted(int index, int count = 1)
        {
            if (LineInserted != null)
                LineInserted(this, new LineInsertedEventArgs(index, count));
        }

        internal void OnLineRemoved(int index, int count, List<int> removedLineIds)
        {
            if (count > 0)
                if (LineRemoved != null)
                    LineRemoved(this, new LineRemovedEventArgs(index, count, removedLineIds));
        }

        /// <summary>
        /// Set VisibleState of line
        /// </summary>
        public void SetVisibleState(int iLine, VisibleState state)
        {
            LineInfo li = LineInfos[iLine];
            li.VisibleState = state;
            LineInfos[iLine] = li;
            needRecalc = true;
        }

        /// <summary>
        /// Returns VisibleState of the line
        /// </summary>
        public VisibleState GetVisibleState(int iLine)
        {
            return LineInfos[iLine].VisibleState;
        }

        /// <summary>
        /// Occurs when undo/redo stack is changed
        /// </summary>
        public void OnUndoRedoStateChanged()
        {
            if (UndoRedoStateChanged != null)
                UndoRedoStateChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Search lines by regex pattern
        /// </summary>
        public List<int> FindLines(string searchPattern, RegexOptions options)
        {
            var iLines = new List<int>();
            foreach (Range r in this.Range.GetRangesByLines(searchPattern, options))
                iLines.Add(r.Start.iLine);

            return iLines;
        }

        /// <summary>
        /// Removes given lines
        /// </summary>
        public void RemoveLines(List<int> iLines)
        {
            TextSource.Manager.ExecuteCommand(new RemoveLinesCommand(TextSource, iLines));
            if (iLines.Count > 0)
                IsChanged = true;
            if (LinesCount == 0)
                Text = "";
            NeedRecalc();
            Invalidate();
        }

        void ISupportInitialize.BeginInit()
        {
            //
        }

        void ISupportInitialize.EndInit()
        {
            OnTextChanged();
            Selection.Start = Place.Empty;
            DoCaretVisible();
            IsChanged = false;
            ClearUndo();
        }

        #region Drag and drop

        internal bool IsDragDrop { get; set; }


        protected override void OnDragEnter(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text) && AllowDrop)
            {
                e.Effect = DragDropEffects.Copy;
                IsDragDrop = true;
            }
            base.OnDragEnter(e);
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            if (ReadOnly || !AllowDrop)
            {
                IsDragDrop = false;
                return;
            }

            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                if (ParentForm != null)
                    ParentForm.Activate();
                Focus();
                Point p = PointToClient(new Point(e.X, e.Y));
                var text = e.Data.GetData(DataFormats.Text).ToString();
                var place = PointToPlace(p);
                DoDragDropHelper.DoDragDrop(this, place, text);
                IsDragDrop = false;
            }
            base.OnDragDrop(e);
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                Point p = PointToClient(new Point(e.X, e.Y));
                Selection.Start = PointToPlace(p);
                if (p.Y < 6 && VerticalScroll.Visible && VerticalScroll.Value > 0)
                {
                    // scroll when near the top
                    this.VerticalScroll.Value = Math.Max(0, VerticalScroll.Value - this.CharHeight);
                }

                DoCaretVisible();
                Invalidate();
            }
            base.OnDragOver(e);
        }

        protected override void OnDragLeave(EventArgs e)
        {
            IsDragDrop = false;
            base.OnDragLeave(e);
        }

        #endregion

        #region MiddleClickScrolling

        private bool middleClickScrollingActivated;
        private Point middleClickScrollingOriginPoint;
        private Point middleClickScrollingOriginScroll;
        private readonly Timer middleClickScrollingTimer = new Timer();
        private ScrollDirection middleClickScollDirection = ScrollDirection.None;

        /// <summary>
        /// Activates the scrolling mode (middle click button).
        /// </summary>
        /// <param name="e">MouseEventArgs</param>
        private void ActivateMiddleClickScrollingMode(MouseEventArgs e)
        {
            if (!middleClickScrollingActivated)
            {
                if ((!HorizontalScroll.Visible) && (!VerticalScroll.Visible))
                if (ShowScrollBars)
                    return;
                middleClickScrollingActivated = true;
                middleClickScrollingOriginPoint = e.Location;
                middleClickScrollingOriginScroll = new Point(HorizontalScroll.Value, VerticalScroll.Value);
                middleClickScrollingTimer.Interval = 50;
                middleClickScrollingTimer.Enabled = true;
                Capture = true;
                // Refresh the control 
                Refresh();
                // Disable drawing
                NativeMethods.SendMessage(Handle, WM_SETREDRAW, 0, 0);
            }
        }

        /// <summary>
        /// Deactivates the scrolling mode (middle click button).
        /// </summary>
        private void DeactivateMiddleClickScrollingMode()
        {
            if (middleClickScrollingActivated)
            {
                middleClickScrollingActivated = false;
                middleClickScrollingTimer.Enabled = false;
                Capture = false;
                base.Cursor = defaultCursor;
                // Enable drawing
                NativeMethods.SendMessage(Handle, WM_SETREDRAW, 1, 0);
                Invalidate();
            }
        }

        /// <summary>
        /// Restore scrolls
        /// </summary>
        private void RestoreScrollsAfterMiddleClickScrollingMode()
        {
            var xea = new ScrollEventArgs(ScrollEventType.ThumbPosition,
                HorizontalScroll.Value,
                middleClickScrollingOriginScroll.X,
                ScrollOrientation.HorizontalScroll);
            OnScroll(xea);

            var yea = new ScrollEventArgs(ScrollEventType.ThumbPosition,
                VerticalScroll.Value,
                middleClickScrollingOriginScroll.Y,
                ScrollOrientation.VerticalScroll);
            OnScroll(yea);
        }


        private const int WM_SETREDRAW = 0xB;

        void middleClickScrollingTimer_Tick(object sender, EventArgs e)
        {
            if (IsDisposed)
                return;

            if (!middleClickScrollingActivated)
                return;

            Point currentMouseLocation = PointToClient(Cursor.Position);

            Capture = true;

            // Calculate angle and distance between current position point and origin point
            int distanceX = this.middleClickScrollingOriginPoint.X - currentMouseLocation.X;
            int distanceY = this.middleClickScrollingOriginPoint.Y - currentMouseLocation.Y;

            if (!VerticalScroll.Visible && ShowScrollBars) distanceY = 0;
            if (!HorizontalScroll.Visible && ShowScrollBars) distanceX = 0;

            double angleInDegree = 180 - Math.Atan2(distanceY, distanceX) * 180 / Math.PI;
            double distance = Math.Sqrt(Math.Pow(distanceX, 2) + Math.Pow(distanceY, 2));

            // determine scrolling direction depending on the angle
            if (distance > 10)
            {
                if (angleInDegree >= 325 || angleInDegree <= 35)
                    this.middleClickScollDirection = ScrollDirection.Right;
                else if (angleInDegree <= 55)
                    this.middleClickScollDirection = ScrollDirection.Right | ScrollDirection.Up;
                else if (angleInDegree <= 125)
                    this.middleClickScollDirection = ScrollDirection.Up;
                else if (angleInDegree <= 145)
                    this.middleClickScollDirection = ScrollDirection.Up | ScrollDirection.Left;
                else if (angleInDegree <= 215)
                    this.middleClickScollDirection = ScrollDirection.Left;
                else if (angleInDegree <= 235)
                    this.middleClickScollDirection = ScrollDirection.Left | ScrollDirection.Down;
                else if (angleInDegree <= 305)
                    this.middleClickScollDirection = ScrollDirection.Down;
                else
                    this.middleClickScollDirection = ScrollDirection.Down | ScrollDirection.Right;
            }
            else
            {
                this.middleClickScollDirection = ScrollDirection.None;
            }

            // Set mouse cursor
            switch (this.middleClickScollDirection)
            {
                case ScrollDirection.Right: base.Cursor = Cursors.PanEast; break;
                case ScrollDirection.Right | ScrollDirection.Up: base.Cursor = Cursors.PanNE; break;
                case ScrollDirection.Up: base.Cursor = Cursors.PanNorth; break;
                case ScrollDirection.Up | ScrollDirection.Left: base.Cursor = Cursors.PanNW; break;
                case ScrollDirection.Left: base.Cursor = Cursors.PanWest; break;
                case ScrollDirection.Left | ScrollDirection.Down: base.Cursor = Cursors.PanSW; break;
                case ScrollDirection.Down: base.Cursor = Cursors.PanSouth; break;
                case ScrollDirection.Down | ScrollDirection.Right: base.Cursor = Cursors.PanSE; break;
                default: base.Cursor = defaultCursor; return;
            }

            var xScrollOffset = (int)(-distanceX / 5.0);
            var yScrollOffset = (int)(-distanceY / 5.0);

            var xea = new ScrollEventArgs(xScrollOffset < 0 ? ScrollEventType.SmallIncrement : ScrollEventType.SmallDecrement,
                HorizontalScroll.Value,
                HorizontalScroll.Value + xScrollOffset,
                ScrollOrientation.HorizontalScroll);

            var yea = new ScrollEventArgs(yScrollOffset < 0 ? ScrollEventType.SmallDecrement : ScrollEventType.SmallIncrement,
                VerticalScroll.Value,
                VerticalScroll.Value + yScrollOffset,
                ScrollOrientation.VerticalScroll);

            if ((middleClickScollDirection & (ScrollDirection.Down | ScrollDirection.Up)) > 0)
                //DoScrollVertical(1 + Math.Abs(yScrollOffset), Math.Sign(distanceY));
                OnScroll(yea, false);

            if ((middleClickScollDirection & (ScrollDirection.Right | ScrollDirection.Left)) > 0)
                OnScroll(xea);

            // Enable drawing
            NativeMethods.SendMessage(Handle, WM_SETREDRAW, 1, 0);
            // Refresh the control 
            Refresh();
            // Disable drawing
            NativeMethods.SendMessage(Handle, WM_SETREDRAW, 0, 0);
        }

        private void DrawMiddleClickScrolling(Graphics gr)
        {
            // If mouse scrolling mode activated draw the scrolling cursor image
            bool ableToScrollVertically = this.VerticalScroll.Visible || !ShowScrollBars;
            bool ableToScrollHorizontally = this.HorizontalScroll.Visible || !ShowScrollBars;

            // Calculate inverse color
            Color inverseColor = Color.FromArgb(100, (byte)~this.BackColor.R, (byte)~this.BackColor.G, (byte)~this.BackColor.B);
            using (SolidBrush inverseColorBrush = new SolidBrush(inverseColor))
            {
                var p = middleClickScrollingOriginPoint;

                var state = gr.Save();

                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.TranslateTransform(p.X, p.Y);
                gr.FillEllipse(inverseColorBrush, -2, -2, 4, 4);

                if (ableToScrollVertically) DrawTriangle(gr, inverseColorBrush);
                gr.RotateTransform(90);
                if (ableToScrollHorizontally) DrawTriangle(gr, inverseColorBrush);
                gr.RotateTransform(90);
                if (ableToScrollVertically) DrawTriangle(gr, inverseColorBrush);
                gr.RotateTransform(90);
                if (ableToScrollHorizontally) DrawTriangle(gr, inverseColorBrush);

                gr.Restore(state);
            }
        }

        private void DrawTriangle(Graphics g, Brush brush)
        {
            const int size = 5;
            var points = new Point[] { new Point(size, 2 * size), new Point(0, 3 * size), new Point(-size, 2 * size) };
            g.FillPolygon(brush, points);
        }

        #endregion


        #region Nested type: LineYComparer

        private class LineYComparer : IComparer<LineInfo>
        {
            private readonly int Y;

            public LineYComparer(int Y)
            {
                this.Y = Y;
            }

            #region IComparer<LineInfo> Members

            public int Compare(LineInfo x, LineInfo y)
            {
                if (x.startY == -10)
                    return -y.startY.CompareTo(Y);
                else
                    return x.startY.CompareTo(Y);
            }

            #endregion
        }

        #endregion
    }

}
