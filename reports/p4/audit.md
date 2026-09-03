# P4 Tenant Query Bypass Audit

## Raw SQL

| File | Line | Method | SQL | Tenant-safe? |
|---|---:|---|---|---|
| `AegisCore.Infrastructure/Onboarding/SetupImportService.cs` | 29 | ExecuteSqlRawAsync | `` | review_required |

## Direct connections

| File | Line | Type | Fixed? |
|---|---:|---|---|
| `AegisCore.Infrastructure/AI/SupportBotQueryService.cs` | 106 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Backup/DatabaseHealthService.cs` | 38 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Backup/DatabaseHealthService.cs` | 58 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/DataImport/DataImportService.cs` | 158 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Deployment/DeploymentReadinessChecker.cs` | 64 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Onboarding/OnboardingService.cs` | 59 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Replication/ReplicaManagerService.cs` | 54 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Replication/ReplicaManagerService.cs` | 111 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Replication/ReplicaManagerService.cs` | 130 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Replication/ReplicaManagerService.cs` | 168 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Support/SupportTicketService.cs` | 92 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Support/SupportTicketService.cs` | 134 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Support/SupportTicketService.cs` | 163 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Support/SupportTicketService.cs` | 196 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Support/SupportTicketService.cs` | 228 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Support/SupportTicketService.cs` | 262 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Support/SupportTicketService.cs` | 321 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Support/SupportTicketService.cs` | 333 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Support/SupportTicketService.cs` | 388 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Support/SupportTicketService.cs` | 431 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Support/SupportTicketService.cs` | 499 | NpgsqlConnection | ❌ review |
| `AegisCore.Infrastructure/Support/SupportTicketService.cs` | 521 | NpgsqlConnection | ❌ review |

## IgnoreQueryFilters

| File | Line | Justification |
|---|---:|---|
| `AegisCore.Infrastructure/Campus/CafeteriaService.cs` | 13 | MISSING |
| `AegisCore.Infrastructure/Campus/CafeteriaService.cs` | 53 | MISSING |
| `AegisCore.Infrastructure/Campus/CafeteriaService.cs` | 60 | MISSING |
| `AegisCore.Infrastructure/Campus/CafeteriaService.cs` | 67 | MISSING |
| `AegisCore.Infrastructure/Campus/CafeteriaService.cs` | 73 | MISSING |
| `AegisCore.Infrastructure/Campus/CafeteriaService.cs` | 81 | MISSING |
| `AegisCore.Infrastructure/Campus/TransportService.cs` | 12 | MISSING |
| `AegisCore.Infrastructure/Campus/TransportService.cs` | 52 | MISSING |
| `AegisCore.Infrastructure/Campus/TransportService.cs` | 64 | MISSING |
| `AegisCore.Infrastructure/Campus/TransportService.cs` | 65 | MISSING |
| `AegisCore.Infrastructure/CampusOperations/ElectionService.cs` | 35 | MISSING |
| `AegisCore.Infrastructure/CampusOperations/ElectionService.cs` | 41 | MISSING |
| `AegisCore.Infrastructure/CampusOperations/ElectionService.cs` | 70 | MISSING |
| `AegisCore.Infrastructure/CampusOperations/ElectionService.cs` | 71 | MISSING |
| `AegisCore.Infrastructure/CampusOperations/ElectionService.cs` | 72 | MISSING |
| `AegisCore.Infrastructure/CampusOperations/ElectionService.cs` | 89 | MISSING |
| `AegisCore.Infrastructure/CampusOperations/ElectionService.cs` | 90 | MISSING |
| `AegisCore.Infrastructure/CampusOperations/ElectionService.cs` | 96 | MISSING |
| `AegisCore.Infrastructure/Deployment/Seeders/DemoSeeder.cs` | 52 | MISSING |
| `AegisCore.Infrastructure/Deployment/Seeders/DemoSeeder.cs` | 76 | MISSING |
| `AegisCore.Infrastructure/Finance/AccountingStandardsEngine.cs` | 15 | MISSING |
| `AegisCore.Infrastructure/Finance/AccountingStandardsEngine.cs` | 28 | MISSING |
| `AegisCore.Infrastructure/Finance/AccountingStandardsEngine.cs` | 30 | MISSING |
| `AegisCore.Infrastructure/Finance/AccountingStandardsEngine.cs` | 48 | MISSING |
| `AegisCore.Infrastructure/Finance/AccountingStandardsEngine.cs` | 65 | MISSING |
| `AegisCore.Infrastructure/Finance/AccountingStandardsEngine.cs` | 67 | MISSING |
| `AegisCore.Infrastructure/Finance/AccountingStandardsEngine.cs` | 68 | MISSING |
| `AegisCore.Infrastructure/Finance/AccountingStandardsEngine.cs` | 90 | MISSING |
| `AegisCore.Infrastructure/Finance/AccountingStandardsEngine.cs` | 111 | MISSING |
| `AegisCore.Infrastructure/Finance/AccountingStandardsEngine.cs` | 122 | MISSING |
| `AegisCore.Infrastructure/Finance/AccountingStandardsEngine.cs` | 124 | MISSING |
| `AegisCore.Infrastructure/Finance/AccountingStandardsEngine.cs` | 136 | MISSING |
| `AegisCore.Infrastructure/Finance/AccountingStandardsEngine.cs` | 138 | MISSING |
| `AegisCore.Infrastructure/Finance/AccountingStandardsEngine.cs` | 140 | MISSING |
| `AegisCore.Infrastructure/Finance/AccountingStandardsEngine.cs` | 141 | MISSING |
| `AegisCore.Infrastructure/Finance/AccountingStandardsEngine.cs` | 160 | MISSING |
| `AegisCore.Infrastructure/Finance/AccountingStandardsEngine.cs` | 171 | MISSING |
| `AegisCore.Infrastructure/Finance/AncillaryFinanceService.cs` | 19 | MISSING |
| `AegisCore.Infrastructure/Finance/AncillaryFinanceService.cs` | 44 | MISSING |
| `AegisCore.Infrastructure/Finance/AncillaryFinanceService.cs` | 61 | MISSING |
| `AegisCore.Infrastructure/Finance/AncillaryFinanceService.cs` | 72 | MISSING |
| `AegisCore.Infrastructure/Finance/AncillaryFinanceService.cs` | 83 | MISSING |
| `AegisCore.Infrastructure/Finance/AncillaryFinanceService.cs` | 102 | MISSING |
| `AegisCore.Infrastructure/Finance/AncillaryFinanceService.cs` | 103 | MISSING |
| `AegisCore.Infrastructure/Finance/AncillaryFinanceService.cs` | 104 | MISSING |
| `AegisCore.Infrastructure/Finance/AncillaryFinanceService.cs` | 124 | MISSING |
| `AegisCore.Infrastructure/Finance/AncillaryFinanceService.cs` | 134 | MISSING |
| `AegisCore.Infrastructure/Finance/AncillaryFinanceService.cs` | 135 | MISSING |
| `AegisCore.Infrastructure/Finance/AncillaryFinanceService.cs` | 140 | MISSING |
| `AegisCore.Infrastructure/Finance/AncillaryFinanceService.cs` | 166 | MISSING |
| `AegisCore.Infrastructure/Finance/AncillaryFinanceService.cs` | 169 | MISSING |
| `AegisCore.Infrastructure/Finance/AncillaryFinanceService.cs` | 177 | MISSING |
| `AegisCore.Infrastructure/Finance/AuditSealService.cs` | 38 | MISSING |
| `AegisCore.Infrastructure/Finance/AuditSealService.cs` | 43 | MISSING |
| `AegisCore.Infrastructure/Finance/AuditSealService.cs` | 89 | MISSING |
| `AegisCore.Infrastructure/Finance/AuditSealService.cs` | 115 | MISSING |
| `AegisCore.Infrastructure/Finance/AuditSealService.cs` | 119 | MISSING |
| `AegisCore.Infrastructure/Finance/AuditSealService.cs` | 163 | MISSING |
| `AegisCore.Infrastructure/Finance/AuditSealService.cs` | 164 | MISSING |
| `AegisCore.Infrastructure/Finance/DefaultPredictionService.cs` | 12 | MISSING |
| `AegisCore.Infrastructure/Finance/DefaultPredictionService.cs` | 16 | MISSING |
| `AegisCore.Infrastructure/Finance/DefaultPredictionService.cs` | 20 | MISSING |
| `AegisCore.Infrastructure/Finance/DefaultPredictionService.cs` | 35 | MISSING |
| `AegisCore.Infrastructure/Finance/DefaultPredictionService.cs` | 59 | MISSING |
| `AegisCore.Infrastructure/Finance/DefaultPredictionService.cs` | 71 | MISSING |
| `AegisCore.Infrastructure/Finance/DefaultPredictionService.cs` | 115 | MISSING |
| `AegisCore.Infrastructure/Finance/DefaultPredictionService.cs` | 146 | MISSING |
| `AegisCore.Infrastructure/Finance/DefaultPredictionService.cs` | 158 | MISSING |
| `AegisCore.Infrastructure/Finance/DefaultPredictionService.cs` | 178 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceAssetLifecycleService.cs` | 82 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceAssetLifecycleService.cs` | 97 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceAssetLifecycleService.cs` | 122 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceAssetLifecycleService.cs` | 127 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 38 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 76 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 101 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 122 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 124 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 125 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 126 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 151 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 163 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 164 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 165 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 166 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 167 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 168 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 188 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 191 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 192 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 193 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 195 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 196 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 197 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 198 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 199 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 201 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 202 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 203 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 204 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 205 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 206 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 207 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 208 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 209 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 210 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 211 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 212 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 213 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 214 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 215 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 216 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 217 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 218 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 219 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 220 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 221 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 222 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 223 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 224 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceContinuousControlService.cs` | 232 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 25 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 31 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 36 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 88 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 91 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 133 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 211 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 214 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 245 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 263 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 267 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 324 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 349 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 352 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 353 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 397 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 411 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 432 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 433 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 465 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 558 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 562 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 584 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 610 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 681 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 734 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 740 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 781 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 807 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 860 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 913 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 937 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 984 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 1006 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 1028 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 1096 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 1116 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 1138 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 1170 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 1171 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 1176 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 1332 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 1341 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 1362 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 1382 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceModuleService.cs` | 1392 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceRegulatoryReportService.cs` | 32 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceRegulatoryReportService.cs` | 45 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceRegulatoryReportService.cs` | 213 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceScholarshipService.cs` | 31 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceScholarshipService.cs` | 52 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceScholarshipService.cs` | 56 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceScholarshipService.cs` | 57 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceScholarshipService.cs` | 74 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceScholarshipService.cs` | 95 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceTreasuryService.cs` | 41 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceTreasuryService.cs` | 44 | MISSING |
| `AegisCore.Infrastructure/Finance/FinanceTreasuryService.cs` | 73 | MISSING |
| `AegisCore.Infrastructure/Finance/PaymentPlanWorker.cs` | 24 | MISSING |
| `AegisCore.Infrastructure/Finance/VendorSanctionsService.cs` | 47 | MISSING |
| `AegisCore.Infrastructure/Finance/VoucherImportService.cs` | 28 | MISSING |
| `AegisCore.Infrastructure/Governance/AccreditationService.cs` | 14 | MISSING |
| `AegisCore.Infrastructure/Governance/AccreditationService.cs` | 18 | MISSING |
| `AegisCore.Infrastructure/Governance/AccreditationService.cs` | 39 | MISSING |
| `AegisCore.Infrastructure/Governance/AccreditationService.cs` | 47 | MISSING |
| `AegisCore.Infrastructure/Governance/AccreditationService.cs` | 66 | MISSING |
| `AegisCore.Infrastructure/Governance/AccreditationService.cs` | 80 | MISSING |
| `AegisCore.Infrastructure/Governance/BoardPortalService.cs` | 22 | MISSING |
| `AegisCore.Infrastructure/Governance/BoardPortalService.cs` | 36 | MISSING |
| `AegisCore.Infrastructure/Governance/BoardPortalService.cs` | 37 | MISSING |
| `AegisCore.Infrastructure/Governance/BoardPortalService.cs` | 43 | MISSING |
| `AegisCore.Infrastructure/Governance/BoardPortalService.cs` | 50 | MISSING |
| `AegisCore.Infrastructure/Governance/BoardPortalService.cs` | 56 | MISSING |
| `AegisCore.Infrastructure/Governance/BoardPortalService.cs` | 62 | MISSING |
| `AegisCore.Infrastructure/Hr/VisitingFacultyService.cs` | 12 | MISSING |
| `AegisCore.Infrastructure/Hr/VisitingFacultyService.cs` | 13 | MISSING |
| `AegisCore.Infrastructure/Hr/VisitingFacultyService.cs` | 96 | MISSING |
| `AegisCore.Infrastructure/Hr/VisitingFacultyService.cs` | 97 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardGatewayService.cs` | 82 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardGatewayService.cs` | 106 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardGatewayService.cs` | 136 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardProvisioningService.cs` | 36 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardProvisioningService.cs` | 51 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardProvisioningService.cs` | 76 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardProvisioningService.cs` | 97 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardProvisioningService.cs` | 120 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardProvisioningService.cs` | 129 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardProvisioningService.cs` | 145 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardSessionService.cs` | 18 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardSessionService.cs` | 29 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardSessionService.cs` | 36 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardSessionService.cs` | 80 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardSessionService.cs` | 114 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardSessionService.cs` | 129 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardSessionService.cs` | 139 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardSessionService.cs` | 145 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardSessionService.cs` | 153 | MISSING |
| `AegisCore.Infrastructure/Onboarding/WizardSessionService.cs` | 159 | MISSING |
| `AegisCore.Infrastructure/Security/Pqc/PqcKeyCeremonyService.cs` | 100 | MISSING |
| `AegisCore.Infrastructure/Security/Pqc/PqcKeyCeremonyService.cs` | 105 | MISSING |
| `AegisCore.Infrastructure/Security/Pqc/PqcSignatureService.cs` | 64 | MISSING |
| `AegisCore.Infrastructure/Security/Pqc/PqcSignatureService.cs` | 97 | MISSING |
| `AegisCore.Infrastructure/Security/Pqc/PqcSignatureService.cs` | 103 | MISSING |
| `AegisCore.Infrastructure/Security/Pqc/PqcSignatureService.cs` | 132 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/ForeignStudentService.cs` | 14 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/ForeignStudentService.cs` | 31 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/ForeignStudentService.cs` | 33 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/ForeignStudentService.cs` | 34 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/ForeignStudentService.cs` | 40 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/ForeignStudentService.cs` | 55 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/ForeignStudentService.cs` | 68 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/ForeignStudentService.cs` | 74 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/GamificationService.cs` | 12 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/GamificationService.cs` | 37 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/GamificationService.cs` | 49 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/GamificationService.cs` | 56 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/GamificationService.cs` | 63 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/GamificationService.cs` | 64 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/MentalHealthService.cs` | 32 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/MentalHealthService.cs` | 44 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/MentalHealthService.cs` | 73 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/MentalHealthService.cs` | 88 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/VoiceAssistantService.cs` | 53 | MISSING |
| `AegisCore.Infrastructure/StudentLifecycle/VoiceAssistantService.cs` | 91 | MISSING |

## Sensitive AsNoTracking review

