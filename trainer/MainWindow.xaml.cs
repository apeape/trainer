using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Un4seen.BassMOD;
using System.Windows.Media.Animation;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Interop;
using WindowsInput;

namespace trainer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Dictionary<string, TrainerOption> Options = new Dictionary<string, TrainerOption>();
        //List<TrainerOption> Options = new List<TrainerOption>();
        const string GameProcessName = "client";
        const string GameWindowCaption = "Ace of Spades";
        Process GameProcess;
        ProcessMemory GameMemory = new ProcessMemory();
        SigScan GameMemoryScanner;
        bool GameProcessFound = false;
        bool TrainerExiting = false;
        uint GameBaseAddress;
        Thread FindGameWindow;
        MouseSimulator Mouse = new MouseSimulator();
        const int MemorySearchRegionSize = 0x40000;

        // memory offsets to store our new values for misc stuff at
        const int SpeedHackOffset = 0x1030;
        const int JumpHeightOffset = SpeedHackOffset + 0x10;
        const int JumpSteeringOffset = JumpHeightOffset + 0x10;
        const int NadeRangeOffset = JumpSteeringOffset + 0x10;
        const int ToolRangeMinOffset = NadeRangeOffset + 0x10;
        const int ToolRangeMaxOffset = ToolRangeMinOffset + 0x10;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // start trainer thread
            FindGameWindow = new Thread(new ThreadStart(TrainerLoop));
            FindGameWindow.Name = "Find Game Window";
            FindGameWindow.Start();

            // check for game window once before proceeding
            GameProcess = LocateGameWindow();
            if (GameProcess == null)
            {
                // play chiptune
                BassMOD.BASSMOD_Init(-1, 441000, BASSInit.BASS_DEVICE_DEFAULT);
                BassMOD.BASSMOD_MusicLoad(chiptune.song, 0, chiptune.song.Length,
                    BASSMusic.BASS_MUSIC_NONINTER);
                BassMOD.BASSMOD_MusicPlayEx(0, BASSMusic.BASS_MUSIC_LOOP, false);

                // start silly wpf animations
                ((Storyboard)this.Resources["LogoZoom"]).Begin();
                ((Storyboard)this.Resources["LogoRotate"]).Begin();
                ((Storyboard)this.Resources["BackgroundRotate"]).Begin();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TrainerExiting = true;

            // stop bassmod
            BassMOD.BASSMOD_MusicStop();
            BassMOD.BASSMOD_Free();
            
            if (FindGameWindow.ThreadState == System.Threading.ThreadState.Running)
                FindGameWindow.Abort();
        }

        private Process LocateGameWindow()
        {
            return Process.GetProcessesByName(GameProcessName)
                .SingleOrDefault(proc => proc.MainWindowTitle == GameWindowCaption);
        }

        /// <summary>
        /// This thread waits for the game window, then stops music/animation and enables the trainer hotkeys
        /// </summary>
        private void TrainerLoop()
        {
            try
            {
                // wait for the game window
                while (!GameProcessFound && !TrainerExiting)
                {
                    GameProcess = LocateGameWindow();
                    if (GameProcess == null)
                        Thread.Sleep(1000);
                    else
                        GameProcessFound = true;
                }

                if (GameProcessFound)
                {
                    // found the process, proceed
                    GameBaseAddress = (uint)GameProcess.MainModule.BaseAddress;

                    // WPF quirks ftw, we have to run this on the thread which owns the window
                    this.Dispatcher.BeginInvoke((ThreadStart)delegate()
                    {
                        this.Title = this.Title + " - game found!";
                    });

                    // stop music and wpf animations

                    // sometimes this doesnt stop correctly, needs investigation
                    BassMOD.BASSMOD_MusicStop();
                    ((Storyboard)this.Resources["LogoZoom"]).Stop();
                    ((Storyboard)this.Resources["LogoRotate"]).Stop();
                    ((Storyboard)this.Resources["BackgroundRotate"]).Stop();

                    GameMemory.Open(GameProcess);

                    GameMemoryScanner = new SigScan(GameProcess, (IntPtr)GameBaseAddress, MemorySearchRegionSize);

                    // enable all option hotkeys
                    SetupOptions();
                }
            }
            catch (ThreadInterruptedException)
            {
            }
        }

#region Trainer Hotkey Event Handlers
        byte[] GodModeOn = new byte[] { 0x90, 0x90, 0x90, 0x90 };
        private void GodMode_HotkeyPressed()
        {
            // locate address from pattern
            uint GodModeAddress = GameMemoryScanner.FindPattern("2B 44 24 10 A3", 0);
            if (GodModeAddress != 0)
                GameMemory.Write(GodModeAddress, ref GodModeOn);
        }

        byte[] InfAmmoOn = new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };
        private void InfAmmo_HotkeyPressed()
        {
            // locate address from pattern
            uint InfAmmoGunAddress = GameMemoryScanner.FindPattern("D9 5C 24 18 FF 0D", 4);
            if (InfAmmoGunAddress != 0)
                GameMemory.Write((uint)InfAmmoGunAddress, ref InfAmmoOn); // gun
            // locate address from pattern
            uint InfAmmoNadesAddress = GameMemoryScanner.FindPattern("83 C4 04 FF 0D ?? ?? ?? ?? 8D", 3);
            if (InfAmmoGunAddress != 0)
                GameMemory.Write((uint)InfAmmoNadesAddress, ref InfAmmoOn); // nades
            // locate address from pattern
            uint InfAmmoBlocksAddress = GameMemoryScanner.FindPattern("83 C4 04 FF 0D ?? ?? ?? ?? 75 0A", 3);
            if (InfAmmoGunAddress != 0)
                GameMemory.Write((uint)InfAmmoBlocksAddress, ref InfAmmoOn); // blocks
        }

        byte[] RapidfireGunOn = new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };
        private void RapidfireGun_HotkeyPressed()
        {
            uint RapidfireGunAddress = GameMemoryScanner.FindPattern("3B 95 ?? ?? ?? ?? 0F 8E E0", 6);
            if (RapidfireGunAddress != 0)
                GameMemory.Write((uint)RapidfireGunAddress, ref RapidfireGunOn);
        }

        byte[] NoRecoilOn = new byte[] { 0x33, 0xC0, 0x90, 0x90, 0x90 };
        private void NoRecoil_HotkeyPressed()
        {
            uint NoRecoilAddress = GameMemoryScanner.FindPattern("A1 ?? ?? ?? ?? 39 44 24 14 0F 85", 0);
            if (NoRecoilAddress != 0)
                GameMemory.Write((uint)NoRecoilAddress, ref NoRecoilOn);
        }

        byte[] RapidfireNadesOn = new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };
        private void RapidfireNades_HotkeyPressed()
        {
            uint RapidfireNadesAddress = GameMemoryScanner.FindPattern("89 0D ?? ?? ?? ?? 75 0A", 0);
            if (RapidfireNadesAddress != 0)
                GameMemory.Write((uint)RapidfireNadesAddress, ref RapidfireNadesOn);
        }

        byte[] NoFogOn = new byte[1024];
        private void NoFog_HotkeyPressed()
        {
            uint FogLUTRefAddress = GameMemoryScanner.FindPattern("B9 FF 07 00 00 3D FF 07 00 00 7F 02 8B C8", 17);
            if (FogLUTRefAddress != 0)
            {
                // read the ptr for the fog LUT
                uint FogLUTAddress = GameMemory.ReadU32((uint)FogLUTRefAddress);
                // fill the LUT with zeroes
                GameMemory.Write(FogLUTAddress, ref NoFogOn);
            }
        }

        object SpeedHackLock = new object();
        bool SpeedHackDone = false;
        private void SpeedHack_HotkeyPressed()
        {
            lock (SpeedHackLock)
            {
                if (!SpeedHackDone)
                {
                    SpeedHackDone = true;

                    // locate address from pattern
                    uint SpeedHackAddress = GameMemoryScanner.FindPattern("EB 12 39 9D ?? ?? ?? ?? 74 0E D9 44 24 10 DC 0D", 16);
                    if (SpeedHackAddress != 0)
                    {
                        // write our new speed value first
                        const double NewSpeed = 2.5f;
                        GameMemory.WriteF64(GameBaseAddress + SpeedHackOffset, NewSpeed);
                        // now change the code to point to it
                        GameMemory.WriteU32(SpeedHackAddress, GameBaseAddress + SpeedHackOffset);
                    }

                    Options["Speedhack"].Hotkey.UnregisterHotKey(); // disable this to avoid it running twice
                }
            }
        }

        object MegaJumpLock = new object();
        bool MegaJumpDone = false;
        private void MegaJump_HotkeyPressed()
        {
            lock (MegaJumpLock)
            {
                if (!MegaJumpDone) // run once
                {
                    MegaJumpDone = true;

                    // jump height
                    // locate address from pattern
                    uint JumpHeightAddress = GameMemoryScanner.FindPattern("D9 05 ?? ?? ?? ?? 39", 2);
                    if (JumpHeightAddress != 0)
                    {
                        // write our new speed value first
                        //const float MegaJumpHeight = -0.8f;
                        GameMemory.WriteF32(GameBaseAddress + JumpHeightOffset, (float)megajumpSlider.Value);
                        // now change the code to point to it
                        GameMemory.WriteU32(JumpHeightAddress, GameBaseAddress + JumpHeightOffset);
                    }

                    // jump steering
                    // locate address from pattern
                    uint JumpSteeringAddress = GameMemoryScanner.FindPattern("DC 0D ?? ?? ?? ?? EB 26", 2);
                    if (JumpSteeringAddress != 0)
                    {
                        // write our new speed value first
                        const double MegaJumpSteering = 0.9f;
                        GameMemory.WriteF64(GameBaseAddress + JumpSteeringOffset, MegaJumpSteering);
                        // now change the code to point to it
                        GameMemory.WriteU32(JumpSteeringAddress, GameBaseAddress + JumpSteeringOffset);
                    }

                    Options["Megajump"].Hotkey.UnregisterHotKey(); // disable this to avoid it running twice
                }
            }
        }

        object SuperNadeRangeLock = new object();
        bool SuperNadeRangeDone = false;
        private void SuperNadeRange_HotkeyPressed()
        {
            lock (SuperNadeRangeLock)
            {
                if (!SuperNadeRangeDone)
                {
                    SuperNadeRangeDone = true;

                    // locate address from pattern
                    uint NadeRangeAddress = GameMemoryScanner.FindPattern("DD 05 ?? ?? ?? ?? DC C9 D9 81", 2);
                    if (NadeRangeAddress != 0)
                    {
                        // write our new speed value first
                        const double SuperNadeRange = 8.0f;
                        GameMemory.WriteF64(GameBaseAddress + NadeRangeOffset, SuperNadeRange);
                        // now change the code to point to it
                        GameMemory.WriteU32(NadeRangeAddress, GameBaseAddress + NadeRangeOffset);
                    }

                    Options["SuperNadeRange"].Hotkey.UnregisterHotKey(); // disable this to avoid it running twice

                    System.Console.Beep();
                }
            }
        }

        private void EnableAll_HotkeyPressed()
        {
            GodMode_HotkeyPressed();
            InfAmmo_HotkeyPressed();
            RapidfireGun_HotkeyPressed();
            NoRecoil_HotkeyPressed();
            RapidfireNades_HotkeyPressed();
            NoFog_HotkeyPressed();
            SpeedHack_HotkeyPressed();
            MegaJump_HotkeyPressed();
            MultiJump_HotkeyPressed();
            System.Console.Beep();
        }

        object NadeSpamLock = new object();
        bool NadeSpamInProgress = false;
        /// <summary>
        /// clicks the mouse rapidly to spam nades
        /// </summary>
        private void NadeSpammer()
        {
            //if (Process.f.MainWindowTitle == "Ace of Spades")
            if (NadeSpamInProgress) return; // to avoid the clicks building up
            lock (NadeSpamLock) // prevent more than 1 of these from running at a time
            {
                new Thread(delegate()
                {
                    NadeSpamInProgress = true;
                    for (int i = 0; i < 15; i++)
                    {
                        Mouse.LeftButtonDown();
                        Thread.Sleep(25);
                        Mouse.LeftButtonUp();
                        Thread.Sleep(25);
                    }
                    NadeSpamInProgress = false;
                }).Start();
                //System.Console.Beep();
            }
        }

        byte[] MultiJumpOn = new byte[] { 0x90, 0x90 };
        private void MultiJump_HotkeyPressed()
        {
            uint MultiJumpAddress = GameMemoryScanner.FindPattern("75 ?? B9 01 00 00 00", 0);
            if (MultiJumpAddress != 0)
                GameMemory.Write((uint)MultiJumpAddress, ref MultiJumpOn);
        }

        object ExtendRangeLock = new object();
        bool ExtendRangeDone = false;
        private void ExtendRange_HotkeyPressed()
        {
            lock (ExtendRangeLock)
            {
                if (!ExtendRangeDone)
                {
                    ExtendRangeDone = true;

                    const float NewToolRangeMax = 32000f;
                    const float NewToolRangeMin = -32000f;

                    // write our new values first
                    GameMemory.WriteF32(GameBaseAddress + ToolRangeMaxOffset, NewToolRangeMax);
                    GameMemory.WriteF32(GameBaseAddress + ToolRangeMinOffset, NewToolRangeMin);

                    // extend the range for pickaxe (was 3, -3)
                    // locate address from pattern
                    uint PickaxeUpperRangeAddress = GameMemoryScanner.FindPattern("D8 64 24 48 D9 5C 24 54", 10);
                    if (PickaxeUpperRangeAddress != 0)
                        // change the code to point to new value
                        GameMemory.WriteU32(PickaxeUpperRangeAddress, GameBaseAddress + ToolRangeMaxOffset);

                    // locate address from pattern
                    uint PickaxeLowerRangeAddress = GameMemoryScanner.FindPattern("0F 8A 3C 01 00 00 D9 05", 8);
                    if (PickaxeLowerRangeAddress != 0)
                        // change the code to point to new value
                        GameMemory.WriteU32(PickaxeLowerRangeAddress, GameBaseAddress + ToolRangeMinOffset);


                    // extend the range for shovel (was 3, -3)
                    // locate address from pattern
                    uint ShovelUpperRangeAddress = GameMemoryScanner.FindPattern("D9 5C 24 48 D9 05 ?? ?? ?? ?? D9 44 24 40 D8 D1", 6);
                    if (ShovelUpperRangeAddress != 0)
                        // change the code to point to new value
                        GameMemory.WriteU32(ShovelUpperRangeAddress, GameBaseAddress + ToolRangeMaxOffset);

                    // locate address from pattern
                    uint ShovelLowerRangeAddress = GameMemoryScanner.FindPattern("0F 8A 07 01 00 00 D9 05 ?? ?? ?? ?? D8 D1", 8);
                    if (ShovelLowerRangeAddress != 0)
                        // change the code to point to new value
                        GameMemory.WriteU32(ShovelLowerRangeAddress, GameBaseAddress + ToolRangeMinOffset);


                    // extend the range for block laying (was 3, -3)
                    // locate address from pattern
                    uint BlockUpperRangeAddress = GameMemoryScanner.FindPattern("D8 AD ?? ?? ?? ?? D9 5C 24 54 D9 05", 12);
                    if (BlockUpperRangeAddress != 0)
                        // change the code to point to new value
                        GameMemory.WriteU32(BlockUpperRangeAddress, GameBaseAddress + ToolRangeMaxOffset);

                    // locate address from pattern
                    uint BlockLowerRangeAddress = GameMemoryScanner.FindPattern("F6 C4 05 0F 8A 08 0B 00 00 D9 05", 11);
                    if (BlockLowerRangeAddress != 0)
                        // change the code to point to new value
                        GameMemory.WriteU32(BlockLowerRangeAddress, GameBaseAddress + ToolRangeMinOffset);

                    Options["ExtendRange"].Hotkey.UnregisterHotKey(); // disable this to avoid it running twice

                    System.Console.Beep();
                }
            }
        }


        byte[] RapidOn = new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };
        object RapidBuildDestroyLock = new object();
        bool RapidBuildDestroyDone = false;
        private void RapidBuildDestroy_HotkeyPressed()
        {
            lock (RapidBuildDestroyLock)
            {
                if (!RapidBuildDestroyDone)
                {
                    RapidBuildDestroyDone = true;

                    // locate address from pattern
                    uint RapidShovelAddress = GameMemoryScanner.FindPattern("75 13 81 C2 E8 03 00 00", 0);
                    if (RapidShovelAddress != 0)
                        GameMemory.WriteU8(RapidShovelAddress, 0xEB);

                    uint RapidPickaxeAddress = GameMemoryScanner.FindPattern("0F 8E 26 0D 00 00 81 C2 C8 00 00 00", 0);
                    if (RapidPickaxeAddress != 0)
                        GameMemory.Write(RapidPickaxeAddress, ref RapidOn);

                    uint RapidBlockAddress = GameMemoryScanner.FindPattern("81 C2 F4 01 00 00 89 15", 6);
                    if (RapidBlockAddress != 0)
                        GameMemory.Write(RapidBlockAddress, ref RapidOn);

                    Options["RapidBuildDestroy"].Hotkey.UnregisterHotKey(); // disable this to avoid it running twice

                    System.Console.Beep();
                }
            }
        }

#endregion

        private void SetupOptions()
        {
            // WPF quirks ftw, we have to run this on the thread which owns the window
            this.Dispatcher.BeginInvoke((ThreadStart)delegate()
            {
                IntPtr windowHandle = new WindowInteropHelper(this).Handle;
                TrainerOption GodMode = new TrainerOption(Keys.F1, GodMode_HotkeyPressed, windowHandle);
                TrainerOption InfAmmo = new TrainerOption(Keys.F2, InfAmmo_HotkeyPressed, windowHandle);
                TrainerOption RapidfireGun = new TrainerOption(Keys.F3, RapidfireGun_HotkeyPressed, windowHandle);
                TrainerOption NoRecoil = new TrainerOption(Keys.F4, NoRecoil_HotkeyPressed, windowHandle);
                TrainerOption RapidfireNades = new TrainerOption(Keys.F5, RapidfireNades_HotkeyPressed, windowHandle);
                TrainerOption NoFog = new TrainerOption(Keys.F6, NoFog_HotkeyPressed, windowHandle);
                TrainerOption Speedhack = new TrainerOption(Keys.F7, SpeedHack_HotkeyPressed, windowHandle);
                TrainerOption Megajump = new TrainerOption(Keys.F8, MegaJump_HotkeyPressed, windowHandle);
                TrainerOption Multijump = new TrainerOption(Keys.F9, MultiJump_HotkeyPressed, windowHandle);

                TrainerOption EnableAll = new TrainerOption(Keys.F10, EnableAll_HotkeyPressed, windowHandle);
                
                TrainerOption ExtendRange = new TrainerOption(Keys.D1, ExtendRange_HotkeyPressed, windowHandle);
                TrainerOption RapidBuildDestroy = new TrainerOption(Keys.D2, RapidBuildDestroy_HotkeyPressed, windowHandle);

                TrainerOption SuperNadeRange = new TrainerOption(Keys.D5, SuperNadeRange_HotkeyPressed, windowHandle);

                TrainerOption NadeSpam = new TrainerOption(Keys.Z, NadeSpammer, windowHandle);
                

                Options.Add("GodMode", GodMode);
                Options.Add("InfAmmo", InfAmmo);
                Options.Add("RapidfireGun", RapidfireGun);
                Options.Add("NoRecoil", NoRecoil);
                Options.Add("RapidfireNades", RapidfireNades);
                Options.Add("NoFog", NoFog);
                Options.Add("Speedhack", Speedhack);
                Options.Add("Megajump", Megajump);
                Options.Add("Multijump", Multijump);
                Options.Add("SuperNadeRange", SuperNadeRange);
                Options.Add("EnableAll", EnableAll);
                Options.Add("NadeSpam", NadeSpam);
                Options.Add("ExtendRange", ExtendRange);
                Options.Add("RapidBuildDestroy", RapidBuildDestroy);
            });
        }

        // TODO: support toggling hotkey and trainer options
        public class TrainerOption
        {
            public bool ToggleState { get; set; }
            public HotKey Hotkey { get; set; }

            // default to hotkey OFF
            public TrainerOption(Keys KeyCode, Action HotkeyPressed, IntPtr WindowHandle)
                : this(ModifierKeys.None, KeyCode, HotkeyPressed, false, WindowHandle)
            {
            }

            public TrainerOption(ModifierKeys Modifier, Keys KeyCode, Action HotkeyPressed, bool Enabled, IntPtr WindowHandle)
            {
                Hotkey = new HotKey(Modifier, KeyCode, WindowHandle);
                Hotkey.HotKeyPressed += (blargh) => HotkeyPressed();

                this.ToggleState = false;
            }
        }

        private void Mute_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            BassMOD.BASSMOD_MusicStop();
        }

        /// <summary>
        /// allow the user to change the jump height after the option is enabled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void megajumpSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (GameProcessFound)
            {
                GameMemory.WriteF32(GameBaseAddress + JumpHeightOffset, (float)megajumpSlider.Value);
            }
        }
    }
}
