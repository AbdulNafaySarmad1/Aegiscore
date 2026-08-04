# AegisCore — Sprint 12: Asset Management + Inventory + HEC Reporting

## Role
You are a senior .NET 10 / C# 14 engineer building the asset and inventory management for AegisCore, plus HEC compliance reporting (HEMIS, mandatory disclosures, accreditation evidence).

---

## Task 1 — Asset Management

### Database Tables
```sql
CREATE TABLE asset_categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    category_name VARCHAR(100) NOT NULL UNIQUE,
    depreciation_rate DECIMAL(5, 2), -- % per year
    asset_life_years INT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE assets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    asset_code VARCHAR(50) NOT NULL UNIQUE,
    asset_name VARCHAR(200) NOT NULL,
    category_id UUID NOT NULL REFERENCES asset_categories(id),
    purchase_date DATE NOT NULL,
    purchase_value DECIMAL(15, 2) NOT NULL,
    current_value DECIMAL(15, 2),
    location_id UUID REFERENCES departments(id),
    custodian_user_id UUID REFERENCES users(id),
    supplier_id UUID REFERENCES vendors(id),
    condition VARCHAR(20) DEFAULT 'good', -- good | fair | poor
    status VARCHAR(20) DEFAULT 'active', -- active | disposed | transferred
    disposal_date DATE,
    disposal_reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE asset_depreciation (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    asset_id UUID NOT NULL REFERENCES assets(id),
    depreciation_date DATE NOT NULL,
    depreciation_amount DECIMAL(15, 2),
    accumulated_depreciation DECIMAL(15, 2),
    net_book_value DECIMAL(15, 2),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE asset_maintenance_contracts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    asset_id UUID NOT NULL REFERENCES assets(id),
    vendor_id UUID NOT NULL REFERENCES vendors(id),
    contract_start_date DATE NOT NULL,
    contract_end_date DATE NOT NULL,
    annual_cost DECIMAL(12, 2),
    contract_details TEXT,
    renewal_reminder_days INT DEFAULT 30,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE asset_insurance (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    asset_id UUID NOT NULL REFERENCES assets(id),
    insurance_provider VARCHAR(200),
    policy_number VARCHAR(100),
    coverage_amount DECIMAL(15, 2),
    policy_start_date DATE NOT NULL,
    policy_end_date DATE NOT NULL,
    annual_premium DECIMAL(12, 2),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Asset Service
```csharp
public interface IAssetService
{
    Task<decimal> CalculateCurrentValueAsync(Guid assetId, CancellationToken ct = default);
    Task<List<AssetDepreciation>> GetDepreciationScheduleAsync(Guid assetId, CancellationToken ct = default);
    Task<AssetRegister> GenerateAssetRegisterAsync(Guid departmentId, CancellationToken ct = default);
    Task TransferAssetAsync(Guid assetId, Guid fromDept, Guid toDept, string reason, Guid actorId, CancellationToken ct = default);
    Task DisposeAssetAsync(Guid assetId, string reason, Guid actorId, CancellationToken ct = default);
}
```

### Depreciation Calculation
```csharp
public class DepreciationCalculator
{
    public decimal CalculateDepreciation(Asset asset, DateTime asAtDate)
    {
        var category = asset.Category;
        var monthsOwned = (asAtDate - asset.PurchaseDate).TotalDays / 30;
        
        // Straight-line depreciation: (cost / life) * months
        var monthlyDepreciation = (asset.PurchaseValue / (category.AssetLifeYears * 12));
        var totalDepreciation = monthlyDepreciation * monthsOwned;
        
        return Math.Min(totalDepreciation, asset.PurchaseValue); // cap at purchase value
    }
}
```

### Asset Register Reports
- Department-wise asset register
- Asset movement history
- Depreciation schedules
- Insurance & AMC expiry alerts
- Condition assessments (for phased replacement)

---

## Task 2 — Inventory / Stores Management

### Database Tables
```sql
CREATE TABLE inventory_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    item_code VARCHAR(100) NOT NULL UNIQUE,
    item_name VARCHAR(200) NOT NULL,
    item_category VARCHAR(50), -- stationery | chemicals | equipment | books
    unit_of_measure VARCHAR(20), -- pieces | kgs | liters | boxes
    reorder_level INT NOT NULL,
    reorder_quantity INT NOT NULL,
    unit_cost DECIMAL(12, 2),
    current_stock INT NOT NULL DEFAULT 0,
    warehouse_location VARCHAR(100),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE inventory_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    item_id UUID NOT NULL REFERENCES inventory_items(id),
    transaction_type VARCHAR(20) NOT NULL, -- received | issued | adjustment | return
    quantity INT NOT NULL,
    transaction_date DATE NOT NULL,
    reference_id UUID, -- GRN_id or requisition_id
    issued_to_department_id UUID REFERENCES departments(id),
    notes TEXT,
    recorded_by UUID NOT NULL REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE inventory_audits (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    audit_date DATE NOT NULL,
    audited_by UUID NOT NULL REFERENCES users(id),
    item_id UUID NOT NULL REFERENCES inventory_items(id),
    system_quantity INT NOT NULL,
    physical_count INT NOT NULL,
    variance INT,
    variance_reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Inventory Service
```csharp
public interface IInventoryService
{
    Task<bool> IsInStockAsync(Guid itemId, int quantity, CancellationToken ct = default);
    Task IssueAsync(Guid itemId, int quantity, Guid departmentId, string reference, Guid actorId, CancellationToken ct = default);
    Task ReceiveAsync(Guid itemId, int quantity, string grnNumber, Guid actorId, CancellationToken ct = default);
    Task<List<InventoryItem>> GetLowStockItemsAsync(CancellationToken ct = default);
}
```

### Low Stock Alerts
- Auto-generated when stock < reorder level
- Triggers purchase requisition
- Notification to procurement department

---

## Task 3 — HEC Compliance & Reporting

### HEMIS Data Export
```sql
CREATE TABLE hemis_export_mappings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    hemis_field_name VARCHAR(100) NOT NULL,
    aegiscore_table VARCHAR(100) NOT NULL,
    aegiscore_column VARCHAR(100) NOT NULL,
    transformation_function TEXT, -- optional SQL/code
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### One-Click HEMIS Export
- Map AegisCore fields to HEC's HEMIS structure
- Extract data for current academic year
- Generate Excel workbook in HEC format
- Includes:
  - Faculty data
  - Student enrollment
  - Program outcomes
  - Course information
  - Grades
  - Degrees awarded

### Mandatory Disclosure Repository
```sql
CREATE TABLE mandatory_disclosures (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    disclosure_type VARCHAR(100), -- financial | academic | governance | infrastructure
    document_name VARCHAR(200) NOT NULL,
    document_path TEXT NOT NULL,
    uploaded_by UUID NOT NULL REFERENCES users(id),
    uploaded_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    validity_from DATE,
    validity_to DATE,
    is_public BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

Types of mandatory disclosures:
- Vision, mission, values
- Governance structure (BOG, academic council)
- Financial statements (audited)
- Academic calendar
- Fee structure
- Grievance procedure
- Accreditation status

### Accreditation Evidence Repository
```sql
CREATE TABLE accreditation_documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    accreditation_type VARCHAR(50), -- NAAC | HEC_QA | NCEAC
    evidence_category VARCHAR(100), -- student_learning | faculty_development | research
    document_name VARCHAR(200) NOT NULL,
    document_path TEXT NOT NULL,
    uploaded_by UUID NOT NULL REFERENCES users(id),
    uploaded_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### HEC Audit Trail
```csharp
[HttpGet("/api/admin/compliance/hemis-export")]
[RequireRole("Registrar")]
public async Task<IActionResult> ExportHEMISAsync()
{
    var hemisData = new HEMISExportService()
        .BuildExport(
            academicYearId: _yearService.GetCurrentYear(),
            timestamp: DateTime.UtcNow
        );
    
    var workbook = new ExcelExport().CreateWorkbook(hemisData);
    var fileName = $"HEMIS_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
    
    // Log export
    _auditService.LogAsync(new AuditEntry
    {
        ActorId = User.Id,
        Action = "HEMIS_Export",
        Module = "Compliance",
        NewValue = fileName,
        Description = $"HEMIS export for {hemisData.AcademicYear}"
    });
    
    return File(workbook, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
}
```

---

## Deliverables Checklist
- [ ] Asset master + asset register
- [ ] Asset depreciation calculation (straight-line)
- [ ] Asset transfer tracking
- [ ] Asset disposal workflow
- [ ] Depreciation schedules
- [ ] AMC & insurance tracking
- [ ] Asset insurance reminders
- [ ] Inventory item master
- [ ] Stock in/out transactions
- [ ] Low stock alerts + reorder triggers
- [ ] Inventory audit reports
- [ ] Stock variance analysis
- [ ] HEMIS data export (one-click)
- [ ] Mandatory disclosure repository
- [ ] Accreditation evidence repository
- [ ] HEC compliance audit trail
- [ ] All tests
