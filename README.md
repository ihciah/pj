# ihciah的pj

------

本文档记录ihciah的地图pj相关

该项目使用C#开发

设计功能：

> * 支持加载指定地图文件
> * 基础地图功能：显示地图、缩放、拖动
> * 路线查找

------

## map类

好像所有功能全写在这一个类里面了

### 成员变量

+ ```bool isdataloaded``` : 是否加载地图文件
+ ```Dictionary<int， way> ways``` : 存储道路、建筑物等的id(作key)、名字(zhs、eng)和node id列表
+ ```Dictionary<int, position> dict``` : id和位置(存储经纬度的position类型)对应的字典
+ ```float mapedge[4]``` : 存储地图文件的边界
+ ```float curedge[4]``` : 存储当前显示区域的边界
+ ```float box[2]``` : 绘图区域的大小(边界)
+ ```int zoom``` : 当前缩放级别

### 公有成员函数

####```map()``` : 
+ 创建空的map对象

####```map(Graphics g, string file)``` : 
+ 使用给定绘图区域和文件路径创建新对象

####```bool readfile(string file)``` : 
+ 读入新文件，失败返回 0

####```void draw(float pos[2])``` :
+ 给出绘图区域左上角经纬度，由经纬度和绘图区域大小确定右下角经纬度并绘图
+ 拖动时检测新区域和旧区域是否有公共部分，如果有则公共部分直接拷贝不重绘

####```void move(posa[2], posb[2])``` :
+ posa、posb为鼠标按下和松开时在绘图区域中的坐标
+ 注意和经纬度和换算

####```void zoomchange(int n)``` :
+ 改变缩放级别
+ 注意同步更新其他数据

####```findplace(string name)``` : 
+ 先判断name中/英文，再查找名字包含该串的way
+ 找到返回存储key的 ```list<int>``` 否则list长度为0

