using Microsoft.Win32;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using System.Windows.Forms;


namespace MyImageView
{
    public partial class Form1 : Form
    {
        #region private var

        private string copyPath;    //copied destination path
        private string imagePath;
        private bool start = false;     //if load files succeed, start will be set true
        private bool thumbnail = true;     //缩略图初始值设为是 打开程序后则显示该模式

        private string[] fileList;          //All Files will be displayed
        private int fileCount = 0;  //total of the files
        private int currentIndex = 0;       //index of the current files
        private int thumc = 0;       //index of the current files 缩略图首文件记数

        private int formHeight;      //windows Height
        private int formWidth;      //windows Width
        private int workHeight;    //Screen Height - taskBar Height

        private int imageHeight;      //the displayed image Height
        private int imageWidth;       //the displayed image width
        private Bitmap bitmap;
        private Bitmap[] thumb=new Bitmap[24];//缩略图页面图片数组 共分为4*6=24个区域
        private Bitmap[] twob = new Bitmap[2];//两图比较对接接口，仅需将单体程序中filelist改为twob【1】twob【2】即可

        private Rectangle formRect;      //display region in form
        private Rectangle imageRect;     //display region in image
        private Rectangle [] thumf=new Rectangle[1000];     //display region in form of thumbnail
        private Rectangle[] thumi=new Rectangle[1000];     //display region in image of thumbnail
        private Rectangle Formr1;
        private Rectangle Formr2;
        private Rectangle Formr3;
        private Rectangle Formr4;//form1-4表示伸缩变换中以鼠标位置为中心将显示屏分割为四片
        private Rectangle imager1;
        private Rectangle imager2;
        private Rectangle imager3;
        private Rectangle imager4;//image1-4表示伸缩变换中以鼠标位置为中心将图片分割为四片
        private Rectangle formRect1;
        private Rectangle formRect2;
        private Rectangle imageRect1;
        private Rectangle imageRect2;//shuangtu duibi

        private Point oldMousePoint;
        private int mouseMoveStep;
        private int copyCount = 0;         //how many photos copy to favor directory
        private int x1, x2, y1, y2;//add 代表图片显示在屏幕上时左上角和右下角横纵坐标位置
        private bool twopic = false;//add 两图比较功能初始值为否
        private double ratio = 1;//add 代表适配屏幕后显示图片与实际图片大小的比例
        private double ratio2 = 1;//add 代表适配屏幕后显示图片与实际图片大小的比例
        private bool firstloadimage = false;

        #endregion

        #region Inital&Closed Function
        public Form1()
        {
            InitializeComponent();    
            //when the program start, it will run in full screen
            formHeight = Screen.PrimaryScreen.Bounds.Height;
            formWidth = Screen.PrimaryScreen.Bounds.Width;
            //the workHeight actually not using, maybe will be used in the future
            workHeight = Screen.PrimaryScreen.WorkingArea.Height;
            this.Height = formHeight;      //full screen mode
            this.Width = formWidth;
            toolStrip1.Items[1].Enabled = false;
            toolStrip1.Items[2].Enabled = false;

            toolStrip1.Location = new Point(formWidth - toolStrip1.Width - 5, formHeight - toolStrip1.Height - 5);
            toolStrip2.Location = new Point(formWidth - toolStrip2.Width - 5, formHeight - toolStrip2.Height - 5);
            this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseWheel);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] commandLine = Environment.GetCommandLineArgs();
            if (commandLine.Length > 1)
            {
                string fileName = commandLine[1];
                imagePath = System.IO.Path.GetDirectoryName(fileName);
                if (start = LoadImageFiles())
                {
                    for (var i = 0; i < fileList.Length; i++)
                    {
                        if (fileName == fileList[i])
                        {
                            currentIndex = i;
                            //ChangeStauts();
                            break;
                        }
                    }
                    LoadImage();
                }
            }
            else
            {
                RegistryKey SoftwareKey = Registry.LocalMachine.OpenSubKey("Software");
                RegistryKey MyName = SoftwareKey.OpenSubKey("Cuishuning");
                if (MyName != null)
                {
                    RegistryKey imageView = MyName.OpenSubKey("ImageView");
                    if (imageView != null)
                    {
                        imagePath = (string)imageView.GetValue("ImagePath");
                    }
                    else
                    {
                        imagePath = Environment.CurrentDirectory;
                    }
                }
                else
                {
                    imagePath = Environment.CurrentDirectory;
                }

                if (start = LoadImageFiles())
                {
                    LoadImage();
                }

            }
            RegistryKey SoftwareKey2 = Registry.LocalMachine.OpenSubKey("Software");
            RegistryKey MyName2 = SoftwareKey2.OpenSubKey("Cuishuning");
            if (MyName2 != null)
            {
                RegistryKey imageView2 = MyName2.OpenSubKey("ImageView");
                if (imageView2 != null)
                {
                    copyPath = (string)imageView2.GetValue("CopyPath");
                }
                else
                {
                    copyPath = Environment.CurrentDirectory;
                }
            }
            else
            {
                copyPath = Environment.CurrentDirectory;
            }

            toolStripButtonOpen_Click(sender, e);   //启动时打开文件夹浏览器

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                RegistryKey SoftwareKey = Registry.LocalMachine.OpenSubKey("Software", true);
                RegistryKey myNameKey = SoftwareKey.CreateSubKey("Cuishuning");
                RegistryKey imageViewKey = myNameKey.CreateSubKey("ImageView");
                imageViewKey.SetValue("ImagePath", (object)imagePath);
                imageViewKey.SetValue("CopyPath", (object)copyPath);
            }
            catch
            {
            }
        }

        #endregion
            
        private string GetProperty(PropertyItem[]pt,ref int orientation)
        {
            string property = String.Empty;
            for (int i = 0; i < pt.Length; i++)
            {
                PropertyItem p = pt[i];
                switch (pt[i].Id)
                {
                    case 0x010F:  // 设备制造商
                        property += "#厂商" + System.Text.ASCIIEncoding.ASCII.GetString(pt[i].Value, 0, pt[i].Value.Length - 1).Trim() + "#";
                        break;
                    case 0x0110: // 设备型号  
                        property += "型号" + System.Text.ASCIIEncoding.ASCII.GetString(pt[i].Value, 0, pt[i].Value.Length - 1).Trim() + "#";
                        break;
                    case 0x9003: // 拍照时间
                        property += "时间" + System.Text.ASCIIEncoding.ASCII.GetString(pt[i].Value, 0, pt[i].Value.Length - 1).Trim() + "#";
                        break;
                    case 0x829A: // 曝光时间  
                        property += "快门" + GetValueOfType5(p.Value) + "#";
                        break;
                    case 0x8827: // ISO  
                        property += "ISO" + GetValueOfType3(p.Value) + "#";
                        break;
                    //case 0x010E: // 图像说明info.description
                    //    this.textBox6.Text = GetValueOfType2(p.Value);
                    //    break;
                    case 0x920a: //焦距
                        property += "焦距" + GetValueOfType5A(p.Value) + "#";
                        break;
                    case 0x829D: //光圈
                        property += "光圈" + GetValueOfType5A(p.Value) + "#";
                        break;
                    case 0xA433:
                        property += "镜头" + System.Text.ASCIIEncoding.ASCII.GetString(pt[i].Value, 0, pt[i].Value.Length-1).Trim() + "#";
                        break;
                    case 0xA434:
                        property += System.Text.ASCIIEncoding.ASCII.GetString(pt[i].Value, 0, pt[i].Value.Length - 1).Trim() + "#";
                        break;
                    case 0x112:
                        orientation = Convert.ToUInt16(pt[i].Value[1] << 8 | pt[i].Value[0]);
                        break;
                    default:
                        break;

                }

            }
            return property;
        }

        private string GetValueOfType3(byte[] b) //对type=3 的value值进行读取
        {
            if (b.Length != 2) return "";
            return Convert.ToUInt16(b[1] << 8 | b[0]).ToString();
        }

        private string GetValueOfType5(byte[] b) //对type=5 的value值进行读取
        {
            if (b.Length != 8) return "";
            UInt32 fm, fz;
            fm = 0;
            fz = 0;
            fz = Convert.ToUInt32(b[7] << 24 | b[6] << 16 | b[5] << 8 | b[4]);
            fm = Convert.ToUInt32(b[3] << 24 | b[2] << 16 | b[1] << 8 | b[0]);
            fz = fz / fm;
            fm = 1;
            return fm.ToString() + "/" + fz.ToString();
        }

        private string GetValueOfType5A(byte[] b)//获取光圈的值
        {
            if (b.Length != 8) return "";
            UInt32 fm, fz;
            fm = 0;
            fz = 0;
            fz = Convert.ToUInt32(b[7] << 24 | b[6] << 16 | b[5] << 8 | b[4]);
            fm = Convert.ToUInt32(b[3] << 24 | b[2] << 16 | b[1] << 8 | b[0]);
            double temp = (double)fm / fz;
            return (temp).ToString();

        }
        /// <summary>
        /// loaded a new image from fileList
        /// Note: not spy any change from file system
        /// </summary>
        private void LoadImage()//分别设置thumbnail为是（内存为24的数组作为载入对象）或否（全屏显示效果）时载入图片情况
        {
            if (start == true && thumbnail == false)
            {
                
                ChangeStauts();
                if (bitmap != null)
                {
                    bitmap.Dispose();
                }
               // currentIndex = 0;
                bitmap = new Bitmap(fileList[currentIndex]);
                
                PropertyItem[] pt = bitmap.PropertyItems;
                //IEnumerable<PropertyItem> propertyItems = pt.Where(ep => ep.Id == 0x112);
                int orientation = 0;
                //if(propertyItems.Count()>0)
                //{
                //    PropertyItem orient = propertyItems.First();
                //    orientation = Convert.ToUInt16(orient.Value[1] << 8 | orient.Value[0]);
                //}
                label3.Text = GetProperty(pt, ref orientation);
                switch (orientation)
                {
                    case 2:
                        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);//horizontal flip
                        break;
                    case 3:
                        bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);//right-top
                        break;
                    case 4:
                        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);//vertical flip
                        break;
                    case 5:
                        bitmap.RotateFlip(RotateFlipType.Rotate90FlipX);
                        break;
                    case 6:
                        bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);//right-top
                        break;
                    case 7:
                        bitmap.RotateFlip(RotateFlipType.Rotate270FlipX);
                        break;
                    case 8:
                        bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);//left-bottom
                        break;
                    default:
                        break;
                }
                imageWidth = bitmap.Width;
                imageHeight = bitmap.Height;

                //image information display
                label1.Text = "W:" + imageWidth.ToString() + " H:" + imageHeight.ToString()
                                     + " Pixel:" + ((imageHeight * imageWidth) / 10000).ToString();
                label2.Text = GetFileNameWithoutPath();
                if (imageWidth <= this.Width && imageHeight <= this.Height)
                    ratio = 1;
                else
                    ratio = Math.Min((double)this.Height / imageHeight, (double)this.Width / imageWidth);
                ShowFull();
            }
            if (start == true && thumbnail == true)
            {
                for (int i = 0; i < 24 && thumc + i < fileCount; i++)
                {
                    if (thumb[i] != null)
                    {
                        thumb[i].Dispose();
                    }
                    thumb[i] = new Bitmap(fileList[thumc + i]);
                }
            }
            
           
            
        }

        private string GetFileNameWithoutPath()
        {
            Regex re = new Regex(@"\\[^\\]*$");
            Match ma = re.Match(fileList[currentIndex]);
            if (ma.Success)
                return ma.Value.Substring(1);
            else
                return "NewFile.jpg";
        }
                
        /// <summary>
        /// base on image size, show a full image on screen
        /// </summary>
        private void ShowFull()//此函数中，为便利ratio的取用和设置，将该语句调至loadimage中，确保每次完整准确调用
        {
            //show full image
            imageRect = new Rectangle(0, 0, imageWidth, imageHeight);
            if (imageWidth <= this.Width && imageHeight <= this.Height)
            {
                ratio = 1;
                int x = (this.Width - imageWidth) / 2;
                int y = (this.Height - imageHeight) / 2;
                formRect = new Rectangle(x, y, imageWidth, imageHeight);
                x1 = x;
                y1 = y;
                x2 = x + imageWidth;
                y2 = y + imageHeight;
            }
            else
            {
                 //此处赋值被移位
                int x = (int)(this.Width - imageWidth * ratio) / 2;
                int y = (int)(this.Height - imageHeight * ratio) / 2;
                formRect = new Rectangle(x, y, (int)(imageWidth * ratio), (int)(imageHeight * ratio));
                //imageWidth = (int)(imageWidth * ratio);
                //imageHeight = (int) (imageHeight * ratio);
                x1 = x;
                y1 = y;
                x2 = x + (int)(imageWidth * ratio);
                y2 = y + (int)(imageHeight * ratio);
            }
            if (start == true && twopic == true&&twob[1]!=null)
            {
                if (twob[0].Width <= this.Width / 2 && twob[0].Height <= this.Height)
                {
                    int x = (this.Width / 2 - twob[0].Width) / 2;
                    int y = (this.Height - twob[0].Height) / 2;
                    formRect1 = new Rectangle(x, y, twob[0].Width, twob[0].Height);
                }
                else
                {
                    double ratio2 = Math.Min((double)this.Height / twob[0].Height, (double)(this.Width / 2) / twob[0].Width);
                    int x = (int)(this.Width / 2 - (twob[0].Width / 2) * ratio2) / 2;
                    int y = (int)(this.Height - twob[0].Height * ratio2) / 2;
                    formRect1 = new Rectangle(x, y, (int)((twob[0].Width / 2) * ratio2), (int)(twob[0].Height * ratio2));
                }

                if (twob[1].Width <= this.Width / 2 && twob[1].Height <= this.Height)
                {
                    int x = (this.Width / 2 - twob[1].Width) / 2 + this.Width / 2; ;
                    int y = (this.Height - twob[1].Height) / 2;
                    formRect2 = new Rectangle(x, y, twob[1].Width, twob[1].Height);
                }
                else
                {
                    double ratio2 = Math.Min((double)this.Height / twob[1].Height, (double)(this.Width / 2) / twob[1].Width);
                    int x = (int)(this.Width / 2 - (twob[1].Width / 2) * ratio2) / 2 + this.Width / 2;
                    int y = (int)(this.Height - twob[1].Height * ratio2) / 2;
                    formRect2 = new Rectangle(x, y, (int)((twob[1].Width / 2) * ratio2), (int)(twob[1].Height * ratio2));
                }
            }

            Invalidate();
        }
        /// <summary>
        /// Load all jpg file form a directory
        /// </summary>
        /// <returns>true if succeed, or false</returns>
        private bool LoadImageFiles()
        {
            try
            {
                //load all jpg files include subdirectory
                fileList = System.IO.Directory.GetFiles(imagePath, "*.jpg", System.IO.SearchOption.AllDirectories);
                fileCount = fileList.GetUpperBound(0);
            }
            catch
            {
                //load jpg files only top directory
                try
                {
                    fileList = System.IO.Directory.GetFiles(imagePath, "*.jpg", System.IO.SearchOption.TopDirectoryOnly);
                    fileCount = fileList.GetUpperBound(0);
                }
                catch
                {
                    return false;
                }

            }
            if(fileCount<0)
            {
                return false;
            }
            currentIndex = 0;
            //ChangeStauts();
            return true;
        }

        //public Bitmap GetImageThumb(Bitmap mg, Size newSize)//网上缩小内存程序 未成功调用
        //{
        //    double ratio = 0d;
        //    double myThumbWidth = 0d;
        //    double myThumbHeight = 0d;
        //    int x = 0;
        //    int y = 0;
        //    Bitmap bp;
        //    if ((mg.Width / Convert.ToDouble(newSize.Width)) > (mg.Height /
        //    Convert.ToDouble(newSize.Height)))
        //        ratio = Convert.ToDouble(mg.Width) / Convert.ToDouble(newSize.Width);
        //    else
        //        ratio = Convert.ToDouble(mg.Height) / Convert.ToDouble(newSize.Height);
        //    myThumbHeight = Math.Ceiling(mg.Height / ratio);
        //    myThumbWidth = Math.Ceiling(mg.Width / ratio);
        //    Size thumbSize = new Size((int)newSize.Width, (int)newSize.Height);
        //    bp = new Bitmap(newSize.Width, newSize.Height);
        //    x = (newSize.Width - thumbSize.Width) / 2;
        //    y = (newSize.Height - thumbSize.Height);
        //    System.Drawing.Graphics g = Graphics.FromImage(bp);
        //    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        //    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        //    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        //    Rectangle rect = new Rectangle(x, y, thumbSize.Width, thumbSize.Height);
        //    g.DrawImage(mg, rect, 0, 0, mg.Width, mg.Height, GraphicsUnit.Pixel);
        //    return bp;
        //}//
        private void Form1_Paint(object sender, PaintEventArgs e)//分为缩略图模式和全屏查看模式
        {
            if (start==true&&thumbnail==false)//全屏查看
            {
                
                toolStrip1.Visible = true;//全屏工具条显示
                toolStrip2.Visible = false;//缩略图工具条隐藏
                e.Graphics.DrawImage(bitmap, formRect, imageRect, System.Drawing.GraphicsUnit.Pixel);
                label1.Visible = true;
                label2.Visible = true;
                label3.Visible = true;//右上角/左下角信息显示
                
               
            }
            if(start==true&&thumbnail==true)
            {
                label1.Visible = false;
                label2.Visible = false;
                label3.Visible = false;
                toolStrip1.Visible=false;
                toolStrip2.Visible = true;
                //Image.GetThumbnailImageAbort myCallback = new Image.GetThumbnailImageAbort(ThumbnailCallback);
                //Image examp = thumb[0].GetThumbnailImage(4000, 4000, myCallback, IntPtr.Zero);
                for (int j = 0; j < 4 ; j++)//依次绘制数组中的24张图片 性能差
                {
                    for (int i = 0; i < 6 && thumc + i + j * 6 < fileCount; i++)
                    {
                        thumf[i + j * 6] = new Rectangle(i * this.Width / 6+10, j * this.Height / 4+20, this.Width / 6-20, this.Height / 4-40);
                        thumi[i + j * 6] = new Rectangle(0, 0, thumb[i + j * 6].Width, thumb[i + j * 6].Height);
                    }
                }
                for (int i = 0; i < 24 && thumb[i] != null && thumc + i <= fileCount; i++)//依次绘制数组中的24张图片 性能差
                {
                    //Size s1 = new System.Drawing.Size(thumi[i].Width, thumi[i].Height);
                   // thumb[i] = GetImageThumb(thumb[i], s1);
                    e.Graphics.DrawImage(thumb[i], thumf[i], thumi[i], System.Drawing.GraphicsUnit.Pixel);
                }
                
            }
            if (start==true&&twopic==true&& twob[1]!=null)
            {
                e.Graphics.DrawImage(twob[0], formRect1, imageRect1, System.Drawing.GraphicsUnit.Pixel);
                e.Graphics.DrawImage(twob[1], formRect2, imageRect2, System.Drawing.GraphicsUnit.Pixel);
            }

        }
        //public bool ThumbnailCallback()
        //{
         //   return false;
        //}
       

        private void Form1_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (start==false&&thumbnail==false)//全屏模式
            {
                currentIndex = (e.Delta > 0 ? currentIndex - 1 : currentIndex + 1);
                currentIndex = (currentIndex < 0 ? 0 : (currentIndex > fileCount ? fileCount : currentIndex));//滚轮移位 1为单位
                //ChangeStauts();
                LoadImage();
            }
            if (start==true&&thumbnail==true)//缩略图模式
            {
                thumc = (e.Delta > 0 ? (thumc >= 6?thumc-6:0) : (thumc<(fileCount-6)?thumc+6:thumc));
                currentIndex = (currentIndex < 0 ? 0 : (currentIndex > fileCount ? fileCount : currentIndex));//滚轮移位 6为单位
                //ChangeStauts();
                LoadImage();
            }
            Invalidate();
        }//分两种模式实现滚轮的不同功能

        private void Form1_MouseDown(object sender, MouseEventArgs e)//分三种情况实现鼠标操作
        {
            if (start == true && thumbnail == false && twopic == false)//全屏显示
            {
                if (e.Button == MouseButtons.Left && e.Button != MouseButtons.Right)//左击复位
                {
                    
                    this.Capture = true;
                    oldMousePoint = e.Location;
                    mouseMoveStep = 0;
                    System.Windows.Forms.Cursor.Hide();
                    //NewRegion(e.Location, 1);      //zoom to original
                    ShowFull();
                    
                }
                if (e.Button == MouseButtons.Right )//右击放大
                {

                    this.Capture = true;
                    oldMousePoint = e.Location;
                    mouseMoveStep = 0;
                    System.Windows.Forms.Cursor.Hide();
                    NewRegion(e.Location, 2);     //zoom to 2X
                    
                   
                }

            }

            //copy to favor directory
            if (e.Button == MouseButtons.Middle)//中击喜欢
            {
                string fileName = GetFileNameWithoutPath();
                if (!System.IO.File.Exists(copyPath + "\\" + fileName))
                {
                    System.IO.File.Copy(fileList[currentIndex], copyPath + "\\" + fileName);
                    label2.Text = (++copyCount).ToString() + " files copied";
                }
            }

            if (start == true && thumbnail == true && twopic == false)//缩略图初始模式
            {
                if (e.Button == MouseButtons.Left)//选择图片并转换至全屏模式
                {
                    for(int j=0;j<4;j++)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            if (MousePosition.X > i * this.Width / 6 + 10 && MousePosition.X < (i + 1) * this.Width / 6 - 10 && MousePosition.Y >( j-1) * this.Height / 4 + 20 && MousePosition.Y > j * this.Height / 4 - 20)
                            {
                                currentIndex = thumc + (j) * 6 + i;
                                thumbnail = false;
                                LoadImage();
                                
                                
                                Invalidate();
                            }
                        }
                    }
 
                }
 
            }
            if (twopic == true)//两图比较
            {
                if (e.Button == MouseButtons.Left)//左击两次后确定twob内容 未调试可能有bug
                {
                    for (int j = 0; j < 4; j++)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            if (MousePosition.X > i * this.Width / 6 + 10 && MousePosition.X < (i + 1) * this.Width / 6 - 10 && MousePosition.Y > (j-1) * this.Height / 4 + 20 && MousePosition.Y < j  * this.Height / 4 - 20)
                            {
                               
                                int k = thumc + j  * 6 + i;
                                twob[t] = new Bitmap(fileList[k]);
                                t++;
                                if (t == 2)
                                {
                                    Invalidate();
                                    twopic = false;
                                    t = 0;
                                }
                            }
                        }
                    }

                }
 
 
            }

        } //already changed
        
        /// <summary>
        /// when zoom or drag, get rectangles of form and image
        /// </summary>
        /// <param name="mousePoint">mouse click point</param>
        /// <param name="zoom">zoom</param>
        
        private void NewRegion(Point mousePoint, int zoom)//zoom倍缩放 以鼠标坐在位置为中心分别将显示屏图片分为四部分 逐个比较
        {
            int imageH1 = (mousePoint.Y-y1) * zoom;
            int imageW1 = (mousePoint.X- x1) * zoom;
            int imageH2 = (mousePoint.Y - y1) * zoom;
            int imageW2 = (x2-mousePoint.X) * zoom;
            int imageH3 = (y2 - mousePoint.Y) * zoom;
            int imageW3 = (mousePoint.X - x1) * zoom;
            int imageH4 = (y2 - mousePoint.Y) * zoom;
            int imageW4 = (x2 - mousePoint.X) * zoom;
            int fh1 = mousePoint.Y;
            int fw1 = mousePoint.X;
            int fh2 = mousePoint.Y;
            int fw2 = this.Width-mousePoint.X;
            int fh3 = this.Height-mousePoint.Y;
            int fw3 = mousePoint.X;
            int fh4 = this.Height - mousePoint.Y;
            int fw4 = this.Width - mousePoint.X;


            if (mousePoint.X > x1 && mousePoint.X < x2 && mousePoint.Y > y1 && mousePoint.Y < y2)//黑色区域停止调用
            {
                //act on form1
                if (imageH1 <= fh1 && imageW1 <= fw1)
                {
                    
                    Formr1 = new Rectangle(mousePoint.X-imageW1, mousePoint.Y-imageH1, imageW1, imageH1);
                    imager1 = new Rectangle(0, 0, imageW1 / zoom, imageH1 / zoom);
                }
                else
                {
                    //Height can be fit screen
                    if (imageH1 <= fh1)
                    {
                        int xxx1 = (imageW1 - mousePoint.X) / zoom;
                        int yyy = fh1 - imageH1;
                        Formr1 = new Rectangle(0, yyy, fw1, fh1 - yyy);
                        imager1 = new Rectangle(xxx1, 0, imageW1/zoom-xxx1, imageH1/zoom);
 
                    }
                    else
                    {
                        if (imageW1 <= fw1)
                        {
                            int xxx = fw1 - imageW1;
                            int yyy1 = (imageH1 -  mousePoint.Y)/zoom ;
                            
                            Formr1 = new Rectangle(xxx, 0, fw1-xxx, fh1);
                            imager1 = new Rectangle(0, yyy1, imageW1/zoom, imageH1/zoom-yyy1);
                        }
                        else
                        {
                            int xxx1 = (imageW1 - mousePoint.X) / zoom;
                            int yyy1 = (imageH1 - mousePoint.Y) / zoom;
                            Formr1 = new Rectangle(0, 0, fw1, fh1);
                            imager1 = new Rectangle(xxx1, yyy1, imageW1/zoom-xxx1, imageH1/zoom-yyy1);
                        }
                    }
                }
                //act on  form2
                if (imageH2 <= fh2 && imageW2 <= fw2)
                {
                    int xxx = mousePoint.X;
                    int yyy = fh2 - imageH2;
                    Formr2 = new Rectangle(xxx, yyy, imageW2, imageH2);
                    imager2 = new Rectangle(xxx - x1, 0, imageW2/zoom, imageH2/zoom);
                }
                else
                {
                    //Height can be fit screen
                    if (imageH2 <= fh2)
                    {
                        int xxx2 = (imageW2 - mousePoint.X) / zoom;
                        int yyy = fh2 - imageH2;
                        Formr2 = new Rectangle(mousePoint.X, yyy, fw2, imageH2);
                        imager2 = new Rectangle(mousePoint.X-x1, 0, fw2 / zoom, imageH2 / zoom);

                    }
                    else
                    {
                        if (imageW2 <= fw2)
                        {
                            int xxx = fw2 - imageW2;
                            int yyy2 = (imageH2 - mousePoint.Y) / zoom;

                            Formr2 = new Rectangle(mousePoint.X, 0, imageW2, fh2);
                            imager2 = new Rectangle(mousePoint.X-x1, yyy2, imageW2 / zoom, fh2 / zoom);
                        }
                        else
                        {
                            int  xxx2 = (imageW2 - mousePoint.X) / zoom;
                            int yyy2 = (imageH2 - mousePoint.Y) / zoom;
                            Formr2 = new Rectangle(mousePoint.X, 0, fw2, fh2);
                            imager2 = new Rectangle(mousePoint.X - x1, yyy2, fw2 / zoom, fh2 / zoom);
                        }
                    }
                }
                //act on  form3
                if (imageH3 <= fh3 && imageW3 <= fw3)
                {
                    
                    Formr3 = new Rectangle(mousePoint.X-imageW3, mousePoint.Y, imageW3, imageH3);
                    imager3 = new Rectangle(0, mousePoint.Y - y1, imageW3 / zoom, imageH3 / zoom);
                }
                else
                {
                    //Height can be fit screen
                    if (imageH3 <= fh3)
                    {
                        Formr3 = new Rectangle(0, mousePoint.Y, fw3, imageH3);
                        imager3 = new Rectangle((imageW3-fw3)/zoom, mousePoint.Y - y1, fw3/zoom, imageH3 / zoom);

                    }
                    else
                    {
                        if (imageW3 <= fw3)
                        {
                            Formr3 = new Rectangle(mousePoint.X - imageW3, mousePoint.Y, imageW3, fh3);
                            imager3 = new Rectangle(0, mousePoint.Y-y1, imageW3 / zoom, fh3 / zoom );
                        }
                        else
                        {
                            Formr3 = new Rectangle(0, mousePoint.Y, fw3, fh3);
                            imager3 = new Rectangle((imageW3 - fw3) / zoom, mousePoint.Y - y1, fw3 / zoom, fh3 / zoom);
                        }
                    }
                }
                //act on  form4
                if (imageH4 <= fh4 && imageW4 <= fw4)
                {
                   
                    Formr4 = new Rectangle(mousePoint.X, mousePoint.Y, imageW4, imageH4);
                    imager4 = new Rectangle(mousePoint.X - x1, mousePoint.Y - y1, imageW4/zoom, imageH4/zoom);
                }
                else
                {
                    //Height can be fit screen
                    if (imageH4 <= fh4)
                    {

                        Formr4 = new Rectangle(mousePoint.X, mousePoint.Y, fw4, imageH4);
                        imager4 = new Rectangle(mousePoint.X-x1, mousePoint.Y - y1, fw4/zoom, imageH4 / zoom);

                    }
                    else
                    {
                        if (imageW4 <= fw4)
                        {
                            Formr4 = new Rectangle(mousePoint.X, mousePoint.Y, imageW4, fh4);
                            imager4 = new Rectangle(mousePoint.X - x1, mousePoint.Y-y1, imageW4 / zoom, fh4/zoom);
                        }
                        else
                        {
                            Formr4 = new Rectangle(mousePoint.X, mousePoint.Y, fw4, fh4);
                            imager4 = new Rectangle(mousePoint.X - x1, mousePoint.Y - y1, fw4 / zoom, fh4 / zoom);
                        }
                    }
                }
                formRect = new Rectangle(Formr1.X, Formr1.Y, Formr1.Width + Formr2.Width, Formr1.Height + Formr3.Height);//组合新建显示器矩阵
                imageRect = new Rectangle((int)(imager1.X / ratio), (int)(imager1.Y / ratio), (int)((imager1.Width + imager2.Width) / ratio), (int)((imager1.Height + imager3.Height) / ratio));
               //组合新建图片矩阵 注意实际图片与显示图片间有比例差因而注入ratio缩放
            }
            
            Invalidate(formRect);
           
        }//already changed
      
        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (start)
            {
                if (e.Button == MouseButtons.Left || e.Button==MouseButtons.Right)
                {
                    this.Capture = false;
                    System.Windows.Forms.Cursor.Show();
                    ShowFull();
                }
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
           
            if (start && this.Capture)
            {
                mouseMoveStep++;
                if (mouseMoveStep <= 2)
                {
                    return;
                }
                else
                {
                    mouseMoveStep = 0;
                    var deltX =( e.Location.X - oldMousePoint.X)*7;
                    var deltY = (e.Location.Y - oldMousePoint.Y)*7;
                    int imageX;
                    int imageY;
                    if (deltX < 0)
                    {
                        imageX = (imageRect.X + deltX < 0 ? 0 : imageRect.X + deltX);
                        //imageX = (imageRect.X + deltX < 0 ? 0 : imageRect.X + deltX);
                    }
                    else
                    {
                        imageX = ((imageRect.X + deltX + imageRect.Width) > imageWidth ? imageWidth - imageRect.Width : imageRect.X + deltX);
                        
                        //imageX = ((imageRect.X + deltX + imageRect.Width) > imageWidth ? imageWidth - imageRect.Width : imageRect.X + deltX);
                    }
                    if (deltY < 0)
                    {
                        imageY = (imageRect.Y + deltY < 0 ? 0 : imageRect.Y + deltY);
                    }
                    else
                    {
                        imageY = ((imageRect.Y + deltY + imageRect.Height) > imageHeight ? imageHeight - imageRect.Height : imageRect.Y + deltY);
                    }
                    imageRect = new Rectangle(imageX, imageY, imageRect.Width, imageRect.Height);
                    oldMousePoint = e.Location;
                    Invalidate(formRect);
                }
           }
        }

       

        private void toolStripButtonOpen_Click(object sender, EventArgs e)//注入thumc值
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.ShowNewFolderButton = true;
            fbd.SelectedPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                imagePath = fbd.SelectedPath;
            }
            start = LoadImageFiles();
            thumc = 0;//载入文件后 thumc初始值为0
            if (start && fileCount >= 0)
            {
                LoadImage();
                //fileSystemWatcher1.Path = imagePath;
            }
            else
            {
                string newImagePath = imagePath;
                imagePath = System.Environment.CurrentDirectory;
                fileList = System.IO.Directory.GetFiles(imagePath, "*.jpg", System.IO.SearchOption.TopDirectoryOnly);
                fileCount = fileList.GetUpperBound(0);
                start = LoadImageFiles();
                if (start)
                    LoadImage();
                toolStrip1.Items[6].Enabled = false;
                MessageBox.Show("当前文件夹已没有图片！");
                fileSystemWatcher1.Path = newImagePath;
            }
            //fileSystemWatcher1.Path = imagePath;
            ChangeStauts();
            Invalidate();
        }

        private void toolStripButtonClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void toolStripButtonRotate_Click(object sender, EventArgs e)
        {
            if (bitmap != null)
            {
                bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                imageWidth = bitmap.Width;
                imageHeight = bitmap.Height;
                ShowFull();
                
                     
             
            }
        }
        /// <summary>
        /// cut the white edge below the toolstrip
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStrip1_Paint(object sender, PaintEventArgs e)
        {
            if ((sender as ToolStrip).RenderMode == ToolStripRenderMode.System)
            {
                Rectangle rect = new Rectangle(0, 0, this.toolStrip1.Width, this.toolStrip1.Height - 2);
                e.Graphics.SetClip(rect);
            }
            

        }

        private void ChangeStauts()
        {
            if (fileCount < 0 || !start || imagePath == System.Environment.CurrentDirectory)
            {
                for (int i = 1; i <= 7; i++)
                    toolStrip1.Items[i].Enabled = false;
                label1.Visible = false;
                label2.Visible = false;
                label3.Visible = false;
            }
            else if (fileCount == 0)
            {
                toolStrip1.Items[1].Enabled = false;
                toolStrip1.Items[2].Enabled = false;
                for (int i = 3; i <= 7; i++)
                    toolStrip1.Items[i].Enabled = true;
                label1.Visible = true;
                label2.Visible = true;
                label3.Visible = true;
            }
            else
            {
                toolStrip1.Items[1].Enabled = true;
                toolStrip1.Items[2].Enabled = true;
                label1.Visible = true;
                label2.Visible = true;
                label3.Visible = true;
                for (int i = 3; i <= 7; i++)
                    toolStrip1.Items[i].Enabled = true;
                if (currentIndex == 0)
                {
                    toolStrip1.Items[1].Enabled = false;
                    toolStrip1.Items[2].Enabled = true;
                    return;
                }
                if (currentIndex == fileCount)
                {
                    toolStrip1.Items[2].Enabled = false;
                    toolStrip1.Items[1].Enabled = true;
                    return;
                }
            }
        }
        private void toolStripButtonBack_Click(object sender, EventArgs e)//设置左button上移一行
        {
            if (start==true&&thumbnail==false)
            {
                currentIndex = currentIndex - 1;
                
                //ChangeStauts();
                //currentIndex = (currentIndex < 0 ? 0 : currentIndex );
                LoadImage();
            }
            if (start == true && thumbnail == true)
            {
                thumc = ((thumc - 6) < 0 ? 0 : (thumc - 6));

                //ChangeStauts();
                //currentIndex = (currentIndex < 0 ? 0 : currentIndex );
                LoadImage();
            }
            Invalidate();
            
        }

        private void toolStripButtonNext_Click(object sender, EventArgs e)//设置右button下移一行（未设置明灭）
        {
            if (start && thumbnail == false)
            {
                currentIndex = currentIndex + 1;
                //currentIndex =(currentIndex > fileCount ? fileCount : currentIndex);
                //ChangeStauts();
                LoadImage();
            }
            if (start && thumbnail == true)
            {
                thumc = ((thumc + 6)<fileCount?(thumc+6):thumc);
                //currentIndex =(currentIndex > fileCount ? fileCount : currentIndex);
                //ChangeStauts();
                LoadImage();
            }
            Invalidate();

        }

        private void toolStripButtonSaveFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath=copyPath;
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                copyPath = fbd.SelectedPath;
            }

        }

        private void toolStripButtonDelete_Click(object sender, EventArgs e)
        {
            if (fileCount > 0)
            {
                int index = currentIndex;
                currentIndex = currentIndex + 1;
                currentIndex = (currentIndex > fileCount ? 0 : currentIndex);
                LoadImage();
                System.IO.File.Delete(fileList[index]);
                while (index < fileCount)
                {
                    fileList[index] = fileList[index + 1];
                    index++;
                }
                fileCount--;
                ChangeStauts();
            }
            else if (fileCount == 0)
            {
                string[] newfileList = new string[fileList.Length];
                for (int i = 0; i < fileList.Length; i++)
                    newfileList[i] = fileList[i];
                int index = currentIndex;
                imagePath = System.Environment.CurrentDirectory;
                fileList = System.IO.Directory.GetFiles(imagePath, "*.jpg", System.IO.SearchOption.TopDirectoryOnly);
                fileCount = fileList.GetUpperBound(0);
                LoadImage();
                System.IO.File.Delete(newfileList[index]);
                ChangeStauts();
                MessageBox.Show("当前文件夹已没有图片！");
                ///try
                ///{
                ///    imagePath = copyPath;
                ///    fileList = System.IO.Directory.GetFiles(imagePath, "*.jpg", System.IO.SearchOption.TopDirectoryOnly);
                ///    fileCount = fileList.GetUpperBound(0);
                ///    LoadImage();
                ///    System.IO.File.Delete(newfileList[index]);
                ///    ChangeStauts();
                ///}
                ///catch
                ///{
                ///    imagePath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                ///    Invalidate();
                ///    //InitializeComponent();                                 
                ///    //System.IO.File.Delete(newfileList[index]);
                ///    //Form1 form = new Form1();                   
                ///    //Application.Run(form);
                ///    //System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
                ///    //Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
                ///    //Graphics blackRectangle = this.CreateGraphics();
                ///    //SolidBrush pen = new SolidBrush(Color.Black);
                ///    //blackRectangle.FillRectangle(pen,rect);
                ///    //Invalidate();
                ///    //System.IO.File.Delete(newfileList[index]);
                ///}
                ///finally
                ///{
                ///    System.IO.File.Delete(newfileList[index]);
                ///    Dispose();
                ///}
            }
        }

        private void toolStripButtonLeft_Click(object sender, EventArgs e)
        {
            if (bitmap != null)
            {
                bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
                imageWidth = bitmap.Width;
                imageHeight = bitmap.Height;
                ShowFull();
            }
        }

        private void toolStripButtonSaveAs_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.AddExtension = true;
            saveFile.CheckFileExists = false;
            saveFile.DefaultExt = "jpg";
            saveFile.Filter = "JPG Files(*.JPG)|*.JPG|All files (*.*)|*.*";
            if (saveFile.ShowDialog() == DialogResult.OK)
            {
                bitmap.Save(saveFile.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyValue)
            {
                //Esc
                case 27:
                    Application.Exit();
                    break;
                //F1
                case 112:
                    toolStripButton8_Click(sender, e);
                    break;
                //F2
                case 113:
                    label1.Visible
                        = label2.Visible
                        = label3.Visible
                        = !label1.Visible;
                    break;
                //Space
                case 32:
                    toolStripButtonBack.Visible
                        = toolStripButtonClose.Visible
                        = toolStripButtonDelete.Visible
                        = toolStripButtonLeft.Visible
                        = toolStripButtonNext.Visible
                        = toolStripButtonOpen.Visible
                        = toolStripButtonRotate.Visible
                        = toolStripButtonSaveAs.Visible
                        = toolStripButtonSaveFolder.Visible
                        = toolStripButton1.Visible
                        = toolStripButton2.Visible
                        = toolStripButton3.Visible
                        = toolStripButton4.Visible
                        = toolStripButton5.Visible
                        = toolStripButton6.Visible
                        = toolStripButton7.Visible
                        = toolStripButton8.Visible
                        = !toolStripButton8.Visible;
                    break;
                //Enter
                case 13:
                    toolStripButtonOpen_Click(sender, e);
                    break;
                //Left
                case 37:
                    toolStripButtonBack_Click(sender, e);
                    break;
                //Next
                case 39:
                    toolStripButtonNext_Click(sender, e);
                    break;
                //Up
                case 38:
                    toolStripButtonLeft_Click(sender, e);
                    break;
                //Down
                case 40:
                    toolStripButtonRotate_Click(sender, e);
                    break;
                //Delete
                case 46:
                    toolStripButtonDelete_Click(sender, e);
                    break;
            }
            
        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void toolStrip2_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = imagePath;
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                imagePath = fbd.SelectedPath;
            }

            start = LoadImageFiles();
            thumc = 0;//载入文件后 thumc初始值为0
            //thumbnail = LoadImageFiles(); 
            if (start)
            {
                LoadImage();
            }
            Invalidate();
        }//添加toolstrip2工具条  程序实现在toolstrip1中 仅实现tool1button click即可

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            toolStripButtonBack_Click(null, null);
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            toolStripButtonNext_Click(null, null);
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            toolStripButtonDelete_Click(null, null);
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            toolStripButtonClose_Click(null, null);
        }
        private int t ;
        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            t = 0;
            twopic = true;
            Invalidate();
        }

        private void toolStripButtonthumbnail_Click(object sender, EventArgs e)
        {
            thumbnail = true;
            LoadImage();
            Invalidate();

        }
        private void fileSystemWatcher1_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            imagePath = fileSystemWatcher1.Path;
            start = LoadImageFiles();            
            fileList = System.IO.Directory.GetFiles(fileSystemWatcher1.Path, "*.jpg", System.IO.SearchOption.TopDirectoryOnly);
            fileCount = fileList.GetUpperBound(0);
            if (start && fileCount >= 0)
            {
                LoadImage();
            }
            else
            {
                imagePath = System.Environment.CurrentDirectory;
                fileList = System.IO.Directory.GetFiles(imagePath, "*.jpg", System.IO.SearchOption.TopDirectoryOnly);
                fileCount = fileList.GetUpperBound(0);
                LoadImage();
                toolStrip1.Items[6].Enabled = false;
                MessageBox.Show("当前文件夹已没有图片！");
            }
            fileSystemWatcher1.Path = imagePath;
            ChangeStauts();


        }

        private void fileSystemWatcher1_Created(object sender, System.IO.FileSystemEventArgs e)
        {
            imagePath = fileSystemWatcher1.Path;
            start = LoadImageFiles();          
            fileList = System.IO.Directory.GetFiles(fileSystemWatcher1 .Path , "*.jpg", System.IO.SearchOption.TopDirectoryOnly);
            fileCount = fileList.GetUpperBound(0);
            if (start && fileCount >= 0)
            {
                LoadImage();
            }
            else
            {
                imagePath = System.Environment.CurrentDirectory;
                fileList = System.IO.Directory.GetFiles(imagePath, "*.jpg", System.IO.SearchOption.TopDirectoryOnly);
                fileCount = fileList.GetUpperBound(0);
                LoadImage();
                toolStrip1.Items[6].Enabled = false;
                MessageBox.Show("当前文件夹已没有图片！");
            }
            fileSystemWatcher1.Path = imagePath;
            ChangeStauts();
        }

        private void fileSystemWatcher1_Deleted(object sender, System.IO.FileSystemEventArgs e)
        {
            start = LoadImageFiles();
            if (start && fileCount >= 0)
            {
                LoadImage();
            }
            else
            {
                imagePath = System.Environment.CurrentDirectory;
                fileList = System.IO.Directory.GetFiles(imagePath, "*.jpg", System.IO.SearchOption.TopDirectoryOnly);
                fileCount = fileList.GetUpperBound(0);
                LoadImage();
                toolStrip1.Items[6].Enabled = false;
                MessageBox.Show("当前文件夹已没有图片！");
            }
            fileSystemWatcher1.Path = imagePath;
            ChangeStauts();
        }

        //private void InfoChange(int num)
        //{
        //    DSOFile.OleDocumentPropertiesClass file = new OleDocumentPropertiesClass();            
        //    file.Open(fileList[currentIndex] , false, dsoFileOpenOptions.dsoOptionDefault);
        //    switch (num)
        //    {
        //        case 0:
        //            file.SummaryProperties.Comments = "亲友照";
        //            break;
        //        case 1:
        //            file.SummaryProperties.Comments = "风景照";
        //            break;
        //        case 2:
        //            file.SummaryProperties.Comments = "虚拟照";
        //            break;
        //    }
        //    file.Save();
        //} //修改图片备注信息

        //private string InfoRead()
        //{
        //    DSOFile.OleDocumentPropertiesClass file = new OleDocumentPropertiesClass();
        //    file.Open(fileList[currentIndex], false, dsoFileOpenOptions.dsoOptionDefault);
        //    return file.SummaryProperties.Comments;
        //}

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (start)
            {

                currentIndex = currentIndex + 1;
                //currentIndex =(currentIndex > fileCount ? fileCount : currentIndex);
                //ChangeStauts();
                if (currentIndex <= fileCount)
                    LoadImage();
                else
                {
                    toolStripButton7.Image = Properties.Resources._11;
                    currentIndex = currentIndex - 1;
                    timer1.Enabled = false;
                }


            }
        }

        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            firstloadimage = !firstloadimage;
            toolStripButton1.Image = firstloadimage ? Properties.Resources._21 : Properties.Resources._11;

            if (timer1.Enabled == true)
            {
                timer1.Enabled = false;
            }
            else
            {
                timer1.Enabled = true;
            }

        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
        
        }
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                this.notifyIcon1.Visible = true;
            }
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            this.Visible = true;
            this.WindowState = FormWindowState.Normal;
            this.notifyIcon1.Visible = false; 
        } //读取图片备注信息
    }
}

