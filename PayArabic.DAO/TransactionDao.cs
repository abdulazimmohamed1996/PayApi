using System.Text;

namespace PayArabic.DAO;

public class TransactionDao : BaseDao, ITransactionDao
{
    public object Insert(TransactionDTO.Info entity, string invoiceStatus, string paymentMethodCode, string refundQuery = "")
    {
        StringBuilder query = new StringBuilder();

        query.AppendLine(@" BEGIN TRANSACTION [TransactionInsert]
                            BEGIN TRY
                                INSERT INTO [Transaction]
                                    (Request_Type, [Type], Invoice_Id, [Status]
                                    , PaymentGateway, PaymentGatewayCode, TransactionID, TrackId, ReferenceId, PaymentId
                                    , Amount, CurrencyCode,IpAddress
                                    , Response, Deleted, InActive, CreatedBy, CreateDate, UpdateDate)
                                VALUES ('" + entity.RequestType + "', '" + entity.Type + "', " + entity.InvoiceId + ", '" + entity.Status + "'" + @"
                                    , N'" + entity.PaymentGateway + "', N'" + entity.PaymentGatewayCode + "', N'" + entity.TransactionId + "', N'" + entity.TrackId + "', N'" + entity.ReferenceId + "', N'" + entity.PaymentId + "'" + @"                                        
                                    , '" + entity.Amount + "', N'" + entity.CurrencyCode + "', N'" + entity.IpAddress + "'" + @"
                                    , N'" + Utility.Wrap(entity.Response) + @"', 0, 0, -1, GETDATE(), GETDATE() );");
        if (invoiceStatus.ToLower().Equals(InvoiceStatus.Paid.ToString().ToLower()))
        {
            // Update invoice status and fees_amount
            query.AppendLine(@" UPDATE inv 
	                                SET inv.[Status] = '" + invoiceStatus + @"'
		                                , inv.Fees = pm.Fees
		                                , inv.Total = pm.Total
		                                , inv.Subtotal = pm.Subtotal
                                        , inv.PaymentDate = GETDATE()
                                FROM Invoice inv 
	                                CROSS APPLY dbo.GetInvoicePaymentMethod(inv.Id,'" + entity.PaymentGatewayCode + @"') pm
                                WHERE inv.Id = " + entity.InvoiceId + @"");
            // Increase vendor awaiting balance

            query.AppendLine(@" IF " + entity.InvoiceId + @" > 0
                                BEGIN
                                    INSERT INTO [Event] ([Type], SendType
                                        , Entity_Type, Entity_Id
                                        , Sent, InActive, CreatedBy, CreateDate, UpdateDate)
                                    VALUES ('" + EventType.InvoicePaid + @"', '" + SendType.Email + @"'
                                        , N'" + EntityType.Invoice + @"', " + entity.InvoiceId + @"
                                        , 0, 0, 0, GETDATE(), GETDATE());
                                END");

        }
        if (!string.IsNullOrEmpty(refundQuery))
            query.AppendLine(refundQuery);


        query.AppendLine(@" COMMIT TRANSACTION [TransactionInsert]
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [TransactionInsert]
                            END CATCH");
        return DB.ExecuteScalar(query.ToString());
    }
    public object Update(TransactionDTO.Info entity, string invoiceStatus, string paymentMethodCode, string refundQuery = "")
    {
        StringBuilder query = new StringBuilder();

        query.AppendLine(@" BEGIN TRANSACTION [TransactionUpdate]
                            BEGIN TRY
                                UPDATE [Transaction] SET 
                                    [Status] = '" + entity.Status + "'" + @", PaymentGateway = N'" + entity.PaymentGateway + @"',
                                    TransactionID=N'" + entity.TransactionId + @"', TrackId=N'" + entity.TrackId + @"',
                                    ReferenceId= N'" + entity.ReferenceId + @"', PaymentId= N'" + entity.PaymentId + @"',
                                    Amount='" + entity.Amount + @"', CurrencyCode=N'" + entity.CurrencyCode + @"',IpAddress=N'" + entity.IpAddress + @"',
                                    Response=N'" + Utility.Wrap(entity.Response) + @"', UpdateDate=GETDATE()
                                WHERE Id=" + entity.Id);
        if (invoiceStatus.ToLower().Equals(InvoiceStatus.Paid.ToString().ToLower()))
        {
            // Update invoice status and fees_amount
            query.AppendLine(@" UPDATE inv 
	                                SET inv.[Status] = '" + invoiceStatus + @"'
		                                , inv.Fees = pm.Fees
		                                , inv.Total = pm.Total
		                                , inv.Subtotal = pm.Subtotal
                                        , inv.PaymentDate = GETDATE()
                                FROM Invoice inv 
	                                CROSS APPLY dbo.GetInvoicePaymentMethod(inv.Id,'" + entity.PaymentGatewayCode + @"') pm
                                WHERE inv.Id = " + entity.InvoiceId + @"");
            // Increase vendor awaiting balance

            query.AppendLine(@" IF " + entity.InvoiceId + @" > 0
                                BEGIN
                                    INSERT INTO [Event] ([Type], SendType, Entity_Type, Entity_Id, Sent, InActive, CreatedBy, CreateDate, UpdateDate)
                                    VALUES ('" + EventType.InvoicePaid + @"', '" + SendType.Email + @"', N'" + EntityType.Invoice + @"', " + entity.InvoiceId + @", 0, 0, 0, GETDATE(), GETDATE())
                                END");


        }
        if (!string.IsNullOrEmpty(refundQuery))
            query.AppendLine(refundQuery);
        query.AppendLine(@" COMMIT TRANSACTION [TransactionUpdate]
                            END TRY
                            BEGIN CATCH
                                ROLLBACK TRANSACTION [TransactionUpdate]
                            END CATCH");
        return DB.ExecuteScalar(query.ToString());
    }
    public TransactionDTO.Info GetPendingByInvoiceId(long invoiceId, string paymentGatewayCode)
    {
        StringBuilder query = new StringBuilder();
        query.AppendLine(@" SELECT *
                            FROM [Transaction]                                
                            WHERE ISNULL(DeletedBy, 0) = 0 
                                AND InActive = 0 
                                AND [Status] = 'Pending' 
                                AND PaymentGatewayCode='" + paymentGatewayCode + @"'
                                AND InvoiceId = " + invoiceId + @" 
                            ORDER BY id DESC");
        TransactionDTO.Info trans = DB.Query<TransactionDTO.Info>(query.ToString()).FirstOrDefault();
        return trans;
    }
    public List<TransactionDTO.Info> GetPendingTransactions(string paymentGatewayCode, int minutesFrom = 0)
    {
        StringBuilder query = new StringBuilder();
        query.AppendLine(@" SELECT t.*
                            FROM [Transaction] t
                                INNER JOIN [Invoice] i ON i.Id = t.InvoiceId AND i.Status = 'Unpaid'
                            WHERE ISNULL(t.DeletedBy, 0) = 0 
                                AND t.InActive = 0 
                                AND t.[Status] = 'Pending' 
                                AND t.PaymentGatewayCode='" + paymentGatewayCode + @"'
                                AND t.CreateDate > DATEADD(MINUTE, -" + minutesFrom.ToString() + @", GETDATE())
                            ORDER BY t.Id DESC ");
        return DB.Query<TransactionDTO.Info>(query.ToString()).ToList();
    }
}