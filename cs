using System.Runtime.InteropServices;
using CSCore;
using CSCore.Codecs;
using CSCore.Codecs.WAV;
using CSCore.MediaFoundation;
using CSCore.SoundIn;
using NAudio.Lame;
using System;
using System.IO;
using System.Windows.Forms;

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

       
        public  void CutAnMp3File()
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

        static void ConsoleRecord()
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
    }
}



<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="CSCore" version="1.0.0.0" targetFramework="net45" />
  <package id="NAudio" version="1.7" targetFramework="net45" />
  <package id="NAudio.Lame" version="1.0.2" targetFramework="net45" />
</packages>

