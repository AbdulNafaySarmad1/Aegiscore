using AegisCore.Application.Finance;
using AegisCore.Infrastructure.Onboarding;
using AegisCore.Infrastructure.Security.Pqc;
using AegisCore.Infrastructure.Finance.Gateways;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.Finance;

public static class FinanceModuleServiceExtensions
{
    public static IServiceCollection AddPqcSigningQueue(this IServiceCollection services)
    {
        services.AddSingleton<PqcSigningQueue>();
        services.AddSingleton<IPqcSigningQueue>(provider => provider.GetRequiredService<PqcSigningQueue>());
        services.AddHostedService(provider => provider.GetRequiredService<PqcSigningQueue>());
        return services;
    }

    public static IServiceCollection AddFinanceModule(this IServiceCollection services)
    {
        services.AddPqcSigningQueue();
        services.AddScoped<IFinanceModuleService, FinanceModuleService>();
        services.AddScoped<IAccountingStandardsEngine, AccountingStandardsEngine>();
        services.AddScoped<IHybridKemService, HybridKemService>();
        services.AddScoped<PqcKeyCeremonyService>();
        services.AddScoped<WizardGatewayService>();
        services.AddScoped<FinanceAuditSealService>();
        services.AddScoped<FinanceLedgerIntegrityService>();
        services.AddHostedService<FinanceAuditSealWorker>();
        services.AddHostedService<FinancePaymentPlanWorker>();
        services.AddHostedService<PqcKeyRotationWorker>();
        services.AddHostedService<FinanceLedgerSignatureWorker>();
        services.AddHttpClient("finance-gateways", client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddScoped<SandboxPaymentGatewayAdapter>();
        services.AddScoped<IPaymentGatewayAdapter, RaastPaymentGatewayAdapter>();
        services.AddScoped<IPaymentGatewayAdapter, StripePaymentGatewayAdapter>();
        services.AddScoped<IPaymentGatewayAdapter, PlaidAchPaymentGatewayAdapter>();
        services.AddScoped<IPaymentGatewayAdapter, GoCardlessPaymentGatewayAdapter>();
        services.AddScoped<IPaymentGatewayAdapter, BillplzFpxPaymentGatewayAdapter>();
        services.AddScoped<IPaymentGatewayAdapter, DuitNowPaymentGatewayAdapter>();
        services.AddScoped<PaymentGatewayRouter>();
        services.AddScoped<VoucherImportService>();
        services.AddScoped<FinanceProcurementService>();
        services.AddScoped<VendorSanctionsService>();
        services.AddHttpClient("sanctions", client => client.Timeout = TimeSpan.FromSeconds(20));
        services.AddHostedService<VendorSanctionsRescreenWorker>();
        services.AddScoped<FinancePayrollSettlementService>();
        services.AddScoped<FinanceScholarshipService>();
        services.AddScoped<FinanceAssetLifecycleService>();
        services.AddScoped<FinanceTreasuryService>();
        services.AddScoped<AncillaryFinanceService>();
        services.AddScoped<FinanceRegulatoryReportService>();
        services.AddScoped<FinanceContinuousControlService>();
        return services;
    }
}
