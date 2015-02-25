using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mips32processor.Mips32
{
    public class MEMComponent : IProcessorComponent
    {
        public IList<IProcessorComponent> SubComponents { get; set; }

        public void Initialize(ProcessorContext context)
        {

        }

        public void Pulse(ProcessorContext context)
        {
            // Startup.
            uint started = context.GetNodeState("mem:started");
            if (started == 0)
            {
                Console.WriteLine("MEM: Not Started");
                return;
            }
            context.SetNodeState("wb:started", 1);

            // Stall.
            uint stall = context.GetNodeState("mem:stall");
            if (stall == 1)
            {
                context.SetNodeState("wb:stall", 1);
                Console.WriteLine("MEM: Stall");
                return;
            }
            context.SetNodeState("wb:stall", 0);

            // Read IR.
            uint ir = context.GetNodeState("mem:ir");

            // Pass IR.
            context.SetNodeState("wb:ir", ir);

            if ((ir >> 26) == 0x3f)
            {
                Console.WriteLine("MEM: Halt");
                return;
            }

            // Pass WREG.
            context.PassNodeState("mem:wreg", "wb:wreg");

            // Pass M2REG.
            uint m2reg = context.GetNodeState("mem:m2reg");
            context.SetNodeState("wb:m2reg", m2reg);

            // Pass REGDST.
            context.PassNodeState("mem:regdst", "wb:regdst");

            // Pass ALUOUT.
            uint aluout = context.GetNodeState("mem:aluout");
            context.SetNodeState("wb:aluout", aluout);

            // Read WMEM.
            uint wmem = context.GetNodeState("mem:wmem");
            
            // Read Q2.
            uint q2 = context.GetNodeState("mem:q2");

            // Memory accessing.
            uint memread = 0;
            try
            {
                uint index = aluout >> 2;
                if (wmem == 0)
                    memread = context.DataMemory[index];
                else
                    context.DataMemory[index] = q2;
            }
            catch (IndexOutOfRangeException)
            {
            }
            context.SetNodeState("wb:memout", memread);
            if (context.GetRegister("q1") == 2)
            {
                context.SetRegister("q1", 0);
                context.SetNodeState("exe:q1", m2reg == 0 ? aluout : memread);
            }
            if (context.GetRegister("q2") == 2)
            {
                context.SetRegister("q2", 0);
                context.SetNodeState("exe:q2", m2reg == 0 ? aluout : memread);
            }

            Console.WriteLine("MEM: inst=0x{0:x}, alu_out(address)=0x{1:x}, mem_read=0x{2:x}, mem_write=0x{3:x}", ir, aluout, memread, q2);
        }
    }
}
