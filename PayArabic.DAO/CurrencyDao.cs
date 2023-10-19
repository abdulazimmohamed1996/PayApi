using System.Text;

namespace PayArabic.DAO;

public class CurrencyDao : BaseDao, ICurrencyDao
{
    public ResponseDTO GetAll(long currentUserId, string currentUserType, string name, string listOptions = null) 
    {
        if (currentUserType != UserType.SuperAdmin.ToString() && currentUserType != UserType.Admin.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };

        StringBuilder query = new();
        query.AppendLine(@"   SELECT Id, Code, IsBase, ISNULL(NameEn, '') NameEn, ISNULL(NameAr, '') NameAr
                                , ISNULL(SymboleEn, '') SymboleEn, ISNULL(SymboleAr, '') SymboleAr
                                , InActive
                            FROM Currency
                            WHERE ISNULL(DeletedBy, 0) = 0 ");
        if (!string.IsNullOrEmpty(name)) query.AppendLine(" AND (NameEn LIKE N'%" + Utility.Wrap(name) + "%' OR NameAr LIKE N'%" + Utility.Wrap(name) + "%')");

        List<CurrencyDTO.CurrencyList> result;
        if (!string.IsNullOrEmpty(listOptions))
        {
            string filterQuery = GetFilterQuery(query, listOptions);
            result = DB.Query<CurrencyDTO.CurrencyList>(filterQuery).ToList();
        }
        else
        {
            query.AppendLine("  ORDER BY Id ASC ");
            result = DB.Query<CurrencyDTO.CurrencyList>(query.ToString()).ToList();
        }
        if (result == null || result.Count <= 0)
            return new ResponseDTO { IsValid = false, ErrorKey = "EmptyResult" };
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = result };
    }
    public ResponseDTO GetById(long currentUserId, string currentUserType, long id)
    {
        if (currentUserType != UserType.SuperAdmin.ToString() && currentUserType != UserType.Admin.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };

        if (id <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "IdRequired", Response = null }; 
        StringBuilder query = new();
        query.AppendLine(@" SELECT Id, Code, IsBase, ISNULL(NameEn, '') NameEn, ISNULL(NameAr, '') NameAr
                                , ISNULL(SymboleEn, '') SymboleEn, ISNULL(SymboleAr, '') SymboleAr
                                , DecimalPlacement, ConversionRate
                                , InActive
                            FROM Currency
                            WHERE ISNULL(DeletedBy, 0) = 0 AND Id = " + id);
        var result = DB.Query<CurrencyDTO.CurrencyGetById>(query.ToString()).FirstOrDefault();
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = result };
    }
    public ResponseDTO GetRate(string currencyCode, float amount)
    {
        if (amount <= 0)
            return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = 0 };
        StringBuilder query = new();
        query.AppendLine(@" SELECT ConversionRate
                            FROM Currency
                            WHERE ISNULL(DeletedBy, 0) = 0 AND InActive = 0 AND SymboleEn ='" + currencyCode+"'");
        var rate = DB.ExecuteScalar(query.ToString());
        try
        {
            rate = Convert.ToSingle(rate) * amount;
            return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = rate };
        }
        catch
        {
            return new ResponseDTO() { IsValid = false, ErrorKey = "CurrencyCodeNotExist", Response = null };
        }
    }
    public ResponseDTO Insert(long currentUserId, string currentUserType, CurrencyDTO.CurrencyInsert entity)
    {
        if (currentUserType != UserType.SuperAdmin.ToString() && currentUserType != UserType.Admin.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null }; 
        if (string.IsNullOrEmpty(entity.NameEn))
            return new ResponseDTO() { IsValid = false, ErrorKey = "NameEnRequired", Response = null };
        if (string.IsNullOrEmpty(entity.SymboleEn))
            return new ResponseDTO() { IsValid = false, ErrorKey = "SymboleEnRequired", Response = null };

        StringBuilder query = new();
        query.AppendLine(@" DECLARE @RowId BIGINT = 0;
                            BEGIN TRANSACTION [CurrencyInsert]
                            BEGIN TRY
                                INSERT INTO Currency (IsBase, NameEn, NameAr
                                    , SymboleEn, SymboleAR
                                    , DecimalPlacement, ConversionRate
                                    , InActive, CreatedBy, CreateDate, UpdateDate)
                                VALUES ('" + entity.IsBase + @"', N'" + Utility.Wrap(entity.NameEn) + "', N'" + Utility.Wrap(entity.NameAr) + @"'
                                    , N'" + Utility.Wrap(entity.SymboleEn) + "', N'" + Utility.Wrap(entity.SymboleAr) + @"'
                                    , " + entity.DecimalPlacement + ", " + entity.ConversionRate + @"
                                    , 0, " + currentUserId + @", GETDATE(), GETDATE() );
                                SELECT @RowId = SCOPE_IDENTITY();
                                EXECUTE dbo.GenerateEntityCode 'Currency', @RowId;                                
                            COMMIT TRANSACTION [CurrencyInsert] 
                            SELECT @RowId
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [CurrencyInsert]
                            END CATCH");
        var id = DB.ExecuteScalar(query.ToString());
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = id };
    }
    public ResponseDTO Update(long currentUserId, string currentUserType, CurrencyDTO.CurrencyUpdate entity)
    {
        if (currentUserType != UserType.SuperAdmin.ToString() && currentUserType != UserType.Admin.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };
        if (entity.Id <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "IdRequired", Response = null };
        if (string.IsNullOrEmpty(entity.NameEn))
            return new ResponseDTO() { IsValid = false, ErrorKey = "NameEnRequired", Response = null };
        if (string.IsNullOrEmpty(entity.SymboleEn))
            return new ResponseDTO() { IsValid = false, ErrorKey = "SymboleEnRequired", Response = null };
        
        StringBuilder query = new();
        query.AppendLine(@" DECLARE @RowId BIGINT = " + entity.Id + @"; 
                            DECLARE @Error NVARCHAR(MAX)='';
                            BEGIN TRANSACTION [CurrencyUpdate]
                            BEGIN TRY");
        query.AppendLine(@" IF NOT EXISTS (SELECT Id FROM Currency WHERE Id = " + entity.Id + @")
                             BEGIN RAISERROR(N'CurrencyNotExist', 11, 1); END");

        query.AppendLine(@" UPDATE Currency SET UpdateDate = GETDATE()
                                , InActive = '" + entity.InActive + @"'
                                , IsBase = N'" + entity.IsBase + "' ");
        if (!string.IsNullOrEmpty(entity.NameEn)) query.AppendLine("   , NameEn = N'" + Utility.Wrap(entity.NameEn) + "'");
        if (!string.IsNullOrEmpty(entity.NameAr)) query.AppendLine("   , NameAr = N'" + Utility.Wrap(entity.NameAr) + "'");
        if (!string.IsNullOrEmpty(entity.SymboleEn)) query.AppendLine("   , SymboleEn = N'" + Utility.Wrap(entity.SymboleEn) + "'");
        if (!string.IsNullOrEmpty(entity.SymboleAr)) query.AppendLine("   , SymboleAr = N'" + Utility.Wrap(entity.SymboleAr) + "'");
        if (entity.DecimalPlacement != 0) query.AppendLine("   , DecimalPlacement = " + entity.DecimalPlacement + "");
        if (entity.ConversionRate != 0) query.AppendLine("   , ConversionRate = " + entity.ConversionRate + "");
        query.AppendLine(@"     WHERE Id = @RowId
                                COMMIT TRANSACTION [CurrencyUpdate] 
                                SELECT @RowId
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [CurrencyUpdate]
                                SELECT ERROR_MESSAGE();
                            END CATCH");
        var result = DB.ExecuteScalar(query.ToString());
        try
        {
            result = Convert.ToInt64(result);
            return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = result };
        }
        catch
        {
            return new ResponseDTO() { IsValid = false, ErrorKey = result.ToString(), Response = null };
        }
    }
    public ResponseDTO Delete(long currentUserId, string currentUserType, long id)
    {
        if (currentUserType != UserType.SuperAdmin.ToString() && currentUserType != UserType.Admin.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null }; 
        if (id <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "IdRequired", Response = null };
        
        StringBuilder query = new();
        query.AppendLine(@" Update Currency SET DeletedBy = "+ currentUserId + @", DeleteDate = GETDATE() 
                            WHERE Id = " + id);
        DB.Execute(query.ToString());
        return new ResponseDTO() { IsValid = true };
    }
}