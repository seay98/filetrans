using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Communication
{
    interface ITcpReceiveDataProc
    {
        int ReceiveData(NetworkStream stream, Object o);
    }
}
