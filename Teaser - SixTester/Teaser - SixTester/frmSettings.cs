using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace TeaserSixTester
{
    public partial class frmSettings : Form
    {
        public event EventHandler evClosePressed;

        public frmSettings()
        {
            InitializeComponent();
            PropertyGridDefaults();
        }

        private void PropertyGridDefaults()
        {
            propertyGrid1.SelectedObject = SettingsHolder.Instance;
            propertyGrid1.PropertySort = PropertySort.Categorized;
        }



        private void btnClose_Click(object sender, EventArgs e)
        {
            evClosePressed.Raise("bla");
        }
    }
}
