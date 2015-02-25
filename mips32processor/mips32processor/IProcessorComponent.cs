using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mips32processor
{
    public interface IProcessorComponent
    {
        ICollection<IProcessorComponent> SubComponents { get; set; }
        void Initialize(ProcessorContext context);
        void Pulse();
    }
}
