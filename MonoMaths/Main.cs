using System;
using Gtk;

namespace MonoMaths
{
  class MainClass
  {
    public static string[] CommandLineArgs; // Added by Jon

    public static void Main (string[] args)
    {
      CommandLineArgs = args; // Added by Jon 
      Application.Init ();
      MainWindow win = new MainWindow ();
      win.Show ();
      Application.Run ();
    }
  }
}

