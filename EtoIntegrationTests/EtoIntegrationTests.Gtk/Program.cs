using System;
using Eto.Forms;

namespace EtoIntegrationTests.Gtk
{
  class Program
  {
    [STAThread]
    public static void Main(string[] args)
    {
      string? runner;
      var application = new Application(Eto.Platforms.Gtk);
      try
      {
        runner = MainForm.GetRunner();
        if (runner == null)
        {
          application.Invoke(() => MessageBox.Show("Test runner is not configured."));
          return;
        }
      }
      catch (Exception e)
      {
        application.Invoke(() => MessageBox.Show(e.Message));
        return;
      }
      application.Run(new MainForm(runner));
    }
  }
}