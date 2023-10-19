using PayArabic.Core.Model;
using System.Data;

namespace PayArabic.DAO;

public class IntegrationDao : BaseDao, IIntegrationDao
{
    private readonly IConfigurationDao _configDao;
    private readonly ITransactionDao _transDao;
    private readonly IInvoiceDao _invoiceDao;
    public IntegrationDao(IConfigurationDao configDao, ITransactionDao transDao, IInvoiceDao invoiceDao)
    {
        _configDao = configDao;
        _transDao = transDao;
        _invoiceDao = invoiceDao;
    }
    public IEnumerable<IntegrationDTO> GetAll()
    {
        string query = @"   SELECT Id, NameEn, NameAr, [Type], Code, InActive
	                        FROM Integration
	                        WHERE DeletedBy IS NULL AND InActive = 0 ";
        var result = DB.Query<IntegrationDTO>(query).ToList();
        return result;
    }
    public IntegrationDTO GetByCode(string code)
    {
        string query = @"   SELECT Id, NameEn, NameAr, [Type], Code, InActive
	                        FROM Integration
	                        WHERE DeletedBy IS NULL AND InActive = 0 
                                AND Code = " + code;
        var result = DB.Query<IntegrationDTO>(query).FirstOrDefault();
        return result;
    }
    public string KentInit(InvoiceDTO.Info inv)
    {
        var configs = _configDao.GetByIntegrationCode(IntegrationCode.Knet.ToString());
        string termResourceKey = default, tranportalId = default, responseUrl = default, errorUrl = default;
        string url = string.Empty;
        string prefix = "";
        if (AppSettings.Instance.KnetPaymentSettings.Mode == "test")
            prefix = "test_";
        string name;
        string trackid = DateTime.Now.ToFileTime().ToString();
        foreach (var conf in configs)
        {
            if (conf.Name.Equals(prefix + "termResourceKey")) { termResourceKey = conf.Value; continue; }
            else if (conf.Name.Equals(prefix + "id")) { tranportalId = conf.Value; }
            else if (conf.Name.Equals(prefix + "responseURL")) { responseUrl = conf.Value; }
            else if (conf.Name.Equals(prefix + "errorURL")) { errorUrl = conf.Value; }
            if (conf.Name.Equals(prefix + "amt")) conf.Value = Math.Round((Decimal)(inv.Fees + inv.Subtotal), 3, MidpointRounding.AwayFromZero).ToString();
            else if (conf.Name.Equals(prefix + "trackid")) conf.Value = trackid;
            else if (conf.Name.Equals(prefix + "udf1")) conf.Value = inv.Id.ToString();
            else if (conf.Name.Equals(prefix + "udf2")) conf.Value = inv.Type.ToString();
            else if (conf.Name.Equals(prefix + "udf3")) conf.Value = inv.OriginalInvoiceId.ToString();
            else if (conf.Name.Equals(prefix + "udf4")) conf.Value = inv.Code;


            if (AppSettings.Instance.KnetPaymentSettings.Mode == "test" && conf.Name.Contains("test_"))
            {
                name = conf.Name.Replace("test_", "");
                url += (string.IsNullOrEmpty(url)) ? name + "=" + conf.Value : "&" + name + "=" + conf.Value;
            }
            else if (AppSettings.Instance.KnetPaymentSettings.Mode == "live" && !conf.Name.Contains("test_"))
            {
                url += (string.IsNullOrEmpty(url)) ? conf.Name + "=" + conf.Value : "&" + conf.Name + "=" + conf.Value;
            }

        }
        var prepparam = Utility.EncryptString(url, termResourceKey);
        string apiGatewayUrl = "";
        if (AppSettings.Instance.KnetPaymentSettings.Mode == "test")
            apiGatewayUrl = AppSettings.Instance.KnetPaymentSettings.ApiTestGatewayUrl;
        else apiGatewayUrl = AppSettings.Instance.KnetPaymentSettings.ApiGatewayUrl;

        var data = apiGatewayUrl + "/kpg/PaymentHTTP.htm?param=paymentInit&trandata=" + prepparam
                + "&tranportalId=" + tranportalId + "&responseURL=" + responseUrl + "&errorURL=" + errorUrl;

        var integration = GetByCode(IntegrationCode.Knet.ToString());

        // init transaction
        TransactionDTO.Info entity = new TransactionDTO.Info();
        entity.RequestType = TransactionRequestType.Capture.ToString();
        entity.Type = TransactionType.Invoice.ToString();
        entity.InvoiceId = inv.Id;
        entity.PaymentGateway = integration.NameEn;
        entity.PaymentGatewayCode = integration.Code;
        entity.TransactionId = "";
        entity.TrackId = trackid;
        entity.PaymentId = "";
        entity.ReferenceId = "";
        entity.Amount = inv.Amount;
        entity.IpAddress = "";
        entity.CurrencyCode = inv.CurrencyCode;
        entity.Status = TransactionStatus.Pending.ToString();
        entity.Response = "";
        _transDao.Insert(entity, InvoiceStatus.Unpaid.ToString(), IntegrationCode.Knet.ToString());

        return data;
    }
    public string KnetProcess(string ErrorText, string paymentid, string trackid
        , string Error, string result, string postdate, string tranid, string auth, string avr
        , string REF, string amt, string udf1, string udf2, string udf3, string udf4, string udf5, string trandata, string ipAddress)
    {
        //knetInput = "CC1464F1001FAE899CB832D1F6AF7429E2652BFCABE981E2FB4DDD65E169B60798752D1A041E74626D6D90F37DDE4EDDD49689ED68317DB78897CA83E1D6B71EF8A0D7E68E33EE2AC7BD58506547C65DF82F54E2472E518780E6E6C7C2F9304F37E826B50823A285BC744F2D658C36BDB574380E36EB048B7423E399E9AFC7B8DC1F7DB1EDB62B9041451F99A7B26EC084DF093697FBD98414CAD3A4840F4CD41E14BC7EAD00881F72A931233A4D4AC54C944D119CFCC830F54B24102C19EDE607A0F7BD876AE3FD58255F8C020357022EA2D07EF0555F2083D658F378F5A4D1664561C484435792315E77FA29BDC7D3219A4C99D082EEE7D925196318CEE145";
        string knetInput = trandata;
        var configs = _configDao.GetByIntegrationCode(IntegrationCode.Knet.ToString());
        string prefix = "";
        if (AppSettings.Instance.KnetPaymentSettings.Mode == "test")
            prefix = "test_";
        string termResourceKey = configs.FirstOrDefault(x => x.Name.Equals(prefix + "termResourceKey")).Value;
        var knetData = Utility.DecryptString(knetInput, termResourceKey);
        if (!string.IsNullOrEmpty(knetData))
        {
            KnetResponse response = Utility.KnetReadData(knetData);
            string postedParameters = "paymentid=" + paymentid + "&trackid=" + trackid;
            string postedData = "paymentid=" + response.PaymentId + "&trackid=" + response.TrackId;
            if (!postedParameters.ToLower().Equals(postedData.ToLower()))
                return AppSettings.Instance.InvoicePublicUrl + response.UDF1 + "/" + TransactionStatus.Failed.ToString();

            var integration = GetByCode(IntegrationCode.Knet.ToString());
            var invoice = _invoiceDao.GetInfo(Convert.ToInt64(response.UDF1), IntegrationCode.Knet.ToString());

            TransactionDTO.Info entity;
            entity = _transDao.GetPendingByInvoiceId(invoice.Response.Id, IntegrationCode.Knet.ToString());

            if (entity == null) 
                entity = new TransactionDTO.Info();

            entity.RequestType = TransactionRequestType.Capture.ToString();
            entity.Type = TransactionType.Invoice.ToString();
            entity.InvoiceId = Convert.ToInt64(response.UDF1);
            entity.PaymentGateway = integration.NameEn;
            entity.PaymentGatewayCode = integration.Code;
            entity.TransactionId = response.TranId;
            entity.TrackId = response.TrackId;
            entity.PaymentId = response.PaymentId;
            entity.ReferenceId = response.REF;
            entity.Amount = invoice.Response.Total;
            entity.IpAddress = ipAddress;
            entity.CurrencyCode = invoice.Response.CurrencyCode;

            if (response.Result.ToLower().Replace("+", "").Equals("captured"))
            {
                entity.Status = TransactionStatus.Success.ToString();
                entity.Response = knetData;
                if (entity.Id == 0)
                    _transDao.Insert(entity, InvoiceStatus.Paid.ToString(), IntegrationCode.Knet.ToString());
                else
                    _transDao.Update(entity, InvoiceStatus.Paid.ToString(), IntegrationCode.Knet.ToString());

                if (response.UDF2.ToLower().Equals(EntityType.Invoice.ToString().ToLower())
                    || response.UDF2.ToLower().Equals(EntityType.Order.ToString().ToLower())
                    || response.UDF2.ToLower().Equals(EntityType.PaymentLink.ToString().ToLower())
                    )
                {
                    //add invoice paid notification                   
                    //new NotificationDao(_config).InvoicePaid(invoice);

                    // redirect to success
                    return AppSettings.Instance.InvoicePublicUrl + invoice.Response.Key + "/" + TransactionStatus.Success.ToString();
                }
                //else if (response.UDF2.ToLower().Equals(EntityType.Refund.ToString().ToLower()))
                //{
                //    var refundEntity = new RefundDao(_config).getInvoiceRefund(invoice.OriginalInvoice_Id);
                //    Invoice.DTORefundData refundInvoice = new InvoiceDao(_config).GetByIdForRefund(refundEntity.Invoice_Id, invoice.Vendor_Id);
                //    bool res = Refund(refundEntity.CustomerCredit_Amount, refundEntity.CustomerCredit_Amount, refundEntity.VendorDebit_Amount, refundEntity.Fees_Amount, refundEntity.FeesAppliedOn, refundInvoice, invoice.Vendor_Id, ipAddress, entity.Amount);
                //    if (res)
                //        return _config["AppSettings:InvoiceUrl"] + response.UDF3 + "/" + TransactionStatus.RefundedSuccess.ToString();
                //    else
                //        return _config["AppSettings:InvoiceUrl"] + response.UDF3 + "/" + TransactionStatus.Failed.ToString();
                //}
            }
            else
            {
                entity.Status = TransactionStatus.Failed.ToString();
                entity.Response = knetData;
                _transDao.Insert(entity, InvoiceStatus.Unpaid.ToString(), IntegrationCode.Knet.ToString());
                return AppSettings.Instance.InvoicePublicUrl + invoice.Response.Key + "/" + TransactionStatus.Failed.ToString();
            }
        }
        return TransactionStatus.Failed.ToString();
    }
}