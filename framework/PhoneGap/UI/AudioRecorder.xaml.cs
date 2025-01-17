﻿/*
 * PhoneGap is available under *either* the terms of the modified BSD license *or* the
 * MIT License (2008). See http://opensource.org/licenses/alphabetical for full text.
 *
 * Copyright (c) 2005-2011, Nitobi Software Inc.
 * Copyright (c) 2011, Microsoft Corporation
 * Copyright (c) 2011, Sergey Grebnov.
 */

using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using AudioResult = WP7GapClassLib.PhoneGap.UI.AudioCaptureTask.AudioResult;

namespace WP7GapClassLib.PhoneGap.UI
{
    /// <summary>
    /// Implements Audio Recording application
    /// </summary>
    public partial class AudioRecorder : PhoneApplicationPage
    {

        #region Constants

        private const string RecordingStartCaption = "Start";
        private const string RecordingStopCaption = "Stop";

        private const string LocalFolderName = "AudioCache";
        private const string FileNameFormat = "Audio-{0}.wav";

        #endregion

        #region Callbacks

        /// <summary>
        /// Occurs when a audio recording task is completed.
        /// </summary>
        public event EventHandler<AudioResult> Completed;

        #endregion

        #region Fields

        /// <summary>
        /// Audio source
        /// </summary>
        private Microphone microphone;

        /// <summary>
        /// Temporary buffer to store audio chunk
        /// </summary>
        private byte[] buffer;

        /// <summary>
        /// Recording duration
        /// </summary>
        private TimeSpan duration;
        
        /// <summary>
        /// Output buffer
        /// </summary>
        private MemoryStream memoryStream;

        /// <summary>
        /// Xna game loop dispatcher
        /// </summary>
        DispatcherTimer dtXna;

        /// <summary>
        /// Recording result, dispatched back when recording page is closed
        /// </summary>
        private AudioResult result = new AudioResult(TaskResult.Cancel);

        /// <summary>
        /// Whether we are recording audio now
        /// </summary>
        private bool IsRecording
        {
            get
            {
                return (this.microphone != null && this.microphone.State == MicrophoneState.Started);
            }
        }

        #endregion

        /// <summary>
        /// Creates new instance of the AudioRecorder class.
        /// </summary>
        public AudioRecorder()
        {
                       
            this.InitializeXnaGameLoop();

            // microphone requires special XNA initialization to work
            InitializeComponent();
        }

        /// <summary>
        /// Starts recording, data is stored in memory
        /// </summary>
        private void StartRecording()
        {
            this.microphone = Microphone.Default;
            this.microphone.BufferDuration = TimeSpan.FromMilliseconds(500);

            this.btnTake.IsEnabled = false;
            this.btnStartStop.Content = RecordingStopCaption;

            this.buffer = new byte[microphone.GetSampleSizeInBytes(this.microphone.BufferDuration)];
            this.microphone.BufferReady += new EventHandler<EventArgs>(MicrophoneBufferReady);

            this.memoryStream = new MemoryStream();
            this.WriteWavHeader(this.memoryStream, this.microphone.SampleRate);

            this.duration = new TimeSpan(0);
            
            this.microphone.Start();
        }

        /// <summary>
        /// Stops recording
        /// </summary>
        private void StopRecording()
        {
            this.microphone.Stop();

            this.microphone.BufferReady -= MicrophoneBufferReady;

            this.microphone = null;
            
            btnStartStop.Content = RecordingStartCaption;

            // check there is some data
            this.btnTake.IsEnabled = true;
        }

        /// <summary>
        /// Handles Start/Stop events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStartStop_Click(object sender, RoutedEventArgs e)
        {

            if (this.IsRecording)
            {
                this.StopRecording();
            }
            else
            {
                this.StartRecording();
            }
        }

        /// <summary>
        /// Handles Take button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnTake_Click(object sender, RoutedEventArgs e)
        {
            this.result = this.SaveAudioClipToLocalStorage();

            if (this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }

        /// <summary>
        /// Handles page closing event, stops recording if needed and dispatches results.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (this.IsRecording)
            {
                StopRecording();
            }

            this.FinalizeXnaGameLoop();

            if (this.Completed != null)
            {
                this.Completed(this, result);
            }

            base.OnNavigatedFrom(e);

        }

        /// <summary>
        /// Copies data from microphone to memory storages and updates recording state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MicrophoneBufferReady(object sender, EventArgs e)
        {
            this.microphone.GetData(this.buffer);
            this.memoryStream.Write(this.buffer, 0, this.buffer.Length);
            TimeSpan bufferDuration = this.microphone.BufferDuration;

            this.Dispatcher.BeginInvoke(() =>
            {
                this.duration += bufferDuration;

                this.txtDuration.Text = "Duration: " + 
                    this.duration.Minutes.ToString().PadLeft(2, '0') + ":" +
                    this.duration.Seconds.ToString().PadLeft(2, '0');
            });     
            
        }

        /// <summary>
        /// Writes audio data from memory to isolated storage
        /// </summary>
        /// <returns></returns>
        private AudioResult SaveAudioClipToLocalStorage()
        {
            if (this.memoryStream == null || this.memoryStream.Length <= 0)
            {
                return new AudioResult(TaskResult.Cancel);
            }

            this.UpdateWavHeader(this.memoryStream);
            
            // save audio data to local isolated storage

            string filename = String.Format(FileNameFormat, Guid.NewGuid().ToString());

            try
            {
                using (IsolatedStorageFile isoFile = IsolatedStorageFile.GetUserStoreForApplication())
                {

                    if (!isoFile.DirectoryExists(LocalFolderName))
                    {
                        isoFile.CreateDirectory(LocalFolderName);
                    }

                    string filePath = System.IO.Path.Combine("/" + LocalFolderName + "/", filename);

                    this.memoryStream.Seek(0, SeekOrigin.Begin);

                    using (IsolatedStorageFileStream fileStream = isoFile.CreateFile(filePath))
                    {

                        this.memoryStream.CopyTo(fileStream);
                    }

                    AudioResult result = new AudioResult(TaskResult.OK);
                    result.AudioFileName = filePath;

                    result.AudioFile = this.memoryStream;
                    result.AudioFile.Seek(0, SeekOrigin.Begin);

                    return result;
                }

                
                
            }
            catch (Exception)
            {
                //TODO: log or do something else
                throw;
            }
        }

        /// <summary>
        /// Special initialization required for the microphone: XNA game loop
        /// </summary>
        private void InitializeXnaGameLoop()
        {
            // Timer to simulate the XNA game loop (Microphone is from XNA)
            this.dtXna = new DispatcherTimer();
            this.dtXna.Interval = TimeSpan.FromMilliseconds(33);
            this.dtXna.Tick += delegate { try { FrameworkDispatcher.Update(); } catch { } };
            this.dtXna.Start();
        }
        /// <summary>
        /// Finalizes XNA game loop for microphone
        /// </summary>
        private void FinalizeXnaGameLoop()
        {
            // Timer to simulate the XNA game loop (Microphone is from XNA)
            this.dtXna.Stop();
            this.dtXna = null;
        }


        #region Wav format
        // Original source http://damianblog.com/2011/02/07/storing-wp7-recorded-audio-as-wav-format-streams/

        /// <summary>
        /// Adds wav file format header to the stream
        /// https://ccrma.stanford.edu/courses/422/projects/WaveFormat/
        /// </summary>
        /// <param name="stream">Wav stream</param>
        /// <param name="sampleRate">Sample Rate</param>
        private void WriteWavHeader(Stream stream, int sampleRate)
        {
            const int bitsPerSample = 16;
            const int bytesPerSample = bitsPerSample / 8;
            var encoding = System.Text.Encoding.UTF8;

            // ChunkID Contains the letters "RIFF" in ASCII form (0x52494646 big-endian form).
            stream.Write(encoding.GetBytes("RIFF"), 0, 4);

            // NOTE this will be filled in later
            stream.Write(BitConverter.GetBytes(0), 0, 4);

            // Format Contains the letters "WAVE"(0x57415645 big-endian form).
            stream.Write(encoding.GetBytes("WAVE"), 0, 4);

            // Subchunk1ID Contains the letters "fmt " (0x666d7420 big-endian form).
            stream.Write(encoding.GetBytes("fmt "), 0, 4);

            // Subchunk1Size 16 for PCM.  This is the size of therest of the Subchunk which follows this number.
            stream.Write(BitConverter.GetBytes(16), 0, 4);

            // AudioFormat PCM = 1 (i.e. Linear quantization) Values other than 1 indicate some form of compression.
            stream.Write(BitConverter.GetBytes((short)1), 0, 2);

            // NumChannels Mono = 1, Stereo = 2, etc.
            stream.Write(BitConverter.GetBytes((short)1), 0, 2);

            // SampleRate 8000, 44100, etc.
            stream.Write(BitConverter.GetBytes(sampleRate), 0, 4);

            // ByteRate =  SampleRate * NumChannels * BitsPerSample/8
            stream.Write(BitConverter.GetBytes(sampleRate * bytesPerSample), 0, 4);

            // BlockAlign NumChannels * BitsPerSample/8 The number of bytes for one sample including all channels.
            stream.Write(BitConverter.GetBytes((short)(bytesPerSample)), 0, 2);

            // BitsPerSample    8 bits = 8, 16 bits = 16, etc.
            stream.Write(BitConverter.GetBytes((short)(bitsPerSample)), 0, 2);

            // Subchunk2ID Contains the letters "data" (0x64617461 big-endian form).
            stream.Write(encoding.GetBytes("data"), 0, 4);

            // NOTE to be filled in later
            stream.Write(BitConverter.GetBytes(0), 0, 4);
        }

        /// <summary>
        /// Updates wav file format header
        /// https://ccrma.stanford.edu/courses/422/projects/WaveFormat/
        /// </summary>
        /// <param name="stream">Wav stream</param>
        private void UpdateWavHeader(Stream stream)
        {
            if (!stream.CanSeek) throw new Exception("Can't seek stream to update wav header");

            var oldPos = stream.Position;

            // ChunkSize  36 + SubChunk2Size
            stream.Seek(4, SeekOrigin.Begin);
            stream.Write(BitConverter.GetBytes((int)stream.Length - 8), 0, 4);

            // Subchunk2Size == NumSamples * NumChannels * BitsPerSample/8 This is the number of bytes in the data.
            stream.Seek(40, SeekOrigin.Begin);
            stream.Write(BitConverter.GetBytes((int)stream.Length - 44), 0, 4);

            stream.Seek(oldPos, SeekOrigin.Begin);
        }

        #endregion


    }
}