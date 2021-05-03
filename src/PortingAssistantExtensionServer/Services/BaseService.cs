using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistantExtensionServer.Services
{
    class BaseService
    {
        protected async Task CreateClientConnectionAsync(string pipeName)
        {
            NamedPipeClientStream client = null;
            try
            {
                client = new NamedPipeClientStream(pipeName);
                await client.ConnectAsync();
                StreamWriter writer = new StreamWriter(client);
                //We don't care what's being written, we just want to ping the client and tell it we're done
                await writer.WriteLineAsync("");
                await writer.FlushAsync();

            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (client != null)
                {
                    if (client.IsConnected)
                    {
                        client.Close();
                    }
                    await client.DisposeAsync();
                }
            }
        }
    }
}
