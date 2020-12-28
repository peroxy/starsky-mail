using System.ComponentModel.DataAnnotations;

namespace StarskyMail.Queue
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

        public string Hostname { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
    }
}