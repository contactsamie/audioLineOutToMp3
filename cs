using CSCore;
using CSCore.Codecs;
using CSCore.Codecs.WAV;
using CSCore.SoundIn;
using NAudio.Lame;
using NAudio.Wave;
using System;
using System.IO;
using System.Windows.Forms;
using MediaFoundationEncoder = CSCore.MediaFoundation.MediaFoundationEncoder;
using WasapiLoopbackCapture = CSCore.SoundIn.WasapiLoopbackCapture;

namespace AudioRecorder
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private WasapiCapture _capture;
        private WaveWriter _w;
        private static readonly string _file = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + "aaa1";

        private void button1_Click(object sender, EventArgs e)
        {
            _capture = new WasapiLoopbackCapture();
            _capture.Initialize();
            _w = new WaveWriter(_file + ".wav", _capture.WaveFormat);
            _capture.DataAvailable += (s, capData) => _w.Write(capData.Data, capData.Offset, capData.ByteCount);
            _capture.Start();
        }

        public void CutAnMp3File()
        {
            var startTimeSpan = TimeSpan.FromSeconds(20);
            var endTimeSpan = TimeSpan.FromSeconds(50);

            using (IWaveSource source = CodecFactory.Instance.GetCodec(_file + ".mp3"))
            using (var mediaFoundationEncoder =
                MediaFoundationEncoder.CreateWMAEncoder(source.WaveFormat, _file + "_cut.mp3"))
            {
                AddTimeSpan(source, mediaFoundationEncoder, startTimeSpan, endTimeSpan);
            }
        }

        private static void AddTimeSpan(IWaveSource source, IWriteable mediaFoundationEncoder, TimeSpan startTimeSpan, TimeSpan endTimeSpan)
        {
            source.SetPosition(startTimeSpan);

            int read = 0;
            long bytesToEncode = source.GetBytes(endTimeSpan - startTimeSpan);

            var buffer = new byte[source.WaveFormat.BytesPerSecond];
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                int bytesToWrite = (int)Math.Min(read, bytesToEncode);
                mediaFoundationEncoder.Write(buffer, 0, bytesToWrite);
                bytesToEncode -= bytesToWrite;
            }
        }

        private static void ConsoleRecord()
        {
            using (WasapiCapture capture = new WasapiLoopbackCapture())
            {
                capture.Initialize();

                using (var w = new WaveWriter(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + "dump.wav", capture.WaveFormat))
                {
                    capture.DataAvailable += (s, ee) => w.Write(ee.Data, ee.Offset, ee.ByteCount);
                    capture.Start();
                    Console.ReadKey();
                    capture.Stop();
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (_w == null || _capture == null) return;
            _capture.Stop();
            _w.Dispose();
            _w = null;
            _capture.Dispose();
            _capture = null;
        }

        public static void WaveToMp3(string waveFileName, string mp3FileName, int bitRate = 128)
        {
            using (var reader = new NAudio.Wave.WaveFileReader(waveFileName))
            using (var writer = new LameMP3FileWriter(mp3FileName, reader.WaveFormat, bitRate))
                reader.CopyTo(writer);
        }

        public static void Mp3ToWave(string mp3FileName, string waveFileName)
        {
            using (var reader = new NAudio.Wave.Mp3FileReader(mp3FileName))
            using (var writer = new NAudio.Wave.WaveFileWriter(waveFileName, reader.WaveFormat))
                reader.CopyTo(writer);
        }

        public static byte[] ConvertWavToMp3(byte[] wavFile)
        {
            using (var retMs = new MemoryStream())
            using (var ms = new MemoryStream(wavFile))
            using (var rdr = new NAudio.Wave.WaveFileReader(ms))
            using (var wtr = new LameMP3FileWriter(retMs, rdr.WaveFormat, 128))
            {
                rdr.CopyTo(wtr);
                return retMs.ToArray();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            WaveToMp3(_file + ".wav", _file + ".mp3", 256);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            CutAnMp3File();
        }

        public NAudio.Wave.WaveIn waveSource = null;
        public NAudio.Wave.WaveFileWriter waveFile = null;

        private void button5_Click(object sender, EventArgs e)
        {
            waveSource = new NAudio.Wave.WaveIn { WaveFormat = new NAudio.Wave.WaveFormat(44100, 1) };

            waveSource.DataAvailable += waveSource_DataAvailable;
            waveSource.RecordingStopped += waveSource_RecordingStopped;

            waveFile = new WaveFileWriter(_file + "_recordings.wav", waveSource.WaveFormat);

            waveSource.StartRecording();
        }

        private decimal intCounter = 0;

        private void waveSource_DataAvailable(object sender, NAudio.Wave.WaveInEventArgs e)
        {
            if (waveFile == null) return;

            var Sum2 = ProcessData(e) * 100;

            // If the Mean-Square is greater than a threshold, set a flag to indicate that noise has happened
            if (Sum2 > AudioThresh)
            {
                intCounter++;
                label1.Text = Sum2.ToString() + "__yeah_" + intCounter;
                //waveFile.Write(e.Buffer, 0, e.BytesRecorded);
                //waveFile.Flush();
            }
            else
            {
                label1.Text = Sum2.ToString() + "_no_sound";
            }

            waveFile.Write(e.Buffer, 0, e.BytesRecorded);
            waveFile.Flush();
        }

        private void waveSource_RecordingStopped(object sender, NAudio.Wave.StoppedEventArgs e)
        {
            if (waveSource != null)
            {
                waveSource.Dispose();
                waveSource = null;
            }

            if (waveFile != null)
            {
                waveFile.Dispose();
                waveFile = null;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            waveSource.StopRecording();
        }

        //calculate the sound level based on the AudioThresh
        private double ProcessData(WaveInEventArgs e)
        {
            bool result = false;

            bool Tr = false;
            double Sum2 = 0;
            int Count = e.BytesRecorded / 2;
            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                double Tmp = (short)((e.Buffer[index + 1] << 8) | e.Buffer[index + 0]);
                Tmp /= 32768.0;
                Sum2 += Tmp * Tmp;
                if (Tmp > AudioThresh)
                    Tr = true;
            }

            Sum2 /= Count;

            return Sum2;
        }
        public static void Mp4ToMp3(string waveFileName, string mp3FileName, int bitRate = 128)
        {
         
            using (var reader =  new MediaFoundationReader(waveFileName))
            using (var writer = new LameMP3FileWriter(mp3FileName, reader.WaveFormat, bitRate))
                reader.CopyTo(writer);
        }
         private void button7_Click(object sender, EventArgs e)
        {
            Mp4ToMp3(_file + "\\" + txtFileName.Text + ".mp4", _file + "\\" + txtFileName.Text + ".mp3", 256);
        }
        public double AudioThresh = 5;
    }
}


<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="CSCore" version="1.0.0.0" targetFramework="net45" />
  <package id="NAudio" version="1.7" targetFramework="net45" />
  <package id="NAudio.Lame" version="1.0.2" targetFramework="net45" />
</packages>

Install-Package CSCore -Version 1.0.0    -Source nuget.org
Install-Package NAudio -Version 1.7    -Source nuget.org
Install-Package NAudio.Lame -Version 1.0.2    -Source nuget.org




