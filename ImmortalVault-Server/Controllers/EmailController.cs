using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;

namespace ImmortalVault_Server.Controllers;

public record EmailRequest(string From, string Name, string Subject, string Message);

[ApiController]
[Route("api/email")]
public class EmailController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public EmailController(IConfiguration configuration)
    {
        this._configuration = configuration;
    }
    
    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
    {
        try
        {
            var username = this._configuration["EMAIL:USERNAME"];
            var password = this._configuration["EMAIL:PASSWORD"];
            var consumer = this._configuration["EMAIL:CONSUMER"];

            if (username == null || password == null || consumer == null)
            {
                throw new Exception("Email username, password or consumer is not provided");
            }
            
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(request.From, request.From),
                Subject = $"Feedback: {request.Subject}",
                Body = $"From: {request.Name}<br/>" +
                       $"With email: {request.From}<br/>" +
                       $"Subject: {request.Subject}<br/>" +
                       $"Message: {request.Message}",
                IsBodyHtml = true
            };
            
            mailMessage.To.Add(consumer);

            await smtpClient.SendMailAsync(mailMessage);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error sending email", Details = ex.Message });
        }
    }
}