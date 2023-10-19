using System.Text;

namespace PayArabic.DAO;

public class ConfigurationDao : BaseDao, IConfigurationDao
{
    public string GetConfigurationByName(string name)
    {
        string query = @"   SELECT ISNULL([Value], '') [Value]
					        FROM [Configuration]
					        WHERE DeletedBy IS NULL AND InActive = 0 AND [Name] = '" + name + "'";
        return DB.ExecuteScalar<string>(query);
    }
    public List<ConfigurationDTO> GetByIntegrationType(string type)
    {
        StringBuilder query = new();
        query.AppendLine(@" SELECT ISNULL(con.[Name], '') [Name]
                                    , ISNULL(con.[Value], '') [Value]
                                    , ISNULL(con.Code, '') Code
					        FROM [Configuration] con 
                                INNER JOIN Integration intg ON intg.Code = con.Code 
					        WHERE ISNULL(con.DeletedBy, 0) = 0 AND con.InActive = 0 
                                AND ISNULL(intg.DeletedBy, 0) = 0 AND intg.InActive = 0 
                                AND intg.[Type] = '" + type + "'");
        return DB.Query<ConfigurationDTO>(query.ToString()).ToList();
    }
    public List<ConfigurationDTO> GetByIntegrationCode(string code)
    {
        StringBuilder query = new();
        query.AppendLine(@" SELECT ISNULL(con.[Name], '') [Name]
                                    , ISNULL(con.[Value], '') [Value]
                                    , ISNULL(con.Code, '') Code
					        FROM [Configuration] con 
                                INNER JOIN Integration intg ON intg.Code = con.Code 
                                    AND (intg.[PaymentMethodType] IS NULL OR intg.[PaymentMethodType] = '') 
					        WHERE ISNULL(con.DeletedBy, 0) = 0 AND con.InActive = 0 
                                AND ISNULL(intg.DeletedBy, 0) = 0 AND intg.InActive = 0 
                                AND intg.[Code] = '" + code + "'");
        return DB.Query<ConfigurationDTO>(query.ToString()).ToList();
    }
}