using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Threading;
using System.IO.Ports;

namespace WindowsFormsApplication2
{
    public partial class Form1 : Form
    {
        private VideoCapture _capture;
        private Thread _captureThread;
        int thresh;
        int threshred;
        int balance = 0;
        int red = 0;
        int leftside = 0;
        int rightside = 0;
        Mat frame;
        SerialPort _serialPort;
        byte[] buffer = {0x01, 0, 0};
        delegate void SetTextCallback(string text1, string text2);
        public Form1()
        {
            InitializeComponent();
            numericUpDown1.Value = 150;
            numericUpDown2.Value = 130;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _capture = new VideoCapture();
            _captureThread = new Thread(DisplayWebcam);
            _captureThread.Start();
            _serialPort = new SerialPort("COM4", 2400);
            _serialPort.DataBits = 8;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.Two;
            _serialPort.Open();
        }

        private void SetText(string text1, string text2)
        {
            if (this.textBox1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text1, text2 });
            }
            else
            {
                this.textBox1.Text = text1;
                this.textBox2.Text = text2;
            }
        }

        private void DisplayWebcam()
        {
            while (_capture.IsOpened)
            {
                frame = _capture.QueryFrame();
                CvInvoke.Resize(frame, frame, pictureBox1.Size);
                Image<Gray, Byte> processCapture = frame.ToImage<Gray, Byte>();
                Image<Hsv, Byte> redProcessCapture = frame.ToImage<Hsv, Byte>();
                processCapture._ThresholdBinary(new Gray(thresh), new Gray(255));
                redProcessCapture._ThresholdBinary(new Hsv(200,threshred,150),new Hsv(255,255,255));
                pictureBox1.Image = processCapture.Bitmap;
                pictureBox2.Image = redProcessCapture.Bitmap;
                leftside = 0;
                rightside = 0;
                balance = 0;
                red = 0;

                for (int x = 0; x < processCapture.Width / 2; x++)
                {
                    for (int y = 0; y < processCapture.Height; y++)
                    {
                        if (processCapture.Data[y, x, 0] == 255)
                            leftside++;
                    }
                }

                for (int x = processCapture.Width/2; x < processCapture.Width; x++)
                {
                    for (int y = 0; y < processCapture.Height; y++)
                    {
                        if (processCapture.Data[y, x, 0] == 255)
                            rightside++;
                    }
                }

                for(int x = 0; x < processCapture.Width; x++)
                {
                    for(int y = 0; y < processCapture.Height; y++)
                    {
                        if (processCapture.Data[y, x, 0] == 255)
                            balance += (x - processCapture.Width / 2);
                        if ((redProcessCapture.Data[y, x, 2] == 255))
                            red++;
                    }
                }
                balance /= 10000;

                if (balance < -40)
                {
                    buffer[1] = 0x0F;
                    buffer[2] = 0x6F;
                }
                else if(balance > 40)
                {
                    buffer[1] = 0x6F;
                    buffer[2] = 0x0F;
                }
                else
                {
                    buffer[1] = 0x6F;
                    buffer[2] = 0x6F;
                }
                if (red > 20000)
                {
                    this.SetText(balance.ToString(), red.ToString());
                    Thread.Sleep(1200);
                    buffer[1] = 0x6F;
                    buffer[2] = 0x5F;
                }
                else
                {
                    this.SetText(balance.ToString(), red.ToString());
                }
                _serialPort.Write(buffer, 0 ,3);
            }
        }

        private void numUpDown1_val(object sender, EventArgs e)
        {
            thresh = (byte)numericUpDown1.Value;
        }

        private void numeUpDown2_val(object sender, EventArgs e)
        {
            threshred = (byte)numericUpDown2.Value;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Ends the program when the camera window is closed.
            _captureThread.Abort();
        }
    }
}
