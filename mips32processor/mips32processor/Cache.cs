using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mips32processor
{
    public class Cache
    {
        public const int BlockSizeInWords = 16;
        public const uint BlockMask = 0x3f;
        public const uint BlockWildmask = 0xffffffff - BlockMask;
        public const int Groups = 16;
        public const uint GroupMask = 0xf;
        public const int GroupShift = 6;
        public const int Ways = 8;
        public const int TagShift = 10;

        private Memory m_memory;
        private bool[,] m_validBits = new bool[Groups, Ways];
        private bool[,] m_dirtyBits = new bool[Groups, Ways];
        private uint[,] m_tags = new uint[Groups, Ways];
        private uint[, ,] m_data = new uint[Groups, Ways, BlockSizeInWords];
        private int[,] m_accesses = new int[Groups, Ways];

        public Cache(Memory memory)
        {
            m_memory = memory;
        }

        public uint this[uint address]
        {
            get
            {   
                int wordIndex = GetWordIndex(address);
                int group = GetGroup(address);
                uint tag = GetTag(address);

                int blockIndex = 0;
                int blockIndexHasFirstInvalidBit = Ways;
                int blockIndexHasLowestAccesses = 0;
                for (; blockIndex < Ways; ++blockIndex)
                {
                    // Look for the first block whose valid bit is 0.
                    if (blockIndex < blockIndexHasFirstInvalidBit && !m_validBits[group, blockIndex])
                        blockIndexHasFirstInvalidBit = blockIndex;
                    // Look for the least frequent accessing block.
                    if (m_validBits[group, blockIndex] && m_accesses[group, blockIndex] < m_accesses[group, blockIndexHasLowestAccesses])
                        blockIndexHasLowestAccesses = blockIndex;
                    if (m_validBits[group, blockIndex] && m_tags[group, blockIndex] == tag)
                        break;
                }

                if (blockIndex < Ways)
                {
                    ++m_accesses[group, blockIndex];
                    return m_data[group, blockIndex, wordIndex];
                }

                if (blockIndexHasFirstInvalidBit != Ways)
                    blockIndex = blockIndexHasFirstInvalidBit;
                else
                    blockIndex = blockIndexHasLowestAccesses;

                if (m_validBits[group, blockIndex] && m_dirtyBits[group, blockIndex])
                    WriteBlock(address, group, blockIndex);

                m_validBits[group, blockIndex] = true;
                m_dirtyBits[group, blockIndex] = false;
                m_accesses[group, blockIndex] = 0;
                m_tags[group, blockIndex] = tag;
                ReadBlock(address, group, blockIndex);
                
                return m_data[group, blockIndex, wordIndex];
            }
            set
            {
                int wordIndex = GetWordIndex(address);
                int group = GetGroup(address);
                uint tag = GetTag(address);

                int blockIndex = 0;
                int blockIndexHasFirstInvalidBit = Ways;
                int blockIndexHasLowestAccesses = 0;
                for (; blockIndex < Ways; ++blockIndex)
                {
                    // Look for the first block whose valid bit is 0.
                    if (blockIndex < blockIndexHasFirstInvalidBit && !m_validBits[group, blockIndex])
                        blockIndexHasFirstInvalidBit = blockIndex;
                    // Look for the least frequent accessing block.
                    if (m_validBits[group, blockIndex] && m_accesses[group, blockIndex] < m_accesses[group, blockIndexHasLowestAccesses])
                        blockIndexHasLowestAccesses = blockIndex;
                    if (m_validBits[group, blockIndex] && m_tags[group, blockIndex] == tag)
                        break;
                }

                if (blockIndex < Ways)
                {
                    ++m_accesses[group, blockIndex];
                    m_dirtyBits[group, blockIndex] = true;
                    m_data[group, blockIndex, wordIndex] = value;
                    return;
                }

                if (blockIndexHasFirstInvalidBit != Ways)
                    blockIndex = blockIndexHasFirstInvalidBit;
                else
                    blockIndex = blockIndexHasLowestAccesses;

                if (m_validBits[group, blockIndex] && m_dirtyBits[group, blockIndex])
                    WriteBlock(address, group, blockIndex);

                m_validBits[group, blockIndex] = true;
                m_dirtyBits[group, blockIndex] = true;
                m_accesses[group, blockIndex] = 0;
                m_tags[group, blockIndex] = tag;
                ReadBlock(address, group, blockIndex);

                m_data[group, blockIndex, wordIndex] = value;
            }
        }

        private void WriteBlock(uint address, int group, int blockIndex)
        {
            uint start = address & BlockWildmask;
            uint end = start + BlockMask;
            for (int wordIndex = 0; start < end; start += 4, ++wordIndex)
                m_memory[start] = m_data[group, blockIndex, wordIndex];
        }

        private void ReadBlock(uint address, int group, int blockIndex)
        {
            uint start = address & BlockWildmask;
            uint end = start + BlockMask;
            for (int wordIndex = 0; start < end; start += 4, ++wordIndex)
                m_data[group, blockIndex, wordIndex] = m_memory[start];
        }

        private int GetWordIndex(uint address)
        {
            return (int)((address & BlockMask) >> 2);
        }

        private int GetGroup(uint address)
        {
            return (int)((address >> GroupShift) & GroupMask);
        }

        private uint GetTag(uint address)
        {
            return address >> TagShift;
        }
    }
}
