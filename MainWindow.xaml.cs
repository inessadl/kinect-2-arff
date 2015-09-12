
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




    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Size of the RGB pixel in the bitmap
        /// </summary>
        private const int BytesPerPixel = 4;

        /// <summary>
        /// Collection of colors to be used to display the BodyIndexFrame data.
        /// </summary>
        /// TODO: Trocar isso por vermelho para inválido e verde para válido (talvez uma outra cor para gravando)
        /// TODO: Verificar problema de não inicializar com qualquer cor
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



        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor _kinectSensor = null;

        /// <summary>
        /// Reader for body index frames
        /// </summary>
        private BodyIndexFrameReader _bodyIndexFrameReader = null;

        /// <summary>
        /// Description of the data contained in the body index frame
        /// </summary>
        private FrameDescription _bodyIndexFrameDescription = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap _bodyIndexBitmap = null;

        /// <summary>
        /// Intermediate storage for frame data converted to color
        /// </summary>
        private uint[] _bodyIndexPixels = null;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string _statusText = null;

        /// <sumarry>
        /// Buffer to save data
        /// </sumarry>
        KinectFileManager _recorder = null;
        BodyFrameReader _reader = null;
        IList<Body> _bodies = null;
        bool _firstFrame = true;


        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // get the _kinectSensor object
            this._kinectSensor = KinectSensor.GetDefault();

            // open the reader for the depth frames
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

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();

            _kinectSensor = KinectSensor.GetDefault();

            //TODO: Quando a tela inicial estiver pronta apagar isso e chamar a tela inicial - Window_Loaded
            if (_kinectSensor != null)
            {
                _kinectSensor.Open();

                _bodies = new Body[_kinectSensor.BodyFrameSource.BodyCount];

                _reader = _kinectSensor.BodyFrameSource.OpenReader();
                _reader.FrameArrived += BodyReader_FrameArrived;

                _recorder = new KinectFileManager();
            }


        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this._bodyIndexBitmap;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
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

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
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

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            // Button button = sender as Button;


            if (this._bodyIndexBitmap != null)
            {
                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();

                // create frame from the writable bitmap and add to encoder
                encoder.Frames.Add(BitmapFrame.Create(this._bodyIndexBitmap));

                string time = System.DateTime.UtcNow.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

                string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                string path = Path.Combine(myPhotos, "KinectScreenshot-BodyIndex-" + time + ".png");

                // write the new file to disk
                try
                {
                    // FileStream is IDisposable
                    using (FileStream fs = new FileStream(path, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }

                    this.StatusText = string.Format(CultureInfo.CurrentCulture, Properties.Resources.SavedScreenshotStatusTextFormat, path);
                }
                catch (IOException)
                {
                    this.StatusText = string.Format(CultureInfo.CurrentCulture, Properties.Resources.FailedScreenshotStatusTextFormat, path);
                }
            }

            // button.Content = "Teste";

        }


        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            string Label1 = Button1.Content.ToString();
            if (Label1.Equals("Gravar"))
            {
                Button1.Content = "Efetivar";
                Button2.Content = "Descartar";

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
                Button1.Content = "Gravar";
                Button2.Content = "Sair";

            }
        }


        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            string Label2 = Button2.Content.ToString();

            if (Label2.Equals("Sair"))
            {
                Exit_Click();
            }
            else
            {
                _recorder.Discard();
                Button1.Content = "Gravar";
                Button2.Content = "Sair";
            }

        }

        //TODO - Trocar para formato Arff
        //TODO - Voltar para a tela inicial
        private void Exit_Click() {

            if (_recorder.IsRecording)
            {
                _recorder.Stop();


                SaveFileDialog dialog = new SaveFileDialog
                {
                    Filter = "Excel files|*.csv"
                };

                dialog.ShowDialog();

                if (!string.IsNullOrWhiteSpace(dialog.FileName))
                {
                    System.IO.File.Copy(_recorder.Result, dialog.FileName);
                }
            }

        }

     
        /// <summary>
        /// Handles the body index frame data arriving from the sensor
        /// </summary>
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

        /// <summary>
        /// Directly accesses the underlying image buffer of the BodyIndexFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the bodyIndexFrameData pointer.
        /// </summary>
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

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderBodyIndexPixels()
        {
            this._bodyIndexBitmap.WritePixels(
                new Int32Rect(0, 0, this._bodyIndexBitmap.PixelWidth, this._bodyIndexBitmap.PixelHeight),
                this._bodyIndexPixels,
                this._bodyIndexBitmap.PixelWidth * (int)BytesPerPixel,
                0);
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
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
