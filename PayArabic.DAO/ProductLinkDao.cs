using Microsoft.AspNetCore.DataProtection.KeyManagement;
using PayArabic.Core.Model;
using System.Text;
using static PayArabic.Core.DTO.DepositDTO;
using static PayArabic.Core.DTO.ProductDTO;

namespace PayArabic.DAO;

public class ProductLinkDao : BaseDao, IProductLinkDao
{
    public ResponseDTO GetAll(long currentUserId, string currentUserType, string name, string listOptions = null)
    {
        if (currentUserType != UserType.Vendor.ToString() && currentUserType != UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" SELECT Id, [Key], Code, ISNULL(NameEn, '') NameEn, ISNULL(NameAr, '') NameAr, InActive
                            FROM ProductLink
                            WHERE ISNULL(DeletedBy, 0) = 0 
                                AND VendorId = " + currentUserId + @"");
        if (!string.IsNullOrEmpty(name)) query.AppendLine(" AND (NameEn LIKE N'%" + Utility.Wrap(name) + "%' OR NameAr LIKE N'%" + Utility.Wrap(name) + "%' )");

        List<ProductLinkDTO.ProductLinkList> result = null;
        if (!string.IsNullOrEmpty(listOptions))
        {
            string filterQuery = GetFilterQuery(query, listOptions);
            result = DB.Query<ProductLinkDTO.ProductLinkList>(filterQuery).ToList();
        }
        else
        {
            query.AppendLine("  ORDER BY Id DESC ");
            result = DB.Query<ProductLinkDTO.ProductLinkList>(query.ToString()).ToList();
        }
        if (result == null || result.Count() <= 0)
            return new ResponseDTO { IsValid = false, ErrorKey = "EmptyResult" };
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = result };
    }
    public ResponseDTO GetById(long currentUserId, string currentUserType, long id)
    {
        if (currentUserType != UserType.Vendor.ToString() && currentUserType != UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };
        if (id <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "IdRequired", Response = null };

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" SELECT Id, [Key], Code, ISNULL(NameEn, '') NameEn, ISNULL(NameAr, '') NameAr
                                , ISNULL(TermsConditionEnabled, 0) TermsConditionEnabled
                                , ISNULL(TermsCondition, '') TermsCondition
                                , InActive
                            FROM ProductLink
                            WHERE ISNULL(DeletedBy, 0) = 0 
                                AND VendorId = " + currentUserId + @"
                                AND Id = " + id);

        var result = DB.Query<ProductLinkDTO.ProductLinkGetById>(query.ToString()).FirstOrDefault();
        if (result != null)
        {
            var categories = GetCategory(currentUserId, currentUserType, result.Key);
            if (categories != null && categories.IsValid)
                result.Categories = categories.Response;
        }
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = result };
    }
    public ResponseDTO GetCategory(long currentUserId, string currentUserType, long productLinkkey = 0, bool isPayment = false, long vendorId = 0)
    {
        if (!isPayment && currentUserType != UserType.Vendor.ToString() && currentUserType != UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };

        string vars = @"    DECLARE @VendorId BIGINT = " + vendorId + @";
                            DECLARE @CurrentUserId BIGINT = " + currentUserId + @";
                            DECLARE @Key BIGINT = " + productLinkkey + @";
                            DECLARE @IsPayment BIT = '" + isPayment + @"';";
        StringBuilder query = new StringBuilder();
        query.AppendLine(vars);
        query.AppendLine(@" SELECT Id, ISNULL(NameEn, '') NameEn, ISNULL(NameAr, '') NameAr    
                            FROM ProductCategory 
                            WHERE ISNULL(DeletedBy, 0) = 0 AND InActive = 0
                                AND VendorId = CASE 
				                                WHEN @CurrentUserId > 0 THEN @CurrentUserId
				                                ELSE @VendorId
			                                   END");
        var categories = DB.Query<ProductLinkDTO.ProductLinkCategory>(query.ToString()).ToList();
        if (categories != null && categories.Count > 0)
        {
            query.Clear();
            query.AppendLine(vars);
            query.AppendLine(@" SELECT DISTINCT ISNULL(p.Id, 0) Id, ISNULL(p.Id, 0) CategoryId
                                    , ISNULL(p.NameEn, '') NameEn, ISNULL(p.NameAr, '') NameAr 
                                    , CASE 
                                        WHEN @Key > 0 THEN ISNULL(pl.Id, 0) 
                                        ELSE 0
                                     END ProductLinkId
                                    , ISNULL(p.DescEn, '') DescEn, ISNULL(p.DescAr, '') DescAr
                                    , p.Quantity, p.Price, p.Stockable, p.StockableShow
                                    --, pro.Image
                                FROM Product p
                                    ");
            if (!isPayment)
                query.AppendLine(@" LEFT JOIN ProductLinkDetail pld ON p.Id = pld.ProductId
	                                LEFT JOIN ProductLink pl ON pl.Id = pld.ProductLinkId 
                                        AND pl.[Key] = @Key");
            else 
                query.AppendLine(@" INNER JOIN ProductLinkDetail pld ON p.Id = pld.ProductId
                                    INNER JOIN ProductLink pl ON pl.Id = pld.ProductLinkId
                                    WHERE pl.[Key] = @Key
                                        AND (
                                            (p.Stockable = 1 AND (p.Quantity > 0 OR p.StockableShow = 1) )
                                            OR
                                            p.Stockable = 0
                                        )");
            query.AppendLine("          AND ISNULL(p.DeletedBy, 0) = 0 AND p.InActive = 0");
            var products = DB.Query<ProductLinkDTO.ProductLinkProduct>(query.ToString()).ToList();
            foreach (var cat in categories)
            {
                cat.Products = new List<ProductLinkDTO.ProductLinkProduct>();

                var pros = products.Where(x => x.CategoryId == cat.Id).ToList();
                foreach (var pr in pros)
                {
                    query.Clear();
                    query.AppendLine(@"  SELECT Id,'"+AppSettings.Instance.AttachmentUrl+"Product/"+@"'+ [Name] [Name], DisplayName, Notes, EntityFieldName
	                                    FROM Attachment
	                                    WHERE DeletedBy IS NULL AND InActive = 0 AND EntityType = 'Product' AND EntityId = " + pr.Id);
                    var attachments = DB.Query<AttachmentDTOLight>(query.ToString()).ToList();
                    pr.Attachments = attachments;

                    cat.Products.Add(pr);
                }
            }
        }
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = categories };
    }
    public ResponseDTO GetForPayment(long key)
    {
        if (key <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "KeyRequired", Response = null };
        StringBuilder query = new StringBuilder();
        query.AppendLine(@" SELECT p.[Key], p.Code, ISNULL(p.NameEn, '') NameEn, ISNULL(p.NameAr, '') NameAr
                                , p.TermsConditionEnabled, p.TermsCondition
                                , p.VendorId, v.CompanyName AS VendorCompany
                                , ISNULL(a.DisplayName, '') AS VendorLogo
                                , ISNULL(a.[Name], '') AS VendorLogoPath
                            FROM ProductLink p
                                INNER JOIN Vendor v ON v.UserId = p.VendorId
                                LEFT JOIN Attachment a ON a.EntityId = p.VendorId
                                    AND a.EntityType = 'Vendor'
                                    AND a.EntityFieldName = 'Logo'
                            WHERE ISNULL(p.DeletedBy, 0) = 0 AND p.InActive = 0
                                    AND p.[Key] = " + key);
        ProductLinkDTO.ProductLinkGetForPayment result = DB.Query<ProductLinkDTO.ProductLinkGetForPayment>(query.ToString()).FirstOrDefault();
        if (result != null)
        {
            var categories = GetCategory(0, "", key, true, result.VendorId);
            if (categories != null && categories.IsValid)
                result.Categories = categories.Response;
        }
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = result };
    }
    public ResponseDTO Insert(long currentUserId, string currentUserType, ProductLinkDTO.ProductLinkInsert entity)
    {
        if (currentUserType != UserType.Vendor.ToString() && currentUserType != UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };
        if (string.IsNullOrEmpty(entity.NameEn))
            return new ResponseDTO() { IsValid = false, ErrorKey = "NameEnRequired", Response = null };
        if (string.IsNullOrEmpty(entity.NameAr))
            return new ResponseDTO() { IsValid = false, ErrorKey = "NameArRequired", Response = null };

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" DECLARE @RowId BIGINT = 0;
                            BEGIN TRANSACTION [ProductLinkInsert]
                            BEGIN TRY
                                INSERT INTO ProductLink (VendorId, NameEn, NameAr
                                    , TermsConditionEnabled, TermsCondition
                                    , InActive, CreatedBy, CreateDate, UpdateDate)
                                VALUES (" + currentUserId + @", N'" + Utility.Wrap(entity.NameEn) + @"', N'" + Utility.Wrap(entity.NameAr) + @"'
                                    , '" + entity.TermsConditionEnabled + @"', N'" + Utility.Wrap(entity.TermsCondition) + @"'
                                    , 0, " + currentUserId + @", GETDATE(), GETDATE() );
                                SELECT @RowId = SCOPE_IDENTITY();
                                EXECUTE dbo.GenerateEntityCode 'ProductLink', @RowId; ");
        if (entity.Products != null && entity.Products.Count > 0)
            entity.Products.ForEach(p => query.AppendLine(@"
                                                            INSERT INTO ProductLinkDetail (ProductLinkId, ProductId, InActive, CreatedBy, CreateDate, UpdateDate)
                                                            VALUES (@RowId, " + p + ", 0, " + currentUserId + @", GETDATE(), GETDATE()) 
                                                            "));
        query.AppendLine(@"
                            COMMIT TRANSACTION [ProductLinkInsert] 
                            SELECT @RowId
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [ProductLinkInsert]
                            END CATCH");
        var id = DB.ExecuteScalar(query.ToString());
        //generate key    
        string key = Utility.GetRandomInvoiceKey(id);
        DB.Execute("UPDATE ProductLink SET [Key] = " + key + " WHERE Id = " + id);
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = key };
    }
    public ResponseDTO Update(long currentUserId, string currentUserType, ProductLinkDTO.ProductLinkUpdate entity)
    {
        if (currentUserType != UserType.Vendor.ToString() && currentUserType != UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };
        if (entity.Id <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "IdRequired", Response = null };
        if (string.IsNullOrEmpty(entity.NameEn))
            return new ResponseDTO() { IsValid = false, ErrorKey = "NameErRequired", Response = null };

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" DECLARE @RowId BIGINT = " + entity.Id + @"; 
                            DECLARE @Error NVARCHAR(MAX)='';
                            BEGIN TRANSACTION [ProductLinkUpdate]
                            BEGIN TRY");
        query.AppendLine(@" IF NOT EXISTS (SELECT Id FROM ProductLink WHERE VendorId = " + currentUserId + @" AND Id = @RowId)
                             BEGIN RAISERROR(N'ProductLinkNotExist', 11, 1); END");

        query.AppendLine(@"     UPDATE ProductLink SET UpdateDate = GETDATE(), InActive = '" + entity.InActive + @"'
                                    , [TermsConditionEnabled] = '" + entity.TermsConditionEnabled + @"'");
        if (!string.IsNullOrEmpty(entity.NameEn)) query.AppendLine("   , NameEn = N'" + Utility.Wrap(entity.NameEn) + "'");
        if (!string.IsNullOrEmpty(entity.NameAr)) query.AppendLine("   , NameAr = N'" + Utility.Wrap(entity.NameAr) + "'");
        if (!string.IsNullOrEmpty(entity.TermsCondition)) query.AppendLine("   , [TermsCondition] = N'" + Utility.Wrap(entity.TermsCondition) + "'");
        query.AppendLine(@"     WHERE Id = @RowId");
        if (entity.Products != null && entity.Products.Count > 0)
        {
            query.AppendLine("DELETE FROM ProductLinkDetail WHERE ProductLinkId = @RowId; ");
            entity.Products.ForEach(p => query.AppendLine(@"
                                                            INSERT INTO ProductLinkDetail (ProductLinkId, ProductId, InActive, CreatedBy, CreateDate, UpdateDate)
                                                            VALUES (@RowId, " + p + ", 0, " + currentUserId + @", GETDATE(), GETDATE()) 
                                                            "));
        }
        query.AppendLine(@"
                                COMMIT TRANSACTION [ProductLinkUpdate] 
                                SELECT @RowId
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [ProductLinkUpdate]
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
        if (currentUserType != UserType.Vendor.ToString() && currentUserType != UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };
        if (id <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "IdRequired", Response = null };

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" DECLARE @Error NVARCHAR(MAX) = '';
                            IF NOT EXISTS (SELECT Id FROM ProductLink WHERE VendorId = " + currentUserId + @" AND Id = " + id + @")
                            BEGIN SET @Error = N'ProductLinkNotExist'; END
                            IF @Error = ''
                            BEGIN
                                Update ProductLink SET InActive = 1, DeletedBy = " + currentUserId + @", DeleteDate = GETDATE() 
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