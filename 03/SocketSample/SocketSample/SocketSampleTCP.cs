using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketSample
{
  

    public class SocketSampleTCP
    {
        private Socket m_listener;
        private Socket m_socket;

        public void Listen(string host, int port , int backlog)
        {
            // 소켓을 생성합니다.
            m_listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // 사용할 포트 번호를 할당합니다.
            m_listener.Bind(new IPEndPoint(IPAddress.Parse(host), port));

            // 대기를 시작한다.
            m_listener.Listen(backlog);
            Thread thread = new Thread(AcceptClient);
            thread.Start();

        }

        void AcceptClient()
        {
            m_socket = null;
            while (true)
            {
                if (m_socket == null && m_listener != null && m_listener.Poll(0 , SelectMode.SelectRead))
                {
                    // 클라이언트가 접속했습니다.
                    m_socket = m_listener.Accept();
                    Console.WriteLine("[TCP]Connected from client.");




                    byte[] buffer = new byte[1400];
                    int recvSize = m_socket.Receive(buffer, buffer.Length, SocketFlags.None);
                    if (recvSize > 0)
                    {
                        string message = System.Text.Encoding.UTF8.GetString(buffer);
                        Console.WriteLine(message);
                    }

                }
            }
        }

        void StopListener()
        {
            if (m_listener != null)
            {
                m_listener.Close();
                m_listener = null;
            }

        }
    }
}
