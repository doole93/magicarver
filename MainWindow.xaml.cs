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
    public partial class MainWindow
    {
        #region Delegates

        private delegate void VoidDelegate();

        #endregion

        #region Properties

        private SeamImage           SeamImage { get; set; }
        private Constants.Maps      CurrentViewBitmap { get; set; }
        private bool                PaintSeam { get; set; }
        private Constants.Direction Direction { get; set; }
        private DrawingAttributes   Highlighter { get; set; }
        private bool                CanOperate { get; set; }

        #endregion

        #region CTors

        public MainWindow()
        {
            InitializeComponent();

            Direction = Constants.Direction.OPTIMAL;
            PaintSeam = false;
        }

        #endregion

        #region Event Handlers

        private void OpenFile_Clicked(object sender, RoutedEventArgs e)
        {
            SizeChanged -= Window_SizeChanged;
            SizeChanged -= Window_DummySizeChanged;

            menuItemEnergyMap.IsChecked = false;
            menuItemNormal.IsChecked = true;
            CurrentViewBitmap = Constants.Maps.NORMAL;

            OpenFileDialog openFile = new OpenFileDialog();
            openFile.ShowDialog();

            if (String.IsNullOrEmpty(openFile.FileName))
            {
                return;
            }

            WorkInProgress(true);

            Thread t1 = new Thread(delegate()
               {
                   Bitmap bitmap = null;

                   try
                   {
                       bitmap = new Bitmap(openFile.FileName); 
                   }catch (ArgumentException)
                   {
                       MessageBox.Show("Invalid file selected. Please select a valid image file.", "Open File Error",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                   }

                   if (bitmap != null)
                   {
                       SeamImage = new SeamImage(bitmap, new Sobel());

                       CanOperate = false;
                       

                       SeamImage.ImageChanged += m_SeamImage_ImageChanged;
                       SeamImage.OperationCompleted += m_SeamImage_OperationCompleted;
                       SeamImage.ColorSeam += m_SeamImage_ColorSeam;
                   }
                   
                   Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)delegate
                       {
                           ResizeMode = ResizeMode.NoResize;


                           if (bitmap != null)
                           {
                               SetImageSource(bitmap, openFile.SafeFileName);
                               WorkInProgress(false);
                           }else
                           {
                               Title = Constants.TEXT_READY;
                           }
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
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(() => WorkInProgress(false)));
        }

        private void m_SeamImage_ImageChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate) delegate
                        {
                            Bitmap bitmap;

                            switch (CurrentViewBitmap)
                            {
                                case Constants.Maps.ENERGY:
                                    bitmap = SeamImage.EnergyMapBitmap;
                                    break;
                                case Constants.Maps.NORMAL:
                                    bitmap = SeamImage.Bitmap;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                             SetImageSource(bitmap, null);
                        });   
        }

        private void InkCanvas_Initialized(object sender, EventArgs e)
        {
            Highlighter = new DrawingAttributes
                              {
                                  IsHighlighter = true,
                                  StylusTip = StylusTip.Rectangle,
                                  Height = 25,
                                  Width = 25
                              };

            theCanvas.DefaultDrawingAttributes = Highlighter;
            theCanvas.EditingMode = InkCanvasEditingMode.None;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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
            SizeChanged -= Window_SizeChanged;

            WorkInProgress(true);

            Thread t1 = new Thread(delegate() {
                SeamImage.Carve(Direction, PaintSeam, 16);
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(delegate { WorkInProgress(false); SizeChanged += Window_DummySizeChanged; }));
            });

            t1.Start();
        }

        private void ChangeMapView_Clicked(object sender, RoutedEventArgs e)
        {
            foreach (MenuItem menuItem in ((MenuItem)((MenuItem)sender).Parent).Items)
            {
                menuItem.IsChecked = menuItem == sender;

                if (menuItem.IsChecked)
                {
                    CurrentViewBitmap = (Constants.Maps)int.Parse(menuItem.Tag.ToString());
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
                    Direction = (Constants.Direction) int.Parse(menuItem.Tag.ToString());
                }
            }
        }

        private void PaintSeam_Clicked(object sender, RoutedEventArgs e)
        {
            PaintSeam = ((MenuItem) sender).IsChecked;
        }

        private void About_Clicked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Under Construction.");
        }

        private void AddSeam_Clicked(object sender, RoutedEventArgs e)
        {
            SizeChanged -= Window_SizeChanged;

            WorkInProgress(true);

            Thread t1 = new Thread(delegate()
            {
                SeamImage.Add(Direction, PaintSeam, 50);
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(delegate { WorkInProgress(false); SizeChanged += Window_DummySizeChanged; }));
            });

            t1.Start();


        }

        private void Window_DummySizeChanged(object sender, SizeChangedEventArgs e)
        {
            SizeChanged -= Window_DummySizeChanged;
            SizeChanged += Window_SizeChanged;
        }

        private void RowDefinition_Loaded(object sender, RoutedEventArgs e)
        {
            RowDefinition rowDefinition = ((RowDefinition) sender);

            rowDefinition.MaxHeight = rowDefinition.ActualHeight;
            rowDefinition.MinHeight = rowDefinition.ActualHeight;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged && CanOperate && e.PreviousSize.Width > e.NewSize.Width && SeamImage.ImageSize.Width > e.PreviousSize.Width - e.NewSize.Width && (int)(e.PreviousSize.Width - e.NewSize.Width) > 0)
            {
                WorkInProgress(true);

                Thread t1 = new Thread(delegate()
                {
                    SeamImage.Carve(Constants.Direction.VERTICAL, false, (int)(e.PreviousSize.Width - e.NewSize.Width));
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(() => WorkInProgress(false)));
                });

                t1.Start();

                t1.Join();
            }
            else if (e.HeightChanged && CanOperate && e.PreviousSize.Height > e.NewSize.Height && SeamImage.ImageSize.Height > e.PreviousSize.Height - e.NewSize.Height && (int)(e.PreviousSize.Height - e.NewSize.Height) > 0)
            {
                WorkInProgress(true);

                Thread t1 = new Thread(delegate()
                {
                    SeamImage.Carve(Constants.Direction.HORIZONTAL, false, (int)(e.PreviousSize.Height - e.NewSize.Height));
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(() => WorkInProgress(false)));
                });

                t1.Start();

                t1.Join();
            }
            else if (e.WidthChanged && CanOperate && e.PreviousSize.Width < e.NewSize.Width && (int)(e.NewSize.Width - e.PreviousSize.Width) > 0)
            {
                WorkInProgress(true);

                Thread t1 = new Thread(delegate()
                {
                    SeamImage.Add(Constants.Direction.VERTICAL, false, (int)(e.NewSize.Width - e.PreviousSize.Width));
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(() => WorkInProgress(false)));
                });

                t1.Start();

                t1.Join();
            }
            else if (e.HeightChanged && CanOperate && e.PreviousSize.Height < e.NewSize.Height && (int)(e.NewSize.Height - e.PreviousSize.Height) > 0)
            {
                WorkInProgress(true);

                Thread t1 = new Thread(delegate()
                {
                    SeamImage.Add(Constants.Direction.HORIZONTAL, false, (int)(e.NewSize.Height - e.PreviousSize.Height));
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(() => WorkInProgress(false)));
                });

                t1.Start();

                t1.Join();
            }
            else
            {
                CanOperate = true;
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
                }
            }else
            {
                if (ToggleLowEng.IsChecked == true)
                {
                    ToggleHighEng.IsChecked = false;
                    Highlighter.Color = Colors.Green;   
                }
            }

            if ((ToggleHighEng.IsChecked == false) && (ToggleLowEng.IsChecked == false))
            {
                theCanvas.EditingMode = InkCanvasEditingMode.None;
            }else
            {
                theCanvas.EditingMode = InkCanvasEditingMode.Ink;
            }
        }

        private void DoneEditing_Clicked(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure?", "Finished Editing", MessageBoxButton.YesNo, MessageBoxImage.Question,
                            MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                //theCanvas.EditingMode = InkCanvasEditingMode.None;

                WorkInProgress(true);

                CanOperate = true;

                StrokeCollection strokes = theCanvas.Strokes.Clone();

                Thread t1 = new Thread(delegate()
                {
                   SeamImage.RecomputeBase();
                   SeamImage.SetEnergy(strokes);
                   SeamImage.RecomputeEntireMap();
                   SeamImage.CalculateIndexMaps(Constants.Direction.OPTIMAL);
                   Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(delegate
                   {
                       WorkInProgress(false);
                       ResizeMode = ResizeMode.CanResize;
                       CanOperate = false;
                       SizeChanged += Window_SizeChanged;
                   }));


               });

                t1.Start();
            }
        }

        #endregion

        #region Other Methods

        private void SetImageSource(Bitmap bitmap, string bitmapName)
        {
            theImage.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(),IntPtr.Zero,
                Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(bitmap.Width, bitmap.Height));

            Size size = SeamImage.ImageSize;

            txtResolution.Text = size.Width + " x " + size.Height;
            if (bitmapName != null)
            {
                Title = Constants.TITLE + " - " + bitmapName;
                Width = bitmap.Width;
                Height = bitmap.Height + theStatusBar.ActualHeight + theMenu.ActualHeight + theToolbar.ActualHeight;
            }

        }

        private void WorkInProgress(bool isWorking)
        {
            txtStatus.Text = isWorking ? Constants.TEXT_WORKING : Constants.TEXT_READY;
            theCanvas.IsEnabled = !isWorking;
            menuItemEnergyMap.IsEnabled = !isWorking;
            menuItemNormal.IsEnabled = !isWorking;
            menuItemNormal.IsChecked = !isWorking;
            menuItemSaveImage.IsEnabled = !isWorking;
            ToggleHighEng.IsEnabled = !isWorking;
            ToggleLowEng.IsEnabled = !isWorking;
            ToggleLowEng.IsChecked = false;
            ToggleHighEng.IsChecked = false;
            DoneEditingButton.IsEnabled = !isWorking;
            menuItemCarve.IsEnabled = CanOperate && !isWorking;
            menuItemAddSeam.IsEnabled = CanOperate && !isWorking;

            if (!isWorking)
            {
                theCanvas.Strokes.Clear();
            }
        }

        #endregion
    }
}
