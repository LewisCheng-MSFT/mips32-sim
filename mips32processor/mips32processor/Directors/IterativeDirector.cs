using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace mips32processor.Directors
{
    public class IterativeDirector : IDirector
    {
        public void Start(IProcessorComponent component, ProcessorContext context)
        {
            component.Initialize(context);

            while (!context.IsHalted)
            {
                context.IncrementCycle();
                Console.WriteLine("-------------------- Cycle {0} --------------------", context.CurrentCycle);
                component.Pulse(context);
                Console.WriteLine("-------------------- End Cycle --------------------\n");
                //Console.ReadLine();
            }
        }
    }
}
