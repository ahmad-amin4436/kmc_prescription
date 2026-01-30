using System;
using System.Data;
using System.Windows.Forms;
using System.Data.SqlClient;
using KMCPrescriptiom.DataAccessLayer;
using System.Linq;
using KMCPrescriptiom.Dataset;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System.Diagnostics;
using System.IO;


namespace KMCPrescriptiom
{
    public partial class FrmPrescription : Form
    {
        public long PatientID = 0;

        private DataTable _patientsCache;
        private bool _suppressEvents;

        public FrmPrescription()
        {
            InitializeComponent();
            ConfigurePatientCombo();
            LoadPatientsCache();
        }
           
        // =======================
        // Configure ComboBox
        // =======================
        private void ConfigurePatientCombo()
        {
            cmbExistingPatients.DropDownStyle = ComboBoxStyle.DropDown;
            cmbExistingPatients.AutoCompleteMode = AutoCompleteMode.None;
            cmbExistingPatients.AutoCompleteSource = AutoCompleteSource.None;
            cmbExistingPatients.Items.Clear();
        }

        // =======================
        // Load Patients ONCE
        // =======================
        private void LoadPatientsCache()
        {
            _patientsCache = DAL.GetData(
                "SELECT PatientId, FullName FROM Patients");
        }

        // =======================
        // Smooth Live Search
        // =======================
        private void cmbExistingPatients_TextUpdate(object sender, EventArgs e)
        {
            if (_suppressEvents) return;

            string text = cmbExistingPatients.Text.Trim();

            if (text.Length < 2)
            {
                cmbExistingPatients.DroppedDown = false;
                return;
            }

            _suppressEvents = true;

            cmbExistingPatients.BeginUpdate();
            cmbExistingPatients.Items.Clear();

            foreach (DataRow row in _patientsCache.Rows)
            {
                string name = row["FullName"].ToString();

                if (name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    cmbExistingPatients.Items.Add(new ComboBoxItem
                    {
                        Text = name,
                        Value = Convert.ToInt64(row["PatientId"])
                    });
                }
            }

            cmbExistingPatients.EndUpdate();

            cmbExistingPatients.DroppedDown = cmbExistingPatients.Items.Count > 0;
            cmbExistingPatients.SelectionStart = text.Length;

            _suppressEvents = false;
        }

        // =======================
        // Load Patient On Select
        // =======================
        private void cmbExistingPatients_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (cmbExistingPatients.SelectedItem is ComboBoxItem item)
            {
                LoadPatient(item.Value);
            }
        }

        // =======================
        // Load Patient Data
        // =======================
        private void LoadPatient(long patientId)
        {
            DataTable dt = DAL.GetData(
                @"SELECT PatientId, MRNo, FullName, Age, Gender, ContactNo, Visit
                  FROM Patients
                  WHERE PatientId = @PatientId",
                new SqlParameter("@PatientId", patientId));

            if (dt.Rows.Count == 0) return;

            DataRow dr = dt.Rows[0];

            PatientID = patientId;
            txtMRNo.Text = dr["MRNo"].ToString();
            txtFullName.Text = dr["FullName"].ToString();
            txtAge.Text = dr["Age"].ToString();
            cmbGender.SelectedIndex = Convert.ToInt32(dr["Gender"]);
            txtContact.Text = dr["ContactNo"].ToString();
            if (dr["Visit"] != DBNull.Value)
            {
                dtVisit.Value = Convert.ToDateTime(dr["Visit"]);
            }
            else
            {
                dtVisit.Value = DateTime.Now; // or leave as-is
            }
            DataTable PatientHistory_dt = DAL.GetData(
                @"SELECT [HistoryId] AS ID
                       ,[PresentComplaints]
                       ,[PastMedical]
                       ,[PastSurgical]
                       ,[DrugAllergies]
                  FROM [PatientHistory]
                  WHERE PatientID = @PatientId",
                new SqlParameter("@PatientId", patientId));
            gvPatientHistory.DataSource = PatientHistory_dt;

            DataTable PatientExam_dt = DAL.GetData(
                @"SELECT [ExamId] AS ID
                        ,[BP]
                      ,[Temperature]
                      ,[Weight]
                      ,[Height]
                      ,[SystemicExam]
                      ,[IsNormal]
                  FROM [PhysicalExamination]
                  WHERE PatientId = @PatientId",
                new SqlParameter("@PatientId", patientId));
            gvPatientExam.DataSource = PatientExam_dt;

            DataTable LabReport_dt = DAL.GetData(
                @"SELECT [ReportId] AS ID
                      ,[TestName]
                      ,[ResultValue]
                      ,[Unit]
                      ,[NormalRange]
                  FROM [PatientLabReports]
                  WHERE [PatientID] = @PatientId",
                new SqlParameter("@PatientId", patientId));
            gvLabReportTests.DataSource = LabReport_dt;

              DataTable Diagnosis_dt = DAL.GetData(
                @"SELECT [PatientDiagnosisId] AS ID
                      ,[DiagnosisType]
                        ,[DiagnosisText]
                  FROM [PatientDiagnosis]
                  WHERE [PatientID] = @PatientId",
                new SqlParameter("@PatientId", patientId));
            gvProvDiagnosis.DataSource = Diagnosis_dt;

             DataTable Prescription_dt = DAL.GetData(
                @"SELECT [PrescriptionId] AS ID
                      ,[Medicine]
                      ,[Days]
                      ,[Dose]
                      ,[Instructions]
                      ,[Morning]
                      ,[Noon]
                      ,[Evening]
                      ,[Night]
                  FROM [Prescriptions]
                  WHERE [PatientID] = @PatientId",
                new SqlParameter("@PatientId", patientId));
            gvPrescription.DataSource = Prescription_dt;

             DataTable Diet_dt = DAL.GetData(
                @"SELECT [PatientDietId] AS ID
                      ,[DietType]
                        ,[CustomAdvice]
                  FROM [PatientDietAdvice]
                  WHERE [PatientID] = @PatientId",
                new SqlParameter("@PatientId", patientId));
            gvDiet.DataSource = Diet_dt;

            AddDeleteButton(gvPatientHistory);
            AddDeleteButton(gvPatientExam);
            AddDeleteButton(gvLabReportTests);
            AddDeleteButton(gvProvDiagnosis);
            AddDeleteButton(gvPrescription);
            AddDeleteButton(gvDiet);


        }

        private void HandleDelete(
    DataGridView gv,
    DataGridViewCellEventArgs e,
    string idColumn,
    string tableName,
    string pkColumn)
        {
            if (e.RowIndex < 0) return;

            if (gv.Columns[e.ColumnIndex].Name != "btnDelete")
                return;

            var id = gv.Rows[e.RowIndex].Cells[idColumn].Value;

            if (MessageBox.Show("Delete this record?",
                "Confirm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            DAL.Execute(
                $"DELETE FROM {tableName} WHERE {pkColumn} = @Id",
                new SqlParameter("@Id", id));

            gv.Rows.RemoveAt(e.RowIndex);
            ConfigurePatientCombo();
            LoadPatientsCache();
            LoadPatient(PatientID);
        }


        private void AddDeleteButton(DataGridView gv)
        {
            if (gv.Columns["btnDelete"] == null)
            {
                DataGridViewButtonColumn btn = new DataGridViewButtonColumn();
                btn.Name = "btnDelete";
                btn.HeaderText = "Delete";
                btn.Text = "Delete";
                btn.UseColumnTextForButtonValue = true;
                btn.Width = 70;
                gv.Columns.Add(btn);
            }
        }

        // =======================
        // Save Patient
        // =======================
        private void btnSavePatient_Click(object sender, EventArgs e)
        {
            long patientId = DAL.SavePatient(
                txtMRNo.Text.Trim(),
                txtFullName.Text.Trim(),
                int.TryParse(txtAge.Text, out int age) ? age : 0,
                cmbGender.SelectedIndex,
                txtContact.Text.Trim(),
                dtVisit.Value
            );

            if (patientId > 0)
            {
                MessageBox.Show("User Added successfully ✔",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                ConfigurePatientCombo();
                LoadPatientsCache();
                LoadPatient(patientId);
            }
            else
            {
                MessageBox.Show("User not added, please try again.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                ConfigurePatientCombo();
                LoadPatientsCache();
            }
        }

        private void btnSaveHistory_Click(object sender, EventArgs e)
        {
            if (PatientID == 0)
            {
                MessageBox.Show("Please Select the Patient First!",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            long HistoryId = DAL.SavePatienHistory(
             PatientID,
             txtPresentingComplaints.Text,
             txtPastMedicalHistory.Text,
             txtPastSurgicalHistory.Text,
             txtDrugsAllergies.Text);

            if (HistoryId > 0)
            {
                MessageBox.Show("Patient History Added successfully ✔",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                ConfigurePatientCombo();
                LoadPatientsCache();
                LoadPatient(PatientID);
            }
            else
            {
                MessageBox.Show("User not added, please try again.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                ConfigurePatientCombo();
                LoadPatientsCache();
            }
        }

        private void btnSaveExamination_Click(object sender, EventArgs e)
        {
            if (PatientID == 0)
            {
                MessageBox.Show("Please Select the Patient First!",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            long PhysicalExamID = DAL.SavePhysicalExamination(
      PatientID,
      txtBP.Text,
      txtPulse.Text,
      txtTemp.Text,
      txtWeight.Text,
      txtHeight.Text,
      txtRemarksSysExam.Text,
      chkNormal.Checked,
      txtHeartBeat.Text,
      txtBreath.Text,
      txtSugar.Text
  );

            if (PhysicalExamID > 0)
            {
                MessageBox.Show("Patient Examination Added successfully ✔",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                ConfigurePatientCombo();
                LoadPatientsCache();
                LoadPatient(PatientID);
            }
            else
            {
                MessageBox.Show("User not added, please try again.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                ConfigurePatientCombo();
                LoadPatientsCache();
            }
        }

        private void btnAddTest_Click(object sender, EventArgs e)
        {
            if (PatientID == 0)
            {
                MessageBox.Show("Please Select the Patient First!",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            long LabTestID = DAL.SavePatientLabReport(
                PatientID,
                txtTestName.Text,
                txtTestResult.Text,
                txtTestUnit.Text,
                txtTestNormalRange.Text
            );

            if (LabTestID > 0)
            {
                MessageBox.Show("Patient Test Added successfully ✔",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                ConfigurePatientCombo();
                LoadPatientsCache();
                LoadPatient(PatientID);
            }
            else
            {
                MessageBox.Show("User not added, please try again.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                ConfigurePatientCombo();
                LoadPatientsCache();
            }
        }

        private void btnSaveDiagnosis_Click(object sender, EventArgs e)
        {
            if (PatientID == 0)
            {
                MessageBox.Show("Please Select the Patient First!",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            long PatientDiagnosisID = DAL.SavePatientDiagnosis(
                PatientID,
                txtProvisionalDiagnosis.Text,
                txtFinalDiagnosis.Text                
            );

            if (PatientDiagnosisID > 0)
            {
                MessageBox.Show("Patient Diagnosis Added successfully ✔",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                ConfigurePatientCombo();
                LoadPatientsCache();
                LoadPatient(PatientID);
            }
            else
            {
                MessageBox.Show("User not added, please try again.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                ConfigurePatientCombo();
                LoadPatientsCache();
            }
        }

        private void btnSavePrescriptions_Click(object sender, EventArgs e)
        {
            if (PatientID == 0)
            {
                MessageBox.Show("Please Select the Patient First!",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            long PatientDiagnosisID = DAL.SavePrescription(
    PatientID,
    txtMedicien.Text,
    txtDays.Text,
    txtDose.Text,
    txtInstructions.Text,
    chkMorning.Checked,
    chkNoon.Checked,
    chkEvening.Checked,
    chkNight.Checked);

            if (PatientDiagnosisID > 0)
            {
                MessageBox.Show("Patient Prescription Added successfully ✔",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                ConfigurePatientCombo();
                LoadPatientsCache();
                LoadPatient(PatientID);
            }
            else
            {
                MessageBox.Show("User not added, please try again.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                ConfigurePatientCombo();
                LoadPatientsCache();
            }
        }

        private void btnSaveDietaryAdvice_Click(object sender, EventArgs e)
        {
            if (PatientID == 0)
            {
                MessageBox.Show("Please Select the Patient First!",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            string dietType = string.Join(", ",
                chkListDietary.CheckedItems.Cast<string>()
            );

            long PatientDietID = DAL.SavePatientDietAdvice(
       PatientID,
       dietType,
       txtCustomDietaryInstructions.Text.Trim()
   );


            if (PatientDietID > 0)
            {
                MessageBox.Show("Patient Prescription Added successfully ✔",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                ConfigurePatientCombo();
                LoadPatientsCache();
                LoadPatient(PatientID);
            }
            else
            {
                MessageBox.Show("User not added, please try again.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                ConfigurePatientCombo();
                LoadPatientsCache();
            }
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            // 1️⃣ Get typed dataset
            Prescription ds = DAL.GetPatientVisitReport(PatientID);

            if (ds == null || ds.Tables["Patient"].Rows.Count == 0)
            {
                MessageBox.Show("No data found for this patient.");
                return;
            }

            // 2️⃣ Load report
            ReportDocument rpt = new ReportDocument();

            string reportPath = Path.Combine(
                Application.StartupPath,
                @"Reports\Prescription.rpt"
            );

            rpt.Load(reportPath);

            // 3️⃣ Assign typed dataset
            rpt.SetDataSource(ds);

            // 4️⃣ A4 page setup
            rpt.PrintOptions.PaperSize = PaperSize.PaperA4;
            rpt.PrintOptions.PaperOrientation = PaperOrientation.Portrait;

            // 5️⃣ Export PDF
            string pdfName = $"Prescription_{PatientID}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            string pdfPath = Path.Combine(
                Application.StartupPath,
                @"Reports\Generated",
                pdfName
            );

            Directory.CreateDirectory(Path.GetDirectoryName(pdfPath));

            rpt.ExportToDisk(
                ExportFormatType.PortableDocFormat,
                pdfPath
            );

            rpt.Close();
            rpt.Dispose();

            // 6️⃣ Open PDF automatically
            Process.Start(new ProcessStartInfo()
            {
                FileName = pdfPath,
                UseShellExecute = true
            });
        }
        private void gvPatientHistory_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            HandleDelete(gvPatientHistory, e, "ID", "PatientHistory", "HistoryId");
        }

        private void gvPatientExam_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            HandleDelete(gvPatientExam, e, "ID", "PhysicalExamination", "ExamId");
        }

        private void gvLabReportTests_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            HandleDelete(gvLabReportTests, e, "ID", "PatientLabReports", "ReportId");
        }

        private void gvProvDiagnosis_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            HandleDelete(gvProvDiagnosis, e, "ID", "PatientDiagnosis", "PatientDiagnosisId");
        }

        private void gvPrescription_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            HandleDelete(gvPrescription, e, "ID", "Prescriptions", "PrescriptionId");
        }

        private void gvDiet_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            HandleDelete(gvDiet, e, "ID", "PatientDietAdvice", "PatientDietId");
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ResetForm(this);  // Reset all controls
            PatientID = 0;
            ConfigurePatientCombo();
            LoadPatientsCache();
            LoadPatient(PatientID);

            // Reset date picker separately if needed
            dtVisit.Value = DateTime.Now;
        }

        /// <summary>
        /// Recursively resets all input controls in a container (Form or Panel)
        /// </summary>
        private void ResetForm(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                switch (ctrl)
                {
                    case TextBox txt:
                        txt.Clear();
                        break;
                    case ComboBox cmb:
                        cmb.SelectedIndex = -1; // reset selection
                        break;
                    case CheckBox chk:
                        chk.Checked = false;
                        break;
                    case RadioButton rb:
                        rb.Checked = false;
                        break;
                    case DateTimePicker dtp:
                        dtp.Value = DateTime.Now;
                        break;
                    case NumericUpDown nud:
                        nud.Value = nud.Minimum;
                        break;
                    case ListBox lb:
                        lb.ClearSelected();
                        break;
                }

                // If the control has child controls, reset them too
                if (ctrl.HasChildren)
                {
                    ResetForm(ctrl);
                }
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Application.Exit(); // Closes all forms and ends the app
        }

        private void btnPatients_Click(object sender, EventArgs e)
        {
            FrmPatients patientForm = new FrmPatients();

            patientForm.Show();

        }

    }

    // =======================
    // ComboBox Item Helper
    // =======================
    class ComboBoxItem
    {
        public string Text { get; set; }
        public long Value { get; set; }

        public override string ToString() => Text;
    }
}
