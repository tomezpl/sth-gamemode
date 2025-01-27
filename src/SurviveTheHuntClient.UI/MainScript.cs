using CitizenFX.Core;
using SurviveTheHuntShared.Utils;
using System;
using System.Reflection;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;
using LemonUI;
using LemonUI.Menus;
using SurviveTheHuntShared;

namespace SurviveTheHuntClient.UI
{
    public class MainScript : ClientScript
    {
        private ObjectPool ObjectPool;

        private NativeMenu MainMenu;

        private NativeItem StartHuntButton;
        private NativeItem CharacterButton;
        private NativeItem SpawnCarsButton;
        private NativeItem AboutButton;
        
        private NativeMenu PlayerMenu;
        private NativeSubmenuItem PlayerMenuItem;
        private NativeItem HealButton;
        private NativeMenu RespawnMenu;
        private NativeSubmenuItem RespawnMenuItem;
        private NativeItem RespawnConfirmButton;

        private bool Shown = false;

        private bool HoldingInteractionMenuPadButton = false;
        private float TimeHoldingInteractionMenu = 0f;

        public MainScript()
        {

        }

        [EventHandler("onClientResourceStart")]
        public void OnClientResourceStarted(string resourceName)
        {
            if(resourceName == GetCurrentResourceName())
            {
                Debug.WriteLine($"[sth-ui]: Client resource started");

                InitUI();

                RegisterCommand("_sthmenukeybind", new Action(MenuKeybindAction), false);

                Tick += Update;
            }
        }

        private void MenuKeybindAction()
        {
            if (ConvarHelper.GetBoolean(GetConvar("sth_registerMenuKeybind", "true")))
            {
                ToggleUI();
            }
        }

        private void InitUI()
        {
            ObjectPool = new ObjectPool();

            MainMenu = new NativeMenu("Survive the Hunt", "Main menu");
            PlayerMenu = new NativeMenu("Player Options", "Player Options", "Restore your health, respawn etc.");
            RespawnMenu = new NativeMenu("Are you sure?", "Respawn", "Respawn immediately. Keep in mind you will lose the round if you are the hunted player.");


            ObjectPool.Add(MainMenu);
            ObjectPool.Add(PlayerMenu);
            ObjectPool.Add(RespawnMenu);

            StartHuntButton = new NativeItem("Start", "Start a new round of Survive the Hunt.");
            CharacterButton = new NativeItem("Appearance", "Change your character's appearance.");
            SpawnCarsButton = new NativeItem("Spawn cars", "Request a fresh batch of rides.");
            PlayerMenuItem = new NativeSubmenuItem(PlayerMenu, MainMenu);
            AboutButton = new NativeItem("About", $"Survive the Hunt v{typeof(MainScript).Assembly.GetName().Version}\n\nBased on FailRace's YouTube videos. Developed by Tomeztos (tomezpl).\n\nSpecial thanks for QA:\n- happygrowls\n- rollschuh2282\n- SpiderVice");

            MainMenu.Add(StartHuntButton);
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
        }

        private void MainMenuClosing(object sender, CancelEventArgs e)
        {
            Shown = false;
        }

        private void HealButtonClicked(object sender, EventArgs e)
        {
            TriggerEvent(Events.Client.Heal);
        }

        private void RespawnConfirmed(object sender, EventArgs e)
        {
            RespawnMenu.Visible = false;
            TriggerEvent(Events.Client.Respawn);
        }

        private void CharacterMenuClicked(object sender, EventArgs e)
        {
            MainMenu.Visible = false;
            TriggerEvent("lbg-openChar");
        }

        private void StartHuntClicked(object sender, EventArgs e)
        {
            TriggerServerEvent(Events.Server.RequestStartHunt);
            MainMenu.Visible = false;
        }

        private void SpawnCarsClicked(object sender, EventArgs e)
        {
            MainMenu.Visible = false;
            TriggerEvent(Events.Client.SpawnCars);
        }

        private void ToggleUI()
        {
            Shown = !Shown;

            MainMenu.Visible = Shown;
        }

        public async Task Update()
        {
            if(HoldingInteractionMenuPadButton && TimeHoldingInteractionMenu >= 0.25f)
            {
                ToggleUI();
                TimeHoldingInteractionMenu = 0f;
                HoldingInteractionMenuPadButton = false;
                EnableControlAction(0, 0, true);
            }

            if(IsControlPressed(0, 244))
            {
                HoldingInteractionMenuPadButton = true;
                TimeHoldingInteractionMenu += GetFrameTime();
                DisableControlAction(0, 0, true);
            } else if (IsControlJustReleased(0, 244))
            {
                EnableControlAction(0, 0, true);
                TimeHoldingInteractionMenu = 0f;
                HoldingInteractionMenuPadButton = false;
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
