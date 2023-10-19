using MySqlConnector;
using Npgsql;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace PayArabic.Core.Base;

public class BaseDao
{
    protected IDbConnection DB;
    public BaseDao()
    {
        //db = new SqlConnection(AppSettings.Instance.DBConnection);
        if (AppSettings.Instance.ProviderName == "PostgreSQL")
            DB = new NpgsqlConnection(AppSettings.Instance.DBConnection);
        else if (AppSettings.Instance.ProviderName == "MySQL")
            DB = new MySqlConnection(AppSettings.Instance.DBConnection);
        else
            DB = new SqlConnection(AppSettings.Instance.DBConnection);
    }
    public string GetFilterQuery(StringBuilder query, string options)
    {
        ListOptions listOptions = null;
        if (!string.IsNullOrEmpty(options))
            listOptions = Newtonsoft.Json.JsonConvert.DeserializeObject<ListOptions>(options);

        string finalQuery = "";
        string sorting = "";
        if (listOptions != null)
        {
            if (listOptions.Sort != null)
            {
                sorting = @" ORDER BY " + listOptions.Sort.Selector;
                if (listOptions.Sort.Desc)
                {
                    sorting +=" DESC ";
                }
            }
            finalQuery = @" WITH ResultTable AS  
                                            (
                                                " + query.ToString() + @"
                                            )";
            finalQuery += @" SELECT (SELECT COUNT(Id) FROM ResultTable) AS TotalCount, ResultTable.* FROM ResultTable  " + sorting;
            if (!listOptions.LoadingAll)
            {
                finalQuery += @"    OFFSET " + listOptions.Skip + @" ROWS -- number of skipped rows
							        FETCH NEXT " + listOptions.Take + @" ROWS ONLY -- number of returned row ";
            }
        }
        return finalQuery;
    }
    public string UploadFile(object entityId, string entityType, AttachmentDTO entity)
    {
        //if (string.IsNullOrEmpty(entity.Attachment))
        //    throw new Exception("NoFileToUpload");

        entity.Name = entityType + "_" + entityId.ToString() + "_" + DateTime.Now.Ticks + "_" + entity.DisplayName;
        string[] splitted = entity.Attachment.Split("base64,");
        string realBase64 = splitted[splitted.Length - 1];

        //var result = Utility.ValidateFileUpload(entity.Attachment);
        //if (result == null || !result.IsValid)
        //    return result;

        string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", entityType, entity.Name);
        File.WriteAllBytes(path, Convert.FromBase64String(realBase64));
        return entity.Name;
    }
}
