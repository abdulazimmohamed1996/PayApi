using System.Text;

namespace PayArabic.DAO;
public class AuditDao : BaseDao, IAuditDao
{
    public IEnumerable<AuditDTO.AuditList> GetAll (string module, string function, string listOptions = null)
    {
        StringBuilder query = new StringBuilder();
        query.AppendLine(@" SELECT a.Id, a.IPAddress, u.Name [User], u.Mobile, u.Email, a.Module, a.[Function]
                                , a.CreateDate
                            FROM Audit a INNER JOIN [User] u ON u.Id = a.UserId ");
        if (!string.IsNullOrEmpty(module))
            query.AppendLine(@" AND a.Module LIKE '%" + module + "%'");
        if (!string.IsNullOrEmpty(function))
            query.AppendLine(@" AND a.[Function] LIKE '%" + function + "%'");

        List<AuditDTO.AuditList> result = null;
        if (!string.IsNullOrEmpty(listOptions))
        {
            string filterQuery = GetFilterQuery(query, listOptions);
            result = DB.Query<AuditDTO.AuditList>(filterQuery).ToList();
        }
        else
        {
            query.AppendLine("  ORDER BY d.Id DESC ");
            result = DB.Query<AuditDTO.AuditList>(query.ToString()).ToList();
        }

        return result;
    }
    public AuditDTO.AuditGetById GetById(long id)
    {
        string query = @"   SELECT a.Id, a.IPAddress, u.Name [User], u.Mobile, u.Email, a.Module, a.[Function]
                                , a.Notes, a.CreateDate
                            FROM Audit a INNER JOIN [User] u ON u.Id = a.UserId  
                            WHERE a.Id = " + id;
        return DB.Query<AuditDTO.AuditGetById>(query).FirstOrDefault();
    }
    public void Insert(AuditDTO.AuditInsert entity)
    {
        StringBuilder query = new StringBuilder();
        query.AppendLine(@" INSERT INTO Audit (UserId, IPAddress
                                            , Module, [Function]
                                            , Notes
                                            , InActive, CreatedBy, CreateDate, UpdateDate) 
                            VALUES (" + entity.UserId + ", N'" + Utility.Wrap(entity.IPAddress) + @"'
                                    , N'" + Utility.Wrap(entity.Module) + "', N'" + Utility.Wrap(entity.Function) + @"'
                                    , N'" + Utility.Wrap(entity.Notes) + @"'
                                    , 0, " + entity.UserId + @", GETDATE(), GETDATE() 
                                    );");
        DB.Execute(query.ToString());
    }
}