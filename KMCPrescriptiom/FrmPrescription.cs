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
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using System.Security;
using System.Drawing;


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
            // try to load application logo into header if available
            try
            {
                string lp = FindAssetPath("amc-logo-removebg-preview.png");
                if (!string.IsNullOrEmpty(lp) && File.Exists(lp))
                {
                    picLogo.Image = Image.FromFile(lp);
                    picLogo.SizeMode = PictureBoxSizeMode.StretchImage;
                }
            }
            catch { }
        }

        // Configure patient ComboBox appearance and behavior
        private void ConfigurePatientCombo()
        {
            cmbExistingPatients.DropDownStyle = ComboBoxStyle.DropDown;
            cmbExistingPatients.AutoCompleteMode = AutoCompleteMode.None;
            cmbExistingPatients.AutoCompleteSource = AutoCompleteSource.None;
            cmbExistingPatients.Items.Clear();
        }

        // Load patients into an in-memory cache for quick search
        private void LoadPatientsCache()
        {
            _patientsCache = DAL.GetData(
                "SELECT PatientId, FullName FROM Patients");
        }

        // Live search helper for the patient combobox
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

        // Simple font resolver that loads fonts from Windows Fonts folder
        class LocalFontResolver : IFontResolver
        {
            public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
            {
                string fam = familyName.ToLower();
                if (fam.Contains("arial"))
                {
                    if (isBold && isItalic) return new FontResolverInfo("arialbi.ttf");
                    if (isBold) return new FontResolverInfo("arialbd.ttf");
                    if (isItalic) return new FontResolverInfo("ariali.ttf");
                    return new FontResolverInfo("arial.ttf");
                }

                return new FontResolverInfo("arial.ttf");
            }

            public byte[] GetFont(string faceName)
            {
                try
                {
                    string fonts = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
                    string path = Path.Combine(fonts, faceName);
                    if (!File.Exists(path))
                    {
                        var files = Directory.GetFiles(fonts, "*.ttf");
                        foreach (var f in files)
                        {
                            if (string.Equals(Path.GetFileName(f), faceName, StringComparison.OrdinalIgnoreCase))
                            {
                                path = f; break;
                            }
                        }
                    }

                    return File.Exists(path) ? File.ReadAllBytes(path) : new byte[0];
                }
                catch
                {
                    return new byte[0];
                }
            }
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

        private void GenerateTicket(string mode)
        {
            if (PatientID == 0)
            {
                MessageBox.Show("Please select a patient first!",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            string gender = cmbGender.SelectedItem != null
                ? cmbGender.SelectedItem.ToString()
                : (cmbGender.SelectedIndex == 1 ? "Female" : cmbGender.SelectedIndex == 2 ? "Other" : "Male");

            // embed logo as base64 to avoid file:// issues; fall back to a tiny transparent PNG if missing
            string logoPath = Path.Combine(Application.StartupPath, "Assets", "amc-logo-removebg-preview.png");
            string logoData;
            if (File.Exists(logoPath))
            {
                try
                {
                    var bytes = File.ReadAllBytes(logoPath);
                    var base64 = Convert.ToBase64String(bytes);
                    var ext = Path.GetExtension(logoPath).TrimStart('.').ToLower();
                    if (ext == "jpg") ext = "jpeg";
                    logoData = $"data:image/{ext};base64,{base64}";
                }
                catch
                {
                    // transparent 1x1 PNG fallback
                    logoData = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR4nGNgYAAAAAMAASsJTYQAAAAASUVORK5CYII=";
                }
            }
            else
            {
                // transparent 1x1 PNG fallback
                logoData = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR4nGNgYAAAAAMAASsJTYQAAAAASUVORK5CYII=";
            }

            string html = $@"<!doctype html>
<html>
<head>
  <meta charset='utf-8' />
  <title>Ticket</title>
  <style>
    body {{ font-family: 'Segoe UI', Tahoma, sans-serif; color:#222; padding:20px; }}
    .card {{ width: 600px; border:1px solid #ddd; padding:16px; border-radius:6px; box-shadow:0 2px 6px rgba(0,0,0,.08); }}
    .logo {{ height:80px; }}
    h1 {{ font-size:20px; margin:8px 0 12px 0; color:#1b4f72; }}
    .meta {{ margin-bottom:12px; }}
    .row {{ display:flex; justify-content:space-between; margin:6px 0; }}
    .label {{ color:#666; font-size:12px; width:120px; }}
    .value {{ font-weight:600; }}
    .footer {{ margin-top:14px; font-size:11px; color:#666; }}
    .mode {{ float:right; font-weight:bold; color:#1b4f72; }}
  </style>
</head>
<body>
  <div class='card'>
    <div style='display:flex; align-items:center; gap:12px;'>
      <img class='logo' src='{logoData}' alt='logo' />
      <div style='flex:1'>
        <h1>AMC Patient Ticket <span class='mode'>{mode}</span></h1>
        <div style='color:#999;font-size:12px;'>Visit: {dtVisit.Value:yyyy-MM-dd HH:mm}</div>
      </div>
    </div>
    <hr />
    <div class='meta'>
      <div class='row'><div><span class='label'>MR No</span> <span class='value'>{txtMRNo.Text}</span></div><div><span class='label'>Age</span> <span class='value'>{txtAge.Text}</span></div></div>
      <div class='row'><div><span class='label'>Name</span> <span class='value'>{txtFullName.Text}</span></div><div><span class='label'>Gender</span> <span class='value'>{gender}</span></div></div>
      <div class='row'><div><span class='label'>Contact</span> <span class='value'>{txtContact.Text}</span></div><div></div></div>
    </div>
    
  </div>
</body>
</html>";

            string nameBase = $"Ticket_{mode}_{PatientID}_{DateTime.Now:yyyyMMddHHmmss}";
            string outDir = Path.Combine(Application.StartupPath, "Reports", "Generated");
            Directory.CreateDirectory(outDir);

            string htmlPath = Path.Combine(outDir, nameBase + ".html");
            string pdfPath = Path.Combine(outDir, nameBase + ".pdf");

            // Write HTML (logo is embedded via base64 data URI)
            File.WriteAllText(htmlPath, html);

            // Try to convert using headless Chrome or Edge if available
            string[] candidates = new[] {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google\\Chrome\\Application\\chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google\\Chrome\\Application\\chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft\\Edge\\Application\\msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft\\Edge\\Application\\msedge.exe")
            };

            bool converted = false;

            foreach (var exe in candidates)
            {
                try
                {
                    if (!File.Exists(exe)) continue;

                    var args = $"--headless --disable-gpu --print-to-pdf=\"{pdfPath}\" \"file:///{htmlPath}\" --allow-file-access-from-files";
                    // add no-sandbox on some systems
                    args += " --no-sandbox";

                    var psi = new ProcessStartInfo()
                    {
                        FileName = exe,
                        Arguments = args,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };

                    using (var proc = Process.Start(psi))
                    {
                        proc.WaitForExit(15000); // wait up to 15s
                    }

                    if (File.Exists(pdfPath) && new FileInfo(pdfPath).Length > 0)
                    {
                        converted = true;
                        break;
                    }
                }
                catch
                {
                    // ignore and try next
                }
            }

            if (converted)
            {
                Process.Start(new ProcessStartInfo() { FileName = pdfPath, UseShellExecute = true });
            }
            else
            {
                // fallback: open HTML in default browser
                Process.Start(new ProcessStartInfo() { FileName = htmlPath, UseShellExecute = true });
            }
        }

        private void btnIn_Click(object sender, EventArgs e)
        {
            GenerateTicketPdfWithPdfSharp("IN");
        }

        private void btnOut_Click(object sender, EventArgs e)
        {
            GenerateTicketPdfWithPdfSharp("OUT");
        }

        // Reliable PDF generation using PdfSharp (embeds image directly)
        private void GenerateTicketPdfWithPdfSharp(string mode)
        {
            // ensure font resolver is registered so PdfSharp can find system fonts like Arial
            if (GlobalFontSettings.FontResolver == null)
            {
                GlobalFontSettings.FontResolver = new LocalFontResolver();
            }

            if (PatientID == 0)
            {
                MessageBox.Show("Please select a patient first!",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            string outDir = Path.Combine(Application.StartupPath, "Reports", "Generated");
            Directory.CreateDirectory(outDir);
            string nameBase = $"Ticket_{mode}_{PatientID}_{DateTime.Now:yyyyMMddHHmmss}";
            string pdfPath = Path.Combine(outDir, nameBase + ".pdf");

            try
            {
                using (PdfDocument doc = new PdfDocument())
                {
                    doc.Info.Title = "AMC Patient Ticket";
                    PdfPage page = doc.AddPage();
                    page.Size = PdfSharp.PageSize.A4;
                    XGraphics gfx = XGraphics.FromPdfPage(page);

                    // margins
                    double marginLeft = 20;
                    double y = 40;

                    // draw header (date + title)
                    XFont small = new XFont("Arial", 9);
                    XFont titleFont = new XFont("Arial", 18);
                    gfx.DrawString(DateTime.Now.ToString("M/d/yy, h:mm tt"), small, XBrushes.Black, new XRect(marginLeft, y, page.Width - marginLeft * 2, 20), XStringFormats.TopLeft);
                   // gfx.DrawString("Outpatient Ticket", small, XBrushes.Black, new XRect(marginLeft, y, page.Width - marginLeft * 2, 20), XStringFormats.TopCenter);
                    y += 30;

                    // draw card border (no grey background for a cleaner look)
                    XRect card = new XRect(marginLeft, y, page.Width - marginLeft * 2, 170);
                    gfx.DrawRectangle(new XPen(XColors.LightGray, 0.6), card);

                    // logo - try common places (output Assets or project Assets)
                    string logoFileName = "amc-logo-removebg-preview.png";
                    string logoPath = FindAssetPath(logoFileName);
                    double imgX = card.Left + 12;
                    double imgY = card.Top + 12;
                    double imgH = 60;
                    double imgW = imgH;
                    if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                    {
                        using (XImage img = XImage.FromFile(logoPath))
                        {
                            gfx.DrawImage(img, imgX, imgY, imgW, imgH);
                        }
                    }

                    // title and mode
                    gfx.DrawString("AMC Patient Ticket", titleFont, XBrushes.DarkSlateBlue, new XRect(imgX + imgW + 12, imgY, card.Width - imgW - 40, 36), XStringFormats.TopLeft);
                    gfx.DrawString(mode, titleFont, XBrushes.DarkSlateBlue, new XRect(card.Right - 80, imgY, 70, 36), XStringFormats.TopLeft);

                    // visit
                    XFont medium = new XFont("Arial", 10);
                    gfx.DrawString($"Visit: {dtVisit.Value:yyyy-MM-dd HH:mm}", medium, XBrushes.Gray, new XRect(imgX + imgW + 12, imgY + 36, card.Width - imgW - 40, 20), XStringFormats.TopLeft);

                    double infoY = imgY + 70;
                    XFont label = new XFont("Arial", 10);
                    XFont val = new XFont("Arial", 12);

                    // Two-column layout: left (labels+values) and right (labels+values)
                    double leftLabelX = card.Left + 20;
                    double leftValueX = leftLabelX + 70;
                    double rightLabelX = card.Left + card.Width / 2 + 10;
                    double rightValueX = rightLabelX + 70;
                    double rowHeight = 24;

                    // Row 1: MR No | Age
                    gfx.DrawString("MR No", label, XBrushes.Gray, new XRect(leftLabelX, infoY, 60, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(txtMRNo.Text, val, XBrushes.Black, new XRect(leftValueX, infoY, 160, rowHeight), XStringFormats.TopLeft);

                    gfx.DrawString("Age", label, XBrushes.Gray, new XRect(rightLabelX, infoY, 60, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(txtAge.Text, val, XBrushes.Black, new XRect(rightValueX, infoY, 60, rowHeight), XStringFormats.TopLeft);

                    infoY += rowHeight;
                    // Row separator
                    gfx.DrawLine(new XPen(XColors.LightGray, 0.3), leftLabelX, infoY, card.Right - 16, infoY);

                    // Row 2: Name | Gender
                    gfx.DrawString("Name", label, XBrushes.Gray, new XRect(leftLabelX, infoY + 6, 60, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(txtFullName.Text, val, XBrushes.Black, new XRect(leftValueX, infoY + 6, card.Width / 2 - 90, rowHeight), XStringFormats.TopLeft);

                    gfx.DrawString("Gender", label, XBrushes.Gray, new XRect(rightLabelX, infoY + 6, 60, rowHeight), XStringFormats.TopLeft);
                    string gender = cmbGender.SelectedItem != null ? cmbGender.SelectedItem.ToString() : (cmbGender.SelectedIndex == 1 ? "Female" : cmbGender.SelectedIndex == 2 ? "Other" : "Male");
                    gfx.DrawString(gender, val, XBrushes.Black, new XRect(rightValueX, infoY + 6, 80, rowHeight), XStringFormats.TopLeft);

                    infoY += rowHeight + 4;
                    gfx.DrawLine(new XPen(XColors.LightGray, 0.3), leftLabelX, infoY, card.Right - 16, infoY);

                    // Row 3: Contact (left) | (right empty)
                    infoY += 6;
                    gfx.DrawString("Contact", label, XBrushes.Gray, new XRect(leftLabelX, infoY, 60, rowHeight), XStringFormats.TopLeft);
                    gfx.DrawString(txtContact.Text, val, XBrushes.Black, new XRect(leftValueX, infoY, 220, rowHeight), XStringFormats.TopLeft);

                    // generated timestamp removed (not displayed on ticket)

                    // --- Render joined data: Prescriptions, Exam, Diagnosis ---
                    double sectionY = card.Bottom + 12;
                    XFont sectionTitle = new XFont("Arial", 11);
                    XFont smallVal = new XFont("Arial", 10);

                    // helper: ensure there's enough space on the current page, create new page if needed
                    Func<double, bool> EnsureSpace = (needed) =>
                    {
                        double bottomMargin = 40;
                        if (sectionY + needed > page.Height - bottomMargin)
                        {
                            page = doc.AddPage();
                            page.Size = PdfSharp.PageSize.A4;
                            gfx = XGraphics.FromPdfPage(page);
                            // small continuation header
                            gfx.DrawString("AMC Patient Ticket (cont.)", titleFont, XBrushes.DarkSlateBlue, new XRect(marginLeft, 40, page.Width - marginLeft * 2, 24), XStringFormats.TopLeft);
                            sectionY = 40 + 30;
                            return true;
                        }
                        return false;
                    };

                    // simple word-wrap that splits text into lines around approxChars
                    Func<string, int, System.Collections.Generic.List<string>> WrapText = (text, approxChars) =>
                    {
                        var result = new System.Collections.Generic.List<string>();
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            result.Add("");
                            return result;
                        }
                        var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var line = new System.Text.StringBuilder();
                        foreach (var w in words)
                        {
                            if (line.Length + w.Length + 1 <= approxChars)
                            {
                                if (line.Length > 0) line.Append(' ');
                                line.Append(w);
                            }
                            else
                            {
                                result.Add(line.ToString());
                                line.Clear();
                                line.Append(w);
                            }
                        }
                        if (line.Length > 0) result.Add(line.ToString());
                        return result;
                    };

                                        // Prescriptions
                                        DataTable dtPres = DAL.GetData(
                        @"SELECT PrescriptionId, PatientID, Medicine, Days, Dose, Instructions, Morning, Noon, Evening, Night
                          FROM Prescriptions
                          WHERE PatientID = @PatientId",
                                                new SqlParameter("@PatientId", PatientID));

                    if (dtPres.Rows.Count > 0)
                    {
                        gfx.DrawString("Prescriptions:", sectionTitle, XBrushes.DarkSlateBlue, new XRect(marginLeft, sectionY, page.Width - marginLeft * 2, 20), XStringFormats.TopLeft);
                        sectionY += 18;

                        // header row (adjusted widths to keep each prescription on one line)
                        double col1 = marginLeft + 8;      // Medicine (220)
                        double col2 = marginLeft + 230;    // Dose (40)
                        double col3 = marginLeft + 280;    // Days (40)
                        double col4 = marginLeft + 330;    // Instructions (200)
                      //  double col5 = marginLeft + 540;    // Times flags (80)

                        gfx.DrawString("Medicine", smallVal, XBrushes.Gray, new XRect(col1, sectionY, 220, 16), XStringFormats.TopLeft);
                        gfx.DrawString("Dose", smallVal, XBrushes.Gray, new XRect(col2, sectionY, 40, 16), XStringFormats.TopLeft);
                        gfx.DrawString("Days", smallVal, XBrushes.Gray, new XRect(col3, sectionY, 40, 16), XStringFormats.TopLeft);
                        gfx.DrawString("Instructions", smallVal, XBrushes.Gray, new XRect(col4, sectionY, 200, 16), XStringFormats.TopLeft);
                        //gfx.DrawString("M N E Ngt", smallVal, XBrushes.Gray, new XRect(col5, sectionY, 80, 16), XStringFormats.TopLeft);
                        sectionY += 16;


                        foreach (DataRow r in dtPres.Rows)
                        {
                            string med = r["Medicine"].ToString();
                            string dose = r["Dose"].ToString();
                            string days = r["Days"].ToString();
                            string instr = r["Instructions"].ToString();
                            string times = "";
                            times += r["Morning"].ToString() == "1" ? "M " : "- ";
                            times += r["Noon"].ToString() == "1" ? "N " : "- ";
                            times += r["Evening"].ToString() == "1" ? "E " : "- ";
                            times += r["Night"].ToString() == "1" ? "Ngt" : "-";

                            // aim to keep the prescription on a single row: truncate instructions if too long
                            int maxInstrChars = 40;
                            string instrSingle = instr ?? "";
                            if (instrSingle.Length > maxInstrChars)
                            {
                                instrSingle = instrSingle.Substring(0, maxInstrChars - 3) + "...";
                            }

                            // ensure space for one row
                            EnsureSpace(20);

                            gfx.DrawString(med, smallVal, XBrushes.Black, new XRect(col1, sectionY, 220, 16), XStringFormats.TopLeft);
                            gfx.DrawString(dose, smallVal, XBrushes.Black, new XRect(col2, sectionY, 40, 16), XStringFormats.TopLeft);
                            gfx.DrawString(days, smallVal, XBrushes.Black, new XRect(col3, sectionY, 40, 16), XStringFormats.TopLeft);
                            gfx.DrawString(instrSingle, smallVal, XBrushes.Black, new XRect(col4, sectionY, 200, 16), XStringFormats.TopLeft);
                            //gfx.DrawString(times, smallVal, XBrushes.Black, new XRect(col5, sectionY, 80, 16), XStringFormats.TopLeft);
                            sectionY += 18;
                        }

                        sectionY += 8;
                    }

                    // Examination (latest)
                                        DataTable dtExam = DAL.GetData(
                        @"SELECT TOP 1 BP, Pulse, Temperature, Weight, Height, SystemicExam, IsNormal, HeartBeat, Breath, Sugar
                          FROM PhysicalExamination
                          WHERE PatientID = @PatientId
                          ORDER BY ExamId DESC",
                                                new SqlParameter("@PatientId", PatientID));

                    if (dtExam.Rows.Count > 0)
                    {
                        var re = dtExam.Rows[0];
                        gfx.DrawString("Examination:", sectionTitle, XBrushes.DarkSlateBlue, new XRect(marginLeft, sectionY, page.Width - marginLeft * 2, 20), XStringFormats.TopLeft);
                        sectionY += 16;

                        string examLine = $"BP: {re["BP"]}  Pulse: {re["Pulse"]}  Temp: {re["Temperature"]}  Wt: {re["Weight"]}  Ht: {re["Height"]}";
                        gfx.DrawString(examLine, smallVal, XBrushes.Black, new XRect(marginLeft + 8, sectionY, page.Width - marginLeft * 2 - 16, 16), XStringFormats.TopLeft);
                        sectionY += 18;
                    }

                    // Diagnosis
                                        DataTable dtDiag = DAL.GetData(
                        @"SELECT PatientDiagnosisId, PatientID, DiagnosisType, DiagnosisText
                          FROM PatientDiagnosis
                          WHERE PatientID = @PatientId",
                                                new SqlParameter("@PatientId", PatientID));

                    if (dtDiag.Rows.Count > 0)
                    {
                        gfx.DrawString("Diagnosis:", sectionTitle, XBrushes.DarkSlateBlue, new XRect(marginLeft, sectionY, page.Width - marginLeft * 2, 20), XStringFormats.TopLeft);
                        sectionY += 16;

                        foreach (DataRow r in dtDiag.Rows)
                        {
                            string type = r["DiagnosisType"].ToString();
                            string text = r["DiagnosisText"].ToString();
                            gfx.DrawString($"- {type}: {text}", smallVal, XBrushes.Black, new XRect(marginLeft + 8, sectionY, page.Width - marginLeft * 2 - 16, 16), XStringFormats.TopLeft);
                            sectionY += 16;
                        }

                        sectionY += 6;
                    }

                    // Lab Tests (join PatientLabReports with LabTests to get NormalRange where available)
                    DataTable dtLab = DAL.GetData(
                        @"SELECT p.ReportId, p.TestName, p.ResultValue, p.Unit, COALESCE(lt.NormalRange, p.NormalRange) AS NormalRange
                          FROM PatientLabReports p
                          LEFT JOIN LabTests lt ON p.TestName = lt.TestName
                          WHERE p.PatientID = @PatientId",
                        new SqlParameter("@PatientId", PatientID));

                    if (dtLab.Rows.Count > 0)
                    {
                        gfx.DrawString("Lab Tests:", sectionTitle, XBrushes.DarkSlateBlue, new XRect(marginLeft, sectionY, page.Width - marginLeft * 2, 20), XStringFormats.TopLeft);
                        sectionY += 18;

                        foreach (DataRow r in dtLab.Rows)
                        {
                            string test = r["TestName"].ToString();
                            string result = r["ResultValue"].ToString();
                            string unit = r["Unit"].ToString();
                            string normal = r["NormalRange"].ToString();

                            gfx.DrawString(test, smallVal, XBrushes.Black, new XRect(marginLeft + 8, sectionY, 260, 16), XStringFormats.TopLeft);
                            gfx.DrawString(result + " " + unit, smallVal, XBrushes.Gray, new XRect(marginLeft + 280, sectionY, 120, 16), XStringFormats.TopLeft);
                            gfx.DrawString(normal, smallVal, XBrushes.Gray, new XRect(marginLeft + 410, sectionY, 160, 16), XStringFormats.TopLeft);
                            sectionY += 16;
                        }

                        sectionY += 8;
                    }

                    // Patient Dietary Advice
                    DataTable dtDiet = DAL.GetData(
                        @"SELECT PatientDietId, PatientID, DietType, CustomAdvice
                          FROM PatientDietAdvice
                          WHERE PatientID = @PatientId",
                        new SqlParameter("@PatientId", PatientID));

                    if (dtDiet.Rows.Count > 0)
                    {
                        gfx.DrawString("Dietary Advice:", sectionTitle, XBrushes.DarkSlateBlue, new XRect(marginLeft, sectionY, page.Width - marginLeft * 2, 20), XStringFormats.TopLeft);
                        sectionY += 18;

                        foreach (DataRow r in dtDiet.Rows)
                        {
                            string diet = r["DietType"].ToString();
                            string advice = r["CustomAdvice"].ToString();
                            gfx.DrawString($"- {diet}: {advice}", smallVal, XBrushes.Black, new XRect(marginLeft + 8, sectionY, page.Width - marginLeft * 2 - 16, 16), XStringFormats.TopLeft);
                            sectionY += 16;
                        }

                        sectionY += 8;
                    }

                    // Patient History
                    DataTable dtHistory = DAL.GetData(
                        @"SELECT HistoryId, PatientID, PresentComplaints, PastMedical, PastSurgical, DrugAllergies
                          FROM PatientHistory
                          WHERE PatientID = @PatientId",
                        new SqlParameter("@PatientId", PatientID));

                    if (dtHistory.Rows.Count > 0)
                    {
                        gfx.DrawString("History:", sectionTitle, XBrushes.DarkSlateBlue, new XRect(marginLeft, sectionY, page.Width - marginLeft * 2, 20), XStringFormats.TopLeft);
                        sectionY += 18;

                        foreach (DataRow r in dtHistory.Rows)
                        {
                            string present = r["PresentComplaints"].ToString();
                            string pastMed = r["PastMedical"].ToString();
                            string pastSurg = r["PastSurgical"].ToString();
                            string allergies = r["DrugAllergies"].ToString();

                            if (!string.IsNullOrWhiteSpace(present))
                            {
                                gfx.DrawString($"Present: {present}", smallVal, XBrushes.Black, new XRect(marginLeft + 8, sectionY, page.Width - marginLeft * 2 - 16, 16), XStringFormats.TopLeft);
                                sectionY += 16;
                            }
                            if (!string.IsNullOrWhiteSpace(pastMed))
                            {
                                gfx.DrawString($"Past Medical: {pastMed}", smallVal, XBrushes.Black, new XRect(marginLeft + 8, sectionY, page.Width - marginLeft * 2 - 16, 16), XStringFormats.TopLeft);
                                sectionY += 16;
                            }
                            if (!string.IsNullOrWhiteSpace(pastSurg))
                            {
                                gfx.DrawString($"Past Surgical: {pastSurg}", smallVal, XBrushes.Black, new XRect(marginLeft + 8, sectionY, page.Width - marginLeft * 2 - 16, 16), XStringFormats.TopLeft);
                                sectionY += 16;
                            }
                            if (!string.IsNullOrWhiteSpace(allergies))
                            {
                                gfx.DrawString($"Allergies: {allergies}", smallVal, XBrushes.Black, new XRect(marginLeft + 8, sectionY, page.Width - marginLeft * 2 - 16, 16), XStringFormats.TopLeft);
                                sectionY += 16;
                            }

                            sectionY += 6;
                        }
                    }

                    // save
                    doc.Save(pdfPath);
                }

                Process.Start(new ProcessStartInfo() { FileName = pdfPath, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate PDF: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Attempts to locate an asset by searching the executable directory and parent folders.
        private string FindAssetPath(string fileName)
        {
            // 1) Check exe/working directory Assets folder
            string try1 = Path.Combine(Application.StartupPath, "Assets", fileName);
            if (File.Exists(try1)) return try1;

            // 2) Check exe folder directly
            string try2 = Path.Combine(Application.StartupPath, fileName);
            if (File.Exists(try2)) return try2;

            // 3) Walk up parent directories looking for an Assets folder (project root during dev)
            var dir = new DirectoryInfo(Application.StartupPath);
            for (int i = 0; i < 6 && dir != null; i++)
            {
                string candidate = Path.Combine(dir.FullName, "Assets", fileName);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }

            // 4) As a last resort, check repository-relative path (two levels up)
            try
            {
                string alt = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Assets", fileName);
                alt = Path.GetFullPath(alt);
                if (File.Exists(alt)) return alt;
            }
            catch { }

            return null;
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
