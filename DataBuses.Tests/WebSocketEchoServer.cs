using WebSocketSharp;
using WebSocketSharp.Server;

namespace Boyd.DataBuses.Tests
{
    public class WebSocketEchoServer : WebSocketBehavior
    {
        protected override void OnMessage (MessageEventArgs e)
        {
            Send (e.RawData);
        }
    }
}