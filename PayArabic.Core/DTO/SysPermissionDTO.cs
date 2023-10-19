namespace PayArabic.Core.DTO;

public class SysPermissionDTO
{
    public class Module
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Permission { get; set; }
        public bool Auditable { get; set; }
        public bool InActive { get; set; }
        public List<Function> Functions { get; set; }
    }
    public class Function
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Permission { get; set; }
        public bool Auditable { get; set; }
        public bool InActive { get; set; }

    }
}