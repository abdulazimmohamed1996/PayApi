using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PayArabic.Core;
using PayArabic.Core.DTO;
using PayArabic.Core.Interfaces;
using PayArabic.DAO;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Web;
using System.Xml;

namespace PayArabic.Services;

public class EventService
{
    public static void Process()
    {
        IEventDao _dao = new EventDao();
        var list = _dao.Get();
        foreach (var entity in list)
        {
            try
            {
                if (entity.SendType.Trim() == SendType.SMS.ToString())
                {
                    SMS(entity);
                }
                else if (entity.SendType == SendType.Email.ToString())
                {
                    Email(entity);
                }
                else
                {
                    _dao.Update(entity.Id, 1);
                }
            }
            catch
            {
                _dao.Update(entity.Id, 1);
            }
        }
    }
    private static void SMS(EventDTO entity)
    {
        Console.WriteLine("Sending SMS...");
        string msgBody;

        if (entity.EntityType == EntityType.Invoice.ToString() && entity.Type == EventType.InvoiceCreated.ToString())
        {
            var res = new InvoiceDao().GetInfo(entity.EntityId, "", true);
            var inv = res.Response;
            var companyName = (inv.VendorCompany != "") ? inv.VendorCompany : inv.VendorName;

            var smsBody = new ConfigurationDao().GetConfigurationByName("InvoiceCreatedSMSBody_" + inv.Lang);
            msgBody = smsBody.Replace("%INVOICE_CODE%", inv.Code);
            msgBody = msgBody.Replace("%VENDOR_NAME%", companyName);
            msgBody = msgBody.Replace("%INVOICE_AMOUNT%", String.Format("{0:#,0.000}", inv.Amount));
            msgBody = msgBody + ' ' + AppSettings.Instance.InvoicePublicUrl + inv.Key;
            Console.WriteLine("SMS To:" + inv.CustomerMobile);
            SendSMS(entity.Id, inv.CustomerMobile, msgBody, inv.SmsSender);
        }
        else if (entity.EntityType == EntityType.User.ToString()
                && (entity.Type == EventType.UserRegister.ToString() || entity.Type == EventType.UserForgetPassword.ToString()))
        {
            var user = new UserDao().GetLightInfo(entity.EntityId);
            msgBody = "Your Pin Key: " + user.MobileActivationKey;
            Console.WriteLine("SMS To:" + user.Mobile);
            SendSMS(entity.Id, user.Mobile, msgBody);
        }
    }
    private static void Email(EventDTO entity)
    {
        if (entity.EntityType == EntityType.Invoice.ToString())
        {
            var res = new InvoiceDao().GetInfo(entity.EntityId, "", true);
            var inv = res.Response;
            var entityName = !string.IsNullOrEmpty(inv.VendorCompany) ? inv.VendorCompany : inv.VendorName;

            if (entity.Type == EventType.InvoiceCreated.ToString())
            {
                var tempData = new
                {
                    inv.CustomerName,
                    VendorName = entityName,
                    InvoiceCode = inv.Code,
                    Subtotal = String.Format("{0:#,0.000}", inv.Subtotal),
                    InvoicePaymentURL = AppSettings.Instance.InvoicePublicUrl + inv.Key
                };
                Console.WriteLine("Email  To:" + inv.CustomerEmail);

                SendEmail("InvoiceCreated_Template_Id", entity.Id, inv.CustomerName, inv.CustomerEmail, tempData);
            }
            else if (entity.Type == EventType.InvoicePaid.ToString())
            {
                var invoiceRes = new InvoiceDao().GetForPaymentByKey(inv.Key);
                var paidInv = invoiceRes.Response;
                var tempData = new
                {
                    paidInv.CustomerName,
                    VendorName = entityName,
                    InvoiceCode = paidInv.Code,
                    Subtotal = String.Format("{0:#,0.000}", paidInv.Amount),
                    Fees = String.Format("{0:#,0.000}", (paidInv.Total - paidInv.Amount)),
                    Total = String.Format("{0:#,0.000}", paidInv.Total),
                    InvoicePaymentURL = AppSettings.Instance.InvoicePublicUrl + inv.Key
                };
                Console.WriteLine("Email  To:" + inv.VendorEmail);
                SendEmail("InvoicePaid_Template_Id", entity.Id, entityName, inv.VendorEmail, tempData);

                if (!string.IsNullOrEmpty(paidInv.CustomerEmail))
                {
                    Console.WriteLine("Email  To Customer:" + paidInv.CustomerEmail);
                    SendEmail("InvoicePaid_Template_Id", entity.Id, entityName, paidInv.CustomerEmail, tempData);
                }
            }
        }
        else if (entity.EntityType == EntityType.User.ToString())
        {
            var user = new UserDao().GetLightInfo(entity.EntityId);
            if (entity.Type == EventType.UserRegister.ToString())
            {
                var tempData = new
                {
                    CustomerName = user.Name,
                    ConfirmEmailURL = AppSettings.Instance.ConfirmEmailUrl + HttpUtility.UrlEncode(user.EmailActivationKey)
                };
                SendEmail("UserRegister_Template_Id", entity.Id, user.Name, user.Email, tempData);
            }
            else if (entity.Type == EventType.UserForgetPassword.ToString())
            {
                var tempData = new { CustomerName = user.Name, ResetPasswordURL = AppSettings.Instance.ResetPasswordUrl + HttpUtility.UrlEncode(user.EmailActivationKey) };
                SendEmail("UserForgetPassword_Template_Id", entity.Id, user.Name, user.Email, tempData);
            }
            else if (entity.Type == EventType.UserForgetPasswordRecovery.ToString())
            {
                var tempData = new { CustomerName = user.Name, NewPassword = entity.Data };
                SendEmail("UserRecoverPassword_Template_Id", entity.Id, user.Name, user.Email, tempData);
            }
        }
    }
    public static void SendSMS(int eventId, string mobile, string msgBody, string SmsSender = "")
    {
        var configs = new ConfigurationDao().GetByIntegrationType(IntegrationType.SMS.ToString());
        using var client = new HttpClient();

        List<KeyValuePair<string, string>> list = new();
        foreach (var conf in configs)
        {
            if (conf.Name.Equals("recipientnumbers")) conf.Value = mobile;
            else if (conf.Name.Equals("messagebody")) conf.Value = msgBody;
            else if (conf.Name.Equals("sendertext") && !string.IsNullOrEmpty(SmsSender)) conf.Value = SmsSender;

            list.Add(new KeyValuePair<string, string>(conf.Name, conf.Value));
        }
        var content = new FormUrlEncodedContent(list);
        Console.WriteLine("http://smsbox.com/smsgateway/services/messaging.asmx/Http_SendSMS");
        Console.WriteLine(content.ToString());
        var result = client.PostAsync("http://smsbox.com/smsgateway/services/messaging.asmx/Http_SendSMS", content);
        var resultContent = result.Result.Content.ReadAsStringAsync();

        XmlDocument doc = new();
        doc.LoadXml(resultContent.Result);
        dynamic json = JsonConvert.SerializeXmlNode(doc);
        dynamic parsedJson = JObject.Parse(json);
        string msg = (string)parsedJson["SendingSMSResult"].Message;
        Console.WriteLine(msg);
        if (msg != "No valid numbers found.")
        {
            new EventDao().Update(eventId);
        }
        else
        {
            new EventDao().Update(eventId, 1);
        }

    }
    public static void SendEmail(string templateKey, int eventId, string name, string email, dynamic tempData)
    {
        List<ConfigurationDTO> configs = new ConfigurationDao().GetByIntegrationType(IntegrationType.Email.ToString());
        string template_Id = configs.FirstOrDefault(x => x.Name.Equals(templateKey))!.Value;
        string email_APIKey = configs.FirstOrDefault(x => x.Name.Equals("Email_APIKey"))!.Value;
        string email_Subject = "";
        string email_From = configs.FirstOrDefault(x => x.Name.Equals("Email_From"))!.Value;
        string email_FromName = configs.FirstOrDefault(x => x.Name.Equals("Email_FromName"))!.Value;

        var from = new EmailAddress(email_From, email_FromName);
        var to = new EmailAddress(email, name);
        var plainTextContent = "";
        var htmlContent = "";

        var client = new SendGridClient(email_APIKey);
        var msg = MailHelper.CreateSingleEmail(from, to, email_Subject, plainTextContent, htmlContent);
        msg.SetTemplateId(template_Id);

        msg.SetTemplateData(tempData);
        var response = client.SendEmailAsync(msg).Result;

        var serializedJson = JsonConvert.SerializeObject(response);
        dynamic parsedJson = JObject.Parse(serializedJson);
        int code = (int)parsedJson["StatusCode"].Value;

        if (code == 202)
        {
            new EventDao().Update(eventId);
        }
        else
        {
            new EventDao().Update(eventId, 1);
        }
    }
}
