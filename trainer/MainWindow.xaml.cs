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
        ProcessMemory GameMemory = new ProcessMemory();
        bool GameProcessFound = false;
        bool TrainerExiting = false;
        uint GameBaseAddress;
        Thread FindGameWindow;
        MouseSimulator Mouse = new MouseSimulator();

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

            // give the thread a second to check for the game window before we do anything stupid
            Thread.Sleep(100);
            if (!GameProcessFound)
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

        /// <summary>
        /// This thread waits for the game window, then stops music/animation and enables the trainer hotkeys
        /// </summary>
        private void TrainerLoop()
        {
            Process gameProcess;

            try
            {
                while (!GameProcessFound && !TrainerExiting)
                {
                    // find game window
                    gameProcess = Process.GetProcessesByName("client")
                        .SingleOrDefault(proc => proc.MainWindowTitle == "Ace of Spades");
                    if (gameProcess != null)
                    {
                        GameBaseAddress = (uint)gameProcess.MainModule.BaseAddress;

                        // WPF quirks ftw, we have to run this on the thread which owns the window
                        this.Dispatcher.BeginInvoke((ThreadStart)delegate()
                        {
                            this.Title = this.Title + " - game found!";
                        });

                        // stop music and wpf animations

                        // sometimes this doesnt stop correctly
                        BassMOD.BASSMOD_MusicStop();
                        BassMOD.BASSMOD_MusicStop();
                        BassMOD.BASSMOD_MusicStop();
                        ((Storyboard)this.Resources["LogoZoom"]).Stop();
                        ((Storyboard)this.Resources["LogoRotate"]).Stop();
                        ((Storyboard)this.Resources["BackgroundRotate"]).Stop();

                        GameMemory.Open(gameProcess);

                        // enable all option hotkeys
                        SetupOptions();

                        GameProcessFound = true;
                        break;
                    }
                    else
                        Thread.Sleep(1000);
                }
            }
            catch (ThreadInterruptedException)
            {
            }
        }

#region Trainer Hotkey Event Handlers
        byte[] GodModeOn = new byte[] { 0x90, 0x90, 0x90, 0x90 };
        byte[] GodModeOff = new byte[] { 0x2B, 0x44, 0x24, 0x10 };
        private void GodMode_HotkeyPressed()
        {
            GameMemory.Write(GameBaseAddress + 0x23DE5, ref GodModeOn);
        }

        byte[] InfAmmoGunOn = new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };
        byte[] InfAmmoGunOff = new byte[] { 0xFF, 0x0D, 0xA4, 0xBC, 0x50, 0x03 };
        byte[] InfAmmoNadesOn = new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };
        byte[] InfAmmoNadesOff = new byte[] { };
        private void InfAmmo_HotkeyPressed()
        {
            GameMemory.Write(GameBaseAddress + 0x2539D, ref InfAmmoGunOn);
            GameMemory.Write(GameBaseAddress + 0x254C7, ref InfAmmoNadesOn);
        }

        byte[] RapidfireGunOn = new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };
        byte[] RapidfireGunOff = new byte[] { 0x0F, 0x8E, 0x54, 0x07, 0x00, 0x00 };
        private void RapidfireGun_HotkeyPressed()
        {
            GameMemory.Write(GameBaseAddress + 0x24C60, ref RapidfireGunOn);
        }

        byte[] NoRecoilOn = new byte[] { 0xE9, 0x37, 0x06, 0x00, 0x00, 0x90 };
        byte[] NoRecoilOff = new byte[] { 0x0F, 0x85, 0x36, 0x06, 0x00, 0x00 };
        private void NoRecoil_HotkeyPressed()
        {
            GameMemory.Write(GameBaseAddress + 0x252BE, ref NoRecoilOn);
        }

        // wip
        //byte[] RapidfireNadesOn1 = new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };
        //byte[] RapidfireNadesOff1 = new byte[] { 0x0F, 0x85, 0x94, 0x00, 0x00, 0x00 };

        byte[] RapidfireNadesOn1 = new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };
        byte[] RapidfireNadesOff1 = new byte[] { 0x89, 0x0D, 0x9C, 0xBC, 0x7D, 0x03 };
        private void RapidfireNades_HotkeyPressed()
        {
            GameMemory.Write(GameBaseAddress + 0x254D3, ref RapidfireNadesOn1);
        }

        byte[] NoFogOn = new byte[1024];
        byte[] NoFogOff = new byte[1024];
        private void NoFog_HotkeyPressed()
        {
            GameMemory.Write(GameBaseAddress + 0x628D0, ref NoFogOn);
        }

        //const double DefaultMovementAccel = 0.1000000014901161;
        //const double SpeedHackAccel = 0.9;
        //byte[] SpeedHackOn = new byte[] { 0xDC, 0x0D, 0x88, 0x84, 0x31, 0x01 };
        //byte[] SpeedHackOff = new byte[] { 0xDC, 0x0D, 0xF0, 0x82, 0x31, 0x01 };
        object SpeedHackLock = new object();
        bool SpeedHackDone = false;
        private void SpeedHack_HotkeyPressed()
        {
            lock (SpeedHackLock)
            {
                if (!SpeedHackDone)
                {
                    SpeedHackDone = true;
                    // read orig speed value offset
                    uint SpeedPtr = GameMemory.ReadU32(GameBaseAddress + 0x23FBA);
                    SpeedPtr += 0x198;
                    GameMemory.WriteU32(GameBaseAddress + 0x23FBa, SpeedPtr);

                    Options["Speedhack"].Hotkey.UnregisterHotKey(); // disable this to avoid it running twice
                }
            }
        }

        //const float DefaultJumpHeight = -0.32f;
        //const float MegaJumpHeight = -0.9f;
        // jump height patch
        //byte[] MegaJumpOn = new byte[] { 0xD9, 0x05, 0xF0, 0x83, 0x31, 0x01 };
        //byte[] MegaJumpOff = new byte[] { 0xD9, 0x05, 0x40, 0x85, 0x31, 0x01 };
        // jump steering patch
        //byte[] MegaJumpSteeringOn = new byte[] { 0xDC, 0x0D, 0x20, 0x84, 0x31, 0x01 };
        //byte[] MegaJumpSteeringOff = new byte[] { 0xDC, 0x0D, 0x48, 0x84, 0x31, 0x01 };
        object MegaJumpLock = new object();
        bool MegaJumpDone = false;
        private void MegaJump_HotkeyPressed()
        {
            lock (MegaJumpLock)
            {
                if (!MegaJumpDone) // run once
                {
                    MegaJumpDone = true;

                    //GameMemory.Write(GameBaseAddress + 0x23F26, ref MegaJumpOn);
                    uint JumpHeightPtr = GameMemory.ReadU32(GameBaseAddress + 0x23F28);
                    JumpHeightPtr -= 0x150;
                    GameMemory.WriteU32(GameBaseAddress + 0x23F28, JumpHeightPtr);
                    //GameMemory.Write(GameBaseAddress + 0x23F90, ref MegaJumpSteeringOn);
                    uint JumpSteeringPtr = GameMemory.ReadU32(GameBaseAddress + 0x23F92);
                    JumpSteeringPtr -= 0x28;
                    GameMemory.WriteU32(GameBaseAddress + 0x23F92, JumpSteeringPtr);

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
                    // read orig speed value offset
                    uint NadeRangePtr = GameMemory.ReadU32(GameBaseAddress + 0x21FD4);
                    NadeRangePtr += 0xB0;
                    GameMemory.WriteU32(GameBaseAddress + 0x21FD4, NadeRangePtr);

                    Options["SuperNadeRange"].Hotkey.UnregisterHotKey(); // disable this to avoid it running twice
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
        byte[] MultiJumpOff = new byte[] { 0x75, 0x0F };
        private void MultiJump_HotkeyPressed()
        {
            GameMemory.Write(GameBaseAddress + 0x23A8A, ref MultiJumpOn);
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
                TrainerOption SuperNadeRange = new TrainerOption(Keys.F10, SuperNadeRange_HotkeyPressed, windowHandle);
                TrainerOption EnableAll = new TrainerOption(Keys.F11, EnableAll_HotkeyPressed, windowHandle);

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
            // sometimes this doesnt stop correctly
            BassMOD.BASSMOD_MusicStop();
            BassMOD.BASSMOD_MusicStop();
            BassMOD.BASSMOD_MusicStop();
        }
    }
}
