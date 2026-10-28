namespace ukg_wfm_sync.Models;

public class TimecardRecord
{
    public string EmployeeNumber { get; set; } = "";
    public string ApplyDate { get; set; } = "";
    public double HoursWorked { get; set; }
    public string ProjectCode { get; set; } = "";
    public string ProjectDescription { get; set; } = "";
    public string ActivityName { get; set; } = "";
    public string ActivityDescription { get; set; } = "";
}
