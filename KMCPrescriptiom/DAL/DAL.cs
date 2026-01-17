using System.Data;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System.Data.Common;
using System;

namespace KMCPrescriptiom.DAL
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
        public static long SavePatient(
             string MRNo,
             string FullName,
             int Age,
             string Gender,
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
                _db.AddInParameter(cmd, "@Gender", DbType.String, Gender ?? string.Empty);
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
        public static DataTable GetData(string spName, params object[] parameters)
        {
            using (DbCommand cmd = _db.GetStoredProcCommand(spName, parameters))
            {
                DataSet ds = _db.ExecuteDataSet(cmd);
                return ds.Tables[0];
            }
        }
    }
}
