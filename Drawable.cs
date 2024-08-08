using System.Drawing;

namespace Vic3MapCSharp
{
    public interface IDrawable
    {
        string Name { get; set; }
        Color Color { get; set; }
        (int x, int y) RectangleCenter { get; set; }
        (int w, int h) MaxRectangleSize { get; set; }
        (int x, int y) SquareCenter { get; set; }
        (int w, int h) MaxSquareSize { get; set; }
        HashSet<(int x, int y)> Coords { get; set; }

        void GetCenter(bool floodFill = false);
    }
}
