namespace UnpTracker.Models;

public class DailyTaskService : BackgroundService
{
    private readonly EmailService _emailService;
    private readonly ILogger<DailyTaskService> _logger;

    public DailyTaskService(EmailService emailService, ILogger<DailyTaskService> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRunTime = now.Date.AddHours(7);

            if (now > nextRunTime) nextRunTime = nextRunTime.AddDays(1);
            
            var delay = nextRunTime - now;
            _logger.LogInformation($"Next run time: {nextRunTime}");
            await Task.Delay(delay, cancellationToken);

            try
            {
                _logger.LogInformation("Sending daily emails");
                await _emailService.SendEmailsAsync();

                _logger.LogInformation("Daily emails sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send daily emails");
            }
        }
    }
}