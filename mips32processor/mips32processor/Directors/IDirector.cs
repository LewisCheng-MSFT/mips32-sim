using System;
namespace mips32processor.Directors
{
    public interface IDirector
    {
        void Start(IProcessorComponent component, ProcessorContext context);
    }
}
