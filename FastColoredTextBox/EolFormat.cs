using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace FastColoredTextBoxNS
{
    public enum EolFormat
    {
        None, // None is default: we set the EolFormat when 

        [Description("Unix (LF)")]
        LF,

        [Description("Dos (CR/LF)")]
        CRLF,

        [Description("Mac (CR)")]
        CR,

    }
}
