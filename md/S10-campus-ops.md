# AegisCore — Sprint 10: Campus Operations (Library + Transport + Visitor Management)

## Role
You are a senior .NET 10 / C# 14 engineer building campus operations modules for AegisCore. These handle day-to-day operational workflows outside academics.

---

## Task 1 — Library Management

### Database Tables
```sql
CREATE TABLE library_books (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    isbn VARCHAR(20),
    title VARCHAR(300) NOT NULL,
    author VARCHAR(200),
    publisher VARCHAR(200),
    publication_year INT,
    category VARCHAR(100),
    subcategory VARCHAR(100),
    quantity_total INT NOT NULL,
    quantity_available INT NOT NULL,
    price DECIMAL(12, 2),
    shelf_location VARCHAR(100),
    added_date DATE NOT NULL DEFAULT CURRENT_DATE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE library_issue_return (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    book_id UUID NOT NULL REFERENCES library_books(id),
    student_id UUID NOT NULL REFERENCES users(id),
    issued_date DATE NOT NULL,
    due_date DATE NOT NULL,
    returned_date DATE,
    fine_amount DECIMAL(10, 2) DEFAULT 0,
    status VARCHAR(20) DEFAULT 'issued', -- issued | overdue | returned
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE library_e_resources (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    resource_name VARCHAR(300) NOT NULL,
    resource_type VARCHAR(50), -- journal | ebook | database | video
    url TEXT NOT NULL,
    access_key VARCHAR(200),
    expiry_date DATE,
    subscription_cost DECIMAL(12, 2),
    publisher VARCHAR(200),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### OPAC (Online Public Access Catalog)
- Search books by title, author, ISBN
- Display availability (# available / total)
- Reservation system (student can request unavailable book)
- Student can view issued books + due dates

### Librarian Portal
- Book catalog management (add, edit, delete)
- Issue/return tracking
- Overdue tracking + fine calculation
- E-resource subscription management
- Inventory reports

### Fine Calculation
```csharp
public class LibraryFineService
{
    public decimal CalculateFine(IssueReturn record)
    {
        var overdueDate = record.DueDate.AddDays(-1);
        var returnedDate = record.ReturnedDate ?? DateTime.Now;
        var daysOverdue = (returnedDate - overdueDate).TotalDays;
        
        if (daysOverdue <= 0) return 0;
        
        var dailyFine = 10m; // PKR per day (configurable)
        var maxFine = 500m; // Cap (configurable)
        
        var calculatedFine = (decimal)daysOverdue * dailyFine;
        return Math.Min(calculatedFine, maxFine);
    }
}
```

### Overdue Reminder
- Automated email/SMS at due date + 3 days + 7 days
- Include fine calculation

---

## Task 2 — Transport Management

### Database Tables
```sql
CREATE TABLE transport_routes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    route_name VARCHAR(100) NOT NULL,
    starting_point VARCHAR(100) NOT NULL,
    ending_point VARCHAR(100) NOT NULL,
    distance_km DECIMAL(5, 2),
    estimated_duration_minutes INT,
    cost_per_semester DECIMAL(10, 2),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE transport_stops (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    route_id UUID NOT NULL REFERENCES transport_routes(id),
    stop_name VARCHAR(100) NOT NULL,
    stop_order INT NOT NULL,
    location_latitude DECIMAL(10, 8),
    location_longitude DECIMAL(10, 8),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE transport_vehicles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vehicle_number VARCHAR(20) NOT NULL UNIQUE,
    vehicle_type VARCHAR(50), -- bus | van | car
    capacity INT NOT NULL,
    route_id UUID REFERENCES transport_routes(id),
    driver_name VARCHAR(100),
    driver_phone VARCHAR(20),
    gps_device_id VARCHAR(100),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE student_transport_allocation (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES users(id),
    route_id UUID NOT NULL REFERENCES transport_routes(id),
    vehicle_id UUID NOT NULL REFERENCES transport_vehicles(id),
    semester_id UUID NOT NULL REFERENCES semesters(id),
    status VARCHAR(20) DEFAULT 'active', -- active | suspended | cancelled
    allocated_date DATE NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Transport Portal
- Students register for transport
- View allocated vehicle, route, pick-up time
- GPS tracking link (live vehicle location)
- Transport fee billing (linked to student fee)

### Admin Portal
- Route management
- Vehicle allocation
- Driver management
- Capacity monitoring
- GPS integration (real-time tracking)

---

## Task 3 — Visitor Management & Gate Pass System

### Database Tables
```sql
CREATE TABLE visitor_pre_registrations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    visitor_name VARCHAR(200) NOT NULL,
    visitor_cnic VARCHAR(15),
    visitor_phone VARCHAR(20),
    host_user_id UUID NOT NULL REFERENCES users(id),
    visit_purpose TEXT,
    visit_date DATE NOT NULL,
    visit_time_from TIME NOT NULL,
    visit_time_to TIME NOT NULL,
    status VARCHAR(20) DEFAULT 'pending', -- pending | approved | rejected | checked_in | checked_out
    approved_by UUID REFERENCES users(id),
    approved_at TIMESTAMPTZ,
    checked_in_at TIMESTAMPTZ,
    checked_out_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE walk_in_visitors (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    visitor_name VARCHAR(200) NOT NULL,
    visitor_cnic VARCHAR(15),
    visitor_phone VARCHAR(20),
    visit_purpose TEXT,
    host_name VARCHAR(200),
    checked_in_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    checked_out_at TIMESTAMPTZ,
    checked_in_by UUID NOT NULL REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE digital_gate_passes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    visitor_registration_id UUID NOT NULL REFERENCES visitor_pre_registrations(id),
    qr_code TEXT NOT NULL,
    pass_number VARCHAR(50) NOT NULL UNIQUE,
    validity_from TIMESTAMPTZ NOT NULL,
    validity_to TIMESTAMPTZ NOT NULL,
    is_used BOOLEAN DEFAULT FALSE,
    used_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Visitor Pre-Registration (for known visitors)
1. Visitor requests visit (via form or phone)
2. System creates pre-registration record
3. Email to host for approval
4. If approved: generate digital QR-based gate pass
5. Email QR pass to visitor
6. At gate: security scans QR → logged in system

### Walk-in Visitor (for unexpected visitors)
1. Visitor arrives at gate
2. Security logs visitor details
3. Security contacts host via phone/intercom
4. If approved: manual gate pass issued (paper or QR-generated on spot)
5. Visitor escorted to host office
6. On departure: logged out

### Event Bulk Visitors
- University organizes event (conference, sports, seminar)
- Event coordinator uploads bulk visitor list (Excel)
- System generates QR passes for all
- Bulk email to visitors
- Gate security uses same QR scanning

### Gate Security Portal
- Mobile-friendly interface
- Scan QR code from visitor's phone
- Mark entry/exit
- Real-time visitor log
- Report suspicious activity

---

## Deliverables Checklist
- [ ] Library catalog management + OPAC (student view)
- [ ] Issue/return workflow + fine calculation
- [ ] Overdue reminders (email + SMS)
- [ ] E-resource subscription tracking
- [ ] Transport route + vehicle management
- [ ] Student transport allocation + GPS tracking
- [ ] Visitor pre-registration workflow
- [ ] Digital QR gate pass generation
- [ ] Walk-in visitor logging
- [ ] Event bulk visitor upload
- [ ] Gate security portal (mobile-optimized)
- [ ] Real-time visitor log
- [ ] All notifications
- [ ] Tests
