using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mips32processor.Mips32
{
    public class MEM : Stage
    {
        private uint m_ir;
        private uint m_memout;
        private uint m_m2reg;
        private uint m_aluout;
        private uint m_wmem;
        private uint m_q2;

        public override void Initialize(ProcessorContext context)
        {
            base.Initialize(context);
        }

        public override bool Startup()
        {
            // Startup.
            if (m_context.GetNodeState("mem:started") == 0)
            {
                Console.WriteLine("MEM: Not Started");
                return false;
            }
            m_context.SetNodeState("wb:started", 1);

            // Stall.
            if (m_context.GetNodeState("mem:stall") == 1)
            {
                m_context.SetNodeState("wb:stall", 1);
                Console.WriteLine("MEM: Stall");
                return false;
            }
            m_context.SetNodeState("wb:stall", 0);

            return true;
        }

        public override bool Read()
        {
            m_ir = m_context.GetNodeState("mem:ir");
            m_m2reg = m_context.GetNodeState("mem:m2reg");
            m_aluout = m_context.GetNodeState("mem:aluout");
            m_wmem = m_context.GetNodeState("mem:wmem");
            m_q2 = m_context.GetNodeState("mem:q2");

            return true;
        }

        public override bool Pass()
        {
            m_context.SetNodeState("wb:ir", m_ir);
            m_context.PassNodeState("mem:wreg", "wb:wreg");
            m_context.SetNodeState("wb:m2reg", m_m2reg);
            m_context.PassNodeState("mem:regdst", "wb:regdst");
            m_context.SetNodeState("wb:aluout", m_aluout);

            return true;
        }

        public override bool Simulate()
        {
            if ((m_ir >> 26) == 0x3f)
            {
                Console.WriteLine("MEM: Halt");
                return false;
            }

            // Memory accessing.
            m_memout = 0;
            try
            {
                if (m_wmem == 0)
                    m_memout = m_context.DataCache[m_aluout];
                else
                    m_context.DataCache[m_aluout] = m_q2;
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("MEM: Warning: address is invalid");
            }

            Console.WriteLine("MEM: inst=0x{0:x}, alu_out(address)=0x{1:x}, mem_read=0x{2:x}, mem_write=0x{3:x}", m_ir, m_aluout, m_memout, m_q2);
            
            return true;
        }

        public override void Write()
        {
            m_context.SetNodeState("wb:memout", m_memout);
            if (m_context.GetRegister("q1") == 2)
            {
                m_context.SetRegister("q1", 0);
                m_context.SetNodeState("exe:q1", m_m2reg == 0 ? m_aluout : m_memout);
            }
            if (m_context.GetRegister("q2") == 2)
            {
                m_context.SetRegister("q2", 0);
                m_context.SetNodeState("exe:q2", m_m2reg == 0 ? m_aluout : m_memout);
            }
        }
    }
}
