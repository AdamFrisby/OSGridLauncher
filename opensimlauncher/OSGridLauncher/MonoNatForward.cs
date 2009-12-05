// Author:
//   Adam Frisby <adam@deepthink.com.au>
//
// Copyright (C) 2009 OSGrid, Inc. A Californian non-profit 
//                               public benefit corporation.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
using System.Threading;
using System.Windows.Forms;
using Mono.Nat;

namespace OSGridLauncher
{
    class MonoNatForward
    {
        private INatDevice router;

        public void Map(Protocol protocol, int port)
        {
            if(router == null)
            {
                Start();
                Thread.Sleep(5000);
            }

            if (router == null)
                return;

            try
            {
                router.CreatePortMap(new Mapping(protocol, port, port));
            }
            catch (MappingException)
            {
                if (port != 11789)
                    MessageBox.Show("Network autoconfiguration failed while trying to automatically forward port " +
                                    port + " (" + (protocol == Protocol.Tcp ? "TCP" : "UDP") +
                                    "). If you have manually forwarded this port, you can ignore this error.");
            }
        }

        public void Unmap(Protocol protocol, int port)
        {
            if (router == null)
            {
                Start();
                Thread.Sleep(5000);
            }

            if (router == null)
                return;

            foreach (Mapping mapping in router.GetAllMappings())
            {
                if (mapping.PublicPort == port && mapping.Protocol == protocol)
                {
                    router.DeletePortMap(mapping);
                }
            }
        }

        public void Start()
        {
            NatUtility.DeviceFound += NatUtility_DeviceFound;
            NatUtility.StartDiscovery();
        }

        void NatUtility_DeviceFound(object sender, DeviceEventArgs e)
        {
            router = e.Device;
        }

    }
}
