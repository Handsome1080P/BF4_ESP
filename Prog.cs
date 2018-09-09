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

            while (true)
            {
                if (GetProcessesByName("bf4", out Process process))
                {
                    Thread.Sleep(5000); //start Overlay after 9 Seconds
                    Overlay overlay = new Overlay(process);
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
