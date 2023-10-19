using PayArabic.Core.Model;
using System.Text;
namespace PayArabic.DAO;

public class ProductDao : BaseDao, IProductDao
{
    public ResponseDTO GetAll(long currentUserId, string currentUserType, long categoryId, string name, string desc, string listOptions = null)
    {
        if (currentUserType == UserType.Vendor.ToString() && currentUserType == UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" SELECT p.Id, p.Code, ISNULL(p.NameEn, '') NameEn, ISNULL(p.NameAr, '') NameAr
                                , p.Quantity, p.Price, p.Stockable 
                                , ISNULL(c.NameEn, '') CategoryNameEn, ISNULL(c.NameAr, '') CategoryNameAr
                            FROM Product p 
                                INNER JOIN ProductCategory c ON c.Id = p.CategoryId
                            WHERE ISNULL(p.DeletedBy, 0) = 0 
                                AND ISNULL(c.DeletedBy, 0) = 0 
                                AND p.VendorId = " + currentUserId + @"
                                AND c.VendorId = " + currentUserId + @"");
        if (categoryId > 0) query.AppendLine(@" AND c.Id ="+categoryId);
        if (!string.IsNullOrEmpty(name)) query.AppendLine(@" AND (p.NameEn LIKE '%" + Utility.Wrap(name) + "%' OR p.NameAr LIKE '%"+Utility.Wrap(name)+"%')");
        List<ProductDTO.ProductList> result = null;
        if (!string.IsNullOrEmpty(listOptions))
        {
            string filterQuery = GetFilterQuery(query, listOptions);
            result = DB.Query<ProductDTO.ProductList>(filterQuery).ToList();
        }
        else
        {
            query.AppendLine("  ORDER BY Id DESC ");
            result = DB.Query<ProductDTO.ProductList>(query.ToString()).ToList();
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
        query.AppendLine(@" SELECT Id, Code, ISNULL(NameEn, '') NameEn, ISNULL(NameAr, '') NameAr
                                , ISNULL(DescEn, '') DescEn, ISNULL(DescAr, '') DescAr
                                , PaymentLinkKey, ProductLinkKey, CategoryId
                                , Quantity, Price, Stockable, StockableShow 
                            FROM Product
                            WHERE ISNULL(DeletedBy, 0) = 0 
                                AND VendorId = " + currentUserId + @"
                                AND Id = " + id);
        var result = DB.Query<ProductDTO.ProductGetById>(query.ToString()).FirstOrDefault();
        if(result != null)
        {
            query.Clear();
            query.AppendLine(@" SELECT Id,'"+AppSettings.Instance.AttachmentUrl+"Product/"+@"'+ [Name] [Name], DisplayName, Notes, EntityFieldName
	                            FROM Attachment
	                            WHERE DeletedBy IS NULL AND InActive = 0 AND EntityType = 'Product' AND EntityId = " + id);
            var attachments = DB.Query<AttachmentDTOLight>(query.ToString()).ToList();
            result.Attachments = attachments;
        }
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = result };
    }
    public ResponseDTO AutoComplete(long currentUserId, string currentUserType, string name)
    {
        if (currentUserType == UserType.Vendor.ToString() && currentUserType == UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" SELECT p.Id, p.Code, ISNULL(p.NameEn, '') NameEn, ISNULL(p.NameAr, '') NameAr
                                , p.Quantity, p.Price, p.Stockable
                                , ISNULL(c.NameEn, '') CategoryNameEn, ISNULL(c.NameAr, '') CategoryNameAr
                            FROM Product p 
                                INNER JOIN ProductCategory c ON c.Id = p.CategoryId
                            WHERE ISNULL(p.DeletedBy, 0) = 0 AND p.InActive = 0 
                                AND ISNULL(c.DeletedBy, 0) = 0  AND c.InActive = 0 
                                AND p.VendorId = " + currentUserId + @"
                                AND c.VendorId = " + currentUserId);
        if (!string.IsNullOrEmpty(name)) query.AppendLine(" AND (p.NameEn LIKE N'%" + Utility.Wrap(name) + "%' OR p.NameAr LIKE N'%" + Utility.Wrap(name) + "%') ");
        var result = DB.Query<ProductDTO.ProductAutoComplete>(query.ToString()).FirstOrDefault();
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = result };
    }
    public ResponseDTO Insert(long currentUserId, string currentUserType, ProductDTO.ProductInsert entity)
    {
        if (currentUserType == UserType.Vendor.ToString() && currentUserType == UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };
        if (entity.CategoryId<=0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "CategoryIdRequired", Response = null };
        if (string.IsNullOrEmpty(entity.NameEn))
            return new ResponseDTO() { IsValid = false, ErrorKey = "NameEnRequired", Response = null };

        if (entity.Attachments != null && entity.Attachments.Count > 0)
            foreach (var obj in entity.Attachments)
            {
                string fileError = Utility.ValidateFileUpload(obj.Attachment);
                if (!string.IsNullOrEmpty(fileError))
                    return new ResponseDTO() { IsValid = false, ErrorKey = fileError, Response = null };
            }

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" DECLARE @CurrentDate DATETIME = (SELECT GETDATE())
                            DECLARE @CurrentUserId BIGINT = " + currentUserId + @"
                            DECLARE @RowId BIGINT = 0;
                            BEGIN TRANSACTION [ProductInsert]
                            BEGIN TRY
                                INSERT INTO Product (VendorId, CategoryId
                                    , NameEn, NameAr
                                    , DescEn, DescAr
                                    , Quantity, Price
                                    , Stockable, StockableShow
                                    , InActive, CreatedBy, CreateDate, UpdateDate)
                                VALUES (" + currentUserId + @", " + entity.CategoryId + @"
                                    , N'" + Utility.Wrap(entity.NameEn) + "', N'" + Utility.Wrap(entity.NameAr) + @"'
                                    , N'" + Utility.Wrap(entity.DescEn) + "', N'" + Utility.Wrap(entity.DescAr) + @"'
                                    , " + entity.Quantity + @", " + entity.Price + @"
                                    , '" + entity.Stockable + @"', '" + entity.StockableShow + @"'
                                    , 0, @CurrentUserId, @CurrentDate, @CurrentDate );
                                SELECT @RowId = SCOPE_IDENTITY();
                                EXECUTE dbo.GenerateEntityCode 'Product', @RowId;");
        if (entity.Attachments != null && entity.Attachments.Count > 0)
        {
            query.AppendLine("IF @RowId > 0 ");
            query.AppendLine("  BEGIN");
            foreach (var obj in entity.Attachments)
            {
                query.AppendLine(@" INSERT INTO Attachment (EntityType, EntityId, EntityFieldName
                                            , DisplayName, Notes
                                            , InActive, CreatedBy, CreateDate, UpdateDate)  
                                        VALUES ('Product', @RowId, N'" + Utility.Wrap(obj.EntityFieldName) + @"'
                                            , N'" + Utility.Wrap(obj.DisplayName) + @"', N'" + Utility.Wrap(obj.Notes) + @"'
                                            , 0, @CurrentUserId, @CurrentDate, @CurrentDate);");
            }
            query.AppendLine("  END");
        }
        query.AppendLine(@"
                            COMMIT TRANSACTION [ProductInsert] 
                            SELECT @RowId
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [ProductInsert]
                            END CATCH");
        var id = DB.ExecuteScalar(query.ToString());
        string attachment_sql = "";
        if (entity.Attachments != null && entity.Attachments.Count > 0)
        {
            foreach (var obj in entity.Attachments)
            {
                if (!string.IsNullOrEmpty(obj.Attachment))
                {
                    string newFileName = UploadFile(id, "Product", obj);
                    attachment_sql += " UPDATE Attachment SET [Name] = '" + newFileName + @"' 
                                                WHERE EntityType = 'Product' 
                                                    AND [EntityId] = " + id + @" 
                                                    AND ISNULL(EntityFieldName, '') = '" + obj.EntityFieldName + @"'
                                                    AND DisplayName = '" + obj.DisplayName + @"'
                                                    AND InActive = 0
                                                    AND CreatedBy =" + currentUserId + @"
                                ";
                }
            }
        }
        if (!string.IsNullOrEmpty(attachment_sql))
            DB.Execute(attachment_sql);
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = id };
    }
    public ResponseDTO Update(long currentUserId, string currentUserType, ProductDTO.ProductUpdate entity)
    {
        if (currentUserType == UserType.Vendor.ToString() && currentUserType == UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };
        if (entity.Id <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "IdRequired", Response = null };
        if (entity.CategoryId <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "CategoryIdRequired", Response = null };
        if (string.IsNullOrEmpty(entity.NameEn))
            return new ResponseDTO() { IsValid = false, ErrorKey = "NameEnRequired", Response = null };

        foreach (var obj in entity.Attachments)
        {
            if (obj.Id <= 0)
            {
                string fileError = Utility.ValidateFileUpload(obj.Attachment);
                if (!string.IsNullOrEmpty(fileError))
                    return new ResponseDTO() { IsValid = false, ErrorKey = fileError, Response = null };
            }
        }

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" 
                            DECLARE @CurrentDate DATETIME = (SELECT GETDATE())
                            DECLARE @CurrentUserId BIGINT = " + currentUserId + @"
                            DECLARE @RowId BIGINT = " + entity.Id + @"; 
                            DECLARE @Error NVARCHAR(MAX)='';
                            BEGIN TRANSACTION [ProductUpdate]
                            BEGIN TRY
                                IF NOT EXISTS (SELECT Id FROM Product WHERE VendorId = " + currentUserId + @" AND Id = @RowId)
                                BEGIN RAISERROR(N'ProductNotExist', 11, 1); END");

        query.AppendLine(@"     UPDATE Product SET UpdateDate = @CurrentDate
                                    , Stockable = '" + entity.Stockable + @"', StockableShow = '" + entity.StockableShow + @"'");
        if (entity.CategoryId > 0) query.AppendLine("   , CategoryId = " + entity.CategoryId + "");
        if (!string.IsNullOrEmpty(entity.NameEn)) query.AppendLine("   , NameEn = N'" + Utility.Wrap(entity.NameEn) + "'");
        if (!string.IsNullOrEmpty(entity.NameAr)) query.AppendLine("   , NameAr = N'" + Utility.Wrap(entity.NameAr) + "'");
        if (!string.IsNullOrEmpty(entity.DescEn)) query.AppendLine("   , DescEn = N'" + Utility.Wrap(entity.DescEn) + "'");
        if (!string.IsNullOrEmpty(entity.DescAr)) query.AppendLine("   , DescAr = N'" + Utility.Wrap(entity.DescAr) + "'");
        if (entity.Quantity > 0) query.AppendLine("   , Quantity = " + entity.Quantity + "");
        if (entity.Price > 0) query.AppendLine("   , Price = " + entity.Price + "");

        query.AppendLine(@"     WHERE Id = @RowId");
        if (entity.Attachments != null && entity.Attachments.Count > 0)
        {
            foreach (var obj in entity.Attachments)
            {
                if (obj.Id <= 0)
                {
                    query.AppendLine(@" INSERT INTO Attachment (EntityType, EntityId, EntityFieldName
                                            , DisplayName, Notes
                                            , InActive, CreatedBy, CreateDate, UpdateDate)  
                                        VALUES ('Product', @RowId, N'" + Utility.Wrap(obj.EntityFieldName) + @"'
                                            , N'" + Utility.Wrap(obj.DisplayName) + @"', N'" + Utility.Wrap(obj.Notes) + @"'
                                            , 0, @CurrentUserId, @CurrentDate, @CurrentDate);");
                }
            }
        }
        query.AppendLine(@"
                                COMMIT TRANSACTION [ProductUpdate] 
                                SELECT @RowId
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [ProductUpdate]
                                SELECT ERROR_MESSAGE();
                            END CATCH");
        var result = DB.ExecuteScalar(query.ToString());
        try
        {
            result = Convert.ToInt64(result);
            string attachment_sql = "";
            if (entity.Attachments != null && entity.Attachments.Count > 0)
            {
                foreach (var obj in entity.Attachments)
                {
                    if (obj.Id <= 0)
                    {
                        if (!string.IsNullOrEmpty(obj.Attachment))
                        {
                            string newFileName = UploadFile(result, "Product", obj);
                            attachment_sql += " UPDATE Attachment SET [Name] = '" + newFileName + @"' 
                                                WHERE EntityType = 'Product' 
                                                    AND [EntityId] = " + result + @" 
                                                    AND ISNULL(EntityFieldName, '') = '" + obj.EntityFieldName + @"'
                                                    AND DisplayName = '" + obj.DisplayName + @"'
                                                    AND InActive = 0
                                                    AND CreatedBy =" + currentUserId + @"
                            ";
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(attachment_sql))
                DB.Execute(attachment_sql);
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
                            IF NOT EXISTS (SELECT Id FROM Product WHERE VendorId = " + currentUserId + @" AND Id = " + id + @")
                            BEGIN SET @Error = N'ProductNotExist'; END
                            IF @Error = ''
                            BEGIN
                                Update Product SET InActive = 1, DeletedBy = " + currentUserId + @", DeleteDate = GETDATE() 
                                WHERE VendorId = " + currentUserId + " AND Id = " + id + @"
                            END
                            SELECT @Error; ");
        var result = DB.ExecuteScalar(query.ToString());
        if (result != null && result.ToString().Length > 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = result.ToString(), Response = null };
        else
            return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = null };
    }
}