using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

using System.Diagnostics;
using tmcnt_gui.MapProcessing;
using GBX.NET; //gives access to Int3

namespace tmcnt_gui.Views;

public partial class MainWindow : Window
{
    private Int3 offset = new Int3(0, 0, 0);
    private MapHandler mapHandler = new MapHandler();

    public MainWindow() {
        InitializeComponent();
    }

    private void UpdateGridDisplayOffset() {
        GridDisplay.SetOffset(this.offset);
        GridDisplay.Refresh();
    }

    private void UpdateGridDisplayMap() {
        GridDisplay.LoadMap(this.mapHandler.Map);
        this.UpdateGridDisplayOffset();
    }


    private async void OpenMap(object? sender, RoutedEventArgs e) {
        var topLevel = TopLevel.GetTopLevel(this);

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions{
                Title="Choose Map", AllowMultiple=false, FileTypeFilter= 
                    new[] {new FilePickerFileType("GBX Challenge File (*.challenge.gbx)")
                    {Patterns = new[] {"*.Challenge.Gbx"}, AppleUniformTypeIdentifiers = new[] { "com.nadeo.gbx" }}
                    }
                }
            );

        if (files.Count >= 1) {
            IStorageFile file = files[0];
            string? filepath = file.TryGetLocalPath();
            if (filepath != null) {
                this.ClearData();
                // Open Map and set up MapHandler
                this.mapHandler.LoadMap(filepath);
                FileDescriptionLabel.Text = file.Name;
                this.UpdateGridDisplayMap();
            }
        }
    }

    private void SetOffset(Int3 offset) {
        this.offset = offset;
        offset = this.mapHandler.ClampOffset(offset);
        OffsetLabel.Text = $"{offset.X} {offset.Y} {offset.Z}";
        this.UpdateGridDisplayOffset();
    }

    private void ClearData() {
        this.SetOffset(new Int3(0, 0, 0));
        X_in.Text = "";
        Y_in.Text = "";
        Z_in.Text = "";
        FileDescriptionLabel.Text = "Map Saved!";
        this.mapHandler.Clear();
        GridDisplay.ClearData();
    }

    private void SetOffsetValues(object? sender, RoutedEventArgs e) {
        string x_raw = (X_in.Text == "" || X_in.Text == null) ? "0" : X_in.Text;
        string y_raw = (Y_in.Text == "" || Y_in.Text == null) ? "0" : Y_in.Text;
        string z_raw = (Z_in.Text == "" || Z_in.Text == null) ? "0" : Z_in.Text;

        if (int.TryParse(x_raw, out int x) && int.TryParse(y_raw, out int y) && int.TryParse(z_raw, out int z)) {
            this.SetOffset(new Int3(x, y, z));
        }
        else {
            OffsetLabel.Text = $"Invalid Offset";
        }
    }

    private void SetOffsetAuto(object? sender, RoutedEventArgs e) {
        Int3 CenterOffset = this.mapHandler.RequiredCenterOffset();
        this.SetOffset(CenterOffset);
    }

    private void ShiftAndSave(object? sender, RoutedEventArgs e) {
        this.mapHandler.ShiftMap(this.offset);
        this.mapHandler.SaveMap();
        this.ClearData();
    }
}