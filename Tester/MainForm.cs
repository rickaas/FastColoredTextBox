using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Tester
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            this.flowLayoutPanel.WrapContents = true;
        }

        protected override void OnLoad(EventArgs e)
        {
            foreach (var ex in ExampleIndex.Examples)
            {
                var button = new Button()
                    {
                        Text = "Show",
                        Tag = ex,
                        Dock = DockStyle.Left,
                    };
                button.Click += ButtonOnClick;
                var label = new Label()
                    {
                        Text = ex.Description,
                        Dock = DockStyle.Fill,
                        AutoEllipsis = true,
                    };
                this.AddRow(button, label);
            }
            base.OnLoad(e);
        }

        private void ButtonOnClick(object sender, EventArgs eventArgs)
        {
            ExampleIndex.ExampleItem example = (ExampleIndex.ExampleItem) ((Button)sender).Tag;
            Form form = (Form) Activator.CreateInstance(example.FormType);     
            form.Show();

        }

        private void AddRow(Button button, Label label)
        {
            var panel = new Panel()
                {
                    Width = 200,
                    Height = 50,
                    BorderStyle = BorderStyle.Fixed3D
                };
            panel.Controls.Add(label);
            panel.Controls.Add(button);
            
            this.flowLayoutPanel.Controls.Add(panel);
        }

    }
}
