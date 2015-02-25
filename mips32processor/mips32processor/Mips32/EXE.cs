using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mips32processor.Mips32
{
    public class EXE : Stage
    {
        private uint m_ir;
        private uint m_shift;
        private uint m_aluimm;
        private uint m_aluc;
        private uint m_aluout;
        private uint m_q1;
        private uint m_q2;
        private uint m_sa;
        private uint m_imm;

        public override void Initialize(ProcessorContext context)
        {
            base.Initialize(context);
        }

        public override bool Startup()
        {
            // Startup.
            if (m_context.GetNodeState("exe:started") == 0)
            {
                Console.WriteLine("EXE: Not Started");
                return false;
            }
            m_context.SetNodeState("mem:started", 1);

            // Stall.
            if (m_context.GetNodeState("exe:stall") == 1)
            {
                m_context.SetNodeState("mem:stall", 1);
                Console.WriteLine("EXE: Stall");
                return false;
            }
            m_context.SetNodeState("mem:stall", 0);

            return true;
        }

        public override bool Read()
        {
            m_ir = m_context.GetNodeState("exe:ir");
            m_shift = m_context.GetNodeState("exe:shift");
            m_aluimm = m_context.GetNodeState("exe:aluimm");
            m_aluc = m_context.GetNodeState("exe:aluc");
            m_q1 = m_context.GetNodeState("exe:q1");
            m_q2 = m_context.GetNodeState("exe:q2");
            m_sa = m_context.GetNodeState("exe:sa");
            m_imm = m_context.GetNodeState("exe:imm");

            return true;
        }

        public override bool Pass()
        {
            m_context.SetNodeState("mem:ir", m_ir);
            m_context.PassNodeState("exe:wreg", "mem:wreg");
            m_context.PassNodeState("exe:m2reg", "mem:m2reg");
            m_context.PassNodeState("exe:wmem", "mem:wmem");
            m_context.PassNodeState("exe:regdst", "mem:regdst");
            m_context.SetNodeState("mem:q2", m_q2);

            return true;
        }

        public override bool Simulate()
        {
            if ((m_ir >> 26) == 0x3f)
            {
                Console.WriteLine("EXE: Halt");
                return false;
            }

            // Alu a.
            uint alua = 0;
            switch (m_shift)
            {
                case 0:
                    alua = m_q1;
                    break;
                case 1:
                    alua = m_sa;
                    break;
            }

            // Alu b.
            uint alub = 0;
            switch (m_aluimm)
            {
                case 0:
                    alub = m_q2;
                    break;
                case 1:
                    alub = m_imm;
                    break;
            }

            // Alu control.
            m_aluout = 0;
            switch (m_aluc)
            {
                case AluControl.AlucAdd:
                    m_aluout = alua + alub;
                    break;
                case AluControl.AlucSub:
                    m_aluout = alua - alub;
                    break;
                case AluControl.AlucLessThan:
                    m_aluout = alua < alub ? 1u : 0u;
                    break;
                case AluControl.AlucAnd:
                    m_aluout = alua & alub;
                    break;
                case AluControl.AlucOr:
                    m_aluout = alua | alub;
                    break;
                case AluControl.AlucShiftLeftLogic:
                    m_aluout = alub << (int)alua;
                    break;
                case AluControl.AlucShiftRightLogic:
                    m_aluout = alub >> (int)alua;
                    break;
                case AluControl.AlucShiftRightArithmetic:
                    // Pay attention to arithmetic shift-right.
                    int quotient = (int)alub;
                    while (alua-- > 0)
                        quotient /= 2;
                    m_aluout = (uint)quotient;
                    break;
                default:
                    throw new NotSupportedException("aluc[" + m_aluc + "] not supported!");
            }

            Console.WriteLine("EXE: inst=0x{0:x}, aluc={1}, alu_a=0x{2:x}, alu_b=0x{3:x}, alu_out=0x{4:x}", m_ir, m_aluc, alua, alub, m_aluout);
            
            return true;
        }

        public override void Write()
        {
            m_context.SetNodeState("mem:aluout", m_aluout);

            if (m_context.GetRegister("q1") == 1)
            {
                m_context.SetRegister("q1", 0);
                m_context.SetNodeState("exe:q1", m_aluout);
            }

            if (m_context.GetRegister("q2") == 1)
            {
                m_context.SetRegister("q2", 0);
                m_context.SetNodeState("exe:q2", m_aluout);
            }
        }
    }
}
