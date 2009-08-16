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
using System.IO;
using System.Windows.Forms;

namespace OSGridLauncher
{
    public partial class mainFrm : Form
    {
        public mainFrm()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            OpenSimConfigurator osc = new OpenSimConfigurator();
            osc.ConfigAndLaunch(textBox1.Text, textBox2.Text, textBox3.Text, progressBar1, lblStatus);
        }

        private void mainFrm_Load(object sender, EventArgs e)
        {
            if(Directory.Exists(Path.Combine(Environment.CurrentDirectory,"opensim")))
            {
                textBox1.Text = "Already Configured";
                textBox1.ReadOnly = true;

                textBox2.Text = "Already Configured";
                textBox2.ReadOnly = true;

                textBox3.Text = "Already Configured";
                textBox3.ReadOnly = true;
            }
        }
    }
}
