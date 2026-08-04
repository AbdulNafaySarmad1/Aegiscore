# AegisCore — Sprint 7: Finance (Fee Management → Accounting → Procurement)

## Role
You are a senior .NET 10 / C# 14 engineer building the complete finance module for AegisCore. This handles student fee management, general ledger accounting, procurement workflows, and HEC/PIFRA compliance reporting for Pakistani universities.

---

## Task 1 — Fee Structure & Payment Management

### Database Tables
```sql
CREATE TABLE fee_structures (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    academic_year_id UUID NOT NULL REFERENCES academic_years(id),
    program_id UUID NOT NULL REFERENCES programs(id),
    semester INT NOT NULL,
    fee_type VARCHAR(50) NOT NULL, -- open_merit | self_finance | hafiz_quota
    total_fee DECIMAL(12, 2) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(academic_year_id, program_id, semester, fee_type)
);

CREATE TABLE fee_components (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    fee_structure_id UUID NOT NULL REFERENCES fee_structures(id),
    component_name VARCHAR(100) NOT NULL, -- tuition | books | lab | activity | hostel
    amount DECIMAL(12, 2) NOT NULL,
    is_mandatory BOOLEAN DEFAULT TRUE,
    order_index INT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE fee_installments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    fee_structure_id UUID NOT NULL REFERENCES fee_structures(id),
    installment_number INT NOT NULL,
    due_date DATE NOT NULL,
    amount DECIMAL(12, 2) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(fee_structure_id, installment_number)
);

CREATE TABLE student_fee_dues (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES users(id),
    semester_id UUID NOT NULL REFERENCES semesters(id),
    fee_structure_id UUID NOT NULL REFERENCES fee_structures(id),
    total_due DECIMAL(12, 2) NOT NULL,
    paid_amount DECIMAL(12, 2) NOT NULL DEFAULT 0,
    scholarship_deduction DECIMAL(12, 2) DEFAULT 0,
    outstanding_amount DECIMAL(12, 2) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending', -- pending | partial | paid | overdue | waived
    waived_by UUID REFERENCES users(id),
    waived_reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE student_fee_payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    fee_due_id UUID NOT NULL REFERENCES student_fee_dues(id),
    payment_method VARCHAR(50) NOT NULL, -- bank_transfer | jazzcash | easypaisa | cheque
    amount DECIMAL(12, 2) NOT NULL,
    reference_number VARCHAR(100),
    payment_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    verified_by UUID REFERENCES users(id),
    verification_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE late_fees (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    fee_due_id UUID NOT NULL REFERENCES student_fee_dues(id),
    grace_days INT NOT NULL DEFAULT 3, -- from policy config
    late_fee_amount DECIMAL(12, 2) NOT NULL,
    applied_on TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Fee Service
```csharp
public interface IFeeService
{
    Task<StudentFeeDues> CalculateFeeAsync(Guid studentId, Guid semesterId, CancellationToken ct = default);
    Task RecordPaymentAsync(Guid feeDueId, PaymentRequest payment, CancellationToken ct = default);
    Task<decimal> CalculateLatefeeAsync(Guid feeDueId, CancellationToken ct = default);
    Task ApplyScholarshipAsync(Guid studentId, decimal amount, Guid scholarshipId, CancellationToken ct = default);
}
```

### Payment Gateway Integration (JazzCash / EasyPaisa)
```csharp
public class PaymentGatewayService
{
    private readonly IHttpClientFactory _httpClient;
    
    public async Task<PaymentInitiationResponse> InitiatePaymentAsync(
        Guid feeDueId, decimal amount, string phoneNumber, CancellationToken ct = default)
    {
        var request = new JazzCashPaymentRequest
        {
            pp_Amount = (long)(amount * 100), // in paisas
            pp_BillReference = feeDueId.ToString("N"),
            pp_Description = "SSUET Semester Fee",
            pp_ReturnURL = "https://aegiscore.ssuet.edu.pk/payment/callback"
        };
        
        var client = _httpClient.CreateClient("jazzcash");
        var response = await client.PostAsJsonAsync(
            "https://sandbox.jazzcash.com.pk/ApplicationAPI/JSON/Purchase/DoMobilAccountTransfer",
            request,
            ct
        );
        
        var result = await response.Content.ReadAsAsync<JazzCashResponse>(ct);
        
        if (result.pp_ResponseCode == "000000")
        {
            return new PaymentInitiationResponse
            {
                IsSuccess = true,
                PaymentUrl = $"https://sandbox.jazzcash.com.pk/pay/{result.pp_TransactionID}",
                TransactionId = result.pp_TransactionID
            };
        }
        
        return new PaymentInitiationResponse { IsSuccess = false };
    }
    
    [HttpPost("/payment/callback")]
    public async Task<IActionResult> PaymentCallbackAsync(PaymentCallbackRequest callback)
    {
        var feeDueId = Guid.Parse(callback.pp_BillReference);
        var feeDue = await _db.StudentFeeDues.FindAsync(feeDueId);
        
        if (callback.pp_ResponseCode == "000000")
        {
            // Payment successful
            await _feeService.RecordPaymentAsync(feeDueId, new PaymentRequest
            {
                Method = "jazzcash",
                Amount = callback.pp_Amount / 100m,
                ReferenceNumber = callback.pp_TransactionID
            });
            
            return Ok(new { success = true });
        }
        
        return Ok(new { success = false });
    }
}
```

---

## Task 2 — Accounting Module (General Ledger)

### Chart of Accounts
```sql
CREATE TABLE chart_of_accounts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    account_code VARCHAR(20) NOT NULL UNIQUE,
    account_name VARCHAR(200) NOT NULL,
    account_type VARCHAR(50) NOT NULL, -- asset | liability | equity | revenue | expense
    account_subtype VARCHAR(50),
    opening_balance DECIMAL(15, 2) DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE general_ledger (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id UUID NOT NULL REFERENCES chart_of_accounts(id),
    transaction_date DATE NOT NULL,
    description TEXT NOT NULL,
    debit DECIMAL(15, 2) DEFAULT 0,
    credit DECIMAL(15, 2) DEFAULT 0,
    reference_id UUID, -- fee_payment_id | payroll_run_id | etc
    reference_type VARCHAR(50), -- fee | payroll | procurement | etc
    posted_by UUID NOT NULL REFERENCES users(id),
    posted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_reconciled BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE cost_centers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(20) NOT NULL UNIQUE,
    name VARCHAR(200) NOT NULL,
    department_id UUID REFERENCES departments(id),
    budget_allocation DECIMAL(15, 2),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE journal_entries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entry_date DATE NOT NULL,
    description TEXT NOT NULL,
    reference_id UUID,
    reference_type VARCHAR(50),
    total_debit DECIMAL(15, 2),
    total_credit DECIMAL(15, 2),
    status VARCHAR(20) DEFAULT 'draft', -- draft | posted | reversed
    created_by UUID NOT NULL REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE journal_line_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    journal_id UUID NOT NULL REFERENCES journal_entries(id),
    account_id UUID NOT NULL REFERENCES chart_of_accounts(id),
    cost_center_id UUID REFERENCES cost_centers(id),
    debit DECIMAL(15, 2) DEFAULT 0,
    credit DECIMAL(15, 2) DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Auto-Posting from Fee Payments
```csharp
public class AccountingService
{
    public async Task PostFeePaymentAsync(StudentFeePayment payment, CancellationToken ct = default)
    {
        var journal = new JournalEntry
        {
            EntryDate = payment.PaymentDate,
            Description = $"Fee payment from {payment.Student.Email}",
            ReferenceId = payment.Id,
            ReferenceType = "fee"
        };
        
        // Debit: Bank/Cash account
        var bankAccount = await _db.ChartOfAccounts
            .FirstAsync(x => x.AccountCode == "1010", ct); // Bank account
        
        journal.LineItems.Add(new JournalLineItem
        {
            AccountId = bankAccount.Id,
            Debit = payment.Amount
        });
        
        // Credit: Student Fee Revenue account
        var revenueAccount = await _db.ChartOfAccounts
            .FirstAsync(x => x.AccountCode == "4010", ct); // Fee revenue
        
        journal.LineItems.Add(new JournalLineItem
        {
            AccountId = revenueAccount.Id,
            Credit = payment.Amount
        });
        
        _db.JournalEntries.Add(journal);
        await _db.SaveChangesAsync(ct);
    }
}
```

---

## Task 3 — Procurement Module

### Database Tables
```sql
CREATE TABLE procurement_requisitions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    requisition_number VARCHAR(50) NOT NULL UNIQUE,
    requesting_department_id UUID NOT NULL REFERENCES departments(id),
    requested_by UUID NOT NULL REFERENCES users(id),
    requisition_date DATE NOT NULL DEFAULT CURRENT_DATE,
    required_by_date DATE,
    total_amount DECIMAL(15, 2),
    status VARCHAR(20) NOT NULL DEFAULT 'draft', -- draft | submitted | approved | rejected | ordered
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE requisition_line_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    requisition_id UUID NOT NULL REFERENCES procurement_requisitions(id),
    item_code VARCHAR(100),
    description TEXT NOT NULL,
    quantity INT NOT NULL,
    unit_price DECIMAL(12, 2),
    total_price DECIMAL(15, 2),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE vendors (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_name VARCHAR(200) NOT NULL,
    vendor_cnic VARCHAR(15),
    bank_account VARCHAR(50),
    contact_person VARCHAR(100),
    email VARCHAR(200),
    phone VARCHAR(20),
    address TEXT,
    status VARCHAR(20) DEFAULT 'active',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE rate_contracts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_id UUID NOT NULL REFERENCES vendors(id),
    item_code VARCHAR(100) NOT NULL,
    item_name VARCHAR(200) NOT NULL,
    rate DECIMAL(12, 2) NOT NULL,
    validity_from DATE NOT NULL,
    validity_to DATE NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE purchase_orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    po_number VARCHAR(50) NOT NULL UNIQUE,
    requisition_id UUID NOT NULL REFERENCES procurement_requisitions(id),
    vendor_id UUID NOT NULL REFERENCES vendors(id),
    po_date DATE NOT NULL,
    delivery_date DATE,
    total_amount DECIMAL(15, 2),
    status VARCHAR(20) DEFAULT 'issued', -- issued | acknowledged | partial_delivery | completed | cancelled
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE goods_received_notes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    grn_number VARCHAR(50) NOT NULL UNIQUE,
    po_id UUID NOT NULL REFERENCES purchase_orders(id),
    received_date DATE NOT NULL,
    received_by UUID NOT NULL REFERENCES users(id),
    total_items INT NOT NULL,
    status VARCHAR(20) DEFAULT 'received', -- received | inspected | accepted | rejected
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Workflow
1. Department submits requisition (draft)
2. HOD reviews and approves requisition
3. Finance Head reviews budget availability
4. Finance creates PO for approved requisition
5. Vendor receives and acknowledges PO
6. Goods received (GRN created)
7. Finance verifies GRN matches PO
8. Payment triggered to vendor

### PPRA Compliance
- Flag requisitions > PKR 500,000 for competitive bidding
- Maintain vendor blacklist (PPRA debarred vendors)
- Export procurement reports for audit

---

## Task 4 — Scholarship & Financial Aid

### Database Tables
```sql
CREATE TABLE scholarships (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    scholarship_name VARCHAR(200) NOT NULL,
    scholarship_type VARCHAR(50), -- need_based | merit_based | sports | hafiz
    amount_per_student DECIMAL(12, 2),
    total_budget DECIMAL(15, 2),
    eligibility_criteria TEXT,
    available_seats INT,
    academic_year_id UUID NOT NULL REFERENCES academic_years(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE scholarship_applications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    scholarship_id UUID NOT NULL REFERENCES scholarships(id),
    student_id UUID NOT NULL REFERENCES users(id),
    application_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    status VARCHAR(20) DEFAULT 'pending', -- pending | approved | rejected | awarded
    approved_by UUID REFERENCES users(id),
    approved_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

---

## Task 5 — Budget & Variance Analysis

```csharp
public interface IBudgetService
{
    Task<BudgetVarianceReport> GetVarianceAsync(Guid costCenterId, Guid periodId, CancellationToken ct = default);
    Task<List<CostCenterBudget>> GetBudgetStatusAsync(CancellationToken ct = default);
}

public class BudgetVarianceReport
{
    public Guid CostCenterId { get; set; }
    public decimal BudgetedAmount { get; set; }
    public decimal ActualExpense { get; set; }
    public decimal Variance { get; set; }
    public decimal VariancePercentage { get; set; }
    public string Status { get; set; } // on_budget | over_budget
}
```

---

## Task 6 — HEC PIFRA Reporting

For Pakistani public sector universities:
- PIFRA = Public Financial Management Reforms
- HEC mandates quarterly financial reports in specific format
- One-click export mapping AegisCore GL to PIFRA template

---

## Deliverables Checklist
- [ ] Fee structure builder + component management
- [ ] Student fee dues calculation
- [ ] Payment gateway integration (JazzCash, EasyPaisa)
- [ ] Chart of accounts + general ledger
- [ ] Journal entry posting
- [ ] Auto-posting from fee/payroll/procurement
- [ ] Cost center management
- [ ] Procurement workflow (requisition → PO → GRN → payment)
- [ ] Vendor management
- [ ] Rate contracts
- [ ] PPRA compliance checks
- [ ] Scholarship management
- [ ] Budget allocation + variance reporting
- [ ] HEC PIFRA export
- [ ] All notifications (payment reminders, low budget alerts)
- [ ] Unit tests + integration tests
