namespace PayArabic.Core;

public class ListOptions
{
    public bool LoadingAll { get; set; } = false;
    public int Skip { get; set; }
    public int Take { get; set; }
    public Sort Sort { get; set; }
}

public class Sort
{
    public string Selector { get; set; }
    public bool Desc { get; set; }
}
