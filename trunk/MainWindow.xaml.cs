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
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace MagiCarver
{
    public partial class MainWindow
    {
        EventTrigger[] arr = new EventTrigger[2];

        #region Delegates

        private delegate void VoidDelegate();

        #endregion

        public DependencyProperty CanOperateProperty;

        #region Properties

        private SeamImage           TheImage { get; set; }
        private Constants.Maps      CurrentViewBitmap { get; set; }
        private bool                PaintSeam { get; set; }
        private Constants.Direction Direction { get; set; }
        private DrawingAttributes   Highlighter { get; set; }
        private bool m_canOperate;
        public bool CanOperate
        {
            get { return (bool)GetValue(CanOperateProperty); }
            set { SetValue(CanOperateProperty, value); }
        }
        private int NumSeamsToCarveOrAdd { get; set; }

        #endregion

        #region CTors

        public MainWindow()
        {
            CanOperateProperty = DependencyProperty.Register("CanOperate",
      typeof(bool), typeof(MainWindow),
      new FrameworkPropertyMetadata(false,
      new PropertyChangedCallback(OnCanOperateChanged)));

            InitializeComponent();

            Direction = Constants.Direction.VERTICAL;
            PaintSeam = false;
            NumSeamsToCarveOrAdd = 20;

            FadeControl(this, true);
        }



        #endregion

private static void OnCanOperateChanged(
   DependencyObject o, DependencyPropertyChangedEventArgs e) {}

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
                       TheImage = new SeamImage(bitmap, new Sobel());


                       Dispatcher.Invoke((VoidDelegate)delegate() { CanOperate = false; }, null);
                       
                       

                       TheImage.ImageChanged += m_SeamImage_ImageChanged;
                       TheImage.OperationCompleted += m_SeamImage_OperationCompleted;
                       TheImage.ColorSeam += m_SeamImage_ColorSeam;
                   }
                   
                   Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)delegate
                       {
                           if (bitmap != null)
                           {
                               SetImageSource(bitmap, openFile.SafeFileName);
                               WorkInProgress(false);
                               myThumb.Visibility = Visibility.Visible;
                               SetCacheSlider();
                               myThumb.Triggers.CopyTo(arr, 0);
                               myThumb.Triggers.Clear();
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
                                    bitmap = TheImage.EnergyMapBitmap;
                                    SetCacheSlider();
                                    break;
                                case Constants.Maps.NORMAL:
                                    bitmap = TheImage.Bitmap;
                                    SetCacheSlider();
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
                TheImage.Carve(Direction, PaintSeam, NumSeamsToCarveOrAdd);
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(delegate {
                    WorkInProgress(false);
                    SizeChanged += Window_DummySizeChanged;

                    if (Direction == Constants.Direction.VERTICAL)
                    {


                        InkCanvas.SetLeft(myThumb, InkCanvas.GetLeft(myThumb) -
                                                    NumSeamsToCarveOrAdd);
                        theCanvas.Width -= NumSeamsToCarveOrAdd;
                    }
                    else
                    {
                        InkCanvas.SetTop(myThumb, InkCanvas.GetTop(myThumb) -
                                                NumSeamsToCarveOrAdd);
                        theCanvas.Height -= NumSeamsToCarveOrAdd;
                    }
                }));
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
                TheImage.Add(Direction, PaintSeam, NumSeamsToCarveOrAdd);
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(delegate {
                    WorkInProgress(false);
                    SizeChanged += Window_DummySizeChanged;

                    if (Direction == Constants.Direction.VERTICAL)
                    {
                        InkCanvas.SetLeft(myThumb, InkCanvas.GetLeft(myThumb) + NumSeamsToCarveOrAdd);
                        theCanvas.Width += NumSeamsToCarveOrAdd;
                    }
                    else
                    {
                        InkCanvas.SetTop(myThumb, InkCanvas.GetTop(myThumb) +
                                                NumSeamsToCarveOrAdd);
                        theCanvas.Height += NumSeamsToCarveOrAdd;
                    }
                }));
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
            //if (e.WidthChanged && CanOperate && e.PreviousSize.Width > e.NewSize.Width && SeamImage.ImageSize.Width > e.PreviousSize.Width - e.NewSize.Width && (int)(e.PreviousSize.Width - e.NewSize.Width) > 0)
            //{
            //    WorkInProgress(true);

            //    Thread t1 = new Thread(delegate()
            //    {
            //        SeamImage.Carve(Constants.Direction.VERTICAL, false, (int)(e.PreviousSize.Width - e.NewSize.Width));
            //        Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(() => WorkInProgress(false)));
            //    });

            //    t1.Start();

            //    t1.Join();
            //}
            //else if (e.HeightChanged && CanOperate && e.PreviousSize.Height > e.NewSize.Height && SeamImage.ImageSize.Height > e.PreviousSize.Height - e.NewSize.Height && (int)(e.PreviousSize.Height - e.NewSize.Height) > 0)
            //{
            //    WorkInProgress(true);

            //    Thread t1 = new Thread(delegate()
            //    {
            //        SeamImage.Carve(Constants.Direction.HORIZONTAL, false, (int)(e.PreviousSize.Height - e.NewSize.Height));
            //        Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(() => WorkInProgress(false)));
            //    });

            //    t1.Start();

            //    t1.Join();
            //}
            //else if (e.WidthChanged && CanOperate && e.PreviousSize.Width < e.NewSize.Width && (int)(e.NewSize.Width - e.PreviousSize.Width) > 0)
            //{
            //    WorkInProgress(true);

            //    Thread t1 = new Thread(delegate()
            //    {
            //        SeamImage.Add(Constants.Direction.VERTICAL, false, (int)(e.NewSize.Width - e.PreviousSize.Width));
            //        Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(() => WorkInProgress(false)));
            //    });

            //    t1.Start();

            //    t1.Join();
            //}
            //else if (e.HeightChanged && CanOperate && e.PreviousSize.Height < e.NewSize.Height && (int)(e.NewSize.Height - e.PreviousSize.Height) > 0)
            //{
            //    WorkInProgress(true);

            //    Thread t1 = new Thread(delegate()
            //    {
            //        SeamImage.Add(Constants.Direction.HORIZONTAL, false, (int)(e.NewSize.Height - e.PreviousSize.Height));
            //        Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(() => WorkInProgress(false)));
            //    });

            //    t1.Start();

            //    t1.Join();
            //}
            //else
            //{
            //    CanOperate = true;
            //}
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
                myThumb.Triggers.Add(arr[0]);
                myThumb.Triggers.Add(arr[1]);
                RecomputeData();
            }
        }

        private void RecomputeData()
        {
            theCanvas.EditingMode = InkCanvasEditingMode.None;

            WorkInProgress(true);

            CanOperate = true;

            StrokeCollection strokes = theCanvas.Strokes.Clone();

            Thread t1 = new Thread(delegate()
            {
                TheImage.RecomputeBase();
                TheImage.SetEnergy(strokes);
                TheImage.RecomputeEntireMap();
                TheImage.CalculateIndexMaps(Constants.Direction.BOTH, 0);
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(delegate
                {
                    WorkInProgress(false);
                }));


            });

            t1.Start();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            NumSeamsToCarveOrAdd = (int)e.NewValue;
        }

        private void onDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (CanOperate)
            {
                myThumb.Tag = true;


                int yChange = (int)e.VerticalChange;
                int xChange = (int)e.HorizontalChange;

                if (Math.Abs(e.VerticalChange) > Math.Abs(e.HorizontalChange))
                {
                    xChange = 0;
                }
                else
                {
                    yChange = 0;
                }

                double yadjust = theCanvas.Height + yChange;
                double xadjust = theCanvas.Width + xChange;

                if ((xadjust >= myThumb.Width) && (yadjust >= myThumb.Height))
                {
                    theCanvas.Width = xadjust;
                    theCanvas.Height = yadjust;
                    InkCanvas.SetLeft(myThumb, InkCanvas.GetLeft(myThumb) +
                                            xChange);
                    InkCanvas.SetTop(myThumb, InkCanvas.GetTop(myThumb) +
                                            yChange);
                }

                if (xChange < 0 && xadjust > 0)
                {
                    WorkInProgress(true);

                    Thread t1 = new Thread(delegate()
                    {
                        TheImage.Carve(Constants.Direction.VERTICAL, false, (int)(Math.Abs(xChange)));
                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(() => WorkInProgress(false)));
                    });

                    t1.Start();

                    t1.Join();
                }
                else if (yChange < 0 && yadjust > 0)
                {
                    WorkInProgress(true);

                    Thread t1 = new Thread(delegate()
                    {
                        TheImage.Carve(Constants.Direction.HORIZONTAL, false, (int)(Math.Abs(yChange)));
                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(() => WorkInProgress(false)));
                    });

                    t1.Start();

                    t1.Join();
                }
                else if (xChange > 0)
                {
                    WorkInProgress(true);

                    Thread t1 = new Thread(delegate()
                    {
                        TheImage.Add(Constants.Direction.VERTICAL, false, (int)(xChange));
                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(() => WorkInProgress(false)));
                    });

                    t1.Start();

                    t1.Join();
                }
                else if (yChange > 0)
                {
                    WorkInProgress(true);

                    Thread t1 = new Thread(delegate()
                    {
                        TheImage.Add(Constants.Direction.HORIZONTAL, false, (int)(yChange));
                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(() => WorkInProgress(false)));
                    });

                    t1.Start();

                    t1.Join();
                }
            }
        }

        private void onDragStarted(object sender, DragStartedEventArgs e)
        {
            // myThumb.Background = Brushes.Orange;
        }

        private void onDragCompleted(object sender, DragCompletedEventArgs e)
        {
            //  myThumb.Background = Brushes.Blue;
        }

        #endregion

        #region Other Methods

        private void SetImageSource(Bitmap bitmap, string bitmapName)
        {
            theImage.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(),IntPtr.Zero,
                Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(bitmap.Width, bitmap.Height));

            Size size = TheImage.ImageSize;

            txtResolution.Text = size.Width + " x " + size.Height;
            if (bitmapName != null)
            {
                Title = Constants.TITLE + " - " + bitmapName;
                //Width = bitmap.Width;
                //Height = bitmap.Height + theStatusBar.ActualHeight + theMenu.ActualHeight + theToolbar.ActualHeight;
                theCanvas.Height = bitmap.Height;
                theCanvas.Width = bitmap.Width;
                InkCanvas.SetLeft(myThumb, bitmap.Width - myThumb.Width);
                InkCanvas.SetTop(myThumb, bitmap.Height - myThumb.Height);
            }

        }

        private void WorkInProgress(bool isWorking)
        {
            txtStatus.Text = isWorking ? Constants.TEXT_WORKING : Constants.TEXT_READY;
            theCanvas.IsEnabled = !isWorking;
            menuItemEnergyMap.IsEnabled = !isWorking;
            menuItemNormal.IsEnabled = !isWorking;
            menuItemSaveImage.IsEnabled = !isWorking;
            ToggleHighEng.IsEnabled = !isWorking;
            ToggleLowEng.IsEnabled = !isWorking;
            ToggleLowEng.IsChecked = false;
            ToggleHighEng.IsChecked = false;
            DoneEditingButton.IsEnabled = !isWorking;
            menuItemCarve.IsEnabled = CarveButton.IsEnabled = CanOperate && !isWorking;
            menuItemAddSeam.IsEnabled = AddButton.IsEnabled = CanOperate && !isWorking;

            if (!isWorking)
            {
                theCanvas.Strokes.Clear();
            }
        }

        public void FadeControl(Control a, bool fadeIn)
        {
            Storyboard storyboard = new Storyboard();
            TimeSpan duration = new TimeSpan(0, 0, 1);

            DoubleAnimation animation = new DoubleAnimation();

            animation.From = fadeIn ? 0.0 : 1.0;
            animation.To = fadeIn ? 1.0 : 0.0;
            animation.Duration = new Duration(duration);

            Storyboard.SetTargetName(animation, a.Name);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Control.OpacityProperty));

            storyboard.Children.Add(animation);

            storyboard.Begin(this);
        }

        #endregion

        private void SetCacheSlider()
        {
            cacheSlider.Maximum = Math.Min(TheImage.ImageSize.Width, TheImage.ImageSize.Height);
            cacheSlider.TickFrequency = (int)(0.05 * cacheSlider.Maximum);
            cacheSlider.IsEnabled = TheImage != null;
            cacheSlider.Value = TheImage.CacheLimit;
        }

        private void cacheSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            BtnCacheApply.IsEnabled = true;
        }

        private void CacheLimitButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Cache recalculation will be needed. Proceed?", "Set Cache Limit", MessageBoxButton.YesNo, MessageBoxImage.Question,
                MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                TheImage.SetCacheLimit((int)cacheSlider.Value);
                BtnCacheApply.IsEnabled = false;
                RecomputeData();
            }
        }

        private void myThumb_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                InkCanvas.SetLeft(myThumb, InkCanvas.GetLeft(myThumb) - (e.NewSize.Width - e.PreviousSize.Width));
            }

            if (e.HeightChanged)
            {
                InkCanvas.SetTop(myThumb, InkCanvas.GetTop(myThumb) - (e.NewSize.Height - e.PreviousSize.Height));
            }
        }

        private void myThumb_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
