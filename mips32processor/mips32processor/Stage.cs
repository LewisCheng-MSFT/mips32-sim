using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mips32processor
{
    public abstract class Stage : IProcessorComponent
    {
        protected ProcessorContext m_context;

        public ICollection<IProcessorComponent> SubComponents { get; set; }

        public virtual void Initialize(ProcessorContext context)
        {
            m_context = context;

            // SubComponents = new List<IProcessorComponent>();
        }

        public abstract bool Startup();
        public abstract bool Read();
        public abstract bool Pass();
        public abstract bool Simulate();
        public abstract void Write();

        public void Pulse()
        {
            if (!Startup())
                return;
            if (!Read())
                return;
            if (!Pass())
                return;
            if (!Simulate())
                return;
            Write();
        }
    }
}
