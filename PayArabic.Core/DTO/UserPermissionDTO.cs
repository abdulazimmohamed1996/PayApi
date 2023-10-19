namespace PayArabic.Core.DTO;

public class UserPermissionDTO
{
    public long UserId { get; set; }
    public long ModuleId { get; set; }
    public string ModuleName { get; set; }
    public string ModuleDisplayName { get; set; }
    //public string ModulePermission { get; set; }
    //public bool ModuleAuditable { get; set; }
    //public bool ModuleInActive { get; set; }
    public long FunctionId { get; set; }
    public string FunctionName { get; set; }
    public string FunctionDisplayName { get; set; }
    public string FunctionPermission { get; set; }
    public bool FunctionAuditable { get; set; }
    public bool FunctionInActive { get; set; }
}
public class UserModuleDTO
{
    public long UserId { get; set; }
    public long ModuleId { get; set; }
    public string Module { get; set; }
    public List<UserFunctionDTO> Functions { get; set; }
}
public class UserFunctionDTO
{
    public long UserId { get; set; }
    public long FunctionId { get; set; }
    public string Function { get; set; } 
    public string Permission { get; set; }
    //public bool Auditable { get; set; }
    public bool InActive { get; set; }
}
public class SaveUserPermissionDTO
{
    public long UserId { get; set; }
    public long ModuleId { get; set; }
    public long FunctionId { get; set; }
    public string FunctionPermission { get; set; }
    public bool FunctionInActive { get; set; }
}