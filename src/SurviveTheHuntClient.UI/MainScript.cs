using CitizenFX.Core;
using SurviveTheHuntShared.Utils;
using System;
using System.Reflection;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;
using LemonUI;
using LemonUI.Menus;

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

        private bool Shown = false;

        public MainScript()
        {

        }

        [EventHandler("onResourceStart")]
        public void OnResourceStarted(string resourceName)
        {
            if(resourceName == "sth-ui")
            {
                Debug.WriteLine("HI THIS IS UI!!!!");
            }

            InitUI();

            RegisterCommand("_sthmenukeybind", new Action(MenuKeybindAction), false);

            Tick += Update;
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

            ObjectPool.Add(MainMenu);

            StartHuntButton = new NativeItem("Start", "Start a new round of Survive the Hunt.");
            CharacterButton = new NativeItem("Appearance", "Change your character's appearance.");
            SpawnCarsButton = new NativeItem("Spawn cars", "Request a fresh batch of rides.");
            AboutButton = new NativeItem("About", $"Survive the Hunt v{typeof(MainScript).Assembly.GetName().Version}\n\nBased on FailRace's YouTube videos. Developed by Tomeztos (tomezpl).\nSpecial thanks for QA:\n- happygrowls\n- rollschuh2282\n- SpiderVice");


            MainMenu.Add(StartHuntButton);
            MainMenu.Add(CharacterButton);
            MainMenu.Add(SpawnCarsButton);
            MainMenu.Add(AboutButton);
        }

        private void ToggleUI()
        {
            Shown = !Shown;

            MainMenu.Visible = Shown;
        }

        public async Task Update()
        {
            ObjectPool.Process();
        }
    }
}
