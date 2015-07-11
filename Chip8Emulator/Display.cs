using System.Collections;

namespace Chip8Emulator
{
    public class Display
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        private readonly BitArray[] _display;

        public Display(int width, int height)
        {
            Width = width;
            Height = height;

            _display = new BitArray[Height];
            for (int i = 0; i < Height; i++)
            {
                _display[i] = new BitArray(Width);
            }
        }

        public void SetByte(int x, int y, byte value)
        {
            BitArray bitArray = _display[y];
            int posX = x;

            for (int i = 7; i >= 0; i--)
            {
                // If we are off the screen around the x axis, don't draw anymore.
                if (posX >= Width)
                {
                    break;
                }

                bitArray[posX] = ((value >> i) & 1) == 1;
                posX++;
            }
        }

        public byte GetByte(int x, int y)
        {
            BitArray bitArray = _display[y];
            int posX = x;
            byte value = 0;

            for (int i = 7; i >= 0; i--)
            {
                if (posX >= Width)
                {
                    break;
                }

                value = (byte)(((bitArray[posX] ? 1 : 0) << i) | value);
                posX++;
            }

            return value;
        }

        public bool GetPixel(int x, int y)
        {
            BitArray bitArray = _display[y];
            return bitArray[x];
        }

        public void Clear()
        {
            for (int i = 0; i < Height; i++)
            {
                _display[i].SetAll(false);
            }
        }
    }
}