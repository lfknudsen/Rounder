using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Rounder;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
    }


    private Image? InputImage => field ??= (Image?)TryFindResource("ImgInput");
    private Image? OutputImage => field ??= (Image?)TryFindResource("ImgOutput");

    private BitmapImage? InputImageSrc {
        get {
            return field ??= InputImage?.Source as BitmapImage;
        }
        set;
    }

    private WriteableBitmap? OutputImageSrc { get; set; }

    private string _recentDirectory = Environment.CurrentDirectory;

    private void OpenFile(object sender, RoutedEventArgs e) {
        var dialogue = new OpenFileDialog {
            Multiselect = true,
            CheckFileExists = true,
            DefaultDirectory = _recentDirectory,
            Title = "Open file."
        };
        bool? result = dialogue.ShowDialog();
        if (!result.HasValue || !result.Value) {
            return;
        }

        _recentDirectory = dialogue.InitialDirectory;
        try {
            var png = new PngBitmapDecoder(new Uri(dialogue.FileName, UriKind.RelativeOrAbsolute),
                                           BitmapCreateOptions.None, BitmapCacheOption.Default);
            InputImage?.Source = png.Frames[0];
            InputImage?.Stretch = Stretch.Uniform;
            InputImage?.InvalidateVisual();
            OutputImage?.Source = png.Frames[0];
            OutputImage?.Stretch = Stretch.Uniform;
            OutputImage?.InvalidateVisual();
        } catch (Exception ex) {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RoundOffCorners(object sender, RoutedEventArgs e) {
        if (InputImage == null) {
            Console.Error.WriteLine("Input image or sources are not set.");
            return;
        }

        if (InputImage.Source is not BitmapSource src) {
            Console.Error.WriteLine("Input image source is not set.");
            return;
        }

        PixelFormat oldFormat = src.Format;
        if (oldFormat.BitsPerPixel >= PixelFormats.Bgra32.BitsPerPixel) {
            OutputImageSrc = new WriteableBitmap(src);
        } else {
            OutputImageSrc = new WriteableBitmap(src.PixelWidth, src.PixelHeight,
                                                 src.DpiX, src.DpiY,
                                                 PixelFormats.Bgra32, src.Palette);
            byte[] pixels = new byte[4 * 8 * src.PixelWidth * src.PixelWidth];
            src.CopyPixels(pixels, 8 * src.PixelWidth, 0);
            OutputImageSrc.WritePixels(new Int32Rect(0, 0, src.PixelWidth, src.PixelHeight),
                                       pixels,
                                       8 * src.PixelWidth, 0);
        }

        OutputImageSrc.EraseBorder();
        OutputImageSrc.SetHorizontal(0, 1, 5, Colour.Transparent);
        OutputImageSrc.SetHorizontal(-1, 1, -5, Colour.Transparent);
        OutputImageSrc.SetHorizontal(0, -2, 5, Colour.Transparent);
        OutputImageSrc.SetHorizontal(-1, -2, -5, Colour.Transparent);
        OutputImageSrc.SetVertical(1, 0, 5, Colour.Transparent);
        OutputImageSrc.SetVertical(-2, 0, 5, Colour.Transparent);
        OutputImageSrc.SetVertical(1, -1, -5, Colour.Transparent);
        OutputImageSrc.SetVertical(-2, -1, -5, Colour.Transparent);
        OutputImageSrc.SetPixel(2, 2, Colour.Transparent);
        OutputImageSrc.SetPixel(-3, 2, Colour.Transparent);
        OutputImageSrc.SetPixel(-3, -3, Colour.Transparent);
        OutputImageSrc.SetPixel(2, -3, Colour.Transparent);
        OutputImage?.Source = OutputImageSrc;
        OutputImage?.InvalidateVisual();
    }

    private void SaveFile(object sender, RoutedEventArgs e) {
        if (OutputImageSrc == null) {
            Console.Error.WriteLine("Output image source is not set.");
            return;
        }

        var dialogue = new SaveFileDialog() {
            Title = "Save file.",
            DefaultDirectory = _recentDirectory,
            DefaultExt = ".png",
            OverwritePrompt = true,
        };
        bool? result = dialogue.ShowDialog();
        if (!result.HasValue || !result.Value) {
            return;
        }

        try {
            using Stream outStream = dialogue.OpenFile();
            var png = new PngBitmapEncoder();
            var frame = BitmapFrame.Create(OutputImageSrc);
            png.Frames.Add(frame);
            png.Save(outStream);
            outStream.Flush();
            outStream.Close();
        } catch (Exception ex) {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
