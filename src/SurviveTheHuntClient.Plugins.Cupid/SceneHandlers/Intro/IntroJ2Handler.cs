using CitizenFX.Core;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models;
using SurviveTheHuntClient.Plugins.Cupid.Utils;
using System;
using static CitizenFX.Core.Native.API;
using static SurviveTheHuntShared.Plugins.Cupid.Constants;

namespace SurviveTheHuntClient.Plugins.Cupid.SceneHandlers.Intro
{
    internal class IntroJ2Handler : SceneHandlerBase<IntroJ2Handler.SceneStage, IntroJ2Handler.Tickers, IntroJ2Handler.State>
    {
        internal class SceneStageTick : SceneStageTickBaseAttribute { internal SceneStageTick(SceneStage stage) : base((int)stage) { } }

        private static readonly int _carHash = GetHashKey("boor"), _carHash2 = GetHashKey("tulip");

        private static readonly Random s_RNG = new Random();

        internal IntroJ2Handler(TriggerEventProxyDelegate triggerEventProxyDelegate, TriggerServerEventProxyDelegate triggerServerEventProxyDelegate) : base(triggerEventProxyDelegate, triggerServerEventProxyDelegate)
        {
            RequestModel((uint)_carHash);
            RequestModel((uint)_carHash2);
        }

        internal enum SceneStage
        {
            Driving,
            BitchSlap,
            GrabMoney,
            DriveHighway,
            Lift,
            Beer,
            DriveHood,
            PrisonOverhead,
            PrisonTalk1,
            PrisonTalk2,
            PrisonTalk3,
            PrisonTalk4,
            PrisonTalk5,
            PrisonBuzzGate,
            PrisonWalkOut,
            PrisonGreet1,
            PrisonGreet2,
            PrisonGreet3,
            Fade,
            Bed,
            DriveTogether,
            Outro
        }

        internal class State : SceneHandlerBaseState
        {
            internal int Car, Car2;
            internal int Lady;
            internal int LadyCar;
            internal int PopSphereId;
            internal int Clerk;
            internal int TaskSequence;
            internal int BeerBox;
            internal float FPPCarPitch;
            internal float FPPCarYaw;

            /// <summary>
            /// Owned by this script (clone)
            /// </summary>
            internal int JasPed;
            
            /// <summary>
            /// Owned by this script (clone)
            /// </summary>
            internal int LuPed;

            internal int Guard;
            internal int Girl1, Girl2;
            
            internal readonly int Camera;

            internal readonly IntroJ2Handler Handler;

            internal readonly PlayerType LocalPlayerType;

            public State()
            {
                Car = 0;
                Car2 = 0;
                LadyCar = 0;
                Lady = 0;
                PopSphereId = 0;
                Clerk = 0;
                TaskSequence = 0;
                FPPCarPitch = 0;
                FPPCarYaw = 0;
                BeerBox = 0;
                Guard = 0;
                Girl1 = Girl2 = 0;

                Camera = 0;
                JasPed = 0;
                LocalPlayerType = PlayerType.Cop;
            }

            internal State(int jasPedId, int luPedId, int camera, PlayerType localPlayerType, IntroJ2Handler handler) : this()
            {
                JasPed = jasPedId;
                LuPed = luPedId;
                Camera = camera;
                Handler = handler;
                LocalPlayerType = localPlayerType;
            }
        }

        internal override float GetStageDuration(SceneStage stage)
        {
            switch(stage)
            {
                case SceneStage.Driving:
                    return 4.95f;
                case SceneStage.BitchSlap:
                    return 0.9f;
                case SceneStage.GrabMoney:
                    return 1.4f;
                case SceneStage.DriveHighway:
                    return 3.8f;
                case SceneStage.Lift:
                    return 3.2f;
                case SceneStage.Beer:
                    return 3.667f;
                case SceneStage.DriveHood:
                    return 4.55f;
                case SceneStage.PrisonOverhead:
                    return 1.95f;
                case SceneStage.PrisonTalk1:
                    return 2.4f;
                case SceneStage.PrisonTalk2:
                    return 1.05f;
                case SceneStage.PrisonTalk3:
                    return 1.85f;
                case SceneStage.PrisonTalk4:
                    return 2f;
                case SceneStage.PrisonTalk5:
                    return 1.45f;
                case SceneStage.PrisonBuzzGate:
                    return 2.55f;
                case SceneStage.PrisonWalkOut:
                    return 3.1f;
                case SceneStage.PrisonGreet1:
                    return 0.73f;
                case SceneStage.PrisonGreet2:
                    return 1.05f;
                case SceneStage.PrisonGreet3:
                    return 1.2f;
                case SceneStage.Fade:
                    return 0.5f;
                case SceneStage.Bed:
                    return 5.65f;
                case SceneStage.DriveTogether:
                    return 4f;
                case SceneStage.Outro:
                    return 37f;
                default:
                    return 0f;
            }
        }

        public override void StartScene(in IGameState gameState, int cameraId)
        {
            base.StartScene(gameState, cameraId);

            HuntPlayer pJ = gameState.Hunt.HuntedPlayers[(int)PlayerType.HuntedJ];
            HuntPlayer pL = gameState.Hunt.HuntedPlayers.Length > 1 ? gameState.Hunt.HuntedPlayers[(int)PlayerType.HuntedL] : pJ;
            // who the fuck made `isNetwork` a float
            CurrentState = new State(ClonePed(GetPlayerPed(pJ.PlayerHandle), 0f, false, false), ClonePed(GetPlayerPed(pL.PlayerHandle), 0f, false, false), cameraId, PlayerUtils.GetPlayerType(PlayerId(), gameState.Hunt.HuntedPlayers), this);

            CurrentState.PopSphereId = AddPopMultiplierSphere(Constants.CarStartPos.X, Constants.CarStartPos.Y, Constants.CarStartPos.Z, 100f, 0, 0, false, false);

            ClearAreaOfVehicles(Constants.CarStartPos.X, Constants.CarStartPos.Y, Constants.CarStartPos.Z, 40f, false, false, false, false, false);
            ClearAreaOfPeds(Constants.CarStartPos.X, Constants.CarStartPos.Y, Constants.CarStartPos.Z, 40f, 0);

            SetFocusEntity(CurrentState.JasPed);

            SetEntityAsMissionEntity(CurrentState.JasPed, false, true);
            SetEntityAsMissionEntity(CurrentState.LuPed, false, true);

            SetEntityInvincible(CurrentState.JasPed, true);
            SetEntityInvincible(CurrentState.LuPed, true);

            RequestModel((uint)_carHash);
        }

        private static class Constants
        {
            internal static Vector3 CarStartPos = new Vector3(-3059.198f, 408.4283f, 6.45f);
            
            internal const string TakedownAnimDict = "melee@unarmed@streamed_variations";
            internal const string TakedownAnimClipKiller = "plyr_takedown_front_backslap";
            internal const string TakedownAnimClipVictim = "victim_takedown_front_backslap";
            internal const string GrabMoneyAnimDict = "oddjobs@shop_robbery@rob_till";
            internal const string GrabMoneyAnimClip = "loop";
            
            internal const float HighwayStartX = -392.4063f, HighwayStartY = -1072.351f;
            
            internal const string LiftAnimDict = "switch@franklin@gym";
            internal const string LiftAnimClip = "001942_02_gc_fras_ig_5_base";

            internal const float HoodCarStartX = -12.91f, HoodCarStartY = -1457.97f;
            internal const float HoodCarStartHeading = 97.62f;

            internal static readonly uint BeerBoxHash = (uint)GetHashKey("prop_cs_beer_box");

            internal const float JasPrison1X = 1830.58f, JasPrison1Y = 2604.86f, JasPrison1Heading = 213.43f;

            internal const float PrisonGuardX = 1832f, PrisonGuardY = 2602.426f, PrisonGuardZ = 44.889f, PrisonGuardHeading = 27.47f;

            internal const float LuPrison1X = 1837.853f, LuPrison1Y = 2609.089f, LuPrison1Heading = 271.8625f;
            internal const float LuPrison2X = 1849.379f, LuPrison2Y = 2609.139f, LuPrison2Heading = 257.2621f;
            internal const float JasPrison2X = 1852.834912109375f, JasPrison2Y = 2610.071728515625f, JasPrison2Heading = 90.03719f;

            internal static readonly uint GirlModelHash = (uint)GetHashKey("a_f_y_carclub_01");
            internal const string GirlAnimDict1 = "amb@world_human_prostitute@hooker@idle_a";
            internal const string GirlAnimDict2 = "amb@world_human_prostitute@french@idle_a";
            internal static readonly string[] GirlAnimClips = { "idle_a", "idle_b", "idle_c" };

            internal const float BedX = -1146.581f, BedY = -1515.909f, BedZ = 10f, BedHeading = 215.3f;
            internal const float
                BedCamPosX = -1147.5504150390625f, BedCamPosY = -1515.123779296875f, BedCamPosZ = 10.453112602233887f,
                BedCamRotX = -2.594290018081665f, BedCamRotY = 0.00026729164528660476f, BedCamRotZ = -140.4815673828125f,
                BedCamFov = 38f;

            internal const string BedAnimDict = "anim@heists@ornate_bank@managers_room";
            internal const string BedAnimJasClip = "ig_11_managers_room_player";
            internal const string BedAnimLuClip = "ig_11_managers_room_manager";

            internal const float Car2X = 2898.972f, Car2Y = 4142.792f, Car2Z = 49.74251f;
            internal const float Car2Heading = 18.75201f;
        }

        internal class Tickers
        {
            [SceneStageTick(SceneStage.Driving)]
            public static void Ticker(float deltaTime, ref State state)
            {
                if (state.Car == 0)
                {
                    RequestModel((uint)_carHash);

                    if (HasModelLoaded((uint)_carHash))
                    {
                        state.Car = CreateVehicle((uint)_carHash, Constants.CarStartPos.X, Constants.CarStartPos.Y, Constants.CarStartPos.Z, 159.6f, false, false);
                        SetEntityAsMissionEntity(state.Car, false, true);
                        SetVehicleEngineOn(state.Car, true, true, true);
                        SetVehicleForwardSpeed(state.Car, 2.5f);
                        SetPedIntoVehicle(state.JasPed, state.Car, -1);
                        TaskVehicleDriveWander(state.JasPed, state.Car, 5f, 0);
                        Vector3 fwdVec = GetEntityForwardVector(state.Car);
                        Vector3 rightVec = -Vector3.Cross(fwdVec, Vector3.Up);
                        Vector3 pedPos = GetEntityCoords(state.JasPed, false);
                        Vector3 offset = new Vector3(-1.35f, 0.18f, 0.78f);
                        //PointCamAtEntity(state.Camera, state.JasPed, 0f, 0f, 0f, true);
                        SetCamRot(state.Camera, 0f, 0f, 0f, 0);
                        Vector3 rot = GetCamRot(state.Camera, 0);
                        HardAttachCamToEntity(state.Camera, state.Car, 0, 0, -90f, offset.X, offset.Y, offset.Z, true);
                        SetVehicleRadioEnabled(state.Car, false);
                    }
                }

                if (state.LadyCar == 0)
                {
                    RequestModel((uint)VehicleHash.Penumbra);
                    RequestModel((uint)PedHash.AmandaTownley);

                    if (HasModelLoaded((uint)VehicleHash.Penumbra) && HasModelLoaded((uint)PedHash.AmandaTownley))
                    {
                        state.LadyCar = CreateVehicle((uint)VehicleHash.Penumbra, -3072.135f, 394.408f, 6.271485f, 250f, false, false);
                        SetEntityAsMissionEntity(state.LadyCar, false, true);
                        state.Lady = CreatePed(0, (uint)PedHash.AmandaTownley, -3071.66f, 397.2125f, 6.968524f, 245.9f, false, false);
                        SetEntityAsMissionEntity(state.Lady, false, true);
                    }
                }

                SetUseHiDof();
                SetCamNearDof(state.Camera, 0.1f);
                SetCamFarDof(state.Camera, 0.5f);
                SetCamDofStrength(state.Camera, 0.6f);
                SetCamUseShallowDofMode(state.Camera, true);
            }

            [SceneStageTick(SceneStage.BitchSlap)]
            public static void BitchSlap(float deltaTime, ref State state)
            {
                if (state.StageJustSwitched)
                {
                    DetachCam(state.Camera);
                    SetEntityCoords(state.JasPed, -3042.045f, 585.9725f, 5.908928f, false, false, false, false);
                    SetEntityHeading(state.JasPed, 199.6f);
                    SetCamCoord(state.Camera, -3040.19946289062f, 583.2584838867188f, 8.370817184448242f);
                    SetCamRot(state.Camera, -2.0012354850769043f, 0.00011842657841043547f, 33.33742904663086f, 2);
                    SetCamFov(state.Camera, 36.6f);
                    SetCamShakeAmplitude(state.Camera, 1f);
                    ShakeCam(state.Camera, "HAND_SHAKE", 0.5f);

                    OpenSequenceTask(ref state.TaskSequence);
                    TaskPlayAnim(0, Constants.TakedownAnimDict, Constants.TakedownAnimClipVictim, 8f, -1f, 850, 0, 0, false, false, false);
                    TaskCower(0, -1);
                    CloseSequenceTask(state.TaskSequence);

                    TaskPlayAnim(state.JasPed, Constants.TakedownAnimDict, Constants.TakedownAnimClipKiller, 8f, -1f, -1, 0, 0, false, false, false);
                    TaskPerformSequence(state.Clerk, state.TaskSequence);
                }

                RequestCollisionAtCoord(Constants.HighwayStartX, Constants.HighwayStartY, 36f);
            }

            [SceneStageTick(SceneStage.GrabMoney)]
            public static void GrabMoney(float deltaTime, ref State state)
            {
                if(state.StageJustSwitched)
                {
                    SetCamCoord(state.Camera, -3040.95546289062f, 585.417114258f, 9.018735885620117f);
                    SetCamRot(state.Camera, -38.00886154174805f, -6.84211540222168f, 152.7925567f, 2);
                    SetCamFov(state.Camera, 26.4f);
                    SetCamShakeAmplitude(state.Camera, 0f);
                    SetEntityCoords(state.JasPed, -3041.219f, 583.8357f, 6.908928f, false, false, false, false);
                    SetEntityCoords(state.Clerk, -3042.219f, 583.7357f, 6.408928f, false, false, false, false);
                    SetEntityHeading(state.JasPed, 18.17478f);

                    TaskPlayAnim(state.JasPed, Constants.GrabMoneyAnimDict, Constants.GrabMoneyAnimClip, 8f, -1f, -1, 0, 0, false, false, false);
                }

                RequestCollisionAtCoord(Constants.HighwayStartX, Constants.HighwayStartY, 36f);
            }

            [SceneStageTick(SceneStage.DriveHighway)]
            public static void DriveHighway(float deltaTime, ref State state)
            {
                if(state.StageJustSwitched)
                {
                    const float startHeading = 1.62f;
                    float startZ = Math.Min(36f, GetHeightmapTopZForPosition(Constants.HighwayStartX, Constants.HighwayStartY));

                    float startHeadingFinal = startHeading;
                    Vector3 startPos = new Vector3(Constants.HighwayStartX, Constants.HighwayStartY, startZ);
                    //GetClosestVehicleNodeWithHeading(Constants.HighwayStartX, Constants.HighwayStartY, startZ, ref startPos, ref startHeadingFinal, 0, 0.2f, 1);

                    SetPedIntoVehicle(state.JasPed, state.Car, -1);
                    SetEntityCoords(state.Car, startPos.X, startPos.Y, startPos.Z, false, false, false, false);
                    SetEntityHeading(state.Car, startHeadingFinal);
                    SetVehicleForwardSpeed(state.Car, 15f);
                    TaskVehicleDriveWander(state.JasPed, state.Car, 13.5f, 0);
                    HardAttachCamToEntity(state.Camera, state.Car, 0f, 0f, 0f, -0.35f, 0.15f, 0.85f, true);
                    SetCamFov(state.Camera, 55f);
                    SetCamShakeAmplitude(state.Camera, 0.4f);
                    ShakeCam(state.Camera, "HAND_SHAKE", 0.45f);
                    InstantlyFillVehiclePopulation();
                }

                if(state.CurrentStageTime >= 1f && state.CurrentStageTime <= 3f)
                {
                    const float pitchDelta = 6f, yawDelta = 2f;
                    //Vector3 camRot = GetCamRot(state.Camera, 2);
                    state.FPPCarPitch += pitchDelta * deltaTime;
                    state.FPPCarYaw += yawDelta * deltaTime;
                    //SetCamRot(state.Camera, camRot.X + pitchDelta * deltaTime, camRot.Y, camRot.Z + yawDelta * deltaTime, 2);
                    HardAttachCamToEntity(state.Camera, state.Car, state.FPPCarPitch, 0f, state.FPPCarYaw, -0.35f, 0.15f, 0.85f, true);
                }

                // prevent the noggin from clipping into the camera
                SetPedResetFlag(state.JasPed, 166, true);
            }

            [SceneStageTick(SceneStage.Lift)]
            public static void Lift(float deltaTime, ref State state)
            {
                if(state.StageJustSwitched)
                {
                    SetEntityCoords(state.JasPed, -1244.868f, -1613.661f, 3.4026925563812256f, false, false, false, true);
                    SetEntityHeading(state.JasPed, 34.8f);
                    TaskPlayAnim(state.JasPed, Constants.LiftAnimDict, Constants.LiftAnimClip, 8f, -1f, -1, 1, 0f, false, false, false);
                    DetachCam(state.Camera);
                    SetCamShakeAmplitude(state.Camera, 0f);
                    SetCamCoord(state.Camera, -1253.62f, -1613.45f, 4.65f);
                    SetCamRot(state.Camera, -1.66f, 0.09f, -86.76f, 2);
                    SetCamFov(state.Camera, 21f);
                }

                RequestCollisionAtCoord(Constants.HoodCarStartX, Constants.HoodCarStartY, 31f);
            }

            [SceneStageTick(SceneStage.Beer)]
            public static void Beer(float dT, ref State state)
            {
                if(state.StageJustSwitched)
                {
                    Vector3 coords = GetEntityCoords(state.JasPed, false);
                    state.BeerBox = CreateObject((int)Constants.BeerBoxHash, coords.X, coords.Y, coords.Z, false, false, false);
                    
                    // TODO: flip the box around
                    AttachEntityToEntity(state.BeerBox, state.JasPed, GetPedBoneIndex(state.JasPed, (int)Bone.IK_R_Hand), 0f, 0f, 0f, 0f, 0f, 0f, false, false, false, true, 2, true);

                    ClearPedTasksImmediately(state.JasPed);
                    const float startX = -1223.8632812f, startY = -906.5182495117188f, startZ = 10.964961051940918f;
                    SetEntityCoords(state.JasPed, startX, startY, startZ, false, false, false, false);
                    SetEntityHeading(state.JasPed, 34.5f);
                    Vector3 fwd = GetEntityForwardVector(state.JasPed);
                    SetCamRot(state.Camera, 0f, 0f, 34.5f, 0);
                    SetCamFov(state.Camera, 40f);
                    HardAttachCamToEntity(state.Camera, state.JasPed, 0f, 0f, 0f, 0f, -1.45f, -0.15f, true);
                    Vector3 target = new Vector3(startX, startY, startZ) + (fwd * 6.7f);

                    TaskGoStraightToCoord(state.JasPed, target.X, target.Y, target.Z, 0.5f, -1, 34.5f, 0.001f);
                }

                RequestCollisionAtCoord(Constants.HoodCarStartX, Constants.HoodCarStartY, 31f);
            }

            [SceneStageTick(SceneStage.DriveHood)]
            public static void DriveHood(float deltaTime, ref State state)
            {
                if(state.StageJustSwitched)
                {
                    if(state.BeerBox != 0)
                    {
                        DeleteEntity(ref state.BeerBox);
                        state.BeerBox = 0;
                    }

                    ClearPedTasksImmediately(state.JasPed);

                    float startZ = Math.Min(32.4f, GetHeightmapBottomZForPosition(Constants.HoodCarStartX, Constants.HoodCarStartY));

                    float startHeadingFinal = Constants.HoodCarStartHeading;
                    Vector3 startPos = new Vector3(Constants.HoodCarStartX, Constants.HoodCarStartY, startZ);
                    //GetClosestVehicleNodeWithHeading(Constants.HoodCarStartX, Constants.HoodCarStartY, startZ, ref startPos, ref startHeadingFinal, 0, 0.2f, 1);

                    SetPedIntoVehicle(state.JasPed, state.Car, -1);
                    SetEntityCoords(state.Car, startPos.X, startPos.Y, startPos.Z, false, false, false, false);
                    SetEntityHeading(state.Car, startHeadingFinal);
                    SetVehicleForwardSpeed(state.Car, 5.5f);
                    TaskVehicleDriveWander(state.JasPed, state.Car, 6.5f, 0);
                    HardAttachCamToEntity(state.Camera, state.Car, 0f, 0f, 0f, 0.1f, -0.15f, 0.8f, true);
                    SetCamFov(state.Camera, 45f);
                    SetCamShakeAmplitude(state.Camera, 0.4f);
                    ShakeCam(state.Camera, "HAND_SHAKE", 0.55f);
                    InstantlyFillVehiclePopulation();
                }

                const float pitchDelta = -1.5f, yawDelta = 6f;
                state.FPPCarPitch += pitchDelta * deltaTime;
                state.FPPCarYaw += yawDelta * deltaTime;
                HardAttachCamToEntity(state.Camera, state.Car, state.FPPCarPitch, 0f, state.FPPCarYaw, 0.1f, -0.15f, 0.8f, true);


                RequestCollisionAtCoord(Constants.JasPrison1X, Constants.JasPrison1Y, 44f);
            }

            [SceneStageTick(SceneStage.PrisonOverhead)]
            public static void PrisonOverhead(float deltaTime, ref State state)
            {
                if(state.StageJustSwitched)
                {
                    DetachCam(state.Camera);

                    // Disable camera shake
                    SetCamShakeAmplitude(state.Camera, 0f);
                    ShakeCam(state.Camera, "HAND_SHAKE", 0f);

                    const float z = 44f;

                    state.Guard = CreatePed(0, (uint)PedHash.Prisguard01SMM, Constants.PrisonGuardX, Constants.PrisonGuardY, Constants.PrisonGuardZ, Constants.PrisonGuardHeading, false, false);
                    SetEntityAsMissionEntity(state.Guard, false, true);
                    
                    SetEntityCoords(state.JasPed, Constants.JasPrison1X, Constants.JasPrison1Y, z, false, false, false, false);
                    SetEntityHeading(state.JasPed, Constants.JasPrison1Heading);
                }

                const float camStartPosX = 1901.8f, camStartPosY = 2645.8f, camStartPosZ = 45.08f;
                const float camEndPosX = 1903.467f, camEndPosY = 2648.04f, camEndPosZ = 45.08f;
                const float camStartRotX = 11.8577f, camStartRotY = 0.093f, camStartRotZ = 54.845f;
                const float camEndRotX = camStartRotX, camEndRotY = camStartRotY, camEndRotZ = 54.855f;
                const float camFov = 15.64f;
                
                CamUtils.Lerp(state.Camera, camStartPosX, camStartPosY, camStartPosZ, camStartRotX, camStartRotY, camStartRotZ, camFov, camEndPosX, camEndPosY, camEndPosZ, camEndRotX, camEndRotY, camEndRotZ, camFov, state.CurrentStageDuration, state.CurrentStageTime);
            }

            [SceneStageTick(SceneStage.PrisonTalk1)]
            public static void PrisonTalk1(float deltaTime, ref State state)
            {
                if(state.StageJustSwitched)
                {
                    const float camStartPosX = 1829.656f, camStartPosY = 2605.177f, camStartPosZ = 46.56636f;
                    const float camStartRotX = -14.8395f, camStartRotY = -6.14592266082f, camStartRotZ = -130.03939819335938f;
                    const float camStartFov = 37f;

                    SetCamCoord(state.Camera, camStartPosX, camStartPosY, camStartPosZ);
                    SetCamRot(state.Camera, camStartRotX, camStartRotY, camStartRotZ, 2);
                    SetCamFov(state.Camera, camStartFov);
                    SetCamShakeAmplitude(state.Camera, 1f);
                    ShakeCam(state.Camera, "HAND_SHAKE", 0.65f);
                }
            }

            [SceneStageTick(SceneStage.PrisonTalk5)]
            public static void PrisonTalk5(float deltaTime, ref State state)
            {
                if(state.StageJustSwitched)
                {
                    state.Handler.AVControllerHelper.StartStage("Prison");

                    CupidPlugin.SetPlayerClothing(state.LuPed, PlayerType.HuntedL, Cupid.Constants.DirectedScene.JasonDrivingHood);
                }
            }

            [SceneStageTick(SceneStage.PrisonBuzzGate)]
            public static void PrisonBuzzGate(float deltaTime, ref State state)
            {
                if (state.StageJustSwitched)
                {
                    const float camStartPosX = 1853.78662109375f, camStartPosY = 2609.88525390625f, camStartPosZ = 46.33966827392578f;
                    const float camStartRotX = -6.158645582199096f, camStartRotY = 0.001460685976780951f, camStartRotZ = 94.22196960449219f;
                    const float camStartFov = 20.91f;

                    SetEntityCoords(state.JasPed, Constants.JasPrison2X, Constants.JasPrison2Y, 44f, false, false, false, true);
                    SetEntityCoords(state.LuPed, Constants.LuPrison1X, Constants.LuPrison1Y, 44f, false, false, false, false);
                    SetEntityHeading(state.LuPed, Constants.LuPrison1Heading);

                    SetEntityHeading(state.JasPed, Constants.JasPrison2Heading + 180f);
                    TaskAchieveHeading(state.JasPed, Constants.JasPrison2Heading, 3000);

                    SetCamCoord(state.Camera, camStartPosX, camStartPosY, camStartPosZ);
                    SetCamRot(state.Camera, camStartRotX, camStartRotY, camStartRotZ, 2);
                    SetCamFov(state.Camera, camStartFov);
                    SetCamShakeAmplitude(state.Camera, 1f);
                    ShakeCam(state.Camera, "HAND_SHAKE", 0.65f);
                }
            }

            [SceneStageTick(SceneStage.PrisonWalkOut)]
            public static void PrisonWalkOut(float deltaTime, ref State state)
            {
                const float camStartPosX = 1844.73583984375f, camStartPosY = 2606.576416015625f, camStartPosZ = 45.413582f;
                const float camStartRotX = -0.33496195077f, camStartRotY = -0.5372275513f, camStartRotZ = 71.35124969482f;
                const float camStartFov = 17.24954f;

                const float camEndPosX = camStartPosX + 2f, camEndPosY = camStartPosY - 0.2f;

                if (state.StageJustSwitched)
                {
                    SetCamShakeAmplitude(state.Camera, 0.5f);
                    ShakeCam(state.Camera, "HAND_SHAKE", 0.15f);

                    TaskGoStraightToCoord(state.LuPed, Constants.LuPrison2X, Constants.LuPrison2Y, 44f, 0.5f, -1, Constants.LuPrison2Heading, 0.001f);
                }

                const float camOffsetX = -1f, camOffsetY = 1f, camOffsetZ = 0.5f;

                CamUtils.Lerp(state.Camera, camStartPosX + camOffsetX, camStartPosY + camOffsetY, camStartPosZ + camOffsetZ, camStartRotX, camStartRotY, camStartRotZ, camStartFov, camEndPosX + camOffsetX, camEndPosY + camOffsetY, camStartPosZ + camOffsetZ, camStartRotX, camStartRotY, camStartRotZ, camStartFov, state.CurrentStageDuration, state.CurrentStageTime);
            }

            [SceneStageTick(SceneStage.PrisonGreet1)]
            public static void PrisonGreet1(float deltaTime, ref State state)
            {
                if (state.StageJustSwitched)
                {
                    const float camStartPosX = 1845.1439208984375f, camStartPosY = 2607.478759765625f, camStartPosZ = 46.21189880371094f;
                    const float camStartRotX = -1.2146679162979126f, camStartRotY = 0.000753010855987668f, camStartRotZ = -72.495872497558f;
                    const float camStartFov = 8.23f;

                    SetCamCoord(state.Camera, camStartPosX, camStartPosY, camStartPosZ);
                    SetCamRot(state.Camera, camStartRotX, camStartRotY, camStartRotZ, 2);
                    SetCamFov(state.Camera, camStartFov);
                    SetCamShakeAmplitude(state.Camera, 1f);
                    ShakeCam(state.Camera, "HAND_SHAKE", 0.05f);

                    float offsetX = Constants.LuPrison2X - Constants.LuPrison1X, offsetY = Constants.LuPrison2Y - Constants.LuPrison1Y;
                    const float skipTo = 0.85f;
                    ClearPedTasksImmediately(state.LuPed);
                    SetEntityCoords(state.LuPed, Constants.LuPrison1X + offsetX * skipTo, Constants.LuPrison1Y + offsetY * skipTo, 44f, false, false, false, false);
                    TaskGoStraightToCoord(state.LuPed, Constants.LuPrison2X, Constants.LuPrison2Y, 44f, 0.5f, -1, Constants.LuPrison2Heading, 0.001f);
                }
            }

            [SceneStageTick(SceneStage.Bed)]
            public static void Bed(float deltaTime, ref State state)
            {
                const float bedSeconds = 3.633f;

                if(state.StageJustSwitched)
                {
                    const float heading = Constants.BedHeading + 90f;

                    DetachCam(state.Camera);
                    SetCamCoord(state.Camera, Constants.BedCamPosX, Constants.BedCamPosY, Constants.BedCamPosZ);
                    SetCamRot(state.Camera, Constants.BedCamRotX, Constants.BedCamRotY, Constants.BedCamRotZ, 2);
                    SetCamFov(state.Camera, Constants.BedCamFov);

                    CupidPlugin.SetPlayerClothing(state.JasPed, PlayerType.HuntedJ, Cupid.Constants.DirectedScene.Bed);
                    CupidPlugin.SetPlayerClothing(state.LuPed, PlayerType.HuntedL, Cupid.Constants.DirectedScene.Bed);

                    SetEntityCoords(state.JasPed, Constants.BedX, Constants.BedY, Constants.BedZ, false, false, false, false);
                    SetEntityNoCollisionEntity(state.JasPed, state.LuPed, false);
                    SetEntityNoCollisionEntity(state.LuPed, state.JasPed, false);
                    SetEntityCoords(state.LuPed, Constants.BedX, Constants.BedY, Constants.BedZ, false, false, false, false);
                    SetEntityHeading(state.LuPed, heading);
                    SetEntityHeading(state.JasPed, heading);

                    const float offsetX = -1.4f + 4.3f + 1.4f - 1.1f, offsetY = -3.64f + 4.8f - 0.6f - 0.8f, offsetZ = 0.6f;
                    TaskPlayAnimAdvanced(state.JasPed, Constants.BedAnimDict, Constants.BedAnimJasClip, Constants.BedX + offsetX + 3f - 2.3f + 1.1f + 0.6f, Constants.BedY + offsetY + 3f - 2.2f + 0.5f - 2.4f, Constants.BedZ + offsetZ - 0.15f, 0f, 0f, heading + 120f, 8f, 8f, -1, 2 | 512 | 8 | 2048, 0.8f, 0, 0);
                    TaskPlayAnimAdvanced(state.LuPed, Constants.BedAnimDict, Constants.BedAnimLuClip, Constants.BedX + offsetX, Constants.BedY + offsetY, Constants.BedZ + offsetZ - 0.1f, 0f, 0f, heading + 150f, 8f, 8f, -1, 2 | 512 | 8 | 2048, 0.8f, 0, 0);
                }

                RequestCollisionAtCoord(Constants.Car2X, Constants.Car2Y, Constants.Car2Z);
            }

            [SceneStageTick(SceneStage.DriveTogether)]
            public static void DriveTogether(float deltaTime, ref State state)
            {
                if(state.StageJustSwitched)
                {
                    int localPlayerId = PlayerId();
                    PlayerType playerType = PlayerUtils.GetPlayerType(localPlayerId, state.Handler.GameState.Hunt.HuntedPlayers);
                    int playerPed = GetPlayerPed(localPlayerId);

                    SetFocusEntity(playerPed);

                    CupidPlugin.SetPlayerClothing(playerPed, playerType, Cupid.Constants.DirectedScene.Default1);

                    if (playerType != PlayerType.Cop)
                    {
                        SetEntityCoords(state.Car2, Constants.Car2X, Constants.Car2Y, Constants.Car2Z, false, false, false, true);
                        SetPedIntoVehicle(playerPed, state.Car2, playerType == PlayerType.HuntedJ ? -1 : 0);
                        SetVehicleRadioEnabled(state.Car2, false);
                        FreezeEntityPosition(state.Car2, false);
                        
                        if(playerType == PlayerType.HuntedJ)
                        {
                            const float carDestX = 2821.6f, carDestY = 4383.233f, carDestZ = 48.84974f, carDestHeading = 22.7f;
                            SetVehicleForwardSpeed(state.Car2, 27.5f);
                            TaskVehicleDriveToCoord(playerPed, state.Car2, carDestX, carDestY, carDestZ, 75f, 1, (uint)_carHash2, 5 | 32, 1f, 1f);
                            //TaskVehicleDriveWander(playerPed, state.Car2, 15f, 0);
                        }
                    }
                }

                if(state.LocalPlayerType != PlayerType.Cop)
                {
                    const float camPosAX = 2907.81665039062f, camPosAY = 4136.63232421875f, camPosAZ = 49.86284637451172f;
                    const float camRotAX = -0.02635776996612549f, camRotAY = 0.0007176236249506474f, camRotAZ = 61.15264129638672f;
                    const float camFovA = 15.432522773742676f;

                    const float camPosBX = 2829.90087890625f, camPosBY = 4383.27539062f, camPosBZ = 51.829647064208984f;
                    const float camRotBX = -12.320538520812988f, camRotBY = -17.3291969299316f, camRotBZ = 86.11876678466797f;
                    const float camFovB = 32.36821746826172f;

                    CamUtils.Lerp(state.Camera, camPosAX, camPosAY, camPosAZ, camRotAX, camRotAY, camRotAZ, camFovA, camPosBX, camPosBY, camPosBZ, camRotBX, camRotBY, camRotBZ, camFovB, state.CurrentStageDuration + 3f, state.CurrentStageTime * 0.55f);
                }
            }

            [SceneStageTick(SceneStage.Outro)]
            public static void Outro(float deltaTime, ref State state)
            {
                if(state.StageJustSwitched)
                {
                    ClearPedTasks(PlayerPedId());
                    SetFocusEntity(PlayerPedId());
                    SetCamActive(state.Camera, false);
                    SetVehicleForwardSpeed(state.Car2, 20f);
                }

                // allow control in this bit, we just need to let the stage run till end so music and UI can play.
                EnableAllControlActions(0);
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();

            if(CurrentState.Car != 0)
            {
                ClearPedTasks(CurrentState.JasPed);
                DeleteEntity(ref CurrentState.Car);
            }

            if(CurrentState.LadyCar != 0)
            {
                DeleteEntity(ref CurrentState.LadyCar);
                DeleteEntity(ref CurrentState.Lady);
            }

            if(CurrentState.Clerk != 0)
            {
                DeleteEntity(ref CurrentState.Clerk);
            }

            if(CurrentState.TaskSequence != 0)
            {
                ClearSequenceTask(ref CurrentState.TaskSequence);
            }

            if(CurrentState.BeerBox != 0)
            {
                DeleteEntity(ref CurrentState.BeerBox);
            }

            if(CurrentState.JasPed != 0)
            {
                DeleteEntity(ref CurrentState.JasPed);
            }

            if(CurrentState.LuPed != 0)
            {
                DeleteEntity(ref CurrentState.LuPed);
            }

            if(CurrentState.Guard != 0)
            {
                DeleteEntity(ref CurrentState.Guard);
            }

            if(CurrentState.Girl1 != 0)
            {
                DeleteEntity(ref CurrentState.Girl1);
                DeleteEntity(ref CurrentState.Girl2);
            }

            if (CurrentState.Car2 != 0)
            {
                SetEntityAsMissionEntity(CurrentState.Car2, true, true);
                DeleteEntity(ref CurrentState.Car2);
                CurrentState.Car2 = 0;
            }

            ClearPopSphere();
        }

        private void ClearPopSphere()
        {
            if (CurrentState.PopSphereId != 0)
            {
                RemovePopMultiplierSphere(CurrentState.PopSphereId, false);
                CurrentState.PopSphereId = 0;
            }
        }

        private const string TulipNetEntName = "CupidTulip";

        public override void OnNetEntityReceived(int netId, string name)
        {
            base.OnNetEntityReceived(netId, name);

            if(CurrentState.Car2 == 0 && name == TulipNetEntName)
            {
                CurrentState.Car2 = NetworkGetEntityFromNetworkId(netId);
            }
        }

        public override void Tick(float deltaTime)
        {
            if(CurrentState.Clerk == 0)
            {
                uint model = (uint)PedHash.ShopKeep01;
                RequestModel(model);
                if(HasModelLoaded(model))
                {
                    CurrentState.Clerk = CreatePed(0, model, -3041.861f, 584.9562f, 7.908928f, 19.71f, false, false);
                    SetEntityAsMissionEntity(CurrentState.Clerk, false, true);
                }
            }

            if(CurrentState.Girl1 == 0)
            {
                if(HasModelLoaded(Constants.GirlModelHash) && HasAnimDictLoaded(Constants.GirlAnimDict1) && HasAnimDictLoaded(Constants.GirlAnimDict2))
                {
                    const float posX = -1242.145f, posY = -1610f, maxPosZ = 3.7f, heading = 128.4f;
                    float posZ = maxPosZ;
                    CurrentState.Girl1 = CreatePed(0, Constants.GirlModelHash, posX, posY, posZ, heading, false, false);
                    Vector3 rightVec = Vector3.Cross(Vector3.Up, GetEntityForwardVector(CurrentState.Girl1));
                    const float distance = 0.45f;
                    CurrentState.Girl2 = CreatePed(0, Constants.GirlModelHash, posX + rightVec.X * distance, posY + rightVec.Y * distance, posZ + rightVec.Z * distance, heading, false, false);
                    
                    SetEntityAsMissionEntity(CurrentState.Girl1, false, true);
                    SetEntityAsMissionEntity(CurrentState.Girl2, false, true);

                    SetPedRandomComponentVariation(CurrentState.Girl1, false);
                    SetPedRandomComponentVariation(CurrentState.Girl2, false);
                    SetPedRandomProps(CurrentState.Girl1);
                    SetPedRandomProps(CurrentState.Girl2);

                    string randomAnim = Constants.GirlAnimClips[s_RNG.Next(Constants.GirlAnimClips.Length)];
                    TaskPlayAnim(CurrentState.Girl1, Constants.GirlAnimDict1, randomAnim, 1f, 1f, -1, 1, 0f, false, false, false);
                    randomAnim = Constants.GirlAnimClips[s_RNG.Next(Constants.GirlAnimClips.Length)];
                    TaskPlayAnim(CurrentState.Girl2, Constants.GirlAnimDict2, randomAnim, 1f, 1f, -1, 1, 0f, false, false, false);
                }
                else
                {
                    RequestAnimDict(Constants.GirlAnimDict1);
                    RequestAnimDict(Constants.GirlAnimDict2);
                }
            }

            bool wasOver = IsOver;

            base.Tick(deltaTime);

            if(!wasOver && IsOver)
            {
                AVControllerHelper.EndStage();
                ClearPedTasks(CurrentState.JasPed);
                RemoveAnimDict(Constants.TakedownAnimDict);
                RemoveAnimDict(Constants.GrabMoneyAnimDict);
                RemoveAnimDict(Constants.LiftAnimDict);
                RemoveAnimDict(Constants.GirlAnimDict1);
                RemoveAnimDict(Constants.GirlAnimDict2);
                RemoveAnimDict(Constants.BedAnimDict);
                SetModelAsNoLongerNeeded(Constants.BeerBoxHash);
                SetModelAsNoLongerNeeded((uint)PedHash.Prisguard01SMM);
                SetModelAsNoLongerNeeded(Constants.GirlModelHash);
                ClearPopSphere();

                // TODO: remove simeon, amanda, car, clones, etc.

                SetVehicleRadioEnabled(CurrentState.Car2, true);
            }
            else if(!wasOver)
            {
                RequestAnimDict(Constants.TakedownAnimDict);
                RequestAnimDict(Constants.GrabMoneyAnimDict);
                RequestAnimDict(Constants.LiftAnimDict);
                RequestModel(Constants.BeerBoxHash);
                RequestModel((uint)PedHash.Prisguard01SMM);
                RequestModel(Constants.GirlModelHash);
                RequestAnimDict(Constants.BedAnimDict);
            }

            if(CurrentStage >= SceneStage.BitchSlap && CurrentStage <= SceneStage.DriveHighway)
            {
                RequestCollisionAtCoord(Constants.HighwayStartX, Constants.HighwayStartY, 30f);
            }

            // need to spawn car2 eventually
            // only huntedj should do this to make sure they own the car
            // this then needs to be synced with huntedl over a server event
            if(CurrentState.LocalPlayerType == PlayerType.HuntedJ)
            {
                if(CurrentState.Car2 == 0)
                {
                    RequestModel((uint)_carHash2);
                    if(HasModelLoaded((uint)_carHash2))
                    {
                        CurrentState.Car2 = CreateVehicle((uint)_carHash2, Constants.Car2X, Constants.Car2Y, Constants.Car2Z, Constants.Car2Heading, true, true);
                        FreezeEntityPosition(CurrentState.Car2, true);
                        TriggerServerEventProxy(SurviveTheHuntShared.Events.Server.NotifyNetEntity, NetworkGetNetworkIdFromEntity(CurrentState.Car2), TulipNetEntName);
                    }
                }
            }

            RenderScriptCams(CurrentStage < s_LastStage, CurrentStage == s_LastStage, 1500, true, false);
        }

        public sealed override bool CanShowHud => CurrentStage >= s_LastStage && CurrentState.CurrentStageTime > 4.5;
    }
}
