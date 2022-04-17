using System;
using System.Collections.Generic;

namespace FzCommon.ExternalModels.UsgsJsonData
{
    public class SiteCode
    {
        public string Value { get; set; }
        public string Network { get; set; }
        public string AgencyCode { get; set; }
    }

    public class DefaultTimeZone
    {
        public string ZoneOffset { get; set; }
        public string ZoneAbbreviation { get; set; }
    }

    public class DaylightSavingsTimeZone
    {
        public string ZoneOffset { get; set; }
        public string ZoneAbbreviation { get; set; }
    }

    public class TimeZoneInfo
    {
        public DefaultTimeZone DefaultTimeZone { get; set; }
        public DaylightSavingsTimeZone DaylightSavingsTimeZone { get; set; }
        public bool SiteUsesDaylightSavingsTime { get; set; }
    }

    public class GeogLocation
    {
        public string Srs { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class GeoLocation
    {
        public GeogLocation GeogLocation { get; set; }
        public List<object> LocalSiteXY { get; set; }
    }

    public class SiteProperty
    {
        public string Value { get; set; }
        public string Name { get; set; }
    }

    public class SourceInfo
    {
        public string SiteName { get; set; }
        public List<SiteCode> SiteCode { get; set; }
        public TimeZoneInfo TimeZoneInfo { get; set; }
        public GeoLocation GeoLocation { get; set; }
        public List<object> Note { get; set; }
        public List<object> TiteType { get; set; }
        public List<SiteProperty> TiteProperty { get; set; }
    }

    public class VariableCode
    {
        public string Value { get; set; }
        public string Network { get; set; }
        public string Vocabulary { get; set; }
        public int VariableID { get; set; }
        public bool Default { get; set; }
    }

    public class Unit
    {
        public string UnitCode { get; set; }
    }

    public class Option
    {
        public string Name { get; set; }
        public string OptionCode { get; set; }
    }

    public class Options
    {
        public List<Option> Option { get; set; }
    }

    public class Variable
    {
        public List<VariableCode> VariableCode { get; set; }
        public string VariableName { get; set; }
        public string VariableDescription { get; set; }
        public string ValueType { get; set; }
        public Unit unit { get; set; }
        public Options Options { get; set; }
        public List<object> Note { get; set; }
        public double NoDataValue { get; set; }
        public List<object> NariableProperty { get; set; }
        public string Oid { get; set; }
    }

    public class TimeSeriesValue
    {
        public double Value { get; set; }
        public List<string> Qualifiers { get; set; }
        public DateTime DateTime { get; set; }
    }

    public class Qualifier
    {
        public string QualifierCode { get; set; }
        public string QualifierDescription { get; set; }
        public int QualifierID { get; set; }
        public string Network { get; set; }
        public string Vocabulary { get; set; }
    }

    public class Method
    {
        public string MethodDescription { get; set; }
        public int MethodID { get; set; }
    }

    public class TimeSeriesValues
    {
        public List<TimeSeriesValue> Value { get; set; }
        public List<Qualifier> Qualifier { get; set; }
        public List<object> QualityControlLevel { get; set; }
        public List<Method> Method { get; set; }
        public List<object> Source { get; set; }
        public List<object> Offset { get; set; }
        public List<object> Sample { get; set; }
        public List<object> CensorCode { get; set; }
    }

    public class TimeSeries
    {
        public SourceInfo SourceInfo { get; set; }
        public Variable Variable { get; set; }
        public List<TimeSeriesValues> Values { get; set; }
        public string Name { get; set; }
    }

    public class TopLevelValue
    {
        public List<TimeSeries> TimeSeries { get; set; }
    }

    public class UsgsJsonResponse
    {
        public string Name { get; set; }
        public TopLevelValue Value { get; set; }
    }
}
