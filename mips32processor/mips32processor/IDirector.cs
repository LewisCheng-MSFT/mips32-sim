using System;
namespace mips32processor
{
    public interface IDirector
    {
        void Start(IProcessorComponent component, ProcessorContext context);
    }
}
