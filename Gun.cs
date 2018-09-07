using System;
using SharpDX;

namespace PZ_BF4
{
    class Gun
    {
        public string Name = "";

        //public float Accuracy;

        public int Ammo;
        public int AmmoClip;

        public bool IsValid()
        {
            return Name.Length > 0;
        }

    }
}
