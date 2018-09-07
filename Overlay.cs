using System;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using Yato.DirectXOverlay;

using PimpMan;

using SharpDX;

namespace PZ_BF4
{
    public class Overlay
    {

        // Game Data
        private static GPlayer localPlayer = null;
        private static List<Gun> localWeapons = null;
        private static List<GPlayer> players = null;
        private static List<GPlayer> targetEnimies = null;

        // Screen Size
        private Rectangle rect;

        #region MAIN : Overlay

        private IntPtr handle;

        private bool OPTIONS_AA = false;
        private bool OPTIONS_VSync = true; //lock fps cheat 60 fps
        private bool OPTIONS_ShowFPS = true;

        private OverlayWindow overlay;
        private Direct2DRenderer d2d;
        private Direct2DBrush clearBrush;

        // Process
        private Process process = null;
        private Thread updateStream = null;
        private Thread gameCheckThread = null;

        private bool IsGameRunning = true;

        // Init
        public Overlay(Process process)
        {

            this.process = process;
          
            // check the game window exists then create the overlay
            while (true)
            {
                handle = NativeMethods.FindWindow(null, "Battlefield 4");

                if (handle != IntPtr.Zero)
                {
                    break;
                }
            }

            gameCheckThread = new Thread(new ParameterizedThreadStart(GameCheck));
            gameCheckThread.Start();
            if (process.MainWindowHandle == IntPtr.Zero)
            {
                Thread.Sleep(1000);
            }

            RPM.OpenProcess(process.Id);
           
            // setup the overlay
            var rendererOptions = new Direct2DRendererOptions()
            {
                AntiAliasing = OPTIONS_AA,
                Hwnd = IntPtr.Zero,
                MeasureFps = OPTIONS_ShowFPS,
                VSync = OPTIONS_VSync
            };

            OverlayManager manager = new OverlayManager(handle, rendererOptions);

            overlay = manager.Window;
            d2d = manager.Graphics;
            clearBrush = d2d.CreateBrush(0xF5, 0xF5, 0xF5, 0);  // our transparent colour

           

            // Init player array
            localPlayer = new GPlayer();
            //localPlayer.CurrentWeapon = new Weapon();
            localWeapons = new List<Gun>();
            players = new List<GPlayer>();
            targetEnimies = new List<GPlayer>();

            // Init update thread
            updateStream = new Thread(new ParameterizedThreadStart(Update));
            updateStream.Start();


            ScreenCapture sc = new ScreenCapture();
            sc.CaptureWindowToFile(process.MainWindowHandle, @"C:\PZ_BF4_FAIRFIGHT_GAME_SS.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);


    

            // Init Key Listener
            KeyAssign();
        }

        // Update Thread
        private void Update(object sender)
        {
            Pimp pimp = new Pimp(Pimp.BlockMethod.Zero, true, true, false);
            if (pimp.Inject("bf4"))
            {
                while (IsGameRunning)
                {

                    bool check = pimp.IsScreenShot();
                    if (!check)
                    {
                        rect.Width = overlay.Width;
                        rect.Height = overlay.Height;
                        d2d.BeginScene();
                        d2d.ClearScene(clearBrush);
                        OverlayControl();
                        d2d.EndScene();
                    }
                }
                RPM.CloseProcess();
                Environment.Exit(0);
            }
        }
        private void OverlayControl()
        {

            try
            {


                // and our font
                var font = d2d.CreateFont("Consolas", 11);



                // Check Window State
                if (IsGameRunning)
                {

                    #region Scan Memory & Draw Players
                    MainScan();
                    #endregion

                    #region Drawing Menu
                    if (bMenuControl)
                        DrawMenu(10, 10);
                    #endregion

                    if (OPTIONS_ShowFPS)
                    {
                        d2d.DrawTextWithBackground($"Cheat FPS: {d2d.FPS}", 20, 20, font, d2d.CreateBrush(255, 0, 0, 255), d2d.CreateBrush(0, 0, 0, 255));
                    }
                }

                CalculateFrameRate();
            }
            catch (Exception ex)
            {
                WriteOnLogFile(DateTime.Now.ToString() + " - OVERLAY ERROR : " + ex);
            }

        }

        #endregion

        #region Scan Game Memory Stuff

        private bool bBoneOk = false;

        private Int64 GetLocalSoldier()
        {
            Int64 pGContext = RPM.ReadInt64(Offsets.OFFSET_CLIENTGAMECONTEXT);
            if (!RPM.IsValid(pGContext))
                return 0x000F000000000000;

            Int64 pPlayerManager = RPM.ReadInt64(pGContext + Offsets.PZ_ClientGameContext.m_pPlayerManager);
            if (!RPM.IsValid(pPlayerManager))
                return 0x000F000000000000;

            Int64 plocalPlayer = RPM.ReadInt64(pPlayerManager + Offsets.PZ_ClientPlayerManager.m_pLocalPlayer);
            if (!RPM.IsValid(plocalPlayer))
                return 0x000F000000000000;

            Int64 pLocalSoldier = GetClientSoldierEntity(plocalPlayer, localPlayer);
            if (!RPM.IsValid(pLocalSoldier))
                return 0x000F000000000000;
            else
                return pLocalSoldier;
        }


        private Int64 GetClientSoldierEntity(Int64 pClientPlayer, GPlayer player)
        {
            player.InVehicle = false;
            player.IsDriver = false;


            Int64 pAttached = RPM.ReadInt64(pClientPlayer + Offsets.PZ_ClientPlayer.m_pAttachedControllable);
            if (RPM.IsValid(pAttached))
            {
                Int64 m_ClientSoldier = RPM.ReadInt64(RPM.ReadInt64(pClientPlayer + Offsets.PZ_ClientPlayer.m_character)) - sizeof(Int64);
                if (RPM.IsValid(m_ClientSoldier))
                {
                    player.InVehicle = true;

                    Int64 pVehicleEntity = RPM.ReadInt64(pClientPlayer + Offsets.PZ_ClientPlayer.m_pAttachedControllable);
                    if (RPM.IsValid(pVehicleEntity))
                    {
                        // Driver
                        if (RPM.ReadInt32(pClientPlayer + Offsets.PZ_ClientPlayer.m_attachedEntryId) == 0)
                        {
                            // Vehicle AABB
                            if (ESP_Vehicle)
                            {
                                Int64 pDynamicPhysicsEntity = RPM.ReadInt64(pVehicleEntity + Offsets.PZ_ClientVehicleEntity.m_pPhysicsEntity);
                                if (RPM.IsValid(pDynamicPhysicsEntity))
                                {
                                    Int64 pPhysicsEntity = RPM.ReadInt64(pDynamicPhysicsEntity + Offsets.PZ_DynamicPhysicsEntity.m_EntityTransform);
                                    player.VehicleTranfsorm = RPM.ReadMatrix(pPhysicsEntity + Offsets.PZ_PhysicsEntityTransform.m_Transform);
                                    player.VehicleAABB = RPM.ReadAABB(pVehicleEntity + Offsets.PZ_ClientVehicleEntity.m_childrenAABB);
                                }
                            }
                            Int64 _EntityData = RPM.ReadInt64(pVehicleEntity + Offsets.PZ_ClientSoldierEntity.m_data);
                            if (RPM.IsValid(_EntityData))
                            {
                                Int64 _NameSid = RPM.ReadInt64(_EntityData + Offsets.PZ_VehicleEntityData.m_NameSid);

                                string strName = RPM.ReadName(_NameSid, 20);
                                if (strName.Length > 11)
                                {
                                    Int64 pAttachedClient = RPM.ReadInt64(m_ClientSoldier + Offsets.PZ_ClientSoldierEntity.m_pPlayer);
                                    // AttachedControllable Max Health
                                    Int64 p = RPM.ReadInt64(pAttachedClient + Offsets.PZ_ClientPlayer.m_pAttachedControllable);
                                    Int64 p2 = RPM.ReadInt64(p + Offsets.PZ_ClientSoldierEntity.m_pHealthComponent);
                                    player.VehicleHealth = RPM.ReadFloat(p2 + Offsets.PZ_HealthComponent.m_vehicleHealth);

                                    // AttachedControllable Health
                                    player.VehicleMaxHealth = RPM.ReadFloat(_EntityData + Offsets.PZ_VehicleEntityData.m_FrontMaxHealth);

                                    // AttachedControllable Name
                                    player.VehicleName = strName.Remove(0, 11);
                                    player.IsDriver = true;
                                }
                            }
                        }
                    }
                }
                return m_ClientSoldier;
            }
            return RPM.ReadInt64(pClientPlayer + Offsets.PZ_ClientPlayer.m_pControlledControllable);
        }

        private bool GetBoneById(Int64 pEnemySoldier, int Id, out Vector3 _World)
        {
            _World = new Vector3();

            Int64 pRagdollComp = RPM.ReadInt64(pEnemySoldier + Offsets.PZ_ClientSoldierEntity.m_ragdollComponent);
            if (!RPM.IsValid(pRagdollComp))
                return false;

            byte m_ValidTransforms = RPM.ReadByte(pRagdollComp + (Offsets.PZ_ClientRagDollComponent.m_ragdollTransforms + Offsets.PZ_UpdatePoseResultData.m_ValidTransforms));
            if (m_ValidTransforms != 1)
                return false;

            Int64 pQuatTransform = RPM.ReadInt64(pRagdollComp + (Offsets.PZ_ClientRagDollComponent.m_ragdollTransforms + Offsets.PZ_UpdatePoseResultData.m_ActiveWorldTransforms));
            if (!RPM.IsValid(pQuatTransform))
                return false;

            _World = RPM.ReadVector3(pQuatTransform + Id * 0x20);
            return true;
        }

        private void MainScan()
        {
            var font = d2d.CreateFont("Consolas", 11);

            players.Clear();
            targetEnimies.Clear();

            // Read Local
            #region Get Local Player

            // Render View
            Int64 pGameRenderer = RPM.ReadInt64(Offsets.OFFSET_GAMERENDERER);
            Int64 pRenderView = RPM.ReadInt64(pGameRenderer + Offsets.PZ_GameRenderer.m_pRenderView);

            // Read Screen Matrix
            localPlayer.ViewProj = RPM.ReadMatrix(pRenderView + Offsets.PZ_RenderView.m_viewProj);

            Int64 pGContext = RPM.ReadInt64(Offsets.OFFSET_CLIENTGAMECONTEXT);
            if (!RPM.IsValid(pGContext))
                return;

            Int64 pPlayerManager = RPM.ReadInt64(pGContext + Offsets.PZ_ClientGameContext.m_pPlayerManager);
            if (!RPM.IsValid(pPlayerManager))
                return;

            Int64 plocalPlayer = RPM.ReadInt64(pPlayerManager + Offsets.PZ_ClientPlayerManager.m_pLocalPlayer);
            if (!RPM.IsValid(plocalPlayer))
                return;

            localPlayer.Team = RPM.ReadInt32(plocalPlayer + Offsets.PZ_ClientPlayer.m_teamId);

            Int64 pLocalSoldier = GetClientSoldierEntity(plocalPlayer, localPlayer);
            if (!RPM.IsValid(pLocalSoldier))
                return;

            Int64 pHealthComponent = RPM.ReadInt64(pLocalSoldier + Offsets.PZ_ClientSoldierEntity.m_pHealthComponent);
            if (!RPM.IsValid(pHealthComponent))
                return;

            Int64 pPredictedController = RPM.ReadInt64(pLocalSoldier + Offsets.PZ_ClientSoldierEntity.m_pPredictedController);
            if (!RPM.IsValid(pPredictedController))
                return;

            // Health
            localPlayer.Health = RPM.ReadFloat(pHealthComponent + Offsets.PZ_HealthComponent.m_Health);
            localPlayer.MaxHealth = RPM.ReadFloat(pHealthComponent + Offsets.PZ_HealthComponent.m_MaxHealth);

            if (localPlayer.IsDead())
                return;

            // Origin
            localPlayer.Origin = RPM.ReadVector3(pPredictedController + Offsets.PZ_ClientSoldierPrediction.m_Position);
            localPlayer.Velocity = RPM.ReadVector3(pPredictedController + Offsets.PZ_ClientSoldierPrediction.m_Velocity);

            // Other
            localPlayer.Pose = RPM.ReadInt32(pLocalSoldier + Offsets.PZ_ClientSoldierEntity.m_poseType);
            localPlayer.Yaw = RPM.ReadFloat(pLocalSoldier + Offsets.PZ_ClientSoldierEntity.m_authorativeYaw);

            localPlayer.IsOccluded = RPM.ReadByte(pLocalSoldier + Offsets.PZ_ClientSoldierEntity.m_occluded);

            #endregion

            // Pointer to Players Array
            Int64 m_ppPlayer = RPM.ReadInt64(pPlayerManager + Offsets.PZ_ClientPlayerManager.m_ppPlayer);
            if (!RPM.IsValid(m_ppPlayer))
                return;


            #region Get Other Players by Id
            for (uint i = 0; i < 64; i++)
            {
                // Create new Player
                GPlayer player = new GPlayer();

                // Pointer to ClientPlayer class (Player Array + (Id * Size of Pointer))
                Int64 pEnemyPlayer = RPM.ReadInt64(m_ppPlayer + (i * sizeof(Int64)));
                if (!RPM.IsValid(pEnemyPlayer))
                    continue;

                player.Name = RPM.ReadString(pEnemyPlayer + Offsets.PZ_ClientPlayer.szName, 10);

                Int64 pEnemySoldier = GetClientSoldierEntity(pEnemyPlayer, player);
                if (!RPM.IsValid(pEnemySoldier))
                    continue;

                Int64 pEnemyHealthComponent = RPM.ReadInt64(pEnemySoldier + Offsets.PZ_ClientSoldierEntity.m_pHealthComponent);
                if (!RPM.IsValid(pEnemyHealthComponent))
                    continue;

                Int64 pEnemyPredictedController = RPM.ReadInt64(pEnemySoldier + Offsets.PZ_ClientSoldierEntity.m_pPredictedController);
                if (!RPM.IsValid(pEnemyPredictedController))
                    continue;

                // Health
                player.Health = RPM.ReadFloat(pEnemyHealthComponent + Offsets.PZ_HealthComponent.m_Health);
                player.MaxHealth = RPM.ReadFloat(pEnemyHealthComponent + Offsets.PZ_HealthComponent.m_MaxHealth);

                if (player.Health <= 0.1f) // DEAD
                    continue;

                // Origin (Position in Game X, Y, Z)
                player.Origin = RPM.ReadVector3(pEnemyPredictedController + Offsets.PZ_ClientSoldierPrediction.m_Position);
                player.Velocity = RPM.ReadVector3(pEnemyPredictedController + Offsets.PZ_ClientSoldierPrediction.m_Velocity);

                // Other
                player.Team = RPM.ReadInt32(pEnemyPlayer + Offsets.PZ_ClientPlayer.m_teamId);
                player.Pose = RPM.ReadInt32(pEnemySoldier + Offsets.PZ_ClientSoldierEntity.m_poseType);
                player.Yaw = RPM.ReadFloat(pEnemySoldier + Offsets.PZ_ClientSoldierEntity.m_authorativeYaw);
                player.IsOccluded = RPM.ReadByte(pEnemySoldier + Offsets.PZ_ClientSoldierEntity.m_occluded);

                // Distance to You
                player.Distance = Vector3.Distance(localPlayer.Origin, player.Origin);

                players.Add(player);

                if (player.IsValid())
                {
                    // Player Bone
                    bBoneOk = (GetBoneById(pEnemySoldier, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_HEAD, out player.Bone.BONE_HEAD)
                            && GetBoneById(pEnemySoldier, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_LEFTELBOWROLL, out player.Bone.BONE_LEFTELBOWROLL)
                            && GetBoneById(pEnemySoldier, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_LEFTFOOT, out player.Bone.BONE_LEFTFOOT)
                            && GetBoneById(pEnemySoldier, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_LEFTHAND, out player.Bone.BONE_LEFTHAND)
                            && GetBoneById(pEnemySoldier, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_LEFTKNEEROLL, out player.Bone.BONE_LEFTKNEEROLL)
                            && GetBoneById(pEnemySoldier, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_LEFTSHOULDER, out player.Bone.BONE_LEFTSHOULDER)
                            && GetBoneById(pEnemySoldier, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_NECK, out player.Bone.BONE_NECK)
                            && GetBoneById(pEnemySoldier, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_RIGHTELBOWROLL, out player.Bone.BONE_RIGHTELBOWROLL)
                            && GetBoneById(pEnemySoldier, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_RIGHTFOOT, out player.Bone.BONE_RIGHTFOOT)
                            && GetBoneById(pEnemySoldier, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_RIGHTHAND, out player.Bone.BONE_RIGHTHAND)
                            && GetBoneById(pEnemySoldier, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_RIGHTKNEEROLL, out player.Bone.BONE_RIGHTKNEEROLL)
                            && GetBoneById(pEnemySoldier, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_RIGHTSHOULDER, out player.Bone.BONE_RIGHTSHOULDER)
                            && GetBoneById(pEnemySoldier, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_SPINE, out player.Bone.BONE_SPINE)
                            && GetBoneById(pEnemySoldier, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_SPINE1, out player.Bone.BONE_SPINE1)
                            && GetBoneById(pEnemySoldier, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_SPINE2, out player.Bone.BONE_SPINE2));



                    #region Drawing ESP on Overlay

                    // Desconsidera Aliados
                    if (!bEspAllies && (player.Team == localPlayer.Team))
                        continue;

                    // Desconsidera os "Não Visíveis"
                    if (bEspVisiblesOnly && (!player.IsVisible() || player.Distance > 75) && !player.InVehicle)
                        continue;

                    #region ESP Bone
                    if (bBoneOk && ESP_Bone)
                        DrawBone(player);
                    #endregion

                    Vector3 w2sFoot, w2sHead;
                    if (WorldToScreen(player.Origin, out w2sFoot) && WorldToScreen(player.Origin, player.Pose, out w2sHead))
                    {
                        float H = w2sFoot.Y - w2sHead.Y;
                        float W = H / 2;
                        float X = w2sHead.X - W / 2;
                        int iAux;
                        #region ESP Color

                        var color = (player.Team == localPlayer.Team) ? d2d.CreateBrush(0, 0, 255, 255) : player.IsVisible() ? d2d.CreateBrush(0, 255, 0, 255) : d2d.CreateBrush(255, 0, 0, 255);
                        #endregion

                        #region ESP Box
                        // ESP Box
                        if (ESP_Box && !bEspVisiblesOnly)
                            if (bEsp3D)
                                DrawAABB(player.GetAABB(), player.Origin, player.Yaw, color); // 3D Box      
                            else
                                d2d.DrawRectangle((int)X, (int)w2sHead.Y, (int)W, (int)H, 2, color); // 2D Box
                        #endregion

                        #region ESP Vehicle
                        if (ESP_Vehicle)
                            DrawAABB(player.VehicleAABB, player.VehicleTranfsorm, player.Team == localPlayer.Team ? d2d.CreateBrush(64, 154, 200, 255) : d2d.CreateBrush(255, 129, 72, 255));
                        #endregion

                        #region ESP Name
                        if (ESP_Name && !bEspVisiblesOnly)
                            d2d.DrawTextWithBackground(player.Name, (int)X, (int)w2sFoot.Y, font, d2d.CreateBrush(240, 240, 240, 255), d2d.CreateBrush(0, 0, 0, 255));
                        #endregion

                        #region ESP Distance
                        if (ESP_Distance && !bEspVisiblesOnly)
                        {
                            iAux = (int)w2sFoot.Y;
                            if (ESP_Name)
                                iAux = iAux + 13;
                            d2d.DrawTextWithBackground((int)player.Distance + "m", (int)X, iAux, font, d2d.CreateBrush(240, 240, 240, 255), d2d.CreateBrush(0, 0, 0, 255));
                        }
                        #endregion

                        #region ESP Health
                        if (ESP_Health && !bEspVisiblesOnly)
                        {
                            DrawHealth((int)X, (int)w2sHead.Y - 6, (int)W, 3, (int)player.Health, (int)player.MaxHealth);
                            if (player.InVehicle && player.IsDriver)
                                DrawHealth((int)X, (int)w2sHead.Y - 10, (int)W, 3, (int)player.VehicleHealth, (int)player.VehicleMaxHealth);
                        }
                        #endregion



                    }
                    #endregion

                }
            }
            #endregion
        }

        #endregion

        #region Keys Stuff
        public void KeyAssign()
        {
            KeysMgr keyMgr = new KeysMgr();
            keyMgr.AddKey(Keys.Home);     // MENU
            keyMgr.AddKey(Keys.Up);       // UP
            keyMgr.AddKey(Keys.Down);     // DOWN
            keyMgr.AddKey(Keys.Right);    // CHANGE OPTION
            keyMgr.AddKey(Keys.Delete);   // QUIT

            keyMgr.AddKey(Keys.F6);       // Clear Weapon Data Bank (Collection)

            keyMgr.AddKey(Keys.F9);       // ATALHO 1
            keyMgr.AddKey(Keys.F10);      // ATALHO 2
            keyMgr.AddKey(Keys.F11);      // ATALHO 3
            keyMgr.AddKey(Keys.F12);      // ATALHO 4

            keyMgr.AddKey(Keys.CapsLock);  // Aimbot Activate 1
            keyMgr.AddKey(Keys.RButton);   // Aimbot Activate 2

            keyMgr.AddKey(Keys.PageUp);    // Optimized Settings
            keyMgr.AddKey(Keys.PageDown);  // Default Settings

            keyMgr.KeyDownEvent += new KeysMgr.KeyHandler(KeyDownEvent);
        }

        public static bool IsKeyDown(int key)
        {
            return Convert.ToBoolean(NativeMethods.GetKeyState(key) & NativeMethods.KEY_PRESSED);
        }

        private void KeyDownEvent(int Id, string Name)
        {
            switch ((Keys)Id)
            {
                case Keys.Home:
                    this.bMenuControl = !this.bMenuControl;
                    break;
                case Keys.Delete:
                    Quit();
                    break;
                case Keys.Right:
                    SelectMenuItem();
                    break;
                case Keys.Up:
                    CycleMenuUp();
                    break;
                case Keys.Down:
                    CycleMenuDown();
                    break;
            }

        }
        #endregion

        #region Menu Stuff

        private bool bMenuControl = true;

        private bool bEspVisiblesOnly = false;
        private bool bEsp3D = false;
        private bool bEspAllies = false;


        private enum mnIndex
        {

            MN_ESP_NAME = 0,
            MN_ESP_BOX = 1,
            MN_ESP_3D = 2,
            MN_ESP_HEALTH = 3,
            MN_ESP_BON = 4,
            MN_ESP_DISTANCE = 5,
            MN_ESP_VEHICLE = 6,
            MN_ESP_VISIBLES_ONLY = 7,
            MN_ESP_ALLIES = 8,

        };
        private mnIndex currMnIndex = mnIndex.MN_ESP_NAME;
        private int LastMenuIndex = Enum.GetNames(typeof(mnIndex)).Length - 1;

        private enum mnEspMode
        {
            NONE,
            MINIMAL,
            PARTIAL,
            FULL
        };
        private mnEspMode currMnEspMode = mnEspMode.FULL;

 

        private void CycleMenuDown()
        {
            if (bMenuControl)
                currMnIndex = (mnIndex)((int)currMnIndex >= LastMenuIndex ? 0 : (int)currMnIndex + 1);
        }

        private void CycleMenuUp()
        {
            if (bMenuControl)
                currMnIndex = (mnIndex)((int)currMnIndex <= 0 ? LastMenuIndex : (int)currMnIndex - 1);
        }

        private void SelectMenuItem()
        {
            switch (currMnIndex)
            {

                case mnIndex.MN_ESP_NAME:
                    ESP_Name = !ESP_Name;
                    break;
                case mnIndex.MN_ESP_BOX:
                    ESP_Box = !ESP_Box;
                    break;
                case mnIndex.MN_ESP_3D:
                    bEsp3D = !bEsp3D;
                    break;
                case mnIndex.MN_ESP_HEALTH:
                    ESP_Health = !ESP_Health;
                    break;
                case mnIndex.MN_ESP_BON:
                    ESP_Bone = !ESP_Bone;
                    break;
                case mnIndex.MN_ESP_DISTANCE:
                    ESP_Distance = !ESP_Distance;
                    break;
                case mnIndex.MN_ESP_VEHICLE:
                    ESP_Vehicle = !ESP_Vehicle;
                    break;

                case mnIndex.MN_ESP_VISIBLES_ONLY:
                    bEspVisiblesOnly = !bEspVisiblesOnly;
                    break;
                case mnIndex.MN_ESP_ALLIES:
                    bEspAllies = !bEspAllies;
                    break;
                

            }
        }

        private string GetMenuString(mnIndex idx)
        {
            string result = "";

            switch (idx)
            {

                case mnIndex.MN_ESP_NAME:
                    result = "ESP NAME : " + (((currMnEspMode != mnEspMode.NONE) && ESP_Name) ? "[ ON ]" : "[ OFF ]");
                    break;
                case mnIndex.MN_ESP_BOX:
                    result = "ESP BOX : " + (((currMnEspMode != mnEspMode.NONE) && ESP_Box) ? "[ ON ]" : "[ OFF ]");
                    break;
                case mnIndex.MN_ESP_3D:
                    result = "ESP 2D/3D : " + ((currMnEspMode == mnEspMode.NONE) ? "[ OFF ]" : (bEsp3D) ? "[ 3D ]" : "[ 2D ]");
                    break;
                case mnIndex.MN_ESP_HEALTH:
                    result = "ESP HEALTH : " + (((currMnEspMode != mnEspMode.NONE) && ESP_Health) ? "[ ON ]" : "[ OFF ]");
                    break;
                case mnIndex.MN_ESP_BON:
                    result = "ESP SKELETON : " + (((currMnEspMode != mnEspMode.NONE) && ESP_Bone) ? "[ ON ]" : "[ OFF ]");
                    break;
                case mnIndex.MN_ESP_DISTANCE:
                    result = "ESP DISTANCE : " + (((currMnEspMode != mnEspMode.NONE) && ESP_Distance) ? "[ ON ]" : "[ OFF ]");
                    break;
                case mnIndex.MN_ESP_VEHICLE:
                    result = "ESP VEHICLE : " + (((currMnEspMode != mnEspMode.NONE) && ESP_Vehicle) ? "[ ON ]" : "[ OFF ]");
                    break;
                case mnIndex.MN_ESP_VISIBLES_ONLY:
                    result = "ESP VISIBLES ONLY : " + (((currMnEspMode != mnEspMode.NONE) && bEspVisiblesOnly) ? "[ ON ]" : "[ OFF ]");
                    break;
                case mnIndex.MN_ESP_ALLIES:
                    result = "ESP FRIENDS : " + (((currMnEspMode != mnEspMode.NONE) && bEspAllies) ? "[ ON ]" : "[ OFF ]");
                    break;
            

            }

            return result;
        }

        #endregion

        #region World to Screen
        private bool WorldToScreen(Vector3 _Enemy, int _Pose, out Vector3 _Screen)
        {
            _Screen = new Vector3(0, 0, 0);
            float HeadHeight = _Enemy.Y;

            #region HeadHeight
            if (_Pose == 0)
            {
                HeadHeight += 1.7f;
            }
            if (_Pose == 1)
            {
                HeadHeight += 1.15f;
            }
            if (_Pose == 2)
            {
                HeadHeight += 0.4f;
            }
            #endregion

            float ScreenW = (localPlayer.ViewProj.M14 * _Enemy.X) + (localPlayer.ViewProj.M24 * HeadHeight) + (localPlayer.ViewProj.M34 * _Enemy.Z + localPlayer.ViewProj.M44);

            if (ScreenW < 0.0001f)
                return false;

            float ScreenX = (localPlayer.ViewProj.M11 * _Enemy.X) + (localPlayer.ViewProj.M21 * HeadHeight) + (localPlayer.ViewProj.M31 * _Enemy.Z + localPlayer.ViewProj.M41);
            float ScreenY = (localPlayer.ViewProj.M12 * _Enemy.X) + (localPlayer.ViewProj.M22 * HeadHeight) + (localPlayer.ViewProj.M32 * _Enemy.Z + localPlayer.ViewProj.M42);

            _Screen.X = (rect.Width / 2) + (rect.Width / 2) * ScreenX / ScreenW;
            _Screen.Y = (rect.Height / 2) - (rect.Height / 2) * ScreenY / ScreenW;
            _Screen.Z = ScreenW;
            return true;
        }

        private bool WorldToScreen(Vector3 _Enemy, out Vector3 _Screen)
        {
            _Screen = new Vector3(0, 0, 0);
            float ScreenW = (localPlayer.ViewProj.M14 * _Enemy.X) + (localPlayer.ViewProj.M24 * _Enemy.Y) + (localPlayer.ViewProj.M34 * _Enemy.Z + localPlayer.ViewProj.M44);

            if (ScreenW < 0.0001f)
                return false;

            float ScreenX = (localPlayer.ViewProj.M11 * _Enemy.X) + (localPlayer.ViewProj.M21 * _Enemy.Y) + (localPlayer.ViewProj.M31 * _Enemy.Z + localPlayer.ViewProj.M41);
            float ScreenY = (localPlayer.ViewProj.M12 * _Enemy.X) + (localPlayer.ViewProj.M22 * _Enemy.Y) + (localPlayer.ViewProj.M32 * _Enemy.Z + localPlayer.ViewProj.M42);

            _Screen.X = (rect.Width / 2) + (rect.Width / 2) * ScreenX / ScreenW;
            _Screen.Y = (rect.Height / 2) - (rect.Height / 2) * ScreenY / ScreenW;
            _Screen.Z = ScreenW;
            return true;
        }
        #endregion

        #region Draw Stuff

        #region Draw - Variables

        // Color
        private Color enemyColor = new Color(255, 0, 0, 255),
            enemyColorVisible = new Color(0, 255, 0, 255),
            enemyColorVehicle = new Color(255, 129, 72, 255),
            enemySkeletonColor = new Color(245, 114, 0, 255),
            friendlyColor = new Color(0, 0, 255, 255),
            friendlyColorVehicle = new Color(64, 154, 200, 255),
            friendSkeletonColor = new Color(46, 228, 213, 255);

        // ESP OPTIONS
        private bool ESP_Box = true,
            ESP_Bone = false,
            ESP_Name = false,
            ESP_Health = false,
            ESP_Distance = true,
            ESP_Vehicle = false;



        #endregion

        #region Draw - Info

        public Vector3 Multiply(Vector3 vector, Matrix mat)
        {
            return new Vector3(mat.M11 * vector.X + mat.M21 * vector.Y + mat.M31 * vector.Z,
                               mat.M12 * vector.X + mat.M22 * vector.Y + mat.M32 * vector.Z,
                               mat.M13 * vector.X + mat.M23 * vector.Y + mat.M33 * vector.Z);
        }
        private void DrawAABB(AxisAlignedBox aabb, Matrix tranform, Direct2DBrush color)
        {
            Vector3 m_Position = new Vector3(tranform.M41, tranform.M42, tranform.M43);
            Vector3 fld = Multiply(new Vector3(aabb.Min.X, aabb.Min.Y, aabb.Min.Z), tranform) + m_Position;
            Vector3 brt = Multiply(new Vector3(aabb.Max.X, aabb.Max.Y, aabb.Max.Z), tranform) + m_Position;
            Vector3 bld = Multiply(new Vector3(aabb.Min.X, aabb.Min.Y, aabb.Max.Z), tranform) + m_Position;
            Vector3 frt = Multiply(new Vector3(aabb.Max.X, aabb.Max.Y, aabb.Min.Z), tranform) + m_Position;
            Vector3 frd = Multiply(new Vector3(aabb.Max.X, aabb.Min.Y, aabb.Min.Z), tranform) + m_Position;
            Vector3 brb = Multiply(new Vector3(aabb.Max.X, aabb.Min.Y, aabb.Max.Z), tranform) + m_Position;
            Vector3 blt = Multiply(new Vector3(aabb.Min.X, aabb.Max.Y, aabb.Max.Z), tranform) + m_Position;
            Vector3 flt = Multiply(new Vector3(aabb.Min.X, aabb.Max.Y, aabb.Min.Z), tranform) + m_Position;

            #region WorldToScreen
            if (!WorldToScreen(fld, out fld) || !WorldToScreen(brt, out brt)
                || !WorldToScreen(bld, out bld) || !WorldToScreen(frt, out frt)
                || !WorldToScreen(frd, out frd) || !WorldToScreen(brb, out brb)
                || !WorldToScreen(blt, out blt) || !WorldToScreen(flt, out flt))
                return;
            #endregion

            #region DrawLines
            d2d.DrawLine(fld.X, fld.Y, flt.X, flt.Y,1, color);
            d2d.DrawLine(flt.X, flt.Y, frt.X, frt.Y,1, color);
            d2d.DrawLine(frt.X, frt.Y, frd.X, frd.Y,1, color);
            d2d.DrawLine(frd.X, frd.Y, fld.X, fld.Y,1, color);
            d2d.DrawLine(bld.X, bld.Y, blt.X, blt.Y,1, color);
            d2d.DrawLine(blt.X, blt.Y, brt.X, brt.Y,1, color);
            d2d.DrawLine(brt.X, brt.Y, brb.X, brb.Y,1, color);
            d2d.DrawLine(brb.X, brb.Y, bld.X, bld.Y,1, color);
            d2d.DrawLine(fld.X, fld.Y, bld.X, bld.Y,1, color);
            d2d.DrawLine(frd.X, frd.Y, brb.X, brb.Y,1, color);
            d2d.DrawLine(flt.X, flt.Y, blt.X, blt.Y,1, color);
            d2d.DrawLine(frt.X, frt.Y, brt.X, brt.Y,1, color);
            #endregion
        }
        private void DrawAABB(AxisAlignedBox aabb, Vector3 m_Position, float Yaw, Direct2DBrush color)
        {
            float cosY = (float)Math.Cos(Yaw);
            float sinY = (float)Math.Sin(Yaw);

            Vector3 fld = new Vector3(aabb.Min.Z * cosY - aabb.Min.X * sinY, aabb.Min.Y, aabb.Min.X * cosY + aabb.Min.Z * sinY) + m_Position; // 0
            Vector3 brt = new Vector3(aabb.Min.Z * cosY - aabb.Max.X * sinY, aabb.Min.Y, aabb.Max.X * cosY + aabb.Min.Z * sinY) + m_Position; // 1
            Vector3 bld = new Vector3(aabb.Max.Z * cosY - aabb.Max.X * sinY, aabb.Min.Y, aabb.Max.X * cosY + aabb.Max.Z * sinY) + m_Position; // 2
            Vector3 frt = new Vector3(aabb.Max.Z * cosY - aabb.Min.X * sinY, aabb.Min.Y, aabb.Min.X * cosY + aabb.Max.Z * sinY) + m_Position; // 3
            Vector3 frd = new Vector3(aabb.Max.Z * cosY - aabb.Min.X * sinY, aabb.Max.Y, aabb.Min.X * cosY + aabb.Max.Z * sinY) + m_Position; // 4
            Vector3 brb = new Vector3(aabb.Min.Z * cosY - aabb.Min.X * sinY, aabb.Max.Y, aabb.Min.X * cosY + aabb.Min.Z * sinY) + m_Position; // 5
            Vector3 blt = new Vector3(aabb.Min.Z * cosY - aabb.Max.X * sinY, aabb.Max.Y, aabb.Max.X * cosY + aabb.Min.Z * sinY) + m_Position; // 6
            Vector3 flt = new Vector3(aabb.Max.Z * cosY - aabb.Max.X * sinY, aabb.Max.Y, aabb.Max.X * cosY + aabb.Max.Z * sinY) + m_Position; // 7

            #region WorldToScreen
            if (!WorldToScreen(fld, out fld) || !WorldToScreen(brt, out brt)
                || !WorldToScreen(bld, out bld) || !WorldToScreen(frt, out frt)
                || !WorldToScreen(frd, out frd) || !WorldToScreen(brb, out brb)
                || !WorldToScreen(blt, out blt) || !WorldToScreen(flt, out flt))
                return;
            #endregion

            #region DrawLines
            d2d.DrawLine(fld.X, fld.Y, brt.X, brt.Y,1, color);
            d2d.DrawLine(brb.X, brb.Y, blt.X, blt.Y,1, color);
            d2d.DrawLine(fld.X, fld.Y, brb.X, brb.Y,1, color);
            d2d.DrawLine(brt.X, brt.Y, blt.X, blt.Y,1, color);

            d2d.DrawLine(frt.X, frt.Y, bld.X, bld.Y,1, color);
            d2d.DrawLine(frd.X, frd.Y, flt.X, flt.Y,1, color);
            d2d.DrawLine(frt.X, frt.Y, frd.X, frd.Y,1, color);
            d2d.DrawLine(bld.X, bld.Y, flt.X, flt.Y,1, color);

            d2d.DrawLine(frt.X, frt.Y, fld.X, fld.Y,1, color);
            d2d.DrawLine(frd.X, frd.Y, brb.X, brb.Y,1, color);
            d2d.DrawLine(brt.X, brt.Y, bld.X, bld.Y,1, color);
            d2d.DrawLine(blt.X, blt.Y, flt.X, flt.Y,1, color);
            #endregion
        }


        private void DrawMenu(int x, int y)
        {
            var font = d2d.CreateFont("Consolas", 11);
            d2d.FillRectangle(x - 5, y - 22, 260, 1500, d2d.CreateBrush(2, 2, 2, 255));

            foreach (mnIndex MnIdx in Enum.GetValues(typeof(mnIndex)))
            {
                var color = d2d.CreateBrush(255, 0, 0, 255);
                if (currMnIndex == MnIdx)
                    color = d2d.CreateBrush(255, 191, 0, 255);
                d2d.DrawText(GetMenuString(MnIdx),x, y = y + 20, font, color);
            }
        }

        #endregion

        #region Draw - ESP

        private void DrawBone(GPlayer player)
        {
            Vector3 BONE_HEAD,
            BONE_NECK,
            BONE_SPINE2,
            BONE_SPINE1,
            BONE_SPINE,
            BONE_LEFTSHOULDER,
            BONE_RIGHTSHOULDER,
            BONE_LEFTELBOWROLL,
            BONE_RIGHTELBOWROLL,
            BONE_LEFTHAND,
            BONE_RIGHTHAND,
            BONE_LEFTKNEEROLL,
            BONE_RIGHTKNEEROLL,
            BONE_LEFTFOOT,
            BONE_RIGHTFOOT;

            if (WorldToScreen(player.Bone.BONE_HEAD, out BONE_HEAD) &&
            WorldToScreen(player.Bone.BONE_NECK, out BONE_NECK) &&
            WorldToScreen(player.Bone.BONE_SPINE2, out BONE_SPINE2) &&
            WorldToScreen(player.Bone.BONE_SPINE1, out BONE_SPINE1) &&
            WorldToScreen(player.Bone.BONE_SPINE, out BONE_SPINE) &&
            WorldToScreen(player.Bone.BONE_LEFTSHOULDER, out BONE_LEFTSHOULDER) &&
            WorldToScreen(player.Bone.BONE_RIGHTSHOULDER, out BONE_RIGHTSHOULDER) &&
            WorldToScreen(player.Bone.BONE_LEFTELBOWROLL, out BONE_LEFTELBOWROLL) &&
            WorldToScreen(player.Bone.BONE_RIGHTELBOWROLL, out BONE_RIGHTELBOWROLL) &&
            WorldToScreen(player.Bone.BONE_LEFTHAND, out BONE_LEFTHAND) &&
            WorldToScreen(player.Bone.BONE_RIGHTHAND, out BONE_RIGHTHAND) &&
            WorldToScreen(player.Bone.BONE_LEFTKNEEROLL, out BONE_LEFTKNEEROLL) &&
            WorldToScreen(player.Bone.BONE_RIGHTKNEEROLL, out BONE_RIGHTKNEEROLL) &&
            WorldToScreen(player.Bone.BONE_LEFTFOOT, out BONE_LEFTFOOT) &&
            WorldToScreen(player.Bone.BONE_RIGHTFOOT, out BONE_RIGHTFOOT))
            {
                int stroke = 3;
                int strokeW = stroke % 2 == 0 ? stroke / 2 : (stroke - 1) / 2;

                // Color
                var skeletonColor = player.Team == localPlayer.Team ? d2d.CreateBrush(0, 0, 255, 255) : d2d.CreateBrush(255, 200, 0, 255);

                // RECT's
                d2d.FillRectangle((int)BONE_HEAD.X - strokeW, (int)BONE_HEAD.Y - strokeW, stroke, stroke,  skeletonColor);
                d2d.FillRectangle((int)BONE_NECK.X - strokeW, (int)BONE_NECK.Y - strokeW, stroke, stroke,  skeletonColor);
                d2d.FillRectangle((int)BONE_LEFTSHOULDER.X - strokeW, (int)BONE_LEFTSHOULDER.Y - strokeW, stroke, stroke,  skeletonColor);
                d2d.FillRectangle((int)BONE_LEFTELBOWROLL.X - strokeW, (int)BONE_LEFTELBOWROLL.Y - strokeW, stroke, stroke,  skeletonColor);
                d2d.FillRectangle((int)BONE_LEFTHAND.X - strokeW, (int)BONE_LEFTHAND.Y - strokeW, stroke, stroke,  skeletonColor);
                d2d.FillRectangle((int)BONE_RIGHTSHOULDER.X - strokeW, (int)BONE_RIGHTSHOULDER.Y - strokeW, stroke, stroke,  skeletonColor);
                d2d.FillRectangle((int)BONE_RIGHTELBOWROLL.X - strokeW, (int)BONE_RIGHTELBOWROLL.Y - strokeW, stroke, stroke,  skeletonColor);
                d2d.FillRectangle((int)BONE_RIGHTHAND.X - strokeW, (int)BONE_RIGHTHAND.Y - strokeW, stroke, stroke,  skeletonColor);
                d2d.FillRectangle((int)BONE_SPINE2.X - strokeW, (int)BONE_SPINE2.Y - strokeW, stroke, stroke, skeletonColor);
                d2d.FillRectangle((int)BONE_SPINE1.X - strokeW, (int)BONE_SPINE1.Y - strokeW, stroke, stroke,  skeletonColor);
                d2d.FillRectangle((int)BONE_SPINE.X - strokeW, (int)BONE_SPINE.Y - strokeW, stroke, stroke,  skeletonColor);
                d2d.FillRectangle((int)BONE_LEFTKNEEROLL.X - strokeW, (int)BONE_LEFTKNEEROLL.Y - strokeW, stroke, stroke,  skeletonColor);
                d2d.FillRectangle((int)BONE_RIGHTKNEEROLL.X - strokeW, (int)BONE_RIGHTKNEEROLL.Y - strokeW, 2, 2,  skeletonColor);
                d2d.FillRectangle((int)BONE_LEFTFOOT.X - strokeW, (int)BONE_LEFTFOOT.Y - strokeW, 2, 2,  skeletonColor);
                d2d.FillRectangle((int)BONE_RIGHTFOOT.X - strokeW, (int)BONE_RIGHTFOOT.Y - strokeW, 2, 2,  skeletonColor);

                // Head -> Neck
                d2d.DrawLine((int)BONE_HEAD.X, (int)BONE_HEAD.Y, (int)BONE_NECK.X, (int)BONE_NECK.Y, 1, skeletonColor);

                // Neck -> Left
                d2d.DrawLine((int)BONE_NECK.X, (int)BONE_NECK.Y, (int)BONE_LEFTSHOULDER.X, (int)BONE_LEFTSHOULDER.Y, 1, skeletonColor);
                d2d.DrawLine((int)BONE_LEFTSHOULDER.X, (int)BONE_LEFTSHOULDER.Y, (int)BONE_LEFTELBOWROLL.X, (int)BONE_LEFTELBOWROLL.Y, 1, skeletonColor);
                d2d.DrawLine((int)BONE_LEFTELBOWROLL.X, (int)BONE_LEFTELBOWROLL.Y, (int)BONE_LEFTHAND.X, (int)BONE_LEFTHAND.Y, 1, skeletonColor);

                // Neck -> Right
                d2d.DrawLine((int)BONE_NECK.X, (int)BONE_NECK.Y, (int)BONE_RIGHTSHOULDER.X, (int)BONE_RIGHTSHOULDER.Y, 1, skeletonColor);
                d2d.DrawLine((int)BONE_RIGHTSHOULDER.X, (int)BONE_RIGHTSHOULDER.Y, (int)BONE_RIGHTELBOWROLL.X, (int)BONE_RIGHTELBOWROLL.Y, 1, skeletonColor);
                d2d.DrawLine((int)BONE_RIGHTELBOWROLL.X, (int)BONE_RIGHTELBOWROLL.Y, (int)BONE_RIGHTHAND.X, (int)BONE_RIGHTHAND.Y, 1, skeletonColor);

                // Neck -> Center
                d2d.DrawLine((int)BONE_NECK.X, (int)BONE_NECK.Y, (int)BONE_SPINE2.X, (int)BONE_SPINE2.Y, 1, skeletonColor);
                d2d.DrawLine((int)BONE_SPINE2.X, (int)BONE_SPINE2.Y, (int)BONE_SPINE1.X, (int)BONE_SPINE1.Y, 1, skeletonColor);
                d2d.DrawLine((int)BONE_SPINE1.X, (int)BONE_SPINE1.Y, (int)BONE_SPINE.X, (int)BONE_SPINE.Y, 1, skeletonColor);

                // Spine -> Left
                d2d.DrawLine((int)BONE_SPINE.X, (int)BONE_SPINE.Y, (int)BONE_LEFTKNEEROLL.X, (int)BONE_LEFTKNEEROLL.Y, 1, skeletonColor);
                d2d.DrawLine((int)BONE_LEFTKNEEROLL.X, (int)BONE_LEFTKNEEROLL.Y, (int)BONE_LEFTFOOT.X, (int)BONE_LEFTFOOT.Y, 1, skeletonColor);

                // Spine -> Right
                d2d.DrawLine((int)BONE_SPINE.X, (int)BONE_SPINE.Y, (int)BONE_RIGHTKNEEROLL.X, (int)BONE_RIGHTKNEEROLL.Y, 1, skeletonColor);
               d2d.DrawLine((int)BONE_RIGHTKNEEROLL.X, (int)BONE_RIGHTKNEEROLL.Y, (int)BONE_RIGHTFOOT.X, (int)BONE_RIGHTFOOT.Y,1,skeletonColor);
            }
        }

        private void DrawHealth(int X, int Y, int W, int H, int Health, int MaxHealth)
        {
            var blackBrush = d2d.CreateBrush(0, 0, 0, 255);

            if (Health <= 0)
                Health = 1;

            if (MaxHealth < Health)
                MaxHealth = 100;

            int progress = (int)((float)Health / ((float)MaxHealth / 100));
            int w = (int)((float)W / 100 * progress);

            if (w <= 2)
                w = 3;

            var Color = d2d.CreateBrush(255, 0, 0, 255);
            if (progress >= 20) Color = d2d.CreateBrush(255, 165, 0, 255);
            if (progress >= 40) Color = d2d.CreateBrush(255, 255, 0, 255);
            if (progress >= 60) Color = d2d.CreateBrush(173, 255, 47, 255);
            if (progress >= 80) Color = d2d.CreateBrush(0, 255, 0, 255);

           d2d.FillRectangle(X, Y - 1, W + 1, H + 2, blackBrush);
            d2d.FillRectangle(X + 1, Y, w - 1, H, Color);
        }

        private void DrawProgress(int X, int Y, int W, int H, int Value, int MaxValue)
        {
            var blackBrush = d2d.CreateBrush(0, 0, 0, 255);

            int progress = (int)((float)Value / ((float)MaxValue / 100));
            int w = (int)((float)W / 100 * progress);

            var Color = d2d.CreateBrush(0, 255, 0, 255);
            if (progress >= 20) Color = d2d.CreateBrush(173, 255, 47, 255);
            if (progress >= 40) Color = d2d.CreateBrush(255, 255, 0, 255);
            if (progress >= 60) Color = d2d.CreateBrush(255, 165, 0, 255);
            if (progress >= 80) Color = d2d.CreateBrush(255, 0, 0, 255);

           d2d.FillRectangle(X, Y - 1, W + 1, H + 2, blackBrush);
            if (w >= 2)
            {
                d2d.FillRectangle(X + 1, Y, w - 1, H, Color);
            }
        }

        #endregion

       

     


        #endregion



        #region Utilities Stuff

        // FPS Stats
        private static int lastTick;
        private static int lastFrameRate;
        private static int frameRate;

        // Get FPS
        public int CalculateFrameRate()
        {
            int tickCount = Environment.TickCount;
            if (tickCount - lastTick >= 1000)
            {
                lastFrameRate = frameRate;
                frameRate = 0;
                lastTick = tickCount;
            }
            frameRate++;
            return lastFrameRate;
        }

        // Quit Application
        private void Quit()
        {
            updateStream.Abort();
            //aimbotStream.Abort();
            RPM.CloseProcess();

            // Close main process
            Environment.Exit(0);
        }

        public int MultiHeight
        {
            get
            {
                Int64 pScreen = RPM.ReadInt64(Offsets.OFFSET_DXRENDERER) == 0 ? 0 : RPM.ReadInt64(RPM.ReadInt64(Offsets.OFFSET_DXRENDERER) + 56);
                return (int)(pScreen == 0 ? 0 : RPM.ReadInt32(pScreen + 92));
            }
        }

        public int MultiWidth
        {
            get
            {
                Int64 pScreen = RPM.ReadInt64(Offsets.OFFSET_DXRENDERER) == 0 ? 0 : RPM.ReadInt64(RPM.ReadInt64(Offsets.OFFSET_DXRENDERER) + 56);
                return (int)(pScreen == 0 ? 0 : RPM.ReadInt32(pScreen + 88));
            }
        }


        private void GameCheck(object sender)
        {
            while (IsGameRunning)
            {
                Process[] pList = Process.GetProcessesByName("bf4");
                process = pList.Length > 0 ? pList[0] : null;
                if (process == null)
                {
                    IsGameRunning = false;
                }

                Thread.Sleep(100);
            }
        }
        private void WriteOnLogFile(string txt)
        {
            WriteOnFile(txt, "Log");
        }

        private void WriteOnFile(string txt, string name)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(AppDomain.CurrentDomain.BaseDirectory + @"\" + name + ".txt", true))
            {
                file.WriteLine(txt);
            }
        }

        #endregion

    }
}
