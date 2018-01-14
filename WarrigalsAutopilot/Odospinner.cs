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
using UnityEngine;

namespace WarrigalsAutopilot
{
    static class Odospinner
    {
        public static int Paint(int value, int minValue, int maxValue, bool wrapAround = false)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Keyboard);

            Rect position = GUILayoutUtility.GetRect(width: 100, height: 30);
            bool thisHasKeyboardFocus = GUIUtility.keyboardControl == controlID;

            GUI.Label(
                position,
                value.ToString(),
                thisHasKeyboardFocus ? Styles.OdoLabelActive : Styles.OdoLabel);

            value = HandleEvent(value, controlID, position, thisHasKeyboardFocus);

            if (GUIUtility.keyboardControl == controlID)
            {
                InputLockManager.SetControlLock(
                    ControlTypes.CAMERACONTROLS, $"WarrigalsAutopilot_CameraLock_{controlID}");
            }
            else
            {
                InputLockManager.RemoveControlLock($"WarrigalsAutopilot_CameraLock_{controlID}");
            }

            if (wrapAround)
            {
                if (value > maxValue) value = minValue;
                if (value < minValue) value = maxValue;
            }
            else
            {
                if (value > maxValue) value = maxValue;
                if (value < minValue) value = minValue;
            }

            return value;
        }

        static int HandleEvent(int value, int controlID, Rect position, bool thisHasKeyboardFocus)
        {
            State state = (State)GUIUtility.GetStateObject(typeof(State), controlID);

            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (position.Contains(Event.current.mousePosition))
                    {
                        Debug.Log("WAP: Odospinner was clicked");
                        GUIUtility.keyboardControl = controlID;
                        GUIUtility.hotControl = controlID;

                        Event.current.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                    }
                    state.MouseDistance = 0;
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        Debug.Log("WAP: Odospinner was dragged");
                        state.MouseDistance -= Event.current.delta.y / 10.0f;
                        value += Mathf.RoundToInt(state.MouseDistance);
                        state.MouseDistance -= Mathf.Round(state.MouseDistance);
                    }
                    break;
                case EventType.KeyDown:
                    if (thisHasKeyboardFocus)
                    {
                        if (Event.current.keyCode == KeyCode.DownArrow)
                        {
                            value -= 1;
                        }
                        else if (Event.current.keyCode == KeyCode.UpArrow)
                        {
                            value += 1;
                        }

                        Event.current.Use();
                    }
                    break;
            }

            return value;
        }

        class State
        {
            public float MouseDistance { get; set; }
        }
    }
}