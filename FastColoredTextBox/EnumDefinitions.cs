using System;
using System.Collections.Generic;
using System.Text;

namespace FastColoredTextBoxNS
{
    /// <summary>
    /// Type of highlighting
    /// </summary>
    public enum HighlightingRangeType
    {
        /// <summary>
        /// Highlight only changed range of text. Highest performance.
        /// </summary>
        ChangedRange,

        /// <summary>
        /// Highlight visible range of text. Middle performance.
        /// </summary>
        VisibleRange,

        /// <summary>
        /// Highlight all (visible and invisible) text. Lowest performance.
        /// </summary>
        AllTextRange
    }

    /// <summary>
    /// Strategy of search of end of folding block
    /// </summary>
    public enum FindEndOfFoldingBlockStrategy
    {
        Strategy1,
        Strategy2
    }

    /// <summary>
    /// Strategy of search of brackets to highlighting
    /// </summary>
    public enum BracketsHighlightStrategy
    {
        Strategy1,
        Strategy2
    }







    public enum TextAreaBorderType
    {
        None,
        Single,
        Shadow
    }

    [Flags]
    public enum ScrollDirection : ushort
    {
        None = 0,
        Left = 1,
        Right = 2,
        Up = 4,
        Down = 8
    }

#if Styles32
    /// <summary>
    /// Style index mask (32 styles)
    /// </summary>
    [Flags]
    public enum StyleIndex : uint
    {
        None = 0,
        Style0 = 0x1,
        Style1 = 0x2,
        Style2 = 0x4,
        Style3 = 0x8,
        Style4 = 0x10,
        Style5 = 0x20,
        Style6 = 0x40,
        Style7 = 0x80,
        Style8 = 0x100,
        Style9 = 0x200,
        Style10 = 0x400,
        Style11 = 0x800,
        Style12 = 0x1000,
        Style13 = 0x2000,
        Style14 = 0x4000,
        Style15 = 0x8000,

        Style16 = 0x10000,
        Style17 = 0x20000,
        Style18 = 0x40000,
        Style19 = 0x80000,
        Style20 = 0x100000,
        Style21 = 0x200000,
        Style22 = 0x400000,
        Style23 = 0x800000,
        Style24 = 0x1000000,
        Style25 = 0x2000000,
        Style26 = 0x4000000,
        Style27 = 0x8000000,
        Style28 = 0x10000000,
        Style29 = 0x20000000,
        Style30 = 0x40000000,
        Style31 = 0x80000000,

        All = 0xffffffff
    }
#else
    /// <summary>
    /// Style index mask (16 styles)
    /// </summary>
    [Flags]
    public enum StyleIndex : ushort
    {
        None = 0,
        Style0 = 0x1,
        Style1 = 0x2,
        Style2 = 0x4,
        Style3 = 0x8,
        Style4 = 0x10,
        Style5 = 0x20,
        Style6 = 0x40,
        Style7 = 0x80,
        Style8 = 0x100,
        Style9 = 0x200,
        Style10 = 0x400,
        Style11 = 0x800,
        Style12 = 0x1000,
        Style13 = 0x2000,
        Style14 = 0x4000,
        Style15 = 0x8000,
        All = 0xffff
    }
#endif

    public enum WordWrapMode
    {
        /// <summary>
        /// Word wrapping by control width
        /// </summary>
        WordWrapControlWidth,

        /// <summary>
        /// Word wrapping by preferred line width (PreferredLineWidth)
        /// </summary>
        WordWrapPreferredWidth,

        /// <summary>
        /// Char wrapping by control width
        /// </summary>
        CharWrapControlWidth,

        /// <summary>
        /// Char wrapping by preferred line width (PreferredLineWidth)
        /// </summary>
        CharWrapPreferredWidth,

        /// <summary>
        /// Custom wrap (by event WordWrapNeeded)
        /// </summary>
        Custom
    }
}
