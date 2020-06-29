using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using MenuItem = Autodesk.AutoCAD.Windows.MenuItem;
using Autodesk.AutoCAD.Geometry;

namespace DwgCompared
{
    public class Command:IExtensionApplication
    {
        public  static  string SourcePath { get; set; }
        public  static  string TargetPath { get; set; }

        public static Dictionary<Handle, ComparedResult> dic = new Dictionary<Handle, ComparedResult>();     
        public static Dictionary<Handle, ComparedResult> dicSource = new Dictionary<Handle, ComparedResult>();

        public static Color samedColor { get; set; }
        public static Color alteredColor { get; set; }
        public static Color deletedColor { get; set; }
        public static Color createdColor { get; set; }

        public enum ComparedResult
        {
            samed=0,
            created=1,
            altered=2,
            deleted=4,
        }
        [CommandMethod("TZBD",CommandFlags.Session)]
        public void OpenCompareDwgForm()
        {
            Application.ShowModelessDialog(new Form1());
        }
   
        /// <summary>
        /// 关闭所有文档
        /// </summary>
        public static void CloseAllDwgs()
        {
            DocumentCollection docs = Application.DocumentManager;
            int i = 0;
            foreach (Document doc in docs)//遍历打开的文档
            {
                if (i>0)
                {
                    doc.CloseAndDiscard();//如果文档没有未保存的修改，则直接关闭
                }
                i++;
            }
        }
        [CommandMethod("testVector2d")]

        public void testVector()
        {
            var vec = new Vector2d(0, -25);
            var angle = vec.Angle;
            MessageBox.Show(angle.ToString());
        }
        [CommandMethod("AddDefaultContextMenu")]
        public void AddCustomMenu()
        {
            //定义一个ContextMenuExtension对象，用于表示快捷菜单
            var ctxMenu = new ContextMenuExtension();
            ctxMenu.Title = "自定义菜单";
            var mi = new MenuItem("图纸比对");
            mi.Click += new EventHandler(mi_Click);
            ctxMenu.MenuItems.Add(mi);
            Application.AddDefaultContextMenuExtension(ctxMenu);
        }
        /// <summary>
        /// 比较两个dwg文件
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="targetPath"></param>
        public static void CompareTwoDwgs(string sourcePath, string targetPath,bool isNeedDrawColor= false)
        {
            SourcePath = sourcePath;
            TargetPath = targetPath;
            //新建一个数据库对象以读取Dwg文件
            using (Database db = new Database(false, true))
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                using (Database db1 = new Database(false, true))
                using (Transaction trans1 = db1.TransactionManager.StartTransaction())
                {
                    var sourceList = IOHelper.ReadDwg(db, trans, sourcePath);
                    if (sourceList.Count==0) return;
                    var targetList = IOHelper.ReadDwg(db1, trans1, targetPath);
                    if (targetList.Count == 0) return;
                    //找到新增图元的handle
                    var deleted = sourceList.Where(s=> targetList.All(t => t.Handle != s.Handle)).ToList();
                    //找到新增图元的handle
                    var created = targetList.Where(t => sourceList.All(s=> t.Handle != s.Handle)).ToList();
                    var altered = new List<Entity>();
                    //判断剩下的图元是否相同（目标文件去掉新增的部分，源文件去掉目标文件删除的部分）
                    var sourceLeft = sourceList.Except(deleted).ToList();
                    var targetLeft = targetList.Except(created).ToList();
                    //对目标文件剩下的部分进行分组
                    var groups = targetLeft.GroupBy(t => t.GetType().Name);
                    foreach (var group in groups)
                    {
                        //找到实体类别
                        var sameGroup = sourceLeft.Where(s => s.GetType().Name == group.Key).ToList();
                        foreach (var entity in group)
                        {
                            var sameEnt = sameGroup.FirstOrDefault(sg => sg.Handle == entity.Handle);
                            if (!IsSame(entity, sameEnt))
                            {
                                altered.Add(entity);
                            }
                        }
                    }
                    var samed= targetLeft.Where(s => altered.All(t => t.Handle != s.Handle)).ToList();
                    if (!isNeedDrawColor)
                    {
                       var txtMessage = "目标文件图元总个数：" + targetList.Count + "\n新增：" + created.Count + "\n删除：" + deleted.Count + "\n修改 : " + altered.Count + "\n相同 : " + samed.Count;
                        MessageBox.Show(txtMessage);
                    }
                    else
                    {
                        dic.Clear();
                        foreach (var ent in samed)
                        {
                            dic.Add(ent.Handle, ComparedResult.samed);
                        }
                        foreach (var ent in created)
                        {
                            dic.Add(ent.Handle, ComparedResult.created);
                        }
                        foreach (var ent in altered)
                        {
                            dic.Add(ent.Handle, ComparedResult.altered);
                        }
                        dicSource.Clear();
                        foreach (var ent in deleted)
                        {
                            dicSource.Add(ent.Handle, ComparedResult.deleted);
                        }
                        ChangeEntityColor();
                    }
                   
                    trans1.Commit();
                }
                trans.Commit();
            }           
        }

        /// <summary>
        /// 修改实体坐标颜色
        /// </summary>
        public static void ChangeEntityColor()
        {
            try
            {
                Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
                //获取文档管理器对象以打开Dwg文件
                DocumentCollection docs = Application.DocumentManager;

                var fileNameSource = SourcePath;
                //打开所选择的Dwg文件
                Document docSource = docs.Open(fileNameSource, false);
                using (DocumentLock acLckDoc = docSource.LockDocument()) // acNewDoc.LockDocument())
                {
                    using (var trans = docSource.Database.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)trans.GetObject(docSource.Database.BlockTableId, OpenMode.ForRead);
                        //打开数据库的模型空间块表记录对象
                        BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                        //循环遍历模型空间中的实体
                        foreach (ObjectId id in btr)
                        {
                            var ent = trans.GetObject(id, OpenMode.ForRead) as Entity;
                            if (dicSource.ContainsKey(ent.Handle))
                            {
                                var ent1 = trans.GetObject(id, OpenMode.ForWrite) as Entity;
                                ent1.Color = GetColor(dicSource[ent.Handle]);
                                ent1.DowngradeOpen();
                            }
                        }
                        trans.Commit();
                    }
                }
                string filename = TargetPath;
                //打开所选择的Dwg文件
                Document doc = docs.Open(filename, false);
                //设置当前的活动文档为新打开的Dwg文件
                Application.DocumentManager.MdiActiveDocument = doc;
                //锁定新文档
                using (DocumentLock acLckDoc = doc.LockDocument()) // acNewDoc.LockDocument())
                {
                    using (var trans = doc.Database.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)trans.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                        //打开数据库的模型空间块表记录对象
                        BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                        //循环遍历模型空间中的实体
                        foreach (ObjectId id in btr)
                        {
                            var ent = trans.GetObject(id, OpenMode.ForRead) as Entity;
                            if (dic.ContainsKey(ent.Handle))
                            {
                                var ent1 = trans.GetObject(id, OpenMode.ForWrite) as Entity;
                                ent1.Color = GetColor(dic[ent.Handle]);
                                ent1.DowngradeOpen();
                            }
                        }
                        trans.Commit();
                    }
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                MessageBox.Show(@"文档被占用，请关闭文档后重新操作！");
            }
        }
        private static Color GetColor(ComparedResult comparedResult)
        {
            switch (comparedResult)
            {
                case ComparedResult.samed:
                    return samedColor;                   
                case ComparedResult.created:
                    return createdColor;
                case ComparedResult.altered:
                    return alteredColor;
                case ComparedResult.deleted:
                    return deletedColor;               
            }
            return samedColor;
        }
        private  static  bool IsSame(Entity ent1,Entity ent2)
        {
            if (IsSameLayer(ent1,ent2)&& IsSameLineType(ent1, ent2) && IsSameLineTypeScale(ent1, ent2) && IsSameColor(ent1, ent2)
                 && IsSameLineWidth(ent1, ent2) && IsSameGeometricExtents(ent1, ent2)&& IsSameThickNess(ent1,ent2))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// 是否相同图层
        /// </summary>
        /// <param name="ent1"></param>
        /// <param name="ent2"></param>
        /// <returns></returns>
        private static bool IsSameLayer(Entity ent1, Entity ent2)
        {
            try
            {
                if (ent1.Layer == ent2.Layer)
                {
                    return true;
                }
                return false;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                return true;
            }
         
        }
        /// <summary>
        /// 是否相同线型
        /// </summary>
        /// <param name="ent1"></param>
        /// <param name="ent2"></param>
        /// <returns></returns>
        private static bool IsSameLineType(Entity ent1, Entity ent2)
        {
            try
            {
                if (ent1.Linetype == ent2.Linetype)
                {
                    return true;
                }
                return false;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                return true;
            }
          
        }
        /// <summary>
        /// 是否相同线型比例
        /// </summary>
        /// <param name="ent1"></param>
        /// <param name="ent2"></param>
        /// <returns></returns>
        private static bool IsSameLineTypeScale(Entity ent1, Entity ent2)
        {
            try
            {
                if (ent1.LinetypeScale.Equals(ent2.LinetypeScale))
                {
                    return true;
                }
                return false;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                return true;
            }
          
        }
        /// <summary>
        /// 是否相同颜色
        /// </summary>
        /// <returns></returns>
        private static bool IsSameColor(Entity ent1, Entity ent2)
        {
            try
            {
                if (ent1.Color.Equals(ent2.Color))
                {
                    return true;
                }
                return false;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                return true;
            }
           
        }
        /// <summary>
        /// 是否相同线宽
        /// </summary>
        /// <returns></returns>
        private static bool IsSameLineWidth(Entity ent1, Entity ent2)
        {
            try
            {
                if (ent1.LineWeight.Equals(ent2.LineWeight))
                {
                    return true;
                }
                return false;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                return true;
            }
          
        }
        /// <summary>
        /// 是否相同几何体数据
        /// </summary>
        /// <returns></returns>
        private static bool IsSameGeometricExtents(Entity ent1, Entity ent2)
        {
            try
            {
                if (ent1.GeometricExtents.Equals(ent2.GeometricExtents))
                {
                    return true;
                }
                return false;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                return true;
            }
            
        }
        /// <summary>
        /// 是否相同厚度
        /// </summary>
        /// <returns></returns>
        private static bool IsSameThickNess(Entity ent1, Entity ent2)
        {
            try
            {
                if (!ent1.ContainProperty("Thickness"))
                {
                    return true;
                }
                if (IOHelper.GetPropertyValue(ent1, "Thickness").Equals(IOHelper.GetPropertyValue(ent2, "Thickness")))
                { 
                    return true;
                }
                return false;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                return true;
            }

        }
      

        void mi_Click(object sender, EventArgs e)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var mi = sender as MenuItem;
            if (mi.Text == "图纸比对")
            {
                doc.SendStringToExecute("TZBD\n", true, false, true);
            }
        }
        public void RemoveMenu()
        {

        }
        public void Initialize()
        {
            AddCustomMenu();
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("自定义菜单—图纸比对 已生成。");
        }

        public void Terminate()
        {

        }
    }
}
