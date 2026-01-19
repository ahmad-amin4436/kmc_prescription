using System;
using System.Data;
using System.Windows.Forms;
using System.Data.SqlClient;
using KMCPrescriptiom.DataAccessLayer;
using System.Linq;

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
             chkNormal.Checked);

            if (PhysicalExamID > 0)
            {
                MessageBox.Show("Patient Examination Added successfully ✔",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                ConfigurePatientCombo();
                LoadPatientsCache();
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
