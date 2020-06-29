using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DwgCompared
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
        }

        //点击源文件  选择文件
        private void btnSource_Click(object sender, EventArgs e)
        {
            var path = IOHelper.OpenFile();
            this.textBox1.Text = path;
        }
        //点击目标文件 选择文件
        private void btnTarget_Click(object sender, EventArgs e)
        {
            var path = IOHelper.OpenFile();
            this.textBox2.Text = path;
        }
      
        //点击确定按钮，渲染各类图元
        private void btnConfirm_Click(object sender, EventArgs e)
        {
            Command.samedColor = GetColor(this.comboBox1.Text);
            Command.createdColor = GetColor(this.comboBox4.Text);
            Command.alteredColor = GetColor(this.comboBox3.Text);
            Command.deletedColor = GetColor(this.comboBox2.Text);
            Command.CompareTwoDwgs(this.textBox1.Text,this.textBox2.Text, true);
        }
        //点击取消按钮，关闭窗口
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        /// <summary>
        /// 查看详情按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void details_Click(object sender, EventArgs e)
        {
            Command.CompareTwoDwgs(this.textBox1.Text, this.textBox2.Text,false);         
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        private Autodesk.AutoCAD.Colors.Color GetColor(string color)
        {
            switch (color)
            {
                case "白色":return Autodesk.AutoCAD.Colors.Color.FromRgb(255,255,255);
                case "红色":return Autodesk.AutoCAD.Colors.Color.FromRgb(255,0,0);
                case "蓝色":return Autodesk.AutoCAD.Colors.Color.FromRgb(0,0,255);
                case "绿色":return Autodesk.AutoCAD.Colors.Color.FromRgb(0,255,0);                
            }
            return Autodesk.AutoCAD.Colors.Color.FromDictionaryName("white");
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Command.CloseAllDwgs();
        }
    }
}
