using System;

namespace Chip8Emulator
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class OpcodeAttribute : Attribute
    {
        public string Value { get; set; }
    }
}