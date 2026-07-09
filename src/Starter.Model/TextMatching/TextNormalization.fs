module Starter.TextMatching.TextNormalization

open System.Text

// Copied from fzf
// https://github.com/junegunn/fzf/blob/master/src/algo/normalize.go

let private normalized =
    [| 0x00E1, int 'a' //  WITH ACUTE, LATIN SMALL LETTER
       0x0103, int 'a' //  WITH BREVE, LATIN SMALL LETTER
       0x01CE, int 'a' //  WITH CARON, LATIN SMALL LETTER
       0x00E2, int 'a' //  WITH CIRCUMFLEX, LATIN SMALL LETTER
       0x00E4, int 'a' //  WITH DIAERESIS, LATIN SMALL LETTER
       0x0227, int 'a' //  WITH DOT ABOVE, LATIN SMALL LETTER
       0x1EA1, int 'a' //  WITH DOT BELOW, LATIN SMALL LETTER
       0x0201, int 'a' //  WITH DOUBLE GRAVE, LATIN SMALL LETTER
       0x00E0, int 'a' //  WITH GRAVE, LATIN SMALL LETTER
       0x1EA3, int 'a' //  WITH HOOK ABOVE, LATIN SMALL LETTER
       0x0203, int 'a' //  WITH INVERTED BREVE, LATIN SMALL LETTER
       0x0101, int 'a' //  WITH MACRON, LATIN SMALL LETTER
       0x0105, int 'a' //  WITH OGONEK, LATIN SMALL LETTER
       0x1E9A, int 'a' //  WITH RIGHT HALF RING, LATIN SMALL LETTER
       0x00E5, int 'a' //  WITH RING ABOVE, LATIN SMALL LETTER
       0x1E01, int 'a' //  WITH RING BELOW, LATIN SMALL LETTER
       0x00E3, int 'a' //  WITH TILDE, LATIN SMALL LETTER
       0x0363, int 'a' // , COMBINING LATIN SMALL LETTER
       0x0250, int 'a' // , LATIN SMALL LETTER TURNED
       0x1E03, int 'b' //  WITH DOT ABOVE, LATIN SMALL LETTER
       0x1E05, int 'b' //  WITH DOT BELOW, LATIN SMALL LETTER
       0x0253, int 'b' //  WITH HOOK, LATIN SMALL LETTER
       0x1E07, int 'b' //  WITH LINE BELOW, LATIN SMALL LETTER
       0x0180, int 'b' //  WITH STROKE, LATIN SMALL LETTER
       0x0183, int 'b' //  WITH TOPBAR, LATIN SMALL LETTER
       0x0107, int 'c' //  WITH ACUTE, LATIN SMALL LETTER
       0x010D, int 'c' //  WITH CARON, LATIN SMALL LETTER
       0x00E7, int 'c' //  WITH CEDILLA, LATIN SMALL LETTER
       0x0109, int 'c' //  WITH CIRCUMFLEX, LATIN SMALL LETTER
       0x0255, int 'c' //  WITH CURL, LATIN SMALL LETTER
       0x010B, int 'c' //  WITH DOT ABOVE, LATIN SMALL LETTER
       0x0188, int 'c' //  WITH HOOK, LATIN SMALL LETTER
       0x023C, int 'c' //  WITH STROKE, LATIN SMALL LETTER
       0x0368, int 'c' // , COMBINING LATIN SMALL LETTER
       0x0297, int 'c' // , LATIN LETTER STRETCHED
       0x2184, int 'c' // , LATIN SMALL LETTER REVERSED
       0x010F, int 'd' //  WITH CARON, LATIN SMALL LETTER
       0x1E11, int 'd' //  WITH CEDILLA, LATIN SMALL LETTER
       0x1E13, int 'd' //  WITH CIRCUMFLEX BELOW, LATIN SMALL LETTER
       0x0221, int 'd' //  WITH CURL, LATIN SMALL LETTER
       0x1E0B, int 'd' //  WITH DOT ABOVE, LATIN SMALL LETTER
       0x1E0D, int 'd' //  WITH DOT BELOW, LATIN SMALL LETTER
       0x0257, int 'd' //  WITH HOOK, LATIN SMALL LETTER
       0x1E0F, int 'd' //  WITH LINE BELOW, LATIN SMALL LETTER
       0x0111, int 'd' //  WITH STROKE, LATIN SMALL LETTER
       0x0256, int 'd' //  WITH TAIL, LATIN SMALL LETTER
       0x018C, int 'd' //  WITH TOPBAR, LATIN SMALL LETTER
       0x0369, int 'd' // , COMBINING LATIN SMALL LETTER
       0x00E9, int 'e' //  WITH ACUTE, LATIN SMALL LETTER
       0x0115, int 'e' //  WITH BREVE, LATIN SMALL LETTER
       0x011B, int 'e' //  WITH CARON, LATIN SMALL LETTER
       0x0229, int 'e' //  WITH CEDILLA, LATIN SMALL LETTER
       0x1E19, int 'e' //  WITH CIRCUMFLEX BELOW, LATIN SMALL LETTER
       0x00EA, int 'e' //  WITH CIRCUMFLEX, LATIN SMALL LETTER
       0x00EB, int 'e' //  WITH DIAERESIS, LATIN SMALL LETTER
       0x0117, int 'e' //  WITH DOT ABOVE, LATIN SMALL LETTER
       0x1EB9, int 'e' //  WITH DOT BELOW, LATIN SMALL LETTER
       0x0205, int 'e' //  WITH DOUBLE GRAVE, LATIN SMALL LETTER
       0x00E8, int 'e' //  WITH GRAVE, LATIN SMALL LETTER
       0x1EBB, int 'e' //  WITH HOOK ABOVE, LATIN SMALL LETTER
       0x025D, int 'e' //  WITH HOOK, LATIN SMALL LETTER REVERSED OPEN
       0x0207, int 'e' //  WITH INVERTED BREVE, LATIN SMALL LETTER
       0x0113, int 'e' //  WITH MACRON, LATIN SMALL LETTER
       0x0119, int 'e' //  WITH OGONEK, LATIN SMALL LETTER
       0x0247, int 'e' //  WITH STROKE, LATIN SMALL LETTER
       0x1E1B, int 'e' //  WITH TILDE BELOW, LATIN SMALL LETTER
       0x1EBD, int 'e' //  WITH TILDE, LATIN SMALL LETTER
       0x0364, int 'e' // , COMBINING LATIN SMALL LETTER
       0x029A, int 'e' // , LATIN SMALL LETTER CLOSED OPEN
       0x025E, int 'e' // , LATIN SMALL LETTER CLOSED REVERSED OPEN
       0x025B, int 'e' // , LATIN SMALL LETTER OPEN
       0x0258, int 'e' // , LATIN SMALL LETTER REVERSED
       0x025C, int 'e' // , LATIN SMALL LETTER REVERSED OPEN
       0x01DD, int 'e' // , LATIN SMALL LETTER TURNED
       0x1D08, int 'e' // , LATIN SMALL LETTER TURNED OPEN
       0x1E1F, int 'f' //  WITH DOT ABOVE, LATIN SMALL LETTER
       0x0192, int 'f' //  WITH HOOK, LATIN SMALL LETTER
       0x01F5, int 'g' //  WITH ACUTE, LATIN SMALL LETTER
       0x011F, int 'g' //  WITH BREVE, LATIN SMALL LETTER
       0x01E7, int 'g' //  WITH CARON, LATIN SMALL LETTER
       0x0123, int 'g' //  WITH CEDILLA, LATIN SMALL LETTER
       0x011D, int 'g' //  WITH CIRCUMFLEX, LATIN SMALL LETTER
       0x0121, int 'g' //  WITH DOT ABOVE, LATIN SMALL LETTER
       0x0260, int 'g' //  WITH HOOK, LATIN SMALL LETTER
       0x1E21, int 'g' //  WITH MACRON, LATIN SMALL LETTER
       0x01E5, int 'g' //  WITH STROKE, LATIN SMALL LETTER
       0x0261, int 'g' // , LATIN SMALL LETTER SCRIPT
       0x1E2B, int 'h' //  WITH BREVE BELOW, LATIN SMALL LETTER
       0x021F, int 'h' //  WITH CARON, LATIN SMALL LETTER
       0x1E29, int 'h' //  WITH CEDILLA, LATIN SMALL LETTER
       0x0125, int 'h' //  WITH CIRCUMFLEX, LATIN SMALL LETTER
       0x1E27, int 'h' //  WITH DIAERESIS, LATIN SMALL LETTER
       0x1E23, int 'h' //  WITH DOT ABOVE, LATIN SMALL LETTER
       0x1E25, int 'h' //  WITH DOT BELOW, LATIN SMALL LETTER
       0x02AE, int 'h' //  WITH FISHHOOK, LATIN SMALL LETTER TURNED
       0x0266, int 'h' //  WITH HOOK, LATIN SMALL LETTER
       0x1E96, int 'h' //  WITH LINE BELOW, LATIN SMALL LETTER
       0x0127, int 'h' //  WITH STROKE, LATIN SMALL LETTER
       0x036A, int 'h' // , COMBINING LATIN SMALL LETTER
       0x0265, int 'h' // , LATIN SMALL LETTER TURNED
       0x2095, int 'h' // , LATIN SUBSCRIPT SMALL LETTER
       0x00ED, int 'i' //  WITH ACUTE, LATIN SMALL LETTER
       0x012D, int 'i' //  WITH BREVE, LATIN SMALL LETTER
       0x01D0, int 'i' //  WITH CARON, LATIN SMALL LETTER
       0x00EE, int 'i' //  WITH CIRCUMFLEX, LATIN SMALL LETTER
       0x00EF, int 'i' //  WITH DIAERESIS, LATIN SMALL LETTER
       0x1ECB, int 'i' //  WITH DOT BELOW, LATIN SMALL LETTER
       0x0209, int 'i' //  WITH DOUBLE GRAVE, LATIN SMALL LETTER
       0x00EC, int 'i' //  WITH GRAVE, LATIN SMALL LETTER
       0x1EC9, int 'i' //  WITH HOOK ABOVE, LATIN SMALL LETTER
       0x020B, int 'i' //  WITH INVERTED BREVE, LATIN SMALL LETTER
       0x012B, int 'i' //  WITH MACRON, LATIN SMALL LETTER
       0x012F, int 'i' //  WITH OGONEK, LATIN SMALL LETTER
       0x0268, int 'i' //  WITH STROKE, LATIN SMALL LETTER
       0x1E2D, int 'i' //  WITH TILDE BELOW, LATIN SMALL LETTER
       0x0129, int 'i' //  WITH TILDE, LATIN SMALL LETTER
       0x0365, int 'i' // , COMBINING LATIN SMALL LETTER
       0x0131, int 'i' // , LATIN SMALL LETTER DOTLESS
       0x1D09, int 'i' // , LATIN SMALL LETTER TURNED
       0x1D62, int 'i' // , LATIN SUBSCRIPT SMALL LETTER
       0x2071, int 'i' // , SUPERSCRIPT LATIN SMALL LETTER
       0x01F0, int 'j' //  WITH CARON, LATIN SMALL LETTER
       0x0135, int 'j' //  WITH CIRCUMFLEX, LATIN SMALL LETTER
       0x029D, int 'j' //  WITH CROSSED-TAIL, LATIN SMALL LETTER
       0x0249, int 'j' //  WITH STROKE, LATIN SMALL LETTER
       0x025F, int 'j' //  WITH STROKE, LATIN SMALL LETTER DOTLESS
       0x0237, int 'j' // , LATIN SMALL LETTER DOTLESS
       0x1E31, int 'k' //  WITH ACUTE, LATIN SMALL LETTER
       0x01E9, int 'k' //  WITH CARON, LATIN SMALL LETTER
       0x0137, int 'k' //  WITH CEDILLA, LATIN SMALL LETTER
       0x1E33, int 'k' //  WITH DOT BELOW, LATIN SMALL LETTER
       0x0199, int 'k' //  WITH HOOK, LATIN SMALL LETTER
       0x1E35, int 'k' //  WITH LINE BELOW, LATIN SMALL LETTER
       0x029E, int 'k' // , LATIN SMALL LETTER TURNED
       0x2096, int 'k' // , LATIN SUBSCRIPT SMALL LETTER
       0x013A, int 'l' //  WITH ACUTE, LATIN SMALL LETTER
       0x019A, int 'l' //  WITH BAR, LATIN SMALL LETTER
       0x026C, int 'l' //  WITH BELT, LATIN SMALL LETTER
       0x013E, int 'l' //  WITH CARON, LATIN SMALL LETTER
       0x013C, int 'l' //  WITH CEDILLA, LATIN SMALL LETTER
       0x1E3D, int 'l' //  WITH CIRCUMFLEX BELOW, LATIN SMALL LETTER
       0x0234, int 'l' //  WITH CURL, LATIN SMALL LETTER
       0x1E37, int 'l' //  WITH DOT BELOW, LATIN SMALL LETTER
       0x1E3B, int 'l' //  WITH LINE BELOW, LATIN SMALL LETTER
       0x0140, int 'l' //  WITH MIDDLE DOT, LATIN SMALL LETTER
       0x026B, int 'l' //  WITH MIDDLE TILDE, LATIN SMALL LETTER
       0x026D, int 'l' //  WITH RETROFLEX HOOK, LATIN SMALL LETTER
       0x0142, int 'l' //  WITH STROKE, LATIN SMALL LETTER
       0x2097, int 'l' // , LATIN SUBSCRIPT SMALL LETTER
       0x1E3F, int 'm' //  WITH ACUTE, LATIN SMALL LETTER
       0x1E41, int 'm' //  WITH DOT ABOVE, LATIN SMALL LETTER
       0x1E43, int 'm' //  WITH DOT BELOW, LATIN SMALL LETTER
       0x0271, int 'm' //  WITH HOOK, LATIN SMALL LETTER
       0x0270, int 'm' //  WITH LONG LEG, LATIN SMALL LETTER TURNED
       0x036B, int 'm' // , COMBINING LATIN SMALL LETTER
       0x1D1F, int 'm' // , LATIN SMALL LETTER SIDEWAYS TURNED
       0x026F, int 'm' // , LATIN SMALL LETTER TURNED
       0x2098, int 'm' // , LATIN SUBSCRIPT SMALL LETTER
       0x0144, int 'n' //  WITH ACUTE, LATIN SMALL LETTER
       0x0148, int 'n' //  WITH CARON, LATIN SMALL LETTER
       0x0146, int 'n' //  WITH CEDILLA, LATIN SMALL LETTER
       0x1E4B, int 'n' //  WITH CIRCUMFLEX BELOW, LATIN SMALL LETTER
       0x0235, int 'n' //  WITH CURL, LATIN SMALL LETTER
       0x1E45, int 'n' //  WITH DOT ABOVE, LATIN SMALL LETTER
       0x1E47, int 'n' //  WITH DOT BELOW, LATIN SMALL LETTER
       0x01F9, int 'n' //  WITH GRAVE, LATIN SMALL LETTER
       0x0272, int 'n' //  WITH LEFT HOOK, LATIN SMALL LETTER
       0x1E49, int 'n' //  WITH LINE BELOW, LATIN SMALL LETTER
       0x019E, int 'n' //  WITH LONG RIGHT LEG, LATIN SMALL LETTER
       0x0273, int 'n' //  WITH RETROFLEX HOOK, LATIN SMALL LETTER
       0x00F1, int 'n' //  WITH TILDE, LATIN SMALL LETTER
       0x2099, int 'n' // , LATIN SUBSCRIPT SMALL LETTER
       0x00F3, int 'o' //  WITH ACUTE, LATIN SMALL LETTER
       0x014F, int 'o' //  WITH BREVE, LATIN SMALL LETTER
       0x01D2, int 'o' //  WITH CARON, LATIN SMALL LETTER
       0x00F4, int 'o' //  WITH CIRCUMFLEX, LATIN SMALL LETTER
       0x00F6, int 'o' //  WITH DIAERESIS, LATIN SMALL LETTER
       0x022F, int 'o' //  WITH DOT ABOVE, LATIN SMALL LETTER
       0x1ECD, int 'o' //  WITH DOT BELOW, LATIN SMALL LETTER
       0x0151, int 'o' //  WITH DOUBLE ACUTE, LATIN SMALL LETTER
       0x020D, int 'o' //  WITH DOUBLE GRAVE, LATIN SMALL LETTER
       0x00F2, int 'o' //  WITH GRAVE, LATIN SMALL LETTER
       0x1ECF, int 'o' //  WITH HOOK ABOVE, LATIN SMALL LETTER
       0x01A1, int 'o' //  WITH HORN, LATIN SMALL LETTER
       0x020F, int 'o' //  WITH INVERTED BREVE, LATIN SMALL LETTER
       0x014D, int 'o' //  WITH MACRON, LATIN SMALL LETTER
       0x01EB, int 'o' //  WITH OGONEK, LATIN SMALL LETTER
       0x00F8, int 'o' //  WITH STROKE, LATIN SMALL LETTER
       0x1D13, int 'o' //  WITH STROKE, LATIN SMALL LETTER SIDEWAYS
       0x00F5, int 'o' //  WITH TILDE, LATIN SMALL LETTER
       0x0366, int 'o' // , COMBINING LATIN SMALL LETTER
       0x0275, int 'o' // , LATIN SMALL LETTER BARRED
       0x1D17, int 'o' // , LATIN SMALL LETTER BOTTOM HALF
       0x0254, int 'o' // , LATIN SMALL LETTER OPEN
       0x1D11, int 'o' // , LATIN SMALL LETTER SIDEWAYS
       0x1D12, int 'o' // , LATIN SMALL LETTER SIDEWAYS OPEN
       0x1D16, int 'o' // , LATIN SMALL LETTER TOP HALF
       0x1E55, int 'p' //  WITH ACUTE, LATIN SMALL LETTER
       0x1E57, int 'p' //  WITH DOT ABOVE, LATIN SMALL LETTER
       0x01A5, int 'p' //  WITH HOOK, LATIN SMALL LETTER
       0x209A, int 'p' // , LATIN SUBSCRIPT SMALL LETTER
       0x024B, int 'q' //  WITH HOOK TAIL, LATIN SMALL LETTER
       0x02A0, int 'q' //  WITH HOOK, LATIN SMALL LETTER
       0x0155, int 'r' //  WITH ACUTE, LATIN SMALL LETTER
       0x0159, int 'r' //  WITH CARON, LATIN SMALL LETTER
       0x0157, int 'r' //  WITH CEDILLA, LATIN SMALL LETTER
       0x1E59, int 'r' //  WITH DOT ABOVE, LATIN SMALL LETTER
       0x1E5B, int 'r' //  WITH DOT BELOW, LATIN SMALL LETTER
       0x0211, int 'r' //  WITH DOUBLE GRAVE, LATIN SMALL LETTER
       0x027E, int 'r' //  WITH FISHHOOK, LATIN SMALL LETTER
       0x027F, int 'r' //  WITH FISHHOOK, LATIN SMALL LETTER REVERSED
       0x027B, int 'r' //  WITH HOOK, LATIN SMALL LETTER TURNED
       0x0213, int 'r' //  WITH INVERTED BREVE, LATIN SMALL LETTER
       0x1E5F, int 'r' //  WITH LINE BELOW, LATIN SMALL LETTER
       0x027C, int 'r' //  WITH LONG LEG, LATIN SMALL LETTER
       0x027A, int 'r' //  WITH LONG LEG, LATIN SMALL LETTER TURNED
       0x024D, int 'r' //  WITH STROKE, LATIN SMALL LETTER
       0x027D, int 'r' //  WITH TAIL, LATIN SMALL LETTER
       0x036C, int 'r' // , COMBINING LATIN SMALL LETTER
       0x0279, int 'r' // , LATIN SMALL LETTER TURNED
       0x1D63, int 'r' // , LATIN SUBSCRIPT SMALL LETTER
       0x015B, int 's' //  WITH ACUTE, LATIN SMALL LETTER
       0x0161, int 's' //  WITH CARON, LATIN SMALL LETTER
       0x015F, int 's' //  WITH CEDILLA, LATIN SMALL LETTER
       0x015D, int 's' //  WITH CIRCUMFLEX, LATIN SMALL LETTER
       0x0219, int 's' //  WITH COMMA BELOW, LATIN SMALL LETTER
       0x1E61, int 's' //  WITH DOT ABOVE, LATIN SMALL LETTER
       0x1E9B, int 's' //  WITH DOT ABOVE, LATIN SMALL LETTER LONG
       0x1E63, int 's' //  WITH DOT BELOW, LATIN SMALL LETTER
       0x0282, int 's' //  WITH HOOK, LATIN SMALL LETTER
       0x023F, int 's' //  WITH SWASH TAIL, LATIN SMALL LETTER
       0x017F, int 's' // , LATIN SMALL LETTER LONG
       0x00DF, int 's' // , LATIN SMALL LETTER SHARP
       0x209B, int 's' // , LATIN SUBSCRIPT SMALL LETTER
       0x0165, int 't' //  WITH CARON, LATIN SMALL LETTER
       0x0163, int 't' //  WITH CEDILLA, LATIN SMALL LETTER
       0x1E71, int 't' //  WITH CIRCUMFLEX BELOW, LATIN SMALL LETTER
       0x021B, int 't' //  WITH COMMA BELOW, LATIN SMALL LETTER
       0x0236, int 't' //  WITH CURL, LATIN SMALL LETTER
       0x1E97, int 't' //  WITH DIAERESIS, LATIN SMALL LETTER
       0x1E6B, int 't' //  WITH DOT ABOVE, LATIN SMALL LETTER
       0x1E6D, int 't' //  WITH DOT BELOW, LATIN SMALL LETTER
       0x01AD, int 't' //  WITH HOOK, LATIN SMALL LETTER
       0x1E6F, int 't' //  WITH LINE BELOW, LATIN SMALL LETTER
       0x01AB, int 't' //  WITH PALATAL HOOK, LATIN SMALL LETTER
       0x0288, int 't' //  WITH RETROFLEX HOOK, LATIN SMALL LETTER
       0x0167, int 't' //  WITH STROKE, LATIN SMALL LETTER
       0x036D, int 't' // , COMBINING LATIN SMALL LETTER
       0x0287, int 't' // , LATIN SMALL LETTER TURNED
       0x209C, int 't' // , LATIN SUBSCRIPT SMALL LETTER
       0x0289, int 'u' //  BAR, LATIN SMALL LETTER
       0x00FA, int 'u' //  WITH ACUTE, LATIN SMALL LETTER
       0x016D, int 'u' //  WITH BREVE, LATIN SMALL LETTER
       0x01D4, int 'u' //  WITH CARON, LATIN SMALL LETTER
       0x1E77, int 'u' //  WITH CIRCUMFLEX BELOW, LATIN SMALL LETTER
       0x00FB, int 'u' //  WITH CIRCUMFLEX, LATIN SMALL LETTER
       0x1E73, int 'u' //  WITH DIAERESIS BELOW, LATIN SMALL LETTER
       0x00FC, int 'u' //  WITH DIAERESIS, LATIN SMALL LETTER
       0x1EE5, int 'u' //  WITH DOT BELOW, LATIN SMALL LETTER
       0x0171, int 'u' //  WITH DOUBLE ACUTE, LATIN SMALL LETTER
       0x0215, int 'u' //  WITH DOUBLE GRAVE, LATIN SMALL LETTER
       0x00F9, int 'u' //  WITH GRAVE, LATIN SMALL LETTER
       0x1EE7, int 'u' //  WITH HOOK ABOVE, LATIN SMALL LETTER
       0x01B0, int 'u' //  WITH HORN, LATIN SMALL LETTER
       0x0217, int 'u' //  WITH INVERTED BREVE, LATIN SMALL LETTER
       0x016B, int 'u' //  WITH MACRON, LATIN SMALL LETTER
       0x0173, int 'u' //  WITH OGONEK, LATIN SMALL LETTER
       0x016F, int 'u' //  WITH RING ABOVE, LATIN SMALL LETTER
       0x1E75, int 'u' //  WITH TILDE BELOW, LATIN SMALL LETTER
       0x0169, int 'u' //  WITH TILDE, LATIN SMALL LETTER
       0x0367, int 'u' // , COMBINING LATIN SMALL LETTER
       0x1D1D, int 'u' // , LATIN SMALL LETTER SIDEWAYS
       0x1D1E, int 'u' // , LATIN SMALL LETTER SIDEWAYS DIAERESIZED
       0x1D64, int 'u' // , LATIN SUBSCRIPT SMALL LETTER
       0x1E7F, int 'v' //  WITH DOT BELOW, LATIN SMALL LETTER
       0x028B, int 'v' //  WITH HOOK, LATIN SMALL LETTER
       0x1E7D, int 'v' //  WITH TILDE, LATIN SMALL LETTER
       0x036E, int 'v' // , COMBINING LATIN SMALL LETTER
       0x028C, int 'v' // , LATIN SMALL LETTER TURNED
       0x1D65, int 'v' // , LATIN SUBSCRIPT SMALL LETTER
       0x1E83, int 'w' //  WITH ACUTE, LATIN SMALL LETTER
       0x0175, int 'w' //  WITH CIRCUMFLEX, LATIN SMALL LETTER
       0x1E85, int 'w' //  WITH DIAERESIS, LATIN SMALL LETTER
       0x1E87, int 'w' //  WITH DOT ABOVE, LATIN SMALL LETTER
       0x1E89, int 'w' //  WITH DOT BELOW, LATIN SMALL LETTER
       0x1E81, int 'w' //  WITH GRAVE, LATIN SMALL LETTER
       0x1E98, int 'w' //  WITH RING ABOVE, LATIN SMALL LETTER
       0x028D, int 'w' // , LATIN SMALL LETTER TURNED
       0x1E8D, int 'x' //  WITH DIAERESIS, LATIN SMALL LETTER
       0x1E8B, int 'x' //  WITH DOT ABOVE, LATIN SMALL LETTER
       0x036F, int 'x' // , COMBINING LATIN SMALL LETTER
       0x00FD, int 'y' //  WITH ACUTE, LATIN SMALL LETTER
       0x0177, int 'y' //  WITH CIRCUMFLEX, LATIN SMALL LETTER
       0x00FF, int 'y' //  WITH DIAERESIS, LATIN SMALL LETTER
       0x1E8F, int 'y' //  WITH DOT ABOVE, LATIN SMALL LETTER
       0x1EF5, int 'y' //  WITH DOT BELOW, LATIN SMALL LETTER
       0x1EF3, int 'y' //  WITH GRAVE, LATIN SMALL LETTER
       0x1EF7, int 'y' //  WITH HOOK ABOVE, LATIN SMALL LETTER
       0x01B4, int 'y' //  WITH HOOK, LATIN SMALL LETTER
       0x0233, int 'y' //  WITH MACRON, LATIN SMALL LETTER
       0x1E99, int 'y' //  WITH RING ABOVE, LATIN SMALL LETTER
       0x024F, int 'y' //  WITH STROKE, LATIN SMALL LETTER
       0x1EF9, int 'y' //  WITH TILDE, LATIN SMALL LETTER
       0x028E, int 'y' // , LATIN SMALL LETTER TURNED
       0x017A, int 'z' //  WITH ACUTE, LATIN SMALL LETTER
       0x017E, int 'z' //  WITH CARON, LATIN SMALL LETTER
       0x1E91, int 'z' //  WITH CIRCUMFLEX, LATIN SMALL LETTER
       0x0291, int 'z' //  WITH CURL, LATIN SMALL LETTER
       0x017C, int 'z' //  WITH DOT ABOVE, LATIN SMALL LETTER
       0x1E93, int 'z' //  WITH DOT BELOW, LATIN SMALL LETTER
       0x0225, int 'z' //  WITH HOOK, LATIN SMALL LETTER
       0x1E95, int 'z' //  WITH LINE BELOW, LATIN SMALL LETTER
       0x0290, int 'z' //  WITH RETROFLEX HOOK, LATIN SMALL LETTER
       0x01B6, int 'z' //  WITH STROKE, LATIN SMALL LETTER
       0x0240, int 'z' //  WITH SWASH TAIL, LATIN SMALL LETTER
       0x0251, int 'a' // , latin small letter script
       0x00C1, int 'A' //  WITH ACUTE, LATIN CAPITAL LETTER
       0x00C2, int 'A' //  WITH CIRCUMFLEX, LATIN CAPITAL LETTER
       0x00C4, int 'A' //  WITH DIAERESIS, LATIN CAPITAL LETTER
       0x00C0, int 'A' //  WITH GRAVE, LATIN CAPITAL LETTER
       0x00C5, int 'A' //  WITH RING ABOVE, LATIN CAPITAL LETTER
       0x023A, int 'A' //  WITH STROKE, LATIN CAPITAL LETTER
       0x00C3, int 'A' //  WITH TILDE, LATIN CAPITAL LETTER
       0x1D00, int 'A' // , LATIN LETTER SMALL CAPITAL
       0x0181, int 'B' //  WITH HOOK, LATIN CAPITAL LETTER
       0x0243, int 'B' //  WITH STROKE, LATIN CAPITAL LETTER
       0x0299, int 'B' // , LATIN LETTER SMALL CAPITAL
       0x1D03, int 'B' // , LATIN LETTER SMALL CAPITAL BARRED
       0x00C7, int 'C' //  WITH CEDILLA, LATIN CAPITAL LETTER
       0x023B, int 'C' //  WITH STROKE, LATIN CAPITAL LETTER
       0x1D04, int 'C' // , LATIN LETTER SMALL CAPITAL
       0x018A, int 'D' //  WITH HOOK, LATIN CAPITAL LETTER
       0x0189, int 'D' // , LATIN CAPITAL LETTER AFRICAN
       0x1D05, int 'D' // , LATIN LETTER SMALL CAPITAL
       0x00C9, int 'E' //  WITH ACUTE, LATIN CAPITAL LETTER
       0x00CA, int 'E' //  WITH CIRCUMFLEX, LATIN CAPITAL LETTER
       0x00CB, int 'E' //  WITH DIAERESIS, LATIN CAPITAL LETTER
       0x00C8, int 'E' //  WITH GRAVE, LATIN CAPITAL LETTER
       0x0246, int 'E' //  WITH STROKE, LATIN CAPITAL LETTER
       0x0190, int 'E' // , LATIN CAPITAL LETTER OPEN
       0x018E, int 'E' // , LATIN CAPITAL LETTER REVERSED
       0x1D07, int 'E' // , LATIN LETTER SMALL CAPITAL
       0x0193, int 'G' //  WITH HOOK, LATIN CAPITAL LETTER
       0x029B, int 'G' //  WITH HOOK, LATIN LETTER SMALL CAPITAL
       0x0262, int 'G' // , LATIN LETTER SMALL CAPITAL
       0x029C, int 'H' // , LATIN LETTER SMALL CAPITAL
       0x00CD, int 'I' //  WITH ACUTE, LATIN CAPITAL LETTER
       0x00CE, int 'I' //  WITH CIRCUMFLEX, LATIN CAPITAL LETTER
       0x00CF, int 'I' //  WITH DIAERESIS, LATIN CAPITAL LETTER
       0x0130, int 'I' //  WITH DOT ABOVE, LATIN CAPITAL LETTER
       0x00CC, int 'I' //  WITH GRAVE, LATIN CAPITAL LETTER
       0x0197, int 'I' //  WITH STROKE, LATIN CAPITAL LETTER
       0x026A, int 'I' // , LATIN LETTER SMALL CAPITAL
       0x0248, int 'J' //  WITH STROKE, LATIN CAPITAL LETTER
       0x1D0A, int 'J' // , LATIN LETTER SMALL CAPITAL
       0x1D0B, int 'K' // , LATIN LETTER SMALL CAPITAL
       0x023D, int 'L' //  WITH BAR, LATIN CAPITAL LETTER
       0x1D0C, int 'L' //  WITH STROKE, LATIN LETTER SMALL CAPITAL
       0x029F, int 'L' // , LATIN LETTER SMALL CAPITAL
       0x019C, int 'M' // , LATIN CAPITAL LETTER TURNED
       0x1D0D, int 'M' // , LATIN LETTER SMALL CAPITAL
       0x019D, int 'N' //  WITH LEFT HOOK, LATIN CAPITAL LETTER
       0x0220, int 'N' //  WITH LONG RIGHT LEG, LATIN CAPITAL LETTER
       0x00D1, int 'N' //  WITH TILDE, LATIN CAPITAL LETTER
       0x0274, int 'N' // , LATIN LETTER SMALL CAPITAL
       0x1D0E, int 'N' // , LATIN LETTER SMALL CAPITAL REVERSED
       0x00D3, int 'O' //  WITH ACUTE, LATIN CAPITAL LETTER
       0x00D4, int 'O' //  WITH CIRCUMFLEX, LATIN CAPITAL LETTER
       0x00D6, int 'O' //  WITH DIAERESIS, LATIN CAPITAL LETTER
       0x00D2, int 'O' //  WITH GRAVE, LATIN CAPITAL LETTER
       0x019F, int 'O' //  WITH MIDDLE TILDE, LATIN CAPITAL LETTER
       0x00D8, int 'O' //  WITH STROKE, LATIN CAPITAL LETTER
       0x00D5, int 'O' //  WITH TILDE, LATIN CAPITAL LETTER
       0x0186, int 'O' // , LATIN CAPITAL LETTER OPEN
       0x1D0F, int 'O' // , LATIN LETTER SMALL CAPITAL
       0x1D10, int 'O' // , LATIN LETTER SMALL CAPITAL OPEN
       0x1D18, int 'P' // , LATIN LETTER SMALL CAPITAL
       0x024A, int 'Q' //  WITH HOOK TAIL, LATIN CAPITAL LETTER SMALL
       0x024C, int 'R' //  WITH STROKE, LATIN CAPITAL LETTER
       0x0280, int 'R' // , LATIN LETTER SMALL CAPITAL
       0x0281, int 'R' // , LATIN LETTER SMALL CAPITAL INVERTED
       0x1D19, int 'R' // , LATIN LETTER SMALL CAPITAL REVERSED
       0x1D1A, int 'R' // , LATIN LETTER SMALL CAPITAL TURNED
       0x023E, int 'T' //  WITH DIAGONAL STROKE, LATIN CAPITAL LETTER
       0x01AE, int 'T' //  WITH RETROFLEX HOOK, LATIN CAPITAL LETTER
       0x1D1B, int 'T' // , LATIN LETTER SMALL CAPITAL
       0x0244, int 'U' //  BAR, LATIN CAPITAL LETTER
       0x00DA, int 'U' //  WITH ACUTE, LATIN CAPITAL LETTER
       0x00DB, int 'U' //  WITH CIRCUMFLEX, LATIN CAPITAL LETTER
       0x00DC, int 'U' //  WITH DIAERESIS, LATIN CAPITAL LETTER
       0x00D9, int 'U' //  WITH GRAVE, LATIN CAPITAL LETTER
       0x1D1C, int 'U' // , LATIN LETTER SMALL CAPITAL
       0x01B2, int 'V' //  WITH HOOK, LATIN CAPITAL LETTER
       0x0245, int 'V' // , LATIN CAPITAL LETTER TURNED
       0x1D20, int 'V' // , LATIN LETTER SMALL CAPITAL
       0x1D21, int 'W' // , LATIN LETTER SMALL CAPITAL
       0x00DD, int 'Y' //  WITH ACUTE, LATIN CAPITAL LETTER
       0x0178, int 'Y' //  WITH DIAERESIS, LATIN CAPITAL LETTER
       0x024E, int 'Y' //  WITH STROKE, LATIN CAPITAL LETTER
       0x028F, int 'Y' // , LATIN LETTER SMALL CAPITAL
       0x1D22, int 'Z' // , LATIN LETTER SMALL CAPITAL
       int 'Ắ', int 'A'
       int 'Ấ', int 'A'
       int 'Ằ', int 'A'
       int 'Ầ', int 'A'
       int 'Ẳ', int 'A'
       int 'Ẩ', int 'A'
       int 'Ẵ', int 'A'
       int 'Ẫ', int 'A'
       int 'Ặ', int 'A'
       int 'Ậ', int 'A'

       int 'ắ', int 'a'
       int 'ấ', int 'a'
       int 'ằ', int 'a'
       int 'ầ', int 'a'
       int 'ẳ', int 'a'
       int 'ẩ', int 'a'
       int 'ẵ', int 'a'
       int 'ẫ', int 'a'
       int 'ặ', int 'a'
       int 'ậ', int 'a'

       int 'Ế', int 'E'
       int 'Ề', int 'E'
       int 'Ể', int 'E'
       int 'Ễ', int 'E'
       int 'Ệ', int 'E'

       int 'ế', int 'e'
       int 'ề', int 'e'
       int 'ể', int 'e'
       int 'ễ', int 'e'
       int 'ệ', int 'e'

       int 'Ố', int 'O'
       int 'Ớ', int 'O'
       int 'Ồ', int 'O'
       int 'Ờ', int 'O'
       int 'Ổ', int 'O'
       int 'Ở', int 'O'
       int 'Ỗ', int 'O'
       int 'Ỡ', int 'O'
       int 'Ộ', int 'O'
       int 'Ợ', int 'O'

       int 'ố', int 'o'
       int 'ớ', int 'o'
       int 'ồ', int 'o'
       int 'ờ', int 'o'
       int 'ổ', int 'o'
       int 'ở', int 'o'
       int 'ỗ', int 'o'
       int 'ỡ', int 'o'
       int 'ộ', int 'o'
       int 'ợ', int 'o'

       int 'Ứ', int 'U'
       int 'Ừ', int 'U'
       int 'Ử', int 'U'
       int 'Ữ', int 'U'
       int 'Ự', int 'U'

       int 'ứ', int 'u'
       int 'ừ', int 'u'
       int 'ử', int 'u'
       int 'ữ', int 'u'
       int 'ự', int 'u' |]
    |> dict

module Rune =
    let normalize (codePoint: int) =
        if codePoint < 0x00C0 || codePoint > 0x2184 then
            codePoint
        else
            try
                normalized[codePoint]
            with _ ->
                codePoint

module String =
    let normalize (str: string) : Rune array =
        let mutable runes = str.EnumerateRunes()
        let mutable list = System.Collections.Generic.List(str.Length)

        while runes.MoveNext() do
            runes.Current.Value
            |> Rune.normalize
            |> Rune
            |> list.Add

        list.ToArray()
