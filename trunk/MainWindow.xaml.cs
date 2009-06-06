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
using Image=System.Drawing.Image;
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
        private bool                FinishedUserInput { get; set; }

        private object Lock = new object();
        private bool busy;

        #endregion

        #region CTors

        public MainWindow()
        {
            InitializeComponent();

            Direction = Constants.Direction.OPTIMAL;
            PaintSeam = true;
        }

        #endregion

        #region Event Handlers

        private void OpenFile_Clicked(object sender, RoutedEventArgs e)
        {
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

                       FinishedUserInput = false;

                       SeamImage.ImageChanged += m_SeamImage_ImageChanged;
                       SeamImage.OperationCompleted += m_SeamImage_OperationCompleted;
                       SeamImage.ColorSeam += m_SeamImage_ColorSeam;
                   }
                   
                   Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)delegate
                       {
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
                            Bitmap bitmap = null;

                            switch (CurrentViewBitmap)
                            {
                                case Constants.Maps.ENERGY:
                                    bitmap = SeamImage.EnergyMapBitmap;
                                    break;
                                case Constants.Maps.NORMAL:
                                    bitmap = SeamImage.Bitmap;
                                    break;
                                case Constants.Maps.HORIZONTAL_INDEX:

                                    break;
                                case Constants.Maps.VERTICAL_INDEX:

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
            WorkInProgress(true);

            Thread t1 = new Thread(delegate() { SeamImage.Carve(Direction, PaintSeam, 1);
            WorkInProgress(false);});

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
            //menuItemCarve.IsEnabled = false;
            //menuItemAddSeam.IsEnabled = false;
            //txtStatus.Text = Constants.TEXT_WORKING;

            //Thread t1 = new Thread(delegate()
            //{
            //    Size minimumSize = new Size(SeamImage.ImageSize.Width + 5, SeamImage.ImageSize.Height + 5);
            //    SeamImage.AddSeam(Direction, minimumSize, PaintSeam);
            //});

            //t1.Start();
            MessageBox.Show("Under Construction.");
        }

        private void RowDefinition_Loaded(object sender, RoutedEventArgs e)
        {
            RowDefinition rowDefinition = ((RowDefinition) sender);

            rowDefinition.MaxHeight = rowDefinition.ActualHeight;
            rowDefinition.MinHeight = rowDefinition.ActualHeight;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged && FinishedUserInput)
            {
                WorkInProgress(true);

                Thread t1 = new Thread(delegate()
                {
                    SeamImage.Carve(Constants.Direction.VERTICAL, PaintSeam, (int)(e.PreviousSize.Width - e.NewSize.Width));
                    WorkInProgress(false);
                });

                t1.Start();

                t1.Join();
            }
            else if (e.HeightChanged && FinishedUserInput)
            {
                WorkInProgress(true);

                Thread t1 = new Thread(delegate()
                {
                    SeamImage.Carve(Constants.Direction.HORIZONTAL, PaintSeam, (int)(e.PreviousSize.Height - e.NewSize.Height));
                    WorkInProgress(false);
                });

                t1.Start();

                t1.Join();
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
                FinishedUserInput = true;
                theCanvas.EditingMode = InkCanvasEditingMode.None;

                WorkInProgress(true);

                Thread t1 = new Thread(() => 
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate) (() =>
                      {   SeamImage.SetEnergy(theCanvas.Strokes);
                          SeamImage.RecomputeEntireMap();
                          SeamImage.CalculateIndexMaps(Constants.Direction.OPTIMAL);
                          WorkInProgress(false);
                    })));

                t1.Start();
            }
        }

        #endregion

        #region Other Methods

        private void SetImageSource(Bitmap bitmap, string bitmapName)
        {
            theImage.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(),
                                                                                           IntPtr.Zero, Int32Rect.Empty,
                                                                                           BitmapSizeOptions.
                                                                                               FromWidthAndHeight(
                                                                                               bitmap.Width,
                                                                                               bitmap.Height));

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
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)delegate
            {
                theCanvas.IsEnabled = !isWorking;
                menuItemEnergyMap.IsEnabled = !isWorking;
                menuItemNormal.IsEnabled = !isWorking;
                menuItemNormal.IsChecked = !isWorking;
                menuItemSaveImage.IsEnabled = !isWorking;
                ToggleHighEng.IsEnabled = !FinishedUserInput && !isWorking;
                ToggleLowEng.IsEnabled = !FinishedUserInput && !isWorking;
                ToggleLowEng.IsChecked = false;
                ToggleHighEng.IsChecked = false;
                DoneEditingButton.IsEnabled = !FinishedUserInput && !isWorking;
                menuItemCarve.IsEnabled = FinishedUserInput && !isWorking;
                menuItemAddSeam.IsEnabled = FinishedUserInput && !isWorking;
                txtStatus.Text = isWorking ? Constants.TEXT_WORKING : Constants.TEXT_READY;

                if (!isWorking)
                {
                    theCanvas.Strokes.Clear();
                }
            });
        }

        #endregion
    }
}
