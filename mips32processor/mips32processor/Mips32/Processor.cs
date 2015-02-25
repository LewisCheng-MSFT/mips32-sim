using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mips32processor.Mips32
{
    class Processor : IProcessorComponent
    {
        private IFComponent m_ifc;
        private IDComponent m_idc;
        private EXEComponent m_exec;
        private MEMComponent m_memc;
        private WBComponent m_wbc;

        public IList<IProcessorComponent> SubComponents { get; set; }

        public Processor()
        {
            m_ifc = new IFComponent();
            m_idc = new IDComponent();
            m_exec = new EXEComponent();
            m_memc = new MEMComponent();
            m_wbc = new WBComponent();
        }

        public void Initialize(ProcessorContext context)
        {
            m_ifc.Initialize(context);
            m_idc.Initialize(context);
            m_exec.Initialize(context);
            m_memc.Initialize(context);
            m_wbc.Initialize(context);

            //for (int i = 0; i < 32; ++i)
            //    context.SetRegister(i.ToString(), (uint)i);
        }

        public void Pulse(ProcessorContext context)
        {
            m_ifc.Pulse(context);
            m_idc.Pulse(context);
            m_exec.Pulse(context);
            m_memc.Pulse(context);
            m_wbc.Pulse(context);
        }
    }
}
