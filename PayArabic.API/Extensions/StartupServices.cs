namespace PayArabic.API.Extensions;

/// <summary>
/// Zein: Register all dependency injection for all dao
/// </summary>
public static class StartupServices
{
    public static void Startup(this IServiceCollection services, IConfiguration Configuration)
    {
        Configuration.Bind("AppSettings", AppSettings.Instance);
        services.AddTransient<ICoreService, CoreService>();
        services.AddTransient<ICoreDao, CoreDao>();
        services.AddTransient<IAuditDao, AuditDao>();
        services.AddTransient<IUserDao, UserDao>();
        services.AddTransient<IConfigurationDao, ConfigurationDao>();
        services.AddTransient<IIntegrationDao, IntegrationDao>();
        services.AddTransient<IInvoiceDao, InvoiceDao>();
        services.AddTransient<IDepositDao, DepositDao>();
        services.AddTransient<IPaymentLinkDao, PaymentLinkDao>();
        services.AddTransient<ICurrencyDao, CurrencyDao>();
        services.AddTransient<IProductCategoryDao, ProductCategoryDao>();
        services.AddTransient<IProductDao, ProductDao>();
        services.AddTransient<IProductLinkDao, ProductLinkDao>();
        services.AddTransient<ITransactionDao, TransactionDao>();

        CoreService.InitSystem();
    }
}
