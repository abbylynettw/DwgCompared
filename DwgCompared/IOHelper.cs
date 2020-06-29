using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Internal.Reactors;
using System.Windows.Forms;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices.Filters;

namespace DwgCompared
{
    public static class IOHelper
    {
       
        public static string OpenFile()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;//该值确定是否可以选择多个文件
            dialog.Title = "请选择文件";
            dialog.Filter = "所有文件(*.dwg)|*.dwg";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.FileName;
            }
            return "";
        }
        /// <summary>
        /// 利用反射来判断对象是否包含某个属性
        /// </summary>
        /// <param name="instance">object</param>
        /// <param name="propertyName">需要判断的属性</param>
        /// <returns>是否包含</returns>
        public static bool ContainProperty(this object instance, string propertyName)
        {
            if (instance != null && !string.IsNullOrEmpty(propertyName))
            {
                PropertyInfo _findedPropertyInfo = instance.GetType().GetProperty(propertyName);
                return (_findedPropertyInfo != null);
            }
            return false;
        }

        /// <summary>
        /// 获取一个类指定的属性值
        /// </summary>
        /// <param name="info">object对象</param>
        /// <param name="field">属性名称</param>
        /// <returns></returns>
        public static object GetPropertyValue(object info, string field)
        {
            if (info == null) return null;
            Type t = info.GetType();
            IEnumerable<System.Reflection.PropertyInfo> property = from pi in t.GetProperties() where pi.Name.ToLower() == field.ToLower() select pi;
            return property.First().GetValue(info, null);
        }

        public static List<Entity> ReadDwg(Database db, Transaction trans, string filename)
        {
            var dbList = new List<Entity>();
            try
            {
               
                //如果文件存在
                if (File.Exists(filename))
                {
                    //把文件读入到数据库中
                    db.ReadDwgFile(filename, FileOpenMode.OpenForReadAndWriteNoShare, false, null);
                    db.CloseInput(true);
                    //PartialOpenDatabase(db);//执行局部加载
                    //获取数据库的块表对象
                    BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                    //打开数据库的模型空间块表记录对象
                    BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                    //循环遍历模型空间中的实体
                    foreach (ObjectId id in btr)
                    {
                        var ent = trans.GetObject(id, OpenMode.ForRead) as Entity;
                        dbList.Add(ent);
                    }
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                MessageBox.Show(@"文档被占用，请关闭文档后重新操作！");
            }
            return dbList;
        }

        /// <summary>
        /// 局部加载处理
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static bool PartialOpenDatabase(Database db)
        {
            if (db == null) return false; //数据库未赋值，返回
            //指定局部加载的范围
            Point2d pt1 = Point2d.Origin;
            Point2d pt2 = new Point2d(100, 100);
            Point2dCollection pts = new Point2dCollection(2) { pt1, pt2 };
            //创建局部加载范围过滤器
            SpatialFilterDefinition filterDef = new SpatialFilterDefinition(pts, Vector3d.ZAxis, 0.0, 0.0, 0.0, true);
            SpatialFilter sFilter = new SpatialFilter();
            sFilter.Definition = filterDef;
            //创建图层过滤器，只加载Circle和Line层
            LayerFilter layerFilter = new LayerFilter();
            layerFilter.Add("Circle");
            layerFilter.Add("Line");
            //对图形数据库应用局部加载
            db.ApplyPartialOpenFilters(sFilter, layerFilter);
            if (db.IsPartiallyOpened) //判断图形数据库是否已局部加载
            {
                db.CloseInput(true); //关闭文件输入
                return true; //局部加载成功
            }
            return false; //局部加载失败
        }
    }
}
