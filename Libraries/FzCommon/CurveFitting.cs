using System.Data;
using MathNet.Numerics;

using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class CurveFitFormula
    {
        public int CalibrationId { get; set; }
        public double Constant { get; set; }
        public double x1 { get; set; }
        public double x2 { get; set; }
        public double x3 { get; set; }
        public double x4 { get; set; }
        public double x5 { get; set; }
    }

    public static class CurveFitting
    {
        private const int CURVE_DEGREE = 5;

        public static bool CalculateCurveFit(int calibrationid)
        {
            List<double> waterheights = new List<double>(), discharges = new List<double>();
            GetCalibrations(calibrationid, ref waterheights, ref discharges);
            if (waterheights.Count <= 0 || discharges.Count <= 0) return false;

            double[] coefficients = Fit.Polynomial(waterheights.ToArray(), discharges.ToArray(), CURVE_DEGREE);

            return (coefficients != null && coefficients.Length >= (CURVE_DEGREE + 1)) ? SaveCurveFitFormula(new CurveFitFormula
            {
                CalibrationId = calibrationid,
                Constant = coefficients[0],
                x1 = coefficients[1],
                x2 = coefficients[2],
                x3 = coefficients[3],
                x4 = coefficients[4],
                x5 = coefficients[5]
            }) : false;
        }

        private static void GetCalibrations(int calibrationid, ref List<double> waterheights, ref List<double> discharges)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand($"SELECT WaterHeight, Discharge FROM StageDischarge WHERE (NOT WaterHeight IS NULL) AND (NOT Discharge IS NULL) AND CalibrationId = {calibrationid}", new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString])))
                {
                    cmd.Connection.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (dr.Read())
                        {
                            waterheights.Add((double)dr["WaterHeight"]);
                            discharges.Add((double)dr["Discharge"]);
                        }
                        dr.Close();
                    }
                }
            }
            catch {  }
        }

        private static bool SaveCurveFitFormula(CurveFitFormula cfformula )
        {
            try
            {
                string sql = $"UPDATE CurveFitFormulas SET Constant = {cfformula.Constant}, x1 = {cfformula.x1}, x2 = {cfformula.x2}, x3 = {cfformula.x3}, x4 = {cfformula.x4}, x5 = {cfformula.x5} WHERE CalibrationId = {cfformula.CalibrationId}; ";
                sql += "if (@@ROWCOUNT <= 0) ";
                sql += "begin ";
                sql += $"INSERT INTO CurveFitFormulas(CalibrationId, Constant, x1, x2, x3, x4, x5) VALUES({cfformula.CalibrationId}, {cfformula.Constant}, {cfformula.x1}, {cfformula.x2}, {cfformula.x3}, {cfformula.x4}, {cfformula.x5}) ";
                sql += "end; ";

                using (SqlCommand cmd = new SqlCommand(sql, new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString])))
                {
                    cmd.Connection.Open();
                    cmd.ExecuteNonQuery();
                    cmd.Connection.Close();
                }
                return true;
            }
            catch { return false; }
        }


        public static double[] GetDisharges(int calibrationid, double[] wl)
        {
            if (wl == null || wl.Length <= 0) return null;

            CurveFitFormula cff = GetCurveFitFormula(calibrationid);
            if (cff == null) return null;

            double[] discharge = new double[wl.Length];

            for (int i = 0; i < wl.Length; i++)
            {
                discharge[i] = CalculateDischarge(cff, wl[i]);
            }
            return discharge;
        }

        public static double CalculateDischarge(CurveFitFormula cff, double wvalue)
        {
            return cff.x5 * Math.Pow(wvalue, 5) +
                    cff.x4 * Math.Pow(wvalue, 4) +
                    cff.x3 * Math.Pow(wvalue, 3) +
                    cff.x2 * Math.Pow(wvalue, 2) +
                    cff.x1 * Math.Pow(wvalue, 1) +
                    cff.Constant;
        }

        public static CurveFitFormula GetCurveFitFormula(int calibrationid)
        {
            CurveFitFormula cff = null;
            try
            {
                using (SqlCommand cmd = new SqlCommand($"SELECT * FROM CurveFitFormulas WHERE CalibrationId = {calibrationid}", new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString])))
                {
                    cmd.Connection.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        if (dr.Read())
                        {
                            cff = new CurveFitFormula
                            {
                                CalibrationId = (int)dr["CalibrationId"],
                                Constant = (dr["Constant"] != null && dr["Constant"] != DBNull.Value) ? (double)dr["Constant"] : 0,
                                x1 = (dr["x1"] != null && dr["x1"] != DBNull.Value) ? (double)dr["x1"] : 0,
                                x2 = (dr["x2"] != null && dr["x2"] != DBNull.Value) ? (double)dr["x2"] : 0,
                                x3 = (dr["x3"] != null && dr["x3"] != DBNull.Value) ? (double)dr["x3"] : 0,
                                x4 = (dr["x4"] != null && dr["x4"] != DBNull.Value) ? (double)dr["x4"] : 0,
                                x5 = (dr["x5"] != null && dr["x5"] != DBNull.Value) ? (double)dr["x5"] : 0
                            };
                        }
                        dr.Close();
                    }
                }
            }
            catch {  }
            return cff;
        }
    }
}
