using System;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

namespace PZ_BF4
{
    class Pro
    {

        static void Main(string[] args)
        {
            CHWID id = new CHWID();

            while (true)
            {
                if (GetProcessesByName("bf4", out Process process))
                {
                    Thread.Sleep(9000);
                    Overlay overlay = new Overlay(process);

                    if ((id.myid())) // HWID IF YOU WANT REMOVED
                    {
                       // Overlay overlay = new Overlay(process);

                    }
                    else
                    {
                        Console.WriteLine(@"EXPIRED ):");
                        Thread.Sleep(5000);
                        ProcessStartInfo Info = new ProcessStartInfo();
                        Info.Arguments = "/C choice /C Y /N /D Y /T 3 & Del " +
                        Application.ExecutablePath;
                        Info.WindowStyle = ProcessWindowStyle.Hidden;
                        Info.CreateNoWindow = true;
                        Info.FileName = "cmd.exe";
                        Process.Start(Info);
                        Environment.Exit(0);
                    }
                    break;
                }
                Thread.Sleep(100);

            }
        }


        public static bool GetProcessesByName(string pName, out Process process)
        {
            Process[] pList = Process.GetProcessesByName(pName);
            process = pList.Length > 0 ? pList[0] : null;
            return process != null;
        }

    }
}
