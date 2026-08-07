# AegisCore Repository File Tree

> Auto-generated from Git-tracked files.
> Build artifacts, caches, ignored files, and local tooling state are excluded.

```text
Aegiscore/
    ├── .github/
    │   └── workflows/
    │       ├── ci.yml
    │       ├── deploy-cloud.yml
    │       ├── deploy.yml
    │       └── release.yml
    ├── .serena/
    │   ├── memories/
    │   │   └── memory_maintenance.md
    │   ├── .gitignore
    │   └── project.yml
    ├── AegisCore.API/
    │   ├── Authorization/
    │   │   ├── PermissionAuthorizationHandler.cs
    │   │   └── RequirePermissionAttribute.cs
    │   ├── Background/
    │   │   ├── AutoLockBackgroundService.cs
    │   │   └── LicenseHeartbeatHostedService.cs
    │   ├── Controllers/
    │   │   ├── AssetInventoryController.cs
    │   │   ├── AttendanceController.cs
    │   │   ├── AuthController.cs
    │   │   ├── BrandingController.cs
    │   │   ├── CampusController.cs
    │   │   ├── CampusOperationsController.cs
    │   │   ├── CommunicationsController.cs
    │   │   ├── ComplianceController.cs
    │   │   ├── ConfigCenterController.cs
    │   │   ├── CoreErpControllers.cs
    │   │   ├── CurriculumController.cs
    │   │   ├── DataImportController.cs
    │   │   ├── DegreeAuditController.cs
    │   │   ├── DeveloperControllers.cs
    │   │   ├── DigitalTwinController.cs
    │   │   ├── DisasterRecoveryController.cs
    │   │   ├── DocumentVaultController.cs
    │   │   ├── EnhancedSetupController.cs
    │   │   ├── ExtendedPlatformControllers.cs
    │   │   ├── ExtendedPlatformControllers2.cs
    │   │   ├── ExtendedPlatformControllers3.cs
    │   │   ├── ExtendedPlatformControllers4.cs
    │   │   ├── FinanceController.cs
    │   │   ├── FinanceWebhookController.cs
    │   │   ├── FormsController.cs
    │   │   ├── GovernanceController.cs
    │   │   ├── HrController.cs
    │   │   ├── I18nController.cs
    │   │   ├── IdentityController.cs
    │   │   ├── LicenseController.cs
    │   │   ├── LoginAuditController.cs
    │   │   ├── MarketplaceController.cs
    │   │   ├── MicrosoftEcosystemController.cs
    │   │   ├── PlatformControllers.cs
    │   │   ├── ProfilePictureController.cs
    │   │   ├── ReplicationController.cs
    │   │   ├── RuleEngineController.cs
    │   │   ├── S2AdmissionsController.cs
    │   │   ├── S3ExaminationController.cs
    │   │   ├── S5ObeController.cs
    │   │   ├── SamlController.cs
    │   │   ├── ScimController.cs
    │   │   ├── SearchController.cs
    │   │   ├── SetupController.cs
    │   │   ├── SsoHitlController.cs
    │   │   ├── StudentLifecycleController.cs
    │   │   ├── SupportController.cs
    │   │   ├── SyncLagController.cs
    │   │   ├── TimetableController.cs
    │   │   └── WizardController.cs
    │   ├── Extensions/
    │   │   ├── ApiServicesExtensions.cs
    │   │   └── ProductionServicesExtensions.cs
    │   ├── Finance/
    │   │   ├── CampusModuleEndpoints.cs
    │   │   ├── DefaultPredictionEndpoints.cs
    │   │   ├── FinanceModuleEndpoints.cs
    │   │   ├── ManagementEndpoints.cs
    │   │   ├── StudentLifecycleModuleEndpoints.cs
    │   │   └── VisitingFacultyEndpoints.cs
    │   ├── Hubs/
    │   │   ├── FinanceHub.cs
    │   │   ├── NotificationHub.cs
    │   │   ├── SignalRNotificationRealtimePublisher.cs
    │   │   └── StateMachineHub.cs
    │   ├── Logging/
    │   │   └── SensitiveDataDestructuringPolicy.cs
    │   ├── Middleware/
    │   │   ├── CasbinAuthorizationMiddleware.cs
    │   │   ├── DemoExpiryMiddleware.cs
    │   │   ├── DemoIdentityMiddleware.cs
    │   │   ├── GlobalExceptionHandlerMiddleware.cs
    │   │   ├── GlobalRateLimitMiddleware.cs
    │   │   ├── InfrastructureMiddleware.cs
    │   │   ├── LicenseLockoutMiddleware.cs
    │   │   ├── RateLimitAuthMiddleware.cs
    │   │   ├── RateLimitingMiddleware.cs
    │   │   ├── SetupRequiredMiddleware.cs
    │   │   ├── TenantContextMiddleware.cs
    │   │   └── ValidationExceptionMiddleware.cs
    │   ├── wwwroot/
    │   │   ├── setup/
    │   │   │   └── index.html
    │   │   └── templates/
    │   │       └── whatsapp-templates.json
    │   ├── AegisCore.API.csproj
    │   ├── GlobalUsings.cs
    │   ├── Program.cs
    │   ├── appsettings.Production.json
    │   └── appsettings.json
    ├── AegisCore.Application/
    │   ├── AI/
    │   │   ├── AiAgentRequest.cs
    │   │   ├── AiAgentResponse.cs
    │   │   ├── GradePredictionResult.cs
    │   │   ├── IAiAgent.cs
    │   │   ├── IDocumentOcrService.cs
    │   │   ├── IEmbeddingProvider.cs
    │   │   ├── ILlmProvider.cs
    │   │   ├── IVectorStore.cs
    │   │   ├── LlmOptions.cs
    │   │   ├── OcrResult.cs
    │   │   ├── StudentCourseRecommendation.cs
    │   │   └── VectorChunk.cs
    │   ├── Abstractions/
    │   │   └── Cloud/
    │   │       └── CloudAbstractions.cs
    │   ├── Admissions/
    │   │   └── AdmissionsContracts.cs
    │   ├── ApiGateway/
    │   │   ├── ApiGatewayDtos.cs
    │   │   └── IApiKeyService.cs
    │   ├── AssetInventory/
    │   │   └── AssetInventoryContracts.cs
    │   ├── Backup/
    │   │   ├── DRDtos.cs
    │   │   └── IBackupService.cs
    │   ├── Campus/
    │   │   ├── CafeteriaContracts.cs
    │   │   ├── CampusDtos.cs
    │   │   ├── ICampusService.cs
    │   │   └── TransportContracts.cs
    │   ├── CampusOperations/
    │   │   ├── CampusOperationsContracts.cs
    │   │   └── ElectionContracts.cs
    │   ├── Common/
    │   │   └── Interfaces/
    │   │       ├── IApiKeyRepository.cs
    │   │       ├── IAttendanceRepository.cs
    │   │       ├── IAuditLogService.cs
    │   │       ├── IBackupRepository.cs
    │   │       ├── IBruteForceProtectionService.cs
    │   │       ├── ICalibrationRecordRepository.cs
    │   │       ├── ICampusRepository.cs
    │   │       ├── IComplianceRepository.cs
    │   │       ├── IConfigCenterRepository.cs
    │   │       ├── ICourseRepository.cs
    │   │       ├── IDegreeAuditRepository.cs
    │   │       ├── IDigitalTwinRepository.cs
    │   │       ├── IEnrollmentRepository.cs
    │   │       ├── IEquipmentBookingRepository.cs
    │   │       ├── IEquipmentMaintenanceRepository.cs
    │   │       ├── IExamSeatingRepository.cs
    │   │       ├── IExtendedPlatformRepository.cs
    │   │       ├── IExtensionRepository.cs
    │   │       ├── IFormRepository.cs
    │   │       ├── IGateEntryRepository.cs
    │   │       ├── IGuardRoundRepository.cs
    │   │       ├── IGuardShiftRepository.cs
    │   │       ├── II18nRepository.cs
    │   │       ├── IIdentityRepository.cs
    │   │       ├── IJwtTokenService.cs
    │   │       ├── ILabBookingRepository.cs
    │   │       ├── ILabConsumableRepository.cs
    │   │       ├── ILabEquipmentRepository.cs
    │   │       ├── ILabSafetyInspectionRepository.cs
    │   │       ├── ILoginAuditRepository.cs
    │   │       ├── ILostFoundItemRepository.cs
    │   │       ├── IMaintenanceContractorRepository.cs
    │   │       ├── IMaintenanceTicketRepository.cs
    │   │       ├── IPasswordHashingService.cs
    │   │       ├── IPlatformRepository.cs
    │   │       ├── IPreventiveMaintenanceScheduleRepository.cs
    │   │       ├── IRepository.cs
    │   │       ├── IResearchLabRepository.cs
    │   │       ├── IRuleRepository.cs
    │   │       ├── ISLADefinitionRepository.cs
    │   │       ├── ISecurityGuardRepository.cs
    │   │       ├── ISecurityIncidentRepository.cs
    │   │       ├── IStudentRepository.cs
    │   │       ├── IUnitOfWork.cs
    │   │       ├── IVehiclePassRepository.cs
    │   │       ├── IVisitorRepository.cs
    │   │       ├── IWorkOrderRepository.cs
    │   │       └── IWorkflowRepository.cs
    │   ├── Compliance/
    │   │   ├── ComplianceDtos.cs
    │   │   └── IComplianceService.cs
    │   ├── ConfigCenter/
    │   │   ├── ConfigCenterDtos.cs
    │   │   └── IConfigurationCenterService.cs
    │   ├── Curriculum/
    │   │   └── CurriculumContracts.cs
    │   ├── DTOs/
    │   │   └── Requests/
    │   │       ├── EnrollStudentRequest.cs
    │   │       ├── GetBatchTimetableRequest.cs
    │   │       ├── MarkAttendanceRequest.cs
    │   │       └── SubmitResultRequest.cs
    │   ├── DataImport/
    │   │   └── DataImportContracts.cs
    │   ├── DegreeAudit/
    │   │   ├── DegreeAuditDtos.cs
    │   │   └── IDegreeAuditService.cs
    │   ├── DigitalTwin/
    │   │   ├── DigitalTwinDtos.cs
    │   │   └── IDigitalTwinService.cs
    │   ├── DocumentVault/
    │   │   └── DocumentVaultContracts.cs
    │   ├── Examination/
    │   │   └── ExaminationContracts.cs
    │   ├── Finance/
    │   │   ├── AccountingEngineContracts.cs
    │   │   ├── DefaultPredictionContracts.cs
    │   │   ├── FinanceAuditContracts.cs
    │   │   ├── FinanceContracts.cs
    │   │   ├── FinanceModuleContracts.cs
    │   │   └── FinanceRealtime.cs
    │   ├── Forms/
    │   │   ├── FormsDtos.cs
    │   │   └── IFormService.cs
    │   ├── Foundation/
    │   │   └── FoundationContracts.cs
    │   ├── Governance/
    │   │   ├── AccreditationContracts.cs
    │   │   ├── BoardPortalContracts.cs
    │   │   └── GovernanceContracts.cs
    │   ├── Grading/
    │   │   ├── GradingSchemeResolver.cs
    │   │   └── ResultService.cs
    │   ├── Hr/
    │   │   └── HrContracts.cs
    │   ├── HumanResources/
    │   │   └── VisitingFacultyContracts.cs
    │   ├── I18n/
    │   │   ├── I18nDtos.cs
    │   │   └── II18nService.cs
    │   ├── Identity/
    │   │   ├── IIdentityService.cs
    │   │   ├── ISamlService.cs
    │   │   └── IdentityDtos.cs
    │   ├── License/
    │   │   └── LicenseContracts.cs
    │   ├── Maintenance/
    │   │   ├── PreventiveMaintenanceService.cs
    │   │   ├── SLAEngineService.cs
    │   │   ├── TicketService.cs
    │   │   └── WorkOrderService.cs
    │   ├── Marketplace/
    │   │   ├── IExtensionService.cs
    │   │   ├── MarketplaceDtos.cs
    │   │   └── MarketplaceRevenue.cs
    │   ├── MicrosoftEcosystem/
    │   │   └── MicrosoftEcosystemContracts.cs
    │   ├── Obe/
    │   │   └── ObeContracts.cs
    │   ├── Onboarding/
    │   │   ├── EnhancedSetupModels.cs
    │   │   └── WizardStateService.cs
    │   ├── Platform/
    │   │   ├── ExtendedPlatformServices.cs
    │   │   ├── ExtendedPlatformServices2.cs
    │   │   ├── ExtendedPlatformServices3.cs
    │   │   ├── ExtendedPlatformServices4.cs
    │   │   └── PlatformServices.cs
    │   ├── ProfilePictures/
    │   │   └── IProfilePictureService.cs
    │   ├── Replication/
    │   │   └── ReplicationContracts.cs
    │   ├── ResearchLabs/
    │   │   ├── CalibrationService.cs
    │   │   ├── ConsumableManagementService.cs
    │   │   ├── EquipmentManagementService.cs
    │   │   ├── LabBookingService.cs
    │   │   └── SafetyInspectionService.cs
    │   ├── RuleEngine/
    │   │   ├── IRuleEngineService.cs
    │   │   └── RuleEngineDtos.cs
    │   ├── Search/
    │   │   ├── ISearchIndex.cs
    │   │   ├── ISearchService.cs
    │   │   └── SearchDtos.cs
    │   ├── Security/
    │   │   ├── GuardManagementService.cs
    │   │   ├── IncidentReportingService.cs
    │   │   ├── LostFoundService.cs
    │   │   ├── VehiclePassService.cs
    │   │   └── VisitorManagementService.cs
    │   ├── SsoHitl/
    │   │   └── SsoHitlContracts.cs
    │   ├── StudentLifecycle/
    │   │   ├── ForeignStudentContracts.cs
    │   │   ├── GamificationContracts.cs
    │   │   ├── MentalHealthContracts.cs
    │   │   ├── StudentLifecycleContracts.cs
    │   │   └── VoiceAssistantContracts.cs
    │   ├── Support/
    │   │   └── SupportModels.cs
    │   ├── SyncLag/
    │   │   └── SyncLagContracts.cs
    │   ├── Workflow/
    │   │   ├── IWorkflowServices.cs
    │   │   ├── WorkflowCanvasDto.cs
    │   │   └── WorkflowDtos.cs
    │   ├── AegisCore.Application.csproj
    │   ├── AssemblyReference.cs
    │   ├── GlobalUsings.cs
    │   └── ServiceCollectionExtensions.cs
    ├── AegisCore.Contracts/
    │   ├── Abstractions/
    │   │   └── Cloud/
    │   │       └── CloudAbstractions.cs
    │   ├── Common/
    │   │   └── Interfaces/
    │   │       ├── IApiKeyRepository.cs
    │   │       ├── IAttendanceRepository.cs
    │   │       ├── IBackupRepository.cs
    │   │       ├── ICalibrationRecordRepository.cs
    │   │       ├── ICampusRepository.cs
    │   │       ├── IComplianceRepository.cs
    │   │       ├── IConfigCenterRepository.cs
    │   │       ├── ICourseRepository.cs
    │   │       ├── IDegreeAuditRepository.cs
    │   │       ├── IDigitalTwinRepository.cs
    │   │       ├── IEnrollmentRepository.cs
    │   │       ├── IEquipmentBookingRepository.cs
    │   │       ├── IEquipmentMaintenanceRepository.cs
    │   │       ├── IExamSeatingRepository.cs
    │   │       ├── IExtendedPlatformRepository.cs
    │   │       ├── IExtensionRepository.cs
    │   │       ├── IFormRepository.cs
    │   │       ├── IGateEntryRepository.cs
    │   │       ├── IGuardRoundRepository.cs
    │   │       ├── IGuardShiftRepository.cs
    │   │       ├── II18nRepository.cs
    │   │       ├── IIdentityRepository.cs
    │   │       ├── ILabBookingRepository.cs
    │   │       ├── ILabConsumableRepository.cs
    │   │       ├── ILabEquipmentRepository.cs
    │   │       ├── ILabSafetyInspectionRepository.cs
    │   │       ├── ILoginAuditRepository.cs
    │   │       ├── ILostFoundItemRepository.cs
    │   │       ├── IMaintenanceContractorRepository.cs
    │   │       ├── IMaintenanceTicketRepository.cs
    │   │       ├── IPlatformRepository.cs
    │   │       ├── IPreventiveMaintenanceScheduleRepository.cs
    │   │       ├── IRepository.cs
    │   │       ├── IResearchLabRepository.cs
    │   │       ├── IRuleRepository.cs
    │   │       ├── ISLADefinitionRepository.cs
    │   │       ├── ISecurityGuardRepository.cs
    │   │       ├── ISecurityIncidentRepository.cs
    │   │       ├── IStudentRepository.cs
    │   │       ├── IUnitOfWork.cs
    │   │       ├── IVehiclePassRepository.cs
    │   │       ├── IVisitorRepository.cs
    │   │       ├── IWorkOrderRepository.cs
    │   │       └── IWorkflowRepository.cs
    │   └── AegisCore.Contracts.csproj
    ├── AegisCore.Domain/
    │   ├── Common/
    │   │   └── ITenantScoped.cs
    │   ├── Entities/
    │   │   ├── AccreditationEntities.cs
    │   │   ├── AdmissionsEntities.cs
    │   │   ├── ApiKey.cs
    │   │   ├── ApprovalRecord.cs
    │   │   ├── Attendance.cs
    │   │   ├── AuditFinding.cs
    │   │   ├── AuditLog.cs
    │   │   ├── BackupConfiguration.cs
    │   │   ├── BackupRecord.cs
    │   │   ├── Batch.cs
    │   │   ├── BoardGovernanceEntities.cs
    │   │   ├── Building.cs
    │   │   ├── CafeteriaEntities.cs
    │   │   ├── CafeteriaExtendedEntities.cs
    │   │   ├── CalibrationRecord.cs
    │   │   ├── Campus.cs
    │   │   ├── CampusConfiguration.cs
    │   │   ├── CatalogIngestionEntities.cs
    │   │   ├── CheckpointScan.cs
    │   │   ├── CisUpload.cs
    │   │   ├── CloPlomapping.cs
    │   │   ├── CommunicationEntities.cs
    │   │   ├── ComplianceControl.cs
    │   │   ├── ComplianceFramework.cs
    │   │   ├── ConfigAuditLog.cs
    │   │   ├── ConfigVersion.cs
    │   │   ├── ConfigurationEntry.cs
    │   │   ├── ConsumableRequisition.cs
    │   │   ├── ControlEvidence.cs
    │   │   ├── CoreErpEntities1.cs
    │   │   ├── CoreErpEntities2.cs
    │   │   ├── CoreErpEntities3.cs
    │   │   ├── CoreErpEntities4.cs
    │   │   ├── CoreErpFinanceHrSis.cs
    │   │   ├── CorrectiveActionPlan.cs
    │   │   ├── CountryPack.cs
    │   │   ├── Course.cs
    │   │   ├── CourseAssignment.cs
    │   │   ├── CreditTransfer.cs
    │   │   ├── Currency.cs
    │   │   ├── CurriculumManagementEntities.cs
    │   │   ├── DataImportEntities.cs
    │   │   ├── DecisionTable.cs
    │   │   ├── DefaultPredictionEntities.cs
    │   │   ├── DegreeAudit.cs
    │   │   ├── DegreeProgram.cs
    │   │   ├── DegreeRequirement.cs
    │   │   ├── Department.cs
    │   │   ├── DocumentVaultEntities.cs
    │   │   ├── ElectionEntities.cs
    │   │   ├── EmergencyAlert.cs
    │   │   ├── EmergencyContact.cs
    │   │   ├── EnergyReading.cs
    │   │   ├── Enrollment.cs
    │   │   ├── EquipmentBooking.cs
    │   │   ├── EquipmentMaintenance.cs
    │   │   ├── EquipmentUsageLog.cs
    │   │   ├── ExamSchedule.cs
    │   │   ├── ExamSeating.cs
    │   │   ├── ExaminationEntities.cs
    │   │   ├── Extension.cs
    │   │   ├── ExtensionInstallation.cs
    │   │   ├── ExtensionReview.cs
    │   │   ├── Faculty.cs
    │   │   ├── FeatureFlag.cs
    │   │   ├── FinanceAccountingEngineEntities.cs
    │   │   ├── FinanceAidEntities.cs
    │   │   ├── FinanceAssetAccountingEvent.cs
    │   │   ├── FinanceAuditComplianceEntities.cs
    │   │   ├── FinanceAuditSeal.cs
    │   │   ├── FinanceEntities.cs
    │   │   ├── FinanceFleetEntities.cs
    │   │   ├── FinanceForeignCurrencyEntities.cs
    │   │   ├── FinanceInsuranceEntities.cs
    │   │   ├── FinanceInventoryEntities.cs
    │   │   ├── FinanceInvestmentEntities.cs
    │   │   ├── FinanceLibraryEntities.cs
    │   │   ├── FinanceModuleEntities.cs
    │   │   ├── FinancePayrollEntities.cs
    │   │   ├── FinancePayrollSettlementEntities.cs
    │   │   ├── FinancePettyCashEntities.cs
    │   │   ├── FinanceProcurementEntities.cs
    │   │   ├── FinanceRegulatoryEntities.cs
    │   │   ├── FinanceTaxEntities.cs
    │   │   ├── FinanceTreasuryEntities.cs
    │   │   ├── FinanceUtilityEntities.cs
    │   │   ├── FinanceVendorEntities.cs
    │   │   ├── FireDrill.cs
    │   │   ├── FireEquipment.cs
    │   │   ├── Floor.cs
    │   │   ├── FloorPlan.cs
    │   │   ├── ForeignStudentEntities.cs
    │   │   ├── FormDefinition.cs
    │   │   ├── FormSubmission.cs
    │   │   ├── FoundationEntities.cs
    │   │   ├── GamificationEntities.cs
    │   │   ├── GateEntry.cs
    │   │   ├── GovernanceEntities.cs
    │   │   ├── GuardRound.cs
    │   │   ├── GuardShift.cs
    │   │   ├── IncidentEvidence.cs
    │   │   ├── IncidentWitness.cs
    │   │   ├── IoTSensor.cs
    │   │   ├── LabBooking.cs
    │   │   ├── LabConsumable.cs
    │   │   ├── LabEquipment.cs
    │   │   ├── LabSafetyInspection.cs
    │   │   ├── LicenseState.cs
    │   │   ├── LmsClassGenerationEntities.cs
    │   │   ├── LoginAuditRecord.cs
    │   │   ├── LostFoundItem.cs
    │   │   ├── MaintenanceContractor.cs
    │   │   ├── MaintenanceTicket.cs
    │   │   ├── MentalHealthEntities.cs
    │   │   ├── MicrosoftEcosystemEntities.cs
    │   │   ├── NotificationEntities.cs
    │   │   ├── NotificationRecord.cs
    │   │   ├── PayrollEntities.cs
    │   │   ├── Permission.cs
    │   │   ├── PlatformCrossCuttingEntities.cs
    │   │   ├── PlatformEntities.cs
    │   │   ├── PlatformEntitiesP100_110.cs
    │   │   ├── PlatformEntitiesP111_120.cs
    │   │   ├── PlatformEntitiesP122_127.cs
    │   │   ├── PlatformEntitiesP87_92.cs
    │   │   ├── PlatformEntitiesP93_98.cs
    │   │   ├── PluginRevenueShare.cs
    │   │   ├── PreventiveMaintenanceSchedule.cs
    │   │   ├── ProfilePictureRecord.cs
    │   │   ├── ReadReplicaConfig.cs
    │   │   ├── ReplicationEntities.cs
    │   │   ├── ResearchLab.cs
    │   │   ├── Result.cs
    │   │   ├── Role.cs
    │   │   ├── RolePermission.cs
    │   │   ├── Room.cs
    │   │   ├── RoomOccupancy.cs
    │   │   ├── RuleDefinition.cs
    │   │   ├── RuleVersion.cs
    │   │   ├── SLADefinition.cs
    │   │   ├── SafetyChecklistItem.cs
    │   │   ├── SamlConfiguration.cs
    │   │   ├── SchedulerEntities.cs
    │   │   ├── SecurityGuard.cs
    │   │   ├── SecurityIncident.cs
    │   │   ├── SensorReading.cs
    │   │   ├── SsoHitlEntities.cs
    │   │   ├── Student.cs
    │   │   ├── StudentLifecycleEntities.cs
    │   │   ├── StudentTransfer.cs
    │   │   ├── SyncLagEntities.cs
    │   │   ├── TaxSlab.cs
    │   │   ├── TicketComment.cs
    │   │   ├── TicketEscalation.cs
    │   │   ├── Timetable.cs
    │   │   ├── TranslationKey.cs
    │   │   ├── TranslationValue.cs
    │   │   ├── TransportEntities.cs
    │   │   ├── User.cs
    │   │   ├── UserRole.cs
    │   │   ├── VehiclePass.cs
    │   │   ├── VisitingFacultyEntities.cs
    │   │   ├── Visitor.cs
    │   │   ├── VoiceAssistantEntities.cs
    │   │   ├── WebhookSubscription.cs
    │   │   ├── WizardAdminCredential.cs
    │   │   ├── WizardSession.cs
    │   │   ├── WorkOrder.cs
    │   │   ├── WorkOrderMaterial.cs
    │   │   ├── WorkOrderTimeEntry.cs
    │   │   ├── WorkflowDefinition.cs
    │   │   ├── WorkflowInstance.cs
    │   │   └── WorkflowStep.cs
    │   ├── AegisCore.Domain.csproj
    │   ├── Enums.cs
    │   └── GlobalUsings.cs
    ├── AegisCore.Infrastructure/
    │   ├── AI/
    │   │   ├── AiPlatformServiceExtensions.cs
    │   │   ├── AzureDocumentIntelligenceService.cs
    │   │   ├── DocumentAiAgent.cs
    │   │   ├── DocumentOcrService.cs
    │   │   ├── InMemoryVectorStore.cs
    │   │   ├── LlmProviderFactory.cs
    │   │   ├── OllamaEmbeddingProvider.cs
    │   │   ├── OllamaLlmProvider.cs
    │   │   ├── OpenAiEmbeddingProvider.cs
    │   │   ├── OpenAiLlmProvider.cs
    │   │   ├── StudentAiAgent.cs
    │   │   ├── SupportBotQueryService.cs
    │   │   └── SystemPromptTemplate.cs
    │   ├── Admissions/
    │   │   └── AdmissionsServices.cs
    │   ├── ApiGateway/
    │   │   ├── ApiGatewayServiceExtensions.cs
    │   │   └── ApiKeyService.cs
    │   ├── AssetInventory/
    │   │   ├── AssetInventoryService.cs
    │   │   └── AssetInventoryServiceExtensions.cs
    │   ├── Backup/
    │   │   ├── AutomatedBackupJob.cs
    │   │   ├── BackupEncryptionService.cs
    │   │   ├── BackupService.cs
    │   │   ├── DRServiceExtensions.cs
    │   │   └── DatabaseHealthService.cs
    │   ├── Campus/
    │   │   ├── CafeteriaService.cs
    │   │   ├── CampusModuleExtensions.cs
    │   │   ├── CampusService.cs
    │   │   ├── CampusServiceExtensions.cs
    │   │   ├── ConsolidatedReportingService.cs
    │   │   └── TransportService.cs
    │   ├── CampusOperations/
    │   │   ├── CampusOperationsModuleExtensions.cs
    │   │   ├── CampusOperationsService.cs
    │   │   ├── CampusOperationsServiceExtensions.cs
    │   │   └── ElectionService.cs
    │   ├── Cloud/
    │   │   ├── Aws/
    │   │   │   └── AwsProvider.cs
    │   │   ├── Azure/
    │   │   │   └── AzureProvider.cs
    │   │   ├── Gcp/
    │   │   │   └── GcpProvider.cs
    │   │   ├── Infisical/
    │   │   │   └── InfisicalSecretService.cs
    │   │   ├── OnPremise/
    │   │   │   └── OnPremiseProvider.cs
    │   │   ├── R2/
    │   │   │   └── R2StorageService.cs
    │   │   ├── CloudProviderExtensions.cs
    │   │   └── VaultConfigurationExtensions.cs
    │   ├── Compliance/
    │   │   ├── ComplianceService.cs
    │   │   └── ComplianceServiceExtensions.cs
    │   ├── Config/
    │   │   ├── DefaultTenantConfigTemplate.cs
    │   │   ├── TenantConfigService.cs
    │   │   └── TenantConfigServiceExtensions.cs
    │   ├── ConfigCenter/
    │   │   ├── ConfigCenterServiceExtensions.cs
    │   │   └── ConfigurationCenterService.cs
    │   ├── Configuration/
    │   │   └── EnvironmentSubstitutionExtensions.cs
    │   ├── CoreERP/
    │   │   └── CoreErpServiceExtensions.cs
    │   ├── Curriculum/
    │   │   ├── CurriculumService.cs
    │   │   └── CurriculumServiceExtensions.cs
    │   ├── DataImport/
    │   │   ├── DataImportService.cs
    │   │   └── DataImportServiceExtensions.cs
    │   ├── DegreeAudit/
    │   │   ├── DegreeAuditService.cs
    │   │   └── DegreeAuditServiceExtensions.cs
    │   ├── Deployment/
    │   │   ├── Seeders/
    │   │   │   └── DemoSeeder.cs
    │   │   ├── DemoDataSeeder.cs
    │   │   ├── DeploymentReadinessChecker.cs
    │   │   ├── ExtendedHealthChecks.cs
    │   │   ├── SeedPipeline.cs
    │   │   └── SystemAlertService.cs
    │   ├── DigitalTwin/
    │   │   ├── DigitalTwinService.cs
    │   │   └── DigitalTwinServiceExtensions.cs
    │   ├── DocumentVault/
    │   │   ├── DocumentVaultService.cs
    │   │   └── DocumentVaultServiceExtensions.cs
    │   ├── Examination/
    │   │   └── GradingService.cs
    │   ├── Finance/
    │   │   ├── Gateways/
    │   │   │   └── PaymentGatewayAdapters.cs
    │   │   ├── AccountingStandardsEngine.cs
    │   │   ├── AncillaryFinanceService.cs
    │   │   ├── AssetDepreciationCalculator.cs
    │   │   ├── AuditSealService.cs
    │   │   ├── DefaultPredictionService.cs
    │   │   ├── DefaultPredictionServiceExtensions.cs
    │   │   ├── FinanceAssetLifecycleService.cs
    │   │   ├── FinanceContinuousControlService.cs
    │   │   ├── FinanceModuleService.cs
    │   │   ├── FinanceModuleServiceExtensions.cs
    │   │   ├── FinancePayrollSettlementService.cs
    │   │   ├── FinanceProcurementService.cs
    │   │   ├── FinanceRegulatoryReportService.cs
    │   │   ├── FinanceScholarshipService.cs
    │   │   ├── FinanceServices.cs
    │   │   ├── FinanceTreasuryService.cs
    │   │   ├── PaymentPlanPenaltyCalculator.cs
    │   │   ├── PaymentPlanSchedule.cs
    │   │   ├── PaymentPlanWorker.cs
    │   │   ├── VendorSanctionsService.cs
    │   │   ├── VoucherImportService.cs
    │   │   ├── accounting-standards.json
    │   │   ├── finance-control-library.json
    │   │   └── regulatory-schemas.json
    │   ├── Forms/
    │   │   ├── FormService.cs
    │   │   └── FormsServiceExtensions.cs
    │   ├── Foundation/
    │   │   ├── FoundationServices.cs
    │   │   └── StorageProviders.cs
    │   ├── Governance/
    │   │   ├── AccreditationBoardExtensions.cs
    │   │   ├── AccreditationService.cs
    │   │   ├── BoardPortalService.cs
    │   │   ├── GovernanceService.cs
    │   │   └── GovernanceServiceExtensions.cs
    │   ├── Hr/
    │   │   ├── PayrollService.cs
    │   │   ├── VisitingFacultyService.cs
    │   │   └── VisitingFacultyServiceExtensions.cs
    │   ├── I18n/
    │   │   ├── CountryPackService.cs
    │   │   ├── I18nServiceExtensions.cs
    │   │   └── TranslationService.cs
    │   ├── Identity/
    │   │   ├── IdentityService.cs
    │   │   ├── IdentityServiceExtensions.cs
    │   │   └── SamlService.cs
    │   ├── Installation/
    │   │   └── InstallationStateService.cs
    │   ├── License/
    │   │   └── LicenseService.cs
    │   ├── Marketplace/
    │   │   ├── ExtensionService.cs
    │   │   └── MarketplaceServiceExtensions.cs
    │   ├── MicrosoftEcosystem/
    │   │   ├── MicrosoftEcosystemServiceExtensions.cs
    │   │   └── MicrosoftEcosystemServices.cs
    │   ├── Migrations/
    │   │   ├── 20260726160353_V1_0_AegisCoreBaseline.Designer.cs
    │   │   ├── 20260726160353_V1_0_AegisCoreBaseline.cs
    │   │   ├── 20260726195806_V1_1_ExaminationSemesterGrades.Designer.cs
    │   │   ├── 20260726195806_V1_1_ExaminationSemesterGrades.cs
    │   │   ├── 20260730161904_V1_2_FinanceCore.Designer.cs
    │   │   ├── 20260730161904_V1_2_FinanceCore.cs
    │   │   ├── 20260730164647_V1_3_WizardSessions.Designer.cs
    │   │   ├── 20260730164647_V1_3_WizardSessions.cs
    │   │   ├── 20260730171751_V1_4_WizardAdminCredentials.Designer.cs
    │   │   ├── 20260730171751_V1_4_WizardAdminCredentials.cs
    │   │   ├── 20260730173916_V1_5_UserPasswordHash.Designer.cs
    │   │   ├── 20260730173916_V1_5_UserPasswordHash.cs
    │   │   ├── 20260730180000_LedgerIsolation.cs
    │   │   ├── 20260730215247_FinanceModelPendingChanges.Designer.cs
    │   │   ├── 20260730215247_FinanceModelPendingChanges.cs
    │   │   ├── 20260731000000_FinanceWebhookIdempotency.cs
    │   │   ├── 20260731000500_FinanceVoucherPqcPublicKey.cs
    │   │   ├── 20260731001500_FinanceAuditSeals.cs
    │   │   ├── 20260731002000_FinancePaymentPlans.cs
    │   │   ├── 20260731002500_FinanceSecurityDeposits.cs
    │   │   ├── 20260731003000_LedgerSandboxFlag.cs
    │   │   ├── 20260731003500_FinancePaymentPlanAccounting.cs
    │   │   ├── 20260731004000_FinanceVendors.cs
    │   │   ├── 20260731004500_StandingPurchaseOrderSpend.cs
    │   │   ├── 20260731005000_FinanceReportArtifacts.cs
    │   │   ├── 20260731005500_FinanceReportProfiles.cs
    │   │   ├── 20260731006000_FinancePqcKeys.cs
    │   │   ├── 20260731006500_FinancePayrollPosting.cs
    │   │   ├── 20260731007000_FinanceTreasuryFeeds.cs
    │   │   ├── 20260731007500_FinanceGrantsScholarships.cs
    │   │   ├── 20260731008000_FinanceUtilities.cs
    │   │   ├── 20260731008500_FinanceInsurance.cs
    │   │   ├── 20260731009000_FinanceInventory.cs
    │   │   ├── 20260731009500_FinanceFleet.cs
    │   │   ├── 20260731010000_FinanceInvestments.cs
    │   │   ├── 20260731010500_FinanceTax.cs
    │   │   ├── 20260731095302_FinanceForeignCurrencyTransactions.Designer.cs
    │   │   ├── 20260731095302_FinanceForeignCurrencyTransactions.cs
    │   │   ├── 20260731100318_FinanceForeignCurrencyModelAlignment.Designer.cs
    │   │   ├── 20260731100318_FinanceForeignCurrencyModelAlignment.cs
    │   │   ├── 20260731111000_TenantFinanceConfiguration.cs
    │   │   ├── 20260731111724_FinancePerformanceIndexes.Designer.cs
    │   │   ├── 20260731111724_FinancePerformanceIndexes.cs
    │   │   ├── 20260731120145_FinanceGatewayEventIndex.Designer.cs
    │   │   ├── 20260731120145_FinanceGatewayEventIndex.cs
    │   │   ├── 20260731130000_FinancePettyCash.cs
    │   │   ├── 20260731140000_FinanceVoucherPostingGuards.cs
    │   │   ├── 20260731143000_FinanceLedgerAppendOnlyGuards.cs
    │   │   ├── 20260731150000_FinanceLibrary.cs
    │   │   ├── 20260731160000_FinanceAccountingEngine.Designer.cs
    │   │   ├── 20260731160000_FinanceAccountingEngine.cs
    │   │   ├── 20260731170000_FinancePqcKeyCeremonies.Designer.cs
    │   │   ├── 20260731170000_FinancePqcKeyCeremonies.cs
    │   │   ├── 20260731180000_FinanceLedgerHashChain.Designer.cs
    │   │   ├── 20260731180000_FinanceLedgerHashChain.cs
    │   │   ├── 20260731190000_FinanceVoucherOperations.Designer.cs
    │   │   ├── 20260731190000_FinanceVoucherOperations.cs
    │   │   ├── 20260731200000_FinanceProcurementWorkflow.Designer.cs
    │   │   ├── 20260731200000_FinanceProcurementWorkflow.cs
    │   │   ├── 20260731210000_FinancePayrollSettlement.Designer.cs
    │   │   ├── 20260731210000_FinancePayrollSettlement.cs
    │   │   ├── 20260731220000_FinanceScholarshipLifecycle.Designer.cs
    │   │   ├── 20260731220000_FinanceScholarshipLifecycle.cs
    │   │   ├── 20260731230000_FinanceAssetAccountingLifecycle.Designer.cs
    │   │   ├── 20260731230000_FinanceAssetAccountingLifecycle.cs
    │   │   ├── 20260801000000_FinanceTreasuryBenchmarks.Designer.cs
    │   │   ├── 20260801000000_FinanceTreasuryBenchmarks.cs
    │   │   ├── 20260801010000_FinanceAncillaryLifecycle.Designer.cs
    │   │   ├── 20260801010000_FinanceAncillaryLifecycle.cs
    │   │   ├── 20260801020000_FinanceRegulatorySchemas.Designer.cs
    │   │   ├── 20260801020000_FinanceRegulatorySchemas.cs
    │   │   ├── 20260801135225_FinanceAuditCompliance.Designer.cs
    │   │   ├── 20260801135225_FinanceAuditCompliance.cs
    │   │   ├── 20260801140104_FinanceComplianceTenantIsolation.Designer.cs
    │   │   ├── 20260801140104_FinanceComplianceTenantIsolation.cs
    │   │   ├── 20260801161936_FinanceTenantActivation.Designer.cs
    │   │   ├── 20260801161936_FinanceTenantActivation.cs
    │   │   ├── 20260801163108_FinanceCostCenterTenantScope.Designer.cs
    │   │   ├── 20260801163108_FinanceCostCenterTenantScope.cs
    │   │   ├── 20260801163925_FinanceCashFlowClassification.Designer.cs
    │   │   ├── 20260801163925_FinanceCashFlowClassification.cs
    │   │   ├── 20260804144527_V2_0_ExtendedPlatformModules.Designer.cs
    │   │   ├── 20260804144527_V2_0_ExtendedPlatformModules.cs
    │   │   ├── 20260805100112_FixPendingModelChanges.Designer.cs
    │   │   ├── 20260805100112_FixPendingModelChanges.cs
    │   │   ├── 20260805125100_V2_1_UserRefreshTokens.Designer.cs
    │   │   ├── 20260805125100_V2_1_UserRefreshTokens.cs
    │   │   ├── 20260805143559_V2_2_UserRefreshNotification.Designer.cs
    │   │   ├── 20260805143559_V2_2_UserRefreshNotification.cs
    │   │   ├── AegisDbContextModelSnapshot.cs
    │   │   └── V1.0_AegisCoreBaseline.sql
    │   ├── Obe/
    │   │   └── ObeServices.cs
    │   ├── Onboarding/
    │   │   ├── EnhancedSetupService.cs
    │   │   ├── OnboardingService.cs
    │   │   ├── OnboardingServiceExtensions.cs
    │   │   ├── SetupImportService.cs
    │   │   ├── WizardActivationService.cs
    │   │   ├── WizardGatewayService.cs
    │   │   ├── WizardProvisioningService.cs
    │   │   └── WizardSessionService.cs
    │   ├── Payments/
    │   │   └── Raast/
    │   │       └── RaastApiClient.cs
    │   ├── Platform/
    │   │   ├── ExtendedPlatformServiceExtensions.cs
    │   │   ├── ExtendedPlatformServices.cs
    │   │   ├── ExtendedPlatformServices2.cs
    │   │   ├── ExtendedPlatformServices3.cs
    │   │   ├── ExtendedPlatformServices4.cs
    │   │   ├── ExtendedPlatformServices5.cs
    │   │   ├── PlatformServiceExtensions.cs
    │   │   └── PlatformServices.cs
    │   ├── ProfilePictures/
    │   │   ├── ProfilePictureService.cs
    │   │   └── ProfilePictureServiceExtensions.cs
    │   ├── Replication/
    │   │   ├── ReplicaManagerService.cs
    │   │   └── ReplicationServiceExtensions.cs
    │   ├── Repositories/
    │   │   ├── ApiKeyRepository.cs
    │   │   ├── AttendanceRepository.cs
    │   │   ├── BackupRepository.cs
    │   │   ├── CalibrationRecordRepository.cs
    │   │   ├── CampusRepository.cs
    │   │   ├── ComplianceRepository.cs
    │   │   ├── ConfigCenterRepository.cs
    │   │   ├── CourseRepository.cs
    │   │   ├── DegreeAuditRepository.cs
    │   │   ├── DigitalTwinRepository.cs
    │   │   ├── EnrollmentRepository.cs
    │   │   ├── EquipmentBookingRepository.cs
    │   │   ├── EquipmentMaintenanceRepository.cs
    │   │   ├── ExamSeatingRepository.cs
    │   │   ├── ExtendedPlatformRepository.cs
    │   │   ├── ExtensionRepository.cs
    │   │   ├── FormRepository.cs
    │   │   ├── GateEntryRepository.cs
    │   │   ├── GuardRoundRepository.cs
    │   │   ├── GuardShiftRepository.cs
    │   │   ├── I18nRepository.cs
    │   │   ├── IdentityRepository.cs
    │   │   ├── LabBookingRepository.cs
    │   │   ├── LabConsumableRepository.cs
    │   │   ├── LabEquipmentRepository.cs
    │   │   ├── LabSafetyInspectionRepository.cs
    │   │   ├── LoginAuditRepository.cs
    │   │   ├── LostFoundItemRepository.cs
    │   │   ├── MaintenanceContractorRepository.cs
    │   │   ├── MaintenanceTicketRepository.cs
    │   │   ├── PlatformRepository.cs
    │   │   ├── PreventiveMaintenanceScheduleRepository.cs
    │   │   ├── Repository.cs
    │   │   ├── ResearchLabRepository.cs
    │   │   ├── RuleRepository.cs
    │   │   ├── SLADefinitionRepository.cs
    │   │   ├── SecurityGuardRepository.cs
    │   │   ├── SecurityIncidentRepository.cs
    │   │   ├── StudentRepository.cs
    │   │   ├── VehiclePassRepository.cs
    │   │   ├── VisitorRepository.cs
    │   │   ├── WorkOrderRepository.cs
    │   │   └── WorkflowRepositories.cs
    │   ├── RuleEngine/
    │   │   ├── RuleEngineService.cs
    │   │   └── RuleEngineServiceExtensions.cs
    │   ├── Search/
    │   │   ├── ElasticsearchIndex.cs
    │   │   ├── SearchService.cs
    │   │   └── SearchServiceExtensions.cs
    │   ├── Security/
    │   │   ├── Abac/
    │   │   │   └── AbacPolicyService.cs
    │   │   ├── Pqc/
    │   │   │   ├── HybridKemService.cs
    │   │   │   ├── HybridPqcReplayStore.cs
    │   │   │   ├── PqcCryptographyService.cs
    │   │   │   ├── PqcKeyCeremonyService.cs
    │   │   │   ├── PqcSignatureService.cs
    │   │   │   └── PqcSigningQueue.cs
    │   │   ├── AuditLogService.cs
    │   │   ├── BruteForceProtectionService.cs
    │   │   ├── CasbinServiceExtensions.cs
    │   │   ├── JwtTokenService.cs
    │   │   ├── LoginAuditService.cs
    │   │   ├── LoginAuditServiceExtensions.cs
    │   │   ├── PasswordHashingService.cs
    │   │   └── UploadValidationService.cs
    │   ├── SsoHitl/
    │   │   ├── SsoHitlServiceExtensions.cs
    │   │   └── SsoHitlServices.cs
    │   ├── StudentLifecycle/
    │   │   ├── ForeignStudentService.cs
    │   │   ├── GamificationService.cs
    │   │   ├── MentalHealthService.cs
    │   │   ├── StudentLifecycleModuleExtensions.cs
    │   │   ├── StudentLifecycleService.cs
    │   │   ├── StudentLifecycleServiceExtensions.cs
    │   │   └── VoiceAssistantService.cs
    │   ├── Support/
    │   │   ├── SupportServiceExtensions.cs
    │   │   └── SupportTicketService.cs
    │   ├── SyncLag/
    │   │   ├── SyncLagService.cs
    │   │   └── SyncLagServiceExtensions.cs
    │   ├── Workflow/
    │   │   ├── StateMachine/
    │   │   │   └── StateMachineService.cs
    │   │   ├── ApprovalService.cs
    │   │   ├── EmailNotificationService.cs
    │   │   ├── WorkflowEngine.cs
    │   │   ├── WorkflowServiceExtensions.cs
    │   │   └── WorkflowTemplateSeeder.cs
    │   ├── AegisCore.Infrastructure.csproj
    │   ├── AegisDbContext.cs
    │   ├── AegisDbContextFactory.cs
    │   ├── DbContextServiceExtensions.cs
    │   ├── GlobalUsings.cs
    │   ├── InfrastructureServiceExtensions.cs
    │   ├── TenantSessionInterceptor.cs
    │   └── UnitOfWork.cs
    ├── AegisCore.LedgerVerifier/
    │   ├── AegisCore.LedgerVerifier.csproj
    │   └── Program.cs
    ├── AegisCore.Mobile/
    │   ├── apps/
    │   │   ├── faculty/
    │   │   │   └── App.tsx
    │   │   └── student/
    │   │       └── App.tsx
    │   ├── e2e/
    │   │   └── aegiscore.e2e.ts
    │   ├── src/
    │   │   ├── api/
    │   │   │   └── client.ts
    │   │   ├── auth/
    │   │   │   ├── biometrics.ts
    │   │   │   └── pushNotifications.ts
    │   │   ├── db/
    │   │   │   └── schema.ts
    │   │   ├── i18n/
    │   │   │   └── strings.ts
    │   │   ├── screens/
    │   │   │   ├── Screen.tsx
    │   │   │   ├── screenData.test.ts
    │   │   │   └── screenData.ts
    │   │   ├── state/
    │   │   │   ├── sessionSlice.ts
    │   │   │   └── store.ts
    │   │   ├── sync/
    │   │   │   └── offlineQueue.ts
    │   │   └── ui/
    │   │       ├── RoleSwitcher.tsx
    │   │       └── styles.ts
    │   ├── .gitignore
    │   ├── App.tsx
    │   ├── README.md
    │   ├── app.json
    │   ├── babel.config.js
    │   ├── eas.json
    │   ├── jest.config.js
    │   ├── package-lock.json
    │   ├── package.json
    │   └── tsconfig.json
    ├── AegisCore.Native/
    │   ├── android/
    │   │   ├── app/
    │   │   │   ├── src/
    │   │   │   │   └── main/
    │   │   │   │       ├── kotlin/
    │   │   │   │       │   └── pk/
    │   │   │   │       │       └── edu/
    │   │   │   │       │           └── aegiscore/
    │   │   │   │       │               └── mobile/
    │   │   │   │       │                   ├── Api.kt
    │   │   │   │       │                   ├── MainActivity.kt
    │   │   │   │       │                   └── OfflineAttendance.kt
    │   │   │   │       ├── res/
    │   │   │   │       │   └── values/
    │   │   │   │       │       └── styles.xml
    │   │   │   │       └── AndroidManifest.xml
    │   │   │   └── build.gradle.kts
    │   │   ├── build.gradle.kts
    │   │   └── settings.gradle.kts
    │   ├── docs/
    │   │   └── docker-mobile-architecture.md
    │   └── ios/
    │       ├── AegisCore/
    │       │   ├── AegisCoreApp.swift
    │       │   ├── ApiClient.swift
    │       │   └── BiometricGate.swift
    │       └── Package.swift
    ├── AegisCore.PluginCli/
    │   ├── AegisCore.PluginCli.csproj
    │   ├── Program.cs
    │   └── README.md
    ├── AegisCore.PluginSdk/
    │   ├── AegisCore.PluginSdk.csproj
    │   ├── PluginContracts.cs
    │   └── README.md
    ├── AegisCore.Portal/
    │   ├── public/
    │   │   ├── aegiscore-logo.png
    │   │   └── manifest.webmanifest
    │   ├── src/
    │   │   ├── app/
    │   │   │   ├── achievements/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── attendance/
    │   │   │   │   └── page.tsx
    │   │   │   ├── audit/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── auditor/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── board/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── cafeteria/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── catalog/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── classes/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── completion/
    │   │   │   │   └── page.tsx
    │   │   │   ├── convocations/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── data-import/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── defaulter-prediction/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── document-vault/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── elections/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── examinations/
    │   │   │   │   └── page.tsx
    │   │   │   ├── finance/
    │   │   │   │   ├── department/
    │   │   │   │   │   └── [departmentId]/
    │   │   │   │   │       └── page.tsx
    │   │   │   │   ├── gateways/
    │   │   │   │   │   └── page.tsx
    │   │   │   │   ├── operations/
    │   │   │   │   │   └── page.tsx
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── foreign-students/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── freezes/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── grade-appeals/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── grade-distributions/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── imports/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── login/
    │   │   │   │   └── page.tsx
    │   │   │   ├── mental-health/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── onboarding/
    │   │   │   │   └── page.tsx
    │   │   │   ├── placement/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── plagiarism/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── portal-health/
    │   │   │   │   └── route.ts
    │   │   │   ├── prerequisite-conflicts/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── proctoring/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── readiness/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── replication/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── setup/
    │   │   │   │   └── page.tsx
    │   │   │   ├── student/
    │   │   │   │   ├── dashboard/
    │   │   │   │   │   └── page.tsx
    │   │   │   │   ├── finance/
    │   │   │   │   │   └── [studentId]/
    │   │   │   │   │       └── page.tsx
    │   │   │   │   ├── profile/
    │   │   │   │   │   └── page.tsx
    │   │   │   │   └── transcript/
    │   │   │   │       └── page.tsx
    │   │   │   ├── sync-lag/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── transport/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── visiting-faculty/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── voice/
    │   │   │   │   ├── layout.tsx
    │   │   │   │   └── page.tsx
    │   │   │   ├── globals.css
    │   │   │   ├── layout.tsx
    │   │   │   └── page.tsx
    │   │   ├── components/
    │   │   │   ├── academic/
    │   │   │   │   ├── grade-appeal-form.tsx
    │   │   │   │   ├── grade-appeals-dashboard.tsx
    │   │   │   │   └── module-dashboard.tsx
    │   │   │   ├── finance/
    │   │   │   │   ├── approval-queue.tsx
    │   │   │   │   ├── audit-dashboard.tsx
    │   │   │   │   ├── auditor-portal.tsx
    │   │   │   │   ├── department-finance-dashboard.tsx
    │   │   │   │   ├── finance-dashboard.tsx
    │   │   │   │   ├── finance-realtime-refresh.tsx
    │   │   │   │   └── student-payment-action.tsx
    │   │   │   ├── layout/
    │   │   │   │   └── AppShell.tsx
    │   │   │   ├── nchart/
    │   │   │   │   ├── index.ts
    │   │   │   │   ├── nchart-data-table.tsx
    │   │   │   │   ├── nchart-gauge.tsx
    │   │   │   │   ├── nchart-heatmap.tsx
    │   │   │   │   ├── nchart-insight-chip.tsx
    │   │   │   │   ├── nchart-legend.tsx
    │   │   │   │   ├── nchart-tooltip.tsx
    │   │   │   │   ├── nchart-zoom-layer.tsx
    │   │   │   │   ├── nchart.tsx
    │   │   │   │   └── types.ts
    │   │   │   ├── wizard/
    │   │   │   │   └── WizardShell.tsx
    │   │   │   ├── locale-switcher.tsx
    │   │   │   ├── paginated-table.tsx
    │   │   │   ├── personality-layout-wrapper.tsx
    │   │   │   ├── personality-toggle.tsx
    │   │   │   ├── portal-shell.tsx
    │   │   │   └── setup-wizard.tsx
    │   │   ├── hooks/
    │   │   │   └── use-pagination.ts
    │   │   ├── i18n/
    │   │   │   ├── messages.ts
    │   │   │   ├── personality-messages.ts
    │   │   │   ├── request.ts
    │   │   │   └── server.ts
    │   │   ├── lib/
    │   │   │   ├── api-client.ts
    │   │   │   ├── personality-context.tsx
    │   │   │   ├── personality-translator.ts
    │   │   │   ├── roles.ts
    │   │   │   └── server-api.ts
    │   │   └── pages/
    │   │       └── wizard/
    │   │           └── Step2_5PaymentGateways.tsx
    │   ├── tests/
    │   │   └── aegiscore.spec.ts
    │   ├── .dockerignore
    │   ├── .env.example
    │   ├── ACCESSIBILITY.md
    │   ├── AEGISCORE_CAPABILITIES.md
    │   ├── DEPARTMENT_STACK_AND_CAPABILITIES.txt
    │   ├── Dockerfile
    │   ├── README.md
    │   ├── TECHNICAL_SPEC_SHEET.md
    │   ├── eslint.config.mjs
    │   ├── next-env.d.ts
    │   ├── next.config.ts
    │   ├── nginx.conf
    │   ├── package-lock.json
    │   ├── package.json
    │   ├── playwright.config.ts
    │   ├── tsconfig.json
    │   └── wrangler.toml
    ├── AegisCore.Setup/
    │   ├── Pages/
    │   │   └── Index.razor
    │   ├── Shared/
    │   │   └── MainLayout.razor
    │   ├── AegisCore.Setup.csproj
    │   ├── App.razor
    │   ├── Program.cs
    │   ├── Routes.razor
    │   └── _Imports.razor
    ├── AegisCore.Tests/
    │   ├── AchievementControllerTests.cs
    │   ├── AdmissionsTests.cs
    │   ├── AegisCore.Tests.csproj
    │   ├── AssetInventoryTests.cs
    │   ├── BoardControllerTests.cs
    │   ├── CampusOperationsTests.cs
    │   ├── CommunicationsTests.cs
    │   ├── CompleteSuiteArchitectureTests.cs
    │   ├── ConvocationControllerTests.cs
    │   ├── CurriculumTests.cs
    │   ├── DefaulterPredictionControllerTests.cs
    │   ├── ElectionControllerTests.cs
    │   ├── EnhancedSetupAndSupportTests.cs
    │   ├── FinanceAuditComplianceTests.cs
    │   ├── FinanceDashboardEndpointTests.cs
    │   ├── FinancePaymentEndpointTests.cs
    │   ├── FinanceProcurementSodTests.cs
    │   ├── FinanceTests.cs
    │   ├── ForeignStudentControllerTests.cs
    │   ├── FoundationStorageTests.cs
    │   ├── GlobalUsings.cs
    │   ├── GovernanceTests.cs
    │   ├── GradeAppealsControllerTests.cs
    │   ├── HybridPqcTransportMiddlewareTests.cs
    │   ├── IdentityPatchTests.cs
    │   ├── InfisicalSecretServiceTests.cs
    │   ├── InstallationStateServiceTests.cs
    │   ├── LicenseServiceTests.cs
    │   ├── LoginAuditControllerTests.cs
    │   ├── MarketplaceRevenueTests.cs
    │   ├── MentalHealthControllerTests.cs
    │   ├── MicrosoftEcosystemTests.cs
    │   ├── OperationalRuntimeTests.cs
    │   ├── PasswordHashingServiceTests.cs
    │   ├── PayrollServiceTests.cs
    │   ├── PlagiarismControllerTests.cs
    │   ├── PqcSignatureServiceTests.cs
    │   ├── ProctoringControllerTests.cs
    │   ├── PromptFiveEdgeCaseTests.cs
    │   ├── S1FoundationTests.cs
    │   ├── SecurityHardeningTests.cs
    │   ├── SemesterFreezeControllerTests.cs
    │   ├── SetupControllerRouteTests.cs
    │   ├── SsoHitlTests.cs
    │   ├── StudentLifecycleTests.cs
    │   ├── TenantContextMiddlewareTests.cs
    │   └── WizardActivationTests.cs
    ├── AegisCore.Web/
    │   ├── Pages/
    │   │   └── Index.razor
    │   ├── Shared/
    │   │   └── MainLayout.razor
    │   ├── wwwroot/
    │   │   └── index.html
    │   ├── AegisCore.Web.csproj
    │   ├── App.razor
    │   ├── GlobalUsings.cs
    │   ├── Program.cs
    │   └── _Imports.razor
    ├── aegiscore-android/
    │   ├── .github/
    │   │   └── workflows/
    │   │       ├── android-build.yml
    │   │       └── android-deploy.yml
    │   ├── app/
    │   │   ├── src/
    │   │   │   └── main/
    │   │   │       ├── kotlin/
    │   │   │       │   └── pk/
    │   │   │       │       └── edu/
    │   │   │       │           └── aegiscore/
    │   │   │       │               └── mobile/
    │   │   │       │                   ├── Api.kt
    │   │   │       │                   ├── MainActivity.kt
    │   │   │       │                   └── OfflineAttendance.kt
    │   │   │       ├── res/
    │   │   │       │   └── values/
    │   │   │       │       └── styles.xml
    │   │   │       └── AndroidManifest.xml
    │   │   └── build.gradle.kts
    │   ├── .gitignore
    │   ├── CONTRIBUTING.md
    │   ├── README.md
    │   ├── build.gradle.kts
    │   └── settings.gradle.kts
    ├── aegiscore-backend/
    │   ├── .github/
    │   │   └── workflows/
    │   │       └── deploy.yml
    │   ├── AegisCore.API/
    │   │   ├── Authorization/
    │   │   │   ├── PermissionAuthorizationHandler.cs
    │   │   │   ├── RequireModuleAttribute.cs
    │   │   │   └── RequirePermissionAttribute.cs
    │   │   ├── Background/
    │   │   │   ├── AutoLockBackgroundService.cs
    │   │   │   └── LicenseHeartbeatHostedService.cs
    │   │   ├── Controllers/
    │   │   │   ├── AssetInventoryController.cs
    │   │   │   ├── CampusController.cs
    │   │   │   ├── CampusOperationsController.cs
    │   │   │   ├── CommunicationsController.cs
    │   │   │   ├── ComplianceController.cs
    │   │   │   ├── ConfigCenterController.cs
    │   │   │   ├── CoreErpControllers.cs
    │   │   │   ├── CurriculumController.cs
    │   │   │   ├── DegreeAuditController.cs
    │   │   │   ├── DeveloperControllers.cs
    │   │   │   ├── DigitalTwinController.cs
    │   │   │   ├── DisasterRecoveryController.cs
    │   │   │   ├── ExtendedPlatformControllers.cs
    │   │   │   ├── FinanceController.cs
    │   │   │   ├── FormsController.cs
    │   │   │   ├── GovernanceController.cs
    │   │   │   ├── HrController.cs
    │   │   │   ├── I18nController.cs
    │   │   │   ├── IdentityController.cs
    │   │   │   ├── LicenseController.cs
    │   │   │   ├── LoginAuditController.cs
    │   │   │   ├── MarketplaceController.cs
    │   │   │   ├── MicrosoftEcosystemController.cs
    │   │   │   ├── PlatformControllers.cs
    │   │   │   ├── RuleEngineController.cs
    │   │   │   ├── S2AdmissionsController.cs
    │   │   │   ├── S3ExaminationController.cs
    │   │   │   ├── S5ObeController.cs
    │   │   │   ├── SamlController.cs
    │   │   │   ├── ScimController.cs
    │   │   │   ├── SearchController.cs
    │   │   │   ├── SetupController.cs
    │   │   │   ├── SsoHitlController.cs
    │   │   │   └── StudentLifecycleController.cs
    │   │   ├── Extensions/
    │   │   │   ├── ApiServicesExtensions.cs
    │   │   │   └── ProductionServicesExtensions.cs
    │   │   ├── Hubs/
    │   │   │   ├── NotificationHub.cs
    │   │   │   ├── SignalRNotificationRealtimePublisher.cs
    │   │   │   └── StateMachineHub.cs
    │   │   ├── Middleware/
    │   │   │   ├── CasbinAuthorizationMiddleware.cs
    │   │   │   ├── GlobalExceptionHandlerMiddleware.cs
    │   │   │   ├── InfrastructureMiddleware.cs
    │   │   │   ├── LicenseLockoutMiddleware.cs
    │   │   │   ├── RateLimitingMiddleware.cs
    │   │   │   ├── SetupRequiredMiddleware.cs
    │   │   │   └── ValidationExceptionMiddleware.cs
    │   │   ├── wwwroot/
    │   │   │   ├── setup/
    │   │   │   │   └── index.html
    │   │   │   └── templates/
    │   │   │       └── whatsapp-templates.json
    │   │   ├── AegisCore.API.csproj
    │   │   ├── GlobalUsings.cs
    │   │   ├── Program.cs
    │   │   ├── appsettings.Production.json
    │   │   └── appsettings.json
    │   ├── AegisCore.Application/
    │   │   ├── AI/
    │   │   │   ├── AiAgentRequest.cs
    │   │   │   ├── AiAgentResponse.cs
    │   │   │   ├── GradePredictionResult.cs
    │   │   │   ├── IAiAgent.cs
    │   │   │   ├── IDocumentOcrService.cs
    │   │   │   ├── IEmbeddingProvider.cs
    │   │   │   ├── ILlmProvider.cs
    │   │   │   ├── IVectorStore.cs
    │   │   │   ├── LlmOptions.cs
    │   │   │   ├── OcrResult.cs
    │   │   │   ├── StudentCourseRecommendation.cs
    │   │   │   └── VectorChunk.cs
    │   │   ├── Abstractions/
    │   │   │   └── Cloud/
    │   │   │       └── CloudAbstractions.cs
    │   │   ├── Admissions/
    │   │   │   └── AdmissionsContracts.cs
    │   │   ├── ApiGateway/
    │   │   │   ├── ApiGatewayDtos.cs
    │   │   │   └── IApiKeyService.cs
    │   │   ├── AssetInventory/
    │   │   │   └── AssetInventoryContracts.cs
    │   │   ├── Backup/
    │   │   │   ├── DRDtos.cs
    │   │   │   └── IBackupService.cs
    │   │   ├── Campus/
    │   │   │   ├── CampusDtos.cs
    │   │   │   └── ICampusService.cs
    │   │   ├── CampusOperations/
    │   │   │   └── CampusOperationsContracts.cs
    │   │   ├── Common/
    │   │   │   └── Interfaces/
    │   │   │       ├── IApiKeyRepository.cs
    │   │   │       ├── IAttendanceRepository.cs
    │   │   │       ├── IAuditLogService.cs
    │   │   │       ├── IBackupRepository.cs
    │   │   │       ├── IBruteForceProtectionService.cs
    │   │   │       ├── ICalibrationRecordRepository.cs
    │   │   │       ├── ICampusRepository.cs
    │   │   │       ├── IComplianceRepository.cs
    │   │   │       ├── IConfigCenterRepository.cs
    │   │   │       ├── ICourseRepository.cs
    │   │   │       ├── IDegreeAuditRepository.cs
    │   │   │       ├── IDigitalTwinRepository.cs
    │   │   │       ├── IEnrollmentRepository.cs
    │   │   │       ├── IEquipmentBookingRepository.cs
    │   │   │       ├── IEquipmentMaintenanceRepository.cs
    │   │   │       ├── IExamSeatingRepository.cs
    │   │   │       ├── IExtendedPlatformRepository.cs
    │   │   │       ├── IExtensionRepository.cs
    │   │   │       ├── IFormRepository.cs
    │   │   │       ├── IGateEntryRepository.cs
    │   │   │       ├── IGuardRoundRepository.cs
    │   │   │       ├── IGuardShiftRepository.cs
    │   │   │       ├── II18nRepository.cs
    │   │   │       ├── IIdentityRepository.cs
    │   │   │       ├── IJwtTokenService.cs
    │   │   │       ├── ILabBookingRepository.cs
    │   │   │       ├── ILabConsumableRepository.cs
    │   │   │       ├── ILabEquipmentRepository.cs
    │   │   │       ├── ILabSafetyInspectionRepository.cs
    │   │   │       ├── ILoginAuditRepository.cs
    │   │   │       ├── ILostFoundItemRepository.cs
    │   │   │       ├── IMaintenanceContractorRepository.cs
    │   │   │       ├── IMaintenanceTicketRepository.cs
    │   │   │       ├── IPasswordHashingService.cs
    │   │   │       ├── IPlatformRepository.cs
    │   │   │       ├── IPreventiveMaintenanceScheduleRepository.cs
    │   │   │       ├── IRepository.cs
    │   │   │       ├── IResearchLabRepository.cs
    │   │   │       ├── IRuleRepository.cs
    │   │   │       ├── ISLADefinitionRepository.cs
    │   │   │       ├── ISecurityGuardRepository.cs
    │   │   │       ├── ISecurityIncidentRepository.cs
    │   │   │       ├── IStudentRepository.cs
    │   │   │       ├── IUnitOfWork.cs
    │   │   │       ├── IVehiclePassRepository.cs
    │   │   │       ├── IVisitorRepository.cs
    │   │   │       ├── IWorkOrderRepository.cs
    │   │   │       └── IWorkflowRepository.cs
    │   │   ├── Compliance/
    │   │   │   ├── ComplianceDtos.cs
    │   │   │   └── IComplianceService.cs
    │   │   ├── ConfigCenter/
    │   │   │   ├── ConfigCenterDtos.cs
    │   │   │   └── IConfigurationCenterService.cs
    │   │   ├── Curriculum/
    │   │   │   └── CurriculumContracts.cs
    │   │   ├── DTOs/
    │   │   │   └── Requests/
    │   │   │       ├── EnrollStudentRequest.cs
    │   │   │       ├── GetBatchTimetableRequest.cs
    │   │   │       ├── MarkAttendanceRequest.cs
    │   │   │       └── SubmitResultRequest.cs
    │   │   ├── DegreeAudit/
    │   │   │   ├── DegreeAuditDtos.cs
    │   │   │   └── IDegreeAuditService.cs
    │   │   ├── DigitalTwin/
    │   │   │   ├── DigitalTwinDtos.cs
    │   │   │   └── IDigitalTwinService.cs
    │   │   ├── Examination/
    │   │   │   └── ExaminationContracts.cs
    │   │   ├── Finance/
    │   │   │   └── FinanceContracts.cs
    │   │   ├── Forms/
    │   │   │   ├── FormsDtos.cs
    │   │   │   └── IFormService.cs
    │   │   ├── Foundation/
    │   │   │   └── FoundationContracts.cs
    │   │   ├── Governance/
    │   │   │   └── GovernanceContracts.cs
    │   │   ├── Grading/
    │   │   │   ├── GradingSchemeResolver.cs
    │   │   │   └── ResultService.cs
    │   │   ├── Hr/
    │   │   │   └── HrContracts.cs
    │   │   ├── I18n/
    │   │   │   ├── I18nDtos.cs
    │   │   │   └── II18nService.cs
    │   │   ├── Identity/
    │   │   │   ├── IIdentityService.cs
    │   │   │   ├── ISamlService.cs
    │   │   │   └── IdentityDtos.cs
    │   │   ├── License/
    │   │   │   └── LicenseContracts.cs
    │   │   ├── Maintenance/
    │   │   │   ├── PreventiveMaintenanceService.cs
    │   │   │   ├── SLAEngineService.cs
    │   │   │   ├── TicketService.cs
    │   │   │   └── WorkOrderService.cs
    │   │   ├── Marketplace/
    │   │   │   ├── IExtensionService.cs
    │   │   │   ├── MarketplaceDtos.cs
    │   │   │   └── MarketplaceRevenue.cs
    │   │   ├── MicrosoftEcosystem/
    │   │   │   └── MicrosoftEcosystemContracts.cs
    │   │   ├── Obe/
    │   │   │   └── ObeContracts.cs
    │   │   ├── Onboarding/
    │   │   │   └── WizardStateService.cs
    │   │   ├── Platform/
    │   │   │   ├── ExtendedPlatformServices.cs
    │   │   │   └── PlatformServices.cs
    │   │   ├── ResearchLabs/
    │   │   │   ├── CalibrationService.cs
    │   │   │   ├── ConsumableManagementService.cs
    │   │   │   ├── EquipmentManagementService.cs
    │   │   │   ├── LabBookingService.cs
    │   │   │   └── SafetyInspectionService.cs
    │   │   ├── RuleEngine/
    │   │   │   ├── IRuleEngineService.cs
    │   │   │   └── RuleEngineDtos.cs
    │   │   ├── Search/
    │   │   │   ├── ISearchIndex.cs
    │   │   │   ├── ISearchService.cs
    │   │   │   └── SearchDtos.cs
    │   │   ├── Security/
    │   │   │   ├── GuardManagementService.cs
    │   │   │   ├── IncidentReportingService.cs
    │   │   │   ├── LostFoundService.cs
    │   │   │   ├── VehiclePassService.cs
    │   │   │   └── VisitorManagementService.cs
    │   │   ├── SsoHitl/
    │   │   │   └── SsoHitlContracts.cs
    │   │   ├── StudentLifecycle/
    │   │   │   └── StudentLifecycleContracts.cs
    │   │   ├── Workflow/
    │   │   │   ├── IWorkflowServices.cs
    │   │   │   ├── WorkflowCanvasDto.cs
    │   │   │   └── WorkflowDtos.cs
    │   │   ├── AegisCore.Application.csproj
    │   │   ├── AssemblyReference.cs
    │   │   ├── GlobalUsings.cs
    │   │   └── ServiceCollectionExtensions.cs
    │   ├── AegisCore.Contracts/
    │   │   ├── Abstractions/
    │   │   │   └── Cloud/
    │   │   │       └── CloudAbstractions.cs
    │   │   ├── Common/
    │   │   │   └── Interfaces/
    │   │   │       ├── IApiKeyRepository.cs
    │   │   │       ├── IAttendanceRepository.cs
    │   │   │       ├── IBackupRepository.cs
    │   │   │       ├── ICalibrationRecordRepository.cs
    │   │   │       ├── ICampusRepository.cs
    │   │   │       ├── IComplianceRepository.cs
    │   │   │       ├── IConfigCenterRepository.cs
    │   │   │       ├── ICourseRepository.cs
    │   │   │       ├── IDegreeAuditRepository.cs
    │   │   │       ├── IDigitalTwinRepository.cs
    │   │   │       ├── IEnrollmentRepository.cs
    │   │   │       ├── IEquipmentBookingRepository.cs
    │   │   │       ├── IEquipmentMaintenanceRepository.cs
    │   │   │       ├── IExamSeatingRepository.cs
    │   │   │       ├── IExtendedPlatformRepository.cs
    │   │   │       ├── IExtensionRepository.cs
    │   │   │       ├── IFormRepository.cs
    │   │   │       ├── IGateEntryRepository.cs
    │   │   │       ├── IGuardRoundRepository.cs
    │   │   │       ├── IGuardShiftRepository.cs
    │   │   │       ├── II18nRepository.cs
    │   │   │       ├── IIdentityRepository.cs
    │   │   │       ├── ILabBookingRepository.cs
    │   │   │       ├── ILabConsumableRepository.cs
    │   │   │       ├── ILabEquipmentRepository.cs
    │   │   │       ├── ILabSafetyInspectionRepository.cs
    │   │   │       ├── ILoginAuditRepository.cs
    │   │   │       ├── ILostFoundItemRepository.cs
    │   │   │       ├── IMaintenanceContractorRepository.cs
    │   │   │       ├── IMaintenanceTicketRepository.cs
    │   │   │       ├── IPlatformRepository.cs
    │   │   │       ├── IPreventiveMaintenanceScheduleRepository.cs
    │   │   │       ├── IRepository.cs
    │   │   │       ├── IResearchLabRepository.cs
    │   │   │       ├── IRuleRepository.cs
    │   │   │       ├── ISLADefinitionRepository.cs
    │   │   │       ├── ISecurityGuardRepository.cs
    │   │   │       ├── ISecurityIncidentRepository.cs
    │   │   │       ├── IStudentRepository.cs
    │   │   │       ├── IUnitOfWork.cs
    │   │   │       ├── IVehiclePassRepository.cs
    │   │   │       ├── IVisitorRepository.cs
    │   │   │       ├── IWorkOrderRepository.cs
    │   │   │       └── IWorkflowRepository.cs
    │   │   └── AegisCore.Contracts.csproj
    │   ├── AegisCore.Domain/
    │   │   ├── Common/
    │   │   │   └── ITenantScoped.cs
    │   │   ├── Entities/
    │   │   │   ├── AdmissionsEntities.cs
    │   │   │   ├── ApiKey.cs
    │   │   │   ├── ApprovalRecord.cs
    │   │   │   ├── Attendance.cs
    │   │   │   ├── AuditFinding.cs
    │   │   │   ├── AuditLog.cs
    │   │   │   ├── BackupConfiguration.cs
    │   │   │   ├── BackupRecord.cs
    │   │   │   ├── Batch.cs
    │   │   │   ├── Building.cs
    │   │   │   ├── CalibrationRecord.cs
    │   │   │   ├── Campus.cs
    │   │   │   ├── CampusConfiguration.cs
    │   │   │   ├── CheckpointScan.cs
    │   │   │   ├── CisUpload.cs
    │   │   │   ├── CloPlomapping.cs
    │   │   │   ├── CommunicationEntities.cs
    │   │   │   ├── ComplianceControl.cs
    │   │   │   ├── ComplianceFramework.cs
    │   │   │   ├── ConfigAuditLog.cs
    │   │   │   ├── ConfigVersion.cs
    │   │   │   ├── ConfigurationEntry.cs
    │   │   │   ├── ConsumableRequisition.cs
    │   │   │   ├── ControlEvidence.cs
    │   │   │   ├── CoreErpEntities1.cs
    │   │   │   ├── CoreErpEntities2.cs
    │   │   │   ├── CoreErpEntities3.cs
    │   │   │   ├── CoreErpEntities4.cs
    │   │   │   ├── CoreErpFinanceHrSis.cs
    │   │   │   ├── CorrectiveActionPlan.cs
    │   │   │   ├── CountryPack.cs
    │   │   │   ├── Course.cs
    │   │   │   ├── CourseAssignment.cs
    │   │   │   ├── CreditTransfer.cs
    │   │   │   ├── Currency.cs
    │   │   │   ├── CurriculumManagementEntities.cs
    │   │   │   ├── DecisionTable.cs
    │   │   │   ├── DegreeAudit.cs
    │   │   │   ├── DegreeProgram.cs
    │   │   │   ├── DegreeRequirement.cs
    │   │   │   ├── Department.cs
    │   │   │   ├── EmergencyAlert.cs
    │   │   │   ├── EmergencyContact.cs
    │   │   │   ├── EnergyReading.cs
    │   │   │   ├── Enrollment.cs
    │   │   │   ├── EquipmentBooking.cs
    │   │   │   ├── EquipmentMaintenance.cs
    │   │   │   ├── EquipmentUsageLog.cs
    │   │   │   ├── ExamSchedule.cs
    │   │   │   ├── ExamSeating.cs
    │   │   │   ├── ExaminationEntities.cs
    │   │   │   ├── Extension.cs
    │   │   │   ├── ExtensionInstallation.cs
    │   │   │   ├── ExtensionReview.cs
    │   │   │   ├── Faculty.cs
    │   │   │   ├── FeatureFlag.cs
    │   │   │   ├── FinanceEntities.cs
    │   │   │   ├── FireDrill.cs
    │   │   │   ├── FireEquipment.cs
    │   │   │   ├── Floor.cs
    │   │   │   ├── FloorPlan.cs
    │   │   │   ├── FormDefinition.cs
    │   │   │   ├── FormSubmission.cs
    │   │   │   ├── FoundationEntities.cs
    │   │   │   ├── GateEntry.cs
    │   │   │   ├── GovernanceEntities.cs
    │   │   │   ├── GuardRound.cs
    │   │   │   ├── GuardShift.cs
    │   │   │   ├── IncidentEvidence.cs
    │   │   │   ├── IncidentWitness.cs
    │   │   │   ├── IoTSensor.cs
    │   │   │   ├── LabBooking.cs
    │   │   │   ├── LabConsumable.cs
    │   │   │   ├── LabEquipment.cs
    │   │   │   ├── LabSafetyInspection.cs
    │   │   │   ├── LicenseState.cs
    │   │   │   ├── LoginAuditRecord.cs
    │   │   │   ├── LostFoundItem.cs
    │   │   │   ├── MaintenanceContractor.cs
    │   │   │   ├── MaintenanceTicket.cs
    │   │   │   ├── MicrosoftEcosystemEntities.cs
    │   │   │   ├── NotificationEntities.cs
    │   │   │   ├── NotificationRecord.cs
    │   │   │   ├── PayrollEntities.cs
    │   │   │   ├── Permission.cs
    │   │   │   ├── PlatformEntities.cs
    │   │   │   ├── PlatformEntitiesP100_110.cs
    │   │   │   ├── PlatformEntitiesP87_92.cs
    │   │   │   ├── PlatformEntitiesP93_98.cs
    │   │   │   ├── PluginRevenueShare.cs
    │   │   │   ├── PreventiveMaintenanceSchedule.cs
    │   │   │   ├── ReadReplicaConfig.cs
    │   │   │   ├── ResearchLab.cs
    │   │   │   ├── Result.cs
    │   │   │   ├── Role.cs
    │   │   │   ├── RolePermission.cs
    │   │   │   ├── Room.cs
    │   │   │   ├── RoomOccupancy.cs
    │   │   │   ├── RuleDefinition.cs
    │   │   │   ├── RuleVersion.cs
    │   │   │   ├── SLADefinition.cs
    │   │   │   ├── SafetyChecklistItem.cs
    │   │   │   ├── SamlConfiguration.cs
    │   │   │   ├── SchedulerEntities.cs
    │   │   │   ├── SecurityGuard.cs
    │   │   │   ├── SecurityIncident.cs
    │   │   │   ├── SensorReading.cs
    │   │   │   ├── SsoHitlEntities.cs
    │   │   │   ├── Student.cs
    │   │   │   ├── StudentLifecycleEntities.cs
    │   │   │   ├── StudentTransfer.cs
    │   │   │   ├── TaxSlab.cs
    │   │   │   ├── TicketComment.cs
    │   │   │   ├── TicketEscalation.cs
    │   │   │   ├── Timetable.cs
    │   │   │   ├── TranslationKey.cs
    │   │   │   ├── TranslationValue.cs
    │   │   │   ├── User.cs
    │   │   │   ├── UserRole.cs
    │   │   │   ├── VehiclePass.cs
    │   │   │   ├── Visitor.cs
    │   │   │   ├── WebhookSubscription.cs
    │   │   │   ├── WorkOrder.cs
    │   │   │   ├── WorkOrderMaterial.cs
    │   │   │   ├── WorkOrderTimeEntry.cs
    │   │   │   ├── WorkflowDefinition.cs
    │   │   │   ├── WorkflowInstance.cs
    │   │   │   └── WorkflowStep.cs
    │   │   ├── AegisCore.Domain.csproj
    │   │   ├── Enums.cs
    │   │   └── GlobalUsings.cs
    │   ├── AegisCore.Infrastructure/
    │   │   ├── AI/
    │   │   │   ├── AiPlatformServiceExtensions.cs
    │   │   │   ├── DocumentAiAgent.cs
    │   │   │   ├── DocumentOcrService.cs
    │   │   │   ├── InMemoryVectorStore.cs
    │   │   │   ├── LlmProviderFactory.cs
    │   │   │   ├── OllamaEmbeddingProvider.cs
    │   │   │   ├── OllamaLlmProvider.cs
    │   │   │   ├── OpenAiEmbeddingProvider.cs
    │   │   │   ├── OpenAiLlmProvider.cs
    │   │   │   ├── StudentAiAgent.cs
    │   │   │   ├── SupportBotQueryService.cs
    │   │   │   └── SystemPromptTemplate.cs
    │   │   ├── Admissions/
    │   │   │   └── AdmissionsServices.cs
    │   │   ├── ApiGateway/
    │   │   │   ├── ApiGatewayServiceExtensions.cs
    │   │   │   └── ApiKeyService.cs
    │   │   ├── AssetInventory/
    │   │   │   ├── AssetInventoryService.cs
    │   │   │   └── AssetInventoryServiceExtensions.cs
    │   │   ├── Backup/
    │   │   │   ├── AutomatedBackupJob.cs
    │   │   │   ├── BackupEncryptionService.cs
    │   │   │   ├── BackupService.cs
    │   │   │   ├── DRServiceExtensions.cs
    │   │   │   └── DatabaseHealthService.cs
    │   │   ├── Campus/
    │   │   │   ├── CampusService.cs
    │   │   │   ├── CampusServiceExtensions.cs
    │   │   │   └── ConsolidatedReportingService.cs
    │   │   ├── CampusOperations/
    │   │   │   ├── CampusOperationsService.cs
    │   │   │   └── CampusOperationsServiceExtensions.cs
    │   │   ├── Cloud/
    │   │   │   ├── Aws/
    │   │   │   │   └── AwsProvider.cs
    │   │   │   ├── Azure/
    │   │   │   │   └── AzureProvider.cs
    │   │   │   ├── Gcp/
    │   │   │   │   └── GcpProvider.cs
    │   │   │   ├── OnPremise/
    │   │   │   │   └── OnPremiseProvider.cs
    │   │   │   ├── R2/
    │   │   │   │   └── R2StorageService.cs
    │   │   │   ├── CloudProviderExtensions.cs
    │   │   │   └── VaultConfigurationExtensions.cs
    │   │   ├── Compliance/
    │   │   │   ├── ComplianceService.cs
    │   │   │   └── ComplianceServiceExtensions.cs
    │   │   ├── Config/
    │   │   │   ├── DefaultTenantConfigTemplate.cs
    │   │   │   ├── TenantConfigService.cs
    │   │   │   └── TenantConfigServiceExtensions.cs
    │   │   ├── ConfigCenter/
    │   │   │   ├── ConfigCenterServiceExtensions.cs
    │   │   │   └── ConfigurationCenterService.cs
    │   │   ├── Configuration/
    │   │   │   └── EnvironmentSubstitutionExtensions.cs
    │   │   ├── CoreERP/
    │   │   │   └── CoreErpServiceExtensions.cs
    │   │   ├── Curriculum/
    │   │   │   ├── CurriculumService.cs
    │   │   │   └── CurriculumServiceExtensions.cs
    │   │   ├── DegreeAudit/
    │   │   │   ├── DegreeAuditService.cs
    │   │   │   └── DegreeAuditServiceExtensions.cs
    │   │   ├── Deployment/
    │   │   │   ├── DeploymentReadinessChecker.cs
    │   │   │   ├── ExtendedHealthChecks.cs
    │   │   │   ├── SeedPipeline.cs
    │   │   │   └── SystemAlertService.cs
    │   │   ├── DigitalTwin/
    │   │   │   ├── DigitalTwinService.cs
    │   │   │   └── DigitalTwinServiceExtensions.cs
    │   │   ├── Examination/
    │   │   │   └── GradingService.cs
    │   │   ├── Finance/
    │   │   │   └── FinanceServices.cs
    │   │   ├── Forms/
    │   │   │   ├── FormService.cs
    │   │   │   └── FormsServiceExtensions.cs
    │   │   ├── Foundation/
    │   │   │   ├── FoundationServices.cs
    │   │   │   └── StorageProviders.cs
    │   │   ├── Governance/
    │   │   │   ├── GovernanceService.cs
    │   │   │   └── GovernanceServiceExtensions.cs
    │   │   ├── Hr/
    │   │   │   └── PayrollService.cs
    │   │   ├── I18n/
    │   │   │   ├── CountryPackService.cs
    │   │   │   ├── I18nServiceExtensions.cs
    │   │   │   └── TranslationService.cs
    │   │   ├── Identity/
    │   │   │   ├── IdentityService.cs
    │   │   │   ├── IdentityServiceExtensions.cs
    │   │   │   └── SamlService.cs
    │   │   ├── Installation/
    │   │   │   └── InstallationStateService.cs
    │   │   ├── License/
    │   │   │   └── LicenseService.cs
    │   │   ├── Marketplace/
    │   │   │   ├── ExtensionService.cs
    │   │   │   └── MarketplaceServiceExtensions.cs
    │   │   ├── MicrosoftEcosystem/
    │   │   │   ├── MicrosoftEcosystemServiceExtensions.cs
    │   │   │   └── MicrosoftEcosystemServices.cs
    │   │   ├── Migrations/
    │   │   │   ├── 20260726160353_V1_0_AegisCoreBaseline.Designer.cs
    │   │   │   ├── 20260726160353_V1_0_AegisCoreBaseline.cs
    │   │   │   ├── 20260726195806_V1_1_ExaminationSemesterGrades.Designer.cs
    │   │   │   ├── 20260726195806_V1_1_ExaminationSemesterGrades.cs
    │   │   │   ├── AegisDbContextModelSnapshot.cs
    │   │   │   └── V1.0_AegisCoreBaseline.sql
    │   │   ├── Obe/
    │   │   │   └── ObeServices.cs
    │   │   ├── Onboarding/
    │   │   │   ├── OnboardingService.cs
    │   │   │   └── OnboardingServiceExtensions.cs
    │   │   ├── Payments/
    │   │   │   └── Raast/
    │   │   │       └── RaastApiClient.cs
    │   │   ├── Platform/
    │   │   │   ├── ExtendedPlatformServiceExtensions.cs
    │   │   │   ├── ExtendedPlatformServices.cs
    │   │   │   ├── PlatformServiceExtensions.cs
    │   │   │   └── PlatformServices.cs
    │   │   ├── Repositories/
    │   │   │   ├── ApiKeyRepository.cs
    │   │   │   ├── AttendanceRepository.cs
    │   │   │   ├── BackupRepository.cs
    │   │   │   ├── CalibrationRecordRepository.cs
    │   │   │   ├── CampusRepository.cs
    │   │   │   ├── ComplianceRepository.cs
    │   │   │   ├── ConfigCenterRepository.cs
    │   │   │   ├── CourseRepository.cs
    │   │   │   ├── DegreeAuditRepository.cs
    │   │   │   ├── DigitalTwinRepository.cs
    │   │   │   ├── EnrollmentRepository.cs
    │   │   │   ├── EquipmentBookingRepository.cs
    │   │   │   ├── EquipmentMaintenanceRepository.cs
    │   │   │   ├── ExamSeatingRepository.cs
    │   │   │   ├── ExtendedPlatformRepository.cs
    │   │   │   ├── ExtensionRepository.cs
    │   │   │   ├── FormRepository.cs
    │   │   │   ├── GateEntryRepository.cs
    │   │   │   ├── GuardRoundRepository.cs
    │   │   │   ├── GuardShiftRepository.cs
    │   │   │   ├── I18nRepository.cs
    │   │   │   ├── IdentityRepository.cs
    │   │   │   ├── LabBookingRepository.cs
    │   │   │   ├── LabConsumableRepository.cs
    │   │   │   ├── LabEquipmentRepository.cs
    │   │   │   ├── LabSafetyInspectionRepository.cs
    │   │   │   ├── LoginAuditRepository.cs
    │   │   │   ├── LostFoundItemRepository.cs
    │   │   │   ├── MaintenanceContractorRepository.cs
    │   │   │   ├── MaintenanceTicketRepository.cs
    │   │   │   ├── PlatformRepository.cs
    │   │   │   ├── PreventiveMaintenanceScheduleRepository.cs
    │   │   │   ├── Repository.cs
    │   │   │   ├── ResearchLabRepository.cs
    │   │   │   ├── RuleRepository.cs
    │   │   │   ├── SLADefinitionRepository.cs
    │   │   │   ├── SecurityGuardRepository.cs
    │   │   │   ├── SecurityIncidentRepository.cs
    │   │   │   ├── StudentRepository.cs
    │   │   │   ├── VehiclePassRepository.cs
    │   │   │   ├── VisitorRepository.cs
    │   │   │   ├── WorkOrderRepository.cs
    │   │   │   └── WorkflowRepositories.cs
    │   │   ├── RuleEngine/
    │   │   │   ├── RuleEngineService.cs
    │   │   │   └── RuleEngineServiceExtensions.cs
    │   │   ├── Search/
    │   │   │   ├── ElasticsearchIndex.cs
    │   │   │   ├── SearchService.cs
    │   │   │   └── SearchServiceExtensions.cs
    │   │   ├── Security/
    │   │   │   ├── Abac/
    │   │   │   │   └── AbacPolicyService.cs
    │   │   │   ├── Pqc/
    │   │   │   │   └── PqcCryptographyService.cs
    │   │   │   ├── AuditLogService.cs
    │   │   │   ├── BruteForceProtectionService.cs
    │   │   │   ├── JwtTokenService.cs
    │   │   │   ├── LoginAuditService.cs
    │   │   │   ├── LoginAuditServiceExtensions.cs
    │   │   │   └── PasswordHashingService.cs
    │   │   ├── SsoHitl/
    │   │   │   ├── SsoHitlServiceExtensions.cs
    │   │   │   └── SsoHitlServices.cs
    │   │   ├── StudentLifecycle/
    │   │   │   ├── StudentLifecycleService.cs
    │   │   │   └── StudentLifecycleServiceExtensions.cs
    │   │   ├── Workflow/
    │   │   │   ├── StateMachine/
    │   │   │   │   └── StateMachineService.cs
    │   │   │   ├── ApprovalService.cs
    │   │   │   ├── EmailNotificationService.cs
    │   │   │   ├── WorkflowEngine.cs
    │   │   │   ├── WorkflowServiceExtensions.cs
    │   │   │   └── WorkflowTemplateSeeder.cs
    │   │   ├── AegisCore.Infrastructure.csproj
    │   │   ├── AegisDbContext.cs
    │   │   ├── AegisDbContextFactory.cs
    │   │   ├── DbContextServiceExtensions.cs
    │   │   ├── GlobalUsings.cs
    │   │   ├── InfrastructureServiceExtensions.cs
    │   │   └── UnitOfWork.cs
    │   ├── AegisCore.PluginCli/
    │   │   ├── AegisCore.PluginCli.csproj
    │   │   ├── Program.cs
    │   │   └── README.md
    │   ├── AegisCore.PluginSdk/
    │   │   ├── AegisCore.PluginSdk.csproj
    │   │   ├── PluginContracts.cs
    │   │   └── README.md
    │   ├── AegisCore.Tests/
    │   │   ├── AdmissionsTests.cs
    │   │   ├── AegisCore.Tests.csproj
    │   │   ├── AssetInventoryTests.cs
    │   │   ├── CampusOperationsTests.cs
    │   │   ├── CommunicationsTests.cs
    │   │   ├── CurriculumTests.cs
    │   │   ├── FinanceTests.cs
    │   │   ├── FoundationStorageTests.cs
    │   │   ├── GlobalUsings.cs
    │   │   ├── GovernanceTests.cs
    │   │   ├── IdentityPatchTests.cs
    │   │   ├── LicenseServiceTests.cs
    │   │   ├── LoginAuditControllerTests.cs
    │   │   ├── MarketplaceRevenueTests.cs
    │   │   ├── MicrosoftEcosystemTests.cs
    │   │   ├── PasswordHashingServiceTests.cs
    │   │   ├── PayrollServiceTests.cs
    │   │   ├── PromptFiveEdgeCaseTests.cs
    │   │   ├── S1FoundationTests.cs
    │   │   ├── SetupControllerRouteTests.cs
    │   │   ├── SsoHitlTests.cs
    │   │   └── StudentLifecycleTests.cs
    │   ├── k8s/
    │   │   ├── api-deployment.yaml
    │   │   ├── api-hpa.yaml
    │   │   ├── api-pdb.yaml
    │   │   ├── api-service.yaml
    │   │   ├── configmap.yaml
    │   │   ├── ingress.yaml
    │   │   ├── namespace.yaml
    │   │   ├── network-policy.yaml
    │   │   ├── postgres-service.yaml
    │   │   ├── postgres-statefulset.yaml
    │   │   ├── redis-deployment.yaml
    │   │   └── redis-service.yaml
    │   ├── scripts/
    │   │   └── start-and-open.sh
    │   ├── .dockerignore
    │   ├── .env.example
    │   ├── AegisCore.slnx
    │   ├── Directory.Build.props
    │   ├── Dockerfile
    │   ├── README.md
    │   ├── docker-compose.yml
    │   └── dotnet-tools.json
    ├── aegiscore-frontend/
    │   ├── .storybook/
    │   │   └── main.ts
    │   ├── cypress/
    │   │   ├── e2e/
    │   │   │   └── portals.cy.ts
    │   │   └── tsconfig.json
    │   ├── public/
    │   │   └── sw.js
    │   ├── src/
    │   │   ├── App.stories.tsx
    │   │   ├── App.tsx
    │   │   ├── CurriculumBuilder.tsx
    │   │   ├── api.ts
    │   │   ├── main.tsx
    │   │   ├── offlineAttendance.ts
    │   │   ├── store.ts
    │   │   ├── styles.css
    │   │   └── vite-env.d.ts
    │   ├── .env.example
    │   ├── ACCESSIBILITY.md
    │   ├── Dockerfile
    │   ├── README.md
    │   ├── cypress.config.ts
    │   ├── index.html
    │   ├── package-lock.json
    │   ├── package.json
    │   ├── tsconfig.json
    │   ├── vite.config.ts
    │   └── wrangler.toml
    ├── aegiscore-mobile/
    │   ├── android/
    │   │   ├── app/
    │   │   │   ├── src/
    │   │   │   │   └── main/
    │   │   │   │       ├── kotlin/
    │   │   │   │       │   └── pk/
    │   │   │   │       │       └── edu/
    │   │   │   │       │           └── aegiscore/
    │   │   │   │       │               └── mobile/
    │   │   │   │       │                   ├── Api.kt
    │   │   │   │       │                   ├── MainActivity.kt
    │   │   │   │       │                   └── OfflineAttendance.kt
    │   │   │   │       ├── res/
    │   │   │   │       │   └── values/
    │   │   │   │       │       └── styles.xml
    │   │   │   │       └── AndroidManifest.xml
    │   │   │   └── build.gradle.kts
    │   │   ├── build.gradle.kts
    │   │   └── settings.gradle.kts
    │   ├── ios/
    │   │   ├── AegisCore/
    │   │   │   ├── AegisCoreApp.swift
    │   │   │   ├── ApiClient.swift
    │   │   │   └── BiometricGate.swift
    │   │   └── Package.swift
    │   ├── react-native/
    │   │   ├── apps/
    │   │   │   ├── faculty/
    │   │   │   │   └── App.tsx
    │   │   │   └── student/
    │   │   │       └── App.tsx
    │   │   ├── e2e/
    │   │   │   └── aegiscore.e2e.ts
    │   │   ├── src/
    │   │   │   ├── api/
    │   │   │   │   └── client.ts
    │   │   │   ├── auth/
    │   │   │   │   ├── biometrics.ts
    │   │   │   │   └── pushNotifications.ts
    │   │   │   ├── db/
    │   │   │   │   └── schema.ts
    │   │   │   ├── i18n/
    │   │   │   │   └── strings.ts
    │   │   │   ├── screens/
    │   │   │   │   ├── Screen.tsx
    │   │   │   │   ├── screenData.test.ts
    │   │   │   │   └── screenData.ts
    │   │   │   ├── state/
    │   │   │   │   ├── sessionSlice.ts
    │   │   │   │   └── store.ts
    │   │   │   ├── sync/
    │   │   │   │   └── offlineQueue.ts
    │   │   │   └── ui/
    │   │   │       ├── RoleSwitcher.tsx
    │   │   │       └── styles.ts
    │   │   ├── .gitignore
    │   │   ├── App.tsx
    │   │   ├── README.md
    │   │   ├── app.json
    │   │   ├── babel.config.js
    │   │   ├── eas.json
    │   │   ├── jest.config.js
    │   │   ├── package-lock.json
    │   │   ├── package.json
    │   │   └── tsconfig.json
    │   ├── .env.example
    │   └── README.md
    ├── aegiscore-mobile-app/
    │   ├── .github/
    │   │   └── workflows/
    │   │       ├── android-build.yml
    │   │       ├── android-deploy.yml
    │   │       └── ios-deploy.yml
    │   ├── android/
    │   │   ├── app/
    │   │   │   ├── src/
    │   │   │   │   └── main/
    │   │   │   │       ├── kotlin/
    │   │   │   │       │   └── pk/
    │   │   │   │       │       └── edu/
    │   │   │   │       │           └── aegiscore/
    │   │   │   │       │               └── mobile/
    │   │   │   │       │                   ├── Api.kt
    │   │   │   │       │                   ├── MainActivity.kt
    │   │   │   │       │                   └── OfflineAttendance.kt
    │   │   │   │       ├── res/
    │   │   │   │       │   └── values/
    │   │   │   │       │       └── styles.xml
    │   │   │   │       └── AndroidManifest.xml
    │   │   │   ├── build.gradle.kts
    │   │   │   └── proguard-rules.pro
    │   │   ├── CONTRIBUTING.md
    │   │   ├── README.md
    │   │   ├── build.gradle.kts
    │   │   └── settings.gradle.kts
    │   ├── docs/
    │   │   ├── docker-mobile-architecture.md
    │   │   ├── mobile-manual.html
    │   │   └── specification.md
    │   ├── ios/
    │   │   ├── AegisCore/
    │   │   │   ├── AegisCoreApp.swift
    │   │   │   ├── ApiClient.swift
    │   │   │   ├── BiometricGate.swift
    │   │   │   └── PrivacyInfo.xcprivacy
    │   │   ├── Package.swift
    │   │   └── exportOptions.plist
    │   ├── react-native/
    │   │   ├── apps/
    │   │   │   ├── faculty/
    │   │   │   │   └── App.tsx
    │   │   │   └── student/
    │   │   │       └── App.tsx
    │   │   ├── assets/
    │   │   │   └── icon.png
    │   │   ├── e2e/
    │   │   │   └── aegiscore.e2e.ts
    │   │   ├── src/
    │   │   │   ├── api/
    │   │   │   │   └── client.ts
    │   │   │   ├── auth/
    │   │   │   │   ├── biometrics.ts
    │   │   │   │   └── pushNotifications.ts
    │   │   │   ├── db/
    │   │   │   │   └── schema.ts
    │   │   │   ├── i18n/
    │   │   │   │   └── strings.ts
    │   │   │   ├── screens/
    │   │   │   │   ├── Screen.tsx
    │   │   │   │   ├── screenData.test.ts
    │   │   │   │   └── screenData.ts
    │   │   │   ├── state/
    │   │   │   │   ├── sessionSlice.ts
    │   │   │   │   └── store.ts
    │   │   │   ├── sync/
    │   │   │   │   └── offlineQueue.ts
    │   │   │   └── ui/
    │   │   │       ├── RoleSwitcher.tsx
    │   │   │       └── styles.ts
    │   │   ├── App.tsx
    │   │   ├── README.md
    │   │   ├── app.json
    │   │   ├── babel.config.js
    │   │   ├── eas.json
    │   │   ├── jest.config.js
    │   │   ├── package-lock.json
    │   │   ├── package.json
    │   │   └── tsconfig.json
    │   └── README.md
    ├── apps/
    │   └── README.md
    ├── building-blocks/
    │   └── README.md
    ├── deploy/
    │   ├── config/
    │   │   ├── Caddyfile
    │   │   ├── alerts.yml
    │   │   ├── pgbouncer.ini
    │   │   └── prometheus.yml
    │   ├── github/
    │   │   └── workflows/
    │   │       └── ci-cd.yml
    │   ├── runbooks/
    │   │   ├── app-crash.md
    │   │   ├── db-failure.md
    │   │   ├── ddos-attack.md
    │   │   └── full-recovery.md
    │   ├── scripts/
    │   │   ├── backup.sh
    │   │   ├── deploy.sh
    │   │   ├── rollback.sh
    │   │   └── warm-cache.sh
    │   ├── .dockerignore
    │   ├── Dockerfile
    │   └── docker-compose.prod.yml
    ├── docker/
    │   └── postgres/
    │       ├── init/
    │       │   └── 01-init.sql
    │       └── wal-archive.sh
    ├── docs/
    │   ├── architecture/
    │   │   └── 0001-modular-monolith.md
    │   ├── deployment/
    │   │   ├── README.md
    │   │   ├── aws.md
    │   │   ├── azure.md
    │   │   ├── gcp.md
    │   │   └── vps.md
    │   ├── 01-vision-document.md
    │   ├── 02-software-requirements-specification.md
    │   ├── 03-software-architecture-document.md
    │   ├── 04-database-design-document.md
    │   ├── 05-api-specification.yaml
    │   ├── 06-security-architecture-document.md
    │   ├── 07-deployment-guide.md
    │   ├── 08-administrator-guide.md
    │   ├── 09-user-manual.md
    │   ├── 10-test-plan.md
    │   ├── 11-test-report.md
    │   ├── 12-performance-report.md
    │   ├── 13-quality-evaluation-report.md
    │   ├── 14-risk-assessment.md
    │   ├── 15-operations-maintenance-guide.md
    │   ├── 16-release-notes-changelog.md
    │   ├── 17-cloud-agnosticism-report.md
    │   ├── 18-university-pitch-cloud-concerns.md
    │   ├── 19-deployment-runbook.md
    │   ├── 20-competitive-analysis.md
    │   ├── 21-positioning-memo.md
    │   ├── 22-pitch-deck.md
    │   ├── 23-go-to-market-and-outreach.md
    │   ├── 24-plugin-ecosystem.md
    │   ├── 25-build-completion-evidence.md
    │   ├── 26-repository-split-report.md
    │   ├── 27-git-cleanup-audit.md
    │   ├── 28-android-cleanup-report.md
    │   ├── 29-test-and-edge-case-report.md
    │   ├── 30-prompts-1-to-7-implementation-report.md
    │   ├── DEPENDENCY_AUDIT.md
    │   ├── ROLE_CAPABILITIES.md
    │   ├── accessibility.html
    │   ├── dcr-runbook.md
    │   ├── deploy-playbook.html
    │   ├── manual.html
    │   └── mobile-manual.html
    ├── formats/
    │   ├── README.txt
    │   ├── alumni.csv
    │   ├── attendance.csv
    │   ├── courses.csv
    │   ├── exam_schedule.csv
    │   ├── faculty.csv
    │   ├── fee_payments.csv
    │   ├── fee_structure.csv
    │   ├── grades.csv
    │   ├── library_books.csv
    │   ├── placement_records.csv
    │   ├── student_enrollments.csv
    │   ├── students.csv
    │   ├── timetable.csv
    │   ├── transcripts.csv
    │   └── visitors.csv
    ├── k8s/
    │   ├── api-deployment.yaml
    │   ├── api-hpa.yaml
    │   ├── api-pdb.yaml
    │   ├── api-service.yaml
    │   ├── configmap.yaml
    │   ├── data-protection-pvc.yaml
    │   ├── ingress.yaml
    │   ├── namespace.yaml
    │   ├── network-policy.yaml
    │   ├── portal-deployment.yaml
    │   ├── portal-service.yaml
    │   ├── postgres-init-configmap.yaml
    │   ├── postgres-service.yaml
    │   ├── postgres-statefulset.yaml
    │   ├── redis-deployment.yaml
    │   └── redis-service.yaml
    ├── manuals/
    │   ├── 1-backend-installation.html
    │   ├── 2-frontend-setup.html
    │   ├── 3-mobile-setup.html
    │   ├── 4-docker-deployment.html
    │   ├── 5-kubernetes-deployment.html
    │   ├── 6-database-setup.html
    │   ├── 7-troubleshooting-guide.html
    │   └── 8-contributing-guide.html
    ├── md/
    │   ├── 1.md
    │   ├── 2.md
    │   ├── 3.md
    │   ├── 4.md
    │   ├── 5.md
    │   ├── 6.md
    │   ├── 7.md
    │   ├── S1.md
    │   ├── S10-campus-ops.md
    │   ├── S11-student-lifecycle.md
    │   ├── S12-asset-inventory-hec.md
    │   ├── S13-mobile-apps.md
    │   ├── S2.md
    │   ├── S3.md
    │   ├── S4.md
    │   ├── S5.md
    │   ├── S6-microsoft-ecosystem.md
    │   ├── S7-finance.md
    │   ├── S8-portals.md
    │   ├── S9-communications.md
    │   ├── U1-codebase-understanding.md
    │   ├── U10-governance-overrides-feedback.md
    │   ├── U11-course-trees-workload-offline.md
    │   ├── U12-deployment-distribution.md
    │   ├── U13-native-mobile-docker-integration.md
    │   ├── U14-multitenant-sso-hitl-exams.md
    │   ├── U2-patch.md
    │   ├── U3-cleanup.md
    │   ├── U4-docker-build-validator.md
    │   ├── U5-onboarding-wizard.md
    │   ├── U6-license-engine.md
    │   ├── U7-cloud-agnosticism.md
    │   ├── U8-competitive-analysis.md
    │   └── U9-community-plugins.md
    ├── modules/
    │   └── README.md
    ├── sample-plugins/
    │   └── fee-reconciliation/
    │       ├── README.md
    │       └── aegiscore-plugin.json
    ├── scripts/
    │   ├── postgres-init/
    │   │   └── 010-harden-app-role.sh
    │   ├── bootstrap-env.sh
    │   ├── demo-data-manifest.md
    │   ├── demo-university-seed.sql
    │   ├── deploy.sh
    │   ├── start-and-open.sh
    │   └── wipe-demo-data.sql
    ├── tests/
    │   ├── reports/
    │   │   └── latest.md
    │   ├── README.md
    │   ├── run-all.sh
    │   └── verify-pqc-interop.mjs
    ├── .dockerignore
    ├── .env.example
    ├── .gitignore
    ├── AEGISCORE.md
    ├── AEGISCORE_BACKEND_SCAN.txt
    ├── AegisCore.slnx
    ├── DOCKER_OPTIMIZATION.md
    ├── Directory.Build.props
    ├── Dockerfile
    ├── FRONTEND_CODEBASE_REPORT.txt
    ├── INDEX.md
    ├── README-DEPLOY.md
    ├── README.md
    ├── aegiscorelogo.png
    ├── build.log
    ├── docker-compose.cloud.yml
    ├── docker-compose.hybrid.yml
    ├── docker-compose.uni.yml
    ├── docker-compose.yml
    ├── dotnet-tools.json
    ├── server.py
    └── wizard.html
```
