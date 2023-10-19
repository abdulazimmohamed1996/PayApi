namespace PayArabic.Core.DTO;
public class AttachmentDTO
{
    public long Id { get; set; }
    //public string EntityType { get; set; }
    public long EntityId { get; set; }
    public string EntityFieldName { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Notes { get; set; }
    public string Attachment { get; set; }
}
public class AttachmentDTOLight
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string EntityFieldName { get; set; }
}