using System;
using System.Configuration;
using System.Data;
using System.Windows;
using DeskApp.Services;

namespace DeskApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                SessionService.Instance.ClearSession();
            }
            catch
            {
            }

            base.OnExit(e);
        }
    }

}
