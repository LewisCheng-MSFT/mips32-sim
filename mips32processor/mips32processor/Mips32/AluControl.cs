using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mips32processor.Mips32
{
    public class AluControl
    {
        public const uint AlucAdd = 0;
        public const uint AlucSub = 1;
        public const uint AlucAnd = 2;
        public const uint AlucOr = 3;
        public const uint AlucLessThan = 4;
        public const uint AlucShiftLeftLogic = 5;
        public const uint AlucShiftRightLogic = 6;
        public const uint AlucShiftRightArithmetic = 7;
    }
}
