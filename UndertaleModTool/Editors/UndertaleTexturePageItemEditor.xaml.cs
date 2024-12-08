using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageMagick;
using Microsoft.Win32;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using UndertaleModTool.Windows;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleTexturePageItemEditor.xaml
    /// </summary>
    public partial class UndertaleTexturePageItemEditor : DataUserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        /// <summary>
        /// Handle on the texture page item we're listening for updates from.
        /// </summary>
        private UndertaleTexturePageItem _textureItemContext = null;

        /// <summary>
        /// Handle on the texture data where we're listening for updates from.
        /// </summary>
        private UndertaleEmbeddedTexture.TexData _textureDataContext = null;

        public UndertaleTexturePageItemEditor()
        {
            InitializeComponent();

            DataContextChanged += SwitchDataContext;
            Unloaded += UnloadTexture;
        }

        private void UpdateImages(UndertaleTexturePageItem item)
        {
            if (item.TexturePage?.TextureData?.Image is null)
            {
                ItemTextureBGImage.Source = null;
                ItemTextureImage.Source = null;
                return;
            }

            GMImage image = item.TexturePage.TextureData.Image;
            BitmapSource bitmap = mainWindow.GetBitmapSourceForImage(image);
            ItemTextureBGImage.Source = bitmap;
            ItemTextureImage.Source = bitmap;
        }

        private void SwitchDataContext(object sender, DependencyPropertyChangedEventArgs e)
        {
            UndertaleTexturePageItem item = (DataContext as UndertaleTexturePageItem);
            if (item is null)
                return;

            // Load current image
            UpdateImages(item);

            // Start listening for texture page updates
            if (_textureItemContext is not null)
            {
                _textureItemContext.PropertyChanged -= ReloadTexturePage;
            }
            _textureItemContext = item;
            _textureItemContext.PropertyChanged += ReloadTexturePage;

            // Start listening for texture image updates
            if (_textureDataContext is not null)
            {
                _textureDataContext.PropertyChanged -= ReloadTextureImage;
            }

            if (item.TexturePage?.TextureData is not null)
            {
                _textureDataContext = item.TexturePage.TextureData;
                _textureDataContext.PropertyChanged += ReloadTextureImage;
            }
        }

        private void ReloadTexturePage(object sender, PropertyChangedEventArgs e)
        {
            // Invoke dispatcher to only perform updates on UI thread
            Dispatcher.Invoke(() =>
            {
                UndertaleTexturePageItem item = (DataContext as UndertaleTexturePageItem);
                if (item is null)
                    return;

                if (e.PropertyName != nameof(UndertaleTexturePageItem.TexturePage))
                    return;

                UpdateImages(item);

                // Start listening for (new) texture image updates
                if (_textureDataContext is not null)
                {
                    _textureDataContext.PropertyChanged -= ReloadTextureImage;
                }
                _textureDataContext = item.TexturePage.TextureData;
                _textureDataContext.PropertyChanged += ReloadTextureImage;
            });
        }

        private void ReloadTextureImage(object sender, PropertyChangedEventArgs e)
        {
            // Invoke dispatcher to only perform updates on UI thread
            Dispatcher.Invoke(() =>
            {
                UndertaleTexturePageItem item = (DataContext as UndertaleTexturePageItem);
                if (item is null)
                    return;

                if (e.PropertyName != nameof(UndertaleEmbeddedTexture.TexData.Image))
                    return;

                // If the texture's image was updated, reload it
                UpdateImages(item);
            });
        }

        private void UnloadTexture(object sender, RoutedEventArgs e)
        {
            ItemTextureBGImage.Source = null;
            ItemTextureImage.Source = null;

            // Stop listening for texture page updates
            if (_textureItemContext is not null)
            {
                _textureItemContext.PropertyChanged -= ReloadTexturePage;
                _textureItemContext = null;
            }

            // Stop listening for texture image updates
            if (_textureDataContext is not null)
            {
                _textureDataContext.PropertyChanged -= ReloadTextureImage;
                _textureDataContext = null;
            }
        }

        bool ImportImage(string filePath)
        {
            try
            {
                using MagickImage image = TextureWorker.ReadBGRAImageFromFile(filePath);
                UndertaleTexturePageItem item = DataContext as UndertaleTexturePageItem;

                var previousFormat = item.TexturePage.TextureData.Image.Format;

                item.ReplaceTexture(image);

                var currentFormat = item.TexturePage.TextureData.Image.Format;

                // If texture was DDS, warn user that texture has been converted to PNG
                if (previousFormat == GMImage.ImageFormat.Dds && currentFormat == GMImage.ImageFormat.Png)
                {
                    mainWindow.ShowMessage($"{item.TexturePage} was converted into PNG format since we don't support converting images into DDS format. This might have performance issues in the game.");
                }

                // Refresh the image of "ItemDisplay"
                if (ItemDisplay.FindName("RenderAreaBorder") is not Border border)
                    return true;
                if (border.Background is not ImageBrush brush)
                    return true;
                BindingOperations.GetBindingExpression(brush, ImageBrush.ImageSourceProperty)?.UpdateTarget();

                return true;
            }
            catch (Exception ex)
            {
                mainWindow.ShowError(ex.Message, "Failed to import image");
                return false;
            }
        }

        bool ExportImage(string filePath)
        {
            using TextureWorker worker = new();
            try
            {
                worker.ExportAsPNG((UndertaleTexturePageItem)DataContext, filePath);
                return true;
            }
            catch (Exception ex)
            {
                mainWindow.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                return false;
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG files (.png)|*.png|All files|*";

            if (!(dlg.ShowDialog() ?? false))
                return;

            ImportImage(dlg.FileName);
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG files (.png)|*.png|All files|*";

            if (dlg.ShowDialog() == true)
            {
                ExportImage(dlg.FileName);
            }
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            UndertaleTexturePageItem item = (DataContext as UndertaleTexturePageItem);

            CancellationTokenSource cancellationTokenSource = new();

            LoaderDialog dialog = new("Image editor open", "Waiting for image editor to close...");
            dialog.Owner = mainWindow;
            dialog.Maximum = null;
            dialog.Closed += (sender, e) =>
            {
                cancellationTokenSource.Cancel();
            };
            dialog.Update("");

            dialog.Show();
            mainWindow.IsEnabled = false;

            // Export file to temp folder
            string directory = Path.Join(Path.GetTempPath(), "UndertaleModTool");
            Directory.CreateDirectory(directory);

            string path = Path.Join(directory, $"Temp {DateTimeOffset.Now.ToUnixTimeMilliseconds()} {item.Name.Content}.png");

            try
            {
                if (!ExportImage(path))
                    return;

                // Open in edit mode
                Process process = new();
                process.StartInfo.FileName = path;
                process.StartInfo.Verb = "edit";
                process.StartInfo.UseShellExecute = true;
                process.Start();


                await process.WaitForExitAsync(cancellationTokenSource.Token);
                ImportImage(path);
            }
            catch (OperationCanceledException)
            {
                // Nothing to do
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
                dialog.Close();
                mainWindow.IsEnabled = true;
            }
        }

        private void FindReferencesButton_Click(object sender, RoutedEventArgs e)
        {
            var obj = (sender as FrameworkElement)?.DataContext;
            if (obj is not UndertaleTexturePageItem item)
                return;

            FindReferencesTypesDialog dialog = null;
            try
            {
                dialog = new(item, mainWindow.Data);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                mainWindow.ShowError("An error occured in the object references related window.\n" +
                                     $"Please report this on GitHub.\n\n{ex}");
            }
            finally
            {
                dialog?.Close();
            }
        }
    }
}
