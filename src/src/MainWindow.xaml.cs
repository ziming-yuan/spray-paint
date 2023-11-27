using Microsoft.Win32;
using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace src
{
    public partial class MainWindow : Window
    {
        private WriteableBitmap writableImage;
        private bool isSprayPainting = false;
        private bool isErasing = false;
        private Color selectedColor = Colors.Black;
        private Random random = new Random();
        private int sprayOpacity;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void DrawOnCanvas(Point position, bool isErasing)
        {
            if (writableImage == null)
            {
                return; // Exit the method if no image is loaded
            }
            int radius = 10; // Size of the spray area
            int density = 100; // Density of the spray

            writableImage.Lock(); // Lock the bitmap for writing.

            int numParticles = (int)(density * 3.14 * radius * radius / 100);

            for (int i = 0; i < numParticles; ++i)
            {
                // Generate random polar coordinates
                double theta = random.NextDouble() * (Math.PI * 2);
                double r = random.NextDouble() * radius;

                // Convert polar coordinates to Cartesian coordinates
                int x = (int)(position.X + Math.Cos(theta) * r);
                int y = (int)(position.Y + Math.Sin(theta) * r);

                // Ensure the coordinates are within the bounds of the writableImage
                if (x >= 0 && x < writableImage.PixelWidth && y >= 0 && y < writableImage.PixelHeight)
                {
                    // Determine the color
                    Color color = isErasing ? Colors.Transparent : selectedColor;
                    int colorData = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;

                    // Apply the color to the pixel
                    writableImage.WritePixels(new Int32Rect(x, y, 1, 1), new[] { colorData }, 4, 0);
                }
            }

            writableImage.Unlock(); // Unlock the bitmap after writing.
        }


        private void OpenImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.bmp, *.jpg, *.jpeg, *.png)|*.bmp;*.jpg;*.jpeg;*.png"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                writableImage = new WriteableBitmap(bitmap);
                LoadedImage.Source = writableImage;
                LoadedImage.VerticalAlignment = VerticalAlignment.Center;
                LoadedImage.HorizontalAlignment = HorizontalAlignment.Center;
                PaintCanvas.Width = writableImage.Width;
                PaintCanvas.Height = writableImage.Height;
                PaintCanvas.VerticalAlignment = VerticalAlignment.Center;
                PaintCanvas.HorizontalAlignment = HorizontalAlignment.Center;
            }
        }


        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                using (var fileStream = new System.IO.FileStream(saveFileDialog.FileName, System.IO.FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(writableImage));
                    encoder.Save(fileStream);
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void SprayButton_Click(object sender, RoutedEventArgs e)
        {
            isSprayPainting = true;
            isErasing = false;
        }

        private void EraseButton_Click(object sender, RoutedEventArgs e)
        {
            isSprayPainting = false;
            isErasing = true;
        }
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                PaintCanvas.CaptureMouse();
                Point position = e.GetPosition(PaintCanvas);

                if (isSprayPainting || isErasing)
                {
                    DrawOnCanvas(position, isErasing);
                }
            }
        }


        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point position = e.GetPosition(PaintCanvas);

                if (isSprayPainting)
                {
                    DrawOnCanvas(position, false);
                }
                else if (isErasing)
                {
                    DrawOnCanvas(position, true);
                }
            }
        }
        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            PaintCanvas.ReleaseMouseCapture();
        }

        private void ColorPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ColorPicker.SelectedItem is ComboBoxItem comboBoxItem && comboBoxItem.Background is SolidColorBrush brush)
            {
                selectedColor = brush.Color;
            }
        }

        private void DensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Slider's value is between 0 (transparent) and 1 (opaque)
            sprayOpacity = (int)(e.NewValue * 255); // Convert to a scale of 0-255
        }

        private void ColorPicker_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (ColorPicker.SelectedItem is ComboBoxItem comboBoxItem && comboBoxItem.Background is SolidColorBrush brush)
            {
                selectedColor = brush.Color;
                selectedColor.A = (byte)sprayOpacity; // Set the alpha channel for the color
            }
        }
    }
}