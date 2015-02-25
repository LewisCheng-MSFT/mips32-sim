using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mips32processor
{
    public class Memory
    {
        private uint[] m_array;

        public Memory(int sizeInWords)
        {
            m_array = new uint[sizeInWords];
        }

        public Memory(uint[] existing)
        {
            m_array = existing;
        }

        public uint this[uint address]
        {
            get
            {
                return m_array[address >> 2];
            }
            set
            {
                m_array[address >> 2] = value;
            }
        }
    }
}
