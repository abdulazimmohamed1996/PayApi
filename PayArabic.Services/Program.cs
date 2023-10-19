using Microsoft.Extensions.Configuration;
using PayArabic.Core;
using PayArabic.Services;

Console.WriteLine("Load setting Started");
IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
config.Bind("AppSettings", AppSettings.Instance);
Console.WriteLine("Load setting Ended");

Console.WriteLine("Event Service Started");
EventService.Process();
Console.WriteLine("Event Service Ended");

Console.WriteLine("Payment Service Started");
EventPaymentService.Process();
Console.WriteLine("Payment Service Ended");

Console.WriteLine("Exit");
Environment.Exit(0);
