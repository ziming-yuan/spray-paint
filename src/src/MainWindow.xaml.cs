﻿using Microsoft.Win32;
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
            SprayIndicator.Fill = new SolidColorBrush(selectedColor);
        }
        private void MoveSprayIndicator(Point position)
        {
            // Adjust the position to center the indicator on the mouse position
            double left = position.X - (SprayIndicator.Width / 2);
            double top = position.Y - (SprayIndicator.Height / 2);

            Canvas.SetLeft(SprayIndicator, left);
            Canvas.SetTop(SprayIndicator, top);
        }

        private void DrawOnCanvas(Point position, bool isErasing)
        {
            if (writableImage == null)
            {
                return; // Exit the method if no image is loaded
            }
            int radius = 1; // Size of the spray area
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
                    int colorData = color.R << 16; // R
                    colorData |= color.G << 8;     // G
                    colorData |= color.B << 0;     // B

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
                LoadedImage.Source = writableImage; // Display the image in the Image control
                                                    // Set the size of the Canvas to match the image
                PaintCanvas.Width = writableImage.Width;
                PaintCanvas.Height = writableImage.Height;
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
                    // Show the spray indicator
                    SprayIndicator.Visibility = Visibility.Visible;
                    MoveSprayIndicator(position);
                    DrawOnCanvas(position, isErasing);
                }
            }
        }


        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point position = e.GetPosition(PaintCanvas);
                MoveSprayIndicator(position);

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
            SprayIndicator.Visibility = Visibility.Collapsed;
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
            // Assuming the slider's value is already between 0 (transparent) and 1 (opaque)
            // If your slider's range is different, you need to adjust the range accordingly.
            sprayOpacity = (int)(e.NewValue * 255); // Convert to a scale of 0-255
        }

        private void ColorPicker_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (ColorPicker.SelectedItem is ComboBoxItem comboBoxItem && comboBoxItem.Background is SolidColorBrush brush)
            {
                selectedColor = brush.Color;
                // Update the spray indicator color
                SprayIndicator.Fill = new SolidColorBrush(selectedColor) { Opacity = 0.3 };
                selectedColor.A = (byte)sprayOpacity; // Set the alpha channel for the color
            }
        }
    }
}