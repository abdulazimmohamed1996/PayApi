namespace PayArabic.Core.DTO;
public class InvoiceItemDTO
{
    public long Id { get; set; }
    public string Name { get; set; }
    public int Quantity { get; set; }
    public float Amount { get; set; }
}