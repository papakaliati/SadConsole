﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SadConsole.Effects;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SadConsole.Consoles
{
    /// <summary>
    /// The base class for a text surface. Provides code for the view port and basic cell access.
    /// </summary>
    [DataContract]
    public class TextSurface : IEnumerable<Cell>, ITextSurface
    {
        [DataMember(Name = "FontName")]
        protected string fontName;

        [DataMember(Name = "FontSize")]
        protected Font.FontSizes fontSize;

        protected Font font;

        [DataMember(Name = "Area")]
        protected Rectangle area;

        /// <summary>
        /// An array of all cells in this surface.
        /// </summary>
        /// <remarks>This array is calculated internally and its size shouldn't be modified. Use the <see cref="width"/> and <see cref="height"/> properties instead. The cell data can be changed.</remarks>
        [DataMember(Name = "Cells")]
        protected Cell[] cells;

        [DataMember(Name = "Width")]
        protected int width = 1;

        [DataMember(Name = "Height")]
        protected int height = 1;

        /// <summary>
        /// Gets a cell based on it's coordinates on the surface.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <returns>The indicated cell.</returns>
        public Cell this[int x, int y]
        {
            get { return cells[y * width + x]; }
            protected set { cells[y * width + x] = value; }
        }

        /// <summary>
        /// Gets a cell by index.
        /// </summary>
        /// <param name="index">The index of the cell.</param>
        /// <returns>The indicated cell.</returns>
        public Cell this[int index]
        {
            get { return cells[index]; }
            protected set { cells[index] = value; }
        }

        /// <summary>
        /// The total cells for this surface.
        /// </summary>
        public int CellCount { get { return cells.Length; } }

        /// <summary>
        /// The default foreground for glyphs on this surface.
        /// </summary>
        [DataMember]
        public Color DefaultForeground { get; set; } = Color.White;

        /// <summary>
        /// The default background for glyphs on this surface.
        /// </summary>
        [DataMember]
        public Color DefaultBackground { get; set; } = Color.Transparent;

        /// <summary>
        /// How many cells wide the surface is.
        /// </summary>
        public int Width
        {
            get { return width; }
        }

        /// <summary>
        /// How many cells high the surface is.
        /// </summary>
        public int Height
        {
            get { return height; }
        }

        /// <summary>
        /// Font used with rendering.
        /// </summary>
        public Font Font { get { return font; } set { font = value; OnFontChanged(); } }

        #region ITextSurfaceView
        /// <summary>
        /// Pixel area of the render cells.
        /// </summary>
        public Rectangle AbsoluteArea { get; set; }

        /// <summary>
        /// Destination rectangles for rendering.
        /// </summary>
        public Rectangle[] RenderRects { get; set; }

        /// <summary>
        /// All cells of the surface.
        /// </summary>
        public Cell[] Cells { get { return cells; } }

        /// <summary>
        /// Cells that will be rendered.
        /// </summary>
        public Cell[] RenderCells { get; set; }

        /// <summary>
        /// A tint used in rendering.
        /// </summary>
        [DataMember]
        public Color Tint { get; set; } = Color.Transparent;

        /// <summary>
        /// Sets the area of the text surface that should be rendered.
        /// </summary>
        public Rectangle RenderArea
        {
            get { return area; }
            set
            {
                area = value;

                if (area == null)
                    area = new Rectangle(0, 0, width, height);

                if (area.Width > width)
                    area.Width = width;
                if (area.Height > height)
                    area.Height = height;

                if (area.X < 0)
                    area.X = 0;
                if (area.Y < 0)
                    area.Y = 0;

                if (area.X + area.Width > width)
                    area.X = width - area.Width;
                if (area.Y + area.Height > height)
                    area.Y = height - area.Height;

                ResetArea();
            }
        }
        #endregion

        /// <summary>
        /// Creates a new text surface with the specified width and height.
        /// </summary>
        /// <param name="width">The width of the surface.</param>
        /// <param name="height">THe height of the surface.</param>
        /// <param name="font">The font used with rendering.</param>
        public TextSurface(int width, int height, Font font)
        {
            this.width = width;
            this.height = height;
            InitializeCells();
            Font = font;
            RenderArea = new Rectangle(0, 0, width, height);
        }

        /// <summary>
        /// Creates a new text surface with the specified width, height, and initial set of cell data.
        /// </summary>
        /// <param name="width">The width of the surface.</param>
        /// <param name="height">THe height of the surface.</param>
        /// <param name="font">The font used with rendering.</param>
        /// <param name="initialCells"></param>
        public TextSurface(int width, int height, Font font, Cell[] initialCells)
        {
            if (initialCells.Length != width * height)
                throw new ArgumentOutOfRangeException("initialCells", "initialCells length must equal width * height");

            this.width = width;
            this.height = height;
            cells = RenderCells = initialCells;
        }

        /// <summary>
        /// Do your own setup.
        /// </summary>
        protected TextSurface() { } 

        /// <summary>
        /// Initializes the cells. This method caches all of the rendering points and rectangles and initializes each cell.
        /// </summary>
        /// <param name="oldWidth">The old size of the surface in width. Used when resizing to preserve existing cells.</param>
        protected virtual void InitializeCells()
        {
            cells = new Cell[width * height];
            RenderCells = cells;

            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = new Cell();
                cells[i].Foreground = this.DefaultForeground;
                cells[i].Background = this.DefaultBackground;
                cells[i].OnCreated();
            }
        }

        /// <summary>
        /// Keeps the text view data in sync with this surface.
        /// </summary>
        protected virtual void ResetArea()
        {
            RenderRects = new Rectangle[area.Width * area.Height];
            RenderCells = new Cell[area.Width * area.Height];

            int index = 0;

            for (int y = 0; y < area.Height; y++)
            {
                for (int x = 0; x < area.Width; x++)
                {
                    RenderRects[index] = new Rectangle(x * font.Size.X, y * font.Size.Y, font.Size.X, font.Size.Y);
                    RenderCells[index] = cells[(y + area.Top) * width + (x + area.Left)];
                    index++;
                }
            }

            // TODO: Optimization by calculating AbsArea and seeing if it's diff from current, if so, don't create new RenderRects
            AbsoluteArea = new Rectangle(0, 0, area.Width * font.Size.X, area.Height * font.Size.Y);
        }

        protected virtual void OnFontChanged()
        {
            ResetArea();
        }


        #region Static Methods


        public static int GetIndexFromPoint(Point location, int width)
        {
            return location.Y * width + location.X;
        }

        public int GetIndexFromPoint(Point location)
        {
            return location.Y * width + location.X;
        }

        public static int GetIndexFromPoint(int x, int y, int width)
        {
            return y * width + x;
        }

        public int GetIndexFromPoint(int x, int y)
        {
            return y * width + x;
        }

        public static Point GetPointFromIndex(int index, int width)
        {
            return new Point(index % width, index / width);
        }

        public Point GetPointFromIndex(int index)
        {
            return new Point(index % width, index / width);
        }
        #endregion


        /// <summary>
        /// Tests if a cell is valid based on its x,y position.
        /// </summary>
        /// <param name="x">The x coordinate of the cell to test.</param>
        /// <param name="y">The y coordinate of the cell to test.</param>
        /// <param name="index">If the cell is valid, the index of the cell when found.</param>
        /// <returns>A true value indicating the cell by x,y does exist in this cell surface.</returns>
        public bool IsValidCell(int x, int y, out int index)
        {
            if (x >= 0 && x < this.width && y >= 0 && y < this.height)
            {
                index = y * width + x;
                return true;
            }
            else
            {
                index = -1;
                return false;
            }
        }

        /// <summary>
        /// Tests if a cell is valid based on its x,y position.
        /// </summary>
        /// <param name="x">The x coordinate of the cell to test.</param>
        /// <param name="y">The y coordinate of the cell to test.</param>
        /// <param name="index">If the cell is valid, the index of the cell when found.</param>
        /// <returns>A true value indicating the cell by x,y does exist in this cell surface.</returns>
        public bool IsValidCell(int x, int y)
        {
            if (x >= 0 && x < this.width && y >= 0 && y < this.height)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a cell based on it's coordinates on the surface.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <returns>The indicated cell.</returns>
        public Cell GetCell(int x, int y)
        {
            return cells[y * width + x];
        }

        /// <summary>
        /// Copies the contents of the cell surface to the destination.
        /// </summary>
        /// <remarks>If the sizes to not match, it will always start at 0,0 and work with what it can and move on to the next row when either surface runs out of columns being processed</remarks>
        /// <param name="source">The source surface</param>
        /// <param name="destination">The destination surface.</param>
        public static void Copy(ITextSurface source, ITextSurface destination)
        {
            int maxX = source.Width >= destination.Width ? destination.Width : source.Width;
            int maxY = source.Height >= destination.Height ? destination.Height : source.Height;

            for (int x = 0; x < maxX; x++)
            {
                for (int y = 0; y < maxY; y++)
                {
                    int sourceIndex;
                    int destIndex;

                    if (source.IsValidCell(x, y, out sourceIndex) && destination.IsValidCell(x, y, out destIndex))
                    {
                        var sourceCell = source.Cells[sourceIndex];
                        var desCell = destination.Cells[destIndex];
                        sourceCell.CopyAppearanceTo(desCell);
                        desCell.Effect = sourceCell.Effect;
                    }
                }
            }
        }

        /// <summary>
        /// Copies the contents of the cell surface to the destination at the specified x,y.
        /// </summary>
        /// <param name="x">The x coordinate of the destination.</param>
        /// <param name="y">The y coordinate of the destination.</param>
        /// <param name="source">The source surface</param>
        /// <param name="destination">The destination surface.</param>
        public static void Copy(ITextSurface source, ITextSurface destination, int x, int y)
        {
            for (int curx = 0; curx < source.Width; curx++)
            {
                for (int cury = 0; cury < source.Height; cury++)
                {
                    int sourceIndex;
                    int destIndex;

                    if (source.IsValidCell(curx, cury, out sourceIndex) && destination.IsValidCell(x + curx, y + cury, out destIndex))
                    {
                        var sourceCell = source.Cells[sourceIndex];
                        var desCell = destination.Cells[destIndex];
                        sourceCell.CopyAppearanceTo(desCell);
                        desCell.Effect = sourceCell.Effect;
                    }
                }
            }
        }

        /// <summary>
        /// Copies the contents of this cell surface at the specified x,y coordinates to the destination, only with the specified width and height, and copies it to the specified <paramref name="destinationX"/> and <paramref name="destinationY"/> position.
        /// </summary>
        /// <param name="source">The source surface</param>
        /// <param name="x">The x coordinate to start from.</param>
        /// <param name="y">The y coordinate to start from.</param>
        /// <param name="width">The width to copy from.</param>
        /// <param name="height">The height to copy from.</param>
        /// <param name="destination">The destination surface.</param>
        /// <param name="destinationX">The x coordinate to copy to.</param>
        /// <param name="destinationY">The y coordinate to copy to.</param>
        public static void Copy(ITextSurface source, int x, int y, int width, int height, ITextSurface destination, int destinationX, int destinationY)
        {
            int destX = destinationX;
            int destY = destinationY;

            for (int curx = 0; curx < width; curx++)
            {
                for (int cury = 0; cury < height; cury++)
                {
                    int sourceIndex;
                    int destIndex;

                    if (source.IsValidCell(curx + x, cury + y, out sourceIndex) && destination.IsValidCell(destX, destY, out destIndex))
                    {
                        var sourceCell = source.Cells[sourceIndex];
                        var desCell = destination.Cells[destIndex];
                        sourceCell.CopyAppearanceTo(desCell);
                        desCell.Effect = sourceCell.Effect;
                    }
                    destY++;
                }
                destY = destinationY;
                destX++;
            }
        }

        public IEnumerator<Cell> GetEnumerator()
        {
            return ((IEnumerable<Cell>)cells).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return cells.GetEnumerator();
        }

        #region Serialization
        /// <summary>
        /// Saves the <see cref="TextSurface"/> to a file.
        /// </summary>
        /// <param name="file">The destination file.</param>
        public void Save(string file)
        {
            Serializer.Save(this, file);
        }

        /// <summary>
        /// Loads a <see cref="TextSurface"/> from a file.
        /// </summary>
        /// <param name="file">The source file.</param>
        /// <returns></returns>
        public static TextSurface Load(string file)
        {
            return Serializer.Load<TextSurface>(file);
        }

        [OnSerializing]
        private void BeforeSerializing(StreamingContext context)
        {
            fontName = Font.Name;
            fontSize = Font.SizeMultiple;
        }

        [OnDeserialized]
        private void AfterDeserialized(StreamingContext context)
        {
            Font font;

            // Try to find font
            if (Engine.Fonts.ContainsKey(fontName))
                font = Engine.Fonts[fontName].GetFont(fontSize);
            else
                font = Engine.DefaultFont;

            Font = font;
        }
        #endregion
    }
}
