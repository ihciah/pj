﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Xml;
using System.Windows.Forms;


namespace pj
{
    
    public class map
    {
        double[] blockbounds = new double[4];
        public int isblocked = 0;
        Graphics g,gmain;
        public Bitmap bitmap;
        HashSet<long>[,] indexway;
        HashSet<long>[,] indexbuilding;
        List<long> shortestway=new List<long>();
        Dictionary<long, building> buildings = new Dictionary<long, building>();//非道路
        Dictionary<long, way> ways = new Dictionary<long, way>();//道路
        Dictionary<long, node> nodedict = new Dictionary<long, node>();//nodeid与对应点
        Dictionary<string, nodeinfo> places = new Dictionary<string, nodeinfo>();//名称与对应nodeid或wayid、类型
        List<long> spotlist = new List<long>();//搜索出来的点
        List<long> dirlist = new List<long>();//搜索出来的路径
        public double[] bounds=new double[4];
        public double[] curedge=new double[4];
        public int[] box=new int[2];
        public double zoomnow;
        public double zoommin;
        int[] indexsize;
        Random rnd = new Random();
        public struct way
        {
            public long id;
            public List<long> nodelist;
            public string name;
            public char highway;
            public double[] pos;
            public way(long id, string name,char highway,double[] pos)
            {
                this.id = id;
                nodelist = new List<long>();
                this.name = name;
                this.highway = highway;
                this.pos = pos;
            }
            public way(long id, string name, List<long> nodelist, char highway, double[] pos)
            {
                this.id = id;
                this.nodelist = nodelist;
                this.name = name;
                this.highway = highway;
                this.pos = pos;
            }
        };
        public struct iddis
        {
            public long id;
            public double dis;
            public double wdis;
            public iddis(long id, double dis,double wdis)
            {
                this.id = id;
                this.dis=dis;
                this.wdis = wdis;
            }
        }
        public struct building
        {
            public long id;
            public List<long> nodelist;
            public string name;
            public double[] pos;
            public building(long id, string name, double[] pos)
            {
                this.id = id;
                nodelist = new List<long>();
                this.name = name;
                this.pos = pos;
            }
            public building(long id, List<long> nodelist, string name, double[] pos)
            {
                this.id = id;
                this.nodelist = nodelist;
                this.name = name;
                this.pos = pos;
            }
        }
        public struct node
        {
            public double lat, lon;
            public string name;
            public List<iddis> adj;
            public node(double lat, double lon, string name)
            {
                this.lat = lat;
                this.lon = lon;
                this.name = name;
                adj = new List<iddis>();
            }
        };
        public struct nodeinfo
        {
            public long id;
            public bool isroad;
            public bool isnode;
            public nodeinfo(long id,bool isroad,bool isnode)
            {
                this.id = id;
                this.isroad = isroad;
                this.isnode = isnode;
            }
        }
        public class rnode : IComparable<rnode>
        {
            public long id;
            public double dis;
            public bool vis;
            public rnode(long id)
            {
                this.id = id;
                dis = 999;
                vis = false;
            }
            public int CompareTo(rnode y)
            {
                if (this.dis < y.dis)
                    return 1;
                else if (this.id == y.id)
                    return 0;
                else
                    return -1;
            }
        }
        class PriorityQueue<T>
        {
            IComparer<T> comparer;
            T[] heap;

            public int Count;
            public PriorityQueue() : this(null) { }
            public PriorityQueue(IComparer<T> comparer) : this(16, comparer) { }
            public PriorityQueue(int capacity, IComparer<T> comparer)
            {
                this.comparer = (comparer == null) ? Comparer<T>.Default : comparer;
                this.heap = new T[capacity];
            }

            public void Push(T v)
            {
                if (Count >= heap.Length) Array.Resize(ref heap, Count * 2);
                heap[Count] = v;
                SiftUp(Count++);
            }

            public T Pop()
            {
                var v = Top();
                heap[0] = heap[--Count];
                if (Count > 0) SiftDown(0);
                return v;
            }

            public T Top()
            {
                if (Count > 0) return heap[0];
                throw new InvalidOperationException("优先队列为空");
            }

            void SiftUp(int n)
            {
                var v = heap[n];
                for (var n2 = n / 2; n > 0 && comparer.Compare(v, heap[n2]) > 0; n = n2, n2 /= 2) heap[n] = heap[n2];
                heap[n] = v;
            }

            void SiftDown(int n)
            {
                var v = heap[n];
                for (var n2 = n * 2; n2 < Count; n = n2, n2 *= 2)
                {
                    if (n2 + 1 < Count && comparer.Compare(heap[n2 + 1], heap[n2]) > 0) n2++;
                    if (comparer.Compare(v, heap[n2]) >= 0) break;
                    heap[n] = heap[n2];
                }
                heap[n] = v;
            }
        }
        
        public map(Graphics pg, string file,int height,int width)
        {
            g = pg;
            box[0] = height;
            box[1] = width;
            bitmap = new Bitmap(width, height);
            g = Graphics.FromImage(bitmap);
            gmain = pg;
            readfile(file);
            draw();
        }
        public bool readfile(string name)
        {
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(name);

            XmlNode bound = xmldoc.SelectNodes("/osm")[0].SelectSingleNode("bounds");
            bounds[0] = Convert.ToDouble(bound.Attributes["minlat"].Value);
            bounds[1] = Convert.ToDouble(bound.Attributes["minlon"].Value);
            bounds[2] = Convert.ToDouble(bound.Attributes["maxlat"].Value);
            bounds[3] = Convert.ToDouble(bound.Attributes["maxlon"].Value);
            //----------读取bound数据


            XmlNodeList NodeList = xmldoc.SelectNodes("/osm/node");
            foreach (XmlNode node in NodeList)
            {
                XmlNodeList childnodes = node.ChildNodes;
                string nodename=null;
                if (childnodes != null)
                {
                    foreach (XmlNode tagnode in childnodes)
                    {
                        string tagn = tagnode.Attributes["k"].Value;
                        if (tagn != null && tagn == "name")
                        {
                            nodename = tagnode.Attributes["v"].Value;
                            break;
                        }
                        else if(tagn != null &&tagn=="name:en"&&nodename==null)
                        {
                            nodename = tagnode.Attributes["v"].Value;
                        }
                    }
                }
                double lat=Convert.ToDouble(node.Attributes["lat"].Value);
                double lon=Convert.ToDouble(node.Attributes["lon"].Value);
                long nodeid=Convert.ToInt64(node.Attributes["id"].Value);
                node inode = new node(lat, lon, nodename);
                nodedict[nodeid] = inode;

                if (nodename != null)
                {
                    places[nodename] = new nodeinfo(nodeid,false,true);
                }
            }
            //----------读取node数据

            NodeList = xmldoc.SelectNodes("/osm/way");
            foreach (XmlNode waynode in NodeList)
            {
                double[] pos = new double[4];
                pos[1] = 0;
                long wayid = Convert.ToInt64(waynode.Attributes["id"].Value);
                List<long> nodelist=new List<long>();
                string wayname=null;
                XmlNodeList nds = waynode.SelectNodes("nd");
                foreach (XmlNode nd in nds)
                {
                    long ndid = Convert.ToInt64(nd.Attributes["ref"].Value);
                    nodelist.Add(ndid);
                    if(pos[1]==0)
                    {
                        pos[0] = pos[2] = nodedict[ndid].lat;
                        pos[1] = pos[3] = nodedict[ndid].lon;
                    }
                    pos[1] = Math.Min(pos[1], nodedict[ndid].lon);
                    pos[3] = Math.Max(pos[3], nodedict[ndid].lon);
                    pos[0] = Math.Min(pos[0], nodedict[ndid].lat);
                    pos[2] = Math.Max(pos[2], nodedict[ndid].lat);
                }

                XmlNodeList childwaynode = waynode.SelectNodes("tag");

                XmlNode n = waynode.SelectSingleNode("tag[@k='name']");
                if (n != null)
                    wayname = n.Attributes["v"].Value;
                else
                {
                    n = waynode.SelectSingleNode("tag[@k='name:en']");
                    if (n != null)
                        wayname = n.Attributes["v"].Value;
                }

                bool tmp = true;
                foreach (XmlNode no in childwaynode)
                {
                    if (no.Attributes["k"].Value == "highway")
                    {
                        string x = no.Attributes["v"].Value;
                        char c = 'u';
                        switch (x)
                        {
                            case "unclassified": c = 'u'; break;
                            case "residential": c = 'r'; break;
                            case "tertiary": c = 't'; break;
                            case "secondary": c = 's'; break;
                            case "footway": c = 'f'; break;
                            case "service": c = 'e'; break;
                            case "motorway": c = 'm'; break;
                            case "trunk": c = 'k'; break;
                            case "primary": c = 'p'; break;
                            case "track": c = 'c'; break;
                        }
                        ways[wayid] = new way(wayid, wayname, nodelist, c,pos);
                        if (wayname != null)
                            places[wayname] = new nodeinfo(wayid, false,false);
                        tmp = false;
                        break;
                    }
                }
                if (tmp)
                {
                    buildings[wayid] = new building(wayid, nodelist, wayname,pos);
                    if(wayname!=null)
                        places[wayname] = new nodeinfo(wayid, false,false);
                }
                
            }
            //---------读取way数据


            NodeList = xmldoc.SelectNodes("/osm/relation");
            foreach (XmlNode relationnode in NodeList)
            {
                XmlNode way = relationnode.SelectSingleNode("member");
                if (way==null||way.Attributes["type"].Value != "way")
                    continue;
                string wayname=null;
                long wayid = Convert.ToInt64(way.Attributes["ref"].Value);
                XmlNode n = relationnode.SelectSingleNode("tag[@k='name']");
                if (n != null)
                    wayname = n.Attributes["v"].Value;
                else
                {
                    XmlNode n2 = relationnode.SelectSingleNode("tag[@k='name:en']");
                    if (n != null)
                        wayname = n.Attributes["v"].Value;
                }
                if (wayname != null)
                {
                    places[wayname] = new nodeinfo(wayid, false, false);
                }

            }

            //---------读取relation数据

            indexsize = new int[2];
            indexsize = whichindex(bounds[2], bounds[3]);
            indexway = new HashSet<long>[indexsize[0] + 1, indexsize[1] + 1];
            indexbuilding = new HashSet<long>[indexsize[0] + 1, indexsize[1] + 1];
            for (int i = 0; i <= indexsize[0]; i++)
            {
                for (int j = 0; j <= indexsize[1]; j++)
                {
                    indexway[i, j] = new HashSet<long>();
                    indexbuilding[i, j] = new HashSet<long>();
                }
            }

                foreach (way w in ways.Values)
                {
                    int[] min = whichindex(w.pos[0], w.pos[1]);
                    int[] max = whichindex(w.pos[2], w.pos[3]);
                    max[0] = Math.Min(max[0], indexsize[0]);
                    max[1] = Math.Min(max[1], indexsize[1]);
                    min[0] = Math.Max(min[0], 0);
                    min[1] = Math.Max(min[1], 0);
                    for (int i = min[0]; i <= max[0]; i++)
                    {
                        for (int j = min[1]; j <= max[1]; j++)
                        {
                            indexway[i, j].Add(w.id);
                        }
                    }
                }
            foreach (building b in buildings.Values)
            {
                int[] min = whichindex(b.pos[0], b.pos[1]);
                int[] max = whichindex(b.pos[2], b.pos[3]);
                max[0] = Math.Min(max[0], indexsize[0]);
                max[1] = Math.Min(max[1], indexsize[1]);
                min[0] = Math.Max(min[0], 0);
                min[1] = Math.Max(min[1], 0);
                for (int i = min[0]; i <= max[0]; i++)
                {
                    for (int j = min[1]; j <= max[1]; j++)
                    {
                        indexbuilding[i, j].Add(b.id);
                    }
                }
            }
            //构建索引

            foreach (way w in ways.Values)
            {
                double weigh = 1;
                switch (w.highway){
                    case 'm': weigh = 0.7; break;
                    case 'k': weigh = 0.7; break;
                    case 'p': weigh = 0.9; break;
                    case 's': weigh = 1; break;
                    case 't': weigh = 1.1; break;
                    case 'u': weigh = 1.3; break;
                    case 'r': weigh = 1.2; break;
                    case 'e': weigh = 1.3; break;
                    case 'c': weigh = 1.4; break;
                }
                for (int i = 1; i < w.nodelist.Count; i++)
                {
                    double dis=Math.Sqrt(calcd(nodedict[w.nodelist[i]].lat,nodedict[w.nodelist[i]].lon,nodedict[w.nodelist[i-1]].lat,nodedict[w.nodelist[i-1]].lon));
                    double ndis = dis * weigh;
                    iddis idanddis=new iddis(w.nodelist[i-1],dis,ndis);
                    iddis idanddis2 = new iddis(w.nodelist[i], dis,ndis);
                    nodedict[w.nodelist[i]].adj.Add(idanddis);
                    nodedict[w.nodelist[i-1]].adj.Add(idanddis2);
                }
            }
            //构建距离信息

            double zoomh = (double)box[0] / (bounds[2] - bounds[0]);
            double zoomw = (double)box[1] / (bounds[3] - bounds[1]);
            zoomnow=zoommin = zoomh < zoomw ? zoomw : zoomh;
            for (int o = 0; o < 4;o++ )
                curedge[o] = bounds[o];
            //初始化

            return true;
        }
        public void draw()
        {
            //读取curedge部分坐标和zoomnow绘图
            
            HashSet<long> paintsetway = new HashSet<long>();
            HashSet<long> paintsetbuilding = new HashSet<long>();

            curedge[3] = curedge[1] + box[1] / zoomnow;
            curedge[2] = curedge[0] + box[0] / zoomnow;
            if(curedge[2]>bounds[2])
            {
                curedge[2] = bounds[2];
                curedge[0] = bounds[2] - box[0] / zoomnow;
                curedge[0] = Math.Max(curedge[0], bounds[0]);
            }
            if (curedge[3] > bounds[3])
            {
                curedge[3] = bounds[3];
                curedge[1] = bounds[1] - box[1] / zoomnow;
                curedge[1] = Math.Max(curedge[1], bounds[1]);
            }
            int[] min = whichindex(curedge[0], curedge[1]);
            int[] max = whichindex(curedge[2], curedge[3]);
            max[0] = Math.Min(max[0], indexsize[0]);
            max[1] = Math.Min(max[1], indexsize[1]);
            min[0] = Math.Max(min[0], 0);
            min[1] = Math.Max(min[1], 0);
            for (int i = min[0]; i <= max[0]; i++)
            {
                    for (int j = min[1]; j <= max[1]; j++)
                    {
                        paintsetway.UnionWith(indexway[i, j]);
                        paintsetbuilding.UnionWith(indexbuilding[i, j]);
                    }
            }
            foreach (long w in paintsetway)
            {
                drawway(ways[w]);
            }
            foreach (long b in paintsetbuilding)
            {
                drawway(buildings[b]);
            }
            drawspotanddir();//画搜索出的点和路径

            if (isblocked == 2)
            {
                Pen p = new Pen(Color.Black, 5);
                double[] poss = new double[2];
                double[] pose = new double[2];
                poss = trans(blockbounds[2], blockbounds[1], zoomnow, curedge);
                pose = trans(blockbounds[0], blockbounds[3], zoomnow, curedge);
                g.DrawRectangle(p, (int)poss[0], (int)poss[1], (int)pose[0] - (int)poss[0], (int)pose[1] - (int)poss[1]);
            }
        }
        
        private void drawway(way w)
        {
            Pen p = new Pen(Color.FromArgb(100, 222, 223, 231), 1);
            switch (w.highway)
            {
                case 'm': p = new Pen(Color.Blue, 5); break;
                case 'k': p = new Pen(Color.Green, 4); break;
                case 'p': if (zoomnow < 2000) return; else { p = new Pen(Color.Orange, 4); } break;
                case 's': if (zoomnow < 2500) return; else { p = new Pen(Color.Yellow, 3); } break;
                case 't': if (zoomnow < 3000) return; else { p = new Pen(Color.White, 2); } break;
                case 'u': if (zoomnow < 6000) return; else { p = new Pen(Color.White, 2); } break;
                case 'r': if (zoomnow < 7000) return; else { p = new Pen(Color.White, 2); } break;
                case 'e': if (zoomnow < 8000) return; else { p = new Pen(Color.White, 1); } break;
                case 'c': if (zoomnow < 30000) return; else { p = new Pen(Color.White, 1); } break;
            }
            if (w.nodelist.Count <= 1)
                return;
            Point[] li = new Point[w.nodelist.Count];
            int co=0;
            foreach (long id in w.nodelist)
            {
                double []pos=new double[2];
                pos=trans(nodedict[id].lat, nodedict[id].lon, zoomnow,curedge);
                li[co++]=new Point((int)pos[0], (int)pos[1]);
            }
            g.DrawLines(p, li);
        }
        private void drawway(building w)
        {
            if (w.nodelist.Count <= 1)
                return;
            Point[] li = new Point[w.nodelist.Count];
            int co = 0;
            foreach (long id in w.nodelist)
            {
                double[] pos = new double[2];
                pos = trans(nodedict[id].lat, nodedict[id].lon, zoomnow,curedge);
                li[co++] = new Point((int)pos[0], (int)pos[1]);
            }
            Pen p = new Pen(Color.FromArgb(100, 222, 223, 231), 1);
            SolidBrush b = new SolidBrush(Color.FromArgb(100, 222, 223, 231));

            if(li[li.Length-1]==li[0])
                g.FillPolygon(b, li);
            else
                g.DrawLines(p, li);

            double prob = 0.5;
            if (zoomnow < 5000)
                prob = 2;
            else if (zoomnow < 8000)
                prob = 4;
            else if (zoomnow < 12000)
                prob = 10;
            else if (zoomnow < 24000)
                prob = 50;
            else if (zoomnow < 30000)
                prob = 80;
            else
                prob = 100;
            
            if (rnd.Next(100)<=prob&&w.name != null && w.name != "")
            {
                g.DrawString(w.name, new Font("宋体", 9), new SolidBrush(Color.Tomato), li[0]);
            }
            
        }
        private void drawspotanddir()
        {
            foreach (long id in spotlist)
            {
                double[] pos = new double[2];
                pos = trans(nodedict[id].lat, nodedict[id].lon, zoomnow,curedge);
                Pen p = new Pen(Color.Red, 5);
                g.DrawLine(p, (int)pos[0], (int)pos[1], (int)pos[0] + 5, (int)pos[1] + 5);

                if (nodedict[id].name != null && nodedict[id].name != "")
                {
                    Point x = new Point((int)pos[0], (int)pos[1]);
                    g.DrawString(nodedict[id].name, new Font("宋体", 9), new SolidBrush(Color.Tomato), x);
                }
            }
            if (dirlist.Count == 0)
                return;
            int co = 0;
            Point[] li = new Point[dirlist.Count];
            foreach (long id in dirlist)
            {
                double[] pos = new double[2];
                pos = trans(nodedict[id].lat, nodedict[id].lon, zoomnow, curedge);
                li[co++] = new Point((int)pos[0], (int)pos[1]);
            }
            Pen rp = new Pen(Color.Red, 5);
            g.DrawLines(rp, li);
        }
        public void clearpic()
        {
            Color col = Color.FromArgb(100,240, 237, 229);
            g.Clear(col);
        }
        private double[] trans(double lat, double lon,double zoomnow,double[] leastpos)
        {
            //返回绘图可直接使用的坐标（按绘图顺序）
            double[] res = new double[2];
            res[0] = (lon - leastpos[1]) * zoomnow;
            res[1] = box[0]-(lat - leastpos[0]) * zoomnow;
            return res;
        }
        private int[] whichindex(double lat, double lon)
        {
            int[] res = new int[2];
            res[0] = (int)((lat - bounds[0]) / 0.01);
            res[1] = (int)((lon - bounds[1]) / 0.01);
            return res;
        }
        public void search(string name)
        {
            if (places.ContainsKey(name))
            {
                spotlist.Clear();
                if (nodedict.ContainsKey(places[name].id))
                {
                    spotlist.Add(places[name].id);
                }
                else if (ways.ContainsKey(places[name].id))
                {
                    spotlist.Add(ways[places[name].id].nodelist[0]);
                }
                else if (buildings.ContainsKey(places[name].id))
                {
                    spotlist.Add(buildings[places[name].id].nodelist[0]);
                }
                clearpic();
                draw();
            }
            else
            {
                MessageBox.Show("Place " + name + " not found.");
            }
        }
        public void findroad(string name1, string name2,int option)
        {
            if (!places.ContainsKey(name1) || !places.ContainsKey(name2))
            {
                MessageBox.Show("起始点或终点未找到");
                return;
            }
            long st,en;
            if (!places[name1].isroad)
            {
                st = findnear(places[name1].id);
            }
            else
            {
                st = places[name1].id;
            }
            if (!places[name2].isroad)
            {
                en = findnear(places[name2].id);
            }
            else
            {
                en = places[name2].id;
            }
            spotlist.Add(st);
            spotlist.Add(en);
            if(option==1)
                dijk(st, en,1);
            else
                dijk(st, en);
        }
        public void dijk(long st, long en)
        {
            Dictionary<long, rnode> D = new Dictionary<long, rnode>();
            Dictionary<long, long> direc = new Dictionary<long, long>();
            PriorityQueue<rnode> Q = new PriorityQueue<rnode>();


            foreach (KeyValuePair<long, node> i in nodedict)
            {
                D[i.Key] = new rnode(i.Key);
            }
            D[st].dis = 0;
            Q.Push(D[st]);
            while (Q.Count != 0)
            {
                rnode x = Q.Top();
                Q.Pop();
                if (x.vis)
                    continue;
                if (isblocked == 2)
                {
                    node tt = nodedict[x.id];
                    if (tt.lat > blockbounds[0] && tt.lat < blockbounds[2] && tt.lon < blockbounds[3] && tt.lon > blockbounds[1])
                        continue;
                }
                D[x.id].vis = true;
                foreach (iddis l in nodedict[x.id].adj)
                {
                    if (D[l.id].dis > x.dis + l.dis)
                    {
                        D[l.id].dis = x.dis + l.dis;
                        direc[l.id] = x.id;
                        Q.Push(D[l.id]);
                    }
                    if (l.id == en)
                    {
                        //copy direc to path
                        long t = en;
                        dirlist.Clear();
                        dirlist.Add(en);
                        while (t != st)
                        {
                            t = direc[t];
                            dirlist.Add(t);
                        }
                        return;
                    }
                }
            }
            MessageBox.Show("Oops!路径没找到");
        }
        private long tonode(long id){
            if (!nodedict.ContainsKey(id))
            {
                if (ways.ContainsKey(id))
                {
                    id=ways[id].nodelist[0];
                }
                else if (buildings.ContainsKey(id))
                {
                    id = buildings[id].nodelist[0];
                }
                else
                {
                    MessageBox.Show("地点未找到");
                }
            }
            return id;
        }
        public long findnear(long id)
        {
            id = tonode(id);
            double lat=nodedict[id].lat;
            double lon=nodedict[id].lon;
            double distance=-1;
            int [] x= whichindex(lat,lon);
            long res = 0 ;
            if (indexway[x[0], x[1]].Count == 0)
            {
                if (x[0] != 0) x[0]--;
            }
            else if (indexway[x[0], x[1]].Count == 0)
            {
                if (x[1] != 0) x[1]--;
            }
                
            foreach (long w in indexway[x[0],x[1]])
            {
                foreach (long k in ways[w].nodelist)
                {
                    double wlat = nodedict[k].lat;
                    double wlon = nodedict[k].lon;
                    double xdis = calcd(lat, lon, wlat, wlon);
                    if (distance == -1)
                    {
                        distance = xdis;
                        res = k;
                    }
                    else
                    {
                        if (xdis < distance)
                        {
                            distance = xdis;
                            res = k;
                        }
                    }
                }
            }
            return res;
        }
        private double calcd(double x1, double y1, double x2, double y2)
        {
            return (x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1);
        }
        public double getdis(long st, long en)
        {
            foreach (iddis x in nodedict[st].adj)
            {
                if (x.id == en)
                {
                    return x.dis;
                }
            }
            foreach (iddis x in nodedict[en].adj)
            {
                if (x.id == st)
                {
                    return x.dis;
                }
            }
            return 999;//INF
        }
        public void click(double lon, double lat)
        {
            if (isblocked == 0 || isblocked == 2)
            {
                blockbounds[0] = lat;
                blockbounds[1] = lon;
                isblocked = 1;
            }
            else if (isblocked == 1)
            {
                blockbounds[2] = lat;
                blockbounds[3] = lon;
                double tmin, tmax;
                tmin = Math.Min(blockbounds[0], blockbounds[2]);
                tmax = Math.Max(blockbounds[0], blockbounds[2]);
                blockbounds[0] = tmin; blockbounds[2] = tmax;
                tmin = Math.Min(blockbounds[1], blockbounds[3]);
                tmax = Math.Max(blockbounds[1], blockbounds[3]);
                blockbounds[1] = tmin; blockbounds[3] = tmax;
                isblocked = 2;
                clearpic();
                draw();
            }
        }

        public void dijk(long st, long en,int option)
        {
            Dictionary<long, rnode> D = new Dictionary<long, rnode>();
            Dictionary<long, long> direc = new Dictionary<long, long>();
            PriorityQueue<rnode> Q = new PriorityQueue<rnode>();


            foreach (KeyValuePair<long, node> i in nodedict)
            {
                D[i.Key] = new rnode(i.Key);
            }
            D[st].dis = 0;

            Q.Push(D[st]);
            while (Q.Count != 0)
            {
                rnode x = Q.Top();
                Q.Pop();

                if (x.vis)
                    continue;
                if (isblocked == 2)
                {
                    node tt = nodedict[x.id];
                    if (tt.lat > blockbounds[0] && tt.lat < blockbounds[2] && tt.lon < blockbounds[3] && tt.lon > blockbounds[1])
                        continue;
                }
                D[x.id].vis = true;
                foreach (iddis l in nodedict[x.id].adj)
                {
                    if (D[l.id].dis > x.dis + l.wdis)
                    {
                        D[l.id].dis = x.dis + l.wdis;
                        direc[l.id] = x.id;
                        Q.Push(D[l.id]);
                    }
                    if (l.id == en)
                    {
                        //copy direc to path
                        long t = en;
                        dirlist.Clear();
                        dirlist.Add(en);
                        while (t != st)
                        {
                            t = direc[t];
                            dirlist.Add(t);
                        }
                        return;
                    }
                }
            }
            MessageBox.Show("Oops!路径没找到");
        }
    }
}
