using System;
using System.Collections.Generic;
using System.IO;

namespace Chip8Dissasembler
{
    public class Disassembler
    {
        private static readonly Dictionary<int, Func<Disassembler, ushort, string>> Opcodes =
            new Dictionary<int, Func<Disassembler, ushort, string>>
            {
                {0x00E0, (@this, opcode) => @this.ClearScreen(opcode)},
                {0x00EE, (@this, opcode) => @this.Return(opcode)},
                {0x1000, (@this, opcode) => @this.Jump(opcode)},
                {0x2000, (@this, opcode) => @this.Call(opcode)},
                {0x3000, (@this, opcode) => @this.SkipNextEqualByte(opcode)},
                {0x4000, (@this, opcode) => @this.SkipNextNotEqualByte(opcode)},
                {0x5000, (@this, opcode) => @this.SkipNextEqualRegisters(opcode)},
                {0x6000, (@this, opcode) => @this.LoadRegisterWithByte(opcode)},
                {0x7000, (@this, opcode) => @this.AddByteToRegister(opcode)},
                {0x8000, (@this, opcode) => @this.LoadRegisterWithRegister(opcode)},
                {0x8001, (@this, opcode) => @this.LoadRegisterWithByte(opcode)},
                {0x8002, (@this, opcode) => @this.AndRegisters(opcode)},
                {0x8003, (@this, opcode) => @this.XorRegisters(opcode)},
                {0x8004, (@this, opcode) => @this.AddRegisters(opcode)},
                {0x8005, (@this, opcode) => @this.SubRegisters(opcode)},
                {0x8006, (@this, opcode) => @this.ShifRightRegister(opcode)},
                {0x8007, (@this, opcode) => @this.SubRegistersInverted(opcode)},
                {0x800E, (@this, opcode) => @this.ShifLeftRegisters(opcode)},
                {0x9000, (@this, opcode) => @this.SkipNextNotEqualRegisters(opcode)},
                {0xA000, (@this, opcode) => @this.LoadIWithAddress(opcode)},
                {0xB000, (@this, opcode) => @this.JumpToAddressPlusRegister(opcode)},
                {0xC000, (@this, opcode) => @this.GenerateRandomNumber(opcode)},
                {0xD000, (@this, opcode) => @this.DrawSprite(opcode)},
                {0xE09E, (@this, opcode) => @this.SkipNextIfKeyPressed(opcode)},
                {0xE0A1, (@this, opcode) => @this.SkipNextIfKeyNotPressed(opcode)},
                {0xF007, (@this, opcode) => @this.LoadRegisterWithDelayTimer(opcode)},
                {0xF00A, (@this, opcode) => @this.WaitForKeyPress(opcode)},
                {0xF015, (@this, opcode) => @this.SetDelayTimer(opcode)},
                {0xF018, (@this, opcode) => @this.SetSoundTimer(opcode)},
                {0xF01E, (@this, opcode) => @this.AddRegisterToI(opcode)},
                {0xF029, (@this, opcode) => @this.LoadFontSprite(opcode)},
                {0xF033, (@this, opcode) => @this.StoreBcd(opcode)},
                {0xF055, (@this, opcode) => @this.StoreRegisters(opcode)},
                {0xF065, (@this, opcode) => @this.ReadRegisters(opcode)}
            };

        private static readonly int[] Masks = {0xF0FF, 0xF00F, 0xF000};

        public IEnumerable<string> Disassemble(string pathToProgram, ushort startAddress = 0x200)
        {
            if (startAddress < 0x200 || startAddress > 0xFFF)
            {
                throw new ArgumentException("Initial address must be between 0x200 and 0xFFF.");
            }

            ushort offset = (ushort)(startAddress - 0x200);
            ushort pc = (ushort)(0x200 + offset);

            List<string> instructions = new List<string>
            {
                "Address\tOpcode\tInstruction"
            };

            using (Stream stream = File.OpenRead(pathToProgram))
            {
                byte[] opcode = new byte[2];
                int bytesRead;

                if (offset > 0)
                {
                    stream.Seek(offset, SeekOrigin.Begin);
                }

                do
                {
                    bytesRead = stream.Read(opcode, 0, 2);

                    string instruction = DisassembleInstruction(pc, (ushort)((opcode[0] << 8) | opcode[1]));
                    instructions.Add(instruction);

                    pc += 2;
                } while (bytesRead != 0);
            }

            return instructions;
        }

        private string DisassembleInstruction(ushort pc, ushort opcode)
        {
            Func<Disassembler, ushort, string> value = null;
            int i = 0;

            while (i < Masks.Length && value == null)
            {
                Opcodes.TryGetValue(opcode & Masks[i], out value);
                i++;
            }

            if (value != null)
            {
                return FormatInstruction(pc, opcode, value(this, opcode));
            }

            return FormatInstruction(pc, opcode, "UNKNOWN");
        }

        private string FormatInstruction(ushort pc, ushort opcode, string instruction)
        {
            return string.Format("{0:X4}\t{1:X4}\t{2}", pc, opcode, instruction);
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

        // 00E0 - CLS
        private string ClearScreen(ushort opcode)
        {
            return "CLS";
        }

        // 00EE - RET
        private string Return(ushort opcode)
        {
            return "RET";
        }

        // 1nnn - JP addr
        private string Jump(ushort opcode)
        {
            ushort address = GetAddress(opcode);
            return string.Format("JP {0:X4}", address);
        }

        // 2nnn - CALL addr
        private string Call(ushort opcode)
        {
            ushort address = GetAddress(opcode);
            return string.Format("CALL {0:X4}", address);
        }

        // 3xkk - SE Vx, byte
        private string SkipNextEqualByte(ushort opcode)
        {
            byte reg = GetX(opcode);
            byte value = GetByte(opcode);
            return string.Format("SE V{0:X1}, {1:X2}", reg, value);
        }

        // 4xkk - SNE Vx, byte
        private string SkipNextNotEqualByte(ushort opcode)
        {
            byte reg = GetX(opcode);
            byte value = GetByte(opcode);
            return string.Format("SNE V{0:X1}, {1:X2}", reg, value);
        }

        // 5xy0 - SE Vx, Vy
        private string SkipNextEqualRegisters(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);
            return string.Format("SE V{0:X1}, V{1:X1}", reg1, reg2);
        }

        // 6xkk - LD Vx, byte
        private string LoadRegisterWithByte(ushort opcode)
        {
            byte reg = GetX(opcode);
            byte value = GetByte(opcode);
            return string.Format("LD V{0:X1}, {1:X2}", reg, value);
        }

        // 7xkk - ADD Vx, byte
        private string AddByteToRegister(ushort opcode)
        {
            byte reg = GetX(opcode);
            byte value = GetByte(opcode);
            return string.Format("ADD V{0:X1}, {1:X2}", reg, value);
        }

        // 8xy0 - LD Vx, Vy
        private string LoadRegisterWithRegister(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);
            return string.Format("LD V{0:X1}, V{1:X1}", reg1, reg2);
        }

        // 8xy1 - OR Vx, Vy
        private string OrRegisters(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);
            return string.Format("OR V{0:X1}, V{1:X1}", reg1, reg2);
        }

        // 8xy2 - AND Vx, Vy
        private string AndRegisters(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);
            return string.Format("AND V{0:X1}, V{1:X1}", reg1, reg2);
        }

        // 8xy3 - XOR Vx, Vy
        private string XorRegisters(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);
            return string.Format("XOR V{0:X1}, {1:X1}", reg1, reg2);
        }

        // 8xy4 - ADD Vx, Vy
        private string AddRegisters(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);
            return string.Format("ADD V{0:X1}, V{1:X1}", reg1, reg2);
        }

        // 8xy5 - SUB Vx, Vy
        private string SubRegisters(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);
            return string.Format("SUB V{0:X1}, V{1:X1}", reg1, reg2);
        }

        // 8xy6 - SHR Vx {, Vy}
        private string ShifRightRegister(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);
            return string.Format("SHR V{0:X1}, V{1:X1}", reg1, reg2);
        }

        // 8xy7 - SUBN Vx, Vy
        private string SubRegistersInverted(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);
            return string.Format("SUBN V{0:X1}, V{1:X1}", reg1, reg2);
        }

        // 8xyE - SHL Vx {, Vy}
        private string ShifLeftRegisters(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);
            return string.Format("SHL V{0:X1}, V{1:X1}", reg1, reg2);
        }

        // 9xy0 - SNE Vx, Vy
        private string SkipNextNotEqualRegisters(ushort opcode)
        {
            byte reg1 = GetX(opcode);
            byte reg2 = GetY(opcode);
            return string.Format("SNE V{0:X1}, V{1:X1}", reg1, reg2);
        }

        // Annn - LD I, addr
        private string LoadIWithAddress(ushort opcode)
        {
            ushort address = GetAddress(opcode);
            return string.Format("LD I, {0:X4}", address);
        }

        // Bnnn - JP V0, addr
        private string JumpToAddressPlusRegister(ushort opcode)
        {
            ushort address = GetAddress(opcode);
            return string.Format("JP V0, {0:X4}", address);
        }

        // Cxkk - RND Vx, byte
        private string GenerateRandomNumber(ushort opcode)
        {
            byte reg = GetX(opcode);
            byte value = GetByte(opcode);
            return string.Format("RND V{0:X1}, V{1:X1}", reg, value);
        }

        // Dxyn - DRW Vx, Vy, nibble
        private string DrawSprite(ushort opcode)
        {
            byte regX = GetX(opcode);
            byte regY = GetY(opcode);
            byte value = GetLastNibble(opcode);
            return string.Format("DRW V{0:X1}, V{1:X1}, {2:X1}", regX, regY, value);
        }

        // Ex9E - SKP Vx
        private string SkipNextIfKeyPressed(ushort opcode)
        {
            byte reg = GetX(opcode);
            return string.Format("SKP V{0:X1}", reg);
        }

        // ExA1 - SKNP Vx
        private string SkipNextIfKeyNotPressed(ushort opcode)
        {
            byte reg = GetX(opcode);
            return string.Format("SKNP V{0:X1}", reg);
        }

        // Fx07 - LD Vx, DT
        private string LoadRegisterWithDelayTimer(ushort opcode)
        {
            byte reg = GetX(opcode);
            return string.Format("LD V{0:X1}, DT", reg);
        }

        // Fx0A - LD Vx, K
        private string WaitForKeyPress(ushort opcode)
        {
            int reg = GetX(opcode);
            return string.Format("LD V{0:X1}, K", reg);
        }

        // Fx15 - LD DT, Vx
        private string SetDelayTimer(ushort opcode)
        {
            byte reg = GetX(opcode);
            return string.Format("LD DT, V{0:X1}", reg);
        }

        // Fx18 - LD ST, Vx
        private string SetSoundTimer(ushort opcode)
        {
            byte reg = GetX(opcode);
            return string.Format("LD ST, V{0:X1}", reg);
        }

        // Fx1E - ADD I, Vx
        private string AddRegisterToI(ushort opcode)
        {
            byte reg = GetX(opcode);
            return string.Format("ADD I, V{0:X1}", reg);
        }

        // Fx29 - LD F, Vx
        private string LoadFontSprite(ushort opcode)
        {
            byte reg = GetX(opcode);
            return string.Format("LD F, V{0:X1}", reg);
        }

        // Fx33 - LD B, Vx
        private string StoreBcd(ushort opcode)
        {
            byte reg = GetX(opcode);
            return string.Format("LD B, V{0:X1}", reg);
        }

        // Fx55 - LD [I], Vx
        private string StoreRegisters(ushort opcode)
        {
            byte reg = GetX(opcode);
            return string.Format("LD [I], V{0:X1}", reg);
        }

        // Fx65 - LD Vx, [I]
        private string ReadRegisters(ushort opcode)
        {
            byte reg = GetX(opcode);
            return string.Format("LD V{0:X1}, [I]", reg);
        }
    }
}