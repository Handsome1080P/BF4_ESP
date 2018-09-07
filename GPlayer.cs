using System;
using SharpDX;

namespace PZ_BF4
{
    class GPlayer
    {
        public string Name;
        public string VehicleName;
        public int Team;
        public Vector3 Origin;
        public Vector3 Velocity;
        public RDoll Bone;
        //public Vector3 BoneTarget;
        public int Pose;

        public Vector2 FoV;

        public Vector2 Sway;

        public int IsOccluded;

        public bool IsDriver;
        public bool InVehicle;

        public float Health;
        public float MaxHealth;


        //public float Accuracy;

        public string LastEnemyNameAimed = "";
        public DateTime LastTimeEnimyAimed = DateTime.Now;

        public Matrix ViewProj;

        //public float BreathControl;
        public bool NoBreathEnabled = false;

        public float Yaw;
        public float Distance;
        public float DistanceToCrosshair;

        // Vehicle
        public AxisAlignedBox VehicleAABB;
        public Matrix VehicleTranfsorm;
        public float VehicleHealth;
        public float VehicleMaxHealth;


        public bool IsValid()
        {
            return (Health > 0.1f && Health <= 100 && !Origin.IsZero);
        }

        public bool IsValidAimbotTarget(bool bTwoSecRule, string lastTargetName, DateTime lastTimeTargeted)
        {
            return IsValid() && (!bTwoSecRule || lastTargetName == Name || DateTime.Now.Subtract(lastTimeTargeted).Seconds >= 2);
        }

        public bool IsDead()
        {
            return !(Health > 0.1f);
        }

        public bool IsVisible()
        {
           return (IsOccluded == 0);
        }

        public bool IsSprinting()
        {
            return ((float)Math.Abs(Velocity.X + Velocity.Y + Velocity.Z) > 4.0f);
        }    


        public AxisAlignedBox GetAABB()
        {
            AxisAlignedBox aabb = new AxisAlignedBox();
            if (this.Pose == 0) // standing
            {
                aabb.Min = new Vector3(-0.350000f, 0.000000f, -0.350000f);
                aabb.Max = new Vector3(0.350000f, 1.700000f, 0.350000f);
            }
            if (this.Pose == 1) // crouching
            {
                aabb.Min = new Vector3(-0.350000f, 0.000000f, -0.350000f);
                aabb.Max = new Vector3(0.350000f, 1.150000f, 0.350000f);
            }
            if (this.Pose == 2) // prone
            {
                aabb.Min = new Vector3(-0.350000f, 0.000000f, -0.350000f);
                aabb.Max = new Vector3(0.350000f, 0.400000f, 0.350000f);
            }
            return aabb;
        }
    }
}
