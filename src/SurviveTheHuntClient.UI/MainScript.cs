using CitizenFX.Core;
using SurviveTheHuntShared.Utils;
using System;
using System.Reflection;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;
using LemonUI;
using LemonUI.Menus;
using SurviveTheHuntShared;
using System.Collections.Generic;

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
        private NativeListItem<string> SelectPlayerItem;
        private NativeItem StartHuntButton;

        private NativeMenu PlayerMenu;
        private NativeSubmenuItem PlayerMenuItem;
        private NativeItem HealButton;
        private NativeMenu RespawnMenu;
        private NativeSubmenuItem RespawnMenuItem;
        private NativeItem RespawnConfirmButton;

        private const string DefaultSelectPlayerDescription = "Choose which player to hunt, or let the server pick a random player.";
        private List<int> SelectablePlayerHandles = new List<int>();

        private bool Shown = false;

        private bool HoldingInteractionMenuPadButton = false;
        private float TimeHoldingInteractionMenu = 0f;

        private const string ShowMenuEventName = "sth:client:ui:showMenu";

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

        private void InitUI()
        {
            ObjectPool = new ObjectPool();

            MainMenu = new NativeMenu("Survive the Hunt", "Main menu");
            PlayerMenu = new NativeMenu("Player Options", "Player Options", "Restore your health, respawn etc.");
            RespawnMenu = new NativeMenu("Are you sure?", "Respawn", "Respawn immediately. Keep in mind you will lose the round if you are the hunted player.");
            StartHuntMenu = new NativeMenu("Confirm settings", "Start hunt", "Select the player to be hunted and start a match.");

            StartHuntMenuItem = new NativeSubmenuItem(StartHuntMenu, MainMenu);

            // This creates SelectPlayerItem
            UpdateSelectablePlayers();
            StartHuntButton = new NativeItem("Start", "Start a new round of Survive the Hunt with the selected settings.");
            StartHuntMenu.Add(SelectPlayerItem);
            StartHuntMenu.Add(StartHuntButton);

            ObjectPool.Add(MainMenu);
            ObjectPool.Add(PlayerMenu);
            ObjectPool.Add(RespawnMenu);
            ObjectPool.Add(StartHuntMenu);

            CharacterButton = new NativeItem("Appearance", "Change your character's appearance.");
            SpawnCarsButton = new NativeItem("Spawn cars", "Request a fresh batch of rides.");
            PlayerMenuItem = new NativeSubmenuItem(PlayerMenu, MainMenu);
            AboutButton = new NativeItem("About", $"Survive the Hunt v{typeof(MainScript).Assembly.GetName().Version}\n\nBased on FailRace's YouTube videos. Developed by Tomeztos (tomezpl).\n\nSpecial thanks for QA:\n- happygrowls\n- rollschuh2282\n- SpiderVice");

            MainMenu.Add(StartHuntMenuItem);
            MainMenu.Add(CharacterButton);
            MainMenu.Add(SpawnCarsButton);
            MainMenu.Add(PlayerMenuItem);

            HealButton = new NativeItem("Heal", "Restore your health immediately. You cannot heal during an active hunt.");
            RespawnMenuItem = new NativeSubmenuItem(RespawnMenu, PlayerMenu);
            RespawnConfirmButton = new NativeItem("Confirm respawn", "Kill your character and respawn at Terminal.");
            PlayerMenu.Add(HealButton);
            PlayerMenu.Add(RespawnMenuItem);
            RespawnMenu.Add(RespawnConfirmButton);
            RespawnConfirmButton.Activated += RespawnConfirmed;
            HealButton.Activated += HealButtonClicked;

            StartHuntButton.Activated += StartHuntClicked;
            CharacterButton.Activated += CharacterMenuClicked;
            SpawnCarsButton.Activated += SpawnCarsClicked;

            MainMenu.Closing += MainMenuClosing;

            MainMenu.Add(AboutButton);

            SelectPlayerItem.ItemChanged += SelectedPlayerChanged;
            StartHuntMenuItem.Activated += OpenedStartHuntMenu;
        }

        private void OpenedStartHuntMenu(object sender, EventArgs e)
        {
            UpdateSelectablePlayers();
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
            if (SelectPlayerItem.SelectedIndex != 0)
            {
                TriggerServerEvent(Events.Server.RequestStartHunt, GetPlayerServerId(SelectablePlayerHandles[SelectPlayerItem.SelectedIndex]));
            }
            else
            {
                TriggerServerEvent(Events.Server.RequestStartHunt);
            }
            MainMenu.Visible = false;
            StartHuntMenu.Visible = false;
        }

        private void SpawnCarsClicked(object sender, EventArgs e)
        {
            MainMenu.Visible = false;
            TriggerEvent(Events.Client.SpawnCars);
        }

        [EventHandler("sth:client:ui:toggleMenu")]
        private void ToggleUI()
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
        }

        [EventHandler("sth:client:ui:closeMenu")]
        private void CloseMenu()
        {
            Shown = false;
            ObjectPool.HideAll();
        }

        public async Task Update()
        {
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
        }
    }
}
