namespace Rounder;

/** Can be used either as a wrapper for an ARGB colour value, or
 * for its static functions which parse/convert between such values. */
public class Colour {
    public uint Value { get; }
    public uint AlphaChannel => Value & 0xff000000;
    public uint RedChannel => Value & 0x00ff0000;
    public uint GreenChannel => Value & 0x0000ff00;
    public uint BlueChannel => Value & 0x000000ff;

    public Colour(uint red, uint green, uint blue, uint alpha = 255) {
        Value = ColourValue(red, green, blue, alpha);
    }

    public Colour(string hexString) {
        Value = ColourValue(hexString);
    }

    public Colour(uint value) {
        Value = value;
    }

    public (uint red, uint green, uint blue, uint alpha) ColourChannels() {
        return (RedChannel, GreenChannel, BlueChannel, AlphaChannel);
    }

    public override string ToString() {
        return HexString(Value);
    }

    public string ToString(bool appendSquare) {
        return HexString(Value, appendSquare);
    }

    public override bool Equals(object? obj) {
        return obj is Colour other && other.Value == Value;
    }

    public override int GetHashCode() {
        return Value.GetHashCode();
    }

    public static implicit operator uint(Colour colour) {
        return colour.Value;
    }

    public static implicit operator Colour(uint value) {
        return new Colour(value);
    }

    public static implicit operator Colour((uint red, uint green, uint blue, uint alpha) channels) {
        return new Colour(channels.red, channels.green, channels.blue, channels.alpha);
    }

    public static implicit operator Colour((uint red, uint green, uint blue) channels) {
        return new Colour(channels.red, channels.green, channels.blue);
    }

    public static implicit operator int(Colour colour) {
        return (int)colour.Value;
    }

    public static implicit operator Colour(int value) {
        return new Colour((uint)value);
    }

    public static implicit operator Colour((int red, int green, int blue, int alpha) channels) {
        return new Colour((uint)channels.red, (uint)channels.green, (uint)channels.blue,
                          (uint)channels.alpha);
    }

    public static implicit operator Colour((int red, int green, int blue) channels) {
        return new Colour((uint)channels.red, (uint)channels.green, (uint)channels.blue);
    }

    //==========================================================================
    // Static functions
    //==========================================================================

    public static uint ColourValue(uint red, uint green, uint blue, uint alpha = 255) {
        return alpha << 24 | red << 16 | green << 8 | blue;
    }

    public static (uint red, uint green, uint blue, uint alpha) ColourChannels(uint value) {
        uint alpha = value & 0xff000000;
        uint red = value & 0x00ff0000;
        uint green = value & 0x0000ff00;
        uint blue = value & 0x000000ff;
        return (red, green, blue, alpha);
    }

    public static uint ColourValue(string hex) {
        if (hex[0].Equals('#')) {
            hex = hex[1..];
        }

        uint alpha = 255, red, green, blue;
        switch (hex.Length) {
            case 8: // RRggBBaa
                alpha = uint.Parse(hex[6..8], System.Globalization.NumberStyles.HexNumber);
                goto case 6;
            case 6: // RRggBB
                red = uint.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber);
                green = uint.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber);
                blue = uint.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber);
                return ColourValue(red, green, blue, alpha);
            case 4: // RGBA
                alpha = uint.Parse(hex[3..4], System.Globalization.NumberStyles.HexNumber);
                goto case 3;
            case 3: // RGB
                red = uint.Parse(hex[..1], System.Globalization.NumberStyles.HexNumber);
                green = uint.Parse(hex[1..2], System.Globalization.NumberStyles.HexNumber);
                blue = uint.Parse(hex[2..], System.Globalization.NumberStyles.HexNumber);
                return ColourValue(red, green, blue, alpha);
            default:
                throw new ArgumentOutOfRangeException(nameof(hex));
        }
    }

    public static string HexString(uint value, bool appendSquare = false) {
        string formatted = value.ToString("X8");
        if (appendSquare) {
            return '#' + formatted[2..] + formatted[..2];
        }

        return formatted[2..] + formatted[..2];
    }

    public static Colour Red => new Colour(0xffff0000);
    public static Colour Green => new Colour(0xff00ff00);
    public static Colour Blue => new Colour(0xff0000ff);
    public static Colour Transparent => new Colour(0x00ffffff);
}