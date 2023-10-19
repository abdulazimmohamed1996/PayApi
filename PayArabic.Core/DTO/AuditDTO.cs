namespace PayArabic.Core.DTO;
public class AuditDTO
{
    public class AuditList:BaseListDTO
    {
        public long Id { get; set; }
        public string IPAddress { get; set; }
        public string User { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string Module { get; set; }
        public string Function { get; set; }
        public string CreateDate { get; set; }
    }
    public class AuditGetById: AuditList
    {
        public string Notes { get; set; }
    }
    public class AuditInsert
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string IPAddress { get; set; }
        public string Module { get; set; }
        public string Function { get; set; }
        public string Notes { get; set; }
    }
}