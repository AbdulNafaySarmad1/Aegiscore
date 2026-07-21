# Document 1 — Vision Document
**Standard:** ISO/IEC/IEEE 29148

| Field | Value |
|-------|-------|
| Document ID | AEGIS-VISION-001 |
| Version | 1.0.0 |
| Date | 2025-01-15 |
| Author | Abdul Nafay Sarmad, Founder & CEO |
| Status | Approved |

## Revision History

| Version | Date | Author | Description |
|---------|------|--------|-------------|
| 1.0.0 | 2025-01-15 | Abdul Nafay Sarmad | Initial release |

## 1. Business Opportunity and Problem Statement

Pakistan's public university sector comprises over 150 Higher Education Institutions (HEIs) serving approximately 2.5 million students. Despite significant government investment through the Higher Education Commission (HEC), the vast majority of these institutions operate with fragmented, paper-based, or locally developed administrative systems that fail to meet modern requirements for data integrity, security, and operational efficiency.

The current landscape presents a critical gap: international Enterprise Resource Planning (ERP) solutions such as SAP and Oracle have been deployed at a handful of elite institutions at costs exceeding $400 million cumulatively, yet these implementations have consistently failed to deliver value commensurate with their investment. The failures stem from fundamental misalignment between global ERP paradigms and the specific regulatory, cultural, and operational realities of Pakistani higher education. These systems require extensive customisation, ongoing expensive consultancy, and create vendor lock-in that leaves institutions vulnerable to price escalation and obsolescence.

Meanwhile, locally developed alternatives lack the architectural rigour, security posture, and scalability required for institutions managing tens of thousands of students. They typically operate as single-tenant installations with no path to horizontal scaling, no formal authorisation model, and no defence against the sophisticated cyber threats that increasingly target educational institutions.

AegisCore addresses this gap directly: a purpose-built, multi-tenant, cloud-native university ERP platform designed specifically for Pakistani public universities, engineered to international standards (ISO/IEC 27001, OWASP ASVS Level 2), validated at 9,000+ concurrent users, and priced at a fraction of the cost of failed international alternatives.

## 2. Product Vision Statement (Geoffrey Moore Format)

**FOR** Pakistani public universities burdened by fragmented administrative systems and failed international ERP deployments

**WHO** require a unified, secure, and scalable platform to manage the complete student lifecycle, financial operations, faculty workflows, and institutional analytics

**THE** AegisCore platform is a multi-tenant, cloud-native university ERP system

**THAT** provides end-to-end digital transformation across six integrated modules — Student Lifecycle, Finance & Fees, Faculty Portal, ML Analytics, Enterprise Security, and System Administration — with enterprise-grade security (Casbin ABAC, Argon2id), validated performance at 9,000+ concurrent users, and full compliance with HEC regulatory requirements

**UNLIKE** SAP and Oracle, which impose foreign paradigms, require extensive customisation, and create unsustainable cost structures, and unlike local alternatives that lack scalability, security, and architectural rigour

**OUR PRODUCT** delivers a purpose-built solution designed from first principles for the Pakistani higher education context, with transparent pricing, no vendor lock-in, and a deployment model that respects institutional sovereignty while providing world-class infrastructure.

## 3. Stakeholder Identification

| Stakeholder | Role | Interest | Influence | Engagement Strategy |
|-------------|------|----------|-----------|---------------------|
| Higher Education Commission (HEC) | Regulatory body | Compliance, standardisation, data reporting | High | Formal partnership, compliance certification |
| Ministry of IT & Telecom | Government oversight | Digital Pakistan alignment, cybersecurity | High | Policy alignment, security certification |
| Vice Chancellors | Institutional leadership | Strategic oversight, cost control, reputation | Critical | Executive briefings, ROI demonstrations |
| Registrars | Operational leadership | Student records, admissions, examinations | Critical | Workflow demonstrations, migration planning |
| Deans & Department Heads | Academic leadership | Faculty management, curriculum, quality assurance | High | Faculty portal demonstrations |
| Faculty Members | End users | Course management, attendance, grading | Medium | Training programmes, feedback loops |
| Students | Primary beneficiaries | Records access, fee payment, timetable, results | Medium | Student portal, mobile access |
| Finance Officers | End users | Fee collection, ledger, reconciliation, POS | High | Financial workflow demonstrations |
| IT Directors | Technical decision-makers | Infrastructure, security, integration | High | Technical deep-dives, architecture reviews |
| NICAT (National Incubation Centre) | Innovation partner | Startup validation, mentorship | Medium | Partnership agreement |
| Investors (SAM, etc.) | Financial stakeholders | Return on investment, market growth | High | Quarterly board updates |
| System Administrators | Operational staff | Deployment, maintenance, monitoring | Medium | Administrator training, documentation |

## 4. Key Needs per Stakeholder

| Stakeholder | Key Need | Priority |
|-------------|----------|----------|
| HEC | Standardised data reporting across all institutions | High |
| HEC | Compliance with data privacy regulations | High |
| HEC | Interoperability with HEC central systems | Medium |
| Ministry of IT | Cybersecurity compliance with national standards | High |
| Ministry of IT | Data sovereignty — all data remains within Pakistan or approved jurisdictions | High |
| Vice Chancellors | Single unified view of institutional performance | High |
| Vice Chancellors | Cost predictability — no surprise licence fees | High |
| Vice Chancellors | Data-driven decision support through analytics | Medium |
| Registrars | Complete student lifecycle management from admission to graduation | Critical |
| Registrars | Automated examination scheduling and result processing | Critical |
| Registrars | Audit trail for all record changes | High |
| Faculty | Simple attendance marking and grade entry | High |
| Faculty | Access to student history and analytics | Medium |
| Students | Self-service access to records, fees, timetable | High |
| Students | Mobile-friendly interface | Medium |
| Finance Officers | Integrated fee collection with POS support | Critical |
| Finance Officers | Automated reconciliation and ledger management | High |
| Finance Officers | Financial reporting for audits | High |
| IT Directors | Multi-tenant architecture with tenant isolation | Critical |
| IT Directors | Horizontal scalability to 50,000 concurrent users | High |
| IT Directors | Disaster recovery with < 1 hour RTO | High |
| Investors | Addressable market of 150+ institutions | High |
| Investors | Recurring revenue model (annual licence + monthly support) | High |
| Investors | Competitive moat through regulatory alignment and localisation | Medium |

## 5. Product Overview and Scope Boundaries

### 5.1 What AegisCore Is

AegisCore is a comprehensive, multi-tenant, cloud-native university ERP platform comprising six integrated modules:

1. **Student Lifecycle Module:** Admissions, enrolment, registration, attendance tracking, examination scheduling, grade management, transcript generation, and graduation processing.

2. **Finance & Fees Module:** Fee structure definition, fee collection (online and POS), ledger management, reconciliation, financial reporting, and integration with institutional banking partners.

3. **Faculty Portal:** Course management, attendance marking, grade entry, student performance analytics, departmental workflow automation, and research output tracking.

4. **ML Analytics Module:** Predictive analytics for student retention, performance trend analysis, resource utilisation forecasting, and institutional benchmarking using pgvector embeddings.

5. **Enterprise Security Module:** Casbin Attribute-Based Access Control (ABAC), Argon2id password hashing, JWT-based authentication, tenant isolation via PostgreSQL Row-Level Security, encryption at rest and in transit, comprehensive audit logging.

6. **System Administration Module:** Tenant provisioning, user management, role and permission configuration, system health monitoring, backup management, and licence tracking.

### 5.2 What AegisCore Is Not

- AegisCore is not a Learning Management System (LMS) — it does not provide course content delivery, online assessments, or virtual classrooms. It integrates with existing LMS platforms via API.
- AegisCore is not a Human Resources Management System (HRMS) — it does not manage payroll, recruitment, or employee benefits beyond basic faculty directory functions.
- AegisCore is not a Library Management System — it does not catalogue books, manage lending, or track library inventory.
- AegisCore is not a Research Management System — it does not manage grant applications, research funding, or publication workflows beyond basic faculty research output tracking.
- AegisCore is not a general-purpose ERP — it is purpose-built for higher education and does not extend to procurement, inventory, or facilities management beyond what is directly relevant to academic administration.

## 6. Assumptions and Dependencies

### 6.1 Assumptions

1. Pakistani public universities will have reliable internet connectivity sufficient for cloud-based operations by the time of deployment.
2. HEC will continue to mandate digital transformation and standardised reporting for public universities.
3. Institutional leadership (Vice Chancellors, Registrars) will champion the adoption of a unified ERP system.
4. Faculty and staff will receive adequate training and change management support.
5. The regulatory environment for data privacy and cybersecurity will continue to evolve in alignment with international standards.
6. The Pakistani Rupee to US Dollar exchange rate will remain within a manageable range for infrastructure costs denominated in foreign currency.
7. The supply of skilled .NET developers in Pakistan will be sufficient to support ongoing development and maintenance.

### 6.2 Dependencies

1. **Hetzner EPYC infrastructure:** AegisCore depends on Hetzner's Helsinki and Nuremberg data centres for primary and disaster recovery hosting.
2. **Azure App Service:** The CI/CD pipeline and certain auxiliary services depend on Microsoft Azure.
3. **PostgreSQL:** The entire data layer depends on PostgreSQL 16+ with pgvector extension.
4. **Redis:** Caching, session management, and real-time features depend on Redis 7+.
5. **Infisical:** Secrets management depends on Infisical for secure configuration injection.
6. **Casbin:** The authorisation layer depends on Casbin.NET for ABAC policy enforcement.
7. **GitHub Actions:** The CI/CD pipeline depends on GitHub Actions for automated build, test, and deployment.
8. **HEC regulatory framework:** Compliance certification depends on HEC's published standards and audit processes.
9. **Banking partners:** Online fee collection depends on integration with Pakistani banking APIs (1Link, etc.).

## 7. Market Positioning and Competitive Context

### 7.1 Competitive Landscape

| Competitor | Category | Strengths | Weaknesses | AegisCore Advantage |
|------------|----------|-----------|------------|---------------------|
| SAP S/4HANA | Global ERP | Brand recognition, extensive module coverage | Prohibitively expensive, requires extensive customisation, vendor lock-in, not designed for Pakistani context | Purpose-built, 90% lower TCO, no vendor lock-in |
| Oracle PeopleSoft | Global ERP | Mature campus solutions, large install base | Legacy architecture, declining investment, expensive maintenance | Modern .NET 10 stack, cloud-native, active development |
| Campus Management (local) | Local ERP | Low initial cost, some localisation | Single-tenant, no scalability, poor security, limited features | Multi-tenant, validated at 9,000+ users, enterprise security |
| Custom-built solutions | In-house | Tailored to specific institution | High maintenance cost, no shared development, security risks | Shared platform, continuous improvement, professional security |
| Manual/paper-based | No system | No technology cost | Inefficient, error-prone, no analytics, no compliance | Complete digital transformation |

### 7.2 Market Positioning Statement

AegisCore occupies a unique position as the only university ERP platform that combines enterprise-grade architecture (multi-tenant, horizontally scalable, ABAC-secured) with deep localisation for Pakistani higher education, at a price point accessible to public universities. It bridges the gap between unaffordable international solutions and inadequate local alternatives.

### 7.3 Target Market Size

- **Primary market:** 150+ Pakistani public universities (HEC-recognised)
- **Secondary market:** Private universities in Pakistan (70+ institutions)
- **Tertiary market:** Higher education institutions in other South Asian and MENA countries with similar regulatory and operational contexts
- **Total Addressable Market (SAM):** PKR 5 billion (approximately $18 million USD at current exchange rates)

## 8. Success Metrics and Acceptance Criteria

### 8.1 Business Success Metrics

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| Institutions deployed | 10 within first 12 months, 50 within 36 months | Contract count |
| Annual recurring revenue | PKR 500 million by Year 3 | Financial reporting |
| Customer retention rate | > 95% annually | Contract renewal rate |
| Net Promoter Score (NPS) | > 50 | Annual survey |
| Time to deploy per institution | < 30 days from contract to production | Project tracking |

### 8.2 Technical Success Metrics

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| Concurrent user capacity | 9,000+ validated, 50,000 design target | Load testing (k6) |
| Response time (p95) | < 200ms under full load | Application performance monitoring |
| System availability | 99.9% uptime (excluding planned maintenance) | Uptime monitoring |
| Security compliance | OWASP ASVS Level 2, ISO/IEC 27001 aligned | External audit |
| Data durability | 99.999999999% (11 nines) | PostgreSQL replication verification |
| Recovery Time Objective (RTO) | < 1 hour | DR drill measurement |
| Recovery Point Objective (RPO) | < 5 minutes | Replication lag monitoring |

### 8.3 Acceptance Criteria for Version 1.0.0

1. All six modules functional and integrated
2. Multi-tenant architecture validated with 10 concurrent tenants
3. Load test passed: 9,000+ concurrent users sustained for 7 days with zero critical failures
4. Security audit passed: OWASP ASVS Level 2 controls verified
5. ABAC authorisation model tested with all defined roles and permissions
6. Disaster recovery failover tested: Helsinki to Nuremberg < 1 hour
7. Documentation suite complete (all 16 documents)
8. HEC compliance certification obtained or application submitted
9. Deployment pipeline automated: GitHub Actions to Hetzner and Azure
10. Two reference institutions successfully deployed and operational for 90 days

## 9. References

1. ISO/IEC/IEEE 29148:2018 — Systems and software engineering — Life cycle processes — Requirements engineering
2. Higher Education Commission Pakistan — Digital Transformation Framework for Public Universities (2024)
3. Ministry of IT & Telecom Pakistan — National Cybersecurity Policy (2023)
4. OWASP Application Security Verification Standard (ASVS) Version 4.0.3
5. NIST Cybersecurity Framework Version 1.1
6. AegisCore P1.txt — Project Context Document
