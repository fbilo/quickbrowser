﻿using System;
using System.Collections.Generic;
using System.ComponentModel;


using System.Diagnostics;
using System.Windows.Forms;

using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using Microsoft.Data.ConnectionUI;
using Microsoft.VisualBasic;


namespace GetDataFromDBIntoFile
{
    public partial class ExploreTablesInDB : Form
    {
        private string strConnection = "";
        private string strTable = "";
        private string strCommand = "";

        private static string strExploreTables = "SELECT s.Name as SchemaName,t.Name as TableName FROM SYS.TABLES t JOIN sys.Schemas s on t.Schema_id = s.Schema_ID WHERE T.TYPE = 'U' AND T.Name LIKE '[A-Z]%'";
        private static string strSelectTop100 = "SELECT TOP 100 * FROM ";
        private static string strNOLOCK = " WITH (NOLOCK)";

        public ExploreTablesInDB()
        {
            InitializeComponent();
        }

        private void ExploreTablesInDB_Load(object sender, EventArgs e)
        {
            // TODO: This line of code loads data into the 'sampleConnectionDataSet.Connections' table. You can move, or remove it, as needed.
            //this.connectionsTableAdapter.Fill(this.sampleConnectionDataSet.Connections);

            this.LoadDataFromLocalDB();
        }

        private void LoadDataFromLocalDB()
        {
            try
            {
                using (SQLiteConnection oSqliteConn = new SQLiteConnection(Properties.Settings.Default.sqliteconn))
                {
                    SQLiteCommand oSqliteComm = new SQLiteCommand("Select name, connection from connections", oSqliteConn);
                    SQLiteDataAdapter oSqliteAdapter = new SQLiteDataAdapter(oSqliteComm);
                    oSqliteAdapter.Fill(this.sampleConnectionDataSet.Connections);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "falied load local Database!");
            }
        }

        private void SetupDataGridView(DataTable oData)
        {
            dataGridView1.Rows.Clear();
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;


            dataGridView1.DataSource = oData;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.Items.Count == 0) return;

            try
            {
                this.strConnection = comboBox1.SelectedValue.ToString().Trim();
                this.strCommand = strExploreTables;
                QueryData qd = new QueryData(strConnection, strCommand);
                DataTable dt = qd.ExecuteDataSet();

                SetupDataGridView(dt);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "failed to Connect to the Data Source!");
            }

        }



        private void button1_Click(object sender, EventArgs e)
        {
            SelectForm sf = new SelectForm();
            sf.Show();
            this.Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            tabControl1.Controls.Clear();

        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            UpdateSource(0);
        }

        private void UpdateSource(int ntype)
        {
            string cCommand = string.Empty;


            DataConnectionDialog odialog = new DataConnectionDialog();
            odialog.DataSources.Add(DataSource.SqlDataSource);
            odialog.SelectedDataProvider = DataProvider.SqlDataProvider;

            if (ntype == 1)
                odialog.ConnectionString = comboBox1.SelectedValue.ToString().Trim();

            if (DataConnectionDialog.Show(odialog, this) == DialogResult.OK)
            {
                string cString = odialog.ConnectionString;
                SqlConnectionStringBuilder oBuilder = new SqlConnectionStringBuilder(cString);

                string cDataSource = oBuilder.DataSource;
                string cDBName = oBuilder.InitialCatalog;
                string cName;

                // get connection string
                switch (ntype)
                {
                    case 0: //add
                        cName = cDataSource.Replace(' ', '_') + '_' + cDBName.Replace(' ', '_');
                        string cResult = Microsoft.VisualBasic.Interaction.InputBox("Enter a name to save the connectionstring", "Save", cName, 0, 0);

                        if (string.Empty != cResult)
                        {
                            cCommand = string.Format("insert into connections (name, connection) values('{0}', '{1}')",
                                cResult,
                                cString);
                        }
                        else return;
                        break;
                    case 1: //update
                        cName = comboBox1.SelectedText;
                        cCommand = string.Format("Update connections set connection = '{1}' where name = '{0}'", cName, cString);
                        break;
                    default:
                        return;
                }

                // Save data
                try
                {
                    using (SQLiteConnection oSqliteConn = new SQLiteConnection(Properties.Settings.Default.sqliteconn))
                    {
                        SQLiteCommand oSqliteComm = new SQLiteCommand(cCommand, oSqliteConn);
                        oSqliteConn.Open();
                        oSqliteComm.ExecuteNonQuery();

                     }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "failed in upgrade to the local database conn.db!");

                }

                // update UI
                try
                {
                    CleanUpUI();
                    this.sampleConnectionDataSet.Clear();
                    LoadDataFromLocalDB();
                }
                catch (Exception)
                {

                    throw;
                }

            }
            else return;

        }

        private void CleanUpUI()
        {
            //Combobox
            comboBox1.DataSource = "";
            comboBox1.Items.Clear();

            //DataGridView
            dataGridView1.DataSource = "";
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
        }

        private void ResetUI()
        {
            comboBox1.DataSource = connectionsBindingSource;
        }

        private void dataGridView1_DoubleClick(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
                return;

            strTable = (dataGridView1.SelectedRows[0]).Cells[1].Value.ToString();
            strCommand = strSelectTop100 + strTable + strNOLOCK;

            TabPage tp = new TabPage();
            //TextBox tb = new TextBox();

            //tp.Text = strTable;
            //tp.Size = new System.Drawing.Size(1000, 400);
            //tp.TabIndex = 1;
            //tp.Controls.Add(tb);

            //tb.Text = strCommand;
            //tb.Size = new System.Drawing.Size(1000, 400);

            myTabs oTab = new myTabs(strTable, strCommand, comboBox1.SelectedValue.ToString().Trim());

            tp.Controls.Add(oTab);
            tp.Text = strTable;

            tabControl1.Controls.Add(tp);
            tabControl1.SelectTab(tabControl1.TabCount - 1);
            //tabControl1.Dock = DockStyle.Fill;
            tp.Size = new System.Drawing.Size(tabControl1.Width - 5, tabControl1.Height - 5);
            oTab.Dock = DockStyle.Fill;

            oTab.ReleaseMe +=  closetab;
            
        }

        public void closetab(myTabs oTab)
        {
            TabPage oPage = (TabPage)oTab.Parent;
            tabControl1.Controls.Remove(oPage);

        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            UpdateSource(1);
        }
    }
}
