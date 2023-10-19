using Dapper;
using PayArabic.Core.Model;
using System.Text;
using static PayArabic.Core.DTO.DepositDTO;
using static PayArabic.Core.DTO.InvoiceDTO;

namespace PayArabic.DAO;

public class DepositDao : BaseDao, IDepositDao
{
    public ResponseDTO GetAll(
        long currentUserId,
        string currentUserType,
        long vendorId,
        string vendor,
        string code,
        string number,
        string status,
        string dateFrom,
        string dateTo,
        float amountFrom,
        float amountTo,
        string depositType,
        string listOptions = null)
    {
        if (!string.IsNullOrEmpty(status) && status != DepositStatus.Started.ToString()
            && status != DepositStatus.Completed.ToString()
            && status != DepositStatus.Cancelled.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidDepositStatus", Response = null };
        if (!string.IsNullOrEmpty(dateFrom) && !Utility.ValidateDate(dateFrom))
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidDateFormat", Response = null };
        if (!string.IsNullOrEmpty(dateTo) && !Utility.ValidateDate(dateTo))
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidDateFormat", Response = null };

        StringBuilder query = new();
        query.AppendLine(@" SELECT d.Id, ISNULL(d.Code, '') Code, ISNULL(d.Number, '') Number, d.[Status]
                                , d.Amount, d.[Date] ");
        if (currentUserType == UserType.Vendor.ToString() || currentUserType == UserType.User.ToString())
            query.AppendLine(", vd.[Id] VendorDepositId, vd.Amount");

        query.AppendLine("  FROM Deposit d ");
        if (
            (currentUserType == UserType.Vendor.ToString() || currentUserType == UserType.User.ToString())
            ||
            (
                (currentUserType == UserType.SystemAdmin.ToString()
                || currentUserType == UserType.SuperAdmin.ToString()
                || currentUserType == UserType.Admin.ToString()
                )
                && vendorId == 0)
        )
        {
            query.AppendLine(@" INNER JOIN VendorDeposit vd ON vd.DepositId = d.Id
                                    AND ISNULL(vd.DeletedBy, 0) = 0 
                                    AND vd.InActive = 0
                                    ");
            if(vendorId>0)
            {
                query.AppendLine(@" AND vd.VendorId = " + vendorId + @"");
            }
            if ((currentUserType == UserType.SystemAdmin.ToString()
                || currentUserType == UserType.SuperAdmin.ToString()
                || currentUserType == UserType.Admin.ToString()
                ) && !string.IsNullOrEmpty(vendor))
            
                query.AppendLine(@" INNER JOIN [User] u ON u.Id = vd.VendorId
                                        AND (
                                                u.Code LIKE '%"+vendor+@"%'
                                                OR 
                                                u.[Name] LIKE '%"+vendor+@"%'
                                            )
                                ");
        }

        query.AppendLine(@" WHERE ISNULL(d.DeletedBy, 0) = 0 
                                AND d.InActive = 0 
                                AND ISNULL(d.[Type], 'Normal') = '" + depositType + "'");
        if (currentUserType == UserType.Vendor.ToString() || currentUserType == UserType.User.ToString())
        {
            query.AppendLine(" AND  d.Status = '" + DepositStatus.Completed.ToString() + "'");
        }

        if (!string.IsNullOrEmpty(dateFrom) && string.IsNullOrEmpty(dateTo)) query.AppendLine("  AND CONVERT(DATE, d.[Date], 102) BETWEEN CONVERT(DATE, '" + dateFrom + "', 102) AND CONVERT(DATE, '" + dateFrom + "', 102)");
        else if (string.IsNullOrEmpty(dateFrom) && !string.IsNullOrEmpty(dateTo)) query.AppendLine("  AND CONVERT(DATE, d.[Date], 102) BETWEEN CONVERT(DATE, '" + dateTo + "', 102) AND CONVERT(DATE, '" + dateTo + "', 102)");
        else if (!string.IsNullOrEmpty(dateFrom) && !string.IsNullOrEmpty(dateTo)) query.AppendLine("  AND CONVERT(DATE, d.[Date], 102) BETWEEN CONVERT(DATE, '" + dateFrom + "', 102) AND CONVERT(DATE, '" + dateTo + "', 102)");

        if (!string.IsNullOrEmpty(code)) query.AppendLine("  AND d.Code LIKE '%" + Utility.Wrap(code) + "%'");
        if (!string.IsNullOrEmpty(number)) query.AppendLine("  AND d.number LIKE '%" + Utility.Wrap(number) + "%'");

        float switch_amount;
        if (amountFrom > amountTo)
        {
            switch_amount = amountTo;
            amountTo = amountFrom;
            amountFrom = switch_amount;
        }

        if (currentUserType == UserType.Vendor.ToString() || currentUserType == UserType.User.ToString())
        {
            if (amountFrom > 0 && amountTo <= 0) query.AppendLine("   AND vd.Amount >= " + amountFrom);
            else if (amountFrom <= 0 && amountTo > 0) query.AppendLine("AND   vd.Amount <= " + amountTo);
            else if (amountFrom > 0 && amountTo > 0) query.AppendLine(" AND  vd.Amount >= " + amountFrom + " AND vd.Amount <= " + amountTo);
        }
        else
        {
            if (amountFrom > 0 && amountTo <= 0) query.AppendLine("   AND d.Amount >= " + amountFrom);
            else if (amountFrom <= 0 && amountTo > 0) query.AppendLine("AND  d.Amount <= " + amountTo);
            else if (amountFrom > 0 && amountTo > 0) query.AppendLine(" AND  d.Amount >= " + amountFrom + " AND d.Amount <= " + amountTo);
        }

        if (!string.IsNullOrEmpty(status)) query.AppendLine("  AND d.status LIKE '%" + Utility.Wrap(status) + "%'");

        List<DepositDTO.DepositList> result;
        if (!string.IsNullOrEmpty(listOptions))
        {
            string filterQuery = GetFilterQuery(query, listOptions);
            result = DB.Query<DepositDTO.DepositList>(filterQuery).ToList();
        }
        else
        {
            query.AppendLine("  ORDER BY d.Id DESC ");
            result = DB.Query<DepositDTO.DepositList>(query.ToString()).ToList();
        }
        if (result == null || result.Count <= 0)
            return new ResponseDTO { IsValid = false, ErrorKey = "EmptyResult" };
        return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = result };
    }
    public ResponseDTO GetById(long currentUserId, string currentUserType, string type, long id)
    {
        StringBuilder query = new();
        query.AppendLine(@" SELECT d.Id, ISNULL(d.Code, '') Code, ISNULL(d.Number, '') Number, d.[Status]
                            , ISNULL(d.Amount, 0) Amount, d.[Date]
                            , ISNULL(d.Note, '') Note, 'KWD' CurrencyCode
                            FROM Deposit d  
                            WHERE ISNULL(d.DeletedBy, 0) = 0 AND d.InActive = 0
                                AND d.Id = " + id);
        if (!string.IsNullOrEmpty(type)) query.AppendLine(@" AND ISNULL(d.[Type], 'normal') = '" + type + @"'");
        if (currentUserType == UserType.Vendor.ToString().ToLower() || currentUserType == UserType.User.ToString().ToLower())
            query.AppendLine(@" AND d.Id IN 
                                    (
                                        SELECT DepositId 
                                        FROM VendorDeposit 
                                        WHERE VendorId = " + currentUserId + @" AND ISNULL(DeletedBy, 0) = 0 AND InActive = 0
                                    )");

        var entity = DB.Query<DepositDTO.DepositGetById>(query.ToString()).FirstOrDefault();

        if (entity != null)
        {
            // Get Deposit Vendors and for each vendor get his invoices
            query.Clear();
            query.AppendLine(@"
                                SELECT inv.Id, inv.[Key], inv.Code, [Type], inv.[Status]
                                    , ISNULL(inv.Amount, 0) Amount, ISNULL(inv.Subtotal, 0) Subtotal
                                    , ISNULL(inv.Fees, 0) Fees, ISNULL(inv.Total, 0) Total
                                    , inv.CreateDate, ISNULL(inv.PaymentDate, ''), inv.CurrencyCode
                                    , inv.CustomerName
                                
                                    , inv.VendorId, ISNULL(inv.VendorDepositId, 0) VendorDepositId
                                    , u.Code VendorCode, u.[Name] VendorName, v.CompanyName VendorCompany
                                    , v.AccountHolderName, v.AccountNumber, v.IBAN
                                    , ISNULL(b.NameAr, '') BankNameAr, ISNULL(b.NameEn, '') BankNameEn, ISNULL(b.Swift, '') BankSwift
                                FROM Invoice inv 
                                    INNER JOIN [User] u ON u.Id = inv.VendorId
                                    INNER JOIN Vendor v ON v.UserId = u.Id
                                    INNER JOIN VendorDeposit vd ON inv.VendorDepositId = vd.Id 
                                        AND inv.VendorId = vd.VendorId 
                                    LEFT JOIN [Bank] b ON b.Id = v.BankId
                                WHERE ISNULL(inv.DeletedBy, 0) = 0 
                                    AND inv.InActive = 0 
                                    AND u.UserType = '" + UserType.Vendor + @"' 
                                    AND ISNULL(u.DeletedBy, 0) = 0 
                                    AND u.InActive = 0
                                    AND inv.[Type] != '" + EntityType.Refund + @"'
                                    AND vd.DepositId=" + id);
            if (currentUserType == UserType.Vendor.ToString().ToLower() || currentUserType == UserType.User.ToString().ToLower())
            {
                query.AppendLine(" AND  vd.VendorId=" + currentUserId);
                query.AppendLine(" AND  inv.VendorId=" + currentUserId);
            }

            List<InvoiceDTO.ForDeposit> result = DB.Query<InvoiceDTO.ForDeposit>(query.ToString()).ToList();
            if (result != null && result.Count > 0)
            {
                var vendors = result.Select(x => new DepositDTO.Vendor()
                {
                    VendorId = x.VendorId,
                    VendorCode = x.VendorCode,
                    VendorName = x.VendorName,
                    VendorCompany = x.VendorCompany,
                    AccountHolderName = x.AccountHolderName,
                    AccountNumber = x.AccountNumber,
                    IBAN = x.IBAN,
                    BankNameAr = x.BankNameAr,
                    BankNameEn = x.BankNameEn,
                    BankSwift = x.BankSwift,
                    InvoiceCount = 0,
                    InvoiceTotal = 0,
                    Invoices = new List<InvoiceDTO.ForVendor>()
                }).DistinctBy(x => x.VendorId).ToList();
                foreach (var vendor in vendors)
                {
                    var invoices = result.Where(x => x.VendorId == vendor.VendorId)
                        .Select(x => new InvoiceDTO.ForVendor()
                        {
                            Id = x.Id,
                            Key = x.Key,
                            Code = x.Code,
                            Type = x.Type,
                            Status = x.Status,
                            Amount = x.Amount,
                            Subtotal = x.Subtotal,
                            Fees = x.Fees,
                            Total = x.Total,
                            CreateDate = x.CreateDate,
                            PaymentDate = x.PaymentDate,
                            CurrencyCode = x.CurrencyCode,
                            CustomerName = x.CustomerName
                        }).DistinctBy(x => x.Id).ToList();
                    if (invoices != null && invoices.Count > 0)
                    {
                        vendor.Invoices = invoices;
                        vendor.InvoiceCount = invoices.Count;
                        vendor.InvoiceTotal = invoices.Sum(x => x.Subtotal);
                    }
                }
                entity.Vendors = new List<DepositDTO.Vendor>();
                entity.Vendors.AddRange(vendors);
            }
            // Get Deposit Attachments
            query.Clear();
            query.AppendLine(@" SELECT Id,'"+AppSettings.Instance.AttachmentUrl+"Deposit/"+@"'+ [Name] [Name], DisplayName, Notes, EntityFieldName
	                            FROM Attachment
	                            WHERE DeletedBy IS NULL AND InActive = 0 AND EntityType = 'Deposit' AND EntityId = " + id);
            var attachments = DB.Query<AttachmentDTOLight>(query.ToString()).ToList();
            entity.Attachments = attachments;
            return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = entity };
        }

        return new ResponseDTO() { IsValid = false, ErrorKey = "EmptyResult", Response = null };
    }
    public ResponseDTO Insert(long currentUserId, string currentUserType, DepositDTO.DepositInsert entity)
    {
        if (currentUserType != UserType.SuperAdmin.ToString() && currentUserType != UserType.Admin.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };

        if (entity.Attachments != null && entity.Attachments.Count > 0)
            foreach (var obj in entity.Attachments)
            {
                string fileError = Utility.ValidateFileUpload(obj.Attachment);
                if (!string.IsNullOrEmpty(fileError))
                    return new ResponseDTO() { IsValid = false, ErrorKey = fileError, Response = null };
            }

        StringBuilder query = new();
        query.AppendLine(@" 
                            DECLARE @CurrentDate DATETIME = (SELECT GETDATE())
                            DECLARE @CurrentUserId BIGINT = " + currentUserId + @";
                            DECLARE @Error NVARCHAR(MAX)='';
                            DECLARE @RowId BIGINT = 0; 
                            DECLARE @DepositTotal FLOAT = 0;
                            DECLARE @VendorId BIGINT = 0; 
                            DECLARE @VendorDepositRowId BIGINT = 0;
                            DECLARE @VendorDepositTotal FLOAT = 0;

                            BEGIN TRANSACTION [DepositInsert]
                            BEGIN TRY
                                
                                
                                INSERT INTO Deposit ([Type], [Status], Amount
                                    , Note, Date, Number
                                    , InActive, CreatedBy, CreateDate, UpdateDate)
                                VALUES ('" + entity.Type + @"', 'Started', 0
                                    , N'" + Utility.Wrap(entity.Note) + @"', @CurrentDate,  N'" + Utility.Wrap(entity.Number) + @"'
                                    , 0, @CurrentUserId, @CurrentDate, @CurrentDate );
    
                                SELECT @RowId = SCOPE_IDENTITY();
                        ");

        foreach (var vendor in entity.Vendors)
        {
            if (vendor.VendorId > 0 && vendor.DepositInvoiceIds != null && vendor.DepositInvoiceIds.Count > 0)
            {
                string vendorInvoiceIds = string.Join(",", vendor.DepositInvoiceIds);
                query.AppendLine(@" 
                                    SET @VendorId = " + vendor.VendorId + @";
                                    SET @VendorDepositRowId = 0;
                                    SET @VendorDepositTotal = 0;

                                    SELECT @VendorDepositTotal = ISNULL(SUM(ISNULL(Subtotal,0)),0)
                                    FROM Invoice 
                                    WHERE VendorId = @VendorId AND Id IN (" + vendorInvoiceIds + @")
                                    
                                    INSERT INTO VendorDeposit (DepositId, VendorId, Amount, [Status]
                                        , InActive, CreatedBy, CreateDate, UpdateDate)
                                    VALUES (@RowId, @VendorId, @VendorDepositTotal, 'Started'
                                        , 0, @CurrentUserId, @CurrentDate, @CurrentDate );

                                    SELECT @VendorDepositRowId = SCOPE_IDENTITY();

                                    UPDATE Invoice SET VendorDepositId = @VendorDepositRowId WHERE Id IN (" + vendorInvoiceIds + @");

                                    SET @DepositTotal +=  @VendorDepositTotal;
                                ");
            }
        }

        if (entity.Attachments != null && entity.Attachments.Count > 0)
        {
            query.AppendLine(@"
                                IF @RowId > 0 
                                BEGIN");
            foreach (var obj in entity.Attachments)
            {
                query.AppendLine(@" 
                                    INSERT INTO Attachment (EntityType, EntityId, EntityFieldName
                                        , DisplayName, Notes
                                        , InActive, CreatedBy, CreateDate, UpdateDate)  
                                    VALUES ('Deposit', @RowId, N'" + Utility.Wrap(obj.EntityFieldName) + @"'
                                        , N'" + Utility.Wrap(obj.DisplayName) + @"', N'" + Utility.Wrap(obj.Notes) + @"'
                                        , 0, @CurrentUserId, @CurrentDate, @CurrentDate);
                                ");
            }
            query.AppendLine("  END");
        }

        query.AppendLine(@" EXECUTE dbo.GenerateEntityCode 'Deposit', @RowId; 
                            UPDATE Deposit SET Amount = @DepositTotal WHERE Id = @RowId;

                            COMMIT TRANSACTION [DepositInsert]
                            SELECT @RowId
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [DepositInsert]
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
                    if (!string.IsNullOrEmpty(obj.Attachment))
                    {
                        string newFileName = UploadFile(result, "Deposit", obj);
                        attachment_sql += " UPDATE Attachment SET [Name] = '" + newFileName + @"' 
                                            WHERE EntityType = 'Deposit' 
                                                AND [EntityId] = " + result + @" 
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

            return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = result };
        }
        catch
        {
            return new ResponseDTO() { IsValid = false, ErrorKey = result.ToString(), Response = null };
        }
    }
    public ResponseDTO Update(long currentUserId, string currentUserType, DepositDTO.DepositUpdate entity)
    {
        if (currentUserType != UserType.SuperAdmin.ToString() && currentUserType != UserType.Admin.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };

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

        StringBuilder query = new();
        query.AppendLine(@" 
                            DECLARE @CurrentDate DATETIME = (SELECT GETDATE())
                            DECLARE @CurrentUserId BIGINT = " + currentUserId + @";
                            DECLARE @Error NVARCHAR(MAX)='';
                            DECLARE @RowId BIGINT = " + entity.Id + @"; 
                            DECLARE @DepositTotal FLOAT = 0;
                            DECLARE @VendorId BIGINT = 0; 
                            DECLARE @VendorDepositRowId BIGINT = 0;
                            DECLARE @VendorDepositTotal FLOAT = 0;


                            BEGIN TRANSACTION [DepositUpdate]
                            BEGIN TRY
                                
                                UPDATE Invoice SET VendorDepositId = 0 
                                WHERE VendorDepositId IN (SELECT Id FROM VendorDeposit WHERE DepositId = @RowId);

                                DELETE FROM VendorDeposit WHERE DepositId = @RowId;

                            ");

        foreach (var vendor in entity.Vendors)
        {
            if (vendor.VendorId > 0 && vendor.DepositInvoiceIds != null && vendor.DepositInvoiceIds.Count > 0)
            {
                string vendorInvoiceIds = string.Join(",", vendor.DepositInvoiceIds);
                query.AppendLine(@" 
                                    SET @VendorId = " + vendor.VendorId + @";
                                    SET @VendorDepositRowId = 0;
                                    SET @VendorDepositTotal = 0;

                                    SELECT @VendorDepositTotal = ISNULL(SUM(ISNULL(Subtotal,0)),0)
                                    FROM Invoice 
                                    WHERE VendorId = @VendorId AND Id IN (" + vendorInvoiceIds + @")
                                    
                                    INSERT INTO VendorDeposit (DepositId, VendorId, Amount, [Status]
                                        , InActive, CreatedBy, CreateDate, UpdateDate)
                                    VALUES (@RowId, @VendorId, @VendorDepositTotal, '" + entity.Status + @"'
                                        , 0, @CurrentUserId, @CurrentDate, @CurrentDate );

                                    SELECT @VendorDepositRowId = SCOPE_IDENTITY();

                                    UPDATE Invoice SET VendorDepositId = @VendorDepositRowId WHERE Id IN (" + vendorInvoiceIds + @");

                                    SET @DepositTotal +=  @VendorDepositTotal;
                                ");
            }
        }
        query.AppendLine(@" 
                            UPDATE Deposit SET UpdateDate = @CurrentDate
                                , [Amount] = @DepositTotal ");
        if (!string.IsNullOrEmpty(entity.Status)) query.AppendLine("          , [Status] = '" + entity.Status + "'");
        if (!string.IsNullOrEmpty(entity.Number)) query.AppendLine("          , Number = N'" + Utility.Wrap(entity.Number) + "'");
        if (!string.IsNullOrEmpty(entity.Date)) query.AppendLine("          , [Date] = N'" + Utility.Wrap(entity.Date) + "'");
        if (!string.IsNullOrEmpty(entity.Note)) query.AppendLine("          , Note = N'" + Utility.Wrap(entity.Note) + "'");
        query.AppendLine(@" WHERE Id = @RowId ");

        if (entity.Status == DepositStatus.Completed.ToString())
        {
            query.AppendLine(@" Update Invoice SET 
                                    Invoice.[Status] = '" + InvoiceStatus.Deposited.ToString() + @"'
                                FROM Invoice 
                                    INNER JOIN VendorDeposit ON Invoice.VendorDepositId = VendorDeposit.Id 
                                WHERE VendorDeposit.DepositId = @RowId ");
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
                                        VALUES ('Deposit', @RowId, N'" + Utility.Wrap(obj.EntityFieldName) + @"'
                                            , N'" + Utility.Wrap(obj.DisplayName) + @"', N'" + Utility.Wrap(obj.Notes) + @"'
                                            , 0, @CurrentUserId, @CurrentDate, @CurrentDate);");
                }
            }
        }

        query.AppendLine(@" COMMIT TRANSACTION [DepositUpdate]
                            SELECT @RowId
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [DepositUpdate]
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
                            string newFileName = UploadFile(result, "Deposit", obj);
                            attachment_sql += " UPDATE Attachment SET [Name] = '" + newFileName + @"' 
                                                WHERE EntityType = 'Deposit' 
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
        if (currentUserType != UserType.SuperAdmin.ToString() && currentUserType != UserType.Admin.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };

        // Deposit edit is only available in case of deposit status is "Started"
        string query = @"   DECLARE @Error NVARCHAR(MAX)='';
                            DECLARE @Status NVARCHAR(50) = '';
                            DECLARE @CurrentUserId BIGINT = " + currentUserId + @"

                            SELECT @Status = ISNULL([Status], '') 
                            FROM Deposit 
                            WHERE Id = " + id + @"; 
                            
                            IF @Status = '' 
                            BEGIN SET @Error = N'DepositNotExists'; END  
                            ELSE IF @Status <> 'Started' 
                            BEGIN SET @Error = N'DepositCannotBeDeleted'; END  
                            ELSE
                            BEGIN
                                Update Invoice SET 
                                        Invoice.VendorDepositId = NULL
                                        , Invoice.[Status] = '" + InvoiceStatus.Paid.ToString() + "'" + @" 
                                        , UpdatedBy = @CurrentUserId
                                        , UpdateDate = GETDATE()
                                FROM Invoice 
                                    INNER JOIN VendorDeposit ON Invoice.VendorDepositId = VendorDeposit.Id 
                                WHERE VendorDeposit.DepositId = " + id + @"; 
                            
                                UPDATE VendorDeposit SET DeletedBy = @CurrentUserId, DeleteDate = GETDATE() WHERE DepositId = " + id + @";
                            
                                UPDATE Deposit SET DeletedBy = @CurrentUserId, DeleteDate = GETDATE() WHERE Id = " + id + @"
                            END
                            SELECT @Error;";
        var error = DB.ExecuteScalar<string>(query);
        if (error != null && !string.IsNullOrEmpty(error))
            return new ResponseDTO() { IsValid = false, ErrorKey = error, Response = null };

        return new ResponseDTO() { IsValid = true };
    }

    public ResponseDTO GetVendorInvoicesReadyForDeposit(
        long currentUserId,
        string currentUserType,
        string depositType,
        string list,
        string vendorCode,
        string vendorName,
        string invoiceCode,
        string invoiceKey,
        string invoiceRefNumber,
        string customerName,
        string customerMobile,
        string customerEmail,
        string invoiceDateFrom,
        string invoiceDateTo,
        float invoiceAmountFrom,
        float invoiceAmountTo)
    {
        if (currentUserType != UserType.SuperAdmin.ToString() && currentUserType != UserType.Admin.ToString())
            return new ResponseDTO() { IsValid = false, ErrorKey = "UnauthorizedAccess", Response = null };
        if (!string.IsNullOrEmpty(invoiceDateFrom) && !Utility.ValidateDate(invoiceDateFrom))
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidDateFormat", Response = null };
        if (!string.IsNullOrEmpty(invoiceDateTo) && !Utility.ValidateDate(invoiceDateTo))
            return new ResponseDTO() { IsValid = false, ErrorKey = "InvalidDateFormat", Response = null };

        StringBuilder query = new();
        query.AppendLine(@" SELECT inv.Id, inv.[Key], inv.Code, [Type], inv.[Status]
                                , ISNULL(inv.Amount, 0) Amount, ISNULL(inv.Subtotal, 0) Subtotal
                                , ISNULL(inv.Fees, 0) Fees, ISNULL(inv.Total, 0) Total
                                , inv.CreateDate, ISNULL(inv.PaymentDate, ''), inv.CurrencyCode
                                , inv.CustomerName
                                
                                , inv.VendorId, ISNULL(inv.VendorDepositId, 0) VendorDepositId
                                , u.Code VendorCode, u.[Name] VendorName, v.CompanyName VendorCompany
                                , v.AccountHolderName, v.AccountNumber, v.IBAN
                                , ISNULL(b.NameAr, '') BankNameAr, ISNULL(b.NameEn, '') BankNameEn, ISNULL(b.Swift, '') BankSwift
                            FROM Invoice inv 
                                INNER JOIN [User] u ON u.Id = inv.VendorId
                                INNER JOIN Vendor v ON v.UserId = u.Id
                                LEFT JOIN [Bank] b ON b.Id = v.BankId
                            WHERE ISNULL(inv.DeletedBy, 0) = 0 
                                AND inv.InActive = 0 
                                AND u.UserType = '" + UserType.Vendor + @"' 
                                AND ISNULL(u.DeletedBy, 0) = 0 
                                AND u.InActive = 0
                                AND inv.[Status] IN ('" + InvoiceStatus.Paid + @"','" + InvoiceStatus.Refunded + @"') 
                                AND ISNULL(inv.VendorDepositId, 0) = 0
                            ");
        if (!string.IsNullOrEmpty(depositType) && depositType.ToLower().Equals("pos")) query.AppendLine("  AND inv.[Type] = 'PosTerminal'");
        if (!string.IsNullOrEmpty(list) && list == "Eligible") query.AppendLine("  AND CAST(inv.PaymentDate AS Date) = DATEADD(DAY, -1, CAST(GETDATE() AS DATE))");
        else if (!string.IsNullOrEmpty(list) && list == "24Hours") query.AppendLine("  AND CAST(inv.PaymentDate AS Date) = CAST(GETDATE() AS DATE)");
        if (!string.IsNullOrEmpty(vendorCode)) query.AppendLine("  AND u.Code LIKE '%" + Utility.Wrap(vendorCode) + "%'");
        if (!string.IsNullOrEmpty(vendorName)) query.AppendLine("  AND u.Name LIKE N'%" + Utility.Wrap(vendorName) + "%'");
        if (!string.IsNullOrEmpty(invoiceCode)) query.AppendLine("  AND inv.Code LIKE '%" + Utility.Wrap(invoiceCode) + "%'");
        if (!string.IsNullOrEmpty(invoiceKey)) query.AppendLine("  AND inv.[Key] LIKE '%" + Utility.Wrap(invoiceKey) + "%'");
        if (!string.IsNullOrEmpty(invoiceRefNumber)) query.AppendLine("  AND inv.RefNumber LIKE N'%" + Utility.Wrap(invoiceRefNumber) + "%'");
        if (!string.IsNullOrEmpty(customerName)) query.AppendLine("  AND inv.CustomerName LIKE N'%" + Utility.Wrap(customerName) + "%'");
        if (!string.IsNullOrEmpty(customerMobile)) query.AppendLine("  AND inv.CustomerMobile LIKE '%" + Utility.Wrap(customerMobile) + "%'");
        if (!string.IsNullOrEmpty(customerEmail)) query.AppendLine("  AND inv.CustomerEmail LIKE '%" + Utility.Wrap(customerEmail) + "%'");
        if (!string.IsNullOrEmpty(invoiceDateFrom) && string.IsNullOrEmpty(invoiceDateTo)) query.AppendLine("  AND CONVERT(DATE, inv.[CreateDate], 102) >= CONVERT(DATE, '" + invoiceDateFrom + "', 102) ");
        else if (string.IsNullOrEmpty(invoiceDateFrom) && !string.IsNullOrEmpty(invoiceDateTo)) query.AppendLine("  AND CONVERT(DATE, inv.[CreateDate], 102) <= CONVERT(DATE, '" + invoiceDateTo + "', 102) ");
        else if (!string.IsNullOrEmpty(invoiceDateFrom) && !string.IsNullOrEmpty(invoiceDateTo)) query.AppendLine("  AND CONVERT(DATE, inv.[CreateDate], 102) BETWEEN CONVERT(DATE, '" + invoiceDateFrom + "', 102) AND CONVERT(DATE, '" + invoiceDateTo + "', 102)");
        if (invoiceAmountFrom > 0 && invoiceAmountTo <= 0) query.AppendLine("   AND inv.Amount >= " + invoiceAmountFrom);
        else if (invoiceAmountFrom <= 0 && invoiceAmountTo > 0) query.AppendLine("   AND inv.Amount <= " + invoiceAmountTo);
        else if (invoiceAmountFrom > 0 && invoiceAmountTo > 0) query.AppendLine("   AND inv.Amount >= " + invoiceAmountFrom + " AND inv.Amount <= " + invoiceAmountTo);

        List<InvoiceDTO.ForDeposit> result = DB.Query<InvoiceDTO.ForDeposit>(query.ToString()).ToList();
        if (result != null && result.Count > 0)
        {
            var vendors = result.Select(x => new DepositDTO.Vendor()
            {
                VendorId = x.VendorId,
                VendorCode = x.VendorCode,
                VendorName = x.VendorName,
                VendorCompany = x.VendorCompany,
                AccountHolderName = x.AccountHolderName,
                AccountNumber = x.AccountNumber,
                IBAN = x.IBAN,
                BankNameAr = x.BankNameAr,
                BankNameEn = x.BankNameEn,
                BankSwift = x.BankSwift,
                InvoiceCount = 0,
                InvoiceTotal = 0,
                Invoices = new List<InvoiceDTO.ForVendor>()
            }).DistinctBy(x => x.VendorId).ToList();
            foreach (var vendor in vendors)
            {
                var invoices = result.Where(x => x.VendorId == vendor.VendorId)
                    .Select(x => new InvoiceDTO.ForVendor()
                    {
                        Id = x.Id,
                        Key = x.Key,
                        Code = x.Code,
                        Type = x.Type,
                        Status = x.Status,
                        Amount = x.Amount,
                        Subtotal = x.Subtotal,
                        Fees = x.Fees,
                        Total = x.Total,
                        CreateDate = x.CreateDate,
                        PaymentDate = x.PaymentDate,
                        CurrencyCode = x.CurrencyCode,
                        CustomerName = x.CustomerName
                    }).DistinctBy(x => x.Id).ToList();
                if (invoices != null && invoices.Count > 0)
                {
                    vendor.Invoices = invoices;
                    vendor.InvoiceCount = invoices.Count;
                    vendor.InvoiceTotal = invoices.Sum(x => x.Subtotal);
                }
            }
            return new ResponseDTO() { IsValid = true, ErrorKey = "", Response = vendors };
        }
        return new ResponseDTO() { IsValid = false, ErrorKey = "EmptyResult", Response = null };
    }
}