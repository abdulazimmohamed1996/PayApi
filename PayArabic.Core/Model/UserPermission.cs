namespace PayArabic.Core.Model;
public class UserPermission : BaseEntity
{
    public long UserId { get; set; }
    public long ModuleId { get; set; }
    public string ModulePermission { get; set; }
    public bool ModuleAuditable { get; set; }
    public long FunctionId { get; set; }
    public string FunctionPermission { get; set; }
    public bool FunctionAuditable { get; set; }
}
