namespace PayArabic.Core.Interfaces;

public interface IIntegrationDao
{
    IEnumerable<IntegrationDTO> GetAll();
    string KentInit(InvoiceDTO.Info inv);
    string KnetProcess(string ErrorText, string paymentid, string trackid
        , string Error, string result, string postdate, string tranid, string auth, string avr
        , string Ref, string amt, string udf1, string udf2, string udf3, string udf4, string udf5, string trandata, string ipAddress);
}

