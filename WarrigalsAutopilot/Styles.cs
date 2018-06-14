// Copyright 2018 by Tanner "Warrigal" Swett.

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

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
        internal static GUIStyle OdoLabel { get; private set; }
        internal static GUIStyle OdoLabelActive { get; private set; }
        internal static GUIStyle OdoLabelSelectedDigit { get; private set; }

        static Styles()
        {
            SafeButton = new GUIStyle(GUI.skin.button);
            ForAllStates(SafeButton, state => state.textColor = new Color(0.0f, 1.0f, 0.0f));

            OdoLabel = new GUIStyle(GUI.skin.label);
            OdoLabel.fontSize = 20;

            OdoLabelActive = new GUIStyle(OdoLabel);
            OdoLabelActive.fontStyle = FontStyle.Bold;
            ForAllStates(OdoLabelActive, state =>
            {
                state.textColor = new Color(1.0f, 0.0f, 1.0f); // magenta
            });

            OdoLabelSelectedDigit = new GUIStyle(OdoLabelActive);
            ForAllStates(OdoLabelSelectedDigit, state =>
            {
                state.textColor = new Color(0.0f, 1.0f, 1.0f); // cyan
            });
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
