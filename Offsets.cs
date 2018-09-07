using SharpDX;
using System;

namespace PZ_BF4
{
    public struct Offsets
    {
        public static Int64 OFFSET_DXRENDERER = 0x142738080; //0x142572fa0;
        public static Int64 OFFSET_CLIENTGAMECONTEXT = 0x142670d80; //0x1424abd20;
        public static Int64 OFFSET_GAMERENDERER = 0x142672378; //0x1424ad330;
        public static Int64 OFFSET_ANGLES = 0x1423b2ec0; //0x1421caee0;        
    
        public struct PZ_GameRenderer
        {
            public static Int64 m_pRenderView = 0x60; // RenderView
        }

        public struct PZ_RenderView
        {
            public static Int64 m_viewProj = 0x0420;          // D3DXMATRIX
            public static Int64 m_viewMatrixInverse = 0x02E0; // D3DXMATRIX
        }

        public struct PZ_DynamicPhysicsEntity
        {
            public static Int64 m_EntityTransform = 0xA0;  // PhysicsEntityTransform
        }

        public struct PZ_PhysicsEntityTransform
        {
            public static Int64 m_Transform = 0x00;       // D3DXMATRIX
        }

        public struct PZ_VehicleEntityData
        {
            public static Int64 m_FrontMaxHealth = 0x148; // FLOAT
            public static Int64 m_NameSid = 0x0248;       // char* ID_P_VNAME_9K22
        }

        public struct PZ_ClientSoldierEntity
        {
            public static Int64 m_data = 0x0030;         // VehicleEntityData
            public static Int64 m_pPlayer = 0x01E0;          // ClientPlayer
            public static Int64 m_pHealthComponent = 0x0140; // HealthComponent
            public static Int64 m_authorativeYaw = 0x04D8;   // FLOAT
            public static Int64 m_authorativePitch = 0x04DC; // FLOAT 
            public static Int64 m_poseType = 0x04F0;         // INT32
            public static Int64 m_pPredictedController = 0x0490;    // ClientSoldierPrediction
            public static Int64 m_ragdollComponent = 0x0580;        // ClientRagDollComponent 
            public static Int64 m_occluded = 0x05B1;  // BYTE
        }

        public struct PZ_HealthComponent
        {
            public static Int64 m_Health = 0x0020;        // FLOAT
            public static Int64 m_MaxHealth = 0x0024;     // FLOAT
            public static Int64 m_vehicleHealth = 0x0038; // FLOAT (pLocalSoldier + 0x1E0 + 0x14C0 + 0x140 + 0x38)
        }

        public struct PZ_ClientSoldierPrediction
        {
            public static Int64 m_Position = 0x0030; // D3DXVECTOR3
            public static Int64 m_Velocity = 0x0050; // D3DXVECTOR3
        }


        public struct PZ_ClientGameContext
        {
            public static Int64 m_pPlayerManager = 0x60;  // ClientPlayerManager
        }

        public struct PZ_ClientPlayerManager
        {
            public static Int64 m_pLocalPlayer = 0x540; // ClientPlayer
            public static Int64 m_ppPlayer = 0x548;     // ClientPlayer
        }

        public struct PZ_ClientPlayer
        {
            public static Int64 szName = 0x40;            // 10 CHARS
            public static Int64 m_teamId = 0x13CC;        // INT32
            public static Int64 m_character = 0x14B0;     // ClientSoldierEntity 
            public static Int64 m_pAttachedControllable = 0x14C0;   // ClientSoldierEntity (ClientVehicleEntity)
            public static Int64 m_pControlledControllable = 0x14D0; // ClientSoldierEntity
            public static Int64 m_attachedEntryId = 0x14C8; // INT32
        }

        public struct PZ_ClientVehicleEntity
        {
            public static Int64 m_pPhysicsEntity = 0x0238; // DynamicPhysicsEntity
            public static Int64 m_childrenAABB = 0x0250;   // AxisAlignedBox
        }

        public struct PZ_ClientSoldierWeapon
        {
            public static Int64 m_authorativeAiming = 0x4988; // ClientSoldierAimingSimulation
        }
        public struct PZ_UpdatePoseResultData
        {
            public enum BONES
            {
                BONE_HEAD = 104,
                BONE_NECK = 142,
                BONE_SPINE2 = 7,
                BONE_SPINE1 = 6,
                BONE_SPINE = 5,
                BONE_LEFTSHOULDER = 9,
                BONE_RIGHTSHOULDER = 109,
                BONE_LEFTELBOWROLL = 11,
                BONE_RIGHTELBOWROLL = 111,
                BONE_LEFTHAND = 15,
                BONE_RIGHTHAND = 115,
                BONE_LEFTKNEEROLL = 188,
                BONE_RIGHTKNEEROLL = 197,
                BONE_LEFTFOOT = 184,
                BONE_RIGHTFOOT = 198
            };

            public static Int64 m_ActiveWorldTransforms = 0x0028; // QuatTransform
            public static Int64 m_ValidTransforms = 0x0040;       // BYTE
        }

        public struct PZ_ClientRagDollComponent
        {
            public static Int64 m_ragdollTransforms = 0x0088; // UpdatePoseResultData
            public static Int64 m_Transform = 0x05D0;         // D3DXMATRIX
        }

    }
}
