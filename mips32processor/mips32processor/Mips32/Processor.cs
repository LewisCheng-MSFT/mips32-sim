using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mips32processor.Mips32
{
    class Processor : IProcessorComponent
    {
        public ICollection<IProcessorComponent> SubComponents { get; set; }

        private IF m_if;
        private ID m_id;
        private EXE m_exe;
        private MEM m_mem;
        private WB m_wb;

        public void Initialize(ProcessorContext context)
        {
            SubComponents = new List<IProcessorComponent>();

            m_if = new IF();
            m_if.Initialize(context);
            SubComponents.Add(m_if);

            m_id = new ID();
            m_id.Initialize(context);
            SubComponents.Add(m_id);

            m_exe = new EXE();
            m_exe.Initialize(context);
            SubComponents.Add(m_exe);

            m_mem = new MEM();
            m_mem.Initialize(context);
            SubComponents.Add(m_mem);

            m_wb = new WB();
            m_wb.Initialize(context);
            SubComponents.Add(m_wb);
        }

        public void Pulse()
        {
            foreach (Stage c in SubComponents)
                c.Pulse();
        }
    }
}
