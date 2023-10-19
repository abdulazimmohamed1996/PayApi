using System.Text;
using static Dapper.SqlMapper;

namespace PayArabic.DAO;

public class InvoiceDao : BaseDao, IInvoiceDao
{
    public ResponseDTO GetAll(string currentUserType,
        long currentUserId,
        long vendorId,
        string code,
        string refNumber,
        string status,
        string createDateFrom,
        string createDateTo,
        string expiryDateFrom,
        string expiryDateTo,
        float amountFrom,
        float amountTo,
        string customerName,
        string customerMobile,
        string customerEmail,
        string paymentMethod = "", 
        string invoiceType = "Invoice",
        string listOptions = null)
    {
        if (!string.IsNullOrEmpty(createDateFrom) && !Utility.ValidateDate(createDateFrom))
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidDateFormat", Response = null };
        if (!string.IsNullOrEmpty(createDateTo) && !Utility.ValidateDate(createDateTo))
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidDateFormat", Response = null };
        if (!string.IsNullOrEmpty(expiryDateFrom) && !Utility.ValidateDate(expiryDateFrom))
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidDateFormat", Response = null };
        if (!string.IsNullOrEmpty(expiryDateTo) && !Utility.ValidateDate(expiryDateTo))
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidDateFormat", Response = null };

        if (!string.IsNullOrEmpty(status)
            && status != InvoiceStatus.Unpaid.ToString()
            && status != InvoiceStatus.Paid.ToString()
            && status != InvoiceStatus.Canceled.ToString()
            && status != InvoiceStatus.Deposited.ToString()
            && status != InvoiceStatus.Refunded.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidInvoiceStatus", Response = null };

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" SELECT inv.Id, inv.VendorId, inv.[Key], inv.Code, inv.[Status], inv.Amount, inv.Total, inv.Subtotal
						        , inv.CustomerName, inv.CustomerMobile, inv.CustomerEmail, inv.RefNumber
						        , ISNULL(inv.CurrencyCode, '" + AppSettings.Instance.DefaultCurrency + @"') CurrencyCode
                                , CONVERT(DATE, ISNULL(inv.[ExpiryDate], GETDATE()), 102) ExpiryDate
                                , inv.ViewsNo, u.Name VendorName, inv.Fees, inv.PaymentDate
                                , inv.CreateDate
					        FROM Invoice inv
                            INNER JOIN [User] u ON inv.vendorId = u.Id");
        //var depositJoin = (!string.IsNullOrEmpty(deposit_Date_From) || !string.IsNullOrEmpty(deposit_Date_To) || !string.IsNullOrEmpty(depositCode)) ? "INNER" : "LEFT";
        var depositJoin = "LEFT";

        query.AppendLine(depositJoin + " JOIN VendorDeposit vd ON vd.Id = inv.VendorDepositId ");
        query.AppendLine(depositJoin + " JOIN Deposit d ON vd.DepositId = d.Id AND d.[Status] ='Completed'");
        query.AppendLine(@" LEFT JOIN [Transaction] t ON t.InvoiceId = inv.Id AND t.[Status] = 'Success' AND t.RequestType != '" + TransactionRequestType.Refund + "'");

        query.AppendLine(@" WHERE inv.InActive = 0 ");
        query.AppendLine("  AND inv.[Type] = '" + invoiceType + "'");
        if (!string.IsNullOrEmpty(paymentMethod))
            query.AppendLine(" AND t.PaymentGatewayCode = '" + Utility.Wrap(paymentMethod) + "'");

        if (vendorId > 0) query.AppendLine("  AND inv.VendorId = " + vendorId);
        if (!string.IsNullOrEmpty(code)) query.AppendLine("  AND inv.Code LIKE '%" + Utility.Wrap(code) + "%'");
        if (!string.IsNullOrEmpty(refNumber)) query.AppendLine("  AND inv.RefNumber LIKE N'%" + Utility.Wrap(refNumber) + "%'");
        if (!string.IsNullOrEmpty(status)) query.AppendLine("  AND inv.[Status] = '" + Utility.Wrap(status) + "'");
        if (!string.IsNullOrEmpty(customerName)) query.AppendLine("  AND inv.CustomerName LIKE N'%" + Utility.Wrap(customerName) + "%'");
        if (!string.IsNullOrEmpty(customerMobile)) query.AppendLine("  AND inv.CustomerMobile LIKE '%" + Utility.Wrap(customerMobile) + "%'");
        if (!string.IsNullOrEmpty(customerEmail)) query.AppendLine("  AND inv.CustomerEmail LIKE '%" + Utility.Wrap(customerEmail) + "%'");

        if (!string.IsNullOrEmpty(createDateFrom) && string.IsNullOrEmpty(createDateTo)) query.AppendLine("  AND CONVERT(DATE, inv.[CreateDate], 102) BETWEEN CONVERT(DATE, '" + createDateFrom + "', 102) AND CONVERT(DATE, '" + createDateFrom + "', 102)");
        else if (string.IsNullOrEmpty(createDateFrom) && !string.IsNullOrEmpty(createDateTo)) query.AppendLine("  AND CONVERT(DATE, inv.[CreateDate], 102) BETWEEN CONVERT(DATE, '" + createDateTo + "', 102) AND CONVERT(DATE, '" + createDateTo + "', 102)");
        else if (!string.IsNullOrEmpty(createDateFrom) && !string.IsNullOrEmpty(createDateTo)) query.AppendLine("  AND CONVERT(DATE, inv.[CreateDate], 102) BETWEEN CONVERT(DATE, '" + createDateFrom + "', 102) AND CONVERT(DATE, '" + createDateTo + "', 102)");

        if (amountFrom > 0 && amountTo <= 0) query.AppendLine("   AND inv.Amount >= " + amountFrom);
        else if (amountFrom <= 0 && amountTo > 0) query.AppendLine("   AND inv.Amount <= " + amountTo);
        else if (amountFrom > 0 && amountTo > 0) query.AppendLine("   AND inv.Amount >= " + amountFrom + " AND inv.Amount <= " + amountTo);


        List<InvoiceDTO.InvoiceList> result = null;
        if (!string.IsNullOrEmpty(listOptions))
        {
            string filterQuery = GetFilterQuery(query, listOptions);
            result = DB.Query<InvoiceDTO.InvoiceList>(filterQuery).ToList();
        }
        else
        {
            query.AppendLine("  ORDER BY inv.Id DESC ");
            result = DB.Query<InvoiceDTO.InvoiceList>(query.ToString()).ToList();
        }
        if (result == null || result.Count() <= 0)
            return new ResponseDTO { IsValid = false, ErrorKey = "EmptyResult" };
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = result };
    }

    public ResponseDTO GetById(string currentUserType, long currentUserId, long vendorId, long id)
    {
        string query = @"   DECLARE @Error NVARCHAR(MAX)='';
                            DECLARE @Id BIGINT = " + id + @";
                            DECLARE @CurrentUserId BIGINT = " + currentUserId + @";
                            DECLARE @CurrentVendorId BIGINT = " + vendorId + @";
                            DECLARE @VendorId BIGINT = 0;
                        
                            SELECT @VendorId = ISNULL(inv.VendorId, 0)
	                        FROM Invoice inv INNER JOIN [User] u ON u.Id = inv.VendorId 
                            WHERE inv.Id = @Id;
    
                            IF @VendorId <= 0
		                        BEGIN SET @Error = N'UnauthorizedAccess'; END
	                        ELSE IF @VendorId > 0 AND @VendorId <> " + vendorId + @"
		                        BEGIN SET @Error = N'UnauthorizedAccess'; END

                            SELECT @Error;";
        var error = DB.ExecuteScalar<string>(query);
        if (error != null && !string.IsNullOrEmpty(error))
            return new ResponseDTO() { IsValid = false, ErrorKey = error, Response = null };

        query = @"   SELECT inv.Id, VendorId, Code, RefNumber, [Key], inv.Status
                            , inv.Amount, inv.AlternateAmount, inv.Total
                            , SendType, Lang, CustomerName, CustomerMobile, CustomerEmail
                            , ISNULL(inv.CurrencyCode, '" + AppSettings.Instance.DefaultCurrency + @"') CurrencyCode
                            , DiscountType, DiscountAmount, CONVERT(DATE, ISNULL([ExpiryDate], GETDATE()), 102) ExpiryDate
                            , RemindAfter
                            , ISNULL(Comment, '') Comment, TermsConditionEnabled, ISNULL(TermsCondition, '') TermsCondition
                            , inv.Type
			            FROM [Invoice] inv
                        WHERE inv.Id = " + id + @" AND inv.InActive = 0 AND DeletedBy IS NULL
		        ";
        InvoiceDTO.InvoiceGetById invoice = DB.Query<InvoiceDTO.InvoiceGetById>(query.ToString()).FirstOrDefault();
        if (invoice != null)
        {
            query = @"  SELECT *
	                    FROM [InvoiceItem]
	                    WHERE DeletedBy IS NULL AND InActive = 0 AND InvoiceId = " + id;
            var items = DB.Query<InvoiceItemDTO>(query.ToString()).ToList();
            invoice.Items = items;

            query = @"  SELECT Id,'"+AppSettings.Instance.AttachmentUrl+"Invoice/"+@"'+ [Name] [Name], DisplayName, Notes, EntityFieldName
	                    FROM Attachment
	                    WHERE DeletedBy IS NULL AND InActive = 0 AND EntityType = 'Invoice' AND EntityId = " + id;
            var attachments = DB.Query<AttachmentDTOLight>(query.ToString()).ToList();
            invoice.Attachments = attachments;
        }


        invoice.URL = AppSettings.Instance.InvoicePublicUrl + invoice.Key;
        invoice.QR = AppSettings.Instance.InvoicePublicUrl.Replace("/pay/", "/qr/") + invoice.Key + "/download";

        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = invoice };
    }
    public ResponseDTO Insert(string currentUserType, long currentUserId, long vendorId, InvoiceDTO.Composite composite)
    {
        foreach (var inv in composite.Invoices)
        {
            string error = ValidateInvoice(inv);
            if (!string.IsNullOrEmpty(error))
                return new ResponseDTO() { IsValid = false, ErrorKey = error, Response = null };

            if (inv.Attachments != null && inv.Attachments.Count > 0)
                foreach (var obj in inv.Attachments)
                {
                    string fileError = Utility.ValidateFileUpload(obj.Attachment);
                    if (!string.IsNullOrEmpty(fileError))
                        return new ResponseDTO() { IsValid = false, ErrorKey = fileError, Response = null };
                }
        }
        
        StringBuilder query = new StringBuilder();
        query.AppendLine(@" DECLARE @CurrentDate DATETIME = (SELECT GETDATE())
                            DECLARE @CurrentUserId BIGINT = " + currentUserId + @";
                            DECLARE @ExchangeRate FLOAT = 0;
                            DECLARE @Amount FLOAT = 0;
                            BEGIN TRANSACTION [InvoiceInsert]
                            BEGIN TRY
                                DECLARE @RowId BIGINT; 
                                DECLARE @Rows NVARCHAR(MAX) = ''; 
                                DECLARE @ExpiryDate NVARCHAR(50) = ''; ");
        foreach (var entity in composite.Invoices)
        {
            if (string.IsNullOrEmpty(entity.CurrencyCode)) entity.CurrencyCode = AppSettings.Instance.DefaultCurrency;
            if (string.IsNullOrEmpty(entity.Lang)) entity.Lang = "ar";
            if (!string.IsNullOrEmpty(entity.CustomerMobile)) entity.CustomerMobile = Utility.toEnglishNumber(entity.CustomerMobile);
            if (string.IsNullOrEmpty(entity.ExpiryDate))
                query.AppendLine(@" SELECT @ExpiryDate = DATEADD(dd, v.InvoiceValidDays, GETDATE()) 
                                    FROM Vendor v INNER JOIN [User] u ON u.Id = v.UserId
                                    WHERE u.UserType='" + UserType.Vendor + "' AND u.Id = @CurrentUserId");
            else
                query.AppendLine(@" SET @ExpiryDate = '" + entity.ExpiryDate + " 23:59:59.999" + "'");
            query.AppendLine(@" SET @RowId = 0; 
                                SET @ExchangeRate = 1;
                                SET @Amount = " + entity.Amount + @";

                                SELECT TOP 1 @ExchangeRate = ISNULL(ConversionRate, 1) 
                                FROM Currency WHERE SymboleEn = '" + entity.CurrencyCode+ @"';
                                
                                SET @Amount = @Amount * @ExchangeRate;
                                
                                INSERT INTO Invoice 
                                    (VendorId, [Type], [Status], SendType
                                    , CurrencyCode, Amount, AlternateAmount, Subtotal, Total
                                    , Lang, ExchangeRate
                                    , CustomerName, CustomerMobile, CustomerEmail
                                    , RefNumber, DiscountType, DiscountAmount
                                    , [ExpiryDate], RemindAfter, Comment
                                    , TermsConditionEnabled, TermsCondition
                                    , InActive, CreatedBy, CreateDate, UpdateDate) 
                                VALUES (" + vendorId + ", '" + InvoiceType.Invoice + @"', 'Unpaid', '" + entity.SendType + @"'
                                    , N'" + entity.CurrencyCode.ToUpper() + @"', @Amount, " + entity.Amount + @", @Amount, @Amount
                                    , N'" + entity.Lang + @"', @ExchangeRate
                                    , N'" + Utility.Wrap(entity.CustomerName) + @"', N'" + Utility.Wrap(entity.CustomerMobile) + @"', N'" + Utility.Wrap(entity.CustomerEmail) + @"'
                                    , N'" + entity.RefNumber + @"', '" + entity.DiscountType + "', " + entity.DiscountAmount + @"
                                    , CONVERT(DATE, @ExpiryDate, 102), " + entity.RemindAfter + @", N'" + Utility.Wrap(entity.Comment) + @"'
                                    , N'" + entity.TermsConditionEnabled + @"', N'" + Utility.Wrap(entity.TermsCondition) + @"'
                                    , 0, @CurrentUserId, @CurrentDate, @CurrentDate );
            
                                SELECT @RowId = SCOPE_IDENTITY();

                                EXECUTE dbo.GenerateEntityCode 'Invoice', @RowId;
                            
                                IF @Rows = '' BEGIN SET @Rows += CAST(@RowId AS VARCHAR); END ELSE BEGIN SET @Rows += ',' + CAST(@RowId AS VARCHAR); END ");
            if (entity.Items != null && entity.Items.Count > 0)
            {
                query.AppendLine("IF @RowId > 0 ");
                query.AppendLine("  BEGIN");
                foreach (var item in entity.Items)
                {
                    query.AppendLine(@" INSERT INTO InvoiceItem (InvoiceId, [Name], Quantity
                                            , Amount, AlternateAmount
                                            , InActive, CreatedBy, CreateDate, UpdateDate) 
                                        VALUES (@RowId, N'" + item.Name + "', " + item.Quantity + @"
                                            , (@ExchangeRate * " + item.Amount + @"), " + item.Amount + @"
                                            , 0, @CurrentUserId, @CurrentDate, @CurrentDate);");
                }
                query.AppendLine("  END");
            }
            if (entity.Attachments != null && entity.Attachments.Count > 0)
            {
                query.AppendLine("IF @RowId > 0 ");
                query.AppendLine("  BEGIN");
                foreach (var obj in entity.Attachments)
                {
                    query.AppendLine(@" INSERT INTO Attachment (EntityType, EntityId, EntityFieldName
                                            , DisplayName, Notes
                                            , InActive, CreatedBy, CreateDate, UpdateDate)  
                                        VALUES ('Invoice', @RowId, N'" + Utility.Wrap(obj.EntityFieldName) + @"'
                                            , N'" + Utility.Wrap(obj.DisplayName) + @"', N'" + Utility.Wrap(obj.Notes) + @"'
                                            , 0, @CurrentUserId, @CurrentDate, @CurrentDate);");
                }
                query.AppendLine("  END");
            }
            if (entity.SendType != SendType.Link.ToString() && entity.SendType != SendType.Whatsapp.ToString())
            {
                query.AppendLine(@" INSERT INTO [Event] ([Type], SendType, EntityType, EntityId
                                        , Sent, InActive, CreatedBy, CreateDate, UpdateDate)
                                    VALUES ('" + EventType.InvoiceCreated + @"', '" + entity.SendType + @"', N'" + EntityType.Invoice + @"', @RowId
                                        , 0, 0, @CurrentUserId, @CurrentDate, @CurrentDate)");
            }
        }

        query.AppendLine(@" COMMIT TRANSACTION [InvoiceInsert]
                            SELECT @Rows

                            END TRY
                            BEGIN CATCH
                                --SELECT  ERROR_NUMBER() AS ErrorNumber, ERROR_SEVERITY() AS ErrorSeverity ,ERROR_STATE() AS ErrorState, ERROR_PROCEDURE() AS ErrorProcedure, ERROR_LINE() AS ErrorLine, ERROR_MESSAGE() AS ErrorMessage;  
                                ROLLBACK TRANSACTION [InvoiceInsert]
                            END CATCH");

        var id = DB.ExecuteScalar(query.ToString());

        //generate key    
        string[] ids = id.ToString().Split(',');
        foreach (string invoiceId in ids)
        {
            string key = Utility.GetRandomInvoiceKey(invoiceId);
            DB.Execute("UPDATE Invoice SET [Key] = " + key + " WHERE Id=" + invoiceId);

            string attachment_sql = "";
            foreach (var inv in composite.Invoices)
                if (inv.Attachments != null && inv.Attachments.Count > 0)
                {
                    foreach (var obj in inv.Attachments)
                    {
                        if (!string.IsNullOrEmpty(obj.Attachment))
                        {
                            string newFileName = UploadFile(invoiceId, "Invoice", obj);
                            attachment_sql += " UPDATE Attachment SET [Name] = '" + newFileName + @"' 
                                                WHERE EntityType = 'Invoice' 
                                                    AND [EntityId] = " + invoiceId + @" 
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
        }

        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = id };
    }
    public ResponseDTO Update(string currentUserType, long currentUserId, long vendorId, InvoiceDTO.InvoiceUpdate entity)
    {
        string error = ValidateInvoice(entity);
        if (!string.IsNullOrEmpty(error))
            return new ResponseDTO() { IsValid = false, ErrorKey = error, Response = null };

        if (entity.Attachments != null && entity.Attachments.Count > 0)
            foreach (var obj in entity.Attachments)
            {
                if (obj.Id <= 0)
                {
                    string fileError = Utility.ValidateFileUpload(obj.Attachment);
                    if (!string.IsNullOrEmpty(fileError))
                        return new ResponseDTO() { IsValid = false, ErrorKey = fileError, Response = null };
                }
            }

        if (!string.IsNullOrEmpty(entity.CustomerMobile)) entity.CustomerMobile = Utility.toEnglishNumber(entity.CustomerMobile);


        StringBuilder query = new StringBuilder();
        query.AppendLine(@" DECLARE @CurrentDate DATETIME = (SELECT GETDATE())
                            DECLARE @CurrentUserId BIGINT = " + currentUserId + @";
                            DECLARE @ExchangeRate FLOAT = 0;
                            DECLARE @Amount FLOAT = 0;
                            DECLARE @Error NVARCHAR(MAX)='';
                            BEGIN TRANSACTION [InvoiceUpdate]
                            BEGIN TRY
                                
                                DECLARE @RowId BIGINT = " + entity.Id + @"; 
                                DECLARE @VendorId INT = 0;
                                DECLARE @Status NVARCHAR(50) = '';
                                
                                SELECT @Status = ISNULL([Status], '')
                                    , @VendorId = ISNULL(VendorId, 0)
   	                            FROM Invoice
                                WHERE Id = @RowId AND ISNULL(DeletedBy, 0) = 0 AND Inactive = 0 ;
        
                                IF @VendorId <= 0
		                            BEGIN RAISERROR(N'UnauthorizedAccess', 11, 1); END
	                            ELSE IF @VendorId > 0 AND @VendorId <> @CurrentUserId 
		                            BEGIN RAISERROR(N'UnauthorizedAccess', 11, 1); END 
                                ELSE IF @Status = '' OR @Status <> 'Unpaid'
		                            BEGIN RAISERROR(N'InvoiceCannotBeUpdated', 11, 1); END 
                                ELSE
                                    BEGIN
                                        UPDATE Invoice SET UpdateDate = @CurrentDate ");
        if (!string.IsNullOrEmpty(entity.Status)) query.AppendLine("          , [Status] = '" + entity.Status + "'");
        //if (entity.Amount > 0) query.AppendLine(", Amount = " + entity.Amount + ", Subtotal = " + entity.Amount + ", Total = " + entity.Amount);
        if (!string.IsNullOrEmpty(entity.SendType)) query.AppendLine("          , SendType = '" + entity.SendType + "'");
        if (entity.DiscountType != null) query.AppendLine("          , DiscountType = '" + entity.DiscountType + "'");
        else entity.DiscountAmount = 0;
        query.AppendLine("          , DiscountAmount = " + entity.DiscountAmount);
        if (entity.RemindAfter > 0) query.AppendLine("          , RemindAfter = " + entity.RemindAfter);
        if (!string.IsNullOrEmpty(entity.RefNumber)) query.AppendLine("          , RefNumber = N'" + Utility.Wrap(entity.RefNumber) + "'");
        if (!string.IsNullOrEmpty(entity.Lang)) query.AppendLine("          , Lang = N'" + Utility.Wrap(entity.Lang) + "'");
        if (!string.IsNullOrEmpty(entity.CustomerName)) query.AppendLine("          , CustomerName = N'" + Utility.Wrap(entity.CustomerName) + "'");
        if (!string.IsNullOrEmpty(entity.CustomerMobile)) query.AppendLine("          , CustomerMobile = N'" + Utility.Wrap(entity.CustomerMobile) + "'");
        if (!string.IsNullOrEmpty(entity.CustomerEmail)) query.AppendLine("          , CustomerEmail = N'" + Utility.Wrap(entity.CustomerEmail) + "'");
        if (!string.IsNullOrEmpty(entity.CurrencyCode)) query.AppendLine("          , CurrencyCode = N'" + Utility.Wrap(entity.CurrencyCode.ToUpper()) + "'");
        if (!string.IsNullOrEmpty(entity.ExpiryDate)) query.AppendLine("          , [ExpiryDate] = N'" + Utility.Wrap(entity.ExpiryDate) + "'");

        if (!string.IsNullOrEmpty(entity.Comment)) query.AppendLine("          , Comment = N'" + Utility.Wrap(entity.Comment) + "'");
        query.AppendLine("          , TermsConditionEnabled = N'" + entity.TermsConditionEnabled + "'");
        if (!string.IsNullOrEmpty(entity.TermsCondition)) query.AppendLine("          , TermsCondition = N'" + Utility.Wrap(entity.TermsCondition) + "'");
        query.AppendLine("          WHERE Id = @RowId");

        query.AppendLine(@" 
                                SET @ExchangeRate = 1;
                                SELECT @ExchangeRate = ISNULL(ConversionRate, 1) 
                                FROM Currency WHERE SymboleEn = '" + entity.CurrencyCode + @"';
                            ");
        if (entity.Amount>0)
        {
            query.AppendLine(@" 
                                SET @Amount = " + entity.Amount + @";
                                SET @Amount = @Amount * @ExchangeRate; 

                                UPDATE Invoice SET 
                                    Amount = @Amount
                                    , Subtotal = @Amount
                                    , Total = @Amount
                                    , AlternateAmount = " + entity.Amount + @"
                                WHERE Id = @RowId;
                            ");
        }
        if (entity.Items != null && entity.Items.Count > 0)
        {
            query.AppendLine("UPDATE InvoiceItem SET DeletedBy = @CurrentUserId, DeleteDate = @CurrentDate, InActive = 1 WHERE InvoiceId = @RowId; ");
            foreach (var item in entity.Items)
            {
                query.AppendLine(@" IF EXISTS (SELECT Id FROM InvoiceItem WHERE InvoiceId = @RowId AND Id = " + item.Id + @")
                                    BEGIN
                                        UPDATE InvoiceItem SET 
                                            [Name] = N'" + item.Name + @"'
                                            , Quantity = " + item.Quantity + @"
                                            , Amount = (@ExchangeRate * " + item.Amount + @")
                                            , AlternateAmount = " + item.Amount + @"
                                            , DeletedBy = NULL, DeleteDate = NULL, InActive = 0, UpdateDate = @CurrentDate 
                                        WHERE InvoiceId = @RowId AND Id = " + item.Id + @"
                                    END
                                    ELSE
                                    BEGIN
                                        INSERT INTO InvoiceItem (InvoiceId, [Name], Quantity
                                            , Amount, AlternateAmount
                                            , InActive, CreatedBy, CreateDate, UpdateDate) 
                                        VALUES (@RowId, N'" + item.Name + "', " + item.Quantity + @"
                                            , (@ExchangeRate * " + item.Amount + @"), " + item.Amount + @"
                                            , 0, @CurrentUserId, @CurrentDate, @CurrentDate );
                                    END");
            }
        }
        if (entity.Attachments != null && entity.Attachments.Count > 0)
        {
            foreach (var obj in entity.Attachments)
            {
                if (obj.Id <= 0)
                {
                    query.AppendLine(@" INSERT INTO Attachment (EntityType, EntityId, EntityFieldName
                                            , DisplayName, Notes
                                            , InActive, CreatedBy, CreateDate, UpdateDate)  
                                        VALUES ('Invoice', @RowId, N'" + Utility.Wrap(obj.EntityFieldName) + @"'
                                            , N'" + Utility.Wrap(obj.DisplayName) + @"', N'" + Utility.Wrap(obj.Notes) + @"'
                                            , 0, @CurrentUserId, @CurrentDate, @CurrentDate);");
                }
            }
        }

        query.AppendLine(@"         END
                            COMMIT TRANSACTION [InvoiceUpdate]
                            SELECT @RowId
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [InvoiceUpdate]
                                SELECT ERROR_MESSAGE()
                                --RAISERROR(@Error, 11, 1);
                                --SELECT  ERROR_NUMBER() AS ErrorNumber, ERROR_SEVERITY() AS ErrorSeverity ,ERROR_STATE() AS ErrorState, ERROR_PROCEDURE() AS ErrorProcedure, ERROR_LINE() AS ErrorLine, ERROR_MESSAGE() AS ErrorMessage;  
                            END CATCH");
        object result = DB.ExecuteScalar(query.ToString());
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
                            string newFileName = UploadFile(result, "Invoice", obj);
                            attachment_sql += " UPDATE Attachment SET [Name] = '" + newFileName + @"' 
                                                WHERE EntityType = 'Invoice' 
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
    public ResponseDTO Delete(string currentUserType, long currentUserId, long vendorId, long id)
    {
        string query = @"   DECLARE @Error NVARCHAR(MAX)='';
                            DECLARE @Id BIGINT = " + id + @"
                            DECLARE @CurrentUserId BIGINT = " + currentUserId + @"
                            DECLARE @VendorId BIGINT = 0;                    
                            DECLARE @Status NVARCHAR(50) = '';
                            
                            SELECT @Status = ISNULL(inv.[Status], '')
                                , @VendorId = ISNULL(inv.VendorId, 0)
	                        FROM Invoice inv INNER JOIN [User] u ON u.Id = inv.VendorId
	                        WHERE inv.Id = @Id;
                        
                            IF @VendorId <= 0
		                        BEGIN SET @Error = N'UnauthorizedAccess'; END
	                        ELSE IF @VendorId > 0 AND @VendorId <> " + vendorId + @"
		                        BEGIN SET @Error = N'UnauthorizedAccess'; END
	                        ELSE IF @Status = '' OR @Status <> 'Unpaid'
		                        BEGIN SET @Error = N'InvoiceCannotBeDeleted'; END
	                        ELSE
		                        BEGIN
			                        Update Invoice SET DeletedBy = @CurrentUserId, DeleteDate = GETDATE() WHERE Id = @Id
		                        END

                            SELECT @Error;";
        var error = DB.ExecuteScalar<string>(query);
        if (error != null && !string.IsNullOrEmpty(error))
            return new ResponseDTO() { IsValid = false, ErrorKey = error, Response = null };

        return new ResponseDTO() { IsValid = true };
    }
    private string ValidateInvoice(InvoiceDTO.InvoiceInsert entity)
    {
        if (entity.Amount <= 0) return "AmountRequired";
        if (!string.IsNullOrEmpty(entity.SendType)
            && entity.SendType != SendType.SMS.ToString()
            && entity.SendType != SendType.Email.ToString()
            && entity.SendType != SendType.Link.ToString()
            && entity.SendType != SendType.Whatsapp.ToString()) return "InvalidSendType";
        if (entity.SendType == SendType.SMS.ToString() && string.IsNullOrEmpty(entity.CustomerMobile)) return "CustomerMobileRequired";
        if (entity.SendType == SendType.Email.ToString() && string.IsNullOrEmpty(entity.CustomerEmail)) return "CustomerEmailRequired";
        if (!string.IsNullOrEmpty(entity.Lang) && entity.Lang.Length != 2) return "InvalidLanguageCode";
        if (string.IsNullOrEmpty(entity.CustomerName)) return "CustomerNameRequired";
        if (!string.IsNullOrEmpty(entity.CurrencyCode) && entity.CurrencyCode.Length != 3) return "InvalidCurrencyCode";
        if (!string.IsNullOrEmpty(entity.DiscountType)
            && entity.DiscountType != DiscountType.Amount.ToString()
            && entity.DiscountType != DiscountType.Percent.ToString()) return "InvalidInvoiceDiscountType";
        if (entity.DiscountType == DiscountType.Amount.ToString() && entity.DiscountAmount <= 0) return "DiscountAmountRequired";
        if (!string.IsNullOrEmpty(entity.ExpiryDate) && !Utility.ValidateDate(entity.ExpiryDate)) return "InvalidDateFormat";
        if (entity.Items != null && entity.Items.Count > 0)
        {
            float discountAmount = entity.DiscountAmount;
            float itemsTotal = entity.Items.Sum(x => x.Amount * x.Quantity);

            if (entity.DiscountType == DiscountType.Percent.ToString())
                discountAmount = ((itemsTotal * discountAmount) / 100);

            if ((entity.Amount + discountAmount) != itemsTotal) return "InvoiceAmountMustEqualsToTotalItemsAmount";
        }

        return string.Empty;
    }

    public ResponseDTO GetForPaymentByKey(long key)
    {
        string query = @"   DECLARE @key BIGINT = " + key + @"
                            DECLARE @ExpiredDate DATETIME = NULL;
                            
                            SELECT @ExpiredDate = CONVERT(DATE, ISNULL([ExpiryDate], GETDATE()), 102)
	                        FROM [Invoice] 
                            WHERE [Key] = @key AND Status = 'Unpaid';
                            
                            IF @ExpiredDate IS NOT NULL AND CONVERT(DATE, @ExpiredDate, 102) < CONVERT(DATE, GETDATE(), 102)
                                UPDATE Invoice SET [Status] = '" + InvoiceStatus.Expired.ToString() + @"' WHERE [Key] = @key
                        
                            UPDATE Invoice SET ViewsNo = ISNULL(ViewsNo, 0)+1 WHERE [Key] = @key;
    
                            SELECT inv.Id, inv.[Type], inv.VendorId, inv.Code, inv.RefNumber
                                , inv.[Status], inv.Amount, SendType, Lang
                                , CustomerName, CustomerMobile, CustomerEmail
                                , inv.CurrencyCode, DiscountType, DiscountAmount, CONVERT(DATE, ISNULL([ExpiryDate], GETDATE()), 102) ExpiryDate
                                , RemindAfter, Comment, inv.TermsConditionEnabled, inv.TermsCondition
                                , ISNULL(Fees, 0) Fees, ISNULL(Subtotal, 0) Subtotal, ISNULL(Total, 0) Total
                                , inv.Type
                                , ISNULL(a.DisplayName, '') AS VendorLogo
                                , v.WebSiteUrl VendorWebSiteUrl
                                , v.SocialLinksJson VendorSocialLinksJson
                                , v.CompanyName VendorCompany
                                
			                FROM Invoice inv
                                INNER JOIN [User] u ON u.Id = inv.VendorId
                                INNER JOIN Vendor vendor ON vendor.UserId = u.Id
                                INNER JOIN Vendor v ON v.UserId = u.Id
								LEFT JOIN Attachment a ON a.EntityId = u.Id
								    AND a.EntityType='Vendor'
								    AND a.EntityFieldName = 'Logo'
			                WHERE inv.[Key] = @key AND inv.DeletedBy IS NULL AND inv.InActive = 0 
		                ";
        InvoiceDTO.InvoicePayment invoice = DB.Query<InvoiceDTO.InvoicePayment>(query.ToString()).FirstOrDefault();
        if (invoice != null)
        {
            query = @"  SELECT *
	                    FROM InvoiceItem
	                    WHERE ISNULL(DeletedBy, 0) = 0 AND InActive = 0 AND InvoiceId = " + invoice.Id;
            var items = DB.Query<InvoiceItemDTO>(query.ToString()).ToList();
            invoice.Items = items;

            query = @"  SELECT Id,'"+AppSettings.Instance.AttachmentUrl+"Invoice/"+@"'+ [Name] [Name], DisplayName, Notes, EntityFieldName
	                    FROM Attachment
	                    WHERE DeletedBy IS NULL AND InActive = 0 AND EntityType = 'Invoice' AND EntityId = " + invoice.Id;
            var attachments = DB.Query<AttachmentDTOLight>(query.ToString()).ToList();
            invoice.Attachments = attachments;
        }

        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = invoice };
    }
    public ResponseDTO CreatePaymentLinkInvoice(InvoiceDTO.ForPaymentLink entity)
    {
        if (entity.PaymentLinkKey <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "PaymentLinkKeyRequired", Response = null }; 
        if (entity.Amount <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "AmountRequired", Response = null }; 
        if (string.IsNullOrEmpty(entity.CustomerName))
            return new ResponseDTO() { IsValid = false, ErrorKey = "CustomerNameRequired", Response = null }; 
        if (string.IsNullOrEmpty(entity.CustomerMobile))
            return new ResponseDTO() { IsValid = false, ErrorKey = "CustomerMobileRequired", Response = null }; 

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" DECLARE @Error NVARCHAR(MAX)='';
                            BEGIN TRANSACTION [PaymentLinkInvoiceInsert]
                            BEGIN TRY
                                DECLARE @RowId BIGINT = 0; 
                                DECLARE @OriginalKey BIGINT = 0
                                DECLARE @Amount FLOAT = " + entity.Amount + @"
                                DECLARE @OriginalAmount FLOAT = 0
                                DECLARE @MinAmount FLOAT = 0
                                DECLARE @MaxAmount FLOAT = 0
                                DECLARE @IsOpenAmount BIT = 0
                                SELECT @IsOpenAmount = ISNULL(IsOpenAmount, 0) 
                                    , @MinAmount = ISNULL(MinAmount, 0)
                                    , @MaxAmount = ISNULL(MaxAmount, 0)
                                    , @OriginalAmount = ISNULL(Amount, 0)
                                    , @OriginalKey = ISNULL([Key], 0)
                                FROM PaymentLink WHERE [Key] = " + entity.PaymentLinkKey + @"
                                
                                IF @OriginalKey IS NULL OR @OriginalKey = 0
                                BEGIN RAISERROR(N'InvalidKey', 11, 1); END
                                ELSE IF @IsOpenAmount = 1 AND (@Amount < @MinAmount OR @Amount > @MaxAmount)
                                BEGIN RAISERROR(N'InvalidAmount', 11, 1); END
                                ELSE IF @IsOpenAmount = 0 AND @Amount <> @OriginalAmount
                                BEGIN RAISERROR(N'InvalidAmount', 11, 1); END
                                
                                INSERT INTO Invoice 
                                    (VendorId, [Type], CurrencyCode, [Status], Amount, Lang
                                    , CustomerName, CustomerMobile, CustomerEmail
                                    , [ExpiryDate], DiscountType
                                    , SendType, PaymentLinkKey
                                    , Subtotal, Total, Comment
                                    , TermsConditionEnabled, TermsCondition
                                    , InActive, CreatedBy, CreateDate, UpdateDate) 
                                SELECT pl.VendorId, '" + InvoiceType.PaymentLink + @"', pl.Currency, 'Unpaid', @Amount, pl.Lang
                                    , N'" + Utility.Wrap(entity.CustomerName) + @"', N'" + Utility.Wrap(entity.CustomerMobile) + @"', N'" + Utility.Wrap(entity.CustomerEmail) + @"'
                                    , DATEADD(dd, v.InvoiceValidDays, GETDATE()), '" + DiscountType.Amount + @"'
                                    , '" + SendType.SMS + @"', pl.[Key] 
                                    , @Amount, @Amount,N'" + Utility.Wrap(entity.Comment) + @"'
                                    , pl.TermsConditionEnabled, pl.TermsCondition
                                    , 0, 0, GETDATE(), GETDATE()
                                FROM PaymentLink pl 
                                    INNER JOIN Vendor v ON v.UserId = pl.VendorId 
                                WHERE pl.[Key] = @OriginalKey
                                SELECT @RowId = SCOPE_IDENTITY();
                                EXECUTE dbo.GenerateEntityCode 'Invoice', @RowId;

                            COMMIT TRANSACTION [PaymentLinkInvoiceInsert]
                            SELECT @RowId
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [PaymentLinkInvoiceInsert]
                                SELECT ERROR_MESSAGE()
                            END CATCH");

        object result = DB.ExecuteScalar(query.ToString());
        try
        {
            result = Convert.ToInt64(result);
            string key = Utility.GetRandomInvoiceKey(result);
            DB.Execute("UPDATE Invoice SET [Key] = " + key + " WHERE Id=" + result);
            return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = result };
        }
        catch
        {
            return new ResponseDTO() { IsValid = false, ErrorKey = result.ToString(), Response = null };
        }
    }
    public ResponseDTO CreateProductLinkInvoice(InvoiceDTO.ForProductLink entity)
    {
        if (entity.ProductLinkKey <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "ProductLinkKeyRequired", Response = null };
        if (string.IsNullOrEmpty(entity.CustomerName))
            return new ResponseDTO() { IsValid = false, ErrorKey = "CustomerNameRequired", Response = null };
        if (string.IsNullOrEmpty(entity.CustomerMobile))
            return new ResponseDTO() { IsValid = false, ErrorKey = "CustomerMobileRequired", Response = null };
        if (entity.TotalAmount <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "TotalAmountRequired", Response = null }; 
        if (entity.Products == null || entity.Products.Count <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "ProductsRequired", Response = null }; 
        if (entity.Products.Sum(x => x.Quantity * x.Amount) != entity.TotalAmount)
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvoiceAmountMustEqualsToTotalItemsAmount", Response = null }; 
        if (entity.Products.Any(x => x.Quantity <= 0))
            return new ResponseDTO() { IsValid = false, ErrorKey = "ProductQuantityRequired", Response = null }; 
        if (entity.Products.Any(x => x.Amount <= 0))
            return new ResponseDTO() { IsValid = false, ErrorKey = "ProductAmountRequired", Response = null }; 

        StringBuilder query = new StringBuilder();
        query.AppendLine(@" DECLARE @Error NVARCHAR(MAX)='';
                            BEGIN TRANSACTION [ProductLinkInvoiceInsert]
                            BEGIN TRY
                                DECLARE @RowId BIGINT = 0;
                                DECLARE @InvoiceAmount FLOAT = " + entity.TotalAmount + @";
                                DECLARE @OriginalKey BIGINT = 0
                                SELECT @OriginalKey = ISNULL([Key], 0)
                                FROM ProductLink WHERE [Key] = " + entity.ProductLinkKey + @"
                                
                                IF @OriginalKey IS NULL OR @OriginalKey = 0
                                BEGIN RAISERROR(N'InvalidKey', 11, 1); END

                                INSERT INTO Invoice 
                                    (VendorId, [Type], CurrencyCode, [Status], Amount, Lang
                                    , CustomerName, CustomerMobile, CustomerEmail
                                    , [ExpiryDate], DiscountType
                                    , SendType, ProductLinkKey
                                    , Subtotal, Total, Comment
                                    , TermsConditionEnabled, TermsCondition
                                    , InActive, CreatedBy, CreateDate, UpdateDate) 
                                SELECT pl.VendorId, '" + InvoiceType.Order + @"', 'KWD', 'Unpaid', @InvoiceAmount, '" + entity.Lang + @"'
                                    , N'" + Utility.Wrap(entity.CustomerName) + @"', N'" + Utility.Wrap(entity.CustomerMobile) + @"', N'" + Utility.Wrap(entity.CustomerEmail) + @"'
                                    , DATEADD(dd, v.InvoiceValidDays, GETDATE()), '" + DiscountType.Amount + @"'
                                    , '" + SendType.SMS + @"', pl.[Key]
                                    , @InvoiceAmount, @InvoiceAmount, N'" + Utility.Wrap(entity.Comment) + @"'
                                    , pl.TermsConditionEnabled, pl.TermsCondition
                                    , 0, 0, GETDATE(), GETDATE()
                                FROM ProductLink pl 
                                    INNER JOIN Vendor v ON v.UserId = pl.VendorId 
                                WHERE pl.[Key] = @OriginalKey
                                SELECT @RowId = SCOPE_IDENTITY();
                                EXECUTE dbo.GenerateEntityCode 'Invoice', @RowId;
                         
            
                                IF @RowId > 0 
                                BEGIN
                                    DECLARE @ItemId BIGINT = 0;
                                    DECLARE @ItemName NVARCHAR(250) = '';
                                    DECLARE @Stockable BIT = 0;
                                    DECLARE @OriginalQuantity FLOAT = 0;
                                    DECLARE @Quantity FLOAT = 0;
                                    DECLARE @Amount FLOAT = 0;
                                    DECLARE @OriginalAmount FLOAT = 0;");
        foreach (var item in entity.Products)
        {
            query.AppendLine(@"     SET @ItemName = ''; SET @Stockable = 0; SET @OriginalQuantity = 0;
                                    SET @ItemId = " + item.ProductId + @"; SET @Quantity = " + item.Quantity + @"; 
                                    SET @Amount = " + item.Amount + @";
                                    SELECT @ItemName = ISNULL(NameEn, '')
                                        , @OriginalQuantity = ISNULL(Quantity, 0)
                                        , @OriginalAmount = ISNULL(Price, 0)
                                        , @Stockable = ISNULL(Stockable, 0)
                                    FROM Product 
                                    WHERE Id = @ItemId
                                    IF @ItemName = '' BEGIN RAISERROR(N'ProductNotExist', 11, 1); END
                                    ELSE IF @Amount <> @OriginalAmount BEGIN RAISERROR(N'InvalidProductAmount', 11, 1); END
                                    ELSE IF @Stockable = 1 AND @Quantity > @OriginalQuantity BEGIN RAISERROR(N'YouExceededAvailableQuantity', 11, 1); END
                                    
                                    INSERT INTO InvoiceItem (InvoiceId, [Name], Quantity, Amount, InActive, CreatedBy, CreateDate, UpdateDate) 
                                    VALUES (@RowId, @ItemName, @Quantity, @Amount, 0, 0, GETDATE(), GETDATE() );
                                    IF @Stockable = 1
                                        BEGIN 
                                            UPDATE Product SET Quantity = (ISNULL(Quantity, 0) - @Quantity) 
                                            WHERE Id = @ItemId; 
                                        END
                        ");
        }
        query.AppendLine(@" END
                            COMMIT TRANSACTION [ProductLinkInvoiceInsert]
                            SELECT @RowId
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [ProductLinkInvoiceInsert]
                                 SELECT ERROR_MESSAGE()
                            END CATCH");

        object result = DB.ExecuteScalar(query.ToString());
        try
        {
            result = Convert.ToInt64(result);
            string key = Utility.GetRandomInvoiceKey(result);
            DB.Execute("UPDATE Invoice SET [Key] = " + key + " WHERE Id=" + result);
            return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = result };
        }
        catch
        {
            return new ResponseDTO() { IsValid = false, ErrorKey = result.ToString(), Response = null };
        }
    }


    public ResponseDTO GetInfo(long id, string paymentMethodCode = "", bool ignore_expire_status = false)
    {
        if (id <= 0)
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvoiceIdRequired", Response = null };
        StringBuilder query = new StringBuilder();
        query.AppendLine(@" SELECT inv.Id, inv.OriginalInvoiceId, inv.[Key], inv.Code, inv.VendorId
                                , inv.[Type], inv.[Status], inv.CurrencyCode
                                , CONVERT(DATE, ISNULL(inv.[ExpiryDate], GETDATE()), 102) ExpiryDate
                                , inv.CustomerName, inv.CustomerMobile, inv.CustomerEmail
                                , inv.Lang, ISNULL(inv.Amount, 0) Amount, ISNULL(inv.AlternateAmount, 0) AlternateAmount
                                , inv.CreatedBy
                                , ISNULL(pm.Subtotal, 0) Subtotal
                                , ISNULL(pm.Fees, 0) Fees, ISNULL(pm.Total, 0) Total
                                , u.[Name] VendorName, u.Mobile VendorMobile, u.Email VendorEmail
                                , v.SmsSender, v.CompanyName VendorCompany
                            FROM Invoice inv 
                                INNER JOIN [User] u ON u.Id = inv.VendorId
                                INNER JOIN Vendor v ON v.UserId = u.Id
                                CROSS APPLY dbo.GetInvoicePaymentMethod(inv.Id,'" + paymentMethodCode + @"') pm
                            WHERE inv.Id =" + id + @"");
        InvoiceDTO.Info inv = DB.Query<InvoiceDTO.Info>(query.ToString()).FirstOrDefault();
        if (inv == null)
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvoiceNotExist", Response = null };

        if (!ignore_expire_status)
        {
            if (inv.Status != InvoiceStatus.Unpaid.ToString())
                return new ResponseDTO() { IsValid = false, ErrorKey = "InvoiceAlreadyPaid", Response = null };
            if (string.IsNullOrEmpty(inv.ExpiryDate) || Convert.ToDateTime(inv.ExpiryDate) < DateTime.Now)
                return new ResponseDTO() { IsValid = false, ErrorKey = "InvoiceExpired", Response = null };
        }
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = inv };
    }

    //public IEnumerable<InvoiceDTO.PaidByVendor> GetByVendorDeposit(long currentUserId, string currentUserType, long vendorId, long depositId)
    //{
    //    string query = @" SELECT i.Id, i.VendorId, ISNULL(i.VendorDepositId, 0) VendorDepositId, i.CurrencyCode
    //                            , i.Code, i.[Status], i.CreateDate, i.CustomerName, i.[Type]
    //                            , ISNULL(i.Amount, 0) Amount, ISNULL(i.Fees, 0) Fees, i.PaymentDate
    //                            , ISNULL(i.Total, 0) Total, ISNULL(i.Subtotal, 0) Subtotal
    //                        FROM Invoice i                            
    //                            INNER JOIN VendorDeposit vd ON i.VendorDepositId = vd.Id 
    //                                AND i.VendorId = vd.VendorId
    //                            INNER JOIN Deposit d ON vd.DepositId = d.Id
    //                        WHERE ISNULL(i.DeletedBy, 0) = 0 
    //                            AND i.InActive = 0
    //                            AND i.VendorId = " + vendorId + @" 
    //                            AND d.Id = " + depositId + @"
    //                            AND i.[Type] != '" + EntityType.Refund + @"'
    //                        GROUP BY i.Id, i.VendorId,  i.VendorDepositId, i.CurrencyCode
    //                            , i.Code, i.[Status], i.CreateDate, i.CustomerName
    //                            ,  i.Amount, Fees, i.PaymentDate, i.[Type]
    //                            ,  i.Total, i.Subtotal
    //                        ";
    //    var result = DB.Query<InvoiceDTO.PaidByVendor>(query).ToList();
    //    return result;
    //}

    //public IEnumerable<InvoiceDTO.PaidByVendor> GetVendorInvoicesForDeposit(long vendorId, List<long> InvoicesIds)
    //{
    //    //remove possible duplications from FRONT END
    //    long[] cleanInvoicesIds = InvoicesIds.Distinct().ToArray();

    //    string query = @"   SELECT Id, VendorId, ISNULL(VendorDepositId, 0) VendorDepositId, CurrencyCode
    //                            , Code, [Status], CreateDate, CustomerName, [Type]
    //                            , ISNULL(Amount, 0) Amount, ISNULL(Fees, 0) Fees
    //                            , ISNULL(Total, 0) Total, ISNULL(Subtotal, 0) Subtotal
    //                        FROM Invoice
    //                        WHERE ISNULL(DeletedBy, 0) = 0 
    //                            AND InActive = 0
    //                            AND Id IN (" + String.Join(",", cleanInvoicesIds) + @") 
    //                            AND VendorId = " + vendorId + @"    
    //                            AND [Status] IN ('" + InvoiceStatus.Paid + @"','" + InvoiceStatus.Refunded + @"')
    //                            AND ISNULL(VendorDepositId, 0) = 0
    //                            AND [Type] != '" + EntityType.Refund + "'";
    //    var result = DB.Query<InvoiceDTO.PaidByVendor>(query.ToString()).ToList();
    //    return result;
    //}

    //public IEnumerable<InvoiceDTO.PaidByVendor> GetReadyForDepositByVendorId(
    //    string depositType,
    //    string currentUserType,
    //    long vendorId,
    //    string list,
    //    string code,
    //    string refNumber,
    //    string dateFrom,
    //    string dateTo,
    //    float amountFrom,
    //    float amountTo,
    //    string customerName,
    //    string customerMobile,
    //    string customerEmail,
    //    string paymentMethod = "")
    //{
    //    string sqlCondition = "";
    //    if (list == "Eligible")
    //        sqlCondition = "    AND CAST(inv.PaymentDate AS Date) = DATEADD(DAY, -1, CAST(GETDATE() AS DATE))";
    //    else if (list == "24Hours")
    //        sqlCondition = "    AND CAST(inv.PaymentDate as Date) = CAST(GETDATE() AS DATE)";

    //    StringBuilder query = new StringBuilder();
    //    query.AppendLine(@" SELECT inv.Id, inv.VendorId, ISNULL(inv.VendorDepositId, 0) VendorDepositId, inv.CurrencyCode
    //                            , inv.Code, inv.[Status], inv.CreateDate, inv.CustomerName
    //                            , ISNULL(inv.Amount, 0) Amount, ISNULL(inv.Fees, 0) Fees, [Type]
    //                            , ISNULL(inv.Total, 0) Total, ISNULL(inv.Subtotal, 0) Subtotal
    //                        FROM Invoice inv
    //                        WHERE ISNULL(inv.DeletedBy, 0) = 0 
    //                            AND InActive = 0
    //                            AND inv.VendorId = " + vendorId + @" 
    //                            AND inv.[Status] IN ('" + InvoiceStatus.Paid + @"','" + InvoiceStatus.Refunded + @"') 
    //                            AND ISNULL(inv.VendorDepositId, 0) = 0
    //                            " + sqlCondition + @"
    //                            AND (
    //                                    inv.[Type] != 'RefundAdjust' 
    //                                    OR  
    //                                    (
    //                                        inv.[Type] = 'RefundAdjust' 
    //                                        AND inv.OriginalInvoice_Id IN (
    //                                                                        SELECT id FROM Invoice nv 
    //                                                                        WHERE nv.[Status] IN ('Paid','Deposited','Refunded')
    //                                                                        )
    //                                    )
    //                                )");
    //    if (depositType.ToLower().Equals("pos"))
    //        query.AppendLine("  AND inv.[Type] = 'PosTerminal'");
    //    else
    //        query.AppendLine("AND inv.[Type] IN ('" + InvoiceType.Invoice + "','" + InvoiceType.Order + "','" + InvoiceType.PaymentLink + "','" + InvoiceType.RefundAdjust + "') ");



    //    if (!string.IsNullOrEmpty(code)) query.AppendLine("  AND inv.Code LIKE '%" + Utility.Wrap(code) + "%'");
    //    if (!string.IsNullOrEmpty(refNumber)) query.AppendLine("  AND inv.RefNumber LIKE N'%" + Utility.Wrap(refNumber) + "%'");
    //    if (!string.IsNullOrEmpty(customerName)) query.AppendLine("  AND inv.CustomerName LIKE N'%" + Utility.Wrap(customerName) + "%'");
    //    if (!string.IsNullOrEmpty(customerMobile)) query.AppendLine("  AND inv.CustomerMobile LIKE '%" + Utility.Wrap(customerMobile) + "%'");
    //    if (!string.IsNullOrEmpty(customerEmail)) query.AppendLine("  AND inv.CustomerEmail LIKE '%" + Utility.Wrap(customerEmail) + "%'");

    //    if (!string.IsNullOrEmpty(dateFrom) && string.IsNullOrEmpty(dateTo)) query.AppendLine("  AND CONVERT(DATE, inv.[CreateDate], 102) >= CONVERT(DATE, '" + dateFrom + "', 102) ");
    //    else if (string.IsNullOrEmpty(dateFrom) && !string.IsNullOrEmpty(dateTo)) query.AppendLine("  AND CONVERT(DATE, inv.[CreateDate], 102) <= CONVERT(DATE, '" + dateTo + "', 102) ");
    //    else if (!string.IsNullOrEmpty(dateFrom) && !string.IsNullOrEmpty(dateTo)) query.AppendLine("  AND CONVERT(DATE, inv.[CreateDate], 102) BETWEEN CONVERT(DATE, '" + dateFrom + "', 102) AND CONVERT(DATE, '" + dateTo + "', 102)");


    //    if (amountFrom > 0 && amountTo <= 0) query.AppendLine("   AND inv.Amount >= " + amountFrom);
    //    else if (amountFrom <= 0 && amountTo > 0) query.AppendLine("   AND inv.Amount <= " + amountTo);
    //    else if (amountFrom > 0 && amountTo > 0) query.AppendLine("   AND inv.Amount >= " + amountFrom + " AND inv.Amount <= " + amountTo);

    //    var result = DB.Query<InvoiceDTO.PaidByVendor>(query.ToString()).ToList();
    //    return result;
    //}
}