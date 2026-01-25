using System.Data;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System.Data.Common;
using System;

namespace KMCPrescriptiom.DataAccessLayer
{
    public static class DAL
    {
        private static readonly Database _db =
            DatabaseFactory.CreateDatabase("KMC");

        public static DataTable GetData(string sql)
        {
            using (DbCommand cmd = _db.GetSqlStringCommand(sql))
            {
                DataSet ds = _db.ExecuteDataSet(cmd);
                return ds.Tables[0];
            }
        }
        public static DataTable GetData(string sql, params DbParameter[] parameters)
        {
            using (DbCommand cmd = _db.GetSqlStringCommand(sql))
            {
                if (parameters != null)
                {
                    foreach (DbParameter param in parameters)
                    {
                        cmd.Parameters.Add(param);
                    }
                }

                DataSet ds = _db.ExecuteDataSet(cmd);
                return ds.Tables.Count > 0 ? ds.Tables[0] : new DataTable();
            }
        }
        public static DataTable GetData(string spName, params object[] parameters)
        {
            using (DbCommand cmd = _db.GetStoredProcCommand(spName, parameters))
            {
                DataSet ds = _db.ExecuteDataSet(cmd);
                return ds.Tables[0];
            }
        }
        public static long SavePatient(
             string MRNo,
             string FullName,
             int Age,
             int GenderIndex,
             string ContactNo,
             DateTime? Visit = null,
             long? PatientId = null)
        {
            using (DbCommand cmd = _db.GetStoredProcCommand("usp_SavePatient"))
            {
                // Input parameters
                _db.AddInParameter(cmd, "@MRNo", DbType.String, MRNo ?? string.Empty);
                _db.AddInParameter(cmd, "@FullName", DbType.String, FullName ?? string.Empty);
                _db.AddInParameter(cmd, "@Age", DbType.Int32, Age);
                _db.AddInParameter(cmd, "@Gender", DbType.String, GenderIndex.ToString() ?? string.Empty);
                _db.AddInParameter(cmd, "@ContactNo", DbType.String, ContactNo ?? string.Empty);

                // Handle nullable Visit date
                _db.AddInParameter(cmd, "@Visit", DbType.DateTime, Visit.HasValue ? (object)Visit.Value : DBNull.Value);

                // Optional PatientId for update

                // Output parameter to get inserted PatientId
                _db.AddOutParameter(cmd, "@PatientId", DbType.Int64, 8);

                // Execute the stored procedure
                _db.ExecuteNonQuery(cmd);

                // Return the output PatientId
                object result = _db.GetParameterValue(cmd, "@PatientId");
                return result != null ? Convert.ToInt64(result) : 0;
            }
        }
        public static long SavePatienHistory(
             long PatientID,
             string PresentComplaints,
             string PastMedical,
             string PastSurgical,
             string DrugAllergies)
        {
            using (DbCommand cmd = _db.GetStoredProcCommand("usp_SavePatientHistory"))
            {
                // Input parameters
                _db.AddInParameter(cmd, "@PatientID", DbType.Int64, PatientID);
                _db.AddInParameter(cmd, "@PresentComplaints", DbType.String, PresentComplaints ?? string.Empty);
                _db.AddInParameter(cmd, "@PastMedical", DbType.String, PastMedical.ToString() ?? string.Empty);
                _db.AddInParameter(cmd, "@PastSurgical", DbType.String, PastSurgical ?? string.Empty);
                _db.AddInParameter(cmd, "@DrugAllergies", DbType.String, DrugAllergies ?? string.Empty);
                
                // Output parameter to get inserted PatientId
                _db.AddOutParameter(cmd, "@HistoryId", DbType.Int64, 8);

                // Execute the stored procedure
                _db.ExecuteNonQuery(cmd);

                // Return the output PatientId
                object result = _db.GetParameterValue(cmd, "@HistoryId");
                return result != null ? Convert.ToInt64(result) : 0;
            }
        }

        public static long SavePhysicalExamination(
    long PatientID,
    string BP,
    string Pulse,
    string Temperature,
    string Weight,
    string Height,
    string SystemicExam,
    bool IsNormal,
    string HeartBeat,
    string Breath,
    string Sugar,
    long? ExamId = null
)

        {
            using (DbCommand cmd = _db.GetStoredProcCommand("usp_SavePhysicalExamination"))
            {
                _db.AddInParameter(cmd, "@PatientID", DbType.Int64, PatientID);
                _db.AddInParameter(cmd, "@BP", DbType.String, BP ?? string.Empty);
                _db.AddInParameter(cmd, "@Pulse", DbType.String, Pulse ?? string.Empty);
                _db.AddInParameter(cmd, "@Temperature", DbType.String, Temperature ?? string.Empty);
                _db.AddInParameter(cmd, "@Weight", DbType.String, Weight ?? string.Empty);
                _db.AddInParameter(cmd, "@Height", DbType.String, Height ?? string.Empty);
                _db.AddInParameter(cmd, "@SystemicExam", DbType.String, SystemicExam ?? string.Empty);
                _db.AddInParameter(cmd, "@IsNormal", DbType.Boolean, IsNormal);
                _db.AddInParameter(cmd, "@HeartBeat", DbType.String, HeartBeat);
                _db.AddInParameter(cmd, "@Breath", DbType.String, Breath);
                _db.AddInParameter(cmd, "@Sugar", DbType.String, Sugar);

                // Single OUTPUT parameter
                _db.AddOutParameter(cmd, "@ExamId", DbType.Int64, 8);

                // If update, set initial value
                if (ExamId.HasValue)
                    _db.SetParameterValue(cmd, "@ExamId", ExamId.Value);

                _db.ExecuteNonQuery(cmd);

                return Convert.ToInt64(_db.GetParameterValue(cmd, "@ExamId"));
            }
        }
        public static long SavePatientLabReport(
             long PatientID,
             string TestName,
             string ResultValue,
             string Unit,
             string NormalRange,
             long? ReportId = null
         )
        {
            using (DbCommand cmd = _db.GetStoredProcCommand("usp_SavePatientLabReport"))
            {
                // Input parameters
                _db.AddInParameter(cmd, "@PatientID", DbType.Int64, PatientID);
                _db.AddInParameter(cmd, "@TestName", DbType.String, TestName ?? string.Empty);
                _db.AddInParameter(cmd, "@ResultValue", DbType.String, ResultValue ?? string.Empty);
                _db.AddInParameter(cmd, "@Unit", DbType.String, Unit ?? string.Empty);
                _db.AddInParameter(cmd, "@NormalRange", DbType.String, NormalRange ?? string.Empty);

                // OUTPUT parameter
                _db.AddOutParameter(cmd, "@ReportId", DbType.Int64, 8);

                // For update
                if (ReportId.HasValue)
                    _db.SetParameterValue(cmd, "@ReportId", ReportId.Value);

                _db.ExecuteNonQuery(cmd);

                return Convert.ToInt64(_db.GetParameterValue(cmd, "@ReportId"));
            }
        }
        public static long SavePatientDiagnosis(
            long PatientID,
            string DiagnosisType,
            string DiagnosisText,
            long? PatientDiagnosisId = null  // optional, for update
        )
        {
            using (DbCommand cmd = _db.GetStoredProcCommand("usp_SavePatientDiagnosis"))
            {
                // Input parameters
                _db.AddInParameter(cmd, "@PatientID", DbType.Int64, PatientID);
                _db.AddInParameter(cmd, "@DiagnosisType", DbType.String, DiagnosisType ?? string.Empty);
                _db.AddInParameter(cmd, "@DiagnosisText", DbType.String, DiagnosisText ?? string.Empty);

                // Output parameter
                _db.AddOutParameter(cmd, "@PatientDiagnosisId", DbType.Int64, 8);

                // For update, set initial value
                if (PatientDiagnosisId.HasValue)
                    _db.SetParameterValue(cmd, "@PatientDiagnosisId", PatientDiagnosisId.Value);

                // Execute stored procedure
                _db.ExecuteNonQuery(cmd);

                // Return inserted/updated ID
                object result = _db.GetParameterValue(cmd, "@PatientDiagnosisId");
                return result != null ? Convert.ToInt64(result) : 0;
            }
        }
        public static long SavePrescription(
            long PatientID,
            string Medicine,
            string Days,
            string Dose,
            string Instructions,
            bool Morning,
            bool Noon,
            bool Evening,
            bool Night,
            long? PrescriptionId = null  // optional, for update
        )
        {
            using (DbCommand cmd = _db.GetStoredProcCommand("usp_SavePrescription"))
            {
                // Input parameters
                _db.AddInParameter(cmd, "@PatientID", DbType.Int64, PatientID);
                _db.AddInParameter(cmd, "@Medicine", DbType.String, Medicine ?? string.Empty);
                _db.AddInParameter(cmd, "@Days", DbType.String, Days ?? string.Empty);
                _db.AddInParameter(cmd, "@Dose", DbType.String, Dose ?? string.Empty);
                _db.AddInParameter(cmd, "@Instructions", DbType.String, Instructions ?? string.Empty);
                _db.AddInParameter(cmd, "@Morning", DbType.Boolean, Morning);
                _db.AddInParameter(cmd, "@Noon", DbType.Boolean, Noon);
                _db.AddInParameter(cmd, "@Evening", DbType.Boolean, Evening);
                _db.AddInParameter(cmd, "@Night", DbType.Boolean, Night);

                // Output parameter
                _db.AddOutParameter(cmd, "@PrescriptionId", DbType.Int64, 8);

                // If updating, set initial value
                if (PrescriptionId.HasValue)
                    _db.SetParameterValue(cmd, "@PrescriptionId", PrescriptionId.Value);

                _db.ExecuteNonQuery(cmd);

                object result = _db.GetParameterValue(cmd, "@PrescriptionId");
                return result != null ? Convert.ToInt64(result) : 0;
            }
        }
        public static long SavePatientDietAdvice(
            long PatientID,
            string DietType,
            string CustomAdvice,
            long? PatientDietId = null  // optional, for update
        )
        {
            using (DbCommand cmd = _db.GetStoredProcCommand("usp_SavePatientDietAdvice"))
            {
                _db.AddInParameter(cmd, "@PatientID", DbType.Int64, PatientID);
                _db.AddInParameter(cmd, "@DietType", DbType.String, DietType ?? string.Empty);
                _db.AddInParameter(cmd, "@CustomAdvice", DbType.String, CustomAdvice ?? string.Empty);

                _db.AddOutParameter(cmd, "@PatientDietId", DbType.Int64, 8);

                if (PatientDietId.HasValue)
                    _db.SetParameterValue(cmd, "@PatientDietId", PatientDietId.Value);

                _db.ExecuteNonQuery(cmd);

                return Convert.ToInt64(_db.GetParameterValue(cmd, "@PatientDietId"));
            }
        }
        public static Dataset.Prescription GetPatientVisitReport(long patientId)
        {
            Dataset.Prescription ds = new Dataset.Prescription();

            using (DbCommand cmd = _db.GetStoredProcCommand("usp_GetPatientVisitReport"))
            {
                _db.AddInParameter(cmd, "@PatientID", DbType.Int64, patientId);

                _db.LoadDataSet(
                    cmd,
                    ds,
                    new string[]
                    {
                "Patient",
                "History",
                "PhysicalExam",
                "Diagnosis",
                "Prescriptions",
                "LabReports",
                "DietAdvice"
                    });
            }

            return ds;
        }



    }
}
