using System.Text;
using static PayArabic.Core.DTO.DepositDTO;

namespace PayArabic.DAO;

public class ProductCategoryDao : BaseDao, IProductCategoryDao
{ 
    public ResponseDTO GetAll(long currentUserId, string currentUserType, string listOptions = null)
    {
        if (currentUserType == UserType.Vendor.ToString() && currentUserType == UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" SELECT Id, Code, ISNULL(NameEn, '') NameEn, ISNULL(NameAr, '') NameAr, Sort, InActive, CreateDate
                            FROM ProductCategory
                            WHERE ISNULL(DeletedBy, 0) = 0 
                                AND VendorId = " + currentUserId + @"");
        List<ProductCategoryDTO.ProductCategoryList> result = null;
        if (!string.IsNullOrEmpty(listOptions))
        {
            string filterQuery = GetFilterQuery(query, listOptions);
            result = DB.Query<ProductCategoryDTO.ProductCategoryList>(filterQuery).ToList();
        }
        else
        {
            query.AppendLine("  ORDER BY Id DESC ");
            result = DB.Query<ProductCategoryDTO.ProductCategoryList>(query.ToString()).ToList();
        }
        if (result == null || result.Count() <= 0)
            return new ResponseDTO { IsValid = false, ErrorKey = "EmptyResult" };
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = result };
    }
    public ResponseDTO GetById(long currentUserId, string currentUserType, long id)
    {
        if (currentUserType == UserType.Vendor.ToString() && currentUserType == UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };

        if (id <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "IdRequired", Response = null };
        StringBuilder query = new StringBuilder();
        query.AppendLine(@" SELECT Id, Code, ISNULL(NameEn, '') NameEn, ISNULL(NameAr, '') NameAr, Sort, InActive, CreateDate
                            FROM ProductCategory
                            WHERE ISNULL(DeletedBy, 0) = 0 
                                AND VendorId = " + currentUserId + @"
                                AND Id = " + id);
        var result = DB.Query<ProductCategoryDTO.ProductCategoryGetById>(query.ToString()).FirstOrDefault();
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = result };
    }
    public ResponseDTO Insert(long currentUserId, string currentUserType, ProductCategoryDTO.ProductCategoryInsert entity)
    {
        if (currentUserType == UserType.Vendor.ToString() && currentUserType == UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };
        if (string.IsNullOrEmpty(entity.NameEn))
            return new ResponseDTO() { IsValid = false, ErrorKey = "NameEnRequired", Response = null };

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" DECLARE @RowId BIGINT = 0;
                            BEGIN TRANSACTION [ProductCategoryInsert]
                            BEGIN TRY
                                INSERT INTO ProductCategory (VendorId, NameEn, NameAr, Sort
                                    , InActive, CreatedBy, CreateDate, UpdateDate)
                                VALUES (" + currentUserId + @", N'" + Utility.Wrap(entity.NameEn) + "', N'" + Utility.Wrap(entity.NameAr) + "', " + entity.Sort + @"
                                    , 0, " + currentUserId + @", GETDATE(), GETDATE() );
                                SELECT @RowId = SCOPE_IDENTITY();
                                EXECUTE dbo.GenerateEntityCode 'ProductCategory', @RowId;                                
                            COMMIT TRANSACTION [ProductCategoryInsert] 
                            SELECT @RowId
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [ProductCategoryInsert]
                            END CATCH");
        var id = DB.ExecuteScalar(query.ToString());
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = id };
    }
    public ResponseDTO Update(long currentUserId, string currentUserType, ProductCategoryDTO.ProductCategoryUpdate entity)
    {
        if (currentUserType == UserType.Vendor.ToString() && currentUserType == UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };
        if (entity.Id <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "IdRequired", Response = null };
        if (string.IsNullOrEmpty(entity.NameEn))
            return new ResponseDTO() { IsValid = false, ErrorKey = "NameEnRequired", Response = null };

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" DECLARE @RowId BIGINT = " + entity.Id + @"; 
                            DECLARE @Error NVARCHAR(MAX)='';
                            BEGIN TRANSACTION [ProductCategoryUpdate]
                            BEGIN TRY
                                IF NOT EXISTS (SELECT Id FROM ProductCategory WHERE VendorId = " + currentUserId + @" AND Id = @RowId)
                                BEGIN RAISERROR(N'ProductCategoryNotExist', 11, 1); END");

        query.AppendLine(@"     UPDATE ProductCategory SET UpdateDate = GETDATE(), InActive = '" + entity.InActive + @"'");
        if (!string.IsNullOrEmpty(entity.NameEn)) query.AppendLine("   , NameEn = N'" + Utility.Wrap(entity.NameEn) + "'");
        if (!string.IsNullOrEmpty(entity.NameAr)) query.AppendLine("   , NameAr = N'" + Utility.Wrap(entity.NameAr) + "'");
        if (entity.Sort > 0) query.AppendLine("   , Sort = " + entity.Sort + "");
        query.AppendLine(@"     WHERE Id = @RowId
                                COMMIT TRANSACTION [ProductCategoryUpdate] 
                                SELECT @RowId
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [ProductCategoryUpdate]
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
        if (currentUserType == UserType.Vendor.ToString() && currentUserType == UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };
        if (id <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "IdRequired", Response = null };

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" DECLARE @Error NVARCHAR(MAX) = '';
                            IF NOT EXISTS (SELECT Id FROM ProductCategory WHERE VendorId = " + currentUserId + @" AND Id = "+ id + @")
                            BEGIN SET @Error = N'ProductCategoryNotExist'; END
                            IF @Error = ''
                            BEGIN
                                Update ProductCategory SET InActive = 1, DeletedBy = " + currentUserId + @", DeleteDate = GETDATE() 
                                WHERE VendorId = " + currentUserId + " AND Id = " + id+@"
                            END
                            SELECT @Error; ");
        var result = DB.ExecuteScalar(query.ToString());
        if(result != null && result.ToString().Length>0)
            return new ResponseDTO() { IsValid = false, ErrorKey = result.ToString(), Response = null };
        else
            return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = null };
    }
}