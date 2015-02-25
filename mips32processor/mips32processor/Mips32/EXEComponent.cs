using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mips32processor.Mips32
{
    public class EXEComponent : IProcessorComponent
    {
        public IList<IProcessorComponent> SubComponents { get; set; }

        public void Initialize(ProcessorContext context)
        {

        }

        public void Pulse(ProcessorContext context)
        {
            // Startup.
            uint started = context.GetNodeState("exe:started");
            if (started == 0)
            {
                Console.WriteLine("EXE: Not Started");
                return;
            }
            context.SetNodeState("mem:started", 1);

            // Stall.
            uint stall = context.GetNodeState("exe:stall");
            if (stall == 1)
            {
                context.SetNodeState("mem:stall", 1);
                Console.WriteLine("EXE: Stall");
                return;
            }
            context.SetNodeState("mem:stall", 0);

            // Read IR.
            uint ir = context.GetNodeState("exe:ir");

            // Pass IR.
            context.SetNodeState("mem:ir", ir);

            if ((ir >> 26) == 0x3f)
            {
                Console.WriteLine("EXE: Stall");
                return;
            }

            // Pass WREG.
            context.PassNodeState("exe:wreg", "mem:wreg");

            // Pass M2REG.
            context.PassNodeState("exe:m2reg", "mem:m2reg");

            // Pass WMEM.
            context.PassNodeState("exe:wmem", "mem:wmem");

            // Pass REGDST.
            context.PassNodeState("exe:regdst", "mem:regdst");

            // Pass Q2.
            context.PassNodeState("exe:q2", "mem:q2");

            // Alu a.
            uint shift = context.GetNodeState("exe:shift");
            uint alua;
            if (shift == 0)
                alua = context.GetNodeState("exe:q1");
            else // Shift amount.
                alua = 0;

            // Alu b.
            uint aluimm = context.GetNodeState("exe:aluimm");
            uint alub;
            if (aluimm == 0)
                alub = context.GetNodeState("exe:q2");
            else
                alub = context.GetNodeState("exe:imm");

            // sa.
            int sa = (int)context.GetNodeState("exe:sa");

            // Alu control.
            uint aluout = 0;
            uint aluc = context.GetNodeState("exe:aluc");
            switch (aluc)
            {
                case AluControl.AlucAdd:
                    aluout = alua + alub;
                    break;
                case AluControl.AlucSub:
                    aluout = alua - alub;
                    break;
                case AluControl.AlucLessThan:
                    aluout = alua < alub ? 1u : 0u;
                    break;
                case AluControl.AlucAnd:
                    aluout = alua & alub;
                    break;
                case AluControl.AlucOr:
                    aluout = alua | alub;
                    break;
                case AluControl.AlucShiftLeftLogic:
                    aluout = alub << sa;
                    break;
                case AluControl.AlucShiftRightLogic:
                    aluout = alub >> sa;
                    break;
                case AluControl.AlucShiftRightArithmetic:
                    // Pay attention to arithmetic shift-right.
                    int quotient = (int)alub;
                    while (sa-- > 0)
                        quotient /= 2;
                    aluout = (uint)quotient;
                    break;
                default:
                    throw new NotSupportedException("aluc[" + aluc + "] not supported!");
            }

            context.SetNodeState("mem:aluout", aluout);

            Console.WriteLine("EXE: inst=0x{0:x}, aluc={1}, alu_a={2}, alu_b={3}, alu_out={4}", ir, aluc, alua, alub, aluout);
        }
    }
}
