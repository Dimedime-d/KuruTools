﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text;

namespace KuruLevelEditor
{
    class EditorGrid
    {
        int tile_size = 8;
        Rectangle bounds;
        SpriteSet sprites;
        int[,] grid;
        Point position;
        int brush_size = 1;
        public EditorGrid(Rectangle bounds, SpriteSet sprites, int[,] grid, Point position)
        {
            this.bounds = bounds;
            this.sprites = sprites;
            this.grid = grid;
            this.position = position;
        }

        public Point TileCoordToScreenCoord(int x, int y)
        {
            return new Point(x * tile_size, y * tile_size) - position + bounds.Location;
        }

        public Rectangle TileCoordToScreenRect(int x, int y)
        {
            return new Rectangle(TileCoordToScreenCoord(x, y), new Point(tile_size, tile_size));
        }

        public Point ScreenCoordToTileCoord(int x, int y)
        {
            Point p = new Point(x, y) + position - bounds.Location;
            return new Point(p.X / tile_size, p.Y / tile_size);
        }

        public Rectangle ScreenCoordToTileRect(int x, int y)
        {
            return new Rectangle(ScreenCoordToTileCoord(x, y), new Point(tile_size, tile_size));
        }

        public int TileSize {
            get { return tile_size; }
            set {
                Rectangle view = new Rectangle(position, new Point (bounds.Size.X, bounds.Size.Y));
                Point center = view.Center;
                position = new Point(center.X * value / tile_size - bounds.Size.X/2, center.Y * value / tile_size - bounds.Size.Y/2);
                tile_size = value;
            }
        }
        public int BrushSize
        {
            get { return brush_size; }
            set { brush_size = value; }
        }
        public Point Position
        {
            get { return position; }
            set { position = value; }
        }
        public int[,] Grid
        {
            get { return grid; }
        }

        List<Rectangle> RectanglesAround(Rectangle r, int size)
        {
            List<Rectangle> res = new List<Rectangle>();
            size--;
            for (int i = -size; i <= size; i++)
            {
                for (int j = -size; j <= size; j++)
                    res.Add(new Rectangle(r.Location + new Point(i*r.Width,j*r.Height), r.Size));
            }
            return res;
        }
        List<Point> PointsAround(Point pt, int size)
        {
            List<Point> res = new List<Point>();
            size--;
            for (int i = -size; i <= size; i++)
            {
                for (int j = -size; j <= size; j++)
                    res.Add(pt + new Point(i,j));
            }
            return res;
        }

        public void PerformAction(Controller.Action action)
        {
            int amount = 16;
            switch (action)
            {
                case Controller.Action.BOTTOM:
                    Position += new Point(0, amount);
                    break;
                case Controller.Action.TOP:
                    Position += new Point(0, -amount);
                    break;
                case Controller.Action.RIGHT:
                    Position += new Point(amount, 0);
                    break;
                case Controller.Action.LEFT:
                    Position += new Point(-amount, 0);
                    break;
                case Controller.Action.ZOOM_IN:
                    if (tile_size < 32)
                        TileSize++;
                    break;
                case Controller.Action.ZOOM_OUT:
                    if (tile_size > 1)
                        TileSize--;
                    break;
                case Controller.Action.BRUSH_PLUS:
                    if (brush_size < 0x20)
                        BrushSize++;
                    break;
                case Controller.Action.BRUSH_MINUS:
                    if (brush_size > 1)
                        BrushSize--;
                    break;
            }
        }

        Point? initial_mouse_move_pos = null;
        Point? initial_mouse_move_map_position = null;
        public void Update(MouseState mouse, KeyboardState keyboard)
        {
            if (initial_mouse_move_pos != null)
            {
                if (mouse.LeftButton == ButtonState.Released)
                {
                    initial_mouse_move_pos = null;
                    initial_mouse_move_map_position = null;
                }
                else
                {
                    Point offset = initial_mouse_move_pos.Value - mouse.Position;
                    Position = initial_mouse_move_map_position.Value + offset;
                }
            }

            if (keyboard.IsKeyDown(Keys.LeftControl))
            {
                if (initial_mouse_move_pos == null && mouse.LeftButton == ButtonState.Pressed)
                {
                    initial_mouse_move_pos = mouse.Position;
                    initial_mouse_move_map_position = position;
                }
            }
            else
            {
                int index = -1;
                if (mouse.LeftButton == ButtonState.Pressed)
                    index = sprites.Selected;
                if (mouse.RightButton == ButtonState.Pressed)
                    index = 0;
                if (index >= 0 && bounds.Contains(mouse.Position))
                {
                    Point cpt = ScreenCoordToTileCoord(mouse.Position.X, mouse.Position.Y);
                    Rectangle map_bounds = new Rectangle(0, 0, grid.GetLength(1), grid.GetLength(0));
                    foreach (Point pt in PointsAround(cpt, brush_size))
                    {
                        if (map_bounds.Contains(pt))
                            grid[pt.Y, pt.X] = index;
                    }
                }
            }
        }

        public void Draw(SpriteBatch sprite_batch, MouseState mouse)
        {
            sprite_batch.FillRectangle(bounds, Color.CornflowerBlue);
            int h = grid.GetLength(0);
            int w = grid.GetLength(1);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Rectangle dst = TileCoordToScreenRect(x,y);
                    if (dst.Intersects(bounds))
                        sprites.Draw(sprite_batch, grid[y, x], dst);
                }
            }
            // Draw map bounds
            Rectangle map_bounds = Rectangle.Union(TileCoordToScreenRect(0, 0), TileCoordToScreenRect(w-1, h-1));
            // Issue with DrawRectangle: https://github.com/rds1983/Myra/issues/211
            SpriteSet.DrawRectangle(sprite_batch, map_bounds, Color.Red, 2);
            // Draw selected element
            Point pt = ScreenCoordToTileCoord(mouse.Position.X, mouse.Position.Y);
            Rectangle cr = TileCoordToScreenRect(pt.X, pt.Y);//new Rectangle(mouse.Position, new Point(tile_size, tile_size));
            Rectangle union = cr;
            foreach (Rectangle r in RectanglesAround(cr, brush_size))
            {
                union = Rectangle.Union(union, r);
                if (r.Intersects(bounds))
                    sprites.DrawSelected(sprite_batch, r);
            }
            SpriteSet.DrawRectangle(sprite_batch, union, Color.White, 1);
        }
    }
}