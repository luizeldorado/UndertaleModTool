﻿using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using NAudio.Vorbis;
using NAudio.Wave;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleEmbeddedAudioEditor.xaml
    /// </summary>
    public partial class UndertaleEmbeddedAudioEditor : DataUserControl
    {
        private WaveOutEvent waveOut;
        private WaveFileReader wavReader;
        private VorbisWaveReader oggReader;

        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        public UndertaleEmbeddedAudioEditor()
        {
            InitializeComponent();
            this.Unloaded += Unload;
        }

        public void Unload(object sender, RoutedEventArgs e)
        {
            if (waveOut != null)
                waveOut.Stop();
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            UndertaleEmbeddedAudio target = DataContext as UndertaleEmbeddedAudio;

            OpenFileDialog dlg = new OpenFileDialog();

            dlg.DefaultExt = ".wav";
            dlg.Filter = "WAV files (.wav)|*.wav|All files|*";

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    byte[] data = File.ReadAllBytes(dlg.FileName);

                    // TODO: Make sure it's valid WAV

                    target.Data = data;
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to import file: " + ex.Message, "Failed to import file");
                }
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            UndertaleEmbeddedAudio target = DataContext as UndertaleEmbeddedAudio;

            SaveFileDialog dlg = new SaveFileDialog();

            dlg.DefaultExt = ".wav";
            dlg.Filter = "WAV files (.wav)|*.wav|All files|*";

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllBytes(dlg.FileName, target.Data);
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                }
            }
        }

        private void InitAudio()
        {
            if (waveOut == null)
                waveOut = new WaveOutEvent() { DeviceNumber = 0 };
            else if (waveOut.PlaybackState != PlaybackState.Stopped)
                waveOut.Stop();
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            UndertaleEmbeddedAudio target = DataContext as UndertaleEmbeddedAudio;

            if (target.Data.Length > 4)
            {
                try
                {
                    if (target.Data[0] == 'R' && target.Data[1] == 'I' && target.Data[2] == 'F' && target.Data[3] == 'F')
                    {
                        wavReader = new WaveFileReader(new MemoryStream(target.Data));
                        InitAudio();
                        waveOut.Init(wavReader);
                        waveOut.Play();
                    }
                    else if (target.Data[0] == 'O' && target.Data[1] == 'g' && target.Data[2] == 'g' && target.Data[3] == 'S')
                    {
                        oggReader = new VorbisWaveReader(new MemoryStream(target.Data));
                        InitAudio();
                        waveOut.Init(oggReader);
                        waveOut.Play();
                    }
                    else
                        mainWindow.ShowError("Failed to play audio!\r\nNot a WAV or OGG.", "Audio failure");
                }
                catch (Exception ex)
                {
                    waveOut = null;
                    mainWindow.ShowError("Failed to play audio!\r\n" + ex.Message, "Audio failure");
                }
            }
        }


        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (waveOut != null)
                waveOut.Stop();
        }
    }
}
