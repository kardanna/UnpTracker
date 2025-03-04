using Microsoft.EntityFrameworkCore;
using UnpTracker.Models;
using UnpTracker.Data;

namespace UnpTracker;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);      

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        
        builder.Services.AddDbContext<NutshellContext>(options =>
        {
            options.UseSqlite(builder.Configuration.GetConnectionString("NutshellConnection"));
        });
        builder.Services.AddLogging(logging => 
        {
            logging.AddConsole();
            logging.AddDebug();
        });
        builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
        builder.Services.AddSingleton<EmailService>();
        builder.Services.AddHostedService<DailyTaskService>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            InitializeDatabase(app);
        }
        
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
        }
        app.UseRouting();

        app.UseAuthorization();

        app.MapStaticAssets();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
            .WithStaticAssets();

        app.Run();
    }

    //Method to initialize the database with test data
    private static void InitializeDatabase(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NutshellContext>();
        
        dbContext.Database.EnsureCreated();

        dbContext.Payers.RemoveRange(dbContext.Payers);
        dbContext.LocalPayers.RemoveRange(dbContext.LocalPayers);
        dbContext.Subscribers.RemoveRange(dbContext.Subscribers);
        dbContext.SubscriberPayers.RemoveRange(dbContext.SubscriberPayers);
        dbContext.SaveChanges();

        var payer = new Payer
        {
            Unp = "123456789",
            IsInLocalDb = false,
            IsInStateDb = false
        };
        dbContext.Payers.Add(payer);

        var localPayer = new LocalPayer
        {
            Payer = payer
        };
        dbContext.LocalPayers.Add(localPayer);

        dbContext.SaveChanges();
    }
}