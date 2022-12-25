﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HoverToot;

public enum SimpleKeyCode
{
    None,
    Backspace,
    Delete,
    Tab,
    Return,
    Pause,
    Escape,
    Space,
    Keypad0,
    Keypad1,
    Keypad2,
    Keypad3,
    Keypad4,
    Keypad5,
    Keypad6,
    Keypad7,
    Keypad8,
    Keypad9,
    KeypadPeriod,
    KeypadDivide,
    KeypadMultiply,
    KeypadMinus,
    KeypadPlus,
    KeypadEnter,
    KeypadEquals,
    UpArrow,
    DownArrow,
    RightArrow,
    LeftArrow,
    Insert,
    Home,
    End,
    PageUp,
    PageDown,
    F1,
    F2,
    F3,
    F4,
    F5,
    F6,
    F7,
    F8,
    F9,
    F10,
    F11,
    F12,
    Alpha0,
    Alpha1,
    Alpha2,
    Alpha3,
    Alpha4,
    Alpha5,
    Alpha6,
    Alpha7,
    Alpha8,
    Alpha9,
    Exclaim,
    DoubleQuote,
    Hash,
    Dollar,
    Percent,
    Ampersand,
    Quote,
    LeftParen,
    RightParen,
    Asterisk,
    Plus,
    Comma,
    Minus,
    Period,
    Slash,
    Colon,
    Semicolon,
    Less,
    Equals,
    Greater,
    Question,
    At,
    LeftBracket,
    Backslash,
    RightBracket,
    Caret,
    Underscore,
    BackQuote,
    A,
    B,
    C,
    D,
    E,
    F,
    G,
    H,
    I,
    J,
    K,
    L,
    M,
    N,
    O,
    P,
    Q,
    R,
    S,
    T,
    U,
    V,
    W,
    X,
    Y,
    Z,
    LeftCurlyBracket,
    Pipe,
    RightCurlyBracket,
    Tilde,
    Numlock,
    CapsLock,
    ScrollLock,
    RightShift,
    LeftShift,
    RightControl,
    LeftControl,
    RightAlt,
    LeftAlt,
    LeftCommand,
    LeftApple,
    LeftWindows,
    RightCommand,
    RightApple,
    RightWindows,
    Mouse0,
    Mouse1,
    Mouse2,
    Mouse3,
    Mouse4,
    Mouse5,
    Mouse6
}

public static class SimpleKeycodeMethods
{
    public static KeyCode ToKeyCode(this SimpleKeyCode kc)
    {
        if (Enum.TryParse(kc.ToString(), out KeyCode result))
            return result;
        else
            throw new InvalidOperationException("This should never happen");
    }
}