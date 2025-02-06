namespace Native.Windows

open System

/// <summary>
/// Represents a virtual key code.
/// </summary>
type KeyCode =
    // | Reserved1 = 0x07
    // | Reserved2 = 0x0A-0B
    // | Unassigned = 0x0E-0F
    // | Undefined = 0x3A-40
    // - 	0x5E 	Reserved
    // - 	0x88-8F 	Reserved
    // - 	0x92-96 	OEM specific
    // - 	0x97-9F 	Unassigned
    // - 	0xB8-B9 	Reserved
    // - 	0xC1-DA 	Reserved
    // - 	0xE0 	Reserved
    // - 	0xE1 	OEM specific
    // - 	0xE3-E4 	OEM specific
    // - 	0xE6 	OEM specific
    // - 	0xE8 	Unassigned
    // - 	0xE9-F5 	OEM specific

    /// Left mouse button
    | VkLbutton = 0x01us
    /// Right mouse button
    | VkRbutton = 0x02us
    /// Control-break processing
    | VkCancel = 0x03us
    /// Middle mouse button
    | VkMbutton = 0x04us
    /// X1 mouse button
    | VkXbutton1 = 0x05us
    /// X2 mouse button
    | VkXbutton2 = 0x06us
    /// BACKSPACE key
    | VkBack = 0x08us
    /// TAB key
    | VkTab = 0x09us
    /// CLEAR key
    | VkClear = 0x0Cus
    /// ENTER key
    | VkReturn = 0x0Dus
    /// SHIFT key
    | VkShift = 0x10us
    /// CTRL key
    | VkControl = 0x11us
    /// ALT key
    | VkMenu = 0x12us
    /// PAUSE key
    | VkPause = 0x13us
    /// CAPS LOCK key
    | VkCapital = 0x14us
    /// IME Kana mode
    | VkKana = 0x15us
    /// IME Hangul mode
    | VkHangul = 0x15us
    /// IME On
    | VkImeOn = 0x16us
    /// IME Junja mode
    | VkJunja = 0x17us
    /// IME final mode
    | VkFinal = 0x18us
    /// IME Hanja mode
    | VkHanja = 0x19us
    /// IME Kanji mode
    | VkKanji = 0x19us
    /// IME Off
    | VkImeOff = 0x1Aus
    /// ESC key
    | VkEscape = 0x1Bus
    /// IME convert
    | VkConvert = 0x1Cus
    /// IME nonconvert
    | VkNonconvert = 0x1Dus
    /// IME accept
    | VkAccept = 0x1Eus
    /// IME mode change request
    | VkModechange = 0x1Fus
    /// SPACEBAR
    | VkSpace = 0x20us
    /// PAGE UP key
    | VkPrior = 0x21us
    /// PAGE DOWN key
    | VkNext = 0x22us
    /// END key
    | VkEnd = 0x23us
    /// HOME key
    | VkHome = 0x24us
    /// LEFT ARROW key
    | VkLeft = 0x25us
    /// UP ARROW key
    | VkUp = 0x26us
    /// RIGHT ARROW key
    | VkRight = 0x27us
    /// DOWN ARROW key
    | VkDown = 0x28us
    /// SELECT key
    | VkSelect = 0x29us
    /// PRINT key
    | VkPrint = 0x2Aus
    /// EXECUTE key
    | VkExecute = 0x2Bus
    /// PRINT SCREEN key
    | VkSnapshot = 0x2Cus
    /// INS key
    | VkInsert = 0x2Dus
    /// DEL key
    | VkDelete = 0x2Eus
    /// HELP key
    | VkHelp = 0x2Fus
    /// 0 key
    | Vc0Key = 0x30us
    /// 1 key
    | Vc1Key = 0x31us
    /// 2 key
    | Vc2Key = 0x32us
    /// 3 key
    | Vc3Key = 0x33us
    /// 4 key
    | Vc4Key = 0x34us
    /// 5 key
    | Vc5Key = 0x35us
    /// 6 key
    | Vc6Key = 0x36us
    /// 7 key
    | Vc7Key = 0x37us
    /// 8 key
    | Vc8Key = 0x38us
    /// 9 key
    | Vc9Key = 0x39us
    /// A key
    | VcAKey = 0x41us
    /// B key
    | VcBKey = 0x42us
    /// C key
    | VcCKey = 0x43us
    /// D key
    | VcDKey = 0x44us
    /// E key
    | VcEKey = 0x45us
    /// F key
    | VcFKey = 0x46us
    /// G key
    | VcGKey = 0x47us
    /// H key
    | VcHKey = 0x48us
    /// I key
    | VcIKey = 0x49us
    /// J key
    | VcJKey = 0x4Aus
    /// K key
    | VcKKey = 0x4Bus
    /// L key
    | VcLKey = 0x4Cus
    /// M key
    | VcMKey = 0x4Dus
    /// N key
    | VcNKey = 0x4Eus
    /// O key
    | VcOKey = 0x4Fus
    /// P key
    | VcPKey = 0x50us
    /// Q key
    | VcQKey = 0x51us
    /// R key
    | VcRKey = 0x52us
    /// S key
    | VcSKey = 0x53us
    /// T key
    | VcTKey = 0x54us
    /// U key
    | VcUKey = 0x55us
    /// V key
    | VcVKey = 0x56us
    /// W key
    | VcWKey = 0x57us
    /// X key
    | VcXKey = 0x58us
    /// Y key
    | VcYKey = 0x59us
    /// Z key
    | VcZKey = 0x5Aus
    /// Left Windows key
    | VkLwin = 0x5Bus
    /// Right Windows key
    | VkRwin = 0x5Cus
    /// Applications key
    | VkApps = 0x5Dus
    /// Computer Sleep key
    | VkSleep = 0x5Fus
    /// Numeric keypad 0 key
    | VkNumpad0 = 0x60us
    /// Numeric keypad 1 key
    | VkNumpad1 = 0x61us
    /// Numeric keypad 2 key
    | VkNumpad2 = 0x62us
    /// Numeric keypad 3 key
    | VkNumpad3 = 0x63us
    /// Numeric keypad 4 key
    | VkNumpad4 = 0x64us
    /// Numeric keypad 5 key
    | VkNumpad5 = 0x65us
    /// Numeric keypad 6 key
    | VkNumpad6 = 0x66us
    /// Numeric keypad 7 key
    | VkNumpad7 = 0x67us
    /// Numeric keypad 8 key
    | VkNumpad8 = 0x68us
    /// Numeric keypad 9 key
    | VkNumpad9 = 0x69us
    /// Multiply key
    | VkMultiply = 0x6Aus
    /// Add key
    | VkAdd = 0x6Bus
    /// Separator key
    | VkSeparator = 0x6Cus
    /// Subtract key
    | VkSubtract = 0x6Dus
    /// Decimal key
    | VkDecimal = 0x6Eus
    /// Divide key
    | VkDivide = 0x6Fus
    /// F1 key
    | VkF1 = 0x70us
    /// F2 key
    | VkF2 = 0x71us
    /// F3 key
    | VkF3 = 0x72us
    /// F4 key
    | VkF4 = 0x73us
    /// F5 key
    | VkF5 = 0x74us
    /// F6 key
    | VkF6 = 0x75us
    /// F7 key
    | VkF7 = 0x76us
    /// F8 key
    | VkF8 = 0x77us
    /// F9 key
    | VkF9 = 0x78us
    /// F10 key
    | VkF10 = 0x79us
    /// F11 key
    | VkF11 = 0x7Aus
    /// F12 key
    | VkF12 = 0x7Bus
    /// F13 key
    | VkF13 = 0x7Cus
    /// F14 key
    | VkF14 = 0x7Dus
    /// F15 key
    | VkF15 = 0x7Eus
    /// F16 key
    | VkF16 = 0x7Fus
    /// F17 key
    | VkF17 = 0x80us
    /// F18 key
    | VkF18 = 0x81us
    /// F19 key
    | VkF19 = 0x82us
    /// F20 key
    | VkF20 = 0x83us
    /// F21 key
    | VkF21 = 0x84us
    /// F22 key
    | VkF22 = 0x85us
    /// F23 key
    | VkF23 = 0x86us
    /// F24 key
    | VkF24 = 0x87us
    /// NUM LOCK key
    | VkNumlock = 0x90us
    /// SCROLL LOCK key
    | VkScroll = 0x91us
    /// Left SHIFT key
    | VkLshift = 0xA0us
    /// Right SHIFT key
    | VkRshift = 0xA1us
    /// Left CONTROL key
    | VkLcontrol = 0xA2us
    /// Right CONTROL key
    | VkRcontrol = 0xA3us
    /// Left ALT key
    | VkLmenu = 0xA4us
    /// Right ALT key
    | VkRmenu = 0xA5us
    /// Browser Back key
    | VkBrowserBack = 0xA6us
    /// Browser Forward key
    | VkBrowserForward = 0xA7us
    /// Browser Refresh key
    | VkBrowserRefresh = 0xA8us
    /// Browser Stop key
    | VkBrowserStop = 0xA9us
    /// Browser Search key
    | VkBrowserSearch = 0xAAus
    /// Browser Favorites key
    | VkBrowserFavorites = 0xABus
    /// Browser Start and Home key
    | VkBrowserHome = 0xACus
    /// Volume Mute key
    | VkVolumeMute = 0xADus
    /// Volume Down key
    | VkVolumeDown = 0xAEus
    /// Volume Up key
    | VkVolumeUp = 0xAFus
    /// Next Track key
    | VkMediaNextTrack = 0xB0us
    /// Previous Track key
    | VkMediaPrevTrack = 0xB1us
    /// Stop Media key
    | VkMediaStop = 0xB2us
    /// Play/Pause Media key
    | VkMediaPlayPause = 0xB3us
    /// Start Mail key
    | VkLaunchMail = 0xB4us
    /// Select Media key
    | VkLaunchMediaSelect = 0xB5us
    /// Start Application 1 key
    | VkLaunchApp1 = 0xB6us
    /// Start Application 2 key
    | VkLaunchApp2 = 0xB7us
    /// Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the ;: key
    | VkOem1 = 0xBAus
    /// For any country/region, the + key
    | VkOemPlus = 0xBBus
    /// For any country/region, the , key
    | VkOemComma = 0xBCus
    /// For any country/region, the - key
    | VkOemMinus = 0xBDus
    /// For any country/region, the . key
    | VkOemPeriod = 0xBEus
    /// Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the /? key
    | VkOem2 = 0xBFus
    /// Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the `~ key
    | VkOem3 = 0xC0us
    /// Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the [{ key
    | VkOem4 = 0xDBus
    /// Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the \\| keyus
    | VkOem5 = 0xDCus
    /// Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the ]} key
    | VkOem6 = 0xDDus
    /// Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the '" key
    | VkOem7 = 0xDEus
    /// Used for miscellaneous characters; it can vary by keyboard.
    | VkOem8 = 0xDFus
    /// The <> keys on the US standard keyboard, or the \\| key on the non-US 102-key keyboardus
    | VkOem102 = 0xE2us
    /// IME PROCESS key
    | VkProcesskey = 0xE5us
    /// Used to pass Unicode characters as if they were keystrokes. The VK_PACKET key is the low word of a 32-bit Virtual Key value used for non-keyboard input methods. For more information, see Remark in KEYBDINPUT, SendInput, WM_KEYDOWN, and WM_KEYUP
    | VkPacket = 0xE7us
    /// Attn key
    | VkAttn = 0xF6us
    /// CrSel key
    | VkCrsel = 0xF7us
    /// ExSel key
    | VkExsel = 0xF8us
    /// Erase EOF key
    | VkEreof = 0xF9us
    /// Play key
    | VkPlay = 0xFAus
    /// Zoom key
    | VkZoom = 0xFBus
    /// Reserved
    | VkNoname = 0xFCus
    /// PA1 key
    | VkPa1 = 0xFDus
    /// Clear key
    | VkOemClear = 0xFEus

module KeyCode =
    let fromUShort i = LanguagePrimitives.EnumOfValue<uint16, KeyCode> i
    let fromInt = uint16 >> fromUShort
