namespace SWP391_AutoWashPro_BE.Service.Branch;

public class Request
{
    public class GetBranchesRequest
    {
        public string Keyword { get; set; }
        public bool? IsActive { get; set; }
    }
    public class BranchRequest
    {
        public string Name { get; set; }

        public string Address { get; set; }
        
        public bool IsActive { get; set; }
    }
}