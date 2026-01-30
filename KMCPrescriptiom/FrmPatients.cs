using KMCPrescriptiom.DataAccessLayer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;

namespace KMCPrescriptiom
{
    public partial class FrmPatients : Form
    {
        public FrmPatients()
        {
            InitializeComponent();
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (gvPatients.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Excel.Application xlApp = new Excel.Application();
                Excel.Workbook xlWorkBook = xlApp.Workbooks.Add();
                Excel.Worksheet xlWorkSheet = (Excel.Worksheet)xlWorkBook.Sheets[1];

                // Add column headers
                for (int i = 0; i < gvPatients.Columns.Count; i++)
                {
                    xlWorkSheet.Cells[1, i + 1] = gvPatients.Columns[i].HeaderText;
                }

                // Add row data
                for (int i = 0; i < gvPatients.Rows.Count; i++)
                {
                    for (int j = 0; j < gvPatients.Columns.Count; j++)
                    {
                        xlWorkSheet.Cells[i + 2, j + 1] = gvPatients.Rows[i].Cells[j].Value?.ToString() ?? "";
                    }
                }

                xlApp.Visible = true; // Optional: show Excel app
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error exporting to Excel: " + ex.Message);
            }
        }

        private void FrmPatients_Load(object sender, EventArgs e)
        {
            LoadPatients();
        }

        private void LoadPatients()
        {
            string sql = @"
        SELECT 
            [PatientId],
            [MRNo],
            [FullName],
            [Age],
            [Gender],
            [ContactNo],
            [CreatedDate],
            [Visit]
        FROM [Patients]
        WHERE 1=1"; // base condition to simplify appending

            // Apply date filter
            if (dtFrom.Value != null)
            {
                sql += $" AND CreatedDate >= '{dtFrom.Value:yyyy-MM-dd}'";
            }
            if (dtTo.Value != null)
            {
                sql += $" AND CreatedDate <= '{dtTo.Value:yyyy-MM-dd}'";
            }

            // Apply name filter
            if (!string.IsNullOrWhiteSpace(txtPatientName.Text))
            {
                sql += $" AND FullName LIKE '%{txtPatientName.Text.Replace("'", "''")}%'";
                // Replace single quote to prevent SQL errors
            }

            // Get data
            DataTable dt = DAL.GetData(sql);
            gvPatients.DataSource = dt;

            // Optional: Adjust column headers
            if (gvPatients.Columns.Contains("PatientId")) gvPatients.Columns["PatientId"].HeaderText = "ID";
            if (gvPatients.Columns.Contains("MRNo")) gvPatients.Columns["MRNo"].HeaderText = "MR No";
            if (gvPatients.Columns.Contains("FullName")) gvPatients.Columns["FullName"].HeaderText = "Full Name";
            if (gvPatients.Columns.Contains("ContactNo")) gvPatients.Columns["ContactNo"].HeaderText = "Contact";
        }


        private void dtFrom_ValueChanged(object sender, EventArgs e)
        {
            LoadPatients();
        }

        private void dtTo_ValueChanged(object sender, EventArgs e)
        {
            LoadPatients();
        }

        private void txtPatientName_TextChanged(object sender, EventArgs e)
        {
            LoadPatients();
        }

    }
}
