using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace FastColoredTextBoxNS
{
    public enum EolFormat
    {
        // None is default: we set the EolFormat when creating a line.
        // The last line has EolFormat.None when the file does not end with a new line.
        None, 

        [Description("Unix (LF)")]
        LF,

        [Description("Dos (CR/LF)")]
        CRLF,

        [Description("Mac (CR)")]
        CR,

    }
}
