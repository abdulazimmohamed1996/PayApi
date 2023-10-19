namespace PayArabic.Core.Base;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public bool InActive { get; set; }
    public int CreatedBy { get; set; }
    public int? DeletedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
    public DateTime? DeleteDate { get; set; }
}
