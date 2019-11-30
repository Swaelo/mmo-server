// ================================================================================================================================
// File:        Controls.cs
// Description: Taken from the bepu demos
// Author:      Bepu Entertainment https://www.github.com/bepu/
// ================================================================================================================================

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ServerUtilities;
using OpenTK.Input;

namespace Server.Logic
{
    /// <summary>
    /// Caches strings for enum values to avoid enum boxing.
    /// </summary>
    static class ControlStrings
    {
        static Dictionary<Key, string> keys;
        static Dictionary<MouseButton, string> mouseButtons;
        static Dictionary<MouseWheelAction, string> mouseWheel;

        public static string GetName(Key key)
        {
            return keys[key];
        }
        public static string GetName(MouseButton button)
        {
            return mouseButtons[button];
        }
        public static string GetName(MouseWheelAction wheelAction)
        {
            return mouseWheel[wheelAction];
        }

        static ControlStrings()
        {
            keys = new Dictionary<Key, string>();
            var keyNames = Enum.GetNames(typeof(Key));
            var keyValues = (Key[])Enum.GetValues(typeof(Key));
            for (int i = 0; i < keyNames.Length; ++i)
            {
                keys.TryAdd(keyValues[i], keyNames[i]);
            }
            mouseButtons = new Dictionary<MouseButton, string>();
            var mouseButtonNames = Enum.GetNames(typeof(MouseButton));
            var mouseButtonValues = (MouseButton[])Enum.GetValues(typeof(MouseButton));
            for (int i = 0; i < mouseButtonNames.Length; ++i)
            {
                mouseButtons.TryAdd(mouseButtonValues[i], mouseButtonNames[i]);
            }
            mouseWheel = new Dictionary<MouseWheelAction, string>();
            var wheelNames = Enum.GetNames(typeof(MouseWheelAction));
            var wheelValues = (MouseWheelAction[])Enum.GetValues(typeof(MouseWheelAction));
            for (int i = 0; i < wheelNames.Length; ++i)
            {
                mouseWheel.TryAdd(wheelValues[i], wheelNames[i]);
            }
        }
    }


    public enum HoldableControlType
    {
        Key,
        MouseButton,
    }
    /// <summary>
    /// A control binding which can be held for multiple frames.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct HoldableBind
    {
        [FieldOffset(0)]
        public Key Key;
        [FieldOffset(0)]
        public MouseButton Button;
        [FieldOffset(4)]
        public HoldableControlType Type;

        public HoldableBind(Key key)
            : this()
        {
            Key = key;
            Type = HoldableControlType.Key;
        }
        public HoldableBind(MouseButton button)
            : this()
        {
            Button = button;
            Type = HoldableControlType.MouseButton;
        }

        public static implicit operator HoldableBind(Key key)
        {
            return new HoldableBind(key);
        }
        public static implicit operator HoldableBind(MouseButton button)
        {
            return new HoldableBind(button);
        }
        public bool IsDown(Input input)
        {
            if (Type == HoldableControlType.Key)
                return input.IsDown(Key);
            return input.IsDown(Button);
        }

        public bool WasPushed(Input input)
        {
            if (Type == HoldableControlType.Key)
                return input.WasPushed(Key);
            return input.WasPushed(Button);
        }

        public override string ToString()
        {
            if (Type == HoldableControlType.Key)
                return ControlStrings.GetName(Key);
            return ControlStrings.GetName(Button);
        }

    }

    public enum InstantControlType
    {
        Key,
        MouseButton,
        MouseWheel,
    }
    public enum MouseWheelAction
    {
        ScrollUp,
        ScrollDown
    }
    /// <summary>
    /// A control binding that supports any form of instant action, but may or may not support being held.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct InstantBind
    {
        [FieldOffset(0)]
        public Key Key;
        [FieldOffset(0)]
        public MouseButton Button;
        [FieldOffset(0)]
        public MouseWheelAction Wheel;
        [FieldOffset(4)]
        public InstantControlType Type;

        public InstantBind(Key key)
            : this()
        {
            Key = key;
            Type = InstantControlType.Key;
        }
        public InstantBind(MouseButton button)
            : this()
        {
            Button = button;
            Type = InstantControlType.MouseButton;
        }
        public InstantBind(MouseWheelAction wheelAction)
            : this()
        {
            Wheel = wheelAction;
            Type = InstantControlType.MouseWheel;
        }

        public static implicit operator InstantBind(Key key)
        {
            return new InstantBind(key);
        }
        public static implicit operator InstantBind(MouseButton button)
        {
            return new InstantBind(button);
        }
        public static implicit operator InstantBind(MouseWheelAction wheelAction)
        {
            return new InstantBind(wheelAction);
        }

        public bool WasTriggered(Input input)
        {
            switch (Type)
            {
                case InstantControlType.Key:
                    return input.WasPushed(Key);
                case InstantControlType.MouseButton:
                    return input.WasPushed(Button);
                case InstantControlType.MouseWheel:
                    return Wheel == MouseWheelAction.ScrollUp ? input.ScrolledUp > 0 : input.ScrolledDown < 0;
            }
            return false;
        }

        public override string ToString()
        {
            switch (Type)
            {
                case InstantControlType.Key:
                    return ControlStrings.GetName(Key);
                case InstantControlType.MouseButton:
                    return ControlStrings.GetName(Button);
                case InstantControlType.MouseWheel:
                    return ControlStrings.GetName(Wheel);
            }
            return "";
        }
    }

    public struct InputControls
    {
        public HoldableBind LShift;
        public HoldableBind RShift;

        public HoldableBind BackSpace;
        public InstantBind Enter;
        public InstantBind Space;
        
        public InstantBind Comma;
        public InstantBind Period;
        public InstantBind Minus;

        public InstantBind Q;
        public InstantBind W;
        public InstantBind E;
        public InstantBind R;
        public InstantBind T;
        public InstantBind Y;
        public InstantBind U;
        public InstantBind I;
        public InstantBind O;
        public InstantBind P;
        public InstantBind A;
        public InstantBind S;
        public InstantBind D;
        public InstantBind F;
        public InstantBind G;
        public InstantBind H;
        public InstantBind J;
        public InstantBind K;
        public InstantBind L;
        public InstantBind Z;
        public InstantBind X;
        public InstantBind C;
        public InstantBind V;
        public InstantBind B;
        public InstantBind N;
        public InstantBind M;

        public InstantBind Zero;
        public InstantBind One;
        public InstantBind Two;
        public InstantBind Three;
        public InstantBind Four;
        public InstantBind Five;
        public InstantBind Six;
        public InstantBind Seven;
        public InstantBind Eight;
        public InstantBind Nine;
        public InstantBind Ten;

        public static InputControls Default
        {
            get
            {
                return new InputControls
                {
                    LShift = Key.ShiftLeft,
                    RShift = Key.ShiftRight,

                    BackSpace = Key.BackSpace,
                    Enter = Key.Enter,
                    Space = Key.Space,

                    Comma = Key.Comma,
                    Period = Key.Period,
                    Minus = Key.Minus,

                    Q = Key.Q,
                    W = Key.W,
                    E = Key.E,
                    R = Key.R,
                    T = Key.T,
                    Y = Key.Y,
                    U = Key.U,
                    I = Key.I,
                    O = Key.O,
                    P = Key.P,
                    A = Key.A,
                    S = Key.S,
                    D = Key.D,
                    F = Key.F,
                    G = Key.G,
                    H = Key.H,
                    J = Key.J,
                    K = Key.K,
                    L = Key.L,
                    Z = Key.Z,
                    X = Key.X,
                    C = Key.C,
                    V = Key.V,
                    B = Key.B,
                    N = Key.N,
                    M = Key.M,

                    Zero = Key.Number0,
                    One = Key.Number1,
                    Two = Key.Number2,
                    Three = Key.Number3,
                    Four = Key.Number4,
                    Five = Key.Number5,
                    Six = Key.Number6,
                    Seven = Key.Number7,
                    Eight = Key.Number8,
                    Nine = Key.Number9,
                };
            }
        }
    }

    public struct Controls
    {
        public HoldableBind MoveForward;
        public HoldableBind MoveBackward;
        public HoldableBind MoveLeft;
        public HoldableBind MoveRight;
        public HoldableBind MoveUp;
        public HoldableBind MoveDown;
        public InstantBind MoveSlower;
        public InstantBind MoveFaster;
        public HoldableBind Grab;
        public HoldableBind GrabRotate;
        public float MouseSensitivity;
        public float CameraSlowMoveSpeed;
        public float CameraMoveSpeed;
        public float CameraFastMoveSpeed;

        public HoldableBind SlowTimesteps;
        public InstantBind LockMouse;
        public InstantBind Exit;
        public InstantBind ShowConstraints;
        public InstantBind ShowContacts;
        public InstantBind ShowBoundingBoxes;
        public InstantBind ChangeTimingDisplayMode;
        public InstantBind ChangeScene;
        public InstantBind ShowControls;

        public static Controls Default
        {
            get
            {
                return new Controls
                {
                    MoveForward = Key.W,
                    MoveBackward = Key.S,
                    MoveLeft = Key.A,
                    MoveRight = Key.D,
                    MoveDown = Key.ControlLeft,
                    MoveUp = Key.ShiftLeft,
                    MoveSlower = MouseWheelAction.ScrollDown,
                    MoveFaster = MouseWheelAction.ScrollUp,
                    Grab = MouseButton.Right,
                    GrabRotate = Key.Q,
                    MouseSensitivity = 3e-3f,
                    CameraSlowMoveSpeed = 0.5f,
                    CameraMoveSpeed = 5,
                    CameraFastMoveSpeed = 50,
                    SlowTimesteps = MouseButton.Middle,

                    LockMouse = Key.Tab,
                    Exit = Key.Escape,
                    ShowConstraints = Key.J,
                    ShowContacts = Key.K,
                    ShowBoundingBoxes = Key.L,
                    ChangeTimingDisplayMode = Key.F2,
                    ChangeScene = Key.Tilde,
                    ShowControls = Key.F1,
                };
            }

        }
    }
}
