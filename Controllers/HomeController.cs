using UnpTracker.Models;
using UnpTracker.Data;
using Microsoft.AspNetCore.Mvc;

namespace UnpTracker.Controllers;

public class HomeController : Controller
{
    private readonly NutshellContext _dbContext;
    private readonly ILogger<HomeController> _logger;
    private readonly EmailService _emailService;

    public HomeController(
        NutshellContext dbContext,
        ILogger<HomeController> logger,
        EmailService emailService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _emailService = emailService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Submit(SubmissionModel model)
    {
        if (model == null) 
        {
            _logger.LogError("Invalid model data");
            return RedirectToAction("Error", new ErrorMessageModel { Message = "Неверная модель данных" });
        }
        
        if (!model.Email.IsValidEmail())
        {
            _logger.LogError("Invalid email");
            return RedirectToAction("Error", new ErrorMessageModel { Message = "Неверно указанный email" });
        }
        
        var validUnps = model.Unps.GetValidUnps().ToList();
        if (validUnps.Count == 0) 
        {
            _logger.LogError("No valid UNPs");
            return RedirectToAction("Error", new ErrorMessageModel { Message = "Отсутствуют верно указанные УНП" });
        }
        
        Subscriber subscriber;

        if (_dbContext.Subscribers.Any(s => s.Email == model.Email))
        {
            subscriber = _dbContext.Subscribers.First(s => s.Email == model.Email);
            _logger.LogInformation($"Subscriber {subscriber.Email} found in the database");
        }
        else
        {
            _logger.LogInformation("Creating new subscriber");
            subscriber = new Subscriber { Email = model.Email };
            _dbContext.Subscribers.Add(subscriber);
            _logger.LogInformation($"Subscriber {subscriber.Email} created successfully");
        }

        var subscribedUnps = new List<string>();

        foreach (var unp in validUnps)
        {
            Payer payer;
            if (_dbContext.Payers.Any(p => p.Unp == unp))
            {
                payer = _dbContext.Payers.First(p => p.Unp == unp);
                _logger.LogInformation($"Payer {payer.Unp} found in the database");
            }
            else
            {
                _logger.LogInformation("Creating new payer");
                payer = new Payer
                {
                    Unp = unp,
                    IsInLocalDb = unp.IsInLocalDb(_dbContext),
                    IsInStateDb = await unp.IsInStateDb()
                };
                _dbContext.Payers.Add(payer);
                _logger.LogInformation($"Payer {payer.Unp} created successfully");
            }

            if (!_dbContext.SubscriberPayers.Any(sp => sp.Subscriber.Id == subscriber.Id && sp.Payer.Id == payer.Id))
            {
                _logger.LogInformation($"Creating new subscription for {subscriber.Email} to UNP {payer.Unp}");
                var subscriberPayer = new SubscriberPayer { Subscriber = subscriber, Payer = payer };
                _dbContext.SubscriberPayers.Add(subscriberPayer);
                subscribedUnps.Add(payer.Unp);
                _logger.LogInformation($"Subscription for {subscriber.Email} to UNP {payer.Unp} created successfully");
            }
        }

        _dbContext.SaveChanges();
        _logger.LogInformation("Database changes saved successfully");

        if (subscribedUnps.Count > 0)
        {
            _logger.LogInformation("Redirecting to Subscribed page");
            var messageModel = new SubscribedMessageModel()
            {
                Email = subscriber.Email,
                Unps = subscribedUnps,
                TotalSubscriptions = _dbContext.SubscriberPayers.Count(sp => sp.Subscriber.Id == subscriber.Id)
            };
            return RedirectToAction("Subscribed", messageModel);
        }

        return RedirectToAction("Error", new ErrorMessageModel { Message = "Вы уже подписаны на все указанные УНП" });
    }

    public IActionResult Subscribed(SubscribedMessageModel model)
    {
        return View(model);
    }

    public IActionResult Error(ErrorMessageModel model)
    {
        return View(model);
    }

    [HttpGet]
    public JsonResult IsInLocalDb(string unp)
    {
        bool exists = unp.IsInLocalDb(_dbContext);
        return Json(new { exists });
    }

    [HttpGet]
    public async Task<JsonResult> IsInStateDb(string unp)
    {
        bool exists = await unp.IsInStateDb();
        return Json(new { exists });
    }
}