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
    /// <summary>
    /// A static class for an "odospinner" control, a custom spinner-like
    /// control which supports "spinning" individual digits.
    /// </summary>
    static class Odospinner
    {
        /// <summary>
        /// Paint an odospinner control. This method can only be used as part
        /// of a Unity IMGUI user interface; it behaves the same way as methods
        /// such as <code>GUIUtility.Button</code>.
        /// </summary>
        public static int Paint(int value, int minValue, int maxValue, bool wrapAround = false)
        {
            // Get the control ID and other information.
            int controlID = GUIUtility.GetControlID(FocusType.Keyboard);

            State state = (State)GUIUtility.GetStateObject(typeof(State), controlID);

            Rect position = GUILayoutUtility.GetRect(width: 100, height: 30);
            bool thisHasKeyboardFocus = GUIUtility.keyboardControl == controlID;

            PaintLabel(value, position, thisHasKeyboardFocus, state.SelectedDigit);

            value = HandleEvent(value, controlID, position, thisHasKeyboardFocus);

            LockCameraIfFocused(controlID);

            value = AdjustValueToRange(value, minValue, maxValue, wrapAround);

            return value;
        }

        static void PaintLabel(
            int value, Rect position, bool thisHasKeyboardFocus, int selectedDigit)
        {
            string valueString = value.ToString().PadLeft(5);

            int digitIndex = 0;
            foreach (char digit in valueString)
            {
                Rect digitPosition = new Rect(
                    position.x + 12.0f * digitIndex, position.y, 25.0f, position.height);

                GUIStyle style =
                    thisHasKeyboardFocus
                    ? (selectedDigit == digitIndex
                       ? Styles.OdoLabelSelectedDigit
                       : Styles.OdoLabelActive)
                    : Styles.OdoLabel;

                char displayDigit = digit;
                if (thisHasKeyboardFocus && selectedDigit == digitIndex && digit == ' ')
                {
                    displayDigit = '0';
                }

                GUI.Label(digitPosition, displayDigit.ToString(), style);

                digitIndex++;
            }
        }

        static int HandleEvent(int value, int controlID, Rect position, bool thisHasKeyboardFocus)
        {
            State state = (State)GUIUtility.GetStateObject(typeof(State), controlID);
            int increment = (new[] { 10000, 1000, 100, 10, 1 })[state.SelectedDigit];

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
                        value += increment * Mathf.RoundToInt(state.MouseDistance);
                        state.MouseDistance -= Mathf.Round(state.MouseDistance);
                    }
                    break;
                case EventType.KeyDown:
                    if (thisHasKeyboardFocus)
                    {
                        if (Event.current.keyCode == KeyCode.DownArrow)
                        {
                            value -= increment;
                        }
                        else if (Event.current.keyCode == KeyCode.UpArrow)
                        {
                            value += increment;
                        }
                        else if (Event.current.keyCode == KeyCode.LeftArrow ||
                                 Event.current.keyCode == KeyCode.Backspace)
                        {
                            state.SelectedDigit = Math.Max(state.SelectedDigit - 1, 0);
                        }
                        else if (Event.current.keyCode == KeyCode.RightArrow)
                        {
                            state.SelectedDigit = Math.Min(state.SelectedDigit + 1, 4);
                        }
                        else if (NumericValueOf(Event.current.keyCode) != null)
                        {
                            char[] settingDigits = value.ToString().PadLeft(5).ToCharArray();
                            int newDigitValue = NumericValueOf(Event.current.keyCode).Value;
                            settingDigits[state.SelectedDigit] = newDigitValue.ToString()[0];
                            value = int.Parse(new string(settingDigits));

                            state.SelectedDigit = Math.Min(state.SelectedDigit + 1, 4);
                        }

                        Event.current.Use();
                    }
                    break;
            }

            return value;
        }

        static int? NumericValueOf(KeyCode code)
        {
            if (code >= KeyCode.Alpha0 && code <= KeyCode.Alpha9)
            {
                return (int)code - (int)KeyCode.Alpha0;
            }
            else if (code >= KeyCode.Keypad0 && code <= KeyCode.Keypad9)
            {
                return (int)code - (int)KeyCode.Keypad0;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// <para>Lock the camera if this control has keyboard focus.</para>
        /// 
        /// <para>The purpose of this is to make it so that if the player clicks this control and
        /// then presses an arrow key in order to interact with the control, then the key press does
        /// NOT move the camera in addition to interacting with the control.</para>
        /// 
        /// <para>This is a bad way to do this, but I'm not aware of any other way.</para>
        /// </summary>
        static void LockCameraIfFocused(int controlID)
        {
            if (GUIUtility.keyboardControl == controlID)
            {
                InputLockManager.SetControlLock(
                    ControlTypes.CAMERACONTROLS, $"WarrigalsAutopilot_CameraLock_{controlID}");
            }
            else
            {
                InputLockManager.RemoveControlLock($"WarrigalsAutopilot_CameraLock_{controlID}");
            }
        }

        static int AdjustValueToRange(int value, int minValue, int maxValue, bool wrapAround)
        {
            if (wrapAround)
            {
                int range = maxValue - minValue + 1;

                value = value - minValue;
                value = (value % range + range) % range;
                value = value + minValue;
            }
            else
            {
                if (value > maxValue) value = maxValue;
                if (value < minValue) value = minValue;
            }

            return value;
        }

        class State
        {
            public float MouseDistance { get; set; }
            public int SelectedDigit { get; set; } = 4;
        }
    }
}