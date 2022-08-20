using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Drawing.Imaging;

namespace HW3_ADONET_Vasilchenko
{
    public partial class Form1 : Form
    {
        string connectionString = ConfigurationManager.ConnectionStrings["CompanyDB"].ConnectionString;
        SqlConnection conn;
        SqlDataAdapter adapter;
        DataSet ds;
        SqlCommandBuilder command;
        string fileName = "";

        public Form1()
        {
            InitializeComponent();
            conn = new SqlConnection(connectionString);
        }

        private void addPictureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Graphics File |*.jpeg; *.jpg; *.bmp; *.png; *.gif";
            ofd.FileName = "";
            if(ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                fileName = ofd.FileName;
                LoadPicture();
            }
        }

        private void LoadPicture()
        {
            try
            {
                byte[] bytes = CreateCopy();
                conn.Open();
                SqlCommand comm = new SqlCommand("Insert into EmployeePictures(EmployeeID, FilePath, Picture) VALUES (@EmployeeID, @FilePath, @Picture)", conn);
                if (toolStripTextBox1.Text == null || toolStripTextBox1.Text.Length == 0) return;
                int index = -1;
                int.TryParse(toolStripTextBox1.Text, out index);
                if (index == -1) return;
                comm.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = index;
                comm.Parameters.Add("@FilePath", SqlDbType.NVarChar, 255).Value = fileName;
                comm.Parameters.Add("@Picture", SqlDbType.Image, bytes.Length).Value = bytes;
                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (conn != null) conn.Close();
            }
        }

        private byte[] CreateCopy()
        {
            Image img = Image.FromFile(fileName);
            int maxWidth = 300, maxHeight = 300;

            double ratioX = (double)maxWidth / img.Width;
            double ratioY = (double)maxHeight / img.Height;
            double ratio = Math.Min(ratioX, ratioY);
            int newWidth = (int)(img.Width*ratio);
            int newHeight = (int)(img.Height*ratio);
            Image mi = new Bitmap(newWidth, newHeight);

            Graphics g = Graphics.FromImage(mi);
            g.DrawImage(img, 0, 0, newWidth, newHeight);
            MemoryStream ms = new MemoryStream();

            mi.Save(ms, ImageFormat.Jpeg);
            ms.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            BinaryReader br = new BinaryReader(ms); 
            byte[] buf = br.ReadBytes((int)ms.Length);
            return buf;
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string query = "SELECT DISTINCT Employee.EmployeeID, FirstName, LastName, BirthDate, EmployeePictures.Picture AS N'Picture' FROM Employee, EmployeePictures WHERE Employee.EmployeeID = EmployeePictures.EmployeeID";
                adapter = new SqlDataAdapter(query, conn);
                SqlCommandBuilder cmb = new SqlCommandBuilder(adapter);
                ds = new DataSet();
                adapter.Fill(ds, "Picture");
                dataGridView1.DataSource = ds.Tables["Picture"];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void selectOneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if(toolStripTextBox1.Text == null || toolStripTextBox1.Text.Length == 0)
                {
                    MessageBox.Show("Write down the number of Employee you want to look at");
                    return;
                }

                int index = -1;
                int.TryParse(toolStripTextBox1.Text, out index);
                if(index == -1)
                {
                    MessageBox.Show("Invalid Employee ID");
                    return ;
                }

                adapter = new SqlDataAdapter("SELECT EmployeeID, FirstName, LastName, BirthDate, PositionID, EmployeePicture FROM Employee WHERE EmployeeID = @EmployeeID", conn);
                SqlCommandBuilder cmb = new SqlCommandBuilder(adapter);
                adapter.SelectCommand.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = index;
                ds = new DataSet();
                adapter.Fill(ds, "Employee");
                dataGridView1.DataSource = ds.Tables[0];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void toolStripTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar <=48 || e.KeyChar>=59) && e.KeyChar !=8)
                e.Handled = true;
        }

        private void insertToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                ds = new DataSet();
                adapter = new SqlDataAdapter(textBox1.Text, conn);
                dataGridView1.DataSource = null;
                command = new SqlCommandBuilder(adapter);
                adapter.Fill(ds, "Employee");
                dataGridView1.DataSource = ds.Tables["Employee"];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            selectAllToolStripMenuItem_Click(sender, e);
        }

        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                command = new SqlCommandBuilder(adapter);
                ds = new DataSet();
                adapter.Fill(ds, "Employee");
                adapter.Update(ds, "Employee");
                adapter.UpdateCommand = command.GetUpdateCommand();
                adapter.Fill(ds, "Employee");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {            
            try 
            {
                if (toolStripTextBox1.Text == null || toolStripTextBox1.Text.Length == 0)
                {
                    MessageBox.Show("Write down the number of Employee you want to look at");
                    return;
                }

                int index = -1;
                int.TryParse(toolStripTextBox1.Text, out index);
                if (index == -1)
                {
                    MessageBox.Show("Invalid Employee ID");
                    return;
                }

                DialogResult dr;
                dr = MessageBox.Show("Are you sure?\nImpossible undo this command while executed", "Deletion confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (dr == DialogResult.Yes)
                {
                    adapter.DeleteCommand = new SqlCommand("DELETE FROM Employee WHERE EmployeeID = @EmployeeID", conn);
                    adapter.DeleteCommand.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = index;
                    conn.Open();
                    adapter.DeleteCommand.ExecuteNonQuery();
                    conn.Close();
                    ds.Clear();
                    adapter.Fill(ds);
                }
                else
                {
                    MessageBox.Show("Deletion cancelled");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            selectAllToolStripMenuItem_Click(sender, e);
        }
    }
}
