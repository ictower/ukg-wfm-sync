public class ActivityRecord
{
    public long ActivityId { get; set; }

    public string ActivityName { get; set; } = "";

    public string ActivityDescription { get; set; } = "";

    public long? LaborCategoryTypeId { get; set; }

    public string LaborCategoryType { get; set; } = "";

    public long? LaborCategoryId { get; set; }

    public string LaborCategoryName { get; set; } = "";
}
