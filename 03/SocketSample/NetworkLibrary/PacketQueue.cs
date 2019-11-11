using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetworkLibrary
{
    public class PacketQueue
    {
        // 패킷 저장 정보
        struct PacketInfo
        {
            public int offset;
            public int size;
        }

        // 데이터를 보존할 버퍼
        private MemoryStream m_streamBuffer;

        // 패킷 정보 관리 리스트
        private List<PacketInfo> m_offsetList;

        // 메모리 배치 오프셋
        private int m_offset = 0;
        // 락 오브젝트
        private Object lockObj = new Object();

        public PacketQueue()
        {
            m_streamBuffer = new MemoryStream();
            m_offsetList = new List<PacketInfo>();
        }

        // 큐를 추가합니다.
        public int Enqueue(byte[] data, int size)
        {
            PacketInfo info = new PacketInfo();

            info.offset = m_offset;
            info.size = size;
            lock (lockObj)
            {
                // 패킷 저장 정보를 보존.
                m_offsetList.Add(info);

                // 패킷 데이터를 보존.
                m_streamBuffer.Position = m_offset;
                m_streamBuffer.Write(data, 0, size);
                m_streamBuffer.Flush();
                m_offset += size;
            }
            return size;
        }

        // 큐를 꺼낸다.
        public int Dequeue(ref byte[] buffer, int size)
        {
            if (m_offsetList.Count <= 0)
            {
                return -1;
            }

            int recvSize = 0;
            lock (lockObj)
            {
                PacketInfo info = m_offsetList[0];
                // 버퍼로부터 해당하는 패킷 데이터를 가져옵니다.
                int dataSize = Math.Min(size, info.size);
                m_streamBuffer.Position = info.offset;
                recvSize = m_streamBuffer.Read(buffer, 0, dataSize);

                // 큐 데이터를 꺼냈으므로 선두 요소 삭제.
                if (recvSize > 0)
                {
                    m_offsetList.RemoveAt(0);
                }

                // 모든 큐 데이터를 꺼냈을 때는 스트림을 클리어해서 메모리를 절약합니다.
                if (m_offsetList.Count == 0)
                {
                    Clear();
                    m_offset = 0;
                }
            }

            return recvSize;
        }
        // 큐를 클리어합니다.	
        public void Clear()
        {
            byte[] buffer = m_streamBuffer.GetBuffer();
            Array.Clear(buffer, 0, buffer.Length);

            m_streamBuffer.Position = 0;
            m_streamBuffer.SetLength(0);
        }
    }

}
