namespace PayArabic.Core.Model;
public class SysFunction : BaseEntity
{
    public long ModuleId { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Permission { get; set; }
    public bool Auditable { get; set; }
}
