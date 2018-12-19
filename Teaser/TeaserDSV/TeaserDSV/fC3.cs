﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TeaserDSV
{
    public partial class fC3 : Form
    {
        private bool IsStarted = false;
        private fInjectedImage frmDisplay;
        public fC3()
        {
            InitializeComponent();
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fSettings frm = new fSettings();
            frm.Show();
           
        }

        private void UpdateButton()
        {
            if (IsStarted)
            {
                button1.Text = "Start";
                IsStarted = false;
                frmDisplay.Close();
                button1.Invalidate();
                button1.Refresh();
            }
            else
            {
                button1.Text = "Stop";
                IsStarted = true;
                button1.Invalidate();
                button1.Refresh();
                frmDisplay = new fInjectedImage();
                frmDisplay.Show();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            UpdateButton();
        }
    }
}
