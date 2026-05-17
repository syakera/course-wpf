using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MedicalCenter.ViewModels;

namespace MedicalCenter.Views
{
    public partial class AddEditWindow : Window
    {
        public AddEditWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => ApplyCursor();
        }

        private void ApplyCursor()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Assets/cursor.cur");
                var info = Application.GetResourceStream(uri);
                if (info != null)
                    Cursor = new Cursor(info.Stream);
            }
            catch { }
        }

        private void DropZone_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                DropZone.BorderBrush = (Brush)FindResource("AccentBrush");
                DropHint.Text = "Отпустите для добавления";
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void DropZone_DragLeave(object sender, DragEventArgs e)
        {
            DropZone.BorderBrush = (Brush)FindResource("BorderBrush");
            DropHint.Text = "Перетащите изображения сюда";
        }

        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            DropZone.BorderBrush = (Brush)FindResource("BorderBrush");
            DropHint.Text = "Перетащите изображения сюда";

            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var imageExts = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };
            var images = files
                .Where(f => imageExts.Contains(Path.GetExtension(f).ToLower()))
                .ToList();

            if (images.Count == 0) return;

            var vm = DataContext as AddEditViewModel;
            if (vm == null) return;

            string existing = vm.ImagePathText;
            string appended = string.IsNullOrWhiteSpace(existing)
                ? string.Join("; ", images)
                : existing + "; " + string.Join("; ", images);

            vm.ImagePathText = appended;
            ImgPathBox.Text = appended;
        }
    }
}