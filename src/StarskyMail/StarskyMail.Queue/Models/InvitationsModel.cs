using System;
using System.Text;
using Newtonsoft.Json;

namespace StarskyMail.Queue.Models
{
    public record InvitationsModel(
        [JsonProperty(Required = Required.Always)]
        string ManagerName,
        [JsonProperty(Required = Required.Always)]
        string EmployeeName,
        [JsonProperty(Required = Required.Always)]
        string EmployeeEmail,
        [JsonProperty(Required = Required.Always)]
        string RegisterUrl)
    {
        
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

        public (string subject, string plainText, string html) ToEmail()
        {
            string htmlContent =
                $"Dear <b>{EmployeeName}</b>,</br></br>" +
                $"You have been invited to join Starsky Scheduling by <b>{ManagerName}</b>.</br></br>" +
                $"You can register by pressing on the following link:</br></br>" +
                $"<a href='{RegisterUrl}'></a>";

            string plainTextContent =
                $"Dear {EmployeeName}, you have been invited to join Starsky Scheduling by {ManagerName}. " +
                $"You can register by pressing on the following link: {RegisterUrl}";

            return ("You have been invited to Starsky Scheduling!", plainTextContent, htmlContent);
        } 
    }
    
}