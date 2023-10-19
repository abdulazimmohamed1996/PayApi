namespace PayArabic.Core.Interfaces;
public interface IAuditDao
{
    IEnumerable<AuditDTO.AuditList> GetAll(string module, string function, string listOptions = null);
    AuditDTO.AuditGetById GetById(long id);
    void Insert(AuditDTO.AuditInsert entity);
}