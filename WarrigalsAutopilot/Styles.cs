using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WarrigalsAutopilot
{
    internal static class Styles
    {
        internal static GUIStyle SafeButton { get; private set; }

        static Styles()
        {
            SafeButton = new GUIStyle(GUI.skin.button);
            ForAllStates(SafeButton, state => state.textColor = new Color(0.0f, 1.0f, 0.0f));
        }

        static void ForAllStates(GUIStyle style, Action<GUIStyleState> action)
        {
            action(style.active);
            action(style.focused);
            action(style.hover);
            action(style.normal);
            action(style.onActive);
            action(style.onFocused);
            action(style.onHover);
            action(style.onNormal);
        }
    }
}
