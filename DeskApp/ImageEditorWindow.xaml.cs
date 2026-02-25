using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Controls;

namespace DeskApp
{
    public partial class ImageEditorWindow : Window
    {
        private BitmapSource _original;
        private WriteableBitmap _working;
        private double _currentAngle = 0;

        public byte[]? EditedImageBytes { get; private set; }

        private double _cropX = 0, _cropY = 0, _cropSize = 100;
        private bool _isDragging;
        private Point _dragStart;
        private bool _isDraggingSelection;
        private Point _selectionDragStart;
        private double _selStartX, _selStartY;

        private double _lastSelU = 0, _lastSelV = 0, _lastSelW = 0;

        public ImageEditorWindow(Uri imageUri)
        {
            InitializeComponent();
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = imageUri;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            _original = bmp;
            _working = new WriteableBitmap(_original);
            EditorImage.Source = _working;

            InitializeOverlay();
        }

        public ImageEditorWindow(BitmapSource source)
        {
            InitializeComponent();
            source.Freeze();
            _original = source;
            _working = new WriteableBitmap(_original);
            EditorImage.Source = _working;

            InitializeOverlay();
        }

        private void InitializeOverlay()
        {
            this.Loaded += ImageEditorWindow_Loaded;

            TopLeftThumb.DragDelta += Thumb_DragDelta;
            TopRightThumb.DragDelta += Thumb_DragDelta;
            BottomLeftThumb.DragDelta += Thumb_DragDelta;
            BottomRightThumb.DragDelta += Thumb_DragDelta;
            OverlayCanvas.MouseLeftButtonDown += OverlayCanvas_MouseLeftButtonDown;
            OverlayCanvas.MouseMove += OverlayCanvas_MouseMove;
            OverlayCanvas.MouseLeftButtonUp += OverlayCanvas_MouseLeftButtonUp;

            try
            {
                RotationSlider.ValueChanged += RotationSlider_ValueChanged;
            }
            catch { }
        }

        private void ImageEditorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var imgWidth = OverlayCanvas.ActualWidth;
            var imgHeight = OverlayCanvas.ActualHeight;

            var min = Math.Min(imgWidth, imgHeight);
            _cropSize = min * 0.6;
            _cropX = (imgWidth - _cropSize) / 2;
            _cropY = (imgHeight - _cropSize) / 2;

            UpdateCropVisuals();
        }

        private void UpdateCropVisuals()
        {
            ClampSelection();
            Canvas.SetLeft(CropRect, _cropX);
            Canvas.SetTop(CropRect, _cropY);
            CropRect.Width = _cropSize;
            CropRect.Height = _cropSize;

            var x1 = _cropX + _cropSize / 3.0;
            var x2 = _cropX + 2 * _cropSize / 3.0;
            var y1 = _cropY + _cropSize / 3.0;
            var y2 = _cropY + 2 * _cropSize / 3.0;

            GridLineV1.X1 = x1; GridLineV1.X2 = x1; GridLineV1.Y1 = _cropY; GridLineV1.Y2 = _cropY + _cropSize;
            GridLineV2.X1 = x2; GridLineV2.X2 = x2; GridLineV2.Y1 = _cropY; GridLineV2.Y2 = _cropY + _cropSize;
            GridLineH1.X1 = _cropX; GridLineH1.X2 = _cropX + _cropSize; GridLineH1.Y1 = y1; GridLineH1.Y2 = y1;
            GridLineH2.X1 = _cropX; GridLineH2.X2 = _cropX + _cropSize; GridLineH2.Y1 = y2; GridLineH2.Y2 = y2;

            Canvas.SetLeft(TopLeftThumb, _cropX - TopLeftThumb.Width/2);
            Canvas.SetTop(TopLeftThumb, _cropY - TopLeftThumb.Height/2);
            Canvas.SetLeft(TopRightThumb, _cropX + _cropSize - TopRightThumb.Width/2);
            Canvas.SetTop(TopRightThumb, _cropY - TopRightThumb.Height/2);
            Canvas.SetLeft(BottomLeftThumb, _cropX - BottomLeftThumb.Width/2);
            Canvas.SetTop(BottomLeftThumb, _cropY + _cropSize - BottomLeftThumb.Height/2);
            Canvas.SetLeft(BottomRightThumb, _cropX + _cropSize - BottomRightThumb.Width/2);
            Canvas.SetTop(BottomRightThumb, _cropY + _cropSize - BottomRightThumb.Height/2);
        }

        private void ClampSelection()
        {
            var maxW = OverlayCanvas.ActualWidth;
            var maxH = OverlayCanvas.ActualHeight;
            if (maxW <= 0 || maxH <= 0) return;

            var maxSize = Math.Min(maxW, maxH);
            if (_cropSize > maxSize) _cropSize = maxSize;
            if (_cropSize < 20) _cropSize = 20;

            if (_cropX < 0) _cropX = 0;
            if (_cropY < 0) _cropY = 0;
            if (_cropX + _cropSize > maxW) _cropX = Math.Max(0, maxW - _cropSize);
            if (_cropY + _cropSize > maxH) _cropY = Math.Max(0, maxH - _cropSize);
        }

        private void Thumb_DragDelta(object? sender, DragDeltaEventArgs e)
        {
            var origSize = _cropSize;
            double delta = Math.Max(e.HorizontalChange, e.VerticalChange);

            if (sender == TopLeftThumb)
            {
                _cropX += delta;
                _cropY += delta;
                _cropSize -= delta;
            }
            else if (sender == TopRightThumb)
            {
                _cropY += delta;
                _cropSize += delta;
            }
            else if (sender == BottomLeftThumb)
            {
                _cropX += delta;
                _cropSize += delta;
            }
            else if (sender == BottomRightThumb)
            {
                _cropSize += delta;
            }

            if (_cropSize < 20) _cropSize = 20;
            ClampSelection();
            UpdateCropVisuals();
        }

        private void OverlayCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(OverlayCanvas);
            var left = Canvas.GetLeft(CropRect);
            var top = Canvas.GetTop(CropRect);
            var right = left + CropRect.Width;
            var bottom = top + CropRect.Height;
            if (p.X >= left && p.X <= right && p.Y >= top && p.Y <= bottom)
            {
                _isDraggingSelection = true;
                _selectionDragStart = p;
                _selStartX = _cropX;
                _selStartY = _cropY;
                OverlayCanvas.CaptureMouse();
            }
            else
            {
                _isDragging = true;
                _dragStart = p;
                OverlayCanvas.CaptureMouse();
            }
        }

        private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(OverlayCanvas);
            if (_isDraggingSelection)
            {
                var dx = pos.X - _selectionDragStart.X;
                var dy = pos.Y - _selectionDragStart.Y;
                _cropX = _selStartX + dx;
                _cropY = _selStartY + dy;
                ClampSelection();
                UpdateCropVisuals();
                return;
            }

            if (!_isDragging) return;
            var dx2 = pos.X - _dragStart.X;
            var dy2 = pos.Y - _dragStart.Y;
            _cropX += dx2;
            _cropY += dy2;
            _dragStart = pos;
            ClampSelection();
            UpdateCropVisuals();
        }

        private void OverlayCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            if (_isDraggingSelection)
            {
                _isDraggingSelection = false;
            }
            OverlayCanvas.ReleaseMouseCapture();
        }

        private void RotationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                var angle = e.NewValue;
                if (RotationValueText != null) RotationValueText.Text = $"{Math.Round(angle)}°";

                _currentAngle = angle;
                var rt = new RotateTransform(angle);
                EditorImage.RenderTransform = rt;
                EditorImage.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            catch { }
        }

        private void RotateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var rotated = new TransformedBitmap(_working, new RotateTransform(90));
                EditorImage.Source = rotated;
                var wb = new WriteableBitmap(rotated as BitmapSource);
                _working = wb;
            }
            catch { }
        }

        private void CropButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyCropFromSelection();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(_working));
                using var ms = new MemoryStream();
                encoder.Save(ms);
                EditedImageBytes = ms.ToArray();
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar imagen: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ApplyCropFromSelection()
        {
            try
            {
                var img = EditorImage;
                if (!(img.Source is BitmapSource bs)) return;

                var dispW = img.ActualWidth;
                var dispH = img.ActualHeight;
                if (dispW <= 0 || dispH <= 0) return;

                int rtbPixelWidth = Math.Max(1, (int)Math.Round(dispW));
                int rtbPixelHeight = Math.Max(1, (int)Math.Round(dispH));

                var rtb = new RenderTargetBitmap(rtbPixelWidth, rtbPixelHeight, 96, 96, PixelFormats.Pbgra32);

                var dv = new DrawingVisual();
                using (var dc = dv.RenderOpen())
                {
                    dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, dispW, dispH));

                    var dest = new Rect(0, 0, dispW, dispH);

                    dc.PushTransform(new TranslateTransform(dispW / 2.0, dispH / 2.0));
                    if (EditorImage.RenderTransform is RotateTransform rt)
                    {
                        dc.PushTransform(new RotateTransform(rt.Angle));
                    }
                    dc.PushTransform(new TranslateTransform(-dispW / 2.0, -dispH / 2.0));

                    dc.DrawImage(bs, dest);

                    dc.Pop();
                    if (EditorImage.RenderTransform is RotateTransform)
                        dc.Pop();
                    dc.Pop();
                }

                rtb.Render(dv);

                var scaleX = (double)rtb.PixelWidth / dispW;
                var scaleY = (double)rtb.PixelHeight / dispH;

                var x = (int)Math.Max(0, Math.Round(_cropX * scaleX));
                var y = (int)Math.Max(0, Math.Round(_cropY * scaleY));
                var size = (int)Math.Round(_cropSize * Math.Max(scaleX, scaleY));

                if (x + size > rtb.PixelWidth) size = rtb.PixelWidth - x;
                if (y + size > rtb.PixelHeight) size = rtb.PixelHeight - y;
                if (size <= 0) return;

                var cb = new CroppedBitmap(rtb, new Int32Rect(x, y, size, size));
                var wb = new WriteableBitmap(cb);
                _working = wb;
                EditorImage.Source = _working;

                this.Dispatcher.InvokeAsync(() =>
                {
                    var newDispW = EditorImage.ActualWidth;
                    var newDispH = EditorImage.ActualHeight;
                    if (newDispW <= 0 || newDispH <= 0)
                    {
                        _cropSize = 100; _cropX = 0; _cropY = 0;
                        UpdateCropVisuals();
                        return;
                    }

                    if (newDispW >= newDispH)
                    {
                        _cropSize = newDispH;
                        _cropX = (newDispW - _cropSize) / 2.0;
                        _cropY = 0;
                    }
                    else
                    {
                        _cropSize = newDispW;
                        _cropX = 0;
                        _cropY = (newDispH - _cropSize) / 2.0;
                    }

                    UpdateCropVisuals();
                });
            }
            catch { }
        }
    }
}
