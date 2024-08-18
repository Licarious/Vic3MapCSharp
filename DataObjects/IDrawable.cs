using System.Drawing;

namespace Vic3MapCSharp.DataObjects
{
    /// <summary>
    /// Represents a drawable object with properties for name, color, coordinates, and size.
    /// </summary>
    public interface IDrawable
    {
        /// <summary>
        /// Gets or sets the name of the drawable object.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the color of the drawable object.
        /// </summary>
        Color Color { get; set; }

        /// <summary>
        /// Gets or sets the x,y center and height, width of maximum rectanges.
        /// </summary>
        List<(int x, int y, int h, int w)> MaximumRectangles { get; set; }

        /// <summary>
        /// Gets or sets the coordinates of the drawable object.
        /// </summary>
        HashSet<(int x, int y)> Coords { get; set; }

        /// <summary>
        /// Calculates the center of the max square and rectangle for the drawable object.
        /// </summary>
        /// <param name="floodFill">If set to <c>true</c>, uses flood fill algorithm to determine the center.</param>
        void GetCenter(bool floodFill = false);
    }
}
