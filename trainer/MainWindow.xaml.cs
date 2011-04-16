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

namespace trainer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<TrainerOption> Options = new List<TrainerOption>();
        ProcessMemory GameMemory = new ProcessMemory();
        bool GameProcessFound = false;
        bool TrainerExiting = false;
        uint GameBaseAddress;
        Thread FindGameWindow;

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

        byte[] NoFogOn = new byte[2048];
        byte[] NoFogOff = new byte[2048];
        private void NoFog_HotkeyPressed()
        {
            GameMemory.Write(GameBaseAddress + 0x628D0, ref NoFogOn);
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

                Options.Add(GodMode);
                Options.Add(InfAmmo);
                Options.Add(RapidfireGun);
                Options.Add(NoRecoil);
                Options.Add(RapidfireNades);
                Options.Add(NoFog);
            });
        }

        // TODO: support toggling hotkey and trainer options
        public class TrainerOption
        {
            bool ToggleState { get; set; }
            HotKey Hotkey;

            // default to hotkey OFF
            public TrainerOption(Keys KeyCode, Action HotkeyPressed, IntPtr WindowHandle)
                : this(ModifierKeys.None, KeyCode, HotkeyPressed, false, WindowHandle)
            {
            }

            public TrainerOption(ModifierKeys Modifier, Keys KeyCode, Action HotkeyPressed, bool Enabled, IntPtr WindowHandle)
            {
                Hotkey = new HotKey(Modifier, KeyCode, WindowHandle);
                Hotkey.HotKeyPressed += (blargh) => HotkeyPressed();

                this.ToggleState           = false;
            }
        }
    }
}
