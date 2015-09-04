using Microsoft.Kinect;
using Microsoft.Kinect.Tools;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace KinectCapture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        private const int MapDepthToByte = 8000 / 256;

        private KinectSensor kinectSensor = null;

        private CoordinateMapper coordinateMapper = null;

        private MultiSourceFrameReader multiFrameSourceReader = null;

        private ushort[] depthFrameData = null;

        private byte[] colorFrameData = null;

        private WriteableBitmap colorBitmap = null;

        private WriteableBitmap depthBitmap = null;

        private byte[] depthPixels = null;

        private FrameDescription depthFrameDescription = null;

        private FrameDescription colorFrameDescription = null;

        class DepthColorInfo
        {
            public ushort[] depthFrameData = null;

            public byte[] colorFrameData = null;

            public ushort MinDepth = 0;
            public ushort MaxDepth = ushort.MaxValue;

            public DepthColorInfo(FrameDescription depthFrameDescription, FrameDescription colorFrameDescription)
            {
                depthFrameData = new ushort[depthFrameDescription.Width * depthFrameDescription.Height];

                colorFrameData = new byte[colorFrameDescription.Width * colorFrameDescription.Height * 4];
            }
        }

        ulong totalMen = 1000000000000000;
        ulong usedMen0 = 0;
        public MainWindow()
        {
            InitializeComponent();

            ComputerInfo InfoX = new ComputerInfo();

            totalMen = InfoX.TotalPhysicalMemory;
            usedMen0 = InfoX.AvailablePhysicalMemory;

            MemoryBar.Value = 0;

            var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();

            ExportPath.Text = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Capture");
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            ComputerInfo InfoX = new ComputerInfo();
            ulong val = InfoX.AvailablePhysicalMemory; // GC.GetTotalMemory(false); //7283920

            double valu = ((double)(val) / (double)totalMen * 100.0);

            MemoryBar.Value = Math.Min(100.0, valu);

            if(valu < 20)
            {
                MemoryBar.Foreground = Brushes.Red;
            } else
            {
                MemoryBar.Foreground = Brushes.Green;
            }

            MemoryLB.Content = string.Format("{0:0.00} MBytes ({1:0.00}%)", val / 1048576.0, valu);

            long uval = (long)usedMen0 - (long)val;

            valu = 100.0 - ((double)(val) / (double)usedMen0 * 100.0);

            UsageLB.Content = string.Format("{0:0.00} MBytes ({1:0.00}%)", uval / 1048576.0, valu);

            UsageBar.Value = Math.Max(0, Math.Min(100.0, valu));

            if (valu > 80)
            {
                UsageBar.Foreground = Brushes.Red;
            }
            else
            {
                UsageBar.Foreground = Brushes.Green;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            if(kinectSensor != null && kinectSensor.IsOpen)
            {
                if (multiFrameSourceReader != null)
                {
                    multiFrameSourceReader.Dispose();
                    multiFrameSourceReader = null;
                }

                if (kinectSensor != null)
                {
                    kinectSensor.Close();
                    kinectSensor = null;
                }
                ButtonOK.Content = "Connect";
                RecordButton.IsEnabled = false;
                return;
            }

            Counter = 0;

            kinectSensor = KinectSensor.GetDefault();

            kinectSensor.Open();

            multiFrameSourceReader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color);

            multiFrameSourceReader.MultiSourceFrameArrived += MultiFrameSourceReader_MultiSourceFrameArrived;

            coordinateMapper = kinectSensor.CoordinateMapper;

            depthFrameDescription = kinectSensor.DepthFrameSource.FrameDescription;

            int depthWidth = depthFrameDescription.Width;

            int depthHeight = depthFrameDescription.Height;

            depthFrameData = new ushort[depthWidth * depthHeight];

            depthPixels = new byte[depthFrameDescription.Width * depthFrameDescription.Height];

            colorFrameDescription = kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            int colorWidth = colorFrameDescription.Width;

            int colorHeight = colorFrameDescription.Height;

            depthBitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            colorFrameData = new byte[colorWidth * colorHeight * bytesPerPixel];

            colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            kinectSensor.IsAvailableChanged += Sensor_IsAvailableChanged;

            DepthImageViewer.Source = DepthSource;
            ColorImageViewer.Source = ImageSource;

            RecordButton.IsEnabled = true;

            ButtonOK.Content = "Disconnect";
        }

        object FrameHolder = new object();

        int Counter = 0;

        List<DepthColorInfo> mFrames = new List<DepthColorInfo>();

        bool Record = false;

        private void MultiFrameSourceReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {

            MultiSourceFrameReference multiRef = e.FrameReference;

            MultiSourceFrame multiFrame = multiRef.AcquireFrame();
            {
                if (multiFrame == null) return;

                // Retrieve data stream frame references
                ColorFrameReference colorRef = multiFrame.ColorFrameReference;
                DepthFrameReference depthRef = multiFrame.DepthFrameReference;

                using (ColorFrame colorFrame = colorRef.AcquireFrame())
                {
                    using (DepthFrame depthFrame = depthRef.AcquireFrame())
                    {
                        if (colorFrame == null || depthFrame == null) return;

                        if(Record)
                        {
                            DepthColorInfo info = new DepthColorInfo(depthFrameDescription, colorFrameDescription);

                            using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                            {
                                ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, ushort.MaxValue);
                                RenderDepthPixels();

                                info.MinDepth = depthFrame.DepthMinReliableDistance;
                                info.MaxDepth = depthFrame.DepthMaxReliableDistance;
                                depthFrame.CopyFrameDataToArray(info.depthFrameData);                                
                            }

                            using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                            {
                                colorBitmap.Lock();

                                colorFrame.CopyConvertedFrameDataToIntPtr(
                                        colorBitmap.BackBuffer,
                                        (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                        ColorImageFormat.Bgra);

                                colorBitmap.AddDirtyRect(new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight));

                                colorBitmap.Unlock();

                                if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                                {
                                    colorFrame.CopyRawFrameDataToArray(info.colorFrameData);
                                }
                                else
                                {
                                    colorFrame.CopyConvertedFrameDataToArray(info.colorFrameData, ColorImageFormat.Bgra);
                                }                               
                            }

                            lock (FrameHolder)
                            {
                                if (Record == true)
                                {
                                    mFrames.Add(info);
                                    Counter++;
                                }
                            }
                        } else
                        {
                            using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                            {
                                ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, ushort.MaxValue);
                                RenderDepthPixels();
                            }

                            using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                            {
                                colorBitmap.Lock();

                                colorFrame.CopyConvertedFrameDataToIntPtr(
                                        colorBitmap.BackBuffer,
                                        (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                        ColorImageFormat.Bgra);

                                colorBitmap.AddDirtyRect(new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight));

                                colorBitmap.Unlock();
                            }
                        }
                    }
                }
            }

            FramesLabel.Content = Counter.ToString();
        }

        public ImageSource DepthSource
        {
            get
            {
                return depthBitmap;
            }
        }    

        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            ushort* frameData = (ushort*)depthFrameData;

            for (int i = 0; i < (int)(depthFrameDataSize / depthFrameDescription.BytesPerPixel); ++i)
            {
                ushort depth = frameData[i];

                depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }

        private void RenderDepthPixels()
        {
            depthBitmap.WritePixels(
                new Int32Rect(0, 0, depthBitmap.PixelWidth, depthBitmap.PixelHeight),
                depthPixels,
                depthBitmap.PixelWidth,
                0);
        } 

        public ImageSource ImageSource
        {
            get
            {
                return colorBitmap;
            }
        }


        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (multiFrameSourceReader != null)
            {
                multiFrameSourceReader.Dispose();
                multiFrameSourceReader = null;
            }

            if (kinectSensor != null)
            {
                kinectSensor.Close();
                kinectSensor = null;
            }
        }
       
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
                       
        }

        public static void RecordClip(string filePath, TimeSpan duration)
        {
            using (KStudioClient client = KStudio.CreateClient())
            {
                client.ConnectToService();

                KStudioEventStreamSelectorCollection streamCollection = new KStudioEventStreamSelectorCollection();
                streamCollection.Add(KStudioEventStreamDataTypeIds.Ir);
                streamCollection.Add(KStudioEventStreamDataTypeIds.Depth);
                streamCollection.Add(KStudioEventStreamDataTypeIds.CompressedColor);

                using (KStudioRecording recording = client.CreateRecording(filePath, streamCollection))
                {
                    recording.StartTimed(duration);
                    while (recording.State == KStudioRecordingState.Recording)
                    {
                        Thread.Sleep(500);
                    }

                    if (recording.State == KStudioRecordingState.Error)
                    {
                        throw new InvalidOperationException("Error: Recording failed!");
                    }
                }

                client.DisconnectFromService();
            }
        }

        public static void PlaybackClip(string filePath, uint loopCount)
        {
            using (KStudioClient client = KStudio.CreateClient())
            {
                client.ConnectToService();

                KStudioEventFile file = client.OpenEventFile(filePath, KStudioEventFileFlags.None);

                KStudioEventStream s = file.GetEventStream(Guid.Parse("2ba0d67d-be11-4534-9444-3fb21ae0f08b"));

                var reader = client.CreateEventReader(filePath);

                KStudioEvent evt = null;
                while ( (evt = reader.GetNextEvent()) != null)
                {
                    if(evt.EventStreamSemanticId.ToString() == "d41013d2-47cb-4957-bb5f-8bb4f71a9d73" && evt.EventStreamDataTypeId.ToString() == "2ba0d67d-be11-4534-9444-3fb21ae0f08b")
                    {
                        Console.Write("dd");
                    }

                    if(evt.EventDataSize > 0)
                    {
                        Console.Write("dd");
                    }
                }


                using (KStudioPlayback playback = client.CreatePlayback(filePath))
                {
                    playback.LoopCount = loopCount;
                    playback.Start();

                    while (playback.State == KStudioPlaybackState.Playing)
                    {
                        

                        Thread.Sleep(500);
                    }

                    if (playback.State == KStudioPlaybackState.Error)
                    {
                        throw new InvalidOperationException("Error: Playback failed!");
                    }
                }

                client.DisconnectFromService();
            }
        }     

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Record = false;

            RecordButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            ButtonOK.IsEnabled = true;
            ClearButton.IsEnabled = true;
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {            
            Record = true;

            RecordButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            ButtonOK.IsEnabled = false;
            ClearButton.IsEnabled = false;
        }

        private void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog SaveDlg = new SaveFileDialog();

            SaveDlg.FileName = "Capture";

            if (SaveDlg.ShowDialog() == false)
            {
                return;                
            } else
            {
                ExportPath.Text = SaveDlg.FileName;
            }
        }

        private String ExportPathText = "";

        private int ExportAll()
        {
            ExportProgress.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { ExportProgress.Value = 0; }));

            int Processed = 0;
            //for (int i = 0; i < mFrames.Count; i++)
            Parallel.For(0, mFrames.Count, (i) =>
           {
               WriteableBitmap LcolorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

               WriteableBitmap LdepthBitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

               WriteableBitmap LdepthColorBitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

               byte[] LdepthPixels = new byte[depthFrameDescription.Width * depthFrameDescription.Height];

               byte[] LalphaPixels = new byte[depthFrameDescription.Width * depthFrameDescription.Height];

               byte[] LnormalPixels = new byte[depthFrameDescription.Width * depthFrameDescription.Height * 4];

               ColorSpacePoint[] colorPoints = new ColorSpacePoint[depthFrameDescription.Width * depthFrameDescription.Height];

               CameraSpacePoint[] cameraPoints = new CameraSpacePoint[depthFrameDescription.Width * depthFrameDescription.Height];

               byte[] depthColoredFrameData = new byte[depthFrameDescription.Width * depthFrameDescription.Height * 4];

               DepthColorInfo info = mFrames[i];

               int len = 0;
               StringBuilder sb = new StringBuilder();

               coordinateMapper.MapDepthFrameToColorSpace(info.depthFrameData, colorPoints);
               coordinateMapper.MapDepthFrameToCameraSpace(info.depthFrameData, cameraPoints);

               for (int y = 0; y < depthFrameDescription.Height; y += 1)
               {
                   for (int x = 0; x < depthFrameDescription.Width; x += 1)
                   {
                       int depthIndex = (y * depthFrameDescription.Width) + x;

                       CameraSpacePoint p = cameraPoints[depthIndex];

                       ColorSpacePoint colorPoint = colorPoints[depthIndex];
                       byte r = 0;
                       byte g = 0;
                       byte b = 0;

                       int colorX = (int)Math.Floor(colorPoint.X + 0.5);
                        
                       int colorY = (int)Math.Floor(colorPoint.Y + 0.5);
                        

                       if ((colorX >= 0) && (colorX < colorFrameDescription.Width) && (colorY >= 0) && (colorY < colorFrameDescription.Height))
                       {

                           int colorIndex = ((colorY * colorFrameDescription.Width) + colorX) * bytesPerPixel;

                           int displayIndex = depthIndex * bytesPerPixel;
                           b = info.colorFrameData[colorIndex++];
                           g = info.colorFrameData[colorIndex++];
                           r = info.colorFrameData[colorIndex++];

                           depthColoredFrameData[depthIndex * 4] = b;
                           depthColoredFrameData[depthIndex * 4 + 1] = g;
                           depthColoredFrameData[depthIndex * 4 + 2] = r;
                           depthColoredFrameData[depthIndex * 4 + 3] = 1;

                           LalphaPixels[depthIndex] = (byte)(info.depthFrameData[depthIndex] >= info.MinDepth && info.depthFrameData[depthIndex] <= ushort.MaxValue ? 255 : 0);

                           byte nx = 0;
                           byte ny = 0;
                           byte nz = 0;

                           ComputeNormal(info.depthFrameData, x, y, ref nx, ref ny, ref nz);

                           LnormalPixels[depthIndex * 4] = nx;
                           LnormalPixels[depthIndex * 4 + 1] = ny;
                           LnormalPixels[depthIndex * 4 + 2] = nz;
                           LnormalPixels[depthIndex * 4 + 3] = 1;
                       }  

                        if (!(Double.IsInfinity(p.X)) && !(Double.IsInfinity(p.Y)) && !(Double.IsInfinity(p.Z)))
                       {
                           if (p.X < 3.0 && p.Y < 3.0 && p.Z < 3.0)
                           {
                               sb.Append(String.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3} {4} {5}\n", p.X, p.Y, p.Z, r, g, b));
                               len++;
                           }
                       }
                   }
               }

               {
                   BitmapEncoder encoder = new PngBitmapEncoder();

                   LcolorBitmap.Lock();

                   LcolorBitmap.WritePixels(new Int32Rect(0, 0, colorFrameDescription.Width, colorFrameDescription.Height), info.colorFrameData, LcolorBitmap.PixelWidth * 4, 0);

                   LcolorBitmap.AddDirtyRect(new Int32Rect(0, 0, LcolorBitmap.PixelWidth, LcolorBitmap.PixelHeight));

                   LcolorBitmap.Unlock();

                   encoder.Frames.Add(BitmapFrame.Create(LcolorBitmap));

                   try
                   {
                       using (FileStream fs = new FileStream(String.Format("{1}_color_{0:000000}.png", i, ExportPathText), FileMode.Create))
                       {
                           encoder.Save(fs);
                       }

                   }
                   catch (IOException)
                   {
                   }
               }

               {
                   BitmapEncoder encoder = new PngBitmapEncoder();

                   for (int j = 0; j < info.depthFrameData.Length; ++j)
                   {
                       ushort depth = info.depthFrameData[j];
                       LdepthPixels[j] = (byte)(depth >= info.MinDepth && depth <= ushort.MaxValue ? (depth / MapDepthToByte) : 0);
                   }

                   LdepthBitmap.Lock();

                   LdepthBitmap.WritePixels(new Int32Rect(0, 0, depthFrameDescription.Width, depthFrameDescription.Height), LdepthPixels, LdepthBitmap.PixelWidth, 0);

                   LdepthBitmap.AddDirtyRect(new Int32Rect(0, 0, LdepthBitmap.PixelWidth, LdepthBitmap.PixelHeight));

                   LdepthBitmap.Unlock();

                   encoder.Frames.Add(BitmapFrame.Create(LdepthBitmap));

                   try
                   {
                       using (FileStream fs = new FileStream(String.Format("{1}_depth_{0:000000}.png", i, ExportPathText), FileMode.Create))
                       {
                           encoder.Save(fs);
                       }

                   }
                   catch (IOException)
                   {
                   }
               }

               {
                   BitmapEncoder encoder = new PngBitmapEncoder();

                   LdepthBitmap.Lock();

                   LdepthBitmap.WritePixels(new Int32Rect(0, 0, depthFrameDescription.Width, depthFrameDescription.Height), LalphaPixels, LdepthBitmap.PixelWidth, 0);

                   LdepthBitmap.AddDirtyRect(new Int32Rect(0, 0, LdepthBitmap.PixelWidth, LdepthBitmap.PixelHeight));

                   LdepthBitmap.Unlock();

                   encoder.Frames.Add(BitmapFrame.Create(LdepthBitmap));

                   try
                   {
                       using (FileStream fs = new FileStream(String.Format("{1}_alpha_{0:000000}.png", i, ExportPathText), FileMode.Create))
                       {
                           encoder.Save(fs);
                       }

                   }
                   catch (IOException)
                   {
                   }
               }

               {
                   BitmapEncoder encoder = new PngBitmapEncoder();

                   LdepthColorBitmap.Lock();

                   LdepthColorBitmap.WritePixels(new Int32Rect(0, 0, depthFrameDescription.Width, depthFrameDescription.Height), depthColoredFrameData, LdepthColorBitmap.PixelWidth * 4, 0);

                   LdepthColorBitmap.AddDirtyRect(new Int32Rect(0, 0, LdepthColorBitmap.PixelWidth, LdepthColorBitmap.PixelHeight));

                   LdepthColorBitmap.Unlock();


                   encoder.Frames.Add(BitmapFrame.Create(LdepthColorBitmap));

                   try
                   {
                       using (FileStream fs = new FileStream(String.Format("{1}_color_depth_{0:000000}.png", i, ExportPathText), FileMode.Create))
                       {
                           encoder.Save(fs);
                       }

                   }
                   catch (IOException)
                   {
                   }
               }

               {
                   BitmapEncoder encoder = new PngBitmapEncoder();

                   LdepthColorBitmap.Lock();

                   LdepthColorBitmap.WritePixels(new Int32Rect(0, 0, depthFrameDescription.Width, depthFrameDescription.Height), LnormalPixels, LdepthColorBitmap.PixelWidth * 4, 0);

                   LdepthColorBitmap.AddDirtyRect(new Int32Rect(0, 0, LdepthColorBitmap.PixelWidth, LdepthColorBitmap.PixelHeight));

                   LdepthColorBitmap.Unlock();

                   encoder.Frames.Add(BitmapFrame.Create(LdepthColorBitmap));

                   try
                   {
                       using (FileStream fs = new FileStream(String.Format("{1}_normal_{0:000000}.png", i, ExportPathText), FileMode.Create))
                       {
                           encoder.Save(fs);
                       }

                   }
                   catch (IOException)
                   {
                   }
               }

               String header = "ply \n" + "format ascii 1.0 \n" + "element vertex " + len + "\n" + "property float x \n" + "property float y \n" + "property float z \n" + "property uchar red \n" + "property uchar green \n" + "property uchar blue \n" + "end_header \n";

               using (System.IO.StreamWriter file = new System.IO.StreamWriter(String.Format("{1}_pcl_{0:000000}.ply", i, ExportPathText)))
               {
                   file.WriteLine(header + sb.ToString());
                   file.Close();
               }

               LcolorBitmap = null;

               LdepthBitmap = null;

               LdepthColorBitmap = null;

               LdepthPixels = null;

               LalphaPixels = null;

               LnormalPixels = null;

               colorPoints = null;

               cameraPoints = null;

               depthColoredFrameData = null;

               info.colorFrameData = null;
               info.depthFrameData = null;

               sb.Clear();
               sb = null;
               
               Interlocked.Increment(ref Processed);     

                ExportProgress.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { ExportProgress.Value = Math.Min(100.0, (Processed * 100.0 / mFrames.Count)); }));
               ExportLB.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { ExportLB.Content = String.Format("Exporting {1:000000} of {2:000000} ({0:00.00}%)", Math.Min(100.0, (Processed * 100.0 / mFrames.Count)), i + 1, mFrames.Count); }));

           });

            return 0;
        }

        private void ComputeNormal(ushort[] depthFrameData, int x, int y, ref byte nx, ref byte ny, ref byte nz)
        {
            double fnx = 0; double fny = 0; double fnz = 0;

            ushort baseHeight = 0;
            List<Vector4> Vecs = new List<Vector4>();

            List<Vector4> Normals = new List<Vector4>();

            int depthIndex = (y * depthFrameDescription.Width) + x;

            baseHeight = depthFrameData[depthIndex];

            for (int yy = -1; yy < 2; yy++)
            {
                for (int xx = -1; xx < 2; xx++)
                {
                    if(yy + y < 0 || yy + y >= depthFrameDescription.Height || xx + x < 0 || xx + x >= depthFrameDescription.Width)
                    {
                        continue;
                    }

                    Vector4 vec = new Vector4();
                    vec.X = xx;
                    vec.Y = yy;

                    depthIndex = ((yy + y) * depthFrameDescription.Width) + (xx + x);

                    vec.Z = depthFrameData[depthIndex] - baseHeight;
                    vec.W = 1;

                    Vecs.Add(vec);
                }
            }

            if (Vecs.Count >= 2)
            {
                for (int i = 0; i < Vecs.Count / 2; i++)
                {
                    if(i * 2 < Vecs.Count || i * 2 + 1 < Vecs.Count)
                    {
                        Vector4 A = Vecs[i * 2];
                        Vector4 B = Vecs[i * 2 + 1];

                        Vector4 vec = new Vector4();

                        vec.X = A.Y * B.Z - A.Z * B.Y;
                        vec.Y = A.Z * B.X - A.X * B.Z;
                        vec.Z = A.X * B.Y - A.Y * B.X;

                        //AxB = (AyBz − AzBy, AzBx − AxBz, AxBy − AyBx)

                        Normals.Add(vec);
                    }                    
                }

                for (int i = 0; i < Normals.Count; i++)
                {
                    fnx += Normals[i].X / Normals.Count;
                    fny += Normals[i].Y / Normals.Count;
                    fnz += Normals[i].Z / Normals.Count;
                }
            }

            double norm = Math.Sqrt(fnx * fnx + fny * fny + fnz * fnz);

            fnx = fnx / norm;
            fny = fny / norm;
            fnz = fnz / norm;

            nx = (byte)((fnx + 1.0) * 0.5 * 255);
            ny = (byte)((fny + 1.0) * 0.5 * 255);
            nz = (byte)((fnz + 1.0) * 0.5 * 255);
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            ExportPathText = ExportPath.Text;

            ClearButton.IsEnabled = false;
            Export.IsEnabled = false;
            RecordButton.IsEnabled = false;
            StopButton.IsEnabled = false;
            FolderButton.IsEnabled = false;
            ExportPath.IsEnabled = false;

            var slowTask = Task<int>.Factory.StartNew(() => ExportAll());

            await slowTask;

            ExportProgress.Value = 0;
            ExportLB.Content = "";

            Export.IsEnabled = true;
            ButtonOK.IsEnabled = true;
            RecordButton.IsEnabled = true;
            StopButton.IsEnabled = true;
            FolderButton.IsEnabled = true;
            ExportPath.IsEnabled = true;
            ClearButton.IsEnabled = true;

            if (kinectSensor != null && kinectSensor.IsOpen)
            {
                RecordButton.IsEnabled = true;
                StopButton.IsEnabled = false;
            }  else
            {
                Export.IsEnabled = false;
                RecordButton.IsEnabled = false;
                StopButton.IsEnabled = false;
                FolderButton.IsEnabled = false;
                ExportPath.IsEnabled = false;                
            }      
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Kinect Capture\n\nDeveloped By: Rodrigo Marques", "About Kinect Capture", MessageBoxButton.OK);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Counter = 0;

            mFrames.Clear();
        }
    }
}
