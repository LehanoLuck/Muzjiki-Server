using System.Net.WebSockets;

namespace Muzjiki_Server
{
    public class ConnectionManager
    {
        public List<WebSocket> Connections = new();
    }
}
