using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Communication
{
    class RecParam
    {
        public String RecDir { get; set; }

        public Boolean RecSme { get; set; }

        public TcpClient client { get; set; }

        public RecParam(TcpClient client)
        {
            this.client = client;
        }
    }
}
