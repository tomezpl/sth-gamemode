using CitizenFX.Core;
using System;
using System.Reflection;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;
using LemonUI;
using LemonUI.Menus;
using SurviveTheHuntShared;
using System.Collections.Generic;
using LemonUI.Elements;
using LemonUI.Tools;
using SurviveTheHuntClient.Models;
using SurviveTheHuntShared.Models.UI;

namespace SurviveTheHuntClient.UI
{
    public class MainScript : ClientScript
    {
        private ObjectPool ObjectPool;

        private NativeMenu MainMenu;

        private NativeSubmenuItem StartHuntMenuItem;
        private NativeItem CharacterButton;
        private NativeItem SpawnCarsButton;
        private NativeItem AboutButton;

        private NativeMenu StartHuntMenu;
        private NativeListItem<string> SelectModeItem;
        private Dictionary<string, string> ModeDescriptions = new Dictionary<string, string>();
        private List<string> GameModeNames = new List<string> { "" };
        private List<string> GameModeTitles = new List<string> { "Default" };
        private NativeListItem<string> SelectPlayerItem;
        private NativeItem StartHuntButton;

        private NativeMenu PlayerMenu;
        private NativeSubmenuItem PlayerMenuItem;
        private NativeItem HealButton;
        private NativeMenu RespawnMenu;
        private NativeSubmenuItem RespawnMenuItem;
        private NativeItem RespawnConfirmButton;
        private NativeItem TeleportToStartingAreaButton;

        private NativeMenu VehicleOptionsMenu;
        private NativeSubmenuItem VehicleOptionsMenuItem;
        private NativeItem VehicleEngineOffButton;

        private NativeMenu HelpMenu;
        private NativeSubmenuItem HelpMenuItem;
        private NativeItem HelpTextGeneral;
        private NativeItem HelpTextBounds;
        private NativeItem HelpTextBuses;
        private NativeItem HelpTextBlendingIn;

        private const string DefaultSelectPlayerDescription = "Choose which player to hunt, or let the server pick a random player.";
        private List<int> SelectablePlayerHandles = new List<int>();

        private bool Shown = false;

        private bool HoldingInteractionMenuPadButton = false;
        private float TimeHoldingInteractionMenu = 0f;

        private const string ShowMenuEventName = "sth:client:ui:showMenu";

        private bool IsVehicleMenuPresent = false;

        private const string MenuTxdName = "sthUiMenuTxd";
        private long MenuTxdHandle = 0;

        // Only spawn cars if the user confirmed it
        private const int SpawnCarsActivationThreshold = 2;
        private int SpawnCarsActivatedCount = 0;
        private const string SpawnCarsDescription = "Request a fresh batch of rides.";

        private static class Textures
        {
            internal struct Texture
            {
                internal const string Txd = MenuTxdName;
                internal string Txn;
                internal string Filename;
                internal long Handle;

                internal static Texture Create(string filename, string txn = null)
                {
                    var ret = new Texture();

                    ret.Txn = txn;
                    ret.Filename = filename;

                    return ret;
                }
            }

            internal static Texture BusTexture = Texture.Create("files/bus.png", "busTexture");
            internal static Texture AppearanceTexture = Texture.Create("files/appearance.png", "appearanceTexture");
            internal static Texture BoundsTexture = Texture.Create("files/lslimits.png", "boundsTexture");

            internal static readonly Texture[] AllTextures =
            {
                BusTexture,
                AppearanceTexture,
                BoundsTexture,
                Texture.Create("files/cpd_contact.png", "cupid_contact_esther"),
                Texture.Create("files/cpd_contact2.png", "cupid_contact_police")
            };

            private static Dictionary<string, Texture> CreateTextureMap(Texture[] textures = null)
            {
                if(textures == null)
                {
                    textures = AllTextures;
                }

                Dictionary<string, Texture> map = new Dictionary<string, Texture>();

                foreach (Texture texture in textures)
                {
                    map.Add(texture.Txn, texture);
                }

                return map;
            }

            internal static readonly Dictionary<string, Texture> TextureMap = CreateTextureMap(AllTextures);
        }

        private List<BlockedItem> BlockedItems = new List<BlockedItem>();

        public MainScript()
        {
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientGameTypeStart);
        }

        public void OnClientGameTypeStart(string resourceName)
        {
            if(resourceName == GetCurrentResourceName())
            {
                Debug.WriteLine($"[sth-ui]: Client resource started");

                InitUI();

                Tick += Update;
                BlockableItemStateChanged += OnBlockableItemStateChanged;
            }
        }


        private Dictionary<NativeItem, bool> WasItemVisibleBeforeBlock = new Dictionary<NativeItem, bool>();
        private void OnBlockableItemStateChanged(NativeItem item, bool isBlocked)
        {
            if(isBlocked)
            {
                WasItemVisibleBeforeBlock[item] = item.Enabled;
            }

            item.Enabled = !isBlocked;
        }

        [EventHandler(SurviveTheHuntShared.Events.Client.UISendText)]
        public void OnGamemodeSentText(string sender, string subject, string message, float duration, string textureName)
        {
            string txd = null, txn = null;

            if(Textures.TextureMap.TryGetValue(textureName, out Textures.Texture texture))
            {
                txd = MenuTxdName;
                txn = texture.Txn;

                Debug.WriteLine($"Found texture {texture.Filename} for {textureName}");
            }
            else
            {
                Debug.WriteLine($"Could not find texture {textureName} in any of the {Textures.TextureMap.Count} textuers");
            }

            BeginTextCommandThefeedPost("STRING");
            AddTextComponentSubstringPlayerName(message);
            EndTextCommandThefeedPostMessagetextTu(txd, txn, true, 0, sender, subject, duration / 15f);
            PlaySoundFrontend(-1, "Phone_Text_Arrive", "DLC_H4_MM_Sounds", true);
        }

        private delegate void BlockableItemStateChangedEvent(NativeItem item, bool isBlocked);

        private event BlockableItemStateChangedEvent BlockableItemStateChanged;

        [EventHandler(SurviveTheHuntShared.Events.Client.UISetItemBlocked)]
        public void HandleSetItemBlocked(BlockedItem item, bool blocked)
        {
            bool changed = false;
            if(!blocked)
            {
                int index = BlockedItems.IndexOf(item);
                if(index != -1)
                {
                    changed = true;
                    BlockedItems.RemoveAt(index);
                }
            }
            else
            {
                if (!BlockedItems.Contains(item))
                {
                    changed = true;
                    BlockedItems.Add(item);
                }
            }

            if(changed)
            {
                NativeItem uiItem = GetBlockableItem(item);
                if (uiItem != null)
                {
                    BlockableItemStateChanged.Invoke(uiItem, blocked);
                }
            }
        }

        private NativeItem GetBlockableItem(BlockedItem item)
        {
            switch(item)
            {
                case BlockedItem.Appearance:
                    return CharacterButton;
                default:
                    return null;
            }
        }

        private void UpdateSelectablePlayers()
        {
            SelectablePlayerHandles.Clear();
            bool menuItemExists = SelectPlayerItem != null;
            if (menuItemExists)
            {
                SelectPlayerItem.Items.Clear();
            }

            List<string> players = new List<string>(GetNumberOfPlayers() + 1);
            players.Add(GetLabelText("FMMC_VEH_RAND"));
            SelectablePlayerHandles.Add(-1);
            foreach (Player player in Players)
            {
                players.Add(player.Handle == Game.Player.Handle ? $"{player.Name} (you)" : player.Name);
                SelectablePlayerHandles.Add(player.Handle);
            }

            if(!menuItemExists)
            {
                SelectPlayerItem = new NativeListItem<string>("Hunted Player", DefaultSelectPlayerDescription, players.ToArray());
            }
            else
            {
                SelectPlayerItem.Items = players;
                SelectPlayerItem.SelectedIndex = 0;
            }
        }

        private void PrepareTextures()
        {
            MenuTxdHandle = CreateRuntimeTxd(MenuTxdName);
            for(int i = 0; i < Textures.AllTextures.Length; i++)
            {
                Textures.Texture texture = Textures.AllTextures[i];
                texture.Handle = CreateRuntimeTextureFromImage(MenuTxdHandle, texture.Txn, texture.Filename);
            }
        }

        private NativeListItem<string> CreateSelectModeItem()
        {
            return new NativeListItem<string>("Game mode", GameModeTitles.ToArray());
        }

        private void InitUI()
        {
            PrepareTextures();

            ObjectPool = new ObjectPool();

            MainMenu = new NativeMenu("Survive the Hunt", "Main menu");
            PlayerMenu = new NativeMenu("Player Options", "Player Options", "Restore your health, respawn etc.");
            RespawnMenu = new NativeMenu("Are you sure?", "Respawn", "Respawn immediately. Keep in mind you will lose the round if you are the hunted player.");
            StartHuntMenu = new NativeMenu("Confirm settings", "Start hunt", "Select the player to be hunted and start a match.");
            VehicleOptionsMenu = new NativeMenu("Vehicle Options", "Vehicle Options", "Perform vehicle-related actions to help with blending in.");
            HelpMenu = new NativeMenu("Help & Tips", "Help & Tips", "Need help? Check this menu for a general introduction and some useful tips!");

            StartHuntMenuItem = new NativeSubmenuItem(StartHuntMenu, MainMenu);

            // This creates SelectPlayerItem
            UpdateSelectablePlayers();
            StartHuntButton = new NativeItem("Start", "Start a new round of Survive the Hunt with the selected settings.");
            SelectModeItem = CreateSelectModeItem();
            StartHuntMenu.Add(SelectModeItem);
            StartHuntMenu.Add(SelectPlayerItem);
            StartHuntMenu.Add(StartHuntButton);

            ObjectPool.Add(MainMenu);
            ObjectPool.Add(PlayerMenu);
            ObjectPool.Add(RespawnMenu);
            ObjectPool.Add(StartHuntMenu);
            ObjectPool.Add(VehicleOptionsMenu);
            ObjectPool.Add(HelpMenu);

            CharacterButton = new NativeItem("Appearance", "Change your character's appearance.");
            SpawnCarsButton = new NativeItem("Spawn cars", SpawnCarsDescription);
            PlayerMenuItem = new NativeSubmenuItem(PlayerMenu, MainMenu);
            AboutButton = new NativeItem("About", $"Survive the Hunt v{typeof(MainScript).Assembly.GetName().Version}\n\nBased on FailRace's YouTube videos. Developed by Tomeztos (tomezpl).\n\nSpecial thanks for QA:\n- happygrowls\n- rollschuh2282\n- SpiderVice");

            MainMenu.Add(StartHuntMenuItem);
            MainMenu.Add(CharacterButton);
            MainMenu.Add(SpawnCarsButton);
            MainMenu.Add(PlayerMenuItem);

            VehicleOptionsMenuItem = new NativeSubmenuItem(VehicleOptionsMenu, MainMenu);
            MainMenu.Add(VehicleOptionsMenuItem);
            IsVehicleMenuPresent = true;

            HealButton = new NativeItem("Heal", "Restore your health immediately. You cannot heal during an active hunt.");
            RespawnMenuItem = new NativeSubmenuItem(RespawnMenu, PlayerMenu);
            RespawnConfirmButton = new NativeItem("Confirm respawn", "Kill your character and respawn at Terminal.");
            PlayerMenu.Add(HealButton);
            PlayerMenu.Add(RespawnMenuItem);
            PlayerMenu.Shown += PlayerMenu_Shown;
            RespawnMenu.Add(RespawnConfirmButton);
            RespawnConfirmButton.Activated += RespawnConfirmed;
            HealButton.Activated += HealButtonClicked;
            TeleportToStartingAreaButton = new NativeItem("Return to Starting Area", "Immediately return to the starting area at Terminal, LS.");
            TeleportToStartingAreaButton.Activated += TeleportToSpawnClicked;
            PlayerMenu.Add(TeleportToStartingAreaButton);

            StartHuntButton.Activated += StartHuntClicked;
            CharacterButton.Activated += CharacterMenuClicked;
            SpawnCarsButton.Activated += SpawnCarsClicked;

            MainMenu.Closing += MainMenuClosing;

            HelpMenuItem = new NativeSubmenuItem(HelpMenu, MainMenu);
            MainMenu.Add(HelpMenuItem);

            MainMenu.Add(AboutButton);

            SelectPlayerItem.ItemChanged += SelectedPlayerChanged;
            SelectModeItem.ItemChanged += SelectedModeChanged;
            StartHuntMenuItem.Activated += OpenedStartHuntMenu;

            VehicleEngineOffButton = new NativeItem("Turn engine off", "Turn the ignition off. It will automatically turn back on if you apply the accelerator.");
            VehicleEngineOffButton.Activated += TurnVehicleEngineOff;
            VehicleOptionsMenu.Add(VehicleEngineOffButton);

            HelpTextGeneral = new NativeItem("General", "24 minutes in LS. You, your ride, and a bunch of psychos scouring the city just to put a bullet in you. Hunters can't see you on the radar, but your approximate area is broadcast once every minute. You won't be gunned down in a driveby, but if you see them step out of their cars, it's over. Hide, blend in, or pray that your 0 to 60 shakes them off.");
            HelpMenu.Add(HelpTextGeneral);
            HelpTextBounds = new NativeItem("Play Area", "The gamemode is limited to Los Santos.\n\nWhen you approach the outer bounds of north LS, you will be warned to return to the play area. You're advised not to leave the play area, as it will reveal your position on the radar to all players until you're back in the city.");
            HelpMenu.Add(HelpTextBounds);
            HelpTextBuses = new NativeItem("Passengers", "No wheels? Hunters swooping in?\nCatch a bus!\nKeep holding the enter button, and you'll politely sit down instead of treating the public transit wage-slave to a fisty smooch.\n\nYou can also enter cars as a passenger, provided they're empty - LS citizens are wary of hitchikers.");
            HelpMenu.Add(HelpTextBuses);
            HelpTextBlendingIn = new NativeItem("Blending in", "Disguising yourself as an ordinary LS citizen is your best chance for survival.\n\nThrow some boring clothes on using the Appearance menu, and make sure to turn off the engine using the Vehicle Options when parked.");
            HelpMenu.Add(HelpTextBlendingIn);
            HelpMenu.Shown += HelpMenu_Shown;
            HelpMenu.Closed += HelpMenu_Closed;

            MainMenu.SelectedIndexChanged += MainMenu_SelectedIndexChanged;

            BlockedItems = new List<BlockedItem>(MainMenu.Items.Count);
        }

        private void SelectedModeChanged(object sender, ItemChangedEventArgs<string> e)
        {
            string mode = GameModeNames[e.Index];
            if(ModeDescriptions.TryGetValue(mode, out string description))
            {
                StartHuntMenu.Description = description;
            }
            else
            {
                StartHuntMenu.Description = "";
            }
        }

        private void TeleportToSpawnClicked(object sender, EventArgs e)
        {
            TriggerEvent(Events.Client.TeleportToSpawn);
            PlayerMenu.Visible = false;
        }

        private void MainMenu_SelectedIndexChanged(object sender, SelectedEventArgs e)
        {
            SpawnCarsActivatedCount = 0;
            SpawnCarsButton.Description = SpawnCarsDescription;
        }

        private void PlayerMenu_Shown(object sender, EventArgs e)
        {
            PlayerMenu.SelectedIndex = 0;
            PlayerMenu.ResetCursor();
            PlayerMenu.Process();
        }

        private void HelpMenu_Closed(object sender, EventArgs e)
        {
            ShowHelpImages = false;
        }

        private bool ShowHelpImages = false;

        private void HelpMenu_Shown(object sender, EventArgs e)
        {
            ShowHelpImages = true;

            HelpMenu.SelectedIndex = 0;
            HelpMenu.ResetCursor();
            HelpMenu.Process();
        }

        private void TurnVehicleEngineOff(object sender, EventArgs e)
        {
            if(IsPedInAnyVehicle(PlayerPedId(), true))
            {
                int vehicle = GetVehiclePedIsIn(PlayerPedId(), false);
                if(DoesEntityExist(vehicle) && IsEntityAVehicle(vehicle))
                {
                    SetVehicleEngineOn(vehicle, false, false, true);
                }
            }
        }

        private void OpenedStartHuntMenu(object sender, EventArgs e)
        {
            UpdateSelectablePlayers();

            StartHuntMenu.SelectedIndex = 0;
            StartHuntMenu.ResetCursor();
            StartHuntMenu.Process();
        }

        private void SelectedPlayerChanged(object sender, ItemChangedEventArgs<string> e)
        {
            if(SelectablePlayerHandles[SelectPlayerItem.SelectedIndex] == Game.Player.Handle)
            {
                SelectPlayerItem.Description = "Choose yourself as the next hunted player.";
            }
            else
            {
                SelectPlayerItem.Description = DefaultSelectPlayerDescription;
            }
        }

        private void MainMenuClosing(object sender, CancelEventArgs e)
        {
            Shown = false;
            SpawnCarsActivatedCount = 0;
        }

        private void HealButtonClicked(object sender, EventArgs e)
        {
            TriggerEvent(Events.Client.Heal);
            PlayerMenu.Visible = false;
        }

        private void RespawnConfirmed(object sender, EventArgs e)
        {
            RespawnMenu.Visible = false;
            TriggerEvent(Events.Client.Respawn);
        }

        private void CharacterMenuClicked(object sender, EventArgs e)
        {
            MainMenu.Visible = false;
            TriggerEvent("lbg-openChar", ShowMenuEventName);
        }

        private void StartHuntClicked(object sender, EventArgs e)
        {
            string modeName = GameModeNames[SelectModeItem.SelectedIndex];

            if (SelectPlayerItem.SelectedIndex != 0)
            {
                TriggerServerEvent(Events.Server.RequestStartHunt, new { Player = GetPlayerServerId(SelectablePlayerHandles[SelectPlayerItem.SelectedIndex]), Mode = modeName });
            }
            else
            {
                TriggerServerEvent(Events.Server.RequestStartHunt, new { Mode = modeName } );
            }
            MainMenu.Visible = false;
            StartHuntMenu.Visible = false;
        }

        private void SpawnCarsClicked(object sender, EventArgs e)
        {
            if(SpawnCarsActivatedCount < SpawnCarsActivationThreshold)
            {
                SpawnCarsActivatedCount++;
                SpawnCarsButton.Description = "Are you sure? This will remove ALL previously spawned vehicles that aren't currently being driven.";
            }

            if (SpawnCarsActivatedCount >= SpawnCarsActivationThreshold)
            {
                MainMenu.Visible = false;
                TriggerEvent(Events.Client.SpawnCars);
                SpawnCarsActivatedCount = 0;
                SpawnCarsButton.Description = SpawnCarsDescription;
            }
        }

        [EventHandler("sth:client:ui:toggleMenu")]
        public void ToggleUI()
        {
            Shown = !Shown;

            MainMenu.Visible = Shown;
        }

        [EventHandler(ShowMenuEventName)]
        private void ShowMenu()
        {
            Shown = true;
            ObjectPool.HideAll();
            MainMenu.Visible = true;

            MainMenu.SelectedIndex = 0;
            MainMenu.ResetCursor();
            MainMenu.Process();
        }

        [EventHandler("sth:client:ui:closeMenu")]
        public void CloseMenu()
        {
            Shown = false;
            ObjectPool.HideAll();
        }

        [EventHandler(Events.Client.UIRecvGameModes)]
        public void ReceiveExtraGameModes(List<object> info)
        {
            Debug.WriteLine($"{nameof(ReceiveExtraGameModes)}, {info.GetType()}");

            // Remove all but the default mode
            if(GameModeNames.Count > 1)
            {
                GameModeNames.RemoveRange(1, GameModeNames.Count - 1);
                GameModeTitles.RemoveRange(1, GameModeTitles.Count - 1);
            }

            Debug.WriteLine($"info count: {info.Count}");
            foreach (string gameModeSerialized in info)
            {
                string gameModeName = gameModeSerialized.Substring(0, gameModeSerialized.IndexOf('\n'));
                string titleAndDesc = gameModeSerialized.Substring(gameModeName.Length + 1);
                int newlineIndex = titleAndDesc.IndexOf('\n');
                string gameModeTitle = newlineIndex == -1 ? titleAndDesc : titleAndDesc.Substring(0, newlineIndex);
                string gameModeDescription = newlineIndex == -1 ? "" : gameModeSerialized.Substring(newlineIndex + 1);

                GameModeNames.Add(gameModeName);
                GameModeTitles.Add(gameModeTitle);
                ModeDescriptions[gameModeName] = gameModeDescription;
            }

            if (SelectModeItem != null)
            {
                SelectModeItem.Items = GameModeTitles;
            }
        }

        private void DrawHelpImages()
        {
            if (ShowHelpImages)
            {
                float menuWidth = Extensions.ToXRelative(HelpMenu.Width);
                float safeZone = 1f - GetSafeZoneSize();
                float originX = HelpMenu.Offset.X + (0.5f * safeZone);
                float origin = Extensions.ToYRelative((37.4f * HelpMenu.Items.Count));

                // TODO: I really can't be bothered with this and LemonUI hiding the Description rect data behind a private field isn't helping.
                // This offset looks good enough so we'll roll with it, as long as the text isn't too long
                origin += 0.285f;

                Textures.Texture? texture = null;

                switch (HelpMenu.SelectedIndex)
                {
                    case 0:
                        origin += 0.05f;
                        break;
                    case 1:
                        origin += 0.15f;
                        texture = Textures.BoundsTexture;
                        break;
                    case 2:
                        origin += 0.175f;
                        texture = Textures.BusTexture;
                        break;
                    case 3:
                        origin += 0.125f;
                        texture = Textures.AppearanceTexture;
                        break;
                    default:
                        origin += 0f;
                        texture = Textures.AppearanceTexture;
                        break;
                }

                if (texture.HasValue)
                {
                    origin += 0.015f;
                    DrawSprite(MenuTxdName, texture.Value.Txn, originX + (menuWidth * .5f), origin + (0.5f * safeZone), menuWidth, 0.19f, 0f, 255, 255, 255, 255);
                }
            }
        }

        public async Task Update()
        {
            DrawHelpImages();

            const float secondsToHold = 0.25f;
            if(HoldingInteractionMenuPadButton && TimeHoldingInteractionMenu >= secondsToHold && !ObjectPool.AreAnyVisible)
            {
                ShowMenu();
                TimeHoldingInteractionMenu = 0f;
                HoldingInteractionMenuPadButton = false;
                EnableControlAction(0, 0, true);
            }

            bool keyboard = IsUsingKeyboard(0);
            if (!keyboard && IsControlPressed(0, 244))
            {
                HoldingInteractionMenuPadButton = true;
                TimeHoldingInteractionMenu += GetFrameTime();
                DisableControlAction(0, 0, true);
            } else if (IsControlJustReleased(0, 244))
            {
                // Don't need to hold the button on keyboard
                if (keyboard)
                {
                    if (!ObjectPool.AreAnyVisible)
                    {
                        ShowMenu();
                    }
                }
                else
                {
                    EnableControlAction(0, 0, true);
                    TimeHoldingInteractionMenu = 0f;
                    HoldingInteractionMenuPadButton = false;
                }
            }

            if (ObjectPool.AreAnyVisible)
            {
                // Disable sprint on controller if the menu is open
                DisableControlAction(0, 21, true);
            } else
            {
                EnableControlAction(0, 21, true);
            }
            ObjectPool.Process();

            // Remove the vehicle options menu if the player is not in a vehicle, and re-add it when they enter one.
            if (IsPedInAnyVehicle(PlayerPedId(), true))
            {
                if (!IsVehicleMenuPresent)
                {
                    int idx = MainMenu.Items.IndexOf(HelpMenuItem);
                    MainMenu.Items.Insert(idx, VehicleOptionsMenuItem);
                    MainMenu.Recalculate();
                    MainMenu.Process();
                    if (MainMenu.Visible & MainMenu.SelectedIndex == MainMenu.Items.IndexOf(HelpMenuItem) - 1)
                    {
                        MainMenu.ResetCursor();
                        MainMenu.Visible = false;
                        MainMenu.Process();
                        MainMenu.SelectedIndex = MainMenu.Items.IndexOf(VehicleOptionsMenuItem);
                        MainMenu.Visible = true;
                    }
                    IsVehicleMenuPresent = true;
                }
            }
            else
            {
                if (IsVehicleMenuPresent)
                {
                    MainMenu.Remove(VehicleOptionsMenuItem);
                    IsVehicleMenuPresent = false;
                }
            }

        }
    }
}
