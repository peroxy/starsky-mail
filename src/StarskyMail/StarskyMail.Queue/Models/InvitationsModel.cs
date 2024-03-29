using System;
using System.Text;
using Newtonsoft.Json;

namespace StarskyMail.Queue.Models
{
    public interface IMailModel
    {
        (bool success, string reason) Validate();
        object ToDynamicTemplateData();
    }

    public record InvitationsMailModel : IMailModel
    {
        [JsonProperty(Required = Required.Always)]
        public string ManagerName { get; init; }

        [JsonProperty(Required = Required.Always)]
        public string EmployeeName { get; init; }

        [JsonProperty(Required = Required.Always)]
        public string EmployeeEmail { get; init; }

        [JsonProperty(Required = Required.Always)]
        public string RegisterUrl { get; init; }

        public (bool success, string reason) Validate()
        {
            if (string.IsNullOrWhiteSpace(EmployeeEmail) || !EmployeeEmail.Contains("@"))
            {
                return (false, "Email is invalid!");
            }

            if (string.IsNullOrWhiteSpace(EmployeeName))
            {
                return (false, "Employee name is invalid!");
            }

            if (string.IsNullOrWhiteSpace(ManagerName))
            {
                return (false, "Manager name is invalid!");
            }

            if (string.IsNullOrWhiteSpace(RegisterUrl) || !Uri.IsWellFormedUriString(RegisterUrl, UriKind.Absolute))
            {
                return (false, "Register URL is invalid!");
            }

            return (true, null);
        }

        public object ToDynamicTemplateData()
        {
            return new
            {
                EmployeeName,
                ManagerName,
                RegisterUrl
            };
        }
    }
}