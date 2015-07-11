using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows.Forms;
using Chip8Emulator;
using Timer = System.Timers.Timer;

namespace Chip8EmulatorGui
{
    public partial class MainForm : Form
    {
        private const int PixelSize = 10;

        private readonly Emulator _emulator;
        private readonly Timer _timer;

        private readonly Keys[] _validKeys =
        {
            Keys.D1, Keys.D2, Keys.D3, Keys.D4,
            Keys.Q, Keys.W, Keys.E, Keys.R,
            Keys.A, Keys.S, Keys.D, Keys.F,
            Keys.Z, Keys.X, Keys.C, Keys.V
        };

        public MainForm()
        {
            InitializeComponent();

            _timer = new Timer(1000 / 30f);
            _timer.Elapsed += TimerOnElapsed;

            _emulator = new Emulator();

            _emulator.DisplayUpdate += EmulatorOnDisplayUpdate;
            _emulator.SoundOn += EmulatorOnSoundOn;
            _emulator.SoundOff += EmulatorOnSoundOff;
        }

        private void EmulatorOnDisplayUpdate(object sender, EventArgs eventArgs)
        {
            canvas.Invalidate();
        }

        private void EmulatorOnSoundOn(object sender, EventArgs eventArgs)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)(() => EmulatorOnSoundOn(sender, eventArgs)));
                return;
            }

            BackColor = Color.DarkSalmon;
        }

        private void EmulatorOnSoundOff(object sender, EventArgs eventArgs)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)(() => EmulatorOnSoundOff(sender, eventArgs)));
                return;
            }

            BackColor = SystemColors.Control;
        }

        private void CanvasPaint(object sender, PaintEventArgs e)
        {
            for (int y = 0; y < _emulator.Display.Height; y++)
            {
                for (int x = 0; x < _emulator.Display.Width; x++)
                {
                    if (_emulator.Display.GetPixel(x, y))
                    {
                        e.Graphics.FillRectangle(Brushes.Black, x * PixelSize, y * PixelSize, PixelSize, PixelSize);
                    }
                }
            }
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            _emulator.DoCycle();
        }

        private void MainFormKeyDown(object sender, KeyEventArgs e)
        {
            int index = Array.IndexOf(_validKeys, e.KeyCode);

            if (index != -1)
            {
                _emulator.KeyDown(index);
            }
        }

        private void MainFormKeyUp(object sender, KeyEventArgs e)
        {
            int index = Array.IndexOf(_validKeys, e.KeyCode);

            if (index != -1)
            {
                _emulator.KeyUp(index);
            }
        }

        private void MainFormLoad(object sender, EventArgs e)
        {
            PopulatePrograms();
            PopulateSpeedLevels();
        }

        private void StartButtonClick(object sender, EventArgs e)
        {
            _timer.Stop();

            string programPath = programsInput.SelectedValue.ToString();
            Emulator.Speed speedLevel = (Emulator.Speed)Enum.Parse(typeof(Emulator.Speed), speedLevelsCombo.SelectedValue.ToString());

            _emulator.LoadProgram(programPath, speedLevel);
            _timer.Start();

            ActiveControl = null;
        }

        private void PopulatePrograms()
        {
            var programs = Directory.EnumerateFiles("Programs")
                                    .Select(f => new {FullPath = f, ProgramName = Path.GetFileNameWithoutExtension(f)})
                                    .ToArray();

            programsInput.DisplayMember = "ProgramName";
            programsInput.ValueMember = "FullPath";
            programsInput.DataSource = programs;
            programsInput.SelectedIndex = 0;

            startButton.Enabled = programs.Length != 0;
        }

        private void PopulateSpeedLevels()
        {
            speedLevelsCombo.DataSource = Enum.GetValues(typeof(Emulator.Speed));
        }
    }
}