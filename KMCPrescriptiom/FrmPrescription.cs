using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using KMCPrescriptiom.DAL;

namespace KMCPrescriptiom
{
    public partial class FrmPrescription : Form
    {
        private readonly string _connectionString =
@"Server=.\SQLEXPRESS;Database=KMC;User Id=sa;Password=abcd@1234;";

        public long PatientID = 0;
        public FrmPrescription()
        {
            InitializeComponent();
            LoadDiagnosisDropdown();
        }
        private void LoadDiagnosisDropdown()
        {
            DataTable dt = KMCPrescriptiom.DAL.DAL.GetData(
                "SELECT [PatientId] ID, [FullName] Name FROM [Patients] ORDER BY ID");
            DataRow dr = dt.NewRow();
            dr["ID"] = 0;
            dr["Name"] = "-- Select Patient --";
            dt.Rows.InsertAt(dr, 0);
            cmbExistingPatients.DataSource = dt;
            cmbExistingPatients.DisplayMember = "Name";
            cmbExistingPatients.ValueMember = "ID";
            cmbExistingPatients.SelectedIndex = 0;


        }

        private DataTable GetLabReportsTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("LabTestId", typeof(int));
            dt.Columns.Add("ResultValue", typeof(string));

            foreach (DataGridViewRow row in gvLabReportTests.Rows)
            {
                if (row.IsNewRow) continue;

                dt.Rows.Add(
                    Convert.ToInt32(row.Cells["LabTestId"].Value),
                    row.Cells["ResultValue"].Value?.ToString()
                );
            }
            return dt;
        }
        private DataTable GetPrescriptionTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("MedicineId", typeof(int));
            dt.Columns.Add("Dose", typeof(string));
            dt.Columns.Add("Morning", typeof(bool));
            dt.Columns.Add("Noon", typeof(bool));
            dt.Columns.Add("Evening", typeof(bool));
            dt.Columns.Add("Night", typeof(bool));
            dt.Columns.Add("Days", typeof(int));

            foreach (DataGridViewRow row in dgvPrescription.Rows)
            {
                if (row.IsNewRow) continue;

                dt.Rows.Add(
                    Convert.ToInt32(row.Cells["MedicineId"].Value),
                    row.Cells["Dose"].Value?.ToString(),
                    Convert.ToBoolean(row.Cells["Morning"].Value),
                    Convert.ToBoolean(row.Cells["Noon"].Value),
                    Convert.ToBoolean(row.Cells["Evening"].Value),
                    Convert.ToBoolean(row.Cells["Night"].Value),
                    Convert.ToInt32(row.Cells["Days"].Value)
                );
            }
            return dt;
        }
        private DataTable GetDietTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("DietId", typeof(int));
            dt.Columns.Add("CustomAdvice", typeof(string));

            foreach (DataGridViewRow row in dgvDiet.Rows)
            {
                if (row.IsNewRow) continue;

                dt.Rows.Add(
                    row.Cells["DietId"].Value == null ? DBNull.Value : row.Cells["DietId"].Value,
                    row.Cells["CustomAdvice"].Value?.ToString()
                );
            }
            return dt;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("usp_SaveFullPatientVisit", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                // Patient
                cmd.Parameters.AddWithValue("@MRNo", txtMRNo.Text);
                cmd.Parameters.AddWithValue("@FullName", txtFullName.Text);
                cmd.Parameters.AddWithValue("@Age", txtAge.Text);
                cmd.Parameters.AddWithValue("@Gender", cmbGender.Text);
                cmd.Parameters.AddWithValue("@ContactNo", txtContact.Text);

                // Visit
                cmd.Parameters.AddWithValue("@DoctorName", "Salman");

                // History
                cmd.Parameters.AddWithValue("@PresentComplaints", txtPresentingComplaints.Text);
                cmd.Parameters.AddWithValue("@PastMedical", txtPastMedicalHistory.Text);
                cmd.Parameters.AddWithValue("@PastSurgical", txtPastSurgicalHistory.Text);
                cmd.Parameters.AddWithValue("@DrugAllergies", txtDrugsAllergies.Text);
                cmd.Parameters.AddWithValue("@FamilyHistory", "");

                // Examination
                cmd.Parameters.AddWithValue("@BP", txtBP.Text);
                cmd.Parameters.AddWithValue("@Pulse", txtPulse.Text);
                cmd.Parameters.AddWithValue("@Temperature", txtTemp.Text);
                cmd.Parameters.AddWithValue("@Weight", txtWeight.Text);
                cmd.Parameters.AddWithValue("@Height", txtHeight.Text);
                cmd.Parameters.AddWithValue("@SystemicExam", txtRemarksSysExam.Text);
                cmd.Parameters.AddWithValue("@IsNormal", chkNormal.Checked);

                // Diagnosis
                cmd.Parameters.AddWithValue("@ProvisionalDiagnosis", cmdProvisionalDiagnosis.Text);
                cmd.Parameters.AddWithValue("@FinalDiagnosis", txtFinalDiagnosis.Text);

                // TVPs
                SqlParameter labParam = cmd.Parameters.AddWithValue("@LabReports", GetLabReportsTable());
                labParam.SqlDbType = SqlDbType.Structured;

                SqlParameter presParam = cmd.Parameters.AddWithValue("@Prescriptions", GetPrescriptionTable());
                presParam.SqlDbType = SqlDbType.Structured;

                SqlParameter dietParam = cmd.Parameters.AddWithValue("@DietAdvice", GetDietTable());
                dietParam.SqlDbType = SqlDbType.Structured;

                con.Open();
                cmd.ExecuteNonQuery();

                MessageBox.Show("Prescription saved successfully ✔",
                                "Success",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
        }

        private void btnSavePatient_Click(object sender, EventArgs e)
        {

            long PatientID = KMCPrescriptiom.DAL.DAL.SavePatient(
                            txtMRNo.Text.Trim(),
                            txtFullName.Text.Trim(),
                            int.TryParse(txtAge.Text, out int age) ? age : 0,
                            cmbGender.Text,          // ✅ Corrected
                            txtContact.Text.Trim(),
                            dtVisit.Value             // ✅ Corrected
                        );

            if (PatientID > 0)
            {
                MessageBox.Show("User Added successfully ✔",
                              "Success",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("User not added, please try again with correct info. ",
                              "Success",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
        }
    }
}
