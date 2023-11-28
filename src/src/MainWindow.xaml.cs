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
        private WriteableBitmap sprayOverlay;
        private bool isSprayPainting = false;
        private bool isErasing = false;
        private Color selectedColor = Colors.Black;
        private Random random = new Random();
        private static int maxDensity = 50;
        private int sprayOpacity = (int)(0.5 * maxDensity);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void DrawOnCanvas(Point position, bool isErasing)
        {
            if (sprayOverlay == null) return;

            // mapping from canvas to bitmap
            position.X = position.X / PaintCanvas.Width * sprayOverlay.PixelWidth;
            position.Y = position.Y / PaintCanvas.Height * sprayOverlay.PixelHeight;

            int radius = 10; // Size of the spray area

            sprayOverlay.Lock();

            if (isErasing)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (x * x + y * y <= radius * radius)
                        {
                            int pixelX = (int)position.X + x;
                            int pixelY = (int)position.Y + y;

                            // Ensure the coordinates are within the bounds of the sprayOverlay
                            if (pixelX >= 0 && pixelX < sprayOverlay.PixelWidth && pixelY >= 0 && pixelY < sprayOverlay.PixelHeight)
                            {
                                sprayOverlay.WritePixels(new Int32Rect(pixelX, pixelY, 1, 1), new int[] { 0 }, 4, 0); // 0 for transparent
                            }
                        }
                    }
                }
            }
            else
            {
                int density = sprayOpacity; // Density of the spray
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
                    if (x >= 0 && x < sprayOverlay.PixelWidth && y >= 0 && y < sprayOverlay.PixelHeight)
                    {
                        Color color = isErasing ? Colors.Transparent : selectedColor;
                        int colorData = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;

                        // Apply the color to the pixel
                        sprayOverlay.WritePixels(new Int32Rect(x, y, 1, 1), new[] { colorData }, 4, 0);
                    }
                }
            }
            sprayOverlay.Unlock();
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
                sprayOverlay = new WriteableBitmap(bitmap.PixelWidth, bitmap.PixelHeight, 96, 96, PixelFormats.Pbgra32, null);
                writableImage = new WriteableBitmap(bitmap);
                LoadedImage.Source = writableImage;
                LoadedImage.Width = writableImage.PixelWidth;
                LoadedImage.Height = writableImage.PixelHeight;
                LoadedImage.VerticalAlignment = VerticalAlignment.Center;
                LoadedImage.HorizontalAlignment = HorizontalAlignment.Center;
                PaintCanvas.Width = writableImage.PixelWidth;
                PaintCanvas.Height = writableImage.PixelHeight;
                PaintCanvas.VerticalAlignment = VerticalAlignment.Center;
                PaintCanvas.HorizontalAlignment = HorizontalAlignment.Center;  
            }
        }

        private void UpdateDisplay()
        {
            if (writableImage == null || sprayOverlay == null) return;

            // Create a temporary bitmap to blend the overlay with the original image
            var combinedImage = new WriteableBitmap(writableImage);
            combinedImage.Lock();
            combinedImage.Blit(new Rect(0, 0, writableImage.PixelWidth, writableImage.PixelHeight), sprayOverlay, new Rect(0, 0, sprayOverlay.PixelWidth, sprayOverlay.PixelHeight));
            combinedImage.Unlock();

            // Display the combined image
            LoadedImage.Source = combinedImage;
        }


        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                UpdateDisplay();
                var combinedImage = (BitmapSource)LoadedImage.Source;
                using (var fileStream = new System.IO.FileStream(saveFileDialog.FileName, System.IO.FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(combinedImage));
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
                    UpdateDisplay();
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
                UpdateDisplay();
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
            sprayOpacity = (int)(e.NewValue *  maxDensity); // Convert to a scale of 0-50
        }
    }
}