using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TeaserDSV
{
    public partial class fSettings : Form
    {
        public fSettings()
        {
            InitializeComponent();
            propertyGrid1.SelectedObject = SettingsHolder.Instance;
        }

        private void fSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            //update?
        }


    }
}
