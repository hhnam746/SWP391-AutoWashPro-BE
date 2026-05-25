namespace SWP391_AutoWashPro_BE.Service.Branch;

public class Request
{
    public class GetBranchesRequest
    {
        public string Keyword { get; set; }
        public bool? IsActive { get; set; }
    }
}