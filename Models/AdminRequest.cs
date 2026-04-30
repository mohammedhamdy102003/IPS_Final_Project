namespace IPS_PROJECT.Models
{
    public class AdminRequest
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public USERS User { get; set; }

        public DateTime RequestDate { get; set; }

        public string Status { get; set; } // Pending / Approved / Rejected
    }
}
