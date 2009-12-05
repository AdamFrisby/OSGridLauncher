// Author:
//   Adam Frisby <adam@deepthink.com.au>
//
// Copyright (C) 2009 OSGrid, Inc.                                  (r01-r19)
// Copyright (C) 2009-2010 DeepThink Pty Ltd - www.deepthink.com.au (r20+)
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
        readonly MonoNatForward router = new MonoNatForward();

        public OpenSimConfigurator()
        {
            SetupDirs();
        }

        private string OpenSimDir = "opensim";
        private string OpenSimBinDir = "opensim";

        private void SetupDirs()
        {
            OpenSimDir = Path.Combine(Environment.CurrentDirectory, "opensim");
            OpenSimBinDir = Path.Combine(OpenSimDir, "bin");
        }

        private void ForwardPort(int port, ProtocolType pt)
        {
            router.Map(pt == ProtocolType.Tcp ? Protocol.Tcp : Protocol.Udp, port);
        }

        private void DefowardPort(int port, ProtocolType pt)
        {
            router.Unmap(pt == ProtocolType.Tcp ? Protocol.Tcp : Protocol.Udp, port);
        }

        private static ProgressBar _pb;
        private static ToolStripItem _status;
        private static StatusStrip _statusStrip;

        private static void SetStatus(int percent, string text)
        {
            SetStatus(percent, text, _pb, _status, _statusStrip);
        }

        private static void SetStatus(int val, string text, ProgressBar pb, ToolStripItem status, Control statusStrip)
        {
            pb.Invoke((MethodInvoker) delegate
                                          {
                                              pb.Value = val;
                                              //status.Text = text;
                                          });

            statusStrip.Invoke((MethodInvoker)delegate
                                              {
                                                  //pb.Value = val;
                                                  status.Text = text;
                                              });
        }

        public void ConfigAndLaunch(string regionName, string avFname, string avLname,
            ProgressBar pb, ToolStripStatusLabel status, StatusStrip statusStrip, bool autoPosition, int posX, int posY)
        {
            _pb = pb;
            _status = status;
            _statusStrip = statusStrip;

            Thread tmp = new Thread(delegate()
                                        {
                                            try
                                            {
                                                if (Directory.Exists(OpenSimDir) && HasLatestVesion())
                                                {
                                                    SetStatus(80, "Setting up port forwards");
                                                    SetupPortForwarding();

                                                    SetStatus(90, "Launching...");
                                                    Run();

                                                    SetStatus(100, "Running.");

                                                    Thread.Sleep(15000);
                                                    Environment.Exit(0);
                                                }
                                                else
                                                {
                                                    SetStatus(10, "Testing network...");

                                                    if (!TestNetwork())
                                                    {
                                                        SetStatus(10, "Test Failed...");
                                                        MessageBox.Show(
                                                            "We were not able to successfully connect to your network device from within.\n\n" +
                                                            "This is commonly caused by either you having UPnP disabled, your router not supporting NAT Loopback, or a network misconfiguration.\n\n" +
                                                            "If you know how, from your router, try manually forward port 9000 on TCP and UDP to " + GetLocalIP() + " and press OK",
                                                            "Network Autoconfiguration Error");
                                                    }

                                                    if (!File.Exists("osg_latest.zip"))
                                                    {
                                                        SetStatus(20, "Downloading...");
                                                        Download();
                                                    }

                                                    SetStatus(60, "Unpacking...");
                                                    Unpack();

                                                    SetStatus(70, "Writing Configuration...");
                                                    WriteRegionConfig(regionName, avFname, avLname, autoPosition, posX,
                                                                      posY);

                                                    SetStatus(80, "Setting up port forwards");
                                                    SetupPortForwarding();

                                                    SetStatus(90, "Launching...");
                                                    Run();

                                                    SetStatus(100, "Running. Will exit launcher in 15s.");

                                                    Thread.Sleep(15000);
                                                    Environment.Exit(0);
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

            processStartInfo.WorkingDirectory = OpenSimBinDir;

            int p = (int) Environment.OSVersion.Platform;
            if ((p == 4) || (p == 6)) // On either Linux or OSX
            {
                processStartInfo.FileName = "mono";
                processStartInfo.Arguments = "OpenSim.32BitLaunch.exe";
            }
            else
            {
                processStartInfo.FileName = "OpenSim.32BitLaunch.exe";
            }

            try
            {
                Process.Start(processStartInfo);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show("Unable to start " + processStartInfo.FileName + " in " +
                                processStartInfo.WorkingDirectory + ", do both exist?");
            }
        }

        private static bool HasLatestVesion()
        {
            if(!File.Exists("config.ver"))
                return false;

            string Url = WebFetch.Fetch("http://www.osgrid.org/elgg/pg/utilities/autowin");

            return File.ReadAllText("config.ver") == Url;
        }

        private static void SetLatestVersion(string url)
        {
            File.WriteAllText("config.ver", url);
        }

        private bool _done;

        private void Download()
        {
            _done = false;

            // Cleanup existing
            if (File.Exists("osg_latest.zip"))
                File.Delete("osg_latest.zip");

            string Url = WebFetch.Fetch("http://www.osgrid.org/elgg/pg/utilities/autowin");
            WebClient wc = new WebClient();
            wc.DownloadProgressChanged += wc_DownloadProgressChanged;
            wc.DownloadFileCompleted += wc_DownloadFileCompleted;
            wc.DownloadFileAsync(new Uri(Url), "osg_latest.zip");
            
            while(_done == false)
            {
                Thread.Sleep(250);
            }

            SetLatestVersion(Url);
        }

        void wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            SetStatus(59, "Download Completed...");
            _done = true;
        }

        private int lastPercent = 0;

        void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            const int min = 21;
            const int max = 59;

            if (e.TotalBytesToReceive != 0)
            {
                double percent = e.BytesReceived/(double) e.TotalBytesToReceive;
                int val = (int) ((double) (max - min)*percent) + min;

                if (lastPercent != val)
                {
                    lastPercent = val;
                    SetStatus(lastPercent, "Downloading... " + (int) (percent*100) + "% Completed.");
                }
            }
            else
            {
                SetStatus(20, "Downloading... " + (e.BytesReceived/1024) + "KiB Recieved.");
            }
        }

        private void Unpack()
        {
            PrepareForUpgrade();

            FastZip tmp = new FastZip();
            tmp.ExtractZip("osg_latest.zip", OpenSimDir, FastZip.Overwrite.Always, FastZipConfirm, "", "", true);
        }

        private static bool FastZipConfirm(string filename)
        {
            SetStatus(70, "Unpacking " + filename + "...");
            return true;
        }

        private void PrepareForUpgrade()
        {
            if (Directory.Exists(OpenSimDir) && Directory.Exists(OpenSimBinDir))
            {
                SetStatus(70, "Preparing for upgrade...");
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

        private void WriteRegionConfig(string regionName, string fname, string lname, bool pos, int x, int y)
        {
            string regionDir = Path.Combine(OpenSimBinDir, "regions");

            // Make region dir
            if (!Directory.Exists(regionDir))
                Directory.CreateDirectory(regionDir);

            string fn = Path.Combine(regionDir, "RegionConfig.ini");

            if(File.Exists(fn))
                return; // Already configured.

            string coords;

            if (pos)
                coords = WebFetch.Fetch("http://www.osgrid.org/elgg/pg/utilities/autocoord");
            else
                coords = String.Format("{0},{1}", x, y);

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
            SetStatus(83,"Port Forwarding 9000/TCP via UPnP...");
            ForwardPort(9000,ProtocolType.Tcp);
            SetStatus(87, "Port Forwarding 9000/UDP via UPnP...");
            ForwardPort(9000, ProtocolType.Udp);
        }

        private static IPAddress GetInternetIP()
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

        private bool TestNetwork_Accepted;
        private TcpListener TestNetwork_Listener; 

        public bool TestNetwork()
        {
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
            catch (SocketException)
            {
                //MessageBox.Show("SocketError: " + e);
                return false;
            }
            finally
            {
                DefowardPort(testPort, ProtocolType.Tcp);

                if (TestNetwork_Listener != null)
                    TestNetwork_Listener.Stop();
            }
        }

        private void TestNetwork_Listen()
        {
            try
            {
                TestNetwork_Listener.Start();
                TestNetwork_Listener.AcceptTcpClient();
                TestNetwork_Accepted = true;
            }
            catch (SocketException)
            {
                // Ignore.
            }
        }
    }
}
