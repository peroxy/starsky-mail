using System.ComponentModel.DataAnnotations;

namespace StarskyMail.Queue.Settings
{
    public record RabbitMQSettings
    {
        /// <summary>
        /// Section inside appsettings.json
        /// </summary>
        public const string Section = "RabbitMQSettings";

        [Required(AllowEmptyStrings = false)] public string Username { get; set; }
        [Required(AllowEmptyStrings = false)] public string Password { get; set; }
        [Required(AllowEmptyStrings = false)] public string ExchangeName { get; set; }
        [Required(AllowEmptyStrings = false)] public string InvitationsQueueName { get; set; }
        [Required(AllowEmptyStrings = false)] public string InvitationsRoutingKey { get; set; }
        [Required(AllowEmptyStrings = false)] public string ScheduleNotifyQueueName { get; set; }
        [Required(AllowEmptyStrings = false)] public string ScheduleNotifyRoutingKey { get; set; }

        public string Hostname { get; set; } = "rabbitmq";
        public int Port { get; set; } = 5672;
    }
}