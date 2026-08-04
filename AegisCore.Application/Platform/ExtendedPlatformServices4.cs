using AegisCore.Domain.Entities;

namespace AegisCore.Application.Platform;

// OPTIONAL-26 Cafeteria Management
public interface ICafeteriaService { Task<CafeteriaUnit> CreateUnitAsync(CafeteriaUnit unit, CancellationToken ct); Task<MenuItem> AddMenuItemAsync(MenuItem item, CancellationToken ct); Task<FoodOrder> PlaceOrderAsync(Guid studentId, Guid menuItemId, Guid unitId, int quantity, CancellationToken ct); Task<DemandForecast> GenerateForecastAsync(Guid menuItemId, Guid unitId, DateTime date, CancellationToken ct); Task<VendorScorecard> EvaluateVendorAsync(Guid vendorId, string month, CancellationToken ct); Task<List<FoodOrder>> GetPendingOrdersAsync(Guid unitId, CancellationToken ct); Task<List<InventoryAlert>> GetInventoryAlertsAsync(Guid unitId, CancellationToken ct); }

// Personality Mode
public interface IPersonalityModeService { Task<PersonalityConfig> GetConfigAsync(CancellationToken ct); Task<PersonalityConfig> UpdateConfigAsync(bool isEnabled, bool isLockedOff, CancellationToken ct); Task SetUserPreferenceAsync(Guid userId, bool isActive, CancellationToken ct); Task<bool> IsActiveForUserAsync(Guid userId, CancellationToken ct); }

// Chart System
public interface IChartService { Task<ChartConfiguration> CreateChartAsync(ChartConfiguration config, CancellationToken ct); Task<InsightChip> ComputeInsightAsync(Guid chartId, CancellationToken ct); Task<List<ChartConfiguration>> GetPublishedChartsAsync(CancellationToken ct); }

// OCR Integration
public interface IOcrIntegrationService { Task<OcrProviderConfig> RegisterProviderAsync(OcrProviderConfig config, CancellationToken ct); Task<string> ExtractTextAsync(Guid configId, byte[] documentBytes, string documentType, CancellationToken ct); Task<List<OcrProviderConfig>> GetProvidersAsync(CancellationToken ct); }

// Improved Plagiarism Detection Engine
public interface IPlagiarismEngineService { Task<double> ComputeCosineSimilarityAsync(string textA, string textB, CancellationToken ct); Task<List<Guid>> DetectCollusionRingsAsync(Guid assignmentId, CancellationToken ct); Task<double> ComputeAiProbabilityAsync(string text, CancellationToken ct); Task<double> ComputeStyleConsistencyAsync(Guid studentId, string text, CancellationToken ct); }

// Improved Proctoring Integration
public interface IProctoringIntegrationService { Task<bool> ValidateBrowserLockdownAsync(Guid sessionId, CancellationToken ct); Task<string> AnalyzeFacialFeaturesAsync(byte[] imageData, CancellationToken ct); Task<bool> DetectMultipleFacesAsync(byte[] imageData, CancellationToken ct); Task<double> EstimateGazeDirectionAsync(byte[] imageData, CancellationToken ct); }

// Improved Fee Defaulter ML Engine
public interface IDefaulterMlEngineService { Task<double> TrainRiskModelAsync(CancellationToken ct); Task<double> PredictDefaultProbabilityAsync(Guid studentId, CancellationToken ct); Task<List<string>> IdentifyRiskDriversAsync(Guid studentId, CancellationToken ct); Task<double> EvaluateModelPerformanceAsync(CancellationToken ct); }
