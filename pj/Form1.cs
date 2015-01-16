using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace pj
{
    
    public partial class Form1 : Form
    {
        map m;
        double[] mousepos=new double[2];
        public Form1()
        {
            InitializeComponent();
            this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.panel1_MouseWheel);
        }
        private void panel1_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Delta > 0&&m!=null)
            {
                m.zoomnow *= 2;
                m.clearpic();
                m.draw();
            }
            else if (e.Delta < 0 && m != null)
            {
                double t = m.zoomnow / 2;
                if (t < m.zoommin)
                {
                    m.zoomnow = m.zoommin;
                }
                else
                {
                    m.zoomnow = t;
                }
                m.clearpic();
                m.draw();
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            mousepos[0] = e.Y;
            mousepos[1] = e.X;
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (m == null)
                return;
            double changeposx = (e.Y - mousepos[0]) / m.zoomnow;
            double changeposy = (e.X - mousepos[1]) / m.zoomnow;
            m.curedge[0] += changeposx;
            m.curedge[1] -= changeposy;
            m.curedge[0] = Math.Max(m.bounds[0], m.curedge[0]);
            m.curedge[0] = Math.Min(m.bounds[2] - m.box[0] / m.zoomnow, m.curedge[0]);
            m.curedge[1] = Math.Max(m.bounds[1], m.curedge[1]);
            m.curedge[1] = Math.Min(m.bounds[3] - m.box[1] / m.zoomnow, m.curedge[1]);
            m.clearpic();
            m.draw();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
        }
        private void pictureBox1_Paint(object sender, PaintEventArgs e){
        }
        private void button1_Click(object sender, EventArgs e)
        {
            

            OpenFileDialog opndlg = new OpenFileDialog();
            opndlg.InitialDirectory = "E:\\";
            opndlg.Filter = "所有文件|*.*|" + "OSM文件(*.osm) | *.osm";
            opndlg.Title = "打开地图文件";
            if (opndlg.ShowDialog() == DialogResult.OK)
            {
                string filename = opndlg.FileName;
                m = new map(pictureBox1.CreateGraphics(), filename, pictureBox1.Height, pictureBox1.Width);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (m != null)
            {
                pictureBox1.Image = m.bitmap;
                label3.Text = ((int)m.zoomnow).ToString();
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            if (m == null)
            {
                MessageBox.Show("Please Load Map File First");
                return;
            }
            if (textBox1.Text == "")
            {
                MessageBox.Show("Please Enter place name");
                return;
            }
            m.search(textBox1.Text);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            m.findroad(textBox2.Text, textBox3.Text);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
        
    }
}
