using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mips32processor
{
    class Program
    {
        static void Main(string[] args)
        {
            //IDirector director = new TimerDirector(100);
            IDirector director = new IterativeDirector();
            director.Start(new Mips32.Processor(), new ProcessorContext());
        }
    }
}
