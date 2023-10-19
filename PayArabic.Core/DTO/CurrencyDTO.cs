namespace PayArabic.Core.DTO;
public class CurrencyDTO
{
    public class CurrencyList : BaseListDTO
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public bool IsBase { get; set; }
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public string SymboleEn { get; set; }
        public string SymboleAr { get; set; }
        public bool InActive { get; set; }
    }
    public class CurrencyGetById : CurrencyList
    {
        public float DecimalPlacement { get; set; }
        public float ConversionRate { get; set; }
    }
    public class CurrencyInsert
    {
        public bool IsBase { get; set; }
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public string SymboleEn { get; set; }
        public string SymboleAr { get; set; }
        public float DecimalPlacement { get; set; }
        public float ConversionRate { get; set; }
    }
    public class CurrencyUpdate : CurrencyInsert
    {
        public long Id { get; set; }
        public bool InActive { get; set; }
    }
}