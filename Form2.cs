using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Project
{
    public partial class Form2 : Form
    {

        private int total = 0;
        List<Color> colors;

        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            if (Form1.sold.Count == 0)
            {
                //MessageBox.Show("no books sold yet");
                label1.Text = "Total books sold: 0"; 
                return;
            }

            colors = new List<Color>();
            int clrValue = 0;
            int devide = 255 / Form1.sold.Count;

            foreach (var item in Form1.sold)
            {
                clrValue += devide;
                colors.Add(Color.FromArgb(clrValue, 255, 255 - clrValue / 4));
                listBox1.Items.Add(item.name);
                listBox2.Items.Add(item.times);

                total += item.times;
            }

            label1.Text = "Total books sold: " + total;

            this.Paint += drawGraf;
            this.Paint += drawColors;
            Invalidate();

        }

        private void drawGraf(object sender, PaintEventArgs e)
        {

            Rectangle r = new Rectangle(70, 160 / 2, 300, 300);
            float startAngle = 0;
            for (int i = 0; i < colors.Count; i++)
            {
                float endAngle = 360f * Form1.sold[i].times / total;

                e.Graphics.FillPie(new SolidBrush(colors[i]), r, startAngle, endAngle);
                e.Graphics.DrawPie(new Pen(Color.Black), r, startAngle, endAngle);
                startAngle += endAngle;
            }

            //e.Graphics.DrawEllipse(new Pen(Color.Black), r);

        }

        private void drawColors(object sender, PaintEventArgs e)
        {

            Point position = listBox1.Location;
            int size = 14;
            position.X -= size;

            foreach (var item in colors)
            {
                Rectangle r = new Rectangle(position.X, position.Y, size - 5, size - 5);
               
                e.Graphics.FillEllipse(new SolidBrush(item), r);
                e.Graphics.DrawEllipse(new Pen(Color.Black), r);

                position.Y += size;
            }

        }

    }
}
