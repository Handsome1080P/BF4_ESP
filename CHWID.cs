using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using UHWID;

namespace PZ_BF4
{

    class CHWID
    {
        WebClient client = new WebClient();
        public bool myid()
        {
            
            string myid = "http://textuploader.com/XXX/raw";
            string Sajad = "http://textuploader.com/XXX/raw";
            string Guillaume = "http://textuploader.com/XXX/raw";
            string Ali = "https://textuploader.com/XXX/raw";
            string dmyid = client.DownloadString(myid);
            string dSajad = client.DownloadString(Sajad);
            string dGuillaume = client.DownloadString(Guillaume);
            string dAli = client.DownloadString(Ali);
            if ((UHWIDEngine.CPUID == dmyid || UHWIDEngine.CPUID == dSajad || UHWIDEngine.CPUID == dGuillaume || UHWIDEngine.CPUID == dAli))
            {
                return true;
            }
            return false;
        }
      
    }
}
