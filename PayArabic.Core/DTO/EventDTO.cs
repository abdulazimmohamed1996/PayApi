namespace PayArabic.Core.DTO;

public class EventDTO
{
    public int Id { get; set; }
    public string Type { get; set; } // 1=Insert, 2=Update
    public string SendType { get; set; } //  1=SMS, 2=Email
    public string EntityType { get; set; } // User, Invoice, Deposit
    public int EntityId { get; set; }
    public bool Sent { get; set; }
    public string Data { get; set; }
    public string ScheduleDate { get; set; }
}
