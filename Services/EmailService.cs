using UnpTracker.Data;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace UnpTracker.Models;

public class EmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger, IServiceProvider serviceProvider)
    {
        _settings = settings.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task SendEmailsAsync()
    {
        _logger.LogInformation("Fetching subscribers who need to be notified");
        var subscriberNotifications = await GetSubscriberNotifications();
        if (subscriberNotifications.Count == 0)
        {
            _logger.LogInformation("No subscribers to notify");
            return;
        }

        _logger.LogInformation($"Creating SMTP client {_settings.SmtpServer}:{_settings.Port}");
        using var smtpClient = new SmtpClient(_settings.SmtpServer, _settings.Port)
        {
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
            EnableSsl = _settings.UseSSL
        };
        _logger.LogInformation("SMTP client created successfully");

        int counter = 0;
        foreach (var (email, notifications) in subscriberNotifications)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = "Уведомление об отслеживаемых плательщиках",
                Body = string.Join(Environment.NewLine, notifications.Select(n => n.ToString())),
                IsBodyHtml = false
            };
            mailMessage.To.Add(email);

            try
            {
                _logger.LogInformation($"Sending email to {email}");
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent to {email} successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {email}");
            }

            counter++;
            if (counter == 100)
            {
                counter = 0;
                _logger.LogInformation("Waiting for 5 minutes");
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
        }
    }

    public async Task<Dictionary<string, List<Notification>>> GetSubscriberNotifications()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NutshellContext>();
        
        var unpNotifications = new Dictionary<string, Notification>();
        
        var LocalDbChangedStatePayers = await dbContext.Payers
            .Where(p => p.IsInLocalDb != dbContext.LocalPayers.Any(lp => lp.PayerId == p.Id))
            .ToListAsync();
        foreach (var payer in LocalDbChangedStatePayers)
        {
            unpNotifications[payer.Unp] = new Notification(payer.Unp);
            unpNotifications[payer.Unp].LocalDbStateChanged(!payer.IsInLocalDb);
            payer.IsInLocalDb = !payer.IsInLocalDb;
        }
        
        await dbContext.SaveChangesAsync();

        foreach (var payer in await dbContext.Payers.ToListAsync())
        {
            bool newState = await payer.Unp.IsInStateDb();
            if (payer.IsInStateDb != newState)
            {
                if (!unpNotifications.ContainsKey(payer.Unp))
                {
                    unpNotifications[payer.Unp] = new Notification(payer.Unp);
                }
                unpNotifications[payer.Unp].StateDbStateChanged(newState);
                payer.IsInStateDb = newState;
            }
        }

        await dbContext.SaveChangesAsync();

        var subscriberNotifications = new Dictionary<string, List<Notification>>();
        var subscriptions = await dbContext.SubscriberPayers
            .Select(sp => new { sp.Subscriber.Email, sp.Payer.Unp })
            .ToListAsync();
        foreach (var subscription in subscriptions)
        {
            if (!unpNotifications.ContainsKey(subscription.Unp)) continue;

            if (!subscriberNotifications.ContainsKey(subscription.Email))
            {
                subscriberNotifications[subscription.Email] = new List<Notification>();
            }
            subscriberNotifications[subscription.Email].Add(unpNotifications[subscription.Unp]);
        }

        return subscriberNotifications;
    }
}