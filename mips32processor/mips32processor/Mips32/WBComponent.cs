using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mips32processor.Mips32
{
    public class WBComponent : IProcessorComponent
    {
        public IList<IProcessorComponent> SubComponents { get; set; }

        public void Initialize(ProcessorContext context)
        {

        }

        public void Pulse(ProcessorContext context)
        {
            // Startup.
            uint started = context.GetNodeState("wb:started");
            if (started == 0)
            {
                Console.WriteLine("WB: Not Started");
                return;
            }

            // Stall.
            uint stall = context.GetNodeState("wb:stall");
            if (stall == 1)
            {
                Console.WriteLine("WB: Stall");
                return;
            }

            // Read IR.
            uint ir = context.GetNodeState("wb:ir");

            if ((ir >> 26) == 0x3f)
            {
                Console.WriteLine("WB: Stall");
                context.IsHalted = true;
                return;
            }

            // Read REGDST.
            uint regdst = context.GetNodeState("wb:regdst");

            // Read WREG.
            uint wreg = context.GetNodeState("wb:wreg");
            if (wreg == 1)
            {
                uint m2reg = context.GetNodeState("wb:m2reg");
                if (m2reg == 0)
                {
                    uint aluout = context.GetNodeState("wb:aluout");
                    context.SetRegister(regdst.ToString(), aluout);
                    Console.WriteLine("WB: inst=0x{0:x}, ${1}=0x{2:x}", ir, regdst, aluout);
                }
                else
                {
                    uint memout = context.GetNodeState("wb:memout");
                    context.SetRegister(regdst.ToString(), memout);
                    Console.WriteLine("WB: inst=0x{0:x}, ${1}=0x{2:x}", ir, regdst, memout);
                }
            }
            else
            {
                Console.WriteLine("WB: inst=0x{0:x}", ir);
            }
        }
    }
}
