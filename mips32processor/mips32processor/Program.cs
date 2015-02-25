using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using mips32processor.Directors;
using mips32processor.Mips32;

namespace mips32processor
{
    class Program
    {
        static void Main(string[] args)
        {
            //IDirector director = new TimerDirector(100);
            IDirector director = new IterativeDirector();
            director.Start(new Processor(), new ProcessorContext());
        }
    }
}
