using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace FastColoredTextBoxNS
{
    internal static class WindowsRegistry
    {
        /// <summary>
        /// Gets the value for the system control panel mouse wheel scroll settings.
        /// The value returns the number of lines that shall be scolled if the user turns the mouse wheet one step.
        /// </summary>
        /// <remarks>
        /// This methods gets the "WheelScrollLines" value our from the registry key "HKEY_CURRENT_USER\Control Panel\Desktop".
        /// If the value of this option is 0, the screen will not scroll when the mouse wheel is turned.
        /// If the value of this option is -1 or is greater than the number of lines visible in the window,
        /// the screen will scroll up or down by one page.
        /// </remarks>
        /// <returns>
        /// Number of lines to scrol l when the mouse wheel is turned
        /// </returns>
        internal static int GetControlPanelWheelScrollLinesValue()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", false))
                {
                    return Convert.ToInt32(key.GetValue("WheelScrollLines"));
                }
            }
            catch
            {
                // Use default value
                return 1;
            }
        }
    }
}
