﻿/*
 Copyright (c) 2015 [Joerg Ruedenauer]
 
 This file is part of Ares.

 Ares is free software; you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation; either version 2 of the License, or
 (at your option) any later version.

 Ares is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with Ares; if not, write to the Free Software
 Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Ares.Player
{
    public partial class NetworkPage : UserControl
    {
        public NetworkPage()
        {
            InitializeComponent();
            if (!IsLinux)
            {
                shieldBox.Image = System.Drawing.SystemIcons.Shield.ToBitmap();
                new ToolTip().SetToolTip(shieldBox, StringResources.ShieldIconToolTip);
            }
            else
            {
                shieldBox.Visible = false;
            }
            SetData();
        }

        private bool listen = true;

        private void SetData()
        {
            listen = false;
            Ares.Settings.Settings settings = Ares.Settings.Settings.Instance;
            enableCustomIpBox.Checked = settings.UseLegacyNetwork;
            enableWebBox.Checked = settings.UseWebNetwork;
            customIpPortUpDown.Value = settings.TcpPort;
            webPortUpDown.Value = settings.WebTcpPort;
            webPortUpDown.Enabled = settings.UseWebNetwork;
            customIpPortUpDown.Enabled = settings.UseLegacyNetwork;
            udpPortUpDown.Value = settings.UdpPort;
            bool foundAddress = false;
            bool foundIPv4Address = false;
            int ipv4AddressIndex = 0;

            foreach (System.Net.IPAddress address in System.Net.Dns.GetHostAddresses(String.Empty))
            {
                //if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                //    continue;
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    foundIPv4Address = true;
                    ipv4AddressIndex = ipAddressBox.Items.Count;
                }
                String s = address.ToString();
                ipAddressBox.Items.Add(s);
                if (s == Settings.Settings.Instance.IPAddress)
                    foundAddress = true;
            }
            if (foundAddress)
            {
                ipAddressBox.SelectedItem = Settings.Settings.Instance.IPAddress;
            }
            else if (ipAddressBox.Items.Count > 0)
            {
                ipAddressBox.SelectedIndex = foundIPv4Address ? ipv4AddressIndex : 0;
                Settings.Settings.Instance.IPAddress = ipAddressBox.SelectedItem.ToString();
            }
            else
            {
                ipAddressBox.Enabled = false;
                enableCustomIpBox.Enabled = false;
                enableWebBox.Enabled = false;
                customIpPortUpDown.Enabled = false;
                webPortUpDown.Enabled = false;
                udpPortUpDown.Enabled = false;
            }

            listen = true;
        }

        private static bool IsLinux
        {
            get
            {
                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }

        private void ChangeSecuritySettings(String args)
        {
            String tempFile = Path.GetTempFileName();
            try
            {
                args += " " + tempFile;
                ProcessStartInfo psi = new ProcessStartInfo("Ares.WinSecurity.exe", args);
                psi.Verb = "runas";
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.UseShellExecute = true;

                var res = Process.Start(psi);
                res.WaitForExit();
                if (res.ExitCode != 0)
                {
                    using (StreamReader reader = new StreamReader(tempFile))
                    {
                        string errorMsg = reader.ReadLine();
                        if (!String.IsNullOrEmpty(errorMsg))
                        {
                            MessageBox.Show(this, String.Format(StringResources.WinSecurityError, errorMsg), StringResources.Ares, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            while (!String.IsNullOrEmpty(errorMsg))
                            {
                                errorMsg = reader.ReadLine();
                            }
                        }
                        else
                        {
                            MessageBox.Show(this, String.Format(StringResources.WinSecurityError, "Unknown"), StringResources.Ares, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, String.Format(StringResources.WinSecurityError, ex.Message), StringResources.Ares, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch (IOException)
                { }
            }
        }

        private void SaveData()
        {
            Ares.Settings.Settings settings = Ares.Settings.Settings.Instance;
            settings.UseLegacyNetwork = enableCustomIpBox.Checked;
            if (settings.WebTcpPort != (int)webPortUpDown.Value)
            {
                int newPort = (int)webPortUpDown.Value;
                if (!IsLinux && enableWebBox.Checked && settings.UseWebNetwork)
                {
                    String args = String.Format("ChangePort {0} {1}", settings.WebTcpPort, newPort);
                    ChangeSecuritySettings(args);
                }
                settings.WebTcpPort = newPort;
            }
            if (settings.UseWebNetwork != enableWebBox.Checked)
            {
                if (!IsLinux)
                {
                    if (enableWebBox.Checked)
                    {
                        String args = String.Format("AddPort {0}", settings.WebTcpPort);
                        ChangeSecuritySettings(args);
                    }
                    else
                    {
                        String args = "RemovePort";
                        ChangeSecuritySettings(args);
                    }
                }
                settings.UseWebNetwork = enableWebBox.Checked;
            }
            settings.TcpPort = (int)customIpPortUpDown.Value;
            settings.IPAddress = ipAddressBox.SelectedItem.ToString();
            settings.UdpPort = (int)udpPortUpDown.Value;
        }

        public void OnConfirm()
        {
            SaveData();
        }

        private void enableWebBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!listen)
                return;
            webPortUpDown.Enabled = enableWebBox.Checked;
        }

        private void enableCustomIpBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!listen)
                return;
            customIpPortUpDown.Enabled = enableCustomIpBox.Checked;
        }
    }

    class NetworkPageHost : Ares.CommonGUI.ISettingsDialogPage
    {
        private NetworkPage m_Page = new NetworkPage();

        public Control Page
        {
            get { return m_Page; }
        }

        public string PageTitle
        {
            get { return StringResources.Network; }
        }

        public void OnConfirm()
        {
            m_Page.OnConfirm();
        }
    }
}