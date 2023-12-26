using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZXing;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace BCMS
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private string connString = "server=192.168.9.241;database=myhis;user=his;pwd=yfyy999;";
        HashSet<string> printNumbers = new HashSet<string>();
        private void SearchData ()
        {
            printNumbers.Clear();
            try
            {
                using(SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open ();
                    string querySql = @"  SELECT d.xm '姓名',   
                                        b.mc '执行科室',   
                                        b2.mc '开单科定',   
                                        c.username '开单医生',   
                                        a.je '金额',   
                                        case a.zfbz when 0 then '' else 'X' end as '作废',   
                                        case a.sfbz when 0 then '' else '√' end as '收费',   
                                        case a.ztbz when 0 then '' else '执行' end as '状态',   
                                        a.sqrq '申请日期',   
		                            a.djh '单据号',
                                    '1' '打印张数',
		                            a.yjxmmc '检验项目'
                                FROM yj_sqd a,H_KES b ,H_KES b2,smmuser c,
                                        h_huanz d 
                                WHERE 1=1";
                    if (!string.IsNullOrWhiteSpace(uiTextKey.Text.Trim()))
                    {
                        querySql = querySql + "   and ( a.zy_id='" + uiTextKey.Text.Trim() + "' or a.hz_id='" + uiTextKey.Text.Trim() + "' or d.xm like '%" + uiTextKey.Text.Trim() + "%')";
                    }
                    querySql = querySql + @"      and a.lx='1'
                                        --and a.ztbz='0'
			                            and a.zxks = b.id 
			                            and a.ks_id = b2.id
			                            and a.ys_id = c.userid
			                            and ( a.hz_id = d.id )
                                        and a.sqrq >= '" + uiDatetime.Text + " 00:00:00'  and a.sqrq <= '" + uiDatetime.Text + " 23:59:59' order by djh";
                    SqlDataAdapter sda = new SqlDataAdapter(querySql,conn);
                    DataTable dt = new DataTable();
                    sda.Fill(dt);
                    uiDataGridView1.DataSource = dt;

                    //uiDataGridView1.Columns["1"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    //uiDataGridView1.Columns["2"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    //uiDataGridView1.Columns["4"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    //uiDataGridView1.Columns["5"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                    int[] centerAlignColumns = { 1, 2, 5, 6, 7, 10 };
                    int[] rightAlignColumns = { 4 };

                    foreach(int columnIndex in centerAlignColumns)
                    {
                        uiDataGridView1.Columns[columnIndex].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
                    foreach(int columnIndex in rightAlignColumns)
                    {
                        uiDataGridView1.Columns[columnIndex].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    }

                    uiDataGridView1.Columns[0].Frozen = true;

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("数据库错误：" + ex.Message, "警告", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
        }

        private void uibtSubmit_Click(object sender, EventArgs e)
        {
            SearchData();
        }

        private void uiDataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                if (uiDataGridView1.Columns[e.ColumnIndex].Name == "单据号")
                {
                    DataGridViewCell cell = uiDataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
                    uiDataGridView1.CurrentCell = cell;
                    if (printNumbers.Contains(uiDataGridView1.Rows[e.RowIndex].Cells["单据号"].Value.ToString()))
                    {
                        printNumbers.Remove(uiDataGridView1.Rows[e.RowIndex].Cells["单据号"].Value.ToString());
                        cell.Style.BackColor = Color.White;
                    }
                    else
                    {
                        printNumbers.Add(uiDataGridView1.Rows[e.RowIndex].Cells["单据号"].Value.ToString());
                        cell.Style.BackColor = Color.LightGreen;
                    }
                }
            }
        }

        private void uiSwitch1_ValueChanged(object sender, bool value)
        {
            //if (uiSwitch1.Active)
            //{
            //    uiLabel1.Text = "开";
            //}
            //else
            //{
            //    uiLabel1.Text = "关";
            //}
        }

        private void uibtPrint_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                MessageBox.Show("请生成条形码！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                PrintDocument pd = new PrintDocument();
                pd.PrintPage += new PrintPageEventHandler(this.pd_PrintPage);
                //pd.DefaultPageSettings.Margins = new Margins(50, 50, 100, 100);
                // 设置打印机属性
                System.Windows.Forms.PrintDialog printDialog = new System.Windows.Forms.PrintDialog();
                pd.PrinterSettings = printDialog.PrinterSettings;

                // 实现在打印时，每张条形码按该行的打印数量，打印指定数量的张数，需要在打印时遍历每个单据号，获取每个单据号需要打印的数量，然后在打印时重复打印相应的次数。
                // 遍历printNumbers列表，为每个单据号生成对应的条形码图片并进行打印
                foreach (string Pintno in printNumbers)
                {
                    // 获取该单据号需要打印的数量
                    int printCount = Convert.ToInt32(uiDataGridView1.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Cells["单据号"].Value.ToString() == Pintno)?.Cells["打印张数"].Value);

                    // 创建条形码写入器
                    BarcodeWriter barcodeWriter = new BarcodeWriter
                    {
                        Format = BarcodeFormat.CODE_128, // 设置条形码格式为CODE_128
                        Options = new ZXing.Common.EncodingOptions
                        {
                            Width = 120, // 设置条形码宽度
                            Height = 50 // 设置条形码高度
                        }
                    };
                    // 生成条形码位图
                    Bitmap barcodeImage = barcodeWriter.Write(Pintno);

                    // 打印相应数量的条形码图片
                    for (int i = 0; i < printCount; i++)
                    {
                        // 设置当前要打印的条形码图片
                        pd.PrintPage += (s, ev) =>
                        {
                            ev.Graphics.DrawImage(barcodeImage, 2, 2);
                        };
                        pd.Print(); // 打印当前条形码图片
                    }
                }
            }

            #region
            ////// 遍历printNumbers列表中的每个单据号，将每个单据号写入表PRINETS里。
            ////foreach (string billNumber in printNumbers)
            ////{
            ////    // 将选中打印的单据号写入表PRINETS里的操作
            ////    // 假设这里使用了名为"connectionString"的数据库连接字符串
            ////    using (SqlConnection connection = new SqlConnection(connString))
            ////    {
            ////        connection.Open();
            ////        string query = "INSERT INTO PRINETS (BillNumber) VALUES (@BillNumber)";
            ////        using (SqlCommand command = new SqlCommand(query, connection))
            ////        {
            ////            command.Parameters.AddWithValue("@BillNumber", billNumber);
            ////            command.ExecuteNonQuery();
            ////        }
            ////    }
            ////}

            ////// 打印完成后，清空printNumbers列表并刷新DataGridView
            ////printNumbers.Clear();
            //////RefreshDataGridView();
            #endregion
        }
        private void pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            g.DrawImage(pictureBox1.Image, 2, 2);
        }

        private void uiDataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            //自定义 DataGridView 数据集最左侧列的序号
            DataGridView dgv = sender as DataGridView;
            System.Drawing.Rectangle rectangle = new System.Drawing.Rectangle(e.RowBounds.Location.X,
                                                e.RowBounds.Location.Y,
                                                dgv.RowHeadersWidth - 4,
                                                e.RowBounds.Height);

            TextRenderer.DrawText(e.Graphics, (e.RowIndex + 1).ToString(),
                                    dgv.RowHeadersDefaultCellStyle.Font,
                                    rectangle,
                                    dgv.RowHeadersDefaultCellStyle.ForeColor,
                                    TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
            dgv.Font = new System.Drawing.Font("Arial", 10);
        }

        private void uiDataGridView1_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            // 检查是否是右键点击
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                // 设置当前单元格为选中状态
                uiDataGridView1.CurrentCell = uiDataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];

                if (e.RowIndex >= 0)
                {
                    DataGridViewRow row = uiDataGridView1.Rows[e.RowIndex];
                    string Pintno = row.Cells["单据号"].Value.ToString();
                    // 创建条形码写入器
                    BarcodeWriter barcodeWriter = new BarcodeWriter
                    {
                        Format = BarcodeFormat.CODE_128, // 设置条形码格式为CODE_128
                        Options = new ZXing.Common.EncodingOptions
                        {
                            Width = 120, // 设置条形码宽度
                            Height = 50 // 设置条形码高度
                        }
                    };

                    // 生成条形码位图
                    Bitmap barcodeImage = barcodeWriter.Write(Pintno);

                    // 在PictureBox中显示条形码
                    pictureBox1.Image = barcodeImage;
                }

                // 显示上下文菜单
                uiContextMenuStrip1.Show(Cursor.Position);
            }

            // 实现当点击选中dataGridView1中的某一行的打印张数单元格时，使用鼠标滚轮来增加或减少该单元格的数值。
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0) // 确保单击的是单元格而不是列标题
            {
                if (uiDataGridView1.Columns[e.ColumnIndex].Name == "打印张数") // 确保单击的是“打印张数”列
                {
                    DataGridViewCell cell = uiDataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
                    uiDataGridView1.CurrentCell = cell; // 将当前单元格设置为选中状态

                    // 订阅鼠标滚轮事件
                    uiDataGridView1.MouseWheel += (s, ev) =>
                    {
                        if (uiDataGridView1.SelectedCells.Count == 1 && uiDataGridView1.SelectedCells[0] == cell) // 确保只有选中的单元格受到影响
                        {
                            if (ev.Delta > 0) // 鼠标向上滚动，增加数值
                            {
                                if (cell.Value == null || cell.Value.ToString() == "")
                                {
                                    cell.Value = 1;
                                }
                                else
                                {
                                    int newValue = Convert.ToInt32(cell.Value) + 1;
                                    if (newValue > 4) newValue = 4; // 最大值为4
                                    cell.Value = newValue;
                                }
                            }
                            else if (ev.Delta < 0) // 鼠标向下滚动，减少数值
                            {
                                if (cell.Value == null || cell.Value.ToString() == "" || Convert.ToInt32(cell.Value) <= 1)
                                {
                                    cell.Value = 1; // 最小值为1
                                }
                                else
                                {
                                    int newValue = Convert.ToInt32(cell.Value) - 1;
                                    if (newValue < 1) newValue = 1; // 最小值为1
                                    cell.Value = newValue;
                                }
                            }
                        }
                    };
                }
            }
        }

        private void uiDataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = uiDataGridView1.Rows[e.RowIndex];
                string Pintno = row.Cells["单据号"].Value.ToString();
                // 创建条形码写入器
                BarcodeWriter barcodeWriter = new BarcodeWriter
                {
                    Format = BarcodeFormat.CODE_128, // 设置条形码格式为CODE_128
                    Options = new ZXing.Common.EncodingOptions
                    {
                        Width = 120, // 设置条形码宽度
                        Height = 50 // 设置条形码高度
                    }
                };

                // 生成条形码位图
                Bitmap barcodeImage = barcodeWriter.Write(Pintno);

                // 在PictureBox中显示条形码
                pictureBox1.Image = barcodeImage;
            }
        }

        private void 打印条形码ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                MessageBox.Show("请生成条形码！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                PrintDocument pd = new PrintDocument();
                pd.PrintPage += new PrintPageEventHandler(this.pd_PrintPage);
                //pd.DefaultPageSettings.Margins = new Margins(50, 50, 100, 100);

                // 设置打印机属性
                System.Windows.Forms.PrintDialog printDialog = new System.Windows.Forms.PrintDialog();
                pd.PrinterSettings = printDialog.PrinterSettings;

                // 遍历printNumbers列表，为每个单据号生成对应的条形码图片并进行打印
                foreach (string Pintno in printNumbers)
                {
                    // 创建条形码写入器
                    BarcodeWriter barcodeWriter = new BarcodeWriter
                    {
                        Format = BarcodeFormat.CODE_128, // 设置条形码格式为CODE_128
                        Options = new ZXing.Common.EncodingOptions
                        {
                            Width = 120, // 设置条形码宽度
                            Height = 50 // 设置条形码高度
                        }
                    };
                    // 生成条形码位图
                    Bitmap barcodeImage = barcodeWriter.Write(Pintno);

                    // 设置当前要打印的条形码图片
                    pd.PrintPage += (s, ev) =>
                    {
                        ev.Graphics.DrawImage(barcodeImage, 2, 2);
                    };
                    pd.Print(); // 打印当前条形码图片
                }
            }
        }
    }
}
