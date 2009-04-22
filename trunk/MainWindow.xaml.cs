using System;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using MagiCarver.EnergyFunctions;
using Size=System.Drawing.Size;

namespace MagiCarver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private SeamImage m_SeamImage { get; set; }
        private bool ViewEvergyMap { get; set; }
        private bool m_PaintSeam { get; set; }
        private Constants.Direction m_Direction { get; set; }

        private delegate void VoidDelegate();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenFile_Clicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.ShowDialog();

            if (String.IsNullOrEmpty(openFile.FileName))
            {
                return;
            }

            txtStatus.Text = Constants.TEXT_WORKING;

            Thread t1 = new Thread(delegate()
               {
                   Bitmap bitmap = new Bitmap(openFile.FileName);

                   m_SeamImage = new SeamImage(bitmap, new Sobel());
                   m_SeamImage.ImageChanged += m_SeamImage_ImageChanged;
                   m_SeamImage.OperationCompleted += m_SeamImage_OperationCompleted;
                   m_SeamImage.ColorSeam += m_SeamImage_ColorSeam;

                   Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)delegate
                       {
                           SetImageSource (bitmap);
                           theCanvas.IsEnabled = true;
                           menuItemEnergyMap.IsEnabled = true;
                           menuItemEnergyMap.IsChecked = false;
                           ViewEvergyMap = false;
                           menuItemSaveImage.IsEnabled = true;
                           menuItemCarve.IsEnabled = true;
                           menuItemAddSeam.IsEnabled = true;
                           theCanvas.Strokes.Clear();
                           Title = Constants.TITLE + " - " + openFile.SafeFileName;
                           txtStatus .Text =  Constants.TEXT_READY;
                       });
               });
            
            t1.Start();
        }

        private void m_SeamImage_ColorSeam(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate) delegate
                {
                    Stroke seamStroke = new Stroke(new StylusPointCollection(((SeamPointArgs) e).Points))
                        {
                            DrawingAttributes = { Color = System.Windows.Media.Color.FromRgb(255, 0, 0), Height = 1, Width = 1}
                        };
                                                                                    
                    theCanvas.Strokes.Add(seamStroke);
                });
        }

        private void m_SeamImage_OperationCompleted(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)delegate
            {
                menuItemCarve.IsEnabled = true;
                menuItemAddSeam.IsEnabled = true;
                txtStatus.Text = Constants.TEXT_READY;
                theCanvas.Strokes.Clear();
            });
        }

        private void m_SeamImage_ImageChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate) delegate
                        {
                             Bitmap bitmap = ViewEvergyMap ? m_SeamImage.EnergyMapBitmap : m_SeamImage. Bitmap;

                             SetImageSource(bitmap);
                        });   
        }

        private void SetImageSource(Bitmap bitmap)
        {
            theImage.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(),
                                                                                           IntPtr.Zero, Int32Rect.Empty,
                                                                                           BitmapSizeOptions.
                                                                                               FromWidthAndHeight(
                                                                                               bitmap.Width,
                                                                                               bitmap.Height));

            Size size = m_SeamImage.ImageSize;

            txtResolution.Text = size.Width + " x " + size.Height;
        }

        private void InkCanvas_Initialized(object sender, EventArgs e)
        {
            DrawingAttributes highlighter = new DrawingAttributes
                                                  {
                                                      Color = Colors.Yellow,
                                                      IsHighlighter = true,
                                                      IgnorePressure = true,
                                                      StylusTip = StylusTip.Ellipse,
                                                      Height = 7,
                                                      Width = 7
                                                  };

            theCanvas.DefaultDrawingAttributes = highlighter;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            m_Direction = Constants.Direction.OPTIMAL;
            m_PaintSeam = true;

            Title = Constants.TITLE;
        }

        private void ClearSelection_Click(object sender, RoutedEventArgs e)
        {
            theCanvas.Strokes.Clear();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFile = new SaveFileDialog {Filter = "Image files (*.png)|*.png"};
            saveFile.ShowDialog();
            if (!String.IsNullOrEmpty(saveFile.FileName))
            {
                Utilities.ExportToPng(new Uri(saveFile.FileName, UriKind.Absolute), theImage);
            }
        }

        private void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            ((ScrollViewer)sender).MaxHeight = SystemParameters.WorkArea.Size.Height * Constants.PERCANTAGE_OF_SCREEN_HEIGHT;
            ((ScrollViewer)sender).MaxWidth = SystemParameters.WorkArea.Size.Width * Constants.PERCANTAGE_OF_SCREEN_WIDTH;
        }

        private void Carve_Clicked(object sender, RoutedEventArgs e)
        {
            menuItemCarve.IsEnabled = false;
            menuItemAddSeam.IsEnabled = false;
            txtStatus.Text = Constants.TEXT_WORKING;

            Thread t1 = new Thread(() => m_SeamImage.Carve(m_Direction, m_PaintSeam, 100));

            t1.Start();

        }

        private void ViewEnergyMap_Clicked(object sender, RoutedEventArgs e)
        {
            ViewEvergyMap = ((MenuItem)sender).IsChecked;

            m_SeamImage_ImageChanged(this, EventArgs.Empty);
        }

        private void ChangeDirection_Clicked(object sender, RoutedEventArgs e)
        {
            foreach (MenuItem menuItem in ((MenuItem)((MenuItem)sender).Parent).Items)
            {
                menuItem.IsChecked = menuItem == sender;

                if (menuItem.IsChecked)
                {
                    m_Direction = (Constants.Direction) int.Parse(menuItem.Tag.ToString());
                }
            }
        }

        private void PaintSeam_Clicked(object sender, RoutedEventArgs e)
        {
            m_PaintSeam = ((MenuItem) sender).IsChecked;
        }

        private void About_Clicked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Under Construction.");
        }

        private void AddSeam_Clicked(object sender, RoutedEventArgs e)
        {
            menuItemCarve.IsEnabled = false;
            menuItemAddSeam.IsEnabled = false;
            txtStatus.Text = Constants.TEXT_WORKING;

            Thread t1 = new Thread(delegate()
            {
                Size minimumSize = new Size(m_SeamImage.ImageSize.Width + 5, m_SeamImage.ImageSize.Height + 5);
                m_SeamImage.AddSeam(m_Direction, minimumSize, m_PaintSeam);
            });

            t1.Start();
        }
    }
}
