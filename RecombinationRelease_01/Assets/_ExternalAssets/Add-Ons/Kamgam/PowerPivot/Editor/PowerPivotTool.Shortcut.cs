using UnityEngine;
using UnityEditor.ShortcutManagement;

namespace Kamgam.PowerPivot
{
    partial class PowerPivotTool
    {
        [Shortcut("Activate Cursor Tool", /*KEY*/KeyCode.U/*KEY*/, ShortcutModifiers.None)] // This line is changed by the settings.
        public static void PowerPivotToolShortcut()
        {
            Activate(null);
        }
    }
}
