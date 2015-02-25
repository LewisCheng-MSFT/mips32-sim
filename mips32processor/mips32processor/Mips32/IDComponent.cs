using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mips32processor.Mips32
{
    public class IDComponent : IProcessorComponent
    {
        public IList<IProcessorComponent> SubComponents { get; set; }

        public void Initialize(ProcessorContext context)
        {

        }

        public void Pulse(ProcessorContext context)
        {
            // Startup.
            uint started = context.GetNodeState("id:started");
            if (started == 0)
            {
                Console.WriteLine("ID: Not Started");
                return;
            }
            context.SetNodeState("exe:started", 1);

            // Stall.
            uint stall = context.GetNodeState("id:stall");
            if (stall > 0)
            {
                context.SetNodeState("id:stall", stall - 1);
                context.SetNodeState("if:stall", 1);
                context.SetNodeState("exe:stall", 1);
                Console.WriteLine("ID: Stall");
                return;
            }
            context.SetNodeState("if:stall", 0);
            context.SetNodeState("exe:stall", 0);

            // Read PC.
            uint pc = context.GetNodeState("id:pc");
            
            // Read IR.
            uint ir = context.GetNodeState("id:ir");

            // Pass IR.
            context.SetNodeState("exe:ir", ir);

            if ((ir >> 26) == 0x3f)
            {
                Console.WriteLine("ID: Halt");
                return;
            }
            
            // Extract rs, rt and rd.
            uint rs = (ir >> 21) & 0x1f;
            uint rt = (ir >> 16) & 0x1f;
            uint rd = (ir >> 11) & 0x1f;

            // imm.
            uint imm = ir & 0xffff;
            uint eimm = (uint)(short)(ir & 0xffff);

            // jump target.
            uint jmp = ir & 0x3ffffff;

            // sa.
            uint sa = (ir >> 6) & 0x1f;
            context.SetNodeState("exe:sa", sa);

            // q1 and q2.
            uint fq1 = context.GetRegister("fq1");
            uint fq2 = context.GetRegister("fq2");
            uint q1 = fq1 == 0 ? context.GetRegister(rs.ToString()) : context.GetNodeState("exe:q1");
            uint q2 = fq2 == 0 ? context.GetRegister(rt.ToString()) : context.GetNodeState("exe:q2");
            context.SetRegister("fq1", 0);
            context.SetRegister("fq2", 0);
            context.SetNodeState("exe:q1", q1);
            context.SetNodeState("exe:q2", q2);

            // Extract op and funct.
            uint op = ir >> 26;
            uint funct = ir & 0x3f;
            Console.WriteLine("ID: inst=0x{0:x}, op={1}, funct={2}, q1={3}, q2={4}, rs={5}, rt={6}", ir, op, funct, q1, q2, rs, rt);

            // Control logic.
            uint wreg = 0;
            uint m2reg = 0;
            uint wmem = 0;
            uint aluc = 0;
            uint aluimm = 0;
            uint shift = 0;
            uint regdst = 0;
            bool isBranch = false;
            switch (op)
            {
                case 0:
                    wreg = 1;
                    regdst = rd;
                    switch (funct)
                    {
                        case 32: // add
                            aluc = AluControl.AlucAdd;
                            break;

                        case 34: // sub
                            aluc = AluControl.AlucSub;
                            break;

                        case 36: // and
                            aluc = AluControl.AlucAnd;
                            break;

                        case 37: // or
                            aluc = AluControl.AlucOr;
                            break;

                        case 42: // slt
                            aluc = AluControl.AlucLessThan;
                            break;

                        case 0: // sll
                            aluc = AluControl.AlucShiftLeftLogic;
                            shift = 1;
                            break;

                        case 2: // srl
                            aluc = AluControl.AlucShiftRightLogic;
                            shift = 1;
                            break;

                        case 3: // sra
                            aluc = AluControl.AlucShiftRightArithmetic;
                            shift = 1;
                            break;

                        default:
                            throw new NotSupportedException("funct[" + funct + "] not supported!");
                    }
                    break;

                case 8: // addi
                    wreg = 1;
                    aluc = AluControl.AlucAdd;
                    aluimm = 1;
                    imm = eimm;
                    regdst = rt;
                    break;

                case 12: // andi
                    wreg = 1;
                    aluc = AluControl.AlucAnd;
                    aluimm = 1;
                    regdst = rt;
                    break;

                case 13: // ori
                    wreg = 1;
                    aluc = AluControl.AlucOr;
                    aluimm = 1;
                    regdst = rt;
                    break;

                case 35: // lw
                    wreg = 1;
                    m2reg = 1;
                    aluc = AluControl.AlucAdd;
                    aluimm = 1;
                    imm = eimm;
                    regdst = rt;
                    break;

                case 43: // sw
                    wmem = 1;
                    aluc = AluControl.AlucAdd;
                    aluimm = 1;
                    imm = eimm;
                    break;

                case 4: // beq
                    isBranch = true;
                    if (q1 == q2)
                        context.SetNodeState("if:pc", pc + (eimm << 2));
                    break;

                case 5: // bne
                    isBranch = true;
                    if (q1 != q2)
                        context.SetNodeState("if:pc", pc + (eimm << 2));
                    break;

                case 2: // j
                    context.SetNodeState("if:pc", jmp << 2);
                    break;

                default:
                    throw new NotSupportedException("op[" + op +"] not supported!");
            }

            bool hasHazard = false;

            // Detect hazard.
            uint prevRegdst = context.GetNodeState("exe:regdst");
            if (context.GetNodeState("exe:stall") == 0 && context.GetNodeState("exe:wreg") == 1)
            { // Hazard with instruction in EXE.
                if (shift == 0 && rs == prevRegdst && fq1 == 0)
                {
                    Console.WriteLine("ID: Hazard with inst in EXE");
                    //hasHazard = true;
                    context.SetRegister("q1", 1);
                    if (isBranch)
                    {
                        context.PassNodeState("id:pc");
                        context.PassNodeState("id:ir");
                        context.SetNodeState("if:pc", pc);
                        context.SetRegister("fq1", 1);
                        context.SetNodeState("id:stall", 0);
                        context.SetNodeState("if:stall", 1);
                        context.SetNodeState("exe:stall", 1);
                    }
                }
                if (aluimm == 0 && rt == prevRegdst && fq2 == 0)
                {
                    Console.WriteLine("ID: Hazard with inst in EXE");
                    context.SetRegister("q2", 1);
                    //hasHazard = true;
                    if (isBranch)
                    {
                        context.PassNodeState("id:pc");
                        context.PassNodeState("id:ir");
                        context.SetNodeState("if:pc", pc);
                        context.SetRegister("fq2", 1);
                        context.SetNodeState("id:stall", 0);
                        context.SetNodeState("if:stall", 1);
                        context.SetNodeState("exe:stall", 1);
                    }
                }
            }

            prevRegdst = context.GetNodeState("mem:regdst");
            if (!hasHazard && context.GetNodeState("mem:stall") == 0 && context.GetNodeState("mem:wreg") == 1)
            { // Hazard with instruction in MEM, stall 2 cycle.
                if (shift == 0 && rs == prevRegdst && fq1 == 0)
                {
                    Console.WriteLine("ID: Hazard with inst in MEM");
                    context.SetRegister("q1", 2);
                    //hasHazard = true;
                    if (isBranch)
                    {
                        context.PassNodeState("id:pc");
                        context.PassNodeState("id:ir");
                        context.SetNodeState("if:pc", pc);
                        context.SetRegister("fq1", 1);
                        context.SetNodeState("id:stall", 0);
                        context.SetNodeState("if:stall", 1);
                        context.SetNodeState("exe:stall", 1);
                    }
                }
                // Pay attention!
                // Though aluimm != 0, 'sw' will make use of q2, so hazard is still possible.
                // So here we must consider 'sw' alone.
                if ((op == 43 || aluimm == 0) && rt == prevRegdst && fq2 == 0)
                {
                    Console.WriteLine("ID: Hazard with inst in MEM");
                    context.SetRegister("q2", 2);
                   // hasHazard = true;
                    if (isBranch)
                    {
                        context.PassNodeState("id:pc");
                        context.PassNodeState("id:ir");
                        context.SetNodeState("if:pc", pc);
                        context.SetRegister("fq2", 1);
                        context.SetNodeState("id:stall", 0);
                        context.SetNodeState("if:stall", 1);
                        context.SetNodeState("exe:stall", 1);
                    }
                }
            }

            prevRegdst = context.GetNodeState("wb:regdst");
            if (!hasHazard && context.GetNodeState("wb:stall") == 0 && context.GetNodeState("wb:wreg") == 1)
            { // Hazard with instruction in WB, stall 1 cycle.
                if (shift == 0 && rs == prevRegdst && fq1 == 0)
                {
                    Console.WriteLine("ID: Hazard with inst in WB");
                    context.SetRegister("q1", 3);
                    if (isBranch)
                    {
                        context.PassNodeState("id:pc");
                        context.PassNodeState("id:ir");
                        context.SetNodeState("if:pc", pc);
                        context.SetRegister("fq1", 1);
                        context.SetNodeState("id:stall", 0);
                        context.SetNodeState("if:stall", 1);
                        context.SetNodeState("exe:stall", 1);
                    }
                }
                if (aluimm == 0 && rt == prevRegdst && fq2 == 0)
                {
                    Console.WriteLine("ID: Hazard with inst in WB");
                    context.SetRegister("q2", 3);
                    if (isBranch)
                    {
                        context.PassNodeState("id:pc");
                        context.PassNodeState("id:ir");
                        context.SetNodeState("if:pc", pc);
                        context.SetRegister("fq2", 1);
                        context.SetNodeState("id:stall", 0);
                        context.SetNodeState("if:stall", 1);
                        context.SetNodeState("exe:stall", 1);
                    }
                }
            }
            
            context.SetNodeState("exe:wreg", wreg);
            context.SetNodeState("exe:m2reg", m2reg);
            context.SetNodeState("exe:wmem", wmem);
            context.SetNodeState("exe:aluc", aluc);
            context.SetNodeState("exe:aluimm", aluimm);
            context.SetNodeState("exe:shift", shift);
            context.SetNodeState("exe:imm", imm);
            context.SetNodeState("exe:regdst", regdst);
        }
    }
}
