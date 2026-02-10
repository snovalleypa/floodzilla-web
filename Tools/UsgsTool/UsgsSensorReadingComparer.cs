using FzCommon;

namespace UsgsTool
{
    public class UsgsSensorReadingComparer
    {
        // This is ugly, but we need to compare every part of the readings.  Tab-separating the fields should
        // make the output slightly easier to import into a spreadsheet for analysis.
        public static void CompareReadings(SensorReading sr1, SensorReading sr2)
        {
            if (sr1.GroundHeight != sr2.GroundHeight)
            {
                Console.WriteLine("MISMATCH:\tGroundHeight\t[{0}]\t{1}\tvs\t[{2}]\t{3}", sr1.Id, sr1.GroundHeight, sr2.Id, sr2.GroundHeight);
            }
            if (sr1.DistanceReading != sr2.DistanceReading)
            {
                Console.WriteLine("MISMATCH:\tDistanceReading\t[{0}]\t{1}\tvs\t[{2}]\t{3}", sr1.Id, sr1.DistanceReading, sr2.Id, sr2.DistanceReading);
            }
            if (sr1.RawWaterHeight != sr2.RawWaterHeight)
            {
                Console.WriteLine("MISMATCH:\tRawWaterHeight\t[{0}]\t{1}\tvs\t[{2}]\t{3}", sr1.Id, sr1.RawWaterHeight, sr2.Id, sr2.RawWaterHeight);
            }
            if (sr1.WaterHeight != sr2.WaterHeight)
            {
                Console.WriteLine("MISMATCH:\tWaterHeight\t[{0}]\t{1}\tvs\t[{2}]\t{3}", sr1.Id, sr1.WaterHeight, sr2.Id, sr2.WaterHeight);
            }
            if (sr1.GroundHeightFeet != sr2.GroundHeightFeet)
            {
                Console.WriteLine("MISMATCH:\tGroundHeightFeet\t[{0}]\t{1}\tvs\t[{2}]\t{3}", sr1.Id, sr1.GroundHeightFeet, sr2.Id, sr2.GroundHeightFeet);
            }
            if (sr1.DistanceReadingFeet != sr2.DistanceReadingFeet)
            {
                Console.WriteLine("MISMATCH:\tDistanceReadingFeet\t[{0}]\t{1}\tvs\t[{2}]\t{3}", sr1.Id, sr1.DistanceReadingFeet, sr2.Id, sr2.DistanceReadingFeet);
            }
            if (sr1.RawWaterHeightFeet != sr2.RawWaterHeightFeet)
            {
                Console.WriteLine("MISMATCH:\tRawWaterHeightFeet\t[{0}]\t{1}\tvs\t[{2}]\t{3}", sr1.Id, sr1.RawWaterHeightFeet, sr2.Id, sr2.RawWaterHeightFeet);
            }
            if (sr1.WaterHeightFeet != sr2.WaterHeightFeet)
            {
                Console.WriteLine("MISMATCH:\tWaterHeightFeet\t[{0}]\t{1}\tvs\t[{2}]\t{3}", sr1.Id, sr1.WaterHeightFeet, sr2.Id, sr2.WaterHeightFeet);
            }
            if (sr1.WaterDischarge != sr2.WaterDischarge)
            {
                Console.WriteLine("MISMATCH:\tWaterDischarge\t[{0}]\t{1}\tvs\t[{2}]\t{3}", sr1.Id, sr1.WaterDischarge, sr2.Id, sr2.WaterDischarge);
            }
            if (sr1.IsDeleted != sr2.IsDeleted)
            {
                Console.WriteLine("MISMATCH:\tIsDeleted\t[{0}]\t{1}\tvs\t[{2}]\t{3}", sr1.Id, sr1.IsDeleted, sr2.Id, sr2.IsDeleted);
            }
            if (sr1.IsFiltered != sr2.IsFiltered)
            {
                Console.WriteLine("MISMATCH:\tIsFiltered\t[{0}]\t{1}\tvs\t[{2}]\t{3}", sr1.Id, sr1.IsFiltered, sr2.Id, sr2.IsFiltered);
            }
            if (sr1.BenchmarkElevation != sr2.BenchmarkElevation)
            {
                Console.WriteLine("MISMATCH:\tBenchmarkElevation\t[{0}]\t{1}\tvs\t[{2}]\t{3}", sr1.Id, sr1.BenchmarkElevation, sr2.Id, sr2.BenchmarkElevation);
            }
            if (sr1.RelativeSensorHeight != sr2.RelativeSensorHeight)
            {
                Console.WriteLine("MISMATCH:\tRelativeSensorHeight\t[{0}]\t{1}\tvs\t[{2}]\t{3}", sr1.Id, sr1.RelativeSensorHeight, sr2.Id, sr2.RelativeSensorHeight);
            }
            if (sr1.Green != sr2.Green)
            {
                Console.WriteLine("MISMATCH:\tGreen\t[{0}]\t{1}\tvs\t[{2}]\t{3}", sr1.Id, sr1.Green, sr2.Id, sr2.Green);
            }
            if (sr1.Brown != sr2.Brown)
            {
                Console.WriteLine("MISMATCH:\tBrown\t[{0}]\t{1}\tvs\t[{2}]\t{3}", sr1.Id, sr1.Brown, sr2.Id, sr2.Brown);
            }
            if (sr1.RoadSaddleHeight != sr2.RoadSaddleHeight)
            {
                Console.WriteLine("MISMATCH:\tRoadSaddleHeight\t[{0}]\t{1}\tvs\t[{2}]\t{3}", sr1.Id, sr1.RoadSaddleHeight, sr2.Id, sr2.RoadSaddleHeight);
            }
            if (sr1.MarkerOneHeight != sr2.MarkerOneHeight)
            {
                Console.WriteLine("MISMATCH:\tMarkerOneHeight\t[{0}]\t{1}\tvs\t[{2}]\t{3}", sr1.Id, sr1.MarkerOneHeight, sr2.Id, sr2.MarkerOneHeight);
            }
            if (sr1.MarkerTwoHeight != sr2.MarkerTwoHeight)
            {
                Console.WriteLine("MISMATCH:\tMarkerTwoHeight\t[{0}]\t{1}\tvs\t[{2}]\t{3}", sr1.Id, sr1.MarkerTwoHeight, sr2.Id, sr2.MarkerTwoHeight);
            }
        }
    }
}