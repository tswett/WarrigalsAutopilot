// Copyright 2017 by Tanner "Warrigal" Swett.

using System;
using UnityEngine;

namespace WarrigalsAutopilot
{
    static class Odospinner
    {
        public static int Paint(int value, int minValue, int maxValue)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Keyboard);

            Rect position = GUILayoutUtility.GetRect(width: 100, height: 30);
            bool thisHasKeyboardFocus = GUIUtility.keyboardControl == controlID;
            State state = (State)GUIUtility.GetStateObject(typeof(State), controlID);

            GUI.Label(
                position,
                value.ToString(),
                thisHasKeyboardFocus ? Styles.OdoLabelActive : Styles.OdoLabel);

            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (position.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.keyboardControl = controlID;
                        GUIUtility.hotControl = controlID;

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
                            value = Math.Max(value - 1, minValue);
                        }
                        else if (Event.current.keyCode == KeyCode.UpArrow)
                        {
                            value = Math.Min(value + 1, maxValue);
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