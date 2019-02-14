using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Sockets;

namespace TcpWindowsServer.UnitTests
{
    [TestClass]
    public class TcpWindowsServiceTest
    {
        [TestMethod]
        public void ConnectTcp()
        {
            var responseData = string.Empty;
            string server = "127.0.0.1";
            string message = "test";
            int port = 3000;
            using (var client = new TcpClient(server, port))
            {
                var data = System.Text.Encoding.UTF8.GetBytes(message);

                using (var stream = client.GetStream())
                {
                    stream.Write(data, 0, data.Length);
                    data = new byte[256];

                    int bytes = stream.Read(data, 0, data.Length);
                    responseData = System.Text.Encoding.UTF8.GetString(data, 0, bytes);

                    stream.Close();
                }
                client.Close();
            }

            Assert.AreEqual("TEST", responseData);
        }
    }
}
