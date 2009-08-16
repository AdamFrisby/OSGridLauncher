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
//
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;

using System.Threading;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
//using NATUPNPLib;
//using Mono.Upnp;
using Mono.Nat;

namespace OSGridLauncher
{
    public class OpenSimConfigurator
    {
        public OpenSimConfigurator()
        {
            SetupDirs();
        }

        private string OpenSimDir = "opensim";
        private string OpenSimBinDir = "opensim";

        private void SetupDirs()
        {
            OpenSimDir = Path.Combine(Environment.CurrentDirectory, "opensim");
            OpenSimBinDir = Path.Combine(OpenSimDir, OpenSimBinDir);
        }

        bool useWinNat = false;

        private void ForwardPort(int port, ProtocolType pt)
        {
            if (useWinNat)
            {
                Mono.Nat.Mapping n = new Mapping(Protocol.Tcp,9000,9000);
                
                UPnPNATClass uPnP = new UPnPNATClass();

                uPnP.StaticPortMappingCollection.Add(port, (pt == ProtocolType.Tcp ? "TCP" : "UDP"), port,
                                                     GetLocalIP().ToString(), true,
                                                     "OSGrid OpenSimulator");
            } else
            {
                NAT.Discover();
                try
                {
                    NAT.ForwardPort(port, pt, "OSGrid OpenSimulator");
                }
                catch (WebException)
                {
                    MessageBox.Show("UPnPLib Failed, reverting to Windows.");
                    useWinNat = true;

                    ForwardPort(port, pt);
                }
            }
        }

        private void DefowardPort(int port, ProtocolType pt)
        {

            if (useWinNat)
            {
                UPnPNATClass uPnP = new UPnPNATClass();

                uPnP.StaticPortMappingCollection.Remove(port, (pt == ProtocolType.Tcp ? "TCP" : "UDP"));
            }
            else
            {
                NAT.Discover();
                try
                {
                    NAT.DeleteForwardingRule(port, pt);
                }
                catch (WebException)
                {
                    MessageBox.Show("UPnPLib Failed, reverting to Windows.");
                    useWinNat = true;

                    DefowardPort(port, pt);
                }
            }
        }

        private void SetStatus(int val, string text, ProgressBar pb, Label status)
        {
            pb.Invoke((MethodInvoker) delegate()
                                          {
                                              pb.Value = val;
                                              //status.Text = text;
                                          });
            status.Invoke((MethodInvoker) delegate()
                                              {
                                                  //pb.Value = val;
                                                  status.Text = text;
                                              });
        }

        public void ConfigAndLaunch(string regionName, string avFname, string avLname,
            ProgressBar pb, Label status)
        {
            Thread tmp = new Thread(delegate()
                                        {
                                            try
                                            {
                                                if (Directory.Exists(OpenSimDir) && HasLatestVesion())
                                                {
                                                    SetStatus(80, "Setting up port forwards",
                                                              pb, status);
                                                    SetupPortForwarding();

                                                    SetStatus(90, "Launching...",
                                                              pb, status);
                                                    Run();

                                                    SetStatus(100, "Running.",
                                                              pb, status);

                                                    Thread.Sleep(15000);
                                                    Environment.Exit(0);
                                                }
                                                else
                                                {
                                                    SetStatus(10, "Testing network...",
                                                              pb, status);

                                                    if (TestNetwork())
                                                    {
                                                        if (!File.Exists("osg_latest.zip"))
                                                        {
                                                            SetStatus(20, "Downloading (may take a while)...",
                                                                      pb, status);
                                                            Download();
                                                        }

                                                        SetStatus(60, "Unpacking...",
                                                                  pb, status);
                                                        Unpack();

                                                        SetStatus(70, "Writing Configuration...",
                                                                  pb, status);
                                                        WriteRegionConfig(regionName, avFname, avLname);

                                                        SetStatus(80, "Setting up port forwards",
                                                                  pb, status);
                                                        SetupPortForwarding();

                                                        SetStatus(90, "Launching...",
                                                                  pb, status);
                                                        Run();

                                                        SetStatus(100, "Running.",
                                                                  pb, status);

                                                        Thread.Sleep(15000);
                                                        Environment.Exit(0);
                                                    }
                                                    else
                                                    {
                                                        MessageBox.Show(
                                                            "Your router does not appear to support either UPnP or NAT Loopback. You may need to host your region on a dedicated server or VPS instead.",
                                                            "NAT Loopback Error");
                                                        Environment.Exit(1);
                                                    }
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                MessageBox.Show(e.ToString());
                                                Environment.Exit(1);
                                            }
                                        });
            tmp.Start();
        }

        private void Run()
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "OpenSim.32bitLaunch.exe";
            processStartInfo.WorkingDirectory = OpenSimBinDir;
            Process.Start(processStartInfo);
        }

        private bool HasLatestVesion()
        {
            if(!File.Exists("config.ver"))
                return false;

            string Url = WebFetch.Fetch("http://www.osgrid.org/elgg/pg/utilities/autowin");

            return File.ReadAllText("config.ver") == Url;
        }

        private void SetLatestVersion(string url)
        {
            File.WriteAllText("config.ver", url);
        }

        private void Download()
        {
            // Cleanup existing
            if (File.Exists("osg_latest.zip"))
                File.Delete("osg_latest.zip");

            string Url = WebFetch.Fetch("http://www.osgrid.org/elgg/pg/utilities/autowin");
            WebClient wc = new WebClient();
            wc.DownloadFile(Url,"osg_latest.zip");

            SetLatestVersion(Url);
        }

        private void Unpack()
        {
            PrepareForUpgrade();

            FastZip tmp = new FastZip();
            tmp.ExtractZip("osg_latest.zip", OpenSimDir, FastZip.Overwrite.Always, FastZipConfirm, "", "", true);
        }

        private bool FastZipConfirm(string filename)
        {
            return true;
        }

        private void PrepareForUpgrade()
        {
            if (Directory.Exists(OpenSimDir) && Directory.Exists(OpenSimBinDir))
            {
                string[] files = Directory.GetFiles(OpenSimBinDir);

                foreach (string file in files)
                {
                    if (file.EndsWith(".xml") || file.EndsWith(".exe") || file.EndsWith(".pdb") || file.EndsWith(".dll"))
                    {
                        File.Delete(file);
                    }
                }
            }
        }

        private void WriteRegionConfig(string regionName, string fname, string lname)
        {
            string regionDir = Path.Combine(OpenSimBinDir, "regions");

            // Make region dir
            if (!Directory.Exists(regionDir))
                Directory.CreateDirectory(regionDir);

            string fn = Path.Combine(regionDir, "RegionConfig.ini");

            if(File.Exists(fn))
                return; // Already configured.

            string coords = WebFetch.Fetch("http://www.osgrid.org/elgg/pg/utilities/autocoord");

            string UUID = Guid.NewGuid().ToString();
            string Location = coords;


            string ini =
                "[" + regionName + "]\r\n" +
                "RegionUUID=" + UUID + "\r\n" +
                "Location=\"" + Location + "\"\r\n" +
                "InternalAddress=0.0.0.0\r\n" +
                "InternalPort=9000\r\n" +
                "AllowAlternatePorts=false\r\n" +
                "MasterAvatarUUID = \"00000000-0000-0000-0000-000000000000\"\r\n" +
                "ExternalHostName=" + GetInternetIP().ToString().Trim() + "\r\n" +
                "MasterAvatarFirstName=" + fname + "\r\n" +
                "MasterAvatarLastName=" + lname + "\r\n";

            File.WriteAllText(fn, ini);
        }

        private void SetupPortForwarding()
        {
            ForwardPort(9000,ProtocolType.Tcp);
            ForwardPort(9000, ProtocolType.Udp);
        }

        private IPAddress GetInternetIP()
        {
            string IP = WebFetch.Fetch("http://www.osgrid.org/elgg/pg/utilities/autoip");
            return IPAddress.Parse(IP);
        }

        private static IPAddress GetLocalIP()
        {
            #pragma warning disable 618,612 // GetHostName is Obsolete();
            IPAddress[] a = Dns.GetHostByName(Dns.GetHostName()).AddressList;
            #pragma warning restore 618,612

            // Look for a NAT IP
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].ToString().StartsWith("192.168."))
                    return a[i];
                if (a[i].ToString().StartsWith("172.16."))
                    return a[i];
                if (a[i].ToString().StartsWith("10."))
                    return a[i];
            }

            // Otherwise return first found.
            return a[0];
        }

        private bool TestNetwork_Accepted = false;
        private TcpListener TestNetwork_Listener; 

        public bool TestNetwork()
        {
            UPnPNATClass uPnP = new UPnPNATClass();
            const int testPort = 11789;

            try
            {
                // Setup port forward
                ForwardPort(testPort, ProtocolType.Tcp);
                
                // Test it
                TestNetwork_Listener = new TcpListener(IPAddress.Any, testPort);

                Thread listenThread = new Thread(TestNetwork_Listen);
                listenThread.Start();

                // Try connect to our internet IP (testing NAT loopback+forwarding)
                IPEndPoint ep = new IPEndPoint(GetInternetIP(), testPort);
                TcpClient testClient = new TcpClient();

                // "Connect" (or not.)
                testClient.Connect(ep);

                // Send some data, so it actually attempts connection.
                testClient.GetStream().WriteByte(13);
                testClient.GetStream().WriteByte(33);
                testClient.GetStream().WriteByte(33);
                testClient.GetStream().WriteByte(37);
                
                // Wait for data to come back
                Thread.Sleep(1000);

                // Cleanup
                testClient.Close();

                return TestNetwork_Accepted;
            }
            catch (SocketException e)
            {
                MessageBox.Show("SocketError: " + e);
                return false;
            }
            finally
            {
                try
                {
                    DefowardPort(testPort, ProtocolType.Tcp);
                }
                catch (NullReferenceException)
                {
                    MessageBox.Show("Operating system not supported.");
                    Environment.Exit(0);
                }
                catch (FileNotFoundException)
                {
                    // Ignore
                }
                if (TestNetwork_Listener != null)
                    TestNetwork_Listener.Stop();
            }
        }

        private void TestNetwork_Listen()
        {
            TestNetwork_Listener.Start();
            TestNetwork_Listener.AcceptTcpClient();
            TestNetwork_Accepted = true;
        }
    }
}
