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
        map m=new map();
        public Form1()
        {
            InitializeComponent();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            m.setbox(pictureBox1.Height, pictureBox1.Width);
            m.setg(pictureBox1.CreateGraphics());
            OpenFileDialog opndlg = new OpenFileDialog();
            opndlg.InitialDirectory = "E:\\";
            opndlg.Filter = "所有文件|*.*|" + "OSM文件(*.osm) | *.osm";
            opndlg.Title = "打开地图文件";
            if (opndlg.ShowDialog() == DialogResult.OK)
            {
                string filename = opndlg.FileName;
                try
                {
                    if (!m.loadfile(filename))
                    {
                        MessageBox.Show("Map file read failed!");
                    }
                }
                catch (Exception exp)
                {
                    MessageBox.Show(exp.Message);
                }
            }
            m.paintmap();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //for test only
            Pen p = new Pen(Color.Blue, 2);
            m.g.DrawLine(p, -10, -10, 10, 10);
            //m.g.DrawLine(p, 0, 0, m.boxw, m.boxh);
        }
        
    }
    public class map
    {
        public Graphics g;//绘图区域
        bool isload;//是否已加载地图文件
        string filename;//地图文件路径
        float[] level=new float[4];//缩放级别
        int curlevel=0;//当前缩放级别下标
        float left, up, right, down;//经纬度边界
        float curleft=0, curup=0, curright=0, curdown=0;//当前地图显示经纬度边界
        public float boxh, boxw;
        float filesize;

        XmlDocument xmldoc=new XmlDocument();//使用XmlDocument将XML全部读入内存-小文件
        //XmlTextReader reader;//使用XmlTextReader顺序读取XML-大文件

        public void setg(Graphics ghs)
        {
            g = ghs;
        }
        public map(){}
        public void setbox(float h,float w){
            boxh=h;boxw=w;
        }
        public bool loadfile(string tfilename)
        {
            //unfinished
            filename = tfilename;
            try
            {
                xmldoc.Load(filename); //使用XmlDocument
                XmlNode node = xmldoc.SelectSingleNode("/osm/bounds");
                curleft=left = (float)Convert.ToSingle(node.Attributes["minlon"].Value);
                up=curup = (float)Convert.ToSingle(node.Attributes["maxlat"].Value);
                curright=right = (float)Convert.ToSingle(node.Attributes["maxlon"].Value);
                curdown=down = (float)Convert.ToSingle(node.Attributes["minlat"].Value);
                isload = true;
                determin_level();//根据读取的地图信息确定缩放级别
                return true;
            }
            catch (Exception e)
            {
                isload = false;
                MessageBox.Show(e.ToString());//debuginfo
                return false; 
            }
        }
        public bool findplace(string place)
        {
            return true;
        }
        private void determin_level()
        {
            float lx=boxw/(right-left);
            float ly=boxh/(up-down);
            level[0] = lx > ly ? lx : ly;//max of div
            for (int i = 1; i < 4; i++)
            {
                level[i] = level[i - 1] * 2;
            }
        }
        public void paintmap()
        {
            XmlNodeList wayNodeList = xmldoc.SelectNodes("/osm/way");
            XmlNodeList NodeList = xmldoc.SelectNodes("/osm/node");
            if (wayNodeList != null)
                foreach (XmlNode Node in wayNodeList)
                {
                    paintworker(Node);
                }
        }
        public void paintworker(XmlNode Node)
        {
            XmlNode ndNode = Node.SelectSingleNode("nd");
            XmlNode placenode = xmldoc.SelectSingleNode("/osm/node[@id='" + ndNode.Attributes["ref"].Value + "']");
            if (placenode == null)
                return;
            float lat = (float)Convert.ToSingle(placenode.Attributes["lat"].Value);
            float lon = (float)Convert.ToSingle(placenode.Attributes["lon"].Value);
            if (lat > curup || lat < curdown || lon > curright || lon < curleft)
                return;
            else
            {
                XmlNodeList ndNodes = Node.SelectNodes("nd");
                for (int i = 0; i < ndNodes.Count - 1; i++)
                {
                    XmlNode placenode_before = xmldoc.SelectSingleNode("/osm/node[@id='" + ndNodes[i].Attributes["ref"].Value + "']");
                    XmlNode placenode_after = xmldoc.SelectSingleNode("/osm/node[@id='" + ndNodes[i + 1].Attributes["ref"].Value + "']");
                    float conlat1 = (float)Convert.ToSingle(placenode_before.Attributes["lat"].Value);
                    float conlon1 = (float)Convert.ToSingle(placenode_before.Attributes["lon"].Value);
                    float conlat2 = (float)Convert.ToSingle(placenode_after.Attributes["lat"].Value);
                    float conlon2 = (float)Convert.ToSingle(placenode_after.Attributes["lon"].Value);
                    Pen p = new Pen(Color.Blue, 1);
                    drawline(conlon1, conlat1, conlon2, conlat2,p);
                }
            }
        }
        private void transform(float a,float b,out float x,out float y)
        {
            //已知地图经纬度(w,h) 返回绘图区域坐标
            x = (a - curleft) * level[curlevel];
            y = (curup-b) * level[curlevel];
        }
        private void drawline(float a1,float a2,float b1,float b2,Pen p){
            float newa1, newa2, newb1, newb2;
            transform(a1, a2, out newa1, out newa2);
            transform(b1, b2, out newb1, out newb2);
            if(a1<=boxw&&a1>=0&&b1<=boxw&&b1>=0&&a2<=boxh&&a2>=0&&b2<=boxh&&b2>=0)
                g.DrawLine(p, newa1, newa2, newb1, newb2);
        }

    }
}
