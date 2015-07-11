using System.Windows.Forms;

namespace Chip8EmulatorGui
{
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
        }
    }
}