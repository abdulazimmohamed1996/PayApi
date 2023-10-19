namespace PayArabic.Core.Model;
public class SysModule : BaseEntity
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Permission { get; set; }
    public bool Auditable { get; set; }
}
