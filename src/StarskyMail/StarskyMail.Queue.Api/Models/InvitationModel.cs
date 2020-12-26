using System.Text.Json.Serialization;

namespace StarskyMail.Queue.Api.Models
{
    public record InvitationModel(string ManagerName,  string EmployeeName, string EmployeeEmail, string RegisterUrl);
}