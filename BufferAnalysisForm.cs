﻿using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.Geoprocessor;

namespace ArcEngine
{
    public delegate void AddLayer(string s);
    
    public partial class BufferAnalysisForm : Form
    {
        
        
        private IHookHelper _hookHelper;

        private AddLayer _addLayer;
        
        private const uint WmVscroll = 0x0115;
        
        private const uint SbBottom = 7;
        
        [DllImport("user32.dll")]
        private static extern int PostMessage(
            IntPtr wnd,
            uint msg,
            IntPtr wParam,
            IntPtr lParam
            );
        
        public BufferAnalysisForm(IHookHelper hookHelper, AddLayer addLayer)
        {
            InitializeComponent();

            _hookHelper = hookHelper;

            _addLayer = addLayer;
        }
        
        private void BufferAnalysisForm_Load(object sender, EventArgs e)
        {
            if (null == _hookHelper || null == _hookHelper.Hook || 0 == _hookHelper.FocusMap.LayerCount)
                return;

            //load all the feature layers in the map to the layers combo
            IEnumLayer layers = GetLayers();
            layers.Reset();
            ILayer layer;
            while ((layer = layers.Next()) != null)
            {
                inputComBox.Items.Add(layer.Name);
            }
            
            //select the first layer
            if (inputComBox.Items.Count > 0)
                inputComBox.SelectedIndex = 0;

//            string tempDir = System.IO.Path.GetTempPath();
//            Show(tempDir);

            string tempDir = @"C:\Users\ynats\Documents\Visual Studio 2012\Projects\ArcEngine\data\";
            outputPath.Text = System.IO.Path.Combine(tempDir, ((string)inputComBox.SelectedItem + "_buffer.shp"));

            //set the default units of the buffer

            esriUnits mapUnit= _hookHelper.FocusMap.MapUnits;

            switch (mapUnit)
            {
                case esriUnits.esriMeters:
                    unitsComBox.SelectedIndex = 0;
                    break;
                
                case esriUnits.esriKilometers:
                    unitsComBox.SelectedIndex = 1;
                    break;
                
                case esriUnits.esriMiles:
                    unitsComBox.SelectedIndex = 2;
                    break;
                
                default:
                    Show(String.Format("未知单位 : {0} ",mapUnit));
                    break;
                    
            }

            
        }
        
        // 执行缓冲区

        private void 确定_Click(object sender, EventArgs e)
        {
            if (linearUnitButton.Checked)
            {
                LinearUnitBuffer();
            }

            
            else if(propertyButton.Checked)
            {
                PropertyBuffer();
            }

           
        }

        private void 取消_Click(object sender, EventArgs e)
        {
            
            Close();
        }
        
        
        // 线性单位缓冲区

        private void LinearUnitBuffer()
        {
            //修改当前指针样式
            Cursor = Cursors.WaitCursor;

            //make sure that all parameters are okay
            double bufferDistance;
            double.TryParse(bufferDistanceBox.Text, out bufferDistance);
            
            if (bufferDistance < 0)
            {
                MessageBox.Show("无效的缓冲距离！");
                return;
            }

            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(outputPath.Text)) ||
              ".shp" != System.IO.Path.GetExtension(outputPath.Text))
            {
                MessageBox.Show("无效的文件名！");
                return;
            }

            if (_hookHelper.FocusMap.LayerCount == 0)
                return;

            //get the layer from the map
            IFeatureLayer layer = GetFeatureLayer((string)inputComBox.SelectedItem);
            
            if (null == layer)
            {
                txtMessages.Text += "图层 " + (string)inputComBox.SelectedItem + "未被找到！\r\n";
                return;
            }

            //scroll the textbox to the bottom
            ScrollToBottom();
            //add message to the messages box
            txtMessages.Text += "进行缓冲区的图层: " + layer.Name + "\r\n";

            txtMessages.Text += "\r\n正在获取空间数据。这可能需要几秒钟时间...\r\n";
            txtMessages.Update();
            
            //get an instance of the geoprocessor
            Geoprocessor gp = new Geoprocessor {OverwriteOutput = true};
            txtMessages.Text += "正在进行缓冲区分析...\r\n";
            txtMessages.Update();
            
            // 确定选择单位
            
            String unit = unitsComBox.Text;

            switch (unit)
            {
                    case "米":
                        unit = "Meters";
                        break;
                    case "千米":
                        unit = "Kilometers";
                        break;
                    case "英里":
                        unit = "Miles";
                        break;
                    
                        
            }
            
            
            //create a new instance of a buffer tool
            ESRI.ArcGIS.AnalysisTools.Buffer buffer = new ESRI.ArcGIS.AnalysisTools.Buffer(
                layer, outputPath.Text, Convert.ToString(bufferDistance, CultureInfo.InvariantCulture) + " " + unit
                                        );

            //execute the buffer tool (very easy :-))
            IGeoProcessorResult results = (IGeoProcessorResult)gp.Execute(buffer, null);
            
            
            if (results.Status != esriJobStatus.esriJobSucceeded)
            {
                txtMessages.Text += "缓冲区失败的图层: " + layer.Name + "\r\n";
            }
            
            txtMessages.Text += ReturnMessages(gp);
            //scroll the textbox to the bottom
            ScrollToBottom();

            txtMessages.Text += "\r\n完成！\r\n";
            txtMessages.Text += "------------------------------------------------------\r\n";
            //scroll the textbox to the bottom
            ScrollToBottom();
            
            //修改当前指针样式
            this.Cursor = Cursors.Default;
            
            DialogResult dialogResult = MessageBox.Show("是否添加图层？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

            if (dialogResult == DialogResult.OK)
            {
                _addLayer(outputPath.Text);
            }
            
        }
        
        // 字段缓存区

        private void PropertyBuffer()
        {
            // https://www.cnblogs.com/mrmaisg/p/9032601.html
            
            Show("slmasfnsa");
            IBufferConstructionProperties bufferProperties = new BufferConstructionClass();
            IBufferConstruction bufferConstruction = new BufferConstructionClass();
            
        }
        
        
        private IFeatureLayer GetFeatureLayer(string layerName)
        {
            //get the layers from the maps
            
            IEnumLayer layers = GetLayers();
            layers.Reset();

            ILayer layer;
            while ((layer = layers.Next()) != null)
            {
                if (layer.Name == layerName)
                    return layer as IFeatureLayer;
            }

            return null;
        }
        
        private IEnumLayer GetLayers()
        {
            UID uid = new UIDClass();
            
            uid.Value = "{40A9E885-5533-11d0-98BE-00805F7CED21}";
            IEnumLayer layers = _hookHelper.FocusMap.get_Layers(uid, true);

            return layers;
        }
     
        private string ReturnMessages(Geoprocessor gp)
        {
            StringBuilder sb = new StringBuilder();
            
            if (gp.MessageCount > 0)
            {
                for (int Count = 0; Count <= gp.MessageCount - 1; Count++)
                {
                    System.Diagnostics.Trace.WriteLine(gp.GetMessage(Count));
                    sb.AppendFormat("{0}\n", gp.GetMessage(Count));
                }
            }
            return sb.ToString();
        }
        
        private void ScrollToBottom()
        {
            PostMessage((IntPtr)txtMessages.Handle, WmVscroll, (IntPtr)SbBottom, (IntPtr)IntPtr.Zero);
        }
        
        private void Show(string s)
        {
            MessageBox.Show(s);
        }

        private void btnOutputLayer_Click(object sender, EventArgs e)
        {
            //set the output layer
            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.CheckPathExists = true;
            saveDlg.Filter = "Shapefile (*.shp)|*.shp";
            saveDlg.OverwritePrompt = true;
            saveDlg.Title = "Output Layer";
            saveDlg.RestoreDirectory = true;
            saveDlg.FileName = (string)inputComBox.SelectedItem + "_buffer.shp";

            DialogResult dr = saveDlg.ShowDialog();
            if (dr == DialogResult.OK)
                outputPath.Text = saveDlg.FileName;
        }
        
        
    }
}
