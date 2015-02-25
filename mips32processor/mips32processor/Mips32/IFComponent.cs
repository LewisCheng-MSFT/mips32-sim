using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mips32processor.Mips32
{
    public class IFComponent : IProcessorComponent
    {
        public IList<IProcessorComponent> SubComponents { get; set; }

        public void Initialize(ProcessorContext context)
        {

        }

        public void Pulse(ProcessorContext context)
        {
            // Startup.
            context.SetNodeState("id:started", 1);

            // Stall.
            uint stall = context.GetNodeState("if:stall");
            if (stall == 1)
            {
                context.PassNodeState("if:pc");
                context.SetNodeState("id:stall", 1);
                Console.WriteLine("IF: Stall");
                return;
            }

            // Read PC.
            uint pc = context.GetNodeState("if:pc");

            try
            {
                // Load instruction.
                uint ir = context.InstMemory[pc >> 2];

                // Pass IR.
                context.SetNodeState("id:ir", ir);

                if ((ir >> 26) == 0x3f)
                {
                    Console.WriteLine("IF: Stall");
                    return;
                }

                Console.WriteLine("IF: inst=0x{0:x}, pc=0x{1:x}", ir, pc);

                // Increment PC.
                pc += 4;
                context.SetNodeState("if:pc", pc);
                context.SetNodeState("id:pc", pc);
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("IF: Nothing fetched");
            }
        }
    }
}
