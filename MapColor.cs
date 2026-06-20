using SixLabors.ImageSharp.PixelFormats;

namespace Vic3MapCSharp
{
    public readonly record struct MapColor(byte R, byte G, byte B, byte A = 255)
    {
        public bool IsEmpty => this == default;

        public static MapColor Black => FromArgb(0, 0, 0);
        public static MapColor White => FromArgb(255, 255, 255);
        public static MapColor LightBlue => FromArgb(173, 216, 230);
        public static MapColor Blue => FromArgb(0, 0, 255);
        public static MapColor Purple => FromArgb(128, 0, 128);
        public static MapColor DarkCyan => FromArgb(0, 139, 139);
        public static MapColor Red => FromArgb(255, 0, 0);
        public static MapColor Yellow => FromArgb(255, 255, 0);
        public static MapColor DarkGreen => FromArgb(0, 100, 0);
        public static MapColor Gray => FromArgb(128, 128, 128);
        public static MapColor Green => FromArgb(0, 128, 0);
        public static MapColor Transparent => FromArgb(0, 0, 0, 0);
        public static MapColor HotPink => FromArgb(255, 105, 180);
        public static MapColor DarkBlue => FromArgb(0, 0, 139);

        public static MapColor FromArgb(int r, int g, int b) => new((byte)r, (byte)g, (byte)b);
        public static MapColor FromArgb(int a, int r, int g, int b) => new((byte)r, (byte)g, (byte)b, (byte)a);
        public static MapColor FromArgb(int a, MapColor color) => new(color.R, color.G, color.B, (byte)a);

        public static MapColor FromHex(string value)
        {
            string hex = value.Trim().TrimStart('#');
            if (hex.Length != 6 && hex.Length != 8) throw new FormatException($"Invalid colour value: {value}");

            byte r = Convert.ToByte(hex[..2], 16);
            byte g = Convert.ToByte(hex[2..4], 16);
            byte b = Convert.ToByte(hex[4..6], 16);
            byte a = hex.Length == 8 ? Convert.ToByte(hex[6..8], 16) : (byte)255;
            return new MapColor(r, g, b, a);
        }

        public Rgba32 ToRgba32() => new(R, G, B, A);
        public SixLabors.ImageSharp.Color ToImageSharpColor() => SixLabors.ImageSharp.Color.FromRgba(R, G, B, A);

        public static implicit operator Rgba32(MapColor color) => color.ToRgba32();
        public static implicit operator SixLabors.ImageSharp.Color(MapColor color) => color.ToImageSharpColor();
        public static implicit operator MapColor(Rgba32 color) => new(color.R, color.G, color.B, color.A);
    }
}
