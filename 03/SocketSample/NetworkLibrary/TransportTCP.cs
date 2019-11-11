using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetworkLibrary
{
    class TransportTCP
    {
        // 리스닝
        private Socket m_listener = null;

        // 클라이언트와의 접속용 소켓
        private Socket m_socket = null;

        // 송신 버퍼.
        private PacketQueue m_sendQueue;

        // 수신 버퍼.
        private PacketQueue m_recvQueue;

        // 이벤트 통지 델리게이트
        public delegate void EventHandler(NetEventState state);

        private EventHandler m_handler;

        // 서버 플래그.	
        private bool m_isServer = false;

        // 접속 플래그.
        private bool m_isConnected = false;

        // 스레스 실행 플래그.
        protected bool m_threadLoop = false;

        protected Thread m_thread = null;

        private static int s_mtu = 1400;

        public TransportTCP()
        {
            // 송수신 버퍼를 작성합니다.
            m_sendQueue = new PacketQueue();
            m_recvQueue = new PacketQueue();
        }
        // 대기 시작.
        public bool StartServer(int port, int connectionNum)
        {
            Console.WriteLine("StartServer called.!");

            // 리스닝 소켓을 생성합니다.
            try
            {
                // 소켓을 생성합니다.
                m_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // 사용할 포트 번호를 할당합니다.
                m_listener.Bind(new IPEndPoint(IPAddress.Any, port));
                // 대기를 시작합니다.
                m_listener.Listen(connectionNum);
            }
            catch
            {
                Console.WriteLine("StartServer fail");
                return false;
            }
            m_isServer = true;

            return LaunchThread();
        }

        // 대기 종료.
        public void StopServer()
        {
            // 리스닝 소켓을 닫는다.
            if (m_listener != null)
            {
                m_listener.Close();
                m_listener = null;
            }
        }

        // 스레드 실행 함수.
        bool LaunchThread()
        {
            try
            {
                // Dispatch용 스레드 시작.
                m_threadLoop = true;
                m_thread = new Thread(new ThreadStart(Dispatch));
                m_thread.Start();
            }
            catch
            {
                Console.WriteLine("Cannot launch thread.");
                return false;
            }

            return true;
        }

        // 스레드 측의 송수신 처리.
        public void Dispatch()
        {
            Console.WriteLine("Dispatch thread started.");
            while (m_threadLoop)
            {
                // 클라이언트로부터의 접속을 기다립니다. 
                AcceptClient();

                // 클라이언트와의 송수신을 처리합니다.
                if (m_socket != null && m_isConnected == true)
                {

                    // 송신처리.
                    DispatchSend();

                    // 수신처리.
                    DispatchReceive();
                }

                Thread.Sleep(5);// 5ms 마다 송신
            }

            Console.WriteLine("Dispatch thread ended.");
        }

        // 클라이언트와의 접속.
        void AcceptClient()
        {
            if (m_listener != null && m_listener.Poll(0, SelectMode.SelectRead))
            {
                // 클라이언트에서 접속했습니다.
                m_socket = m_listener.Accept();
                m_isConnected = true;
                Console.WriteLine("Connected from client.");
            }
        }

        // 스레드 측의 송신
        void DispatchSend()
        {
            //송신
            if (m_socket.Poll(0, SelectMode.SelectWrite))
            {
                byte[] buffer = new byte[s_mtu];
                int sendSize = m_sendQueue.Dequeue(ref buffer, buffer.Length);
                while (sendSize > 0)
                {
                    m_socket.Send(buffer, sendSize, SocketFlags.None);
                    sendSize = m_sendQueue.Dequeue(ref buffer, buffer.Length);
                }
            }
        }

        // 스레드 측의 수신
        void DispatchReceive()
        {
            //수신
            if (m_socket.Poll(0, SelectMode.SelectRead))
            {
                byte[] buffer = new byte[s_mtu];
                int recvSize = m_socket.Receive(buffer, buffer.Length, SocketFlags.None);
                if (recvSize == 0)
                {
                    // 접속 종료
                    Disconnect();
                }
                else if (recvSize > 0)
                {
                    m_recvQueue.Enqueue(buffer, recvSize);
                }

            }
        }

        // 이벤트 통지함수 등록.
        public void RegisterEventHandler(EventHandler handler)
        {
            m_handler += handler;
        }

        // 이벤트 통지함수 삭제.
        public void UnregisterEventHandler(EventHandler handler)
        {
            m_handler -= handler;
        }


        public void Disconnect()
        {
            m_isConnected = false;
            if (m_socket != null)
            {
                // 소켓 클로즈.
                m_socket.Shutdown(SocketShutdown.Both);
                m_socket.Close();
                m_socket = null;
            }

            // 끊기를 통지합니다.
            if (m_handler != null)
            {
                NetEventState state = new NetEventState();
                state.type = NetEventType.Disconnect;
                state.result = NetEventResult.Success;
                m_handler(state);
            }

        }
    }
}
