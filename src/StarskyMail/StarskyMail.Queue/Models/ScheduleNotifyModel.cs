using System;
using Newtonsoft.Json;

namespace StarskyMail.Queue.Models
{
    public record ScheduleNotifyModel : IMailModel
    {
        [JsonProperty(Required = Required.Always)]
        public string ManagerName { get; init; }

        [JsonProperty(Required = Required.Always)]
        public string EmployeeName { get; init; }

        [JsonProperty(Required = Required.Always)]
        public string EmployeeEmail { get; init; }

        [JsonProperty(Required = Required.Always)]
        public string StarskyHomeUrl { get; init; }

        [JsonProperty(Required = Required.Always)]
        public string ScheduleDate { get; init; }

        [JsonProperty(Required = Required.Always)]
        public string Shifts { get; init; }

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

            if (string.IsNullOrWhiteSpace(StarskyHomeUrl) || !Uri.IsWellFormedUriString(StarskyHomeUrl, UriKind.Absolute))
            {
                return (false, "Home URL is invalid!");
            }

            if (string.IsNullOrWhiteSpace(ScheduleDate))
            {
                return (false, "Schedule date is invalid!");
            }

            if (string.IsNullOrWhiteSpace(Shifts))
            {
                return (false, "Shifts are invalid!");
            }

            return (true, null);
        }

        public object ToDynamicTemplateData()
        {
            return new
            {
                EmployeeName,
                ManagerName,
                ScheduleDate,
                Shifts,
                StarskyHomeUrl
            };
        }
    }
}