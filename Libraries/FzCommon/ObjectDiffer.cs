using ObjectsComparator.Comparator.Helpers;
using ObjectsComparator.Comparator.RepresentationDistinction;

namespace FzCommon
{
    public class FieldDiff(string field, string oldValue, string newValue)
    {
        public string Field = field;
        public string OldValue = oldValue;
        public string NewValue = newValue;
    }

    public class ObjectDiff(string id)
    {
        public string Id = id;
        public List<FieldDiff> Diffs = [];
    }

    public class ObjectDiffer
    {
        public static ObjectDiff DiffSensorReadings(SensorReading r1, SensorReading r2)
        {
            DeepEqualityResult result = r1.DeeplyEquals<SensorReading>(r2, ["GreenASL", "BrownASL"]);
            return ProcessResult(result, r1.Id.ToString(), "SensorReading.");
        }

        public static ObjectDiff DiffObjects(string objectId, object o1, object o2)
        {
            return ProcessResult(o1.DeeplyEquals(o2), objectId, null);
        }

        private static ObjectDiff ProcessResult(DeepEqualityResult result, string objectId, string? prefix)
        {
            ObjectDiff diff = new(objectId);
            foreach (Distinction? val in result)
            {
                if (val == null)
                {
                    continue;
                }
                string oldVal = (val.ExpectedValue == null) ? "null" : val.ExpectedValue.ToString() ?? "null";
                string newVal = (val.ActualValue == null) ? "null" : val.ActualValue.ToString() ?? "null";
                if (prefix != null && val.Path.StartsWith(prefix))
                {
                    diff.Diffs.Add(new(val.Path[prefix.Length..], oldVal, newVal));
                }
                else
                {
                    diff.Diffs.Add(new(val.Path, oldVal, newVal));
                }
            }

            return diff;
        }
    }
}
