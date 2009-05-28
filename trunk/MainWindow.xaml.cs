using System;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using MagiCarver.EnergyFunctions;
using Color=System.Drawing.Color;
using Size=System.Drawing.Size;

namespace MagiCarver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private SeamImage m_SeamImage { get; set; }
        private Constants.Maps ViewEvergyMap { get; set; }
        private bool m_PaintSeam { get; set; }
        private Constants.Direction m_Direction { get; set; }
        private DrawingAttributes Highlighter { get; set; }

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
                           menuItemNormal.IsEnabled = true;
                           menuItemNormal.IsChecked = true;
                           menuItemSaveImage.IsEnabled = true;
                           ToggleHighEng.IsEnabled = true;
                           ToggleLowEng.IsEnabled = true;
                           DoneEditingButton.IsEnabled = true;
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

                            Bitmap bitmap;

                            switch (ViewEvergyMap)
                            {
                                case Constants.Maps.ENERGY:
                                    bitmap = m_SeamImage.EnergyMapBitmap;
                                    break;
                                case Constants.Maps.NORMAL:
                                    bitmap = m_SeamImage.Bitmap;
                                    break;
                                case Constants.Maps.HORIZONTAL_INDEX:
                                    bitmap = m_SeamImage.HorizontalIndexMap;
                                    break;
                                case Constants.Maps.VERTICAL_INDEX:
                                    bitmap = m_SeamImage.VerticalIndexMap;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

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

            Width = bitmap.Width;
            Height = bitmap.Height + theStatusBar.ActualHeight + theMenu.ActualHeight + theToolbar.ActualHeight;

        }

        private void InkCanvas_Initialized(object sender, EventArgs e)
        {
            Highlighter = new DrawingAttributes
                                                  {
                                                      Color = Colors.Yellow,
                                                      IsHighlighter = true,
                                                      IgnorePressure = true,
                                                      StylusTip = StylusTip.Ellipse,
                                                      Height = 7,
                                                      Width = 7
                                                  };

            theCanvas.EditingMode = InkCanvasEditingMode.None;
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

        private void Carve_Clicked(object sender, RoutedEventArgs e)
        {
            menuItemCarve.IsEnabled = false;
            menuItemAddSeam.IsEnabled = false;
            txtStatus.Text = Constants.TEXT_WORKING;

            Thread t1 = new Thread(() => m_SeamImage.Carve(m_Direction, m_PaintSeam, 500));

            t1.Start();

        }

        private void ChangeMapView_Clicked(object sender, RoutedEventArgs e)
        {
            foreach (MenuItem menuItem in ((MenuItem)((MenuItem)sender).Parent).Items)
            {
                menuItem.IsChecked = menuItem == sender;

                if (menuItem.IsChecked)
                {
                    ViewEvergyMap = (Constants.Maps)int.Parse(menuItem.Tag.ToString());
                }
            }

            m_SeamImage_ImageChanged(null, null);
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

        private void RowDefinition_Loaded(object sender, RoutedEventArgs e)
        {
            RowDefinition rowDefinition = ((RowDefinition) sender);

            rowDefinition.MaxHeight = rowDefinition.ActualHeight;
            rowDefinition.MinHeight = rowDefinition.ActualHeight;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged && (e.NewSize.Width - e.PreviousSize.Width < 0))
            {
                ((Window) sender).Width = e.PreviousSize.Width - 1;
            }
        }

        private void ToggleBrush_Clicked(object sender, RoutedEventArgs e)
        {
            if (sender == ToggleHighEng)
            {
                if (ToggleHighEng.IsChecked == true)
                {
                    ToggleLowEng.IsChecked = false;
                    Highlighter.Color = Colors.Yellow;
                    theCanvas.DefaultDrawingAttributes = Highlighter;
                    theCanvas.EditingMode = InkCanvasEditingMode.Ink;
                }
            }else
            {
                if (ToggleLowEng.IsChecked == true)
                {
                    ToggleHighEng.IsChecked = false;
                    Highlighter.Color = Colors.Green;
                    theCanvas.DefaultDrawingAttributes = Highlighter;
                    theCanvas.EditingMode = InkCanvasEditingMode.Ink;
                }
            }

            if ((ToggleHighEng.IsChecked == false) && (ToggleLowEng.IsChecked == false))
            {
                theCanvas.EditingMode = InkCanvasEditingMode.None;
            }
        }

        private void DoneEditing_Clicked(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure?", "Finished Editing", MessageBoxButton.YesNo, MessageBoxImage.Question,
                            MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                ToggleHighEng.IsEnabled = false;
                ToggleLowEng.IsEnabled = false;
                ToggleHighEng.IsChecked = false;
                ToggleLowEng.IsChecked = false;
                DoneEditingButton.IsEnabled = false;

                m_SeamImage.SetEnergy(theCanvas.Strokes);
            }
        }
    }
}
