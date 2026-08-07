# AegisCore Demo Data Manifest — IoBM Pilot (August 2026)

## Demo Account Credentials

| Icon | Role | Email | Password | Name |
|------|------|-------|----------|------|
| 👑 | Vice Chancellor | `vc@iobm.demo` | `Lambo$Aventador1` | Prof. Dr. Waseem Ahmed |
| 📋 | Registrar | `registrar@iobm.demo` | `Ferrari$Roma22` | Dr. Sana Mirza |
| 💰 | Finance Head | `finance@iobm.demo` | `Porsche$GT3RS` | Mr. Khalid Hussain |
| 📝 | Exam Controller | `exams@iobm.demo` | `McLaren$720S!` | Ms. Ayesha Qureshi |
| 🔒 | IT Head | `it@iobm.demo` | `Bugatti$Chiron9` | Mr. Bilal Raza |
| 👥 | HR Head | `hr@iobm.demo` | `Aston$Martin77` | Ms. Nadia Shah |
| 👨‍🏫 | Faculty | `faculty@iobm.demo` | `Pagani$Huayra3` | Dr. Imran Siddiqui |
| 🎓 | Student | `student@iobm.demo` | `Koenigsegg$One1` | Ali Hassan |
| 🎓 | Student 2 | `student2@iobm.demo` | `Rimac$Nevera88` | Fatima Malik |

---

## Entity Counts (Demo Dataset)

| Entity | Count | Notes |
|--------|-------|-------|
| **Users** | 500 | 50 staff, 50 faculty, 400 students |
| **Departments** | 4 | CS, Business Admin, Social Sciences, Accounting & Finance |
| **Degree Programs** | 5 | BS CS (4yr), BBA (4yr), BS SS (4yr), MBA (2yr), BS AF (4yr) |
| **Courses** | 48 | 12 per department, with CLO/PLO mapping |
| **Sections** | 62 | Large courses have 2 sections |
| **Faculty** | 50 | 20 permanent, 15 visiting, 10 adjunct, 5 PhD/TAs |
| **Students** | 400 | Distributed across 5 programs, 8 semesters |
| **Enrollments** | ~1,920 | 400 students × ~4.8 courses avg |
| **Grades (Midterm)** | ~1,920 | Midterm marks entered for all |
| **Assignment Marks** | ~4,800 | 2-3 per course per student |
| **Quiz Marks** | ~3,840 | 2 per course per student |
| **Fee Demands** | 400 | One per student for Fall 2025 |
| **Fee Payments** | 362 | 38 defaulters |
| **Scholarship Awards** | 45 | Various types |
| **Attendance Records** | ~38,000 | Daily records across semester |
| **Leave Applications** | 18 | 12 approved, 4 pending, 2 rejected |
| **Payroll Records** | 150 | 3 months × 50 staff |

---

## Key Data Points to Highlight During Demo

### For Vice Chancellor:
- Dashboard: 4,200 enrolled, 68% fee collection, 74% accreditation
- Alert: 3 pending escalations (fee defaulters, conflict detection, lab equipment)
- Enrollment trend chart shows 12% growth

### For Finance Head:
- Revenue this month: PKR 12.4M of PKR 18M target
- 38 fee defaulters, PKR 5.74M outstanding
- Top defaulter: PKR 83,000 overdue (120 days)
- 3 audit flags: duplicate voucher, missing SoD, uncategorized expense

### For Registrar:
- Foreign students: 12 (Afghan 5, Bangladeshi 4, Sri Lankan 3)
- Convocation: 89 eligible, 71 cleared, 18 pending
- Prerequisite conflicts: 3 unresolved

### For Exam Controller:
- 8 grade appeals filed this term, 3 resolved, 5 pending
- Final exam schedule: Dec 8-22, 2025
- 2 unfair means cases reported

### For Student (Ali Hassan):
- CGPA: 3.67, Credits completed: 87/130
- 6 enrolled courses, 92% avg attendance
- Fee: PKR 48,500 (unpaid, 15 days to due)
- 1 pending course registration deadline

---

## Cleanup

To wipe all demo data:
```bash
psql -U aegiscore -d aegiscore -f scripts/wipe-demo-data.sql
```

## Tech Stack
- Next.js 16 + TypeScript (frontend)
- .NET 10 + PostgreSQL (backend)
- Demo mode: `NEXT_PUBLIC_DEMO_MODE=true` in `.env.local`
