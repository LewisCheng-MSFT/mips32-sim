using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mips32processor.Mips32
{
    public class IF : Stage
    {
        private uint m_pc;
        private uint m_ir;

        public override void Initialize(ProcessorContext context)
        {
            base.Initialize(context);
        }

        public override bool Startup()
        {
            // Startup.
            m_context.SetNodeState("id:started", 1);

            // Stall.
            if (m_context.GetNodeState("if:stall") == 1)
            {
                m_context.PassNodeState("if:pc");
                m_context.SetNodeState("id:stall", 1);
                Console.WriteLine("IF: Stall");
                return false;
            }

            return true;
        }

        public override bool Read()
        {
            m_pc = m_context.GetNodeState("if:pc");

            try
            {
                m_ir = m_context.InstMemory[m_pc >> 2];
                return true;
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("IF: Nothing fetched");
                return false;
            }
        }

        public override bool Pass()
        {
            m_context.SetNodeState("id:ir", m_ir);

            return true;
        }

        public override bool Simulate()
        {
            if ((m_ir >> 26) == 0x3f)
            {
                Console.WriteLine("IF: Halt");
                return false;
            }

            Console.WriteLine("IF: inst=0x{0:x}, pc=0x{1:x}", m_ir, m_pc);

            m_pc += 4;

            return true;
        }

        public override void Write()
        {
            m_context.SetNodeState("if:pc", m_pc);
            m_context.SetNodeState("id:pc", m_pc);
        }
    }
}
