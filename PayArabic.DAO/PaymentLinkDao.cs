using System.Text;

namespace PayArabic.DAO;

public class PaymentLinkDao : BaseDao, IPaymentLinkDao
{
    public ResponseDTO GetAll(long currentUserId, string currentUserType, string title, string listOptions = null) 
    {
        if (currentUserType != UserType.Vendor.ToString() && currentUserType != UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };

        StringBuilder query = new StringBuilder();
        query.AppendLine(@"   SELECT Id, [Key], ISNULL(Code, Id) Code, ISNULL(Title, '') Title, Lang
                                , ISNULL(Amount, 0) Amount, IsOpenAmount
                                , ISNULL(MinAmount, 0) MinAmount, ISNULL(MaxAmount, 0) MaxAmount
                                , CreateDate, InActive
                            FROM PaymentLink
                            WHERE ISNULL(DeletedBy, 0) = 0 
                                AND VendorId = " + currentUserId + @"");
        if (!string.IsNullOrEmpty(title)) query.AppendLine(" AND Title LIKE N'%" + Utility.Wrap(title) + "%'");

        List<PaymentLinkDTO.PaymentLinkList> result = null;
        if (!string.IsNullOrEmpty(listOptions))
        {
            string filterQuery = GetFilterQuery(query, listOptions);
            result = DB.Query<PaymentLinkDTO.PaymentLinkList>(filterQuery).ToList();
        }
        else
        {
            query.AppendLine("  ORDER BY Id DESC ");
            result = DB.Query<PaymentLinkDTO.PaymentLinkList>(query.ToString()).ToList();
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
        query.AppendLine(@" SELECT Id, [Key], ISNULL(Code, Id) Code, ISNULL(Title, '') Title
                                , ISNULL(Lang, 'ar') Lang, ISNULL(Amount, 0) Amount                            
                                , ISNULL(IsOpenAmount, 0) IsOpenAmount
                                , ISNULL(MinAmount, 0) MinAmount
                                , ISNULL(MaxAmount, 0) MaxAmount
                                , ISNULL(Currency, 'KWD') Currency
                                , ISNULL(Comment, '') Comment
                                , ISNULL(TermsConditionEnabled, 0) TermsConditionEnabled
                                , ISNULL(TermsCondition, '') TermsCondition
                                , CreateDate, InActive
                            FROM PaymentLink
                            WHERE ISNULL(DeletedBy, 0) = 0 
                                AND VendorId = " + currentUserId + @"
                                AND Id = " + id);


        var result = DB.Query<PaymentLinkDTO.PaymentLinkGetById>(query.ToString()).FirstOrDefault();
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = result };
    }
    public ResponseDTO Insert(long currentUserId, string currentUserType, long vendorId, PaymentLinkDTO.PaymentLinkInsert entity)
    {
        if (currentUserType != UserType.Vendor.ToString() && currentUserType != UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null }; 
        if (string.IsNullOrEmpty(entity.Title))
            return new ResponseDTO() { IsValid = false, ErrorKey = "TitleRequired", Response = null }; 

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" DECLARE @RowId BIGINT = 0;
                            BEGIN TRANSACTION [PaymentLinkInsert]
                            BEGIN TRY
                                INSERT INTO PaymentLink (VendorId, Title, Amount, Currency
                                    , Lang, IsOpenAmount
                                    , MinAmount, MaxAmount, Comment
                                    , TermsConditionEnabled, TermsCondition
                                    , InActive, CreatedBy, CreateDate, UpdateDate)
                                VALUES (" + vendorId + @", N'" + Utility.Wrap(entity.Title) + "', " + entity.Amount + @", N'" + Utility.Wrap(entity.Currency) + @"'
                                    , N'" + Utility.Wrap(entity.Lang) + @"', '" + entity.IsOpenAmount + @"'
                                    , " + entity.MinAmount + ", " + entity.MaxAmount + ", N'" + Utility.Wrap(entity.Comment) + @"'
                                    , '" + entity.TermsConditionEnabled + @"', N'" + Utility.Wrap(entity.TermsCondition) + @"'
                                    , 0, " + currentUserId + @", GETDATE(), GETDATE() );
                                SELECT @RowId = SCOPE_IDENTITY();
                                EXECUTE dbo.GenerateEntityCode 'PaymentLink', @RowId;                                
                            COMMIT TRANSACTION [PaymentLinkInsert] 
                            SELECT @RowId
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [PaymentLinkInsert]
                            END CATCH");
        var id = DB.ExecuteScalar(query.ToString());
        //generate key    
        string key = Utility.GetRandomInvoiceKey(id);
        DB.Execute("UPDATE [PaymentLink] SET [Key] = " + key + " WHERE Id = " + id);
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = key };
    }
    public ResponseDTO Update(long currentUserId, string currentUserType, long vendorId, PaymentLinkDTO.PaymentLinkUpdate entity)
    {
        if (currentUserType != UserType.Vendor.ToString() && currentUserType != UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };
        if (entity.Id <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "IdRequired", Response = null }; 
        if (string.IsNullOrEmpty(entity.Title))
            return new ResponseDTO() { IsValid = false, ErrorKey = "TitleRequired", Response = null }; 

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" DECLARE @RowId BIGINT = " + entity.Id + @"; 
                            DECLARE @Error NVARCHAR(MAX)='';
                            BEGIN TRANSACTION [PaymentLinkUpdate]
                            BEGIN TRY");
        query.AppendLine(@" IF NOT EXISTS (SELECT Id FROM PaymentLink WHERE VendorId = " + vendorId + " AND Id = " + entity.Id + @")
                             BEGIN RAISERROR(N'PaymentLinkNotExist', 11, 1); END");

        query.AppendLine(@"     UPDATE PaymentLink SET UpdateDate = GETDATE(), InActive = '" + entity.InActive + @"'
                                    , [IsOpenAmount] = '" + entity.IsOpenAmount + @"'
                                    , [TermsConditionEnabled] = '" + entity.TermsConditionEnabled + @"'");
        if (!string.IsNullOrEmpty(entity.Title)) query.AppendLine("   , [Title] = N'" + Utility.Wrap(entity.Title) + "'");
        if (entity.Amount > 0) query.AppendLine("   , [Amount] = " + entity.Amount + "");
        if (!string.IsNullOrEmpty(entity.Currency)) query.AppendLine("   , [Currency] = N'" + Utility.Wrap(entity.Currency) + "'");
        if (!string.IsNullOrEmpty(entity.Lang)) query.AppendLine("   , [Lang] = N'" + Utility.Wrap(entity.Lang) + "'");
        if (entity.MinAmount > 0) query.AppendLine("   , [MinAmount] = " + entity.MinAmount + "");
        if (entity.MaxAmount > 0) query.AppendLine("   , [MaxAmount] = " + entity.MaxAmount + "");
        if (!string.IsNullOrEmpty(entity.Comment)) query.AppendLine("   , [Comment] = N'" + Utility.Wrap(entity.Comment) + "'");
        if (!string.IsNullOrEmpty(entity.TermsCondition)) query.AppendLine("   , [TermsCondition] = N'" + Utility.Wrap(entity.TermsCondition) + "'");
        query.AppendLine(@"     WHERE Id = @RowId
                                COMMIT TRANSACTION [PaymentLinkUpdate] 
                                SELECT @RowId
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [PaymentLinkUpdate]
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
    public ResponseDTO GetForPayment(long key)
    {
        if (key <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "KeyRequired", Response = null }; 
        StringBuilder query = new StringBuilder();
        query.AppendLine(@" SELECT p.[Key], p.Code, ISNULL(p.Title, '') Title
                                , CASE 
                                    WHEN ISNULL(p.IsOpenAmount, 0) = 0 THEN ISNULL(p.Amount, 0) 
                                    ELSE ISNULL(p.MaxAmount, 0)
                                  END Amount
                                , ISNULL(p.IsOpenAmount, 0) IsOpenAmount
                                , ISNULL(p.MinAmount, 0) MinAmount
                                , ISNULL(p.MaxAmount, 0) MaxAmount
                                , TermsConditionEnabled, TermsCondition
                                , v.CompanyName AS VendorCompany
                                , ISNULL(a.DisplayName, '') AS VendorLogo
                                , ISNULL(a.[Name], '') AS VendorLogoPath
                            FROM PaymentLink p
                                INNER JOIN Vendor v ON v.UserId = p.VendorId
                                LEFT JOIN Attachment a ON a.EntityId = p.VendorId
                                    AND a.EntityType = 'Vendor'
                                    AND a.EntityFieldName = 'Logo'
                            WHERE ISNULL(p.DeletedBy, 0) = 0 AND p.InActive = 0
                                    AND p.[Key] = " + key);
        PaymentLinkDTO.PaymentLinkGetForPayment result = DB.Query<PaymentLinkDTO.PaymentLinkGetForPayment>(query.ToString()).FirstOrDefault();
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = result };
    }
    public ResponseDTO Delete(long currentUserId, string currentUserType, long vendorId, long id)
    {
        if (currentUserType != UserType.Vendor.ToString() && currentUserType != UserType.User.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null }; 
        if (id <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "IdRequired", Response = null };
        
        StringBuilder query = new StringBuilder();
        query.AppendLine(@" DECLARE @Error NVARCHAR(MAX) = '';
                            IF NOT EXISTS (SELECT Id FROM PaymentLink WHERE VendorId = " + currentUserId + @" AND Id = " + id + @")
                            BEGIN SET @Error = N'PaymentLinkNotExist'; END
                            IF @Error = ''
                            BEGIN
                                Update PaymentLink SET InActive = 1, DeletedBy = " + currentUserId + @", DeleteDate = GETDATE() 
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