using System.ComponentModel.DataAnnotations;

namespace StarskyMail.Queue.Settings
{
    public class SendGridSettings
    {
        /// <summary>
        /// Section inside appsettings.json
        /// </summary>
        public const string Section = "SendGridSettings";
        
        [Required]
        public bool Enabled { get; set; }

        public string ApiKey { get; set; }
        
        [Required(AllowEmptyStrings = false)]
        public string FromAddress { get; set; }
        
        [Required(AllowEmptyStrings = false)]
        public string InvitationsTemplateId { get; set; }
        
        [Required]
        public int UnsubscribeGroupId { get; set; }
        
        [Required(AllowEmptyStrings = false)]
        public string ScheduleNotificationTemplateId { get; set; }
        
        
    }
}