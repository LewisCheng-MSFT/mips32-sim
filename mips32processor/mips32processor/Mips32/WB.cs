using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mips32processor.Mips32
{
    public class WB : Stage
    {
        private uint m_ir;
        private uint m_regdst;
        private uint m_wreg;
        private uint m_m2reg;
        private uint m_aluout;
        private uint m_memout;
        private uint m_regval;

        public override void Initialize(ProcessorContext context)
        {
            base.Initialize(context);
        }

        public override bool Startup()
        {
            // Startup.
            if (m_context.GetNodeState("wb:started") == 0)
            {
                Console.WriteLine("WB: Not Started");
                return false;
            }

            // Stall.
            if (m_context.GetNodeState("wb:stall") == 1)
            {
                Console.WriteLine("WB: Stall");
                return false;
            }

            return true;
        }

        public override bool Read()
        {
            m_ir = m_context.GetNodeState("wb:ir");
            m_regdst = m_context.GetNodeState("wb:regdst");
            m_wreg = m_context.GetNodeState("wb:wreg");
            m_m2reg = m_context.GetNodeState("wb:m2reg");
            m_aluout = m_context.GetNodeState("wb:aluout");
            m_memout = m_context.GetNodeState("wb:memout");

            return true;
        }

        public override bool Pass()
        {
            return true;
        }

        public override bool Simulate()
        {
            if ((m_ir >> 26) == 0x3f)
            {
                Console.WriteLine("WB: Halt");
                m_context.IsHalted = true;
                return false;
            }

            // Read WREG.
            if (m_wreg == 1)
            {
                m_regval = 0;
                if (m_m2reg == 0)
                    m_regval = m_aluout;
                else
                    m_regval = m_memout;

                Console.WriteLine("WB: inst=0x{0:x}, ${1}=0x{2:x}", m_ir, m_regdst, m_regval);
            }
            else
            {
                Console.WriteLine("WB: inst=0x{0:x}", m_ir);
            }

            return true;
        }

        public override void Write()
        {
            if (m_wreg == 1)
            {
                m_context.SetRegister(m_regdst.ToString(), m_regval);

                if (m_context.GetRegister("q1") == 3)
                {
                    m_context.SetRegister("q1", 0);
                    m_context.SetNodeState("exe:q1", m_regval);
                }

                if (m_context.GetRegister("q2") == 3)
                {
                    m_context.SetRegister("q2", 0);
                    m_context.SetNodeState("exe:q2", m_regval);
                }
            }
        }
    }
}
