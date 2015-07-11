using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Chip8Emulator
{
    public class Emulator
    {
        public Display Display { get; private set; }
        public bool IsHalted { get; private set; }

        public enum Speed
        {
            Slow = 150,
            Moderate = 500,
            Normal = 840,
            Fast = 1500,
            UltraFast = 3000
        }

        public event EventHandler DisplayUpdate;
        public event EventHandler SoundOn;
        public event EventHandler SoundOff;

        private const int MemorySize = 4096;
        private const int ReservedMemorySize = 512;
        private const int MaximumProgramSize = MemorySize - ReservedMemorySize;
        private const int ScreenWidth = 64;
        private const int ScreenHeight = 32;

        private static readonly int[] Masks = {0xF0FF, 0xF00F, 0xF000};

        private static readonly byte[] Fonts =
        {
            0xF0, 0x90, 0x90, 0x90, 0xF0, 0x20, 0x60, 0x20, 0x20, 0x70, 0xF0, 0x10, 0xF0, 0x80, 0xF0,
            0xF0, 0x10, 0xF0, 0x10, 0xF0, 0x90, 0x90, 0xF0, 0x10, 0x10, 0xF0, 0x80, 0xF0, 0x10, 0xF0,
            0xF0, 0x80, 0xF0, 0x90, 0xF0, 0xF0, 0x10, 0x20, 0x40, 0x40, 0xF0, 0x90, 0xF0, 0x90, 0xF0,
            0xF0, 0x90, 0xF0, 0x10, 0xF0, 0xF0, 0x90, 0xF0, 0x90, 0x90, 0xE0, 0x90, 0xE0, 0x90, 0xE0,
            0xF0, 0x80, 0x80, 0x80, 0xF0, 0xE0, 0x90, 0x90, 0x90, 0xE0, 0xF0, 0x80, 0xF0, 0x80, 0xF0,
            0xF0, 0x80, 0xF0, 0x80, 0x80
        };

        private static readonly Dictionary<ushort, Action<Emulator, ushort>> Opcodes;

        private readonly byte[] _memory;
        private readonly ushort[] _stack;

        // Represents the keyboard state.
        private readonly bool[] _keyboard;

        // 16 registers.
        private readonly byte[] _v;

        // The I register.
        private ushort _i;

        // The stack pointer.
        private byte _sp;

        // The program counter register.
        private ushort _pc;

        // Delay timer.
        private byte _dt;

        // Sound timer.
        private byte _st;

        private readonly Random _random;
        private DateTime _lastTime;
        private int _cyclesPerSecond = 840;

        static Emulator()
        {
            /* Find all methods which implement a particular opcode, and build a lookup table.
             *          
             * The opcodes in chip8 could take the following formats:
             * 
             *    0x1xxx
             *    0x1xx1
             *    0x1x11
             *    
             * Where "1" represents a fixed and known nibble, and "x" represents a parameter.
             * Given this, I decided to replace every "x" in the opcode with 0, and use that as a key
             * to the lookup table. Of course, in order the find an opcode in the lookup table, I'll need
             * to first do: opcode & 0xF0FF, and use that as a key. If there isn't a matching entry, then
             * I'll do: opcode & 0xF00F... and finally: opcode & 0xF000. This means that in the worst case
             * executing and opcode would take three lookups... There are more efficients ways to implement this
             * and less efficients ways, I only chose a fairly simple way. */
            Opcodes = new Dictionary<ushort, Action<Emulator, ushort>>();

            var methods = typeof(Emulator).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                                          .Where(p => p.IsDefined(typeof(OpcodeAttribute), true))
                                          .ToList();

            foreach (MethodInfo method in methods)
            {
                OpcodeAttribute attribute = (OpcodeAttribute)Attribute.GetCustomAttribute(method, typeof(OpcodeAttribute));
                ushort opcode = (ushort)int.Parse(Regex.Replace(attribute.Value.ToUpper(), "[XYNK]", "0"), NumberStyles.HexNumber);

                MethodInfo m = method;
                Opcodes[opcode] = (@this, op) => m.Invoke(@this, new object[] {op});
            }
        }

        public Emulator()
        {
            Display = new Display(ScreenWidth, ScreenHeight);

            _v = new byte[16];
            _memory = new byte[MemorySize];
            _stack = new ushort[16];
            _keyboard = new bool[16];
            _random = new Random();
        }

        public void LoadProgram(string pathToProgram, Speed speedLevel)
        {
            InitializeEmulator();
            _cyclesPerSecond = (int)speedLevel;

            using (Stream stream = File.OpenRead(pathToProgram))
            {
                stream.Read(_memory, _pc, MaximumProgramSize);
            }
        }

        public void DoCycle()
        {
            if (IsHalted)
            {
                return;
            }

            DateTime now = DateTime.Now;

            if (_lastTime == DateTime.MinValue)
            {
                _lastTime = now;
            }

            // Calculate how many instructiones we need to execute given the elapsed time since last call.
            TimeSpan elapsedTime = now.Subtract(_lastTime);
            int cyclesToExecute = (int)(_cyclesPerSecond * elapsedTime.TotalSeconds);

            for (int i = 0; i < cyclesToExecute; i++)
            {
                ushort opcode = (ushort)((_memory[_pc] << 8) | _memory[_pc + 1]);
                ExecuteOpcode(opcode);
            }

            DecrementDelayTimer(elapsedTime);
            DecrementSoundTimer(elapsedTime);

            _lastTime = now;
        }

        public void KeyDown(int key)
        {
            _keyboard[key] = true;
        }

        public void KeyUp(int key)
        {
            _keyboard[key] = false;
        }

        private void InitializeEmulator()
        {
            ClearMemory();
            InitializeRegisters();
            InitializeFonts();
            InitializeKeyboard();
            Display.Clear();

            _lastTime = DateTime.MinValue;
            IsHalted = false;
        }

        private void ClearMemory()
        {
            for (int i = 0; i < _memory.Length; i++)
            {
                _memory[i] = 0x00;
            }
        }

        private void InitializeRegisters()
        {
            for (int i = 0; i < 16; i++)
            {
                _v[i] = 0;
            }

            _i = 0;
            _pc = 0x200;
            _sp = 0;
            _dt = 0;
            _st = 0;
        }

        private void InitializeFonts()
        {
            Array.Copy(Fonts, _memory, Fonts.Length);
        }

        private void InitializeKeyboard()
        {
            for (int i = 0; i < _keyboard.Length; i++)
            {
                _keyboard[i] = false;
            }
        }

        private void DecrementDelayTimer(TimeSpan elapsedTime)
        {
            if (_dt > 0)
            {
                // The timer decrements at a rate of 60Hz.
                byte delta = (byte)(elapsedTime.TotalSeconds * 60);
                _dt -= Math.Min(_dt, delta);
            }
        }

        private void DecrementSoundTimer(TimeSpan elapsedTime)
        {
            if (_st > 0)
            {
                // The timer decrements at a rate of 60Hz.
                byte delta = (byte)(elapsedTime.TotalSeconds * 60);
                _st -= Math.Min(_st, delta);

                if (_st == 0)
                {
                    if (SoundOff != null)
                    {
                        SoundOff(this, EventArgs.Empty);
                    }
                }
            }
        }

        private void ExecuteOpcode(ushort opcode)
        {
            Action<Emulator, ushort> value = null;
            int i = 0;

            while (i < Masks.Length && value == null)
            {
                Opcodes.TryGetValue((ushort)(opcode & Masks[i]), out value);
                i++;
            }

            if (value != null)
            {
                // Debug.WriteLine("opcode: {0:X4}", opcode);
                value(this, opcode);
            }
            else
            {
                throw new Exception(string.Format("Unknown opcode: {0:X4}", opcode));
            }
        }

        private byte GetX(ushort opcode)
        {
            return (byte)((opcode >> 8) & 0xF);
        }

        private byte GetY(ushort opcode)
        {
            return (byte)((opcode >> 4) & 0xF);
        }

        private ushort GetAddress(ushort opcode)
        {
            return (ushort)(opcode & 0xFFF);
        }

        private byte GetByte(ushort opcode)
        {
            return (byte)(opcode & 0xFF);
        }

        private byte GetLastNibble(ushort opcode)
        {
            return (byte)(opcode & 0xF);
        }

        // CLS
        [Opcode(Value = "00E0")]
        private void ClearScreen(ushort opcode)
        {
            Display.Clear();
            _pc += 2;
        }

        // RET
        [Opcode(Value = "00EE")]
        private void Return(ushort opcode)
        {
            _pc = _stack[--_sp];
        }

        // JP addr
        [Opcode(Value = "1nnn")]
        private void Jump(ushort opcode)
        {
            ushort address = GetAddress(opcode);

            // Halt if we are in an infinite loop.
            if (_pc == address)
            {
                IsHalted = true;
            }

            _pc = address;
        }

        // CALL addr
        [Opcode(Value = "2nnn")]
        private void Call(ushort opcode)
        {
            ushort address = GetAddress(opcode);

            _stack[_sp++] = (ushort)(_pc + 2);
            _pc = address;
        }

        // SE Vx, byte
        [Opcode(Value = "3xkk")]
        private void SkipNextEqualByte(ushort opcode)
        {
            byte reg = GetX(opcode);
            byte value = GetByte(opcode);

            if (_v[reg] == value)
            {
                _pc += 2;
            }

            _pc += 2;
        }

        // SNE Vx, byte
        [Opcode(Value = "4xkk")]
        private void SkipNextNotEqualByte(ushort opcode)
        {
            byte reg = GetX(opcode);
            byte value = GetByte(opcode);

            if (_v[reg] != value)
            {
                _pc += 2;
            }

            _pc += 2;
        }

        [Opcode(Value = "5xy0")]
        // SE Vx, Vy
        private void SkipNextEqualRegisters(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);

            if (_v[reg1] == _v[reg2])
            {
                _pc += 2;
            }

            _pc += 2;
        }

        // LD Vx, byte
        [Opcode(Value = "6xkk")]
        private void LoadRegisterWithByte(ushort opcode)
        {
            byte reg = GetX(opcode);
            byte value = GetByte(opcode);

            _v[reg] = value;
            _pc += 2;
        }

        // ADD Vx, byte
        [Opcode(Value = "7xkk")]
        private void AddByteToRegister(ushort opcode)
        {
            byte reg = GetX(opcode);
            byte value = GetByte(opcode);

            _v[reg] += value;
            _pc += 2;
        }

        // LD Vx, Vy
        [Opcode(Value = "8xy0")]
        private void LoadRegisterWithRegister(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);

            _v[reg1] = _v[reg2];
            _pc += 2;
        }

        // OR Vx, Vy
        [Opcode(Value = "8xy1")]
        private void OrRegisters(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);

            _v[reg1] |= _v[reg2];
            _pc += 2;
        }

        // AND Vx, Vy
        [Opcode(Value = "8xy2")]
        private void AndRegisters(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);

            _v[reg1] &= _v[reg2];
            _pc += 2;
        }

        // XOR Vx, Vy
        [Opcode(Value = "8xy3")]
        private void XorRegisters(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);

            _v[reg1] ^= _v[reg2];
            _pc += 2;
        }

        // ADD Vx, Vy
        [Opcode(Value = "8xy4")]
        private void AddRegisters(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);

            int sum = _v[reg1] + _v[reg2];
            _v[0xF] = (byte)(sum > 255 ? 1 : 0);
            _v[reg1] = (byte)sum;

            _pc += 2;
        }

        // SUB Vx, Vy
        [Opcode(Value = "8xy5")]
        private void SubRegisters(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);

            _v[0xF] = (byte)(_v[reg1] > _v[reg2] ? 1 : 0);
            _v[reg1] -= _v[reg2];

            _pc += 2;
        }

        // SHR Vx {, Vy}
        [Opcode(Value = "8xy6")]
        private void ShifRightRegister(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);

            _v[0xF] = (byte)(_v[reg1] & 1);
            _v[reg1] = (byte)(_v[reg2] >> 1);

            _pc += 2;
        }

        // SUBN Vx, Vy
        [Opcode(Value = "8xy7")]
        private void SubRegistersInverted(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);

            _v[0xF] = (byte)(_v[reg2] > _v[reg1] ? 1 : 0);
            _v[reg1] = (byte)(_v[reg2] - _v[reg1]);

            _pc += 2;
        }

        // SHL Vx {, Vy}
        [Opcode(Value = "8xyE")]
        private void ShifLeftRegisters(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);

            _v[0xF] = (byte)(_v[reg1] & 1);
            _v[reg1] = (byte)(_v[reg2] << 1);

            _pc += 2;
        }

        // SNE Vx, Vy
        [Opcode(Value = "9xy0")]
        private void SkipNextNotEqualRegisters(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);

            if (_v[reg1] != _v[reg2])
            {
                _pc += 2;
            }

            _pc += 2;
        }

        // LD I, addr
        [Opcode(Value = "Annn")]
        private void LoadIWithAddress(ushort opcode)
        {
            ushort address = GetAddress(opcode);

            _i = address;
            _pc += 2;
        }

        // JP V0, addr
        [Opcode(Value = "Bnnn")]
        private void JumpToAddressPlusRegister(ushort opcode)
        {
            ushort address = GetAddress(opcode);
            _pc = (ushort)(_v[0x0] + address);
        }

        // RND Vx, byte
        [Opcode(Value = "Cxkk")]
        private void GenerateRandomNumber(ushort opcode)
        {
            byte reg = GetX(opcode);
            byte value = GetByte(opcode);
            byte rnd = (byte)_random.Next(0, 256);

            _v[reg] = (byte)(rnd & value);
            _pc += 2;
        }

        // DRW Vx, Vy, nibble
        [Opcode(Value = "Dxyn")]
        private void DrawSprite(ushort opcode)
        {
            byte regX = GetX(opcode);
            byte regY = GetY(opcode);
            byte bytesToRead = GetLastNibble(opcode);

            int x = _v[regX];
            int initialY = _v[regY];

            if (x >= Display.Width)
            {
                x %= Display.Width;
            }

            if (initialY >= Display.Height)
            {
                initialY %= Display.Height;
            }

            _v[0xF] = 0;

            for (int i = 0; i < bytesToRead; i++)
            {
                int y = initialY + i;

                /* No need to continue drawing if the sprite is off the screen around
                 * the y axis. */
                if (y == Display.Height)
                {
                    break;
                }

                byte value = _memory[_i + i];
                byte currentByteOnScreen = Display.GetByte(x, y);

                byte newValue = (byte)(value ^ currentByteOnScreen);
                Display.SetByte(x, y, newValue);

                bool collision = (value | currentByteOnScreen) != newValue;
                if (collision)
                {
                    _v[0xF] = 1;
                }
            }

            if (DisplayUpdate != null)
            {
                DisplayUpdate(this, EventArgs.Empty);
            }

            _pc += 2;
        }

        // SKP Vx
        [Opcode(Value = "Ex9E")]
        private void SkipNextIfKeyPressed(ushort opcode)
        {
            byte reg = GetX(opcode);
            byte key = _v[reg];

            if (_keyboard[key])
            {
                _pc += 2;
            }

            _pc += 2;
        }

        // SKNP Vx
        [Opcode(Value = "ExA1")]
        private void SkipNextIfKeyNotPressed(ushort opcode)
        {
            byte reg = GetX(opcode);
            byte key = _v[reg];

            if (!_keyboard[key])
            {
                _pc += 2;
            }

            _pc += 2;
        }

        // LD Vx, DT
        [Opcode(Value = "Fx07")]
        private void LoadRegisterWithDelayTimer(ushort opcode)
        {
            byte reg = GetX(opcode);

            _v[reg] = _dt;
            _pc += 2;
        }

        // LD Vx, K
        [Opcode(Value = "Fx0A")]
        private void WaitForKeyPress(ushort opcode)
        {
            int index = Array.IndexOf(_keyboard, true);

            /* Execute the next instruction if a key is pressed. If a key is not pressed, then
             * the program counter will keep pointing to the same opcode in an infinite loop, waiting
             * for a key to be pressed. */
            if (index != -1)
            {
                int reg = GetX(opcode);
                _v[reg] = (byte)index;

                _pc += 2;
            }
        }

        // LD DT, Vx
        [Opcode(Value = "Fx15")]
        private void SetDelayTimer(ushort opcode)
        {
            byte reg = GetX(opcode);

            _dt = _v[reg];
            _pc += 2;
        }

        // LD ST, Vx
        [Opcode(Value = "Fx18")]
        private void SetSoundTimer(ushort opcode)
        {
            byte reg = GetX(opcode);

            _st = _v[reg];

            if (_st > 0)
            {
                if (SoundOn != null)
                {
                    SoundOn(this, EventArgs.Empty);
                }
            }

            _pc += 2;
        }

        // ADD I, Vx
        [Opcode(Value = "Fx1E")]
        private void AddRegisterToI(ushort opcode)
        {
            byte reg = GetX(opcode);
            _i += _v[reg];

            _pc += 2;
        }

        // LD F, Vx
        [Opcode(Value = "Fx29")]
        private void LoadFontSprite(ushort opcode)
        {
            byte reg = GetX(opcode);
            byte fontDigit = _v[reg];

            /* The font's sprites are stored in the interpreter area of chip8 memory (0x000 to 0x1FF), and
             * every sprite is 5 bytes long, or 8x5 pixels. */
            _i = (ushort)(fontDigit * 5);
            _pc += 2;
        }

        // LD B, Vx
        [Opcode(Value = "Fx33")]
        private void StoreBcd(ushort opcode)
        {
            byte reg = GetX(opcode);
            byte value = _v[reg];

            _memory[_i] = (byte)(value / 100);
            _memory[_i + 1] = (byte)((value % 100) / 10);
            _memory[_i + 2] = (byte)(value % 10);

            _pc += 2;
        }

        // LD [I], Vx
        [Opcode(Value = "Fx55")]
        private void StoreRegisters(ushort opcode)
        {
            byte reg = GetX(opcode);

            for (int i = 0; i <= reg; i++)
            {
                _memory[_i + i] = _v[i];
            }

            _pc += 2;
        }

        // LD Vx, [I]
        [Opcode(Value = "Fx65")]
        private void ReadRegisters(ushort opcode)
        {
            byte reg = GetX(opcode);

            for (int i = 0; i <= reg; i++)
            {
                _v[i] = _memory[_i + i];
            }

            _pc += 2;
        }
    }
}