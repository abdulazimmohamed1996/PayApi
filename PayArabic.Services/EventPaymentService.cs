using PayArabic.Core;
using PayArabic.DAO;
using System.Text;

namespace PayArabic.Services;

public class EventPaymentService
{
    public static void Process()
    {
        var configs = new ConfigurationDao().GetByIntegrationCode(IntegrationCode.Knet.ToString());
        string password = string.Empty, tranportalId= string.Empty, apiGatewayUrl;
        if (AppSettings.Instance.KnetPaymentSettings.Mode == "test")
            apiGatewayUrl = AppSettings.Instance.KnetPaymentSettings.ApiTestGatewayUrl;
        else apiGatewayUrl = AppSettings.Instance.KnetPaymentSettings.ApiGatewayUrl;

        string url = apiGatewayUrl + "/kpg/tranPipe.htm?param=tranInit";

        string prefix = "";
        if (AppSettings.Instance.KnetPaymentSettings.Mode == "test")
            prefix = "test_";
        foreach (var conf in configs)
        {
            if (conf.Name.Equals(prefix + "id")) { tranportalId = conf.Value; }
            else if (conf.Name.Equals(prefix + "password")) { password = conf.Value; }
        }

        var transactions = new TransactionDao().GetPendingTransactions(IntegrationCode.Knet.ToString(), 30);
        foreach (var transaction in transactions)
        {

            StringBuilder xml = new ();
            xml.AppendLine("<id>" + tranportalId + "</id>");
            xml.AppendLine("<password>" + password + "</password>");
            xml.AppendLine("<action>8</action>");
            xml.AppendLine("<amt>" + String.Format("{0:#,0.000}", transaction.Amount) + "</amt>");
            xml.AppendLine("<transId>" + transaction.TrackId + "</transId>");
            xml.AppendLine("<udf5>TrackID</udf5>");
            xml.AppendLine("<trackid>" + transaction.TrackId + "</trackid>");

            var requestContent = new StringContent(xml.ToString());
            var client = new HttpClient();

            HttpResponseMessage res = client.PostAsync(url, requestContent).Result;
            HttpContent responseContent = res.Content;
            var xml_response = "";
            using (var reader = new StreamReader(responseContent.ReadAsStreamAsync().Result))
                xml_response = reader.ReadToEndAsync().Result;
            System.Xml.Linq.XDocument doc = System.Xml.Linq.XDocument.Parse("<root>" + xml_response + "</root>");
            //saving transaction            
            bool success = doc.Root!.Element("result") != null && (doc.Root.Element("result")!.Value == "CAPTURED" || doc.Root.Element("result")!.Value == "SUCCESS");

            if (success)
            {
                var invResult = new InvoiceDao().GetInfo(Convert.ToInt64(doc.Root.Element("udf1")!.Value), IntegrationCode.Knet.ToString());
                var invoice = invResult.Response;
                if (invoice.Status == InvoiceStatus.Paid.ToString()) continue;

                transaction.Status = TransactionStatus.Success.ToString();
                transaction.Response = xml_response;
                transaction.TransactionId = doc.Root.Element("tranid")!.Value;
                transaction.PaymentId = doc.Root.Element("payid")!.Value;
                transaction.ReferenceId = doc.Root.Element("ref")!.Value;
                new TransactionDao().Update(transaction, InvoiceStatus.Paid.ToString(), IntegrationCode.Knet.ToString());

                if (doc.Root.Element("udf2")!.Value.ToLower().Equals(EntityType.Invoice.ToString().ToLower())
                    || doc.Root.Element("udf2")!.Value.ToLower().Equals(EntityType.Order.ToString().ToLower())
                    || doc.Root.Element("udf2")!.Value.ToLower().Equals(EntityType.PaymentLink.ToString().ToLower())
                    )
                {
                    //add invoice paid notification                   
                    //new NotificationDao(config).InvoicePaid(invoice);

                    // in case of installment plan then update installment plan
                    //if (doc.Root.Element("udf2")!.Value.ToLower().Equals(EntityType.InstallmentPlan.ToString().ToLower()))
                    //    new InstallmentPlanDao(config).UpdatePlanAfterPayment(transaction.Invoice_Id, IntegrationCode.Knet.ToString());
                }
            }
        }
    }
}
