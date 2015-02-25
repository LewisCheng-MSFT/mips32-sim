using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mips32processor.Mips32
{
    public class ID : Stage
    {
        private uint m_ir;
        private uint m_pc;
        private uint m_bpc;
        private uint m_rs;
        private uint m_rt;
        private uint m_rd;
        private uint m_imm;
        private uint m_eimm;
        private uint m_jmp;
        private uint m_sa;
        private uint m_op;
        private uint m_funct;
        private uint m_wreg;
        private uint m_m2reg;
        private uint m_wmem;
        private uint m_aluc;
        private uint m_aluimm;
        private uint m_shift;
        private uint m_regdst;
        private bool m_isBranch;
        private bool m_isBranchTaken;
        private uint m_q1;
        private uint m_q2;
        private uint m_fq1;
        private uint m_fq2;
        private uint m_exeRegDst;
        private uint m_exeStall;
        private uint m_exeWreg;
        private uint m_memRegDst;
        private uint m_memStall;
        private uint m_memWreg;
        private uint m_wbRegDst;
        private uint m_wbStall;
        private uint m_wbWreg;

        public override void Initialize(ProcessorContext context)
        {
            base.Initialize(context);
        }

        public override bool Startup()
        {
            // Startup.
            if (m_context.GetNodeState("id:started") == 0)
            {
                Console.WriteLine("ID: Not Started");
                return false;
            }
            m_context.SetNodeState("exe:started", 1);

            // Stall.
            uint stall = m_context.GetNodeState("id:stall");
            if (stall > 0)
            {
                m_context.SetNodeState("id:stall", stall - 1);
                m_context.SetNodeState("if:stall", 1);
                m_context.SetNodeState("exe:stall", 1);
                Console.WriteLine("ID: Stall");
                return false;
            }
            m_context.SetNodeState("if:stall", 0);
            m_context.SetNodeState("exe:stall", 0);

            return true;
        }

        public override bool Read()
        {
            m_pc = m_context.GetNodeState("id:pc");

            m_ir = m_context.GetNodeState("id:ir");
            m_rs = (m_ir >> 21) & 0x1f;
            m_rt = (m_ir >> 16) & 0x1f;
            m_rd = (m_ir >> 11) & 0x1f;
            m_imm = m_ir & 0xffff;
            m_eimm = (uint)(short)(m_ir & 0xffff);
            m_jmp = m_ir & 0x3ffffff;
            m_sa = (m_ir >> 6) & 0x1f;
            m_op = m_ir >> 26;
            m_funct = m_ir & 0x3f;

            m_fq1 = m_context.GetRegister("fq1");
            m_fq2 = m_context.GetRegister("fq2");
            m_q1 = m_fq1 == 0 ? m_context.GetRegister(m_rs.ToString()) : m_context.GetNodeState("exe:q1");
            m_q2 = m_fq2 == 0 ? m_context.GetRegister(m_rt.ToString()) : m_context.GetNodeState("exe:q2");
            m_context.SetRegister("fq1", 0);
            m_context.SetRegister("fq2", 0);

            m_exeRegDst = m_context.GetNodeState("exe:regdst");
            m_exeStall = m_context.GetNodeState("exe:stall");
            m_exeWreg = m_context.GetNodeState("exe:wreg");
            m_memRegDst = m_context.GetNodeState("mem:regdst");
            m_memStall = m_context.GetNodeState("mem:stall");
            m_memWreg = m_context.GetNodeState("mem:wreg");
            m_wbRegDst = m_context.GetNodeState("wb:regdst");
            m_wbStall = m_context.GetNodeState("wb:stall");
            m_wbWreg = m_context.GetNodeState("wb:wreg");
            return true;
        }

        public override bool Pass()
        {
            m_context.SetNodeState("exe:ir", m_ir);
            m_context.SetNodeState("exe:sa", m_sa);

            return true;
        }

        public override bool Simulate()
        {
            if ((m_ir >> 26) == 0x3f)
            {
                Console.WriteLine("ID: Halt");
                return false;
            }

            Console.WriteLine("ID: inst=0x{0:x}, op={1}, funct={2}, q1={3}, q2={4}, rs={5}, rt={6}", m_ir, m_op, m_funct, m_q1, m_q2, m_rs, m_rt);

            // Control logic.
            m_wreg = 0;
            m_m2reg = 0;
            m_wmem = 0;
            m_aluc = 0;
            m_aluimm = 0;
            m_shift = 0;
            m_regdst = 0;
            m_isBranch = false;
            m_isBranchTaken = false;
            switch (m_op)
            {
                case 0:
                    m_wreg = 1;
                    m_regdst = m_rd;
                    switch (m_funct)
                    {
                        case 32: // add
                            m_aluc = AluControl.AlucAdd;
                            break;

                        case 34: // sub
                            m_aluc = AluControl.AlucSub;
                            break;

                        case 36: // and
                            m_aluc = AluControl.AlucAnd;
                            break;

                        case 37: // or
                            m_aluc = AluControl.AlucOr;
                            break;

                        case 42: // slt
                            m_aluc = AluControl.AlucLessThan;
                            break;

                        case 0: // sll
                            m_aluc = AluControl.AlucShiftLeftLogic;
                            m_shift = 1;
                            break;

                        case 2: // srl
                            m_aluc = AluControl.AlucShiftRightLogic;
                            m_shift = 1;
                            break;

                        case 3: // sra
                            m_aluc = AluControl.AlucShiftRightArithmetic;
                            m_shift = 1;
                            break;

                        default:
                            throw new NotSupportedException("funct[" + m_funct + "] not supported!");
                    }
                    break;

                case 8: // addi
                    m_wreg = 1;
                    m_aluc = AluControl.AlucAdd;
                    m_aluimm = 1;
                    m_imm = m_eimm;
                    m_regdst = m_rt;
                    break;

                case 12: // andi
                    m_wreg = 1;
                    m_aluc = AluControl.AlucAnd;
                    m_aluimm = 1;
                    m_regdst = m_rt;
                    break;

                case 13: // ori
                    m_wreg = 1;
                    m_aluc = AluControl.AlucOr;
                    m_aluimm = 1;
                    m_regdst = m_rt;
                    break;

                case 35: // lw
                    m_wreg = 1;
                    m_m2reg = 1;
                    m_aluc = AluControl.AlucAdd;
                    m_aluimm = 1;
                    m_imm = m_eimm;
                    m_regdst = m_rt;
                    break;

                case 43: // sw
                    m_wmem = 1;
                    m_aluc = AluControl.AlucAdd;
                    m_aluimm = 1;
                    m_imm = m_eimm;
                    break;

                case 4: // beq
                    m_isBranch = true;
                    if (m_q1 == m_q2)
                    {
                        m_isBranchTaken = true;
                        m_bpc = m_pc + (m_eimm << 2);
                    }
                    break;

                case 5: // bne
                    m_isBranch = true;
                    if (m_q1 != m_q2)
                    {
                        m_isBranchTaken = true;
                        m_bpc = m_pc + (m_eimm << 2);
                    }
                    break;

                case 2: // j
                    m_isBranchTaken = true;
                    m_bpc = m_jmp << 2;
                    break;

                default:
                    throw new NotSupportedException("op[" + m_op + "] not supported!");
            }
            
            return true;
        }

        public override void Write()
        {
            if (m_isBranchTaken)
                m_context.SetNodeState("if:pc", m_bpc);

            HandleHazard();

            m_context.SetNodeState("exe:q1", m_q1);
            m_context.SetNodeState("exe:q2", m_q2);
            m_context.SetNodeState("exe:wreg", m_wreg);
            m_context.SetNodeState("exe:m2reg", m_m2reg);
            m_context.SetNodeState("exe:wmem", m_wmem);
            m_context.SetNodeState("exe:aluc", m_aluc);
            m_context.SetNodeState("exe:aluimm", m_aluimm);
            m_context.SetNodeState("exe:shift", m_shift);
            m_context.SetNodeState("exe:imm", m_imm);
            m_context.SetNodeState("exe:regdst", m_regdst);
        }

        private void HandleHazard()
        {
            if (m_exeStall == 0 && m_exeWreg == 1)
            { // Hazard with instruction in EXE.
                if (m_shift == 0 && m_rs == m_exeRegDst && m_fq1 == 0)
                {
                    Console.WriteLine("ID: Hazard with inst in EXE");
                    m_context.SetRegister("q1", 1);
                    if (m_isBranch)
                    {
                        m_context.PassNodeState("id:pc");
                        m_context.PassNodeState("id:ir");
                        m_context.SetNodeState("if:pc", m_pc);
                        m_context.SetRegister("fq1", 1);
                        m_context.SetNodeState("id:stall", 0);
                        m_context.SetNodeState("if:stall", 1);
                        m_context.SetNodeState("exe:stall", 1);
                    }
                }
                if (m_aluimm == 0 && m_rt == m_exeRegDst && m_fq2 == 0)
                {
                    Console.WriteLine("ID: Hazard with inst in EXE");
                    m_context.SetRegister("q2", 1);
                    if (m_isBranch)
                    {
                        m_context.PassNodeState("id:pc");
                        m_context.PassNodeState("id:ir");
                        m_context.SetNodeState("if:pc", m_pc);
                        m_context.SetRegister("fq2", 1);
                        m_context.SetNodeState("id:stall", 0);
                        m_context.SetNodeState("if:stall", 1);
                        m_context.SetNodeState("exe:stall", 1);
                    }
                }
            }

            if (m_memStall == 0 && m_memWreg == 1)
            { // Hazard with instruction in MEM, stall 2 cycle.
                if (m_shift == 0 && m_rs == m_memRegDst && m_fq1 == 0)
                {
                    Console.WriteLine("ID: Hazard with inst in MEM");
                    m_context.SetRegister("q1", 2);
                    // lw will produce result by the end of next cycle.
                    if (m_isBranch || m_op == 35)
                    {
                        m_context.PassNodeState("id:pc");
                        m_context.PassNodeState("id:ir");
                        m_context.SetNodeState("if:pc", m_pc);
                        m_context.SetRegister("fq1", 1);
                        m_context.SetNodeState("id:stall", 0);
                        m_context.SetNodeState("if:stall", 1);
                        m_context.SetNodeState("exe:stall", 1);
                    }
                }
                // Pay attention!
                // Though aluimm != 0, 'sw' will make use of q2, so hazard is still possible.
                // So here we must consider 'sw' alone.
                if ((m_op == 43 || m_aluimm == 0) && m_rt == m_memRegDst && m_fq2 == 0)
                {
                    Console.WriteLine("ID: Hazard with inst in MEM");
                    m_context.SetRegister("q2", 2);
                    if (m_isBranch)
                    {
                        m_context.PassNodeState("id:pc");
                        m_context.PassNodeState("id:ir");
                        m_context.SetNodeState("if:pc", m_pc);
                        m_context.SetRegister("fq2", 1);
                        m_context.SetNodeState("id:stall", 0);
                        m_context.SetNodeState("if:stall", 1);
                        m_context.SetNodeState("exe:stall", 1);
                    }
                }
            }

            if (m_wbStall == 0 && m_wbWreg == 1)
            { // Hazard with instruction in WB, stall 1 cycle.
                if (m_shift == 0 && m_rs == m_wbRegDst && m_fq1 == 0)
                {
                    Console.WriteLine("ID: Hazard with inst in WB");
                    m_context.SetRegister("q1", 3);
                    if (m_isBranch)
                    {
                        m_context.PassNodeState("id:pc");
                        m_context.PassNodeState("id:ir");
                        m_context.SetNodeState("if:pc", m_pc);
                        m_context.SetRegister("fq1", 1);
                        m_context.SetNodeState("id:stall", 0);
                        m_context.SetNodeState("if:stall", 1);
                        m_context.SetNodeState("exe:stall", 1);
                    }
                }
                if (m_aluimm == 0 && m_rt == m_wbRegDst && m_fq2 == 0)
                {
                    Console.WriteLine("ID: Hazard with inst in WB");
                    m_context.SetRegister("q2", 3);
                    if (m_isBranch)
                    {
                        m_context.PassNodeState("id:pc");
                        m_context.PassNodeState("id:ir");
                        m_context.SetNodeState("if:pc", m_pc);
                        m_context.SetRegister("fq2", 1);
                        m_context.SetNodeState("id:stall", 0);
                        m_context.SetNodeState("if:stall", 1);
                        m_context.SetNodeState("exe:stall", 1);
                    }
                }
            }
        }
    }
}
