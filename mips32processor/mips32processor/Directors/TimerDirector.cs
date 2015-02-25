using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace mips32processor.Directors
{
    public class TimerDirector : IDirector
    {
        private System.Timers.Timer m_timer;
        private Semaphore m_semStop;
        private IProcessorComponent m_component;
        private ProcessorContext m_context;

        public TimerDirector(double period)
        {
            m_timer = new System.Timers.Timer(period);
            m_timer.Elapsed += OnTimedEvent;

            m_semStop = new Semaphore(0, 1);
        }

        public void Start(IProcessorComponent component, ProcessorContext context)
        {
            m_component = component;
            m_context = context;
            component.Initialize(context);
            m_timer.Start();
            m_semStop.WaitOne();
        }

        private void OnTimedEvent(object source, System.Timers.ElapsedEventArgs e)
        {
            m_context.IncrementCycle();
            Console.WriteLine("-------------------- Cycle {0} --------------------", m_context.CurrentCycle);
            m_component.Pulse(m_context);
            Console.WriteLine("-------------------- End Cycle --------------------\n");
            if (m_context.IsHalted)
            {
                m_timer.Stop();
                m_semStop.Release();
            }
        }
    }
}
