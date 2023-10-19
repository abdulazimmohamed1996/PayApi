namespace PayArabic.Core;

public class FilterDataSource
{
    public int First { get; set; }
    public int Rows { get; set; }
    public string SortField { get; set; }
    public string SortOrder { get; set; }
    public List<Filter> Filters { get; set; }
}
public class Filter
{
    public List<ColumnFilter> Filters { get; set; }
}
public class ColumnFilter
{
    public string Value { get; set; }
    public string MatchMode { get; set; }
    public string Operator { get; set; }
}
