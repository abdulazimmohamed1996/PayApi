using System.Text;

namespace PayArabic.Core;

public static class QueryBuilder
{
    public static string GetFilterQuery(StringBuilder query, ListOptions listOptions, string alias = "")
    {
        string filterQuery = "";
        if (listOptions != null)
        {
            if (listOptions.Sort != null)
            {
                query.AppendLine(@" ORDER BY " + listOptions.Sort[0].Selector);
                if (listOptions.Sort[0].Desc)
                {
                    query.AppendLine(" DESC ");
                }
            }
            filterQuery = @" WITH ResultTable AS  
                                            (
                                                " + query.ToString() + @"
                                            )";
            filterQuery += @" SELECT (SELECT COUNT(Id) FROM ResultTable) AS TotalCount, ResultTable.* FROM ResultTable  ";
            if (!listOptions.LoadingAll)
            {
                filterQuery += @" OFFSET " + listOptions.Skip + @" ROWS -- number of skipped rows
							      FETCH NEXT " + listOptions.Take + @" ROWS ONLY -- number of returned row";
            }
        }
        return filterQuery;
    }
}
