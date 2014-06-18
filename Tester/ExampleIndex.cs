using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tester
{
    public static class ExampleIndex
    {

        public static List<ExampleItem> Examples = new List<ExampleItem>()
            {
                new ExampleItem(typeof(PowerfulSample),"Powerful sample. It shows syntax highlighting and many features."),
                new ExampleItem(typeof(MarkerToolSample),"Marker sample. It shows how to make marker tool."),
                new ExampleItem(typeof(CustomStyleSample),"Custom style sample. This example shows how to create own custom style."),
                new ExampleItem(typeof(VisibleRangeChangedDelayedSample),"VisibleRangeChangedDelayed usage sample. This example shows how to highlight synt" +
    "ax for extremally large text by VisibleRangeChangedDelayed event."), // FIXME
                new ExampleItem(typeof(SimplestSyntaxHighlightingSample),"Simplest custom syntax highlighting sample. It shows how to make custom syntax hi" +
    "ghlighting."),
                new ExampleItem(typeof(JokeSample),"Joke sample :)"),
                new ExampleItem(typeof(SimplestCodeFoldingSample),"Simplest code folding sample. This example shows how to make simplest code foldin" +
    "g."),
                new ExampleItem(typeof(AutocompleteSample),"Autocomplete sample. This example shows simplest way to create autocomplete functionality."),
                new ExampleItem(typeof(DynamicSyntaxHighlighting),"Dynamic syntax highlighting. This example finds the functions declared in the pro" +
    "gram and dynamically highlights all of their entry into the code of LISP."),
                new ExampleItem(typeof(SyntaxHighlightingByXmlDescription),"Syntax highlighting by XML description file. This example shows how to use XML fi" +
    "le for description of syntax highlighting."),
                new ExampleItem(typeof(IMEsample),"This example supports IME entering mode and rendering of wide characters."),
                new ExampleItem(typeof(PowerfulCSharpEditor),"Powerfull C# source file editor"),
                new ExampleItem(typeof(GifImageDrawingSample),"Example of image drawing"),
                new ExampleItem(typeof(AutocompleteSample2),"Autocomplete sample 2.\r\nThis example demonstrates more flexible variant of Autoco" +
    "mpleteMenu using."),
                new ExampleItem(typeof(AutoIndentSample),"AutoIndent sample"),
                new ExampleItem(typeof(BookmarksSample),"Bookmarks sample"),
                new ExampleItem(typeof(LoggerSample),"Logger sample. It shows how to add text with predefined style."),
                new ExampleItem(typeof(TooltipSample),"Tooltip sample."),
                new ExampleItem(typeof(SplitSample),"Split sample. This example shows how to make split-screen mode."),
                new ExampleItem(typeof(LazyLoadingSample),"Lazy loading sample."),
                new ExampleItem(typeof(ConsoleSample),"Console sample"),
                new ExampleItem(typeof(CustomFoldingSample),"Custom code folding sample."),
                new ExampleItem(typeof(BilingualHighlighterSample),"Bilingual highlighter sample"),
                new ExampleItem(typeof(HyperlinkSample),"Hyperlink sample"),
                new ExampleItem(typeof(CustomTextSourceSample),"Custom TextSource sample. This example shows how to display very large string\r\n"),
                new ExampleItem(typeof(HintSample),"Hints sample"),
                new ExampleItem(typeof(ReadOnlyBlocksSample),"ReadOnly blocks sample. Are you needed readonly blocks of text? Yep, we can do it" +
    "..."),
                new ExampleItem(typeof(PredefinedStylesSample),"Predefined styles sample. Here we create large text with predefined styles, hyper" +
    "links and tooltips..."),
                new ExampleItem(typeof(MacrosSample),"This sample shows how to use macros for hard formatting of the code."),
                new ExampleItem(typeof(OpenTypeFontSample),"How to use OpenType fonts."),
                new ExampleItem(typeof(Sandbox),"Sandbox"),
                new ExampleItem(typeof(RulerSample),"Ruler sample."),
                new ExampleItem(typeof(AutocompleteSample3),"Autocomplete sample 3.\r\n How to make dynamic autocomplete menu."),
                new ExampleItem(typeof(AutocompleteSample4),"Autocomplete sample 4.\r\nHow to make intellisense menu with predefined list of cla" +
    "sses and methods."),
                new ExampleItem(typeof(DocumentMapSample),"Document map sample."),
                new ExampleItem(typeof(DiffMergeSample),"DiffMerge sample."),
                new ExampleItem(typeof(CustomScrollBarsSample),"Custom scrollbars sample."),
                new ExampleItem(typeof(CustomWordWrapSample),"Custom wordwrap sample."),
                new ExampleItem(typeof(TabStopsInText),"Tab stop characters"),
                new ExampleItem(typeof(BraceComplete),"Brace Completion"),
                new ExampleItem(typeof(EndOfLineSample),"Line endings")

            };

        public class ExampleItem
        {
            public Type FormType { get; set; }
            public string Description { get; set; }

            public ExampleItem(Type formType, string description)
            {
                this.FormType = formType;
                this.Description = description;
            }
        }
    }


}
