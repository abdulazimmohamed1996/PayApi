namespace PayArabic.Core.DTO;

public class SysModuleDTO
{
    public long ModuleUserId { get; set; }
    public long ModuleId { get; set; }
    public string ModuleName { get; set; }
    public string ModuleDisplayName { get; set; }
    public string ModulePermission { get; set; }
    public bool ModuleAuditable { get; set; }
    public bool ModuleInActive { get; set; }
    public List<SysFunctionDTO> Functions { get; set; }
}
public class SysFunctionDTO
{
    public long FunctionUserId { get; set; }
    public long FunctionId { get; set; }
    public string FunctionName { get; set; }
    public string FunctionDisplayName { get; set; }
    public string FunctionPermission { get; set; }
    public bool FunctionAuditable { get; set; }
    public bool FunctionInActive { get; set; }

}
