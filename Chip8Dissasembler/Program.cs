using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Chip8Dissasembler
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: Chip8Disassembler program [initial address in hex].");
                return;
            }

            string program = args[0];
            int startAddress = -1;

            if (!File.Exists(program))
            {
                Console.WriteLine("Couldn't find program: {0}.", program);
                return;
            }

            if (args.Length == 2)
            {
                if (!int.TryParse(args[1], NumberStyles.HexNumber, CultureInfo.CurrentCulture, out startAddress))
                {
                    Console.WriteLine("Invalid initial address.");
                    return;
                }
            }

            Disassembler disassembler = new Disassembler();

            List<string> instructions;
           
            if (startAddress != -1) 
            {
                try
                {
                    instructions = disassembler.Disassemble(program, (ushort)startAddress).ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }
            }
            else
            {
                instructions = disassembler.Disassemble(program).ToList();
            }

            foreach (string instruction in instructions)
            {
                Console.WriteLine(instruction);
            }
        }
    }
}