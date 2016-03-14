
namespace Microsoft.Samples.Kinect.BodyIndexBasics
{
    using Microsoft.Win32;
    using System;
    using System.ComponentModel;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Windows.Controls;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Navigation;


    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// Size of the RGB pixel in the bitmap
        private const int BytesPerPixel = 4;

        /// Collection of colors to be used to display the BodyIndexFrame data.
        private static readonly uint[] BodyColor =
        {
            0x0000FF00,
            0x0000FF00,
            0x0000FF00,
            0x0000FF00,
            0x0000FF00,
            0x0000FF00,
            //0x00FF0000,
        };


        /// Active Kinect sensor
        private KinectSensor _kinectSensor = null;

        /// Reader for body index frames
        private BodyIndexFrameReader _bodyIndexFrameReader = null;

        /// Description of the data contained in the body index frame
        private FrameDescription _bodyIndexFrameDescription = null;

        /// Bitmap to display
        private WriteableBitmap _bodyIndexBitmap = null;

        /// Intermediate storage for frame data converted to color
        private uint[] _bodyIndexPixels = null;

        /// Current status text to display
        private string _statusText = null;

        /// Buffer to save data
        KinectFileManager _recorder = null;
        BodyFrameReader _reader = null;
        IList<Body> _bodies = null;
        bool _firstFrame = true;


        /// Initializes a new instance of the MainWindow class.
        public MainWindow()
        {
            // get the _kinectSensor object
            this._kinectSensor = KinectSensor.GetDefault();

            // open the reader for bodyIndex frames
            this._bodyIndexFrameReader = this._kinectSensor.BodyIndexFrameSource.OpenReader();

            // wire handler for frame arrival
            this._bodyIndexFrameReader.FrameArrived += this.Reader_FrameArrived;

            this._bodyIndexFrameDescription = this._kinectSensor.BodyIndexFrameSource.FrameDescription;

            // allocate space to put the pixels being converted
            this._bodyIndexPixels = new uint[this._bodyIndexFrameDescription.Width * this._bodyIndexFrameDescription.Height];

            // create the bitmap to display
            this._bodyIndexBitmap = new WriteableBitmap(this._bodyIndexFrameDescription.Width, this._bodyIndexFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            // set IsAvailableChanged event notifier
            this._kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this._kinectSensor.Open();



            // set the status text
            this.StatusText = this._kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // use the window object as a view model
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();

            _kinectSensor = KinectSensor.GetDefault();

            if (_kinectSensor != null)
            {
                _kinectSensor.Open();

                _bodies = new Body[_kinectSensor.BodyFrameSource.BodyCount];

                _reader = _kinectSensor.BodyFrameSource.OpenReader();
                _reader.FrameArrived += BodyReader_FrameArrived;

                _recorder = new KinectFileManager();
            }


        }

        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        public event PropertyChangedEventHandler PropertyChanged;

        /// Gets the bitmap to display
        public ImageSource ImageSource
        {
            get
            {
                return this._bodyIndexBitmap;
            }
        }

        /// Gets or sets the current status text to display
        public string StatusText
        {
            get
            {
                return this._statusText;
            }

            set
            {
                if (this._statusText != value)
                {
                    this._statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }


        /// Execute shutdown tasks
        /// </summary>
        /// <param name ="sender">object sending the event</param>
        /// <param name ="e">event arguments</param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this._bodyIndexFrameReader != null)
            {
                Exit_Click();

                // remove the event handler
                this._bodyIndexFrameReader.FrameArrived -= this.Reader_FrameArrived;

                // BodyIndexFrameReder is IDisposable
                this._bodyIndexFrameReader.Dispose();
                this._bodyIndexFrameReader = null;
            }


        }

        // Button1: Save / Back
        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            string Label1 = Button1.Content.ToString();
            if (Label1.Equals("Record"))
            {
                Button1.Content = "Save";
                Button2.Content = "Back";

                if (_firstFrame)
                {
                    _firstFrame = false;
                    _recorder.Start();
                }
                else
                {
                    _recorder.Continue();
                }
            }
            else
            {
                _recorder.Pause();
                Button1.Content = "Record";
                Button2.Content = "Exit";

            }
        }


        // Button 2: Record / Exit
        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            string Label2 = Button2.Content.ToString();

            if (Label2.Equals("Exit"))
            {
                Exit_Click();
            }
            else
            {
                _recorder.Discard();
                Button1.Content = "Record";
                Button2.Content = "Exit";
            }

        }

        //TODO - Voltar para a tela inicial
        private void Exit_Click() {

            if (_recorder.IsRecording)
            {
                _recorder.Stop();


                SaveFileDialog dialog = new SaveFileDialog
                {
                    Filter = "WEKA files|*.arff"
                };

                dialog.ShowDialog();

                if (!string.IsNullOrWhiteSpace(dialog.FileName))
                {
                    System.IO.File.Copy(_recorder.Result, dialog.FileName);
                }
            }

        }


        /// Handles the body index frame data arriving from the sensor
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyIndexFrameArrivedEventArgs e)
        {
            bool bodyIndexFrameProcessed = false;

            using (BodyIndexFrame bodyIndexFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyIndexFrame != null)
                {
                    // the fastest way to process the body index data is to directly access
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer bodyIndexBuffer = bodyIndexFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if (((this._bodyIndexFrameDescription.Width * this._bodyIndexFrameDescription.Height) == bodyIndexBuffer.Size) &&
                            (this._bodyIndexFrameDescription.Width == this._bodyIndexBitmap.PixelWidth) && (this._bodyIndexFrameDescription.Height == this._bodyIndexBitmap.PixelHeight))
                        {
                            this.ProcessBodyIndexFrameData(bodyIndexBuffer.UnderlyingBuffer, bodyIndexBuffer.Size);
                            bodyIndexFrameProcessed = true;
                        }
                    }
                }
            }

            if (bodyIndexFrameProcessed)
            {
                this.RenderBodyIndexPixels();
            }
        }

        /// Directly accesses the underlying image buffer of the BodyIndexFrame to
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the bodyIndexFrameData pointer.
        /// <param name="bodyIndexFrameData">Pointer to the BodyIndexFrame image data</param>
        /// <param name="bodyIndexFrameDataSize">Size of the BodyIndexFrame image data</param>
        private unsafe void ProcessBodyIndexFrameData(IntPtr bodyIndexFrameData, uint bodyIndexFrameDataSize)
        {
            byte* frameData = (byte*)bodyIndexFrameData;

            // convert body index to a visual representation
            for (int i = 0; i < (int)bodyIndexFrameDataSize; ++i)
            {
                // the BodyColor array has been sized to match
                // BodyFrameSource.BodyCount
                if (frameData[i] < BodyColor.Length)
                {
                    // this pixel is part of a player,
                    // display the appropriate color
                    this._bodyIndexPixels[i] = BodyColor[frameData[i]];
                }
                else
                {
                    // this pixel is not part of a player
                    // display black
                    this._bodyIndexPixels[i] = 0x00000000;
                }
            }
        }

        /// Renders color pixels into the writeableBitmap.
        private void RenderBodyIndexPixels()
        {
            this._bodyIndexBitmap.WritePixels(
                new Int32Rect(0, 0, this._bodyIndexBitmap.PixelWidth, this._bodyIndexBitmap.PixelHeight),
                this._bodyIndexPixels,
                this._bodyIndexBitmap.PixelWidth * (int)BytesPerPixel,
                0);
        }

        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this._kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }


        //chamar função na tela inicial para instanciar objetos (ligar o kinect)
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _kinectSensor = KinectSensor.GetDefault();

            if (_kinectSensor != null)
            {
                _kinectSensor.Open();

                _bodies = new Body[_kinectSensor.BodyFrameSource.BodyCount];

                _reader = _kinectSensor.BodyFrameSource.OpenReader();
                _reader.FrameArrived += BodyReader_FrameArrived;

                _recorder = new KinectFileManager();
            }
        }

        void BodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    frame.GetAndRefreshBodyData(_bodies);

                    Body body = _bodies.Where(b => b != null && b.IsTracked).FirstOrDefault();

                    if (body != null)
                    {
                        _recorder.Update(body);
                    }
                }
            }
        }
    }
}
