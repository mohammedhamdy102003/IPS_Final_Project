using System.ComponentModel.DataAnnotations;

namespace IPS_PROJECT.Models
{
    public class SYSTEM_STATUS
    {
        [Key]
        public int Id { get; set; }

        public bool IsSecure { get; set; } = true;

        public DateTime LastUpdated { get; set; } = DateTime.Now;

      //  public bool FirstAdminCreated { get; set; } = false;
    }
}
 