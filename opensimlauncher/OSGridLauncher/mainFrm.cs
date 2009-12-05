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
            int x =0, y =0;
            Int32.TryParse(textBoxX.Text, out x);
            Int32.TryParse(textBoxY.Text, out y);

            textBoxX.Enabled = false;
            textBoxY.Enabled = false;
            checkBoxAutoposition.Enabled = false;
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            textBox3.Enabled = false;

            OpenSimConfigurator osc = new OpenSimConfigurator();
            osc.ConfigAndLaunch(textBox1.Text, textBox2.Text, textBox3.Text, progressBar1, toolStripStatusLabel1,
                                statusStrip1, checkBoxAutoposition.Checked, x, y);
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

                checkBoxAutoposition.Enabled = false;
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.osgrid.org/");
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private void textBoxX_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (textBoxX.ReadOnly)
                return;

            int d = -1;
            if (Int32.TryParse(textBoxX.Text, out d) && d.ToString() == textBoxX.Text)
            {
                if (Convert.ToInt32(textBoxX.Text) >= 65536 || Convert.ToInt32(textBoxX.Text) < 0)
                {
                    e.Cancel = true;
                    textBoxX.SelectAll();
                    errorProvider1.SetError(textBoxX, "Value must be between 0 and 65,535");
                    return;
                }
            }
            else
            {
                e.Cancel = true;
                textBoxX.SelectAll();
                errorProvider1.SetError(textBoxX, "Value must be numeric.");
                return;
            }
        }

        private void textBoxY_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(textBoxY.ReadOnly)
                return;

            int d = -1;
            if (Int32.TryParse(textBoxY.Text, out d) && d.ToString() == textBoxY.Text)
            {
                if (Convert.ToInt32(textBoxY.Text) >= 65536 || Convert.ToInt32(textBoxY.Text) < 0)
                {
                    e.Cancel = true;
                    textBoxY.SelectAll();
                    errorProvider1.SetError(textBoxY, "Value must be between 0 and 65,535");
                    return;
                }
            }
            else
            {
                e.Cancel = true;
                textBoxY.SelectAll();
                errorProvider1.SetError(textBoxY, "Value must be numeric.");
                return;
            }
        }

        private void textBox1_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (textBox1.ReadOnly)
                return;

            if(textBox1.Text.Length > 64)
            {
                e.Cancel = true;
                textBox1.SelectAll();
                errorProvider1.SetError(textBox1,"Must be less than 64 characters");
                return;
            }
            if(String.IsNullOrEmpty(textBox1.Text))
            {
                e.Cancel = true;
                textBox1.SelectAll();
                errorProvider1.SetError(textBox1, "Must be at least one character long.");
                return;
            }
        }

        private void checkBoxAutoposition_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxAutoposition.Checked)
            {
                textBoxX.ReadOnly = true;
                textBoxY.ReadOnly = true;
            }
            else
            {
                textBoxX.ReadOnly = false;
                textBoxY.ReadOnly = false;
            }
        }

        private void textBoxX_Validated(object sender, EventArgs e)
        {
            errorProvider1.SetError(textBoxX, "");
        }

        private void textBoxY_Validated(object sender, EventArgs e)
        {
            errorProvider1.SetError(textBoxY, "");
        }

        private void textBox1_Validated(object sender, EventArgs e)
        {
            errorProvider1.SetError(textBox1, "");
        }

        private void sponsorButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.simhost.com/");
        }

        private void textBoxY_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
