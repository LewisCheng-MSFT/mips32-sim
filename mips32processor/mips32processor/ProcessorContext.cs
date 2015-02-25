using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mips32processor
{
    public class ProcessorContext
    {
        private long m_currentCycle = -1;
        private IDictionary<string, uint> m_registers;
        private IDictionary<string, uint> m_nextNodeStates;
        private IDictionary<string, uint> m_currentNodeStates;

        public long CurrentCycle
        {
            get
            {
                return m_currentCycle;
            }
        }

        public bool IsHalted;

        public uint[] InstMemory;
        public uint[] DataMemory;

        public ProcessorContext()
        {
            m_nextNodeStates = new Dictionary<string, uint>();
            m_currentNodeStates = new Dictionary<string, uint>();
            m_registers = new Dictionary<string, uint>();

            /*InstMemory = new uint[] {
                0x2001000A,
                0x00411020,
                0x2021FFFF,
                0x1420FFFD,
                0xFFFFFFFF
            };*/
            
            InstMemory = new uint[] {
                0x20020005,
                0x2003000C,
                0x2067FFF7,
                0x00E22025,
                0x00642824,
                0x00A42820,
                0x10A70012,
                0x0064202A,
                0x10800004,
                0x20050000,
                0x20050000,
                0x20050000,
                0x20050000,
                0x00E2202A,
                0x00853820,
                0x00E23822,
                0xAC670044,
                0x8C020050,
                0x08000019,
                0x20020001,
                0x20020001,
                0x20020001,
                0x20020001,
                0x20020001,
                0x20020001,
                0xAC020054,
                0xFFFFFFFF
            };

            // 4KB
            DataMemory = new uint[1024];
            //DataMemory[1] = 0xcc;
            //DataMemory[4] = 0xff;
        }

        public uint GetRegister(string name)
        {
            try
            {
                return m_registers[name];
            }
            catch (KeyNotFoundException)
            {
                return 0;
            }
        }

        public uint GetNodeState(string name)
        {
            try
            {
                return m_currentNodeStates[name];
            }
            catch (KeyNotFoundException)
            {
                return 0;
            }
        }

        public void SetNodeState(string name, uint state)
        {
            m_nextNodeStates[name] = state;
        }

        public void PassNodeState(string from, string to)
        {
            SetNodeState(to, GetNodeState(from));
        }

        // Continue current state in next cycle.
        public void PassNodeState(string name)
        {
            SetNodeState(name, GetNodeState(name));
        }

        public void SetRegister(string name, uint value)
        {
            m_registers[name] = value;
        }

        public void IncrementCycle()
        {
            ++m_currentCycle;

            // Switch state.
            IDictionary<string, uint> temp = m_currentNodeStates;
            m_currentNodeStates = m_nextNodeStates;
            m_nextNodeStates = temp;
        }
    }
}
