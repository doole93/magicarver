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
using Point=System.Windows.Point;

namespace MagiCarver
{
    public partial class MainWindow
    {

        

        #region Data Members

        private readonly EventTrigger[]  DragToolTriggers = new EventTrigger[2];

        #endregion

        #region Delegates

        private delegate void VoidDelegate();

        #endregion

        #region Properties

        private SeamImage           TheImage { get; set; }
        private Constants.Maps      CurrentViewBitmap { get; set; }
        private bool                PaintSeam { get; set; }
        private Constants.Direction Direction { get; set; }
        private DrawingAttributes   Highlighter { get; set; }
        public  DependencyProperty  CanOperateProperty;
        private int                 NumSeamsToCarveOrAdd { get; set; }
        private Point               startDrag { get; set; }
        private Constants.EnergyFunctions EnergyFunction { get; set; }

        private double x, y;

        public bool CanOperate
        {
            get 
            { 
                return (bool)GetValue(CanOperateProperty); 
            }
            set 
            {
                SetValue(CanOperateProperty, value); 
            }
        }

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
            EnergyFunction = Constants.EnergyFunctions.SOBEL;

            FadeControl(this, true);
        }

        #endregion

        #region Event Handlers

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
                myThumb.Triggers.Add(DragToolTriggers[0]);
                myThumb.Triggers.Add(DragToolTriggers[1]);
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

        private static void OnCanOperateChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) 
        { 
        }

        private void OpenFile_Clicked(object sender, RoutedEventArgs e)
        {
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

            ParameterizedThreadStart starter = LoadFile;
            new Thread(starter).Start(openFile);
        }

        private void LoadFile(object openFile)
        {
            if (TheImage != null)
            {
                TheImage.Dispose();
            }

            Bitmap bitmap = null;

            if (openFile != null)
            {
                try
                {
                    if (openFile.GetType() == typeof(OpenFileDialog))
                    {
                        bitmap = new Bitmap(((OpenFileDialog) openFile).FileName);
                    }else
                    {
                        bitmap = (Bitmap) openFile;
                    }
                }
                catch (ArgumentException)
                {
                    MessageBox.Show("Invalid file selected. Please select a valid image file.", "Open File Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            if (bitmap != null)
            {
                TheImage = new SeamImage(bitmap, EnergyFunction);

                Dispatcher.Invoke((VoidDelegate)delegate { CanOperate = false; }, null);

                TheImage.ImageChanged += m_SeamImage_ImageChanged;
                TheImage.OperationCompleted += m_SeamImage_OperationCompleted;
                TheImage.ColorSeam += m_SeamImage_ColorSeam;
            }

            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)delegate
            {
                if (bitmap != null)
                {
                    if (openFile != null)
                    {
                        if (openFile.GetType() == typeof(OpenFileDialog))
                        {
                            SetImageSource(bitmap, ((OpenFileDialog)openFile).SafeFileName);
                        }else
                        {
                            SetImageSource(bitmap, null);
                        }
                    }

                    WorkInProgress(false);
                    myThumb.Visibility = Visibility.Visible;
                    SetCacheSlider();
                    myThumb.Triggers.CopyTo(DragToolTriggers, 0);
                    myThumb.Triggers.Clear();
                }
                else
                {
                    Title = Constants.TEXT_READY;
                }
            });
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
            WorkInProgress(true);

            Thread t1 = new Thread(delegate() {
                TheImage.Carve(Direction, PaintSeam, NumSeamsToCarveOrAdd);
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(delegate {
                    WorkInProgress(false);

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
            WorkInProgress(true);

            Thread t1 = new Thread(delegate()
            {
                TheImage.Add(Direction, PaintSeam, NumSeamsToCarveOrAdd);
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(delegate {
                    WorkInProgress(false);

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

        private void RowDefinition_Loaded(object sender, RoutedEventArgs e)
        {
            RowDefinition rowDefinition = ((RowDefinition) sender);

            rowDefinition.MaxHeight = rowDefinition.ActualHeight;
            rowDefinition.MinHeight = rowDefinition.ActualHeight;
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
                RecomputeData();
                myThumb.Triggers.Add(DragToolTriggers[0]);
                myThumb.Triggers.Add(DragToolTriggers[1]);
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
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (VoidDelegate)(() => WorkInProgress(false)));
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

                EnableSelection(false);

                if (xChange < 0 && xadjust > 0)
                {
                    WorkInProgress(true);

                    Thread t1 = new Thread(delegate()
                    {
                        TheImage.Carve(Constants.Direction.VERTICAL, false, Math.Abs(xChange));
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
                        TheImage.Carve(Constants.Direction.HORIZONTAL, false, Math.Abs(yChange));
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
                        TheImage.Add(Constants.Direction.VERTICAL, false, xChange);
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
                        TheImage.Add(Constants.Direction.HORIZONTAL, false, yChange);
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

            theImage.Source.Freeze();

            Size size = TheImage.ImageSize;

            txtResolution.Text = size.Width + " x " + size.Height;

            if (bitmapName != null)
            {
                Title = Constants.TITLE + " - " + bitmapName;
            }

            theCanvas.Height = bitmap.Height;
            theCanvas.Width = bitmap.Width;
            InkCanvas.SetLeft(myThumb, bitmap.Width - myThumb.Width);
            InkCanvas.SetTop(myThumb, bitmap.Height - myThumb.Height);
        }

        private void WorkInProgress(bool isWorking)
        {
            txtStatus.Text = isWorking ? Constants.TEXT_WORKING : Constants.TEXT_READY;
            theCanvas.IsEnabled = !isWorking;
            menuItemEnergyMap.IsEnabled = !isWorking;
            menuItemNormal.IsEnabled = !isWorking;
            menuItemSaveImage.IsEnabled = !isWorking;
            menuItemPrewittFunc.IsEnabled = !isWorking;
            menuItemSobelFnc.IsEnabled = !isWorking;
            menuItemRobertsFunc.IsEnabled = !isWorking;
            ToggleHighEng.IsEnabled = !isWorking;
            ToggleLowEng.IsEnabled = !isWorking;
            ToggleLowEng.IsChecked = false;
            ToggleHighEng.IsChecked = false;
            ToggleSelection.IsChecked = false;
            ToggleSelection.IsEnabled = !isWorking;
            DoneEditingButton.IsEnabled = !isWorking;
            CropSelectionButton.IsEnabled = !isWorking;
            menuItemCarve.IsEnabled = CarveButton.IsEnabled = CanOperate && !isWorking;
            menuItemAddSeam.IsEnabled = AddButton.IsEnabled = CanOperate && !isWorking;

            if (!isWorking)
            {
                theCanvas.Strokes.Clear();
            }
        }

        private void SetCacheSlider()
        {
            cacheSlider.Maximum = Math.Min(TheImage.ImageSize.Width, TheImage.ImageSize.Height);
            cacheSlider.TickFrequency = (int)(0.05 * cacheSlider.Maximum);
            cacheSlider.IsEnabled = TheImage != null;
            if (TheImage != null)
            {
                cacheSlider.Value = TheImage.CacheLimit;
            }
        }

        public void FadeControl(Control a, bool fadeIn)
        {
            Storyboard storyboard = new Storyboard();
            TimeSpan duration = new TimeSpan(0, 0, 1);

            DoubleAnimation animation = new DoubleAnimation
                                            {
                                                From = fadeIn ? 0.0 : 1.0,
                                                To = fadeIn ? 1.0 : 0.0,
                                                Duration = new Duration(duration)
                                            };

            Storyboard.SetTargetName(animation, a.Name);
            Storyboard.SetTargetProperty(animation, new PropertyPath(OpacityProperty));

            storyboard.Children.Add(animation);

            storyboard.Begin(this);
        }

        #endregion

        private void ChangeEnergyFunc_Clicked(object sender, RoutedEventArgs e)
        {
            EnergyFunction = (Constants.EnergyFunctions)Enum.Parse(typeof(Constants.EnergyFunctions), ((MenuItem)sender).Header.ToString().ToUpper());

            foreach (MenuItem menuItem in ((MenuItem)((MenuItem)sender).Parent).Items)
            {
                menuItem.IsChecked = menuItem == sender;
            }

            if (TheImage != null)
            {
                WorkInProgress(true);
                CanOperate = false;
                TheImage.ChangeEnergyFunc(EnergyFunction, true);
                WorkInProgress(false); 
            }
        }

        private void ToggleSelection_Clicked(object sender, RoutedEventArgs e)
        {
            theCanvas.EditingMode = InkCanvasEditingMode.None;
            
            EnableSelection((bool) ToggleSelection.IsChecked);
        }

        private void EnableSelection(bool enabled)
        {
            if (enabled)
            {
                theCanvas.MouseDown += canvas_MouseDown;
                theCanvas.MouseUp += canvas_MouseUp;
                theCanvas.MouseMove += canvas_MouseMove;
            }else
            {
                theCanvas.MouseDown -= canvas_MouseDown;
                theCanvas.MouseUp -= canvas_MouseUp;
                theCanvas.MouseMove -= canvas_MouseMove;

                rectangle.Visibility = Visibility.Hidden;
            }
        }

        private void CropSelection_Clicked(object sender, RoutedEventArgs e)
        {
            ParameterizedThreadStart starter = LoadFile;
            new Thread(starter).Start(Utilities.CropImage(TheImage.Bitmap, new Rectangle((int)x, (int)y, (int) rectangle.Width, (int) rectangle.Height)));   
            EnableSelection(false);
        }

        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //Set the start point
            startDrag = e.GetPosition(theCanvas);
            //Move the selection marquee on top of all other objects in canvas
            Panel.SetZIndex(rectangle, theCanvas.Children.Count);
            //Capture the mouse
            if (!theCanvas.IsMouseCaptured)
                theCanvas.CaptureMouse();
            theCanvas.Cursor = Cursors.Cross;
        }

        private void canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //Release the mouse
            if (theCanvas.IsMouseCaptured)
                theCanvas.ReleaseMouseCapture();
            theCanvas.Cursor = Cursors.Arrow;
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (theCanvas.IsMouseCaptured)
            {
                Point currentPoint = e.GetPosition(theCanvas);

                //Calculate the top left corner of the rectangle 
                //regardless of drag direction
                 x = startDrag.X < currentPoint.X ? startDrag.X : currentPoint.X;
                 y = startDrag.Y < currentPoint.Y ? startDrag.Y : currentPoint.Y;

                if (rectangle.Visibility == Visibility.Hidden)
                    rectangle.Visibility = Visibility.Visible;

                //Move the rectangle to proper place
                rectangle.RenderTransform = new TranslateTransform(x, y);
                //Set its size
                rectangle.Width = Math.Abs(e.GetPosition(theCanvas).X - startDrag.X);
                rectangle.Height = Math.Abs(e.GetPosition(theCanvas).Y - startDrag.Y);
            }
        }
    }
}
