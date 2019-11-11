using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var tcp  = new SocketSampleTCP();
            tcp.Listen("127.0.0.1", 7979, 100);
            while (true)
            {

            }
        }
    }
}
