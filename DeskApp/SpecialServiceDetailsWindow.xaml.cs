using System;
using System.Windows;
using DeskApp.Models;

namespace DeskApp
{
    public partial class SpecialServiceDetailsWindow : Window
    {
        public SpecialServiceDetailsWindow(SpecialServiceData? data)
        {
            InitializeComponent();

            if (data == null)
            {
                TitleText.Text = "No hay datos";
                return;
            }

            var svc = data.SpecialService;

            var rawType = svc?.Type ?? data.Type ?? string.Empty;
            var friendlyType = SpecialServiceTypeDisplayConverter.Map(rawType);
            TitleText.Text = $"Detalle servicio: {friendlyType}";
            TypeText.Text = friendlyType == string.Empty ? "-" : friendlyType;
            StatusText.Text = svc?.Status ?? data.Status ?? "-";
            ModeText.Text = svc?.Mode ?? data.Mode ?? "-";
            DeliveryText.Text = (svc?.Delivery ?? data.Delivery)?.ToString("g") ?? "-";
            ObservationsText.Text = svc?.Observations ?? data.Observations ?? "-";

            var bound = svc?.Bound ?? data.Bound;
            if (bound != null)
            {
                BoundPanel.Visibility = Visibility.Visible;
                BoundCoverType.Text = Helpers.EnumDisplayHelpers.ToSpanish(bound.CoverType);
                BoundCoverColor.Text = Helpers.EnumDisplayHelpers.ToSpanish(bound.CoverColor);
                BoundSpiral.Text = Helpers.EnumDisplayHelpers.ToSpanish(bound.Spiral);
            }

            var spiral = svc?.Spiral ?? data.Spiral;
            if (spiral != null)
            {
                SpiralPanel.Visibility = Visibility.Visible;
                SpiralType.Text = Helpers.EnumDisplayHelpers.ToSpanish(spiral.SpiralType);
            }

            var doc = svc?.Document ?? data.Document;
            if (doc != null)
            {
                DocumentPanel.Visibility = Visibility.Visible;
                DocumentType.Text = Helpers.EnumDisplayHelpers.ToSpanish(doc.DocumentType);
            }

            var photo = svc?.Photo ?? data.Photo;
            if (photo != null)
            {
                PhotoPanel.Visibility = Visibility.Visible;
                PhotoSize.Text = Helpers.EnumDisplayHelpers.ToSpanishPhotoSize(photo.PhotoSize);
                PhotoPaperType.Text = Helpers.EnumDisplayHelpers.ToSpanish(photo.PaperType);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
