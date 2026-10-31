using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.LZO;

using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;


namespace tmcnt_gui.MapProcessing;


public class MapHandler {
    private Gbx<CGameCtnChallenge>? gbx = null;
    public CGameCtnChallenge? Map = null;

    private Int3 MaxValues;
    private Int3 MinValues;
    private Int3 Average;

    private string Filepath = "";

    public bool is_empty() {
        return (this.Map is null) ? true : false;
    }

    private void CalculateAverageAndLimits() {
        if (this.is_empty()) {return;}

        Int3 max = new Int3(0, 0, 0);
        Int3 min = this.Map.Size;
        List<int> xValues = new List<int>();
        List<int> zValues = new List<int>();

        foreach (var block in this.Map!.GetBlocks()) {
            xValues.Add(block.Coord.X);
            zValues.Add(block.Coord.Z);
            if (block.Coord.X > max.X) {max = new Int3(block.Coord.X, max.Y, max.Z);}
            if (block.Coord.X < min.X) {min = new Int3(block.Coord.X, min.Y, min.Z);}
            if (block.Coord.Y > max.Y) {max = new Int3(max.X, block.Coord.Y, max.Z);}
            if (block.Coord.Y < min.Y) {min = new Int3(min.X, block.Coord.Y, min.Z);}
            if (block.Coord.Z > max.Z) {max = new Int3(max.X, max.Y, block.Coord.Z);}
            if (block.Coord.Z < min.Z) {min = new Int3(min.X, min.Y, block.Coord.Z);}
        }
        this.MaxValues = max;
        this.MinValues = min;
        this.Average = new Int3((int)xValues.Average(), 0, (int)zValues.Average());
    }

    public void LoadMap(string filepath) {
        this.Filepath = filepath;
        this.gbx = Gbx.Parse<CGameCtnChallenge>(filepath);
        this.Map = this.gbx.Node;
        this.CalculateAverageAndLimits();
    }

    public Int3 RequiredCenterOffset() {
        if (this.is_empty()) {return new Int3(0, 0, 0);}
        Int3 offset = new Int3((int)this.Map.Size.X/2 - this.Average.X, 0, (int)this.Map.Size.Z/2 - this.Average.Z);
        return this.ClampOffset(offset);
    }

    public Int3 ClampOffset(Int3 offset) {
        if (this.is_empty()) {return offset;}
        offset = new Int3(Math.Clamp(offset.X, -this.MinValues.X, this.Map.Size.X - this.MaxValues.X),
                          Math.Clamp(offset.Y, -this.MinValues.Y, this.Map.Size.Y - this.MaxValues.Y),
                          Math.Clamp(offset.Z, -this.MinValues.Z, this.Map.Size.Z - this.MaxValues.Z));
        return offset;
    }

    public void ShiftMap(Int3 offset) {
        if (this.is_empty()) {return;}
        offset = this.ClampOffset(offset);
        foreach (var block in this.Map.Blocks) {block.Coord += offset;}
        this.SaveMap();
    }

    public void SaveMap() {
        if (this.is_empty()) {return;}

        this.Map.AuthorTime = null;
        this.gbx.Save(this.Filepath);
    }

    public MapHandler() {
        Gbx.LZO = new Lzo();
    }

    public void Clear() {
        this.gbx = null;
        this.Map = null;
    }
}


public class MapGridControl : Control {
    private HashSet<Int2> BlockPositions = new HashSet<Int2>();

    private Int2 Offset = new Int2(0, 0);
    private Int2 Size = new Int2(0, 0);

    public void LoadMap(CGameCtnChallenge Map) {
        foreach (var block in Map.GetBlocks()) {this.BlockPositions.Add(new Int2(block.Coord.X, block.Coord.Z));}
        this.Size = new Int2(Map.Size.X, Map.Size.Z);
        this.Offset = new Int2(0, 0);
    }

    public void SetOffset(Int3 offset) {
        this.Offset = new Int2(offset.X, offset.Z);
    }

    public void Refresh() {
        InvalidateVisual();
    }

    public void ClearData() {
        this.Offset = new Int2(0, 0);
        this.Size = new Int2(0, 0);
        this.BlockPositions = new HashSet<Int2>();
    }

    public override void Render(Avalonia.Media.DrawingContext context) {
        base.Render(context);
        if (this.BlockPositions.Count <= 0) {return;}

        const double padding = 10;

        int CellSize = (int)Math.Min(
            (Bounds.Width - padding * 2) / this.Size.X,
            (Bounds.Height - padding * 2) / this.Size.Y
        );

        if (CellSize <= 0) {return;}

        Int2 PixelOffset = new Int2(CellSize*this.Offset.X, CellSize*this.Offset.Y);

        var fill = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#888888"));
        var outline = new Avalonia.Media.Pen(Avalonia.Media.Brushes.Black, 0.5);

        foreach (Int2 BlockPosition in this.BlockPositions) {
            int x = PixelOffset.X + BlockPosition.X * CellSize;
            int y = PixelOffset.Y + BlockPosition.Y * CellSize;

            context.DrawRectangle(fill, outline, new Avalonia.Rect(x, y, CellSize, CellSize));
        }
    }
}