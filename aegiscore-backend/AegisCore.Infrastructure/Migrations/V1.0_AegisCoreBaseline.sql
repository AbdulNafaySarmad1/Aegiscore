CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TYPE account_type AS ENUM ('asset', 'liability', 'equity', 'revenue', 'expense');
    CREATE TYPE accreditation_cycle_status AS ENUM ('planning', 'self_study', 'visit_scheduled', 'visit_completed', 'report_received', 'accredited', 'probation', 'not_accredited');
    CREATE TYPE achievement_type AS ENUM ('academic', 'sports', 'cultural', 'research', 'community_service', 'other');
    CREATE TYPE action_item_status AS ENUM ('open', 'in_progress', 'completed', 'cancelled');
    CREATE TYPE activity_type AS ENUM ('call', 'email', 'meeting', 'sms', 'note', 'status_change');
    CREATE TYPE ai_model_capability AS ENUM ('chat', 'embedding', 'vision', 'function_calling');
    CREATE TYPE ai_provider_type AS ENUM ('open_ai', 'anthropic', 'azure_open_ai', 'ollama', 'custom');
    CREATE TYPE alert_condition AS ENUM ('below', 'above', 'equals');
    CREATE TYPE alert_trigger_type AS ENUM ('consecutive_absences', 'low_gpa', 'high_risk_score', 'financial_hold', 'course_drop', 'low_engagement');
    CREATE TYPE alignment_level AS ENUM ('low', 'medium', 'high', 'critical');
    CREATE TYPE allocation_status AS ENUM ('active', 'checked_out', 'suspended');
    CREATE TYPE api_key_status AS ENUM ('active', 'revoked', 'expired', 'suspended');
    CREATE TYPE api_key_tier AS ENUM ('free', 'basic', 'pro', 'enterprise');
    CREATE TYPE api_permission AS ENUM ('read_only', 'read_write', 'admin');
    CREATE TYPE application_status AS ENUM ('submitted', 'under_review', 'shortlisted', 'interviewed', 'offered', 'rejected', 'hired');
    CREATE TYPE approval_status AS ENUM ('pending', 'approved', 'rejected', 'delegated', 'timed_out');
    CREATE TYPE approval_type AS ENUM ('single', 'parallel', 'sequential');
    CREATE TYPE assessment_type AS ENUM ('quiz', 'assignment', 'midterm', 'final', 'project', 'lab', 'presentation', 'survey', 'exit_interview');
    CREATE TYPE asset_maintenance_status AS ENUM ('reported', 'assigned', 'in_progress', 'completed', 'cancelled');
    CREATE TYPE asset_status AS ENUM ('active', 'in_maintenance', 'transferred', 'disposed', 'written_off');
    CREATE TYPE attendance_method AS ENUM ('biometric', 'face_recognition', 'rfid', 'mobile_gps', 'manual');
    CREATE TYPE attendance_status AS ENUM ('present', 'absent', 'late', 'excused');
    CREATE TYPE audit_finding_severity AS ENUM ('observation', 'minor_nc', 'major_nc', 'critical');
    CREATE TYPE audit_finding_status AS ENUM ('open', 'in_progress', 'resolved', 'closed', 'accepted');
    CREATE TYPE audit_status AS ENUM ('satisfied', 'in_progress', 'not_started', 'waived', 'failed');
    CREATE TYPE auth_provider AS ENUM ('local', 'saml', 'oidc', 'azure_ad', 'google', 'ldap');
    CREATE TYPE backup_retention_tier AS ENUM ('hot', 'cold');
    CREATE TYPE backup_status AS ENUM ('running', 'completed', 'failed', 'verified');
    CREATE TYPE backup_type AS ENUM ('full', 'incremental', 'wal_archive', 'file_storage');
    CREATE TYPE benchmark_status AS ENUM ('running', 'completed', 'failed');
    CREATE TYPE benchmark_tier AS ENUM ('tier1_1k', 'tier2_10k', 'tier3_100k', 'tier4_1m');
    CREATE TYPE blooms_level AS ENUM ('remember', 'understand', 'apply', 'analyze', 'evaluate', 'create');
    CREATE TYPE book_status AS ENUM ('available', 'checked_out', 'reserved', 'under_maintenance', 'lost', 'damaged', 'withdrawn');
    CREATE TYPE booking_status AS ENUM ('pending', 'approved', 'rejected', 'cancelled', 'completed');
    CREATE TYPE budget_status AS ENUM ('active', 'exhausted', 'closed');
    CREATE TYPE calendar_type AS ENUM ('semester', 'trimester', 'quarter');
    CREATE TYPE calibration_status AS ENUM ('scheduled', 'completed', 'overdue');
    CREATE TYPE call_direction AS ENUM ('inbound', 'outbound');
    CREATE TYPE call_disposition AS ENUM ('interested', 'callback', 'not_interested', 'no_answer', 'wrong_number', 'converted');
    CREATE TYPE campaign_status AS ENUM ('draft', 'scheduled', 'active', 'completed', 'cancelled');
    CREATE TYPE campaign_type AS ENUM ('email', 'sms', 'social_media', 'print', 'event');
    CREATE TYPE campus_status AS ENUM ('active', 'inactive', 'under_construction');
    CREATE TYPE cap_status AS ENUM ('planned', 'in_progress', 'completed', 'overdue', 'cancelled');
    CREATE TYPE career_event_status AS ENUM ('upcoming', 'in_progress', 'completed', 'cancelled');
    CREATE TYPE career_event_type AS ENUM ('job_fair', 'campus_drive', 'webinar', 'workshop', 'networking');
    CREATE TYPE career_goal_status AS ENUM ('active', 'achieved', 'abandoned');
    CREATE TYPE cip_status AS ENUM ('planned', 'in_progress', 'completed', 'cancelled');
    CREATE TYPE clearance_status AS ENUM ('pending', 'cleared', 'rejected');
    CREATE TYPE committee_role AS ENUM ('chair', 'co_chair', 'secretary', 'member', 'external_member');
    CREATE TYPE compliance_framework_status AS ENUM ('active', 'under_review', 'archived');
    CREATE TYPE condition_operator AS ENUM ('equals', 'not_equals', 'greater_than', 'less_than', 'contains', 'is_empty', 'is_not_empty');
    CREATE TYPE config_entry_status AS ENUM ('active', 'deprecated', 'draft');
    CREATE TYPE config_environment AS ENUM ('development', 'staging', 'production');
    CREATE TYPE configuration_domain AS ENUM ('academic', 'financial', 'hr', 'branding', 'communication', 'identity', 'feature_toggle', 'system');
    CREATE TYPE control_review_frequency AS ENUM ('monthly', 'quarterly', 'bi_annually', 'annually');
    CREATE TYPE control_status AS ENUM ('compliant', 'non_compliant', 'in_progress', 'not_applicable');
    CREATE TYPE copy_status AS ENUM ('available', 'issued', 'reserved', 'under_repair', 'lost', 'damaged');
    CREATE TYPE country_pack_status AS ENUM ('active', 'inactive', 'deprecated');
    CREATE TYPE curriculum_approval_status AS ENUM ('draft', 'department_review', 'board_review', 'approved', 'rejected');
    CREATE TYPE curriculum_version AS ENUM ('v2020', 'v2021', 'v2022', 'v2023', 'v2024', 'v2025');
    CREATE TYPE dashboard_type AS ENUM ('executive', 'department', 'campus', 'custom');
    CREATE TYPE data_category AS ENUM ('student_records', 'employee_records', 'financial_records', 'health_records', 'email_communications', 'system_logs', 'research_data', 'admission_records', 'legal_documents', 'other');
    CREATE TYPE day_of_week AS ENUM ('monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday');
    CREATE TYPE defaulter_status AS ENUM ('pending', 'notified', 'rtp_attempted', 'cleared', 'escalated');
    CREATE TYPE degree_requirement_category AS ENUM ('core', 'elective', 'general_education', 'thesis', 'internship', 'lab');
    CREATE TYPE deliverable_status AS ENUM ('pending', 'submitted', 'reviewed', 'accepted', 'rejected');
    CREATE TYPE deliverable_type AS ENUM ('report', 'dataset', 'software', 'prototype', 'presentation', 'other');
    CREATE TYPE delivery_status AS ENUM ('queued', 'sent', 'delivered', 'opened', 'failed', 'bounced');
    CREATE TYPE dependency_type AS ENUM ('data', 'event', 'api');
    CREATE TYPE depreciation_method AS ENUM ('straight_line', 'reducing_balance');
    CREATE TYPE designation AS ENUM ('professor', 'associate_professor', 'assistant_professor', 'lecturer', 'teaching_assistant', 'dean', 'registrar', 'administrator');
    CREATE TYPE device_type AS ENUM ('desktop', 'mobile', 'tablet', 'api', 'bot');
    CREATE TYPE diagnostic_severity AS ENUM ('info', 'warning', 'critical');
    CREATE TYPE discipline_severity AS ENUM ('minor', 'moderate', 'major', 'critical');
    CREATE TYPE discipline_status AS ENUM ('reported', 'under_investigation', 'resolved', 'appealed');
    CREATE TYPE disposal_method AS ENUM ('write_off', 'auction', 'scrap', 'donation', 'sale');
    CREATE TYPE disposal_status AS ENUM ('pending', 'approved', 'rejected', 'completed');
    CREATE TYPE drive_status AS ENUM ('scheduled', 'in_progress', 'completed', 'cancelled');
    CREATE TYPE drive_type AS ENUM ('on_campus', 'virtual', 'hybrid');
    CREATE TYPE driver_status AS ENUM ('active', 'on_leave', 'suspended', 'terminated');
    CREATE TYPE emergency_alert_severity AS ENUM ('low', 'medium', 'high', 'critical');
    CREATE TYPE emergency_alert_type AS ENUM ('fire', 'earthquake', 'lockdown', 'medical', 'weather', 'other');
    CREATE TYPE emergency_contact_type AS ENUM ('fire_department', 'police', 'ambulance', 'hospital', 'internal_security', 'other');
    CREATE TYPE employee_status AS ENUM ('active', 'on_leave', 'suspended', 'terminated', 'resigned', 'retired');
    CREATE TYPE employee_type AS ENUM ('permanent', 'contract', 'visiting', 'adjunct', 'temporary');
    CREATE TYPE employer_status AS ENUM ('pending', 'verified', 'rejected', 'suspended');
    CREATE TYPE energy_reading_type AS ENUM ('electricity', 'water', 'gas', 'solar');
    CREATE TYPE enrollment_status AS ENUM ('active', 'inactive', 'graduated', 'suspended', 'withdrawn');
    CREATE TYPE entry_type AS ENUM ('vehicle', 'pedestrian', 'visitor');
    CREATE TYPE equipment_status AS ENUM ('available', 'in_use', 'under_maintenance', 'out_of_order', 'retired');
    CREATE TYPE escalation_level AS ENUM ('level1', 'level2', 'level3', 'management');
    CREATE TYPE ethics_review_status AS ENUM ('submitted', 'under_review', 'amendments_requested', 'approved', 'rejected');
    CREATE TYPE etl_job_status AS ENUM ('idle', 'running', 'completed', 'failed');
    CREATE TYPE event_status AS ENUM ('published', 'processed', 'failed', 'replayed');
    CREATE TYPE exam_type AS ENUM ('midterm', 'final', 'quiz', 'assignment', 'project');
    CREATE TYPE export_format AS ENUM ('pdf', 'excel', 'csv');
    CREATE TYPE extension_category AS ENUM ('finance', 'academic', 'hr', 'analytics', 'integrations', 'theme', 'payment_gateway');
    CREATE TYPE extension_pricing_model AS ENUM ('free', 'one_time', 'subscription', 'revenue_share');
    CREATE TYPE extension_status AS ENUM ('draft', 'published', 'deprecated', 'suspended');
    CREATE TYPE failover_mode AS ENUM ('automatic', 'manual', 'scheduled');
    CREATE TYPE fee_defaulter_level AS ENUM ('level1_reminder', 'level2_warning', 'level3_final', 'level4_legal');
    CREATE TYPE fee_status AS ENUM ('pending', 'partially_paid', 'paid', 'overdue', 'waived');
    CREATE TYPE finding_severity AS ENUM ('low', 'medium', 'high', 'critical');
    CREATE TYPE finding_type AS ENUM ('strength', 'weakness', 'concern', 'deficiency', 'observation');
    CREATE TYPE fire_equipment_status AS ENUM ('active', 'expired', 'under_maintenance', 'out_of_service');
    CREATE TYPE flag_scope AS ENUM ('global', 'tenant', 'user_group');
    CREATE TYPE flag_type AS ENUM ('module', 'feature', 'beta', 'emergency');
    CREATE TYPE form_field_type AS ENUM ('text', 'text_area', 'number', 'date', 'dropdown', 'radio', 'checkbox', 'file_upload', 'signature', 'table', 'section_header', 'formula', 'email', 'phone');
    CREATE TYPE form_status AS ENUM ('draft', 'published', 'closed', 'archived');
    CREATE TYPE gate_log_type AS ENUM ('entry', 'exit');
    CREATE TYPE grading_scheme AS ENUM ('percentage', 'gpa4', 'gpa5', 'letter');
    CREATE TYPE grant_status AS ENUM ('applied', 'under_review', 'approved', 'rejected', 'active', 'completed', 'closed');
    CREATE TYPE guard_status AS ENUM ('active', 'on_leave', 'suspended', 'terminated');
    CREATE TYPE guardrail_action AS ENUM ('block', 'flag', 'log', 'bypass');
    CREATE TYPE guardrail_severity AS ENUM ('low', 'medium', 'high', 'critical');
    CREATE TYPE hardcoding_severity AS ENUM ('info', 'warning', 'critical');
    CREATE TYPE hardcoding_type AS ENUM ('string', 'number', 'url', 'credential', 'path');
    CREATE TYPE health_score_grade AS ENUM ('a', 'b', 'c', 'd', 'f');
    CREATE TYPE hostel_block_type AS ENUM ('male', 'female', 'mixed');
    CREATE TYPE hostel_discipline_status AS ENUM ('reported', 'under_investigation', 'warning_issued', 'show_cause_issued', 'resolved', 'appealed');
    CREATE TYPE hostel_status AS ENUM ('active', 'checked_out', 'suspended');
    CREATE TYPE incident_severity AS ENUM ('low', 'medium', 'high', 'critical');
    CREATE TYPE incident_status AS ENUM ('reported', 'under_investigation', 'resolved', 'closed', 'escalated');
    CREATE TYPE incident_type AS ENUM ('theft', 'fight', 'accident', 'fire', 'vandalism', 'trespassing', 'harassment', 'other');
    CREATE TYPE inspection_status AS ENUM ('scheduled', 'in_progress', 'completed', 'signed_off');
    CREATE TYPE inspection_type AS ENUM ('move_in', 'periodic', 'move_out', 'complaint');
    CREATE TYPE installation_status AS ENUM ('installed', 'installing', 'failed', 'uninstalled');
    CREATE TYPE integration_status AS ENUM ('up', 'degraded', 'down');
    CREATE TYPE integration_type AS ENUM ('identity_provider', 'payment_gateway', 'communication', 'lms', 'cloud', 'webhook');
    CREATE TYPE intervention_outcome AS ENUM ('ongoing', 'resolved', 'escalated', 'closed');
    CREATE TYPE intervention_type AS ENUM ('academic_support', 'financial_aid', 'counseling', 'tutoring', 'mentoring', 'career_advising', 'other');
    CREATE TYPE interview_status AS ENUM ('scheduled', 'completed', 'cancelled', 'no_show');
    CREATE TYPE interview_type AS ENUM ('on_campus', 'virtual', 'phone');
    CREATE TYPE issue_status AS ENUM ('issued', 'returned', 'overdue', 'lost', 'damaged');
    CREATE TYPE job_category AS ENUM ('report_generation', 'payroll', 'backup', 'data_cleanup', 'data_import', 'notification', 'ai', 'maintenance', 'sync', 'archive');
    CREATE TYPE job_execution_status AS ENUM ('running', 'completed', 'failed', 'cancelled', 'queued');
    CREATE TYPE job_posting_status AS ENUM ('active', 'closed', 'filled', 'draft');
    CREATE TYPE job_type AS ENUM ('full_time', 'part_time', 'internship', 'contract', 'remote');
    CREATE TYPE journal_entry_type AS ENUM ('manual', 'auto_generated', 'reversal', 'adjustment');
    CREATE TYPE lab_status AS ENUM ('active', 'under_maintenance', 'closed');
    CREATE TYPE lead_source AS ENUM ('web_form', 'chatbot', 'walk_in', 'school_visit', 'referral', 'campaign', 'call_center', 'other');
    CREATE TYPE lead_status AS ENUM ('new', 'contacted', 'interested', 'applied', 'enrolled', 'lost');
    CREATE TYPE lead_task_status AS ENUM ('pending', 'completed', 'cancelled');
    CREATE TYPE leave_status AS ENUM ('pending', 'approved_by_hod', 'approved_by_hr', 'rejected', 'cancelled');
    CREATE TYPE leave_type AS ENUM ('annual', 'medical', 'sabbatical', 'casual', 'maternity', 'paternity', 'bereavement', 'unpaid');
    CREATE TYPE legal_hold_status AS ENUM ('active', 'released');
    CREATE TYPE license_edition AS ENUM ('community', 'professional', 'enterprise', 'unlimited');
    CREATE TYPE license_status AS ENUM ('active', 'expired', 'revoked', 'suspended', 'trial');
    CREATE TYPE logical_operator AS ENUM ('and', 'or');
    CREATE TYPE login_event AS ENUM ('login', 'logout', 'login_failed', 'token_refresh', 'session_expired', 'forced_logout', 'password_reset');
    CREATE TYPE login_provider AS ENUM ('microsoft_sso', 'local', 'google_sso', 'api_key', 'mobile_app', 'impersonation');
    CREATE TYPE lost_found_status AS ENUM ('unclaimed', 'claimed', 'disposed');
    CREATE TYPE lost_found_type AS ENUM ('lost', 'found');
    CREATE TYPE maintenance_category AS ENUM ('electrical', 'civil', 'hvac', 'it', 'cleaning', 'security', 'vehicle', 'plumbing', 'other');
    CREATE TYPE maintenance_status AS ENUM ('reported', 'assigned', 'in_progress', 'completed', 'cancelled');
    CREATE TYPE mapping_strength AS ENUM ('low', 'medium', 'high');
    CREATE TYPE meal_type AS ENUM ('breakfast', 'lunch', 'dinner', 'snacks');
    CREATE TYPE meeting_status AS ENUM ('scheduled', 'completed', 'cancelled', 'no_show');
    CREATE TYPE member_status AS ENUM ('active', 'suspended', 'expired', 'blacklisted');
    CREATE TYPE member_type AS ENUM ('student', 'faculty', 'staff', 'external', 'alumni');
    CREATE TYPE message_sender AS ENUM ('visitor', 'bot', 'counselor');
    CREATE TYPE milestone_status AS ENUM ('pending', 'in_progress', 'completed', 'delayed');
    CREATE TYPE misconduct_severity AS ENUM ('minor', 'moderate', 'major', 'critical');
    CREATE TYPE misconduct_status AS ENUM ('reported', 'under_investigation', 'hearing_scheduled', 'resolved', 'appealed');
    CREATE TYPE notification_channel AS ENUM ('email', 'sms', 'in_app');
    CREATE TYPE notification_status AS ENUM ('pending', 'sent', 'failed');
    CREATE TYPE notification_template_channel AS ENUM ('email', 'sms', 'push', 'in_app', 'teams', 'slack', 'whats_app', 'webhook');
    CREATE TYPE occupancy_state AS ENUM ('vacant', 'partially_occupied', 'full', 'reserved', 'under_maintenance');
    CREATE TYPE order_status AS ENUM ('pending', 'approved', 'shipped', 'received', 'cancelled');
    CREATE TYPE patent_status AS ENUM ('filed', 'under_examination', 'granted', 'rejected', 'abandoned');
    CREATE TYPE placement_type AS ENUM ('full_time', 'internship', 'contract');
    CREATE TYPE pm_frequency AS ENUM ('daily', 'weekly', 'monthly', 'quarterly', 'semi_annually', 'annually');
    CREATE TYPE prompt_approval_status AS ENUM ('draft', 'review', 'approved', 'active', 'rejected');
    CREATE TYPE publication_type AS ENUM ('journal_article', 'conference_paper', 'book_chapter', 'book', 'patent', 'technical_report', 'thesis');
    CREATE TYPE queue_status AS ENUM ('healthy', 'backlogged', 'paused', 'failing');
    CREATE TYPE referral_status AS ENUM ('active', 'expired', 'filled');
    CREATE TYPE refresh_frequency AS ENUM ('real_time', 'hourly', 'daily', 'weekly', 'monthly');
    CREATE TYPE registration_status AS ENUM ('open', 'in_progress', 'closed', 'waitlisted');
    CREATE TYPE replica_status AS ENUM ('online', 'lagging', 'offline', 'promoting');
    CREATE TYPE report_category AS ENUM ('finance', 'hr', 'academic', 'research', 'admissions', 'student', 'asset', 'custom');
    CREATE TYPE requirement_type AS ENUM ('minimum_credits', 'course_completion', 'minimum_cgpa', 'course_group', 'elective_credits');
    CREATE TYPE requisition_status AS ENUM ('pending', 'approved', 'issued', 'rejected');
    CREATE TYPE research_member_role AS ENUM ('principal_investigator', 'co_investigator', 'research_assistant', 'research_fellow', 'project_coordinator');
    CREATE TYPE research_project_status AS ENUM ('proposed', 'approved', 'in_progress', 'completed', 'suspended', 'terminated');
    CREATE TYPE reservation_status AS ENUM ('active', 'fulfilled', 'expired', 'cancelled');
    CREATE TYPE review_status AS ENUM ('draft', 'submitted', 'acknowledged', 'appealed');
    CREATE TYPE review_type AS ENUM ('annual', 'probation', 'three_sixty', 'self_assessment');
    CREATE TYPE risk_category AS ENUM ('low', 'moderate', 'high', 'critical');
    CREATE TYPE room_status AS ENUM ('available', 'occupied', 'under_maintenance', 'closed');
    CREATE TYPE room_type AS ENUM ('single', 'double', 'triple', 'dormitory');
    CREATE TYPE route_type AS ENUM ('morning', 'evening', 'special', 'shuttle');
    CREATE TYPE rule_action_type AS ENUM ('set_field', 'trigger_workflow', 'send_notification', 'block_operation', 'assign_value', 'calculate');
    CREATE TYPE rule_domain AS ENUM ('admissions', 'scholarships', 'attendance', 'finance', 'procurement', 'library', 'examinations', 'graduation', 'hr');
    CREATE TYPE rule_match_strategy AS ENUM ('first_match', 'all_matches');
    CREATE TYPE rule_status AS ENUM ('draft', 'active', 'archived');
    CREATE TYPE sanction_type AS ENUM ('warning', 'grade_reduction', 'course_failure', 'suspension', 'expulsion');
    CREATE TYPE schedule_frequency AS ENUM ('daily', 'weekly', 'monthly');
    CREATE TYPE scholarship_status AS ENUM ('active', 'suspended', 'completed', 'terminated');
    CREATE TYPE sensor_status AS ENUM ('online', 'offline', 'error', 'maintenance');
    CREATE TYPE sensor_type AS ENUM ('occupancy_pir', 'temperature', 'humidity', 'energy_meter', 'water_meter', 'air_quality', 'fire_smoke', 'noise', 'light');
    CREATE TYPE service_status AS ENUM ('up', 'degraded', 'down');
    CREATE TYPE session_status AS ENUM ('active', 'expired', 'terminated');
    CREATE TYPE shift_status AS ENUM ('scheduled', 'in_progress', 'completed', 'absent');
    CREATE TYPE ssr_review_status AS ENUM ('pending', 'approved', 'changes_requested');
    CREATE TYPE ssr_section_status AS ENUM ('not_started', 'in_progress', 'completed', 'reviewed');
    CREATE TYPE ssr_status AS ENUM ('draft', 'in_review', 'approved', 'submitted', 'rejected');
    CREATE TYPE submission_status AS ENUM ('draft', 'submitted', 'under_review', 'approved', 'rejected');
    CREATE TYPE tax_type AS ENUM ('income_tax', 'sales_tax', 'vat', 'gst', 'zakat', 'sst');
    CREATE TYPE text_direction AS ENUM ('ltr', 'rtl');
    CREATE TYPE ticket_priority AS ENUM ('critical', 'high', 'medium', 'low');
    CREATE TYPE ticket_status AS ENUM ('open', 'assigned', 'in_progress', 'resolved', 'closed', 'reopened');
    CREATE TYPE training_status AS ENUM ('assigned', 'in_progress', 'completed', 'overdue');
    CREATE TYPE transfer_request_status AS ENUM ('draft', 'submitted', 'source_approved', 'destination_approved', 'completed', 'rejected');
    CREATE TYPE transfer_status AS ENUM ('requested', 'approved', 'rejected', 'completed');
    CREATE TYPE transport_status AS ENUM ('active', 'inactive', 'suspended');
    CREATE TYPE travel_status AS ENUM ('requested', 'approved', 'rejected', 'completed', 'reimbursed');
    CREATE TYPE tunnel_status AS ENUM ('connected', 'disconnected', 'error');
    CREATE TYPE update_channel AS ENUM ('stable', 'beta', 'lts');
    CREATE TYPE update_status AS ENUM ('available', 'downloading', 'installed', 'rolled_back', 'failed');
    CREATE TYPE user_status AS ENUM ('active', 'inactive', 'suspended', 'pending_activation');
    CREATE TYPE utility_type AS ENUM ('electricity', 'water', 'gas');
    CREATE TYPE vehicle_maintenance_status AS ENUM ('reported', 'assigned', 'in_progress', 'completed', 'cancelled');
    CREATE TYPE vehicle_pass_status AS ENUM ('active', 'expired', 'suspended');
    CREATE TYPE vehicle_pass_type AS ENUM ('student', 'faculty', 'staff', 'visitor', 'daily', 'event');
    CREATE TYPE vehicle_status AS ENUM ('active', 'in_maintenance', 'out_of_service', 'retired');
    CREATE TYPE verification_cycle_status AS ENUM ('planned', 'in_progress', 'completed', 'cancelled');
    CREATE TYPE verification_status AS ENUM ('pending', 'verified', 'not_found', 'discrepancy');
    CREATE TYPE visit_status AS ENUM ('scheduled', 'in_progress', 'completed', 'cancelled');
    CREATE TYPE visitor_status AS ENUM ('registered', 'checked_in', 'checked_out', 'blacklisted');
    CREATE TYPE visitor_type AS ENUM ('walk_in', 'pre_approved', 'vip', 'contractor');
    CREATE TYPE visualization_type AS ENUM ('bar_chart', 'line_chart', 'pie_chart', 'scatter_plot', 'heatmap', 'funnel', 'gauge', 'treemap', 'geographic_map', 'kpi_card', 'table');
    CREATE TYPE webhook_status AS ENUM ('active', 'failed', 'disabled');
    CREATE TYPE work_order_status AS ENUM ('assigned', 'in_progress', 'completed', 'signed_off', 'cancelled');
    CREATE TYPE workflow_definition_status AS ENUM ('draft', 'published', 'deprecated');
    CREATE TYPE workflow_instance_status AS ENUM ('running', 'completed', 'failed', 'cancelled', 'suspended');
    CREATE TYPE workflow_node_type AS ENUM ('start', 'action', 'condition', 'loop', 'wait', 'approval', 'notification', 'end');
    CREATE TYPE workflow_step_status AS ENUM ('pending', 'in_progress', 'completed', 'skipped', 'failed', 'awaiting_approval');
    CREATE TYPE workflow_trigger_type AS ENUM ('record_created', 'record_updated', 'schedule', 'manual', 'webhook', 'form_submission');
    CREATE TYPE workload_unit_type AS ENUM ('lecture', 'lab', 'tutorial', 'supervision', 'research', 'administrative');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AcademicYears" (
        "Id" uuid NOT NULL,
        "Name" character varying(50) NOT NULL,
        "StartDate" date NOT NULL,
        "EndDate" date NOT NULL,
        "IsCurrent" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AcademicYears" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AccessAuditLogs" (
        "Id" uuid NOT NULL,
        "SourceIp" character varying(100),
        "RequestPath" character varying(500),
        "StatusCode" integer NOT NULL,
        "IsBlocked" boolean NOT NULL,
        "RecordedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AccessAuditLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AccreditationBodies" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Description" character varying(1000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AccreditationBodies" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AccreditationCriterionMappings" (
        "Id" uuid NOT NULL,
        "AccreditationCriterionId" uuid NOT NULL,
        "CourseId" uuid,
        "ProgramOutcomeId" uuid,
        "MappingNotes" character varying(1000),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AccreditationCriterionMappings" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AccreditationReports" (
        "Id" uuid NOT NULL,
        "Title" character varying(500) NOT NULL,
        "AccreditationBody" character varying(100),
        "SubmittedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AccreditationReports" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ActionItems" (
        "Id" uuid NOT NULL,
        "CommitteeMeetingId" uuid NOT NULL,
        "Title" character varying(500) NOT NULL,
        "Status" integer NOT NULL,
        "AssignedToUserId" uuid,
        "DueDate" timestamp with time zone,
        "CompletedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ActionItems" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ActivityParticipations" (
        "Id" uuid NOT NULL,
        "ActivityId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "ParticipationRole" character varying(50) NOT NULL,
        "ParticipationStatus" character varying(20) NOT NULL,
        "PointsEarned" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ActivityParticipations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AdmissionApplications" (
        "Id" uuid NOT NULL,
        "EnquiryLeadId" uuid,
        "StudentName" character varying(200) NOT NULL,
        "Email" character varying(200) NOT NULL,
        "Phone" character varying(20) NOT NULL,
        "Cnic" character varying(15) NOT NULL,
        "DateOfBirth" date NOT NULL,
        "Gender" character varying(20),
        "ProgramId" uuid NOT NULL,
        "QuotaType" character varying(50) NOT NULL,
        "MatricPercentage" numeric(5,2),
        "FscPercentage" numeric(5,2),
        "EntryTestScore" integer,
        "IbccEquivalenceApplied" boolean NOT NULL,
        "ApplicationStatus" character varying(20) NOT NULL,
        "SubmittedAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AdmissionApplications" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AdmissionLeads" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Email" character varying(300),
        "Phone" character varying(50),
        "Source" integer NOT NULL,
        "Status" integer NOT NULL,
        "ProgramInterest" character varying(200),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AdmissionLeads" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AdvisorAssignments" (
        "Id" uuid NOT NULL,
        "FacultyId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "AssignedAt" timestamp with time zone NOT NULL,
        "IsActive" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AdvisorAssignments" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AgenticGradeRuns" (
        "Id" uuid NOT NULL,
        "ScanId" uuid NOT NULL,
        "SuggestedMarks" numeric(5,2) NOT NULL,
        "Confidence" numeric(4,3) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "AgentTraceJson" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AgenticGradeRuns" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AIFeedback" (
        "Id" uuid NOT NULL,
        "Domain" character varying(100) NOT NULL,
        "PromptId" uuid,
        "Rating" integer NOT NULL,
        "IsThumbsUp" boolean NOT NULL,
        "Comment" character varying(2000),
        "UserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AIFeedback" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AIModels" (
        "Id" uuid NOT NULL,
        "ProviderId" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Version" character varying(50),
        "ContextWindow" integer NOT NULL,
        "CostPerInputToken" numeric(12,6) NOT NULL,
        "CostPerOutputToken" numeric(12,6) NOT NULL,
        "Capabilities" character varying(500),
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AIModels" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AIProviders" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "ProviderType" integer NOT NULL,
        "BaseUrl" character varying(500),
        "DefaultModel" character varying(200),
        "IsActive" boolean NOT NULL,
        "IsFallbackOnly" boolean NOT NULL,
        "TimeoutSeconds" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AIProviders" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AITokenUsage" (
        "Id" uuid NOT NULL,
        "ModelId" uuid NOT NULL,
        "Domain" character varying(100) NOT NULL,
        "InputTokens" integer NOT NULL,
        "OutputTokens" integer NOT NULL,
        "LatencyMs" integer NOT NULL,
        "IsSuccess" boolean NOT NULL,
        "ErrorMessage" character varying(2000),
        "RecordedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AITokenUsage" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AlertRules" (
        "Id" uuid NOT NULL,
        "RuleName" character varying(100) NOT NULL,
        "TriggerType" integer NOT NULL,
        "Threshold" numeric(18,4) NOT NULL,
        "IsActive" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AlertRules" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AlumniDonations" (
        "Id" uuid NOT NULL,
        "AlumniProfileId" uuid NOT NULL,
        "DonationAmount" numeric(18,2) NOT NULL,
        "DonationDate" timestamp with time zone NOT NULL,
        "DonationType" character varying(50) NOT NULL,
        "Purpose" character varying(200) NOT NULL,
        "ReceiptNumber" character varying(50) NOT NULL,
        "PaymentMethod" character varying(50),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AlumniDonations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AlumniProfiles" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "GraduationYear" integer NOT NULL,
        "AlumniId" character varying(50) NOT NULL,
        "CurrentCompany" character varying(200),
        "CurrentDesignation" character varying(100),
        "CurrentLocation" character varying(200),
        "Bio" character varying(4000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AlumniProfiles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AlumniReferrals" (
        "Id" uuid NOT NULL,
        "AlumniUserId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "Notes" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AlumniReferrals" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AnonymousFeedbackSubmissions" (
        "Id" uuid NOT NULL,
        "UniversityId" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "FacultyId" uuid NOT NULL,
        "Rating" integer NOT NULL,
        "SentimentScore" numeric(5,2) NOT NULL,
        "EncryptedPayload" text NOT NULL,
        "ThemesCsv" character varying(300) NOT NULL,
        "SubmittedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AnonymousFeedbackSubmissions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ApiEndpoints" (
        "Id" uuid NOT NULL,
        "Method" character varying(20) NOT NULL,
        "Path" character varying(500) NOT NULL,
        "Module" character varying(100),
        "AuthRequired" boolean NOT NULL,
        "Version" character varying(50),
        "RegisteredAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ApiEndpoints" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ApiKeys" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "KeyHash" character varying(100) NOT NULL,
        "KeyPrefix" character varying(20) NOT NULL,
        "Tier" integer NOT NULL,
        "Status" integer NOT NULL,
        "Permission" integer NOT NULL,
        "AllowedDomains" character varying(500),
        "OwnerName" character varying(200) NOT NULL,
        "OwnerEmail" character varying(300),
        "RateLimitPerMinute" integer NOT NULL,
        "RateLimitPerDay" integer NOT NULL,
        "TotalRequests" bigint NOT NULL,
        "ExpiresAt" timestamp with time zone,
        "LastUsedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "RevokedAt" timestamp with time zone,
        "RevocationReason" character varying(500),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ApiKeys" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AssessmentSchemes" (
        "Id" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "SemesterId" uuid NOT NULL,
        "ExamControllerId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AssessmentSchemes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AssetCategories" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Code" character varying(50),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AssetCategories" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AssetDisposals" (
        "Id" uuid NOT NULL,
        "AssetId" uuid NOT NULL,
        "Method" integer NOT NULL,
        "Status" integer NOT NULL,
        "SaleValue" numeric(18,2),
        "DisposalDate" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AssetDisposals" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AssetMaintenances" (
        "Id" uuid NOT NULL,
        "AssetId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "Description" character varying(500),
        "Cost" numeric(18,2) NOT NULL,
        "ScheduledDate" timestamp with time zone,
        "CompletedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AssetMaintenances" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AssetTransfers" (
        "Id" uuid NOT NULL,
        "AssetId" uuid NOT NULL,
        "FromLocationId" uuid NOT NULL,
        "ToLocationId" uuid NOT NULL,
        "TransferredByUserId" uuid NOT NULL,
        "TransferredAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AssetTransfers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AttainmentThresholds" (
        "Id" uuid NOT NULL,
        "ProgramOutcomeId" uuid NOT NULL,
        "Threshold" numeric NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AttainmentThresholds" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AttendanceRecords" (
        "Id" uuid NOT NULL,
        "EmployeeId" uuid NOT NULL,
        "Method" integer NOT NULL,
        "CheckIn" timestamp with time zone NOT NULL,
        "CheckOut" timestamp with time zone,
        "Status" integer NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AttendanceRecords" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AuditLogs" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Action" character varying(50) NOT NULL,
        "ResourceType" character varying(100) NOT NULL,
        "ResourceId" uuid NOT NULL,
        "Details" character varying(2000),
        "Success" boolean NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "IpAddress" character varying(45),
        CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "BackupConfigurations" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "BackupType" integer NOT NULL,
        "CronExpression" character varying(100) NOT NULL,
        "IsEnabled" boolean NOT NULL,
        "RetentionDaysHot" integer NOT NULL,
        "RetentionDaysCold" integer NOT NULL,
        "EncryptBackup" boolean NOT NULL,
        "StoragePath" character varying(500) NOT NULL,
        "DatabaseName" character varying(100),
        "VerifyAfterBackup" boolean NOT NULL,
        "LastRunAt" timestamp with time zone,
        "NextRunAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_BackupConfigurations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "BenchmarkRuns" (
        "Id" uuid NOT NULL,
        "ScenarioName" character varying(200) NOT NULL,
        "Tier" integer NOT NULL,
        "Status" integer NOT NULL,
        "StartedAt" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        "RequestsPerSecond" double precision,
        "P95LatencyMs" double precision,
        "ErrorRate" double precision,
        "ResultsJson" jsonb NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_BenchmarkRuns" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "BookIssues" (
        "Id" uuid NOT NULL,
        "LibraryMemberId" uuid NOT NULL,
        "BookCopyId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "IssueDate" timestamp with time zone NOT NULL,
        "DueDate" timestamp with time zone NOT NULL,
        "ReturnDate" timestamp with time zone,
        "FineAmount" numeric(18,2) NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_BookIssues" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "BookOrders" (
        "Id" uuid NOT NULL,
        "LibraryVendorId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "OrderDate" timestamp with time zone NOT NULL,
        "DeliveryDate" timestamp with time zone,
        "TotalAmount" numeric(18,2) NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_BookOrders" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "BookReservations" (
        "Id" uuid NOT NULL,
        "LibraryMemberId" uuid NOT NULL,
        "LibraryBookId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "ReservedAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_BookReservations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "BusAttendances" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "TransportRouteId" uuid NOT NULL,
        "AttendanceDate" timestamp with time zone NOT NULL,
        "IsPresent" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_BusAttendances" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CallLogs" (
        "Id" uuid NOT NULL,
        "AdmissionLeadId" uuid NOT NULL,
        "Direction" integer NOT NULL,
        "Disposition" integer NOT NULL,
        "DurationSeconds" integer NOT NULL,
        "Notes" character varying(1000),
        "CallDate" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_CallLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CampusDrives" (
        "Id" uuid NOT NULL,
        "EmployerId" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "DriveType" integer NOT NULL,
        "Status" integer NOT NULL,
        "ScheduledDate" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_CampusDrives" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Campuses" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "CampusCode" character varying(50) NOT NULL,
        "Address" character varying(500) NOT NULL,
        "City" character varying(100),
        "Country" character varying(100),
        "Phone" character varying(50),
        "Email" character varying(300),
        "Status" integer NOT NULL,
        "CalendarType" integer NOT NULL,
        "GradingScheme" integer NOT NULL,
        "Timezone" character varying(50),
        "Language" character varying(10),
        "IsMain" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Campuses" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CareerEvents" (
        "Id" uuid NOT NULL,
        "Title" character varying(200) NOT NULL,
        "EventType" integer NOT NULL,
        "Status" integer NOT NULL,
        "EventDate" timestamp with time zone NOT NULL,
        "Location" character varying(200),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_CareerEvents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CareerGoals" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "Goal" character varying(500) NOT NULL,
        "Status" integer NOT NULL,
        "TargetDate" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_CareerGoals" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ChartOfAccounts" (
        "Id" uuid NOT NULL,
        "AccountCode" character varying(50) NOT NULL,
        "AccountName" character varying(200) NOT NULL,
        "AccountType" integer NOT NULL,
        "ParentAccountId" uuid,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ChartOfAccounts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ChatbotConversations" (
        "Id" uuid NOT NULL,
        "LeadId" uuid,
        "StartedAt" timestamp with time zone NOT NULL,
        "EndedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ChatbotConversations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CisUploads" (
        "Id" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "FacultyId" uuid NOT NULL,
        "CisFilePath" text NOT NULL,
        "UploadDate" timestamp with time zone NOT NULL,
        "ProcessingStatus" character varying(20) NOT NULL,
        "ExtractedData" text,
        "HumanReviewStatus" character varying(20) NOT NULL,
        "ReviewedBy" uuid,
        "ReviewedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_CisUploads" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CLOAssessmentMappings" (
        "Id" uuid NOT NULL,
        "CourseLearningOutcomeId" uuid NOT NULL,
        "AssessmentId" uuid NOT NULL,
        "AssessmentType" integer NOT NULL,
        "Weightage" numeric NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_CLOAssessmentMappings" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CLOAttainments" (
        "Id" uuid NOT NULL,
        "CourseLearningOutcomeId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "AttainmentLevel" numeric NOT NULL,
        "Semester" character varying(50),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_CLOAttainments" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ComplianceFrameworks" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Description" character varying(2000) NOT NULL,
        "Version" character varying(20) NOT NULL,
        "Status" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "LastReviewedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ComplianceFrameworks" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ConferenceTravels" (
        "Id" uuid NOT NULL,
        "ConferenceName" character varying(300) NOT NULL,
        "Location" character varying(200),
        "Status" integer NOT NULL,
        "TravelDateFrom" timestamp with time zone NOT NULL,
        "TravelDateTo" timestamp with time zone NOT NULL,
        "ApprovedBudget" numeric(18,2) NOT NULL,
        "FacultyId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ConferenceTravels" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ConfigurationEntries" (
        "Id" uuid NOT NULL,
        "Key" character varying(200) NOT NULL,
        "Domain" integer NOT NULL,
        "ValueJson" jsonb NOT NULL,
        "SchemaUrl" character varying(100),
        "Description" character varying(2000),
        "Status" integer NOT NULL,
        "Environment" integer NOT NULL,
        "CurrentVersion" integer NOT NULL,
        "UpdatedByUserId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ConfigurationEntries" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ContinuousImprovementPlans" (
        "Id" uuid NOT NULL,
        "Title" character varying(500) NOT NULL,
        "Status" integer NOT NULL,
        "TargetDate" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ContinuousImprovementPlans" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CostCenters" (
        "Id" uuid NOT NULL,
        "Code" character varying(20) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "DepartmentId" uuid,
        "BudgetAllocation" numeric(15,2) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_CostCenters" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CountryPacks" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "CountryCode" character varying(10) NOT NULL,
        "DefaultLanguage" character varying(20) NOT NULL,
        "TextDirection" integer NOT NULL,
        "DefaultCurrency" character varying(20) NOT NULL,
        "Timezone" character varying(50),
        "DefaultCalendarType" integer NOT NULL,
        "DefaultGradingScheme" integer NOT NULL,
        "Version" character varying(20) NOT NULL,
        "Status" integer NOT NULL,
        "IsInstalled" boolean NOT NULL,
        "InstalledAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_CountryPacks" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CourseGrades" (
        "Id" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "TotalMarksObtained" numeric(10,2) NOT NULL,
        "PercentageObtained" numeric(5,2) NOT NULL,
        "LetterGrade" character varying(2),
        "GradePoints" numeric(3,2) NOT NULL,
        "IsPassing" boolean NOT NULL,
        "ResultStatus" character varying(20) NOT NULL,
        "CalculatedAt" timestamp with time zone,
        "PublishedAt" timestamp with time zone,
        CONSTRAINT "PK_CourseGrades" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CourseLearningOutcomes" (
        "Id" uuid NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Description" character varying(1000) NOT NULL,
        "CourseId" uuid NOT NULL,
        "BloomsLevel" integer NOT NULL,
        "SortOrder" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_CourseLearningOutcomes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CourseReEnrollments" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "SourceSemesterId" uuid NOT NULL,
        "TargetSemesterId" uuid NOT NULL,
        "FeeWaived" boolean NOT NULL,
        "Status" character varying(20) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_CourseReEnrollments" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Currencies" (
        "Id" uuid NOT NULL,
        "Code" character varying(10) NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Symbol" character varying(10) NOT NULL,
        "DecimalPlaces" integer NOT NULL,
        "DecimalSeparator" character varying(5) NOT NULL,
        "ThousandsSeparator" character varying(5) NOT NULL,
        "ExchangeRateToBase" numeric(18,6) NOT NULL,
        "ExchangeRateUpdatedAt" timestamp with time zone NOT NULL,
        "IsBaseCurrency" boolean NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Currencies" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CurriculumChangeHistory" (
        "Id" uuid NOT NULL,
        "CurriculumVersionId" uuid NOT NULL,
        "ChangeType" character varying(40) NOT NULL,
        "CourseId" uuid,
        "BeforeValueJson" jsonb,
        "AfterValueJson" jsonb,
        "ChangedBy" uuid NOT NULL,
        "ChangedAt" timestamp with time zone NOT NULL,
        "Reason" character varying(1000),
        CONSTRAINT "PK_CurriculumChangeHistory" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CurriculumProposals" (
        "Id" uuid NOT NULL,
        "Title" character varying(300) NOT NULL,
        "Description" character varying(5000),
        "Status" integer NOT NULL,
        "ProposedByUserId" uuid NOT NULL,
        "ChangesJson" jsonb NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ApprovedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_CurriculumProposals" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CurriculumVersions" (
        "Id" uuid NOT NULL,
        "ProgramId" uuid NOT NULL,
        "VersionNumber" integer NOT NULL,
        "YearEffective" integer NOT NULL,
        "Status" character varying(20) NOT NULL,
        "CreatedBy" uuid NOT NULL,
        "ApprovedBy" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ApprovedAt" timestamp with time zone,
        "Changelog" character varying(4000),
        CONSTRAINT "PK_CurriculumVersions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Dashboards" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "DashboardType" integer NOT NULL,
        "CampusId" uuid,
        "IsPublic" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Dashboards" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "DataWarehouseTables" (
        "Id" uuid NOT NULL,
        "TableName" character varying(200) NOT NULL,
        "SourceModule" character varying(100),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_DataWarehouseTables" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "DecisionTables" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Domain" integer NOT NULL,
        "InputColumnsJson" jsonb NOT NULL,
        "OutputColumnsJson" jsonb NOT NULL,
        "RowsJson" jsonb NOT NULL,
        "Status" integer NOT NULL,
        "Priority" integer NOT NULL,
        "EffectiveFrom" timestamp with time zone,
        "EffectiveTo" timestamp with time zone,
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_DecisionTables" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "DegreePrograms" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Code" character varying(50) NOT NULL,
        "DegreeType" character varying(50) NOT NULL,
        "DurationYears" integer NOT NULL,
        "MinCreditHours" integer NOT NULL,
        "MinCGPA" numeric NOT NULL,
        "CurriculumVersion" integer NOT NULL,
        "DepartmentId" uuid NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_DegreePrograms" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "DegreeVerifications" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "VerificationToken" character varying(100) NOT NULL,
        "Status" integer NOT NULL,
        "RequestedBy" character varying(300),
        "EmployerName" character varying(100),
        "RequestedAt" timestamp with time zone NOT NULL,
        "VerifiedAt" timestamp with time zone,
        "ExpiresAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_DegreeVerifications" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Departments" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Code" character varying(50),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Departments" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "DiagnosticFindings" (
        "Id" uuid NOT NULL,
        "Title" character varying(200) NOT NULL,
        "Category" character varying(100) NOT NULL,
        "Severity" integer NOT NULL,
        "Description" character varying(2000),
        "Recommendation" character varying(1000),
        "DataJson" jsonb,
        "FoundAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_DiagnosticFindings" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "DomainEvents" (
        "Id" uuid NOT NULL,
        "EventType" character varying(200) NOT NULL,
        "AggregateId" character varying(100) NOT NULL,
        "CorrelationId" character varying(100),
        "CausationId" character varying(100),
        "SourceModule" character varying(100) NOT NULL,
        "PayloadJson" jsonb NOT NULL,
        "Status" integer NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        "ProcessedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_DomainEvents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "DriverIncidents" (
        "Id" uuid NOT NULL,
        "TransportDriverId" uuid NOT NULL,
        "Description" character varying(1000),
        "IncidentDate" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_DriverIncidents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "EmergencyAlerts" (
        "Id" uuid NOT NULL,
        "AlertMessage" character varying(300) NOT NULL,
        "Type" integer NOT NULL,
        "Severity" integer NOT NULL,
        "SentAt" timestamp with time zone NOT NULL,
        "SentByUserId" uuid NOT NULL,
        "RecipientCount" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_EmergencyAlerts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "EmergencyContacts" (
        "Id" uuid NOT NULL,
        "ContactName" character varying(100) NOT NULL,
        "Department" character varying(100) NOT NULL,
        "Phone" character varying(20) NOT NULL,
        "AlternatePhone" character varying(20) NOT NULL,
        "Email" character varying(100) NOT NULL,
        "Type" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_EmergencyContacts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Employees" (
        "Id" uuid NOT NULL,
        "FirstName" character varying(100) NOT NULL,
        "LastName" character varying(100) NOT NULL,
        "EmployeeCode" character varying(50),
        "Email" character varying(300),
        "EmployeeType" integer NOT NULL,
        "Status" integer NOT NULL,
        "Designation" integer NOT NULL,
        "DepartmentId" uuid NOT NULL,
        "DateOfJoining" timestamp with time zone,
        "DateOfLeaving" timestamp with time zone,
        "Salary" numeric(18,2),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Employees" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Employers" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(500),
        "ContactEmail" character varying(300),
        "Status" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Employers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "EthicsProtocols" (
        "Id" uuid NOT NULL,
        "Title" character varying(300) NOT NULL,
        "Status" integer NOT NULL,
        "SubmittedByUserId" uuid NOT NULL,
        "SubmittedAt" timestamp with time zone NOT NULL,
        "ReviewedAt" timestamp with time zone,
        "ReviewComments" character varying(200),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_EthicsProtocols" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ETLJobs" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Status" integer NOT NULL,
        "LastRunAt" timestamp with time zone,
        "RecordsProcessed" integer NOT NULL,
        "CronExpression" character varying(100),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ETLJobs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "EventSchemaVersions" (
        "Id" uuid NOT NULL,
        "EventType" character varying(200) NOT NULL,
        "Version" integer NOT NULL,
        "SchemaJson" jsonb NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_EventSchemaVersions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ExamPaperBatches" (
        "Id" uuid NOT NULL,
        "ExamId" uuid NOT NULL,
        "PaperCount" integer NOT NULL,
        "Status" character varying(40) NOT NULL,
        "PreparedBy" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ExamPaperBatches" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ExamPaperScans" (
        "Id" uuid NOT NULL,
        "BatchId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "QrPayload" character varying(120) NOT NULL,
        "PageCount" integer NOT NULL,
        "BlurDetected" boolean NOT NULL,
        "BlankDetected" boolean NOT NULL,
        "OcrText" text NOT NULL,
        "ScannedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ExamPaperScans" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ExamRegistrations" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "ExamDate" date NOT NULL,
        "ExamTime" time without time zone NOT NULL,
        "ExamHall" character varying(100),
        "SeatNumber" integer,
        "AttendancePercentage" numeric(5,2) NOT NULL,
        "IsAttendanceEligible" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ExamRegistrations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ExportLogs" (
        "Id" uuid NOT NULL,
        "Domain" character varying(100) NOT NULL,
        "Format" character varying(50) NOT NULL,
        "RowCount" integer NOT NULL,
        "FileUrl" character varying(500),
        "ExportedByUserId" uuid NOT NULL,
        "ExportedByName" character varying(200),
        "CreatedAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone,
        "IsEncrypted" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ExportLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Extensions" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(2000) NOT NULL,
        "Version" character varying(50) NOT NULL,
        "Category" integer NOT NULL,
        "Status" integer NOT NULL,
        "PricingModel" integer NOT NULL,
        "Price" numeric(18,2) NOT NULL,
        "IconUrl" character varying(500),
        "ScreenshotsJson" jsonb NOT NULL,
        "ManifestJson" jsonb NOT NULL,
        "DeveloperName" character varying(200) NOT NULL,
        "DeveloperEmail" character varying(300),
        "DeveloperWebsite" character varying(500),
        "PackageChecksum" character varying(100),
        "PackageUrl" character varying(500),
        "DependenciesJson" jsonb NOT NULL,
        "RequiredPermissionsJson" jsonb NOT NULL,
        "DownloadCount" bigint NOT NULL,
        "AverageRating" double precision NOT NULL,
        "ReviewCount" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "PublishedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Extensions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "FacultyCVs" (
        "Id" uuid NOT NULL,
        "FacultyId" uuid NOT NULL,
        "CvJson" jsonb NOT NULL,
        "Version" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_FacultyCVs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "FacultyWorkloads" (
        "Id" uuid NOT NULL,
        "FacultyId" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "UnitType" integer NOT NULL,
        "HoursPerWeek" integer NOT NULL,
        "StudentCount" integer NOT NULL,
        "Schedule" character varying(200),
        "EffectiveFrom" timestamp with time zone NOT NULL,
        "EffectiveTo" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_FacultyWorkloads" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "FeatureFlags" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Description" character varying(500),
        "FlagType" integer NOT NULL,
        "IsEnabled" boolean NOT NULL,
        "Module" character varying(100),
        "TenantOverridesJson" jsonb NOT NULL,
        "RulesJson" jsonb NOT NULL,
        "ActiveFrom" timestamp with time zone,
        "ActiveTo" timestamp with time zone,
        "Tags" character varying(200),
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_FeatureFlags" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "FeeDefaulterRecords" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "AmountDue" numeric(18,2) NOT NULL,
        "Status" integer NOT NULL,
        "Level" integer NOT NULL,
        "NotificationsSent" integer NOT NULL,
        "LastNotifiedAt" timestamp with time zone,
        "RTPAttemptedAt" timestamp with time zone,
        "RTPAttempted" boolean NOT NULL,
        "DueDate" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_FeeDefaulterRecords" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "FeeStructures" (
        "Id" uuid NOT NULL,
        "AcademicYearId" uuid NOT NULL,
        "ProgramId" uuid NOT NULL,
        "Semester" integer NOT NULL,
        "FeeType" character varying(50) NOT NULL,
        "TotalFee" numeric(12,2) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_FeeStructures" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "FinePayments" (
        "Id" uuid NOT NULL,
        "BookIssueId" uuid NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "PaidAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_FinePayments" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "FiscalYears" (
        "Id" uuid NOT NULL,
        "Name" character varying(50) NOT NULL,
        "StartDate" timestamp with time zone NOT NULL,
        "EndDate" timestamp with time zone NOT NULL,
        "IsClosed" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_FiscalYears" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "FormDefinitions" (
        "Id" uuid NOT NULL,
        "Title" character varying(300) NOT NULL,
        "Description" character varying(2000),
        "Status" integer NOT NULL,
        "FieldsJson" jsonb NOT NULL,
        "PagesJson" jsonb NOT NULL,
        "AllowMultipleSubmissions" boolean NOT NULL,
        "AllowDraftSave" boolean NOT NULL,
        "OpenAt" timestamp with time zone,
        "CloseAt" timestamp with time zone,
        "AccessRoles" character varying(500),
        "WorkflowDefinitionId" uuid,
        "IsTemplate" boolean NOT NULL,
        "Category" character varying(100),
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_FormDefinitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "FuelLogs" (
        "Id" uuid NOT NULL,
        "TransportVehicleId" uuid NOT NULL,
        "Litres" numeric(10,2) NOT NULL,
        "Cost" numeric(10,2) NOT NULL,
        "FuelDate" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_FuelLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "GapAnalyses" (
        "Id" uuid NOT NULL,
        "ProgramOutcomeId" uuid NOT NULL,
        "Gap" character varying(2000),
        "Action" character varying(2000),
        "Status" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_GapAnalyses" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "GateLogs" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "Type" integer NOT NULL,
        "LogTime" timestamp with time zone NOT NULL,
        "Purpose" character varying(500),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_GateLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "GradeOverrideRequests" (
        "Id" uuid NOT NULL,
        "UniversityId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "SemesterId" uuid NOT NULL,
        "OriginalGrade" character varying(2),
        "OriginalMarks" integer NOT NULL,
        "OriginalGpa" numeric(3,2) NOT NULL,
        "OverrideGrade" character varying(2),
        "OverrideMarks" integer NOT NULL,
        "OverrideGpa" numeric(3,2) NOT NULL,
        "Reason" character varying(1000) NOT NULL,
        "Justification" character varying(4000),
        "RequestedBy" uuid NOT NULL,
        "ApprovedBy" uuid,
        "ApprovalStatus" character varying(20) NOT NULL,
        "ApprovalThreshold" character varying(20) NOT NULL,
        "RequestedAt" timestamp with time zone NOT NULL,
        "ApprovedAt" timestamp with time zone,
        CONSTRAINT "PK_GradeOverrideRequests" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "GradeScales" (
        "Id" uuid NOT NULL,
        "UniversityId" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_GradeScales" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "GrievanceTickets" (
        "Id" uuid NOT NULL,
        "TicketNumber" character varying(50) NOT NULL,
        "SubmitterId" uuid NOT NULL,
        "Category" character varying(50) NOT NULL,
        "Subject" character varying(200) NOT NULL,
        "Description" character varying(10000) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "AssignedTo" uuid,
        "Priority" character varying(20) NOT NULL,
        "SubmittedAt" timestamp with time zone NOT NULL,
        "ResolvedAt" timestamp with time zone,
        "ResolutionNotes" character varying(5000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_GrievanceTickets" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "GuardrailRules" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Domain" character varying(100) NOT NULL,
        "Action" integer NOT NULL,
        "Severity" integer NOT NULL,
        "Pattern" character varying(1000),
        "Category" character varying(100),
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_GuardrailRules" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "GuardrailViolations" (
        "Id" uuid NOT NULL,
        "RuleId" uuid NOT NULL,
        "Domain" character varying(100) NOT NULL,
        "InputPreview" character varying(5000),
        "OutputPreview" character varying(5000),
        "ActionTaken" integer NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_GuardrailViolations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "HardcodingFindings" (
        "Id" uuid NOT NULL,
        "FilePath" character varying(500) NOT NULL,
        "LineNumber" integer NOT NULL,
        "CodeSnippet" character varying(1000),
        "Type" integer NOT NULL,
        "Severity" integer NOT NULL,
        "Recommendation" character varying(500),
        "IsResolved" boolean NOT NULL,
        "FoundAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_HardcodingFindings" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "HealthScores" (
        "Id" uuid NOT NULL,
        "OverallScore" double precision NOT NULL,
        "PerformanceScore" double precision NOT NULL,
        "SecurityScore" double precision NOT NULL,
        "AvailabilityScore" double precision NOT NULL,
        "ComplianceScore" double precision NOT NULL,
        "StorageScore" double precision NOT NULL,
        "DatabaseScore" double precision NOT NULL,
        "IntegrationScore" double precision NOT NULL,
        "Grade" integer NOT NULL,
        "TopIssuesJson" jsonb NOT NULL,
        "GeneratedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_HealthScores" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "HostelBlocks" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "BlockType" integer NOT NULL,
        "TotalFloors" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_HostelBlocks" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "HostelDisciplines" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "Description" character varying(1000),
        "Status" integer NOT NULL,
        "IncidentDate" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_HostelDisciplines" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "HumanGradeReviews" (
        "Id" uuid NOT NULL,
        "GradeRunId" uuid NOT NULL,
        "ReviewedBy" uuid,
        "FinalMarks" numeric(5,2),
        "Status" character varying(20) NOT NULL,
        "Notes" character varying(1000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "ReviewedAt" timestamp with time zone,
        CONSTRAINT "PK_HumanGradeReviews" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ImportBatches" (
        "Id" uuid NOT NULL,
        "FileName" character varying(300) NOT NULL,
        "Format" character varying(50) NOT NULL,
        "Domain" character varying(100) NOT NULL,
        "ColumnMappingsJson" jsonb NOT NULL,
        "TotalRows" integer NOT NULL,
        "SuccessCount" integer NOT NULL,
        "ErrorCount" integer NOT NULL,
        "Status" text NOT NULL,
        "ErrorReportJson" character varying(5000),
        "UploadedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        "IsRolledBack" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ImportBatches" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "IntegrationIncidents" (
        "Id" uuid NOT NULL,
        "IntegrationId" uuid NOT NULL,
        "StartedAt" timestamp with time zone NOT NULL,
        "ResolvedAt" timestamp with time zone,
        "DurationMinutes" integer NOT NULL,
        "Resolution" character varying(2000),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_IntegrationIncidents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "IntegrationRegistrations" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Type" integer NOT NULL,
        "Endpoint" character varying(500),
        "Status" integer NOT NULL,
        "CheckIntervalSeconds" integer NOT NULL,
        "LastCheckedAt" timestamp with time zone,
        "LastSuccessAt" timestamp with time zone,
        "LastFailureAt" timestamp with time zone,
        "UptimePercentage" double precision NOT NULL,
        "LastError" character varying(2000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_IntegrationRegistrations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Interventions" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "Type" integer NOT NULL,
        "Outcome" integer NOT NULL,
        "Description" character varying(2000),
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Interventions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "InterviewSchedules" (
        "Id" uuid NOT NULL,
        "JobApplicationId" uuid NOT NULL,
        "ScheduledAt" timestamp with time zone NOT NULL,
        "Status" integer NOT NULL,
        "Type" integer NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_InterviewSchedules" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "InterviewSlots" (
        "Id" uuid NOT NULL,
        "JobPostingId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "ScheduledAt" timestamp with time zone NOT NULL,
        "Status" integer NOT NULL,
        "Type" integer NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_InterviewSlots" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "IoTSensors" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "HardwareId" character varying(100),
        "SensorType" integer NOT NULL,
        "Status" integer NOT NULL,
        "BuildingId" uuid,
        "RoomId" uuid,
        "Latitude" numeric,
        "Longitude" numeric,
        "LocationDescription" character varying(500),
        "ConfigJson" jsonb NOT NULL,
        "LastReadingAt" timestamp with time zone,
        "InstalledAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_IoTSensors" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "JobPostings" (
        "Id" uuid NOT NULL,
        "Title" character varying(300) NOT NULL,
        "EmployerId" uuid NOT NULL,
        "JobType" integer NOT NULL,
        "Location" character varying(200),
        "Description" character varying(5000),
        "Status" integer NOT NULL,
        "PostedAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_JobPostings" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "JournalEntries" (
        "Id" uuid NOT NULL,
        "EntryNumber" character varying(50) NOT NULL,
        "EntryType" integer NOT NULL,
        "Description" character varying(1000),
        "EntryDate" timestamp with time zone NOT NULL,
        "TotalDebit" numeric(18,2) NOT NULL,
        "TotalCredit" numeric(18,2) NOT NULL,
        "IsPosted" boolean NOT NULL,
        "PostedByUserId" uuid,
        "PostedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_JournalEntries" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "LabInventories" (
        "Id" uuid NOT NULL,
        "LabId" uuid NOT NULL,
        "ItemName" character varying(200) NOT NULL,
        "Quantity" integer NOT NULL,
        "Value" numeric(18,2),
        "LastVerifiedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_LabInventories" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "LeadTasks" (
        "Id" uuid NOT NULL,
        "AdmissionLeadId" uuid NOT NULL,
        "Title" character varying(500) NOT NULL,
        "Status" integer NOT NULL,
        "AssignedToUserId" uuid,
        "DueDate" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_LeadTasks" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "LeaveApplications" (
        "Id" uuid NOT NULL,
        "EmployeeId" uuid NOT NULL,
        "LeaveType" integer NOT NULL,
        "Status" integer NOT NULL,
        "StartDate" timestamp with time zone NOT NULL,
        "EndDate" timestamp with time zone NOT NULL,
        "Reason" character varying(1000),
        "ApprovedByUserId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_LeaveApplications" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "LibraryBooks" (
        "Id" uuid NOT NULL,
        "Title" character varying(500) NOT NULL,
        "Author" character varying(200),
        "Isbn" character varying(50),
        "Publisher" character varying(200),
        "PublicationYear" integer NOT NULL,
        "Category" character varying(100),
        "TotalCopies" integer NOT NULL,
        "AvailableCopies" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_LibraryBooks" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "LibraryBudgets" (
        "Id" uuid NOT NULL,
        "FiscalYear" integer NOT NULL,
        "TotalBudget" numeric(18,2) NOT NULL,
        "SpentAmount" numeric(18,2) NOT NULL,
        "Status" integer NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_LibraryBudgets" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "LibraryMembers" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "MemberType" integer NOT NULL,
        "Status" integer NOT NULL,
        "MembershipExpiry" timestamp with time zone NOT NULL,
        "MaxBooksAllowed" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_LibraryMembers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "LibraryUsages" (
        "Id" uuid NOT NULL,
        "LibraryMemberId" uuid NOT NULL,
        "EntryTime" timestamp with time zone NOT NULL,
        "ExitTime" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_LibraryUsages" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "LibraryVendors" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Contact" character varying(300),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_LibraryVendors" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "LicenseHeartbeats" (
        "Id" uuid NOT NULL,
        "LicenseId" uuid NOT NULL,
        "InstanceId" character varying(100) NOT NULL,
        "Version" character varying(50),
        "ActiveUsers" integer NOT NULL,
        "HeartbeatAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_LicenseHeartbeats" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Licenses" (
        "Id" uuid NOT NULL,
        "CustomerName" character varying(200) NOT NULL,
        "CustomerEmail" character varying(300),
        "LicenseKey" character varying(100) NOT NULL,
        "Edition" integer NOT NULL,
        "Status" integer NOT NULL,
        "MaxUsers" integer NOT NULL,
        "MaxCampuses" integer NOT NULL,
        "FeatureEntitlementsJson" jsonb NOT NULL,
        "IssuedAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone,
        "RevokedAt" timestamp with time zone,
        "RevocationReason" character varying(500),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Licenses" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "LicenseStates" (
        "Id" uuid NOT NULL,
        "LicenseKey" text NOT NULL,
        "UniversityId" uuid NOT NULL,
        "UniversityName" character varying(200) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "LicenseJson" text NOT NULL,
        "ActivatedAt" timestamp with time zone,
        "LastHeartbeatAt" timestamp with time zone,
        "LastHeartbeatStatus" character varying(20),
        "FailedHeartbeatCount" integer NOT NULL,
        "LockedAt" timestamp with time zone,
        "LockReason" text,
        "OverrideBy" uuid,
        "OverrideReason" text,
        "OverrideUntil" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_LicenseStates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "LmsSyncLogs" (
        "Id" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "SyncType" character varying(20) NOT NULL,
        "SyncStatus" character varying(20) NOT NULL,
        "ErrorMessage" character varying(2000),
        "SyncedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_LmsSyncLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "LoginAuditRecords" (
        "AuditId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "UserId" uuid,
        "UserEmail" character varying(300) NOT NULL,
        "UserRole" character varying(100) NOT NULL,
        "EventType" integer NOT NULL,
        "Provider" integer NOT NULL,
        "IpAddress" character varying(50) NOT NULL,
        "IpCountry" character varying(100),
        "IpCity" character varying(100),
        "GpsLatitude" double precision,
        "GpsLongitude" double precision,
        "UserAgent" character varying(500),
        "DeviceType" integer,
        "OperatingSystem" character varying(100),
        "Browser" character varying(100),
        "SessionId" character varying(200),
        "IsSuccessful" boolean NOT NULL,
        "FailureReason" character varying(200),
        "MicrosoftOid" character varying(200),
        "OccurredAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_LoginAuditRecords" PRIMARY KEY ("AuditId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "LostFoundItems" (
        "Id" uuid NOT NULL,
        "ItemName" character varying(200) NOT NULL,
        "Description" character varying(500) NOT NULL,
        "Type" integer NOT NULL,
        "FoundDate" timestamp with time zone NOT NULL,
        "FoundLocation" character varying(100) NOT NULL,
        "PhotoUrl" character varying(500) NOT NULL,
        "Status" integer NOT NULL,
        "ClaimedByUserId" uuid,
        "ClaimedAt" timestamp with time zone,
        "HandedOverByUserId" uuid,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_LostFoundItems" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "MaintenanceContractors" (
        "Id" uuid NOT NULL,
        "CompanyName" character varying(200) NOT NULL,
        "ContactPerson" character varying(200) NOT NULL,
        "Email" character varying(100) NOT NULL,
        "Phone" character varying(20) NOT NULL,
        "Address" character varying(300) NOT NULL,
        "Specialization" character varying(500) NOT NULL,
        "IsActive" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_MaintenanceContractors" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "MarketingCampaigns" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "CampaignType" integer NOT NULL,
        "Status" integer NOT NULL,
        "StartDate" timestamp with time zone NOT NULL,
        "EndDate" timestamp with time zone,
        "Budget" numeric(18,2) NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_MarketingCampaigns" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "MarkModerations" (
        "Id" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "SemesterId" uuid NOT NULL,
        "Status" character varying(20) NOT NULL,
        "SubmittedBy" uuid NOT NULL,
        "SubmittedAt" timestamp with time zone,
        "ReviewedBy" uuid,
        "ReviewedAt" timestamp with time zone,
        "ReviewComments" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_MarkModerations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "MedicalVisits" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "VisitDate" timestamp with time zone NOT NULL,
        "VisitReasonEncrypted" character varying(4000),
        "MedicalOfficerId" uuid NOT NULL,
        "DiagnosisEncrypted" character varying(4000),
        "TreatmentEncrypted" character varying(4000),
        "RestDays" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_MedicalVisits" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "MentorAssignments" (
        "Id" uuid NOT NULL,
        "MentorId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "AssignedAt" timestamp with time zone NOT NULL,
        "IsActive" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_MentorAssignments" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "MeritLists" (
        "Id" uuid NOT NULL,
        "ProgramId" uuid NOT NULL,
        "SemesterId" uuid NOT NULL,
        "QuotaType" character varying(50) NOT NULL,
        "RoundNumber" integer NOT NULL,
        "TotalSeats" integer NOT NULL,
        "GeneratedAt" timestamp with time zone NOT NULL,
        "GeneratedBy" uuid NOT NULL,
        "Status" character varying(20) NOT NULL,
        "PublishedAt" timestamp with time zone,
        CONSTRAINT "PK_MeritLists" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "MessAttendances" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "MealType" integer NOT NULL,
        "AttendanceDate" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_MessAttendances" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "MessFeedbacks" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "Rating" integer NOT NULL,
        "Comments" character varying(1000),
        "SubmittedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_MessFeedbacks" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "MessMenus" (
        "Id" uuid NOT NULL,
        "MealType" integer NOT NULL,
        "Items" character varying(1000),
        "MenuDate" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_MessMenus" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "MisconductCases" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "Title" character varying(300) NOT NULL,
        "Description" character varying(5000),
        "Severity" integer NOT NULL,
        "Status" integer NOT NULL,
        "ReportedByUserId" uuid NOT NULL,
        "AssignedToUserId" uuid,
        "HearingDate" timestamp with time zone,
        "Sanction" integer,
        "Resolution" character varying(2000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "ResolvedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_MisconductCases" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "MisconductEvidence" (
        "Id" uuid NOT NULL,
        "CaseId" uuid NOT NULL,
        "FileUrl" character varying(500),
        "Description" character varying(1000),
        "UploadedByUserId" uuid NOT NULL,
        "UploadedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_MisconductEvidence" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "NoticeBoardItems" (
        "Id" uuid NOT NULL,
        "Title" character varying(200) NOT NULL,
        "Content" character varying(10000) NOT NULL,
        "PostedBy" uuid NOT NULL,
        "Visibility" character varying(50) NOT NULL,
        "PostedDate" timestamp with time zone NOT NULL,
        "ExpiryDate" timestamp with time zone,
        "IsArchived" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_NoticeBoardItems" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "NotificationDeliveries" (
        "Id" uuid NOT NULL,
        "Channel" integer NOT NULL,
        "Recipient" character varying(500) NOT NULL,
        "Subject" character varying(300) NOT NULL,
        "Module" character varying(100),
        "Status" integer NOT NULL,
        "RetryCount" integer NOT NULL,
        "SentAt" timestamp with time zone,
        "DeliveredAt" timestamp with time zone,
        "OpenedAt" timestamp with time zone,
        "ErrorMessage" character varying(2000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_NotificationDeliveries" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "NotificationPreferences" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "EventType" character varying(100) NOT NULL,
        "EnabledChannelsJson" jsonb NOT NULL,
        "DoNotDisturbFrom" character varying(50),
        "DoNotDisturbTo" character varying(50),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_NotificationPreferences" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "NotificationTemplates" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Channel" integer NOT NULL,
        "Subject" character varying(300) NOT NULL,
        "Body" character varying(10000) NOT NULL,
        "Module" character varying(100),
        "LanguageCode" character varying(50) NOT NULL,
        "Version" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_NotificationTemplates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "OfficialModules" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Category" character varying(100),
        "Version" character varying(50) NOT NULL,
        "Description" character varying(2000),
        "Price" numeric NOT NULL,
        "IsPublished" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_OfficialModules" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "OfflineAttendanceEvents" (
        "Id" uuid NOT NULL,
        "ClientEventId" character varying(100) NOT NULL,
        "CourseId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "MarkedBy" uuid NOT NULL,
        "Status" character varying(20) NOT NULL,
        "WasOfflineAt" timestamp with time zone NOT NULL,
        "ReceivedAt" timestamp with time zone NOT NULL,
        "SyncedAt" timestamp with time zone,
        CONSTRAINT "PK_OfflineAttendanceEvents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "PaperDestructionManifests" (
        "Id" uuid NOT NULL,
        "BatchId" uuid NOT NULL,
        "PaperCount" integer NOT NULL,
        "ManifestHash" character varying(64) NOT NULL,
        "WitnessedBy" uuid NOT NULL,
        "DestroyedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PaperDestructionManifests" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "PayrollRuns" (
        "Id" uuid NOT NULL,
        "Month" date NOT NULL,
        "Status" character varying(20) NOT NULL,
        "TotalEmployees" integer NOT NULL,
        "TotalSalary" numeric(15,2) NOT NULL,
        "RunDate" timestamp with time zone NOT NULL,
        "RunBy" uuid,
        CONSTRAINT "PK_PayrollRuns" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "PerformanceReviews" (
        "Id" uuid NOT NULL,
        "EmployeeId" uuid NOT NULL,
        "ReviewType" integer NOT NULL,
        "Status" integer NOT NULL,
        "ReviewPeriodStart" timestamp with time zone NOT NULL,
        "ReviewPeriodEnd" timestamp with time zone NOT NULL,
        "OverallRating" integer,
        "Comments" character varying(2000),
        "ReviewedByUserId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_PerformanceReviews" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Permissions" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Resource" character varying(100) NOT NULL,
        "Action" character varying(100) NOT NULL,
        "Description" character varying(500) NOT NULL,
        "Module" character varying(200) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Permissions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "PlacementRecords" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "EmployerId" uuid NOT NULL,
        "Position" character varying(200),
        "Type" integer NOT NULL,
        "Salary" numeric(18,2),
        "StartDate" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_PlacementRecords" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "PlatformMetrics" (
        "Id" uuid NOT NULL,
        "MetricName" character varying(100) NOT NULL,
        "Category" character varying(100) NOT NULL,
        "Value" numeric(18,4) NOT NULL,
        "Unit" character varying(50),
        "RecordedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_PlatformMetrics" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "POAttainments" (
        "Id" uuid NOT NULL,
        "ProgramOutcomeId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "AttainmentLevel" numeric NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_POAttainments" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "PolicyConfigs" (
        "Id" uuid NOT NULL,
        "Module" character varying(100) NOT NULL,
        "Key" character varying(200) NOT NULL,
        "Value" text NOT NULL,
        "ValueType" character varying(20) NOT NULL,
        "Description" text,
        "EffectiveFrom" timestamp with time zone NOT NULL,
        "EffectiveTo" timestamp with time zone,
        "CreatedBy" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedBy" uuid,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_PolicyConfigs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "PowerBiReports" (
        "Id" uuid NOT NULL,
        "ReportName" character varying(200) NOT NULL,
        "PowerBiReportId" character varying(200) NOT NULL,
        "PowerBiWorkspaceId" character varying(200) NOT NULL,
        "DatasetId" character varying(200) NOT NULL,
        "EmbedUrl" character varying(800) NOT NULL,
        "TargetRole" character varying(50) NOT NULL,
        "Description" character varying(2000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_PowerBiReports" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "PreviousVisitFindings" (
        "Id" uuid NOT NULL,
        "AccreditationCycleId" uuid NOT NULL,
        "FindingType" integer NOT NULL,
        "FindingSeverity" integer NOT NULL,
        "Description" character varying(2000),
        "IsResolved" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_PreviousVisitFindings" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ProcurementRequisitions" (
        "Id" uuid NOT NULL,
        "RequisitionNumber" character varying(50) NOT NULL,
        "RequestingDepartmentId" uuid NOT NULL,
        "RequestedBy" uuid NOT NULL,
        "RequisitionDate" date NOT NULL,
        "RequiredByDate" date,
        "TotalAmount" numeric(15,2) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ProcurementRequisitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ProgramEducationalObjectives" (
        "Id" uuid NOT NULL,
        "Description" character varying(1000) NOT NULL,
        "ProgramId" uuid NOT NULL,
        "SortOrder" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ProgramEducationalObjectives" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ProgramOutcomes" (
        "Id" uuid NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Description" character varying(1000) NOT NULL,
        "ProgramId" uuid NOT NULL,
        "SortOrder" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ProgramOutcomes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "PromptABTests" (
        "Id" uuid NOT NULL,
        "PromptId" uuid NOT NULL,
        "VersionA" integer NOT NULL,
        "VersionB" integer NOT NULL,
        "TrafficSplitA" double precision NOT NULL,
        "ResponsesA" integer NOT NULL,
        "ResponsesB" integer NOT NULL,
        "ThumbsUpA" integer NOT NULL,
        "ThumbsUpB" integer NOT NULL,
        "AvgLatencyMsA" double precision NOT NULL,
        "AvgLatencyMsB" double precision NOT NULL,
        "AvgTokensA" double precision NOT NULL,
        "AvgTokensB" double precision NOT NULL,
        "IsActive" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_PromptABTests" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Prompts" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Domain" character varying(100) NOT NULL,
        "ModelTarget" character varying(100),
        "CurrentVersion" integer NOT NULL,
        "ApprovalStatus" integer NOT NULL,
        "ActiveVersion" integer NOT NULL,
        "SystemPrompt" text NOT NULL,
        "UserPromptTemplate" text NOT NULL,
        "VariablesJson" jsonb NOT NULL,
        "ModelParametersJson" jsonb NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "ReviewedByUserId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Prompts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "PromptVersions" (
        "Id" uuid NOT NULL,
        "PromptId" uuid NOT NULL,
        "VersionNumber" integer NOT NULL,
        "SystemPrompt" text NOT NULL,
        "UserPromptTemplate" text NOT NULL,
        "ChangeDescription" character varying(500),
        "ChangedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PromptVersions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "PurchaseOrders" (
        "Id" uuid NOT NULL,
        "PoNumber" character varying(50) NOT NULL,
        "RequisitionId" uuid NOT NULL,
        "VendorId" uuid NOT NULL,
        "PoDate" date NOT NULL,
        "DeliveryDate" date,
        "TotalAmount" numeric(15,2) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PurchaseOrders" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "QueueDefinitions" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Purpose" character varying(500),
        "CurrentDepth" integer NOT NULL,
        "ConsumerCount" integer NOT NULL,
        "Status" integer NOT NULL,
        "JobsPerMinute" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_QueueDefinitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "QueueJobs" (
        "Id" uuid NOT NULL,
        "QueueId" uuid NOT NULL,
        "JobType" character varying(200) NOT NULL,
        "PayloadJson" jsonb NOT NULL,
        "Status" integer NOT NULL,
        "Priority" integer NOT NULL,
        "AttemptCount" integer NOT NULL,
        "EnqueuedAt" timestamp with time zone,
        "StartedAt" timestamp with time zone,
        "CompletedAt" timestamp with time zone,
        "ErrorMessage" character varying(5000),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_QueueJobs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ReadReplicaConfigs" (
        "Id" uuid NOT NULL,
        "ConnectionString" character varying(500) NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Priority" integer NOT NULL,
        "Status" integer NOT NULL,
        "MaxLagSeconds" integer NOT NULL,
        "CurrentLagSeconds" integer,
        "Weight" integer NOT NULL,
        "IsEnabled" boolean NOT NULL,
        "LastHealthCheckAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ReadReplicaConfigs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "RegistrationEnrollments" (
        "Id" uuid NOT NULL,
        "WindowId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "IsWaitlisted" boolean NOT NULL,
        "WaitlistPosition" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_RegistrationEnrollments" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "RegistrationWindows" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Semester" character varying(50),
        "Status" integer NOT NULL,
        "OpensAt" timestamp with time zone NOT NULL,
        "ClosesAt" timestamp with time zone NOT NULL,
        "MaxCoursesPerStudent" integer NOT NULL,
        "RequiresAdvisorApproval" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_RegistrationWindows" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ReleaseDownloads" (
        "Id" uuid NOT NULL,
        "Version" character varying(100) NOT NULL,
        "Channel" integer NOT NULL,
        "DownloadUrl" character varying(500),
        "Checksum" character varying(100),
        "ReleaseNotes" character varying(5000),
        "ReleasedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ReleaseDownloads" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ReportSchedules" (
        "Id" uuid NOT NULL,
        "BIReportId" uuid NOT NULL,
        "Frequency" integer NOT NULL,
        "Format" integer NOT NULL,
        "Recipients" character varying(500),
        "IsActive" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ReportSchedules" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ResearchGrants" (
        "Id" uuid NOT NULL,
        "Title" character varying(300) NOT NULL,
        "FundingAgency" character varying(200),
        "Amount" numeric(18,2) NOT NULL,
        "Status" integer NOT NULL,
        "ApplicationDate" timestamp with time zone NOT NULL,
        "AwardDate" timestamp with time zone,
        "ExpiryDate" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ResearchGrants" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ResearchPatents" (
        "Id" uuid NOT NULL,
        "Title" character varying(300) NOT NULL,
        "PatentNumber" character varying(100),
        "Status" integer NOT NULL,
        "FiledDate" timestamp with time zone,
        "GrantedDate" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ResearchPatents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ResearchProjects" (
        "Id" uuid NOT NULL,
        "Title" character varying(300) NOT NULL,
        "Description" character varying(5000),
        "PrincipalInvestigatorId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "StartDate" timestamp with time zone NOT NULL,
        "EndDate" timestamp with time zone,
        "Budget" numeric(18,2) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ResearchProjects" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ResearchPublications" (
        "Id" uuid NOT NULL,
        "Title" character varying(500) NOT NULL,
        "Type" integer NOT NULL,
        "JournalName" character varying(200),
        "PublishedDate" timestamp with time zone,
        "Doi" character varying(200),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ResearchPublications" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Roles" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Description" character varying(500) NOT NULL,
        "IsSystem" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Roles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "RoommatePreferences" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "PreferredStudentId" uuid NOT NULL,
        "IsMutual" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_RoommatePreferences" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Rubrics" (
        "Id" uuid NOT NULL,
        "Title" character varying(300) NOT NULL,
        "CourseLearningOutcomeId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Rubrics" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "RuleDefinitions" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(1000),
        "Domain" integer NOT NULL,
        "Status" integer NOT NULL,
        "MatchStrategy" integer NOT NULL,
        "Priority" integer NOT NULL,
        "CurrentVersion" integer NOT NULL,
        "ConditionsJson" jsonb NOT NULL,
        "ActionsJson" jsonb NOT NULL,
        "EffectiveFrom" timestamp with time zone,
        "EffectiveTo" timestamp with time zone,
        "Category" character varying(100),
        "Tags" character varying(200),
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_RuleDefinitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "SamlConfigurations" (
        "Id" uuid NOT NULL,
        "EntityId" character varying(500) NOT NULL,
        "IdpSsoUrl" character varying(500) NOT NULL,
        "IdpSloUrl" character varying(500),
        "IdpCertificate" text NOT NULL,
        "SpEntityId" character varying(500) NOT NULL,
        "SpAcsUrl" character varying(500) NOT NULL,
        "AttributeMappingJson" jsonb NOT NULL,
        "IsEnabled" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_SamlConfigurations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ScalingEvents" (
        "Id" uuid NOT NULL,
        "EventType" character varying(50) NOT NULL,
        "ReplicaName" character varying(100),
        "TotalReplicas" integer NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ScalingEvents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ScheduledJobs" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(1000),
        "Category" integer NOT NULL,
        "Module" character varying(100),
        "CronExpression" character varying(100) NOT NULL,
        "IsEnabled" boolean NOT NULL,
        "TimeoutSeconds" integer NOT NULL,
        "MaxRetries" integer NOT NULL,
        "ParametersJson" jsonb NOT NULL,
        "LastRunAt" timestamp with time zone,
        "NextRunAt" timestamp with time zone,
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ScheduledJobs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "SchoolVisits" (
        "Id" uuid NOT NULL,
        "SchoolName" character varying(200) NOT NULL,
        "Status" integer NOT NULL,
        "VisitDate" timestamp with time zone NOT NULL,
        "AssignedToUserId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_SchoolVisits" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "SeatAllocations" (
        "Id" uuid NOT NULL,
        "MeritListEntryId" uuid NOT NULL,
        "ApplicationId" uuid NOT NULL,
        "SeatType" character varying(50) NOT NULL,
        "AllocatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SeatAllocations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "SectionOverrideRequests" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "FromSectionId" uuid NOT NULL,
        "ToSectionId" uuid NOT NULL,
        "CurrentRegularCount" integer NOT NULL,
        "CurrentExamCount" integer NOT NULL,
        "CapacityAllowed" boolean NOT NULL,
        "FeeAdjustment" numeric(12,2) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "RequestedBy" uuid NOT NULL,
        "RequestedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SectionOverrideRequests" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ServiceDependencies" (
        "Id" uuid NOT NULL,
        "SourceServiceId" uuid NOT NULL,
        "TargetServiceId" uuid NOT NULL,
        "DependencyType" integer NOT NULL,
        "Description" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ServiceDependencies" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ServiceRegistrations" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Runtime" character varying(100),
        "Port" integer NOT NULL,
        "Status" integer NOT NULL,
        "HealthEndpoint" character varying(500),
        "RegisteredAt" timestamp with time zone NOT NULL,
        "LastHeartbeatAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ServiceRegistrations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "SickLeaveApplications" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "FromDate" timestamp with time zone NOT NULL,
        "ToDate" timestamp with time zone NOT NULL,
        "MedicalVisitId" uuid,
        "Status" character varying(20) NOT NULL,
        "ApprovedBy" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_SickLeaveApplications" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "SkillGaps" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "SkillName" character varying(200) NOT NULL,
        "CurrentLevel" character varying(50),
        "TargetLevel" character varying(50),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_SkillGaps" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "SLADefinitions" (
        "Id" uuid NOT NULL,
        "Category" integer NOT NULL,
        "Priority" integer NOT NULL,
        "ResponseTimeHours" integer NOT NULL,
        "ResolutionTimeHours" integer NOT NULL,
        "EscalationTimeHours" integer NOT NULL,
        "EscalationLevel" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_SLADefinitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "SlowQueries" (
        "Id" uuid NOT NULL,
        "QueryText" character varying(5000) NOT NULL,
        "DurationMs" double precision NOT NULL,
        "SourceModule" character varying(100),
        "Frequency" integer NOT NULL,
        "RecordedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_SlowQueries" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "SocietyMemberships" (
        "Id" uuid NOT NULL,
        "SocietyId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "Role" character varying(50) NOT NULL,
        "JoinedDate" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_SocietyMemberships" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "SsoSessions" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Provider" character varying(20) NOT NULL,
        "AccessTokenEncrypted" text NOT NULL,
        "RefreshTokenEncrypted" text,
        "TokenExpiresAt" timestamp with time zone NOT NULL,
        "DeviceId" character varying(255),
        "DeviceName" character varying(200),
        "IpAddress" text,
        "UserAgent" text,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SsoSessions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentAchievements" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "Type" integer NOT NULL,
        "Description" character varying(500),
        "AchievedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_StudentAchievements" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentActivities" (
        "Id" uuid NOT NULL,
        "ActivityName" character varying(200) NOT NULL,
        "ActivityType" character varying(50) NOT NULL,
        "OrganizingSocietyId" uuid,
        "ScheduledDate" timestamp with time zone NOT NULL,
        "Location" character varying(200),
        "Description" character varying(4000),
        "OrganizerId" uuid NOT NULL,
        "ApprovalStatus" character varying(20) NOT NULL,
        "ApprovedBy" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_StudentActivities" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentAlerts" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "TriggerType" integer NOT NULL,
        "Message" character varying(500),
        "IsRead" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_StudentAlerts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentClearances" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "Department" character varying(100),
        "ClearedByUserId" uuid,
        "ClearedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_StudentClearances" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentCounselings" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "Notes" character varying(2000),
        "SessionDate" timestamp with time zone NOT NULL,
        "CounselorUserId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_StudentCounselings" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentDisciplines" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "Severity" integer NOT NULL,
        "Status" integer NOT NULL,
        "Description" character varying(2000),
        "IncidentDate" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_StudentDisciplines" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentDocuments" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "DocumentType" character varying(200) NOT NULL,
        "FileUrl" character varying(500),
        "UploadedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_StudentDocuments" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentFeeDues" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "SemesterId" uuid NOT NULL,
        "FeeStructureId" uuid NOT NULL,
        "TotalDue" numeric(12,2) NOT NULL,
        "PaidAmount" numeric(12,2) NOT NULL,
        "ScholarshipDeduction" numeric(12,2) NOT NULL,
        "OutstandingAmount" numeric(12,2) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "WaivedBy" uuid,
        "WaivedReason" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_StudentFeeDues" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentFeePayments" (
        "Id" uuid NOT NULL,
        "FeeDueId" uuid NOT NULL,
        "PaymentMethod" character varying(50) NOT NULL,
        "Amount" numeric(12,2) NOT NULL,
        "ReferenceNumber" character varying(100),
        "PaymentDate" timestamp with time zone NOT NULL,
        "VerifiedBy" uuid,
        "VerificationDate" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_StudentFeePayments" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentFees" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "Status" integer NOT NULL,
        "DueDate" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_StudentFees" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentGuardians" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Relationship" character varying(50),
        "Phone" character varying(50),
        "Email" character varying(300),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_StudentGuardians" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentHealthRecords" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "BloodType" character varying(5),
        "AllergiesEncrypted" character varying(4000),
        "ChronicConditionsEncrypted" character varying(4000),
        "EmergencyContactName" character varying(100),
        "EmergencyContactPhone" character varying(20),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_StudentHealthRecords" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentHostels" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "HostelRoomId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "CheckInDate" timestamp with time zone NOT NULL,
        "CheckOutDate" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_StudentHostels" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentProfiles" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "ProfileJson" jsonb NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_StudentProfiles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentResumes" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "ResumeJson" jsonb NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_StudentResumes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentRiskScores" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "Score" double precision NOT NULL,
        "Category" integer NOT NULL,
        "CalculatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_StudentRiskScores" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentRiskTrends" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "ScoresJson" jsonb NOT NULL,
        "FromDate" timestamp with time zone NOT NULL,
        "ToDate" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_StudentRiskTrends" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentScholarships" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "ScholarshipName" character varying(200) NOT NULL,
        "Status" integer NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "AwardedAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_StudentScholarships" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentSocieties" (
        "Id" uuid NOT NULL,
        "SocietyName" character varying(200) NOT NULL,
        "Description" character varying(4000),
        "FacultyAdvisorId" uuid NOT NULL,
        "PresidentId" uuid,
        "FoundedYear" integer,
        "Active" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_StudentSocieties" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentTransports" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "TransportRouteId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "StartDate" timestamp with time zone NOT NULL,
        "EndDate" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_StudentTransports" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "SupportTickets" (
        "Id" uuid NOT NULL,
        "Subject" character varying(300) NOT NULL,
        "Description" character varying(5000),
        "Priority" character varying(50) NOT NULL,
        "Status" character varying(50) NOT NULL,
        "CustomerId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ResolvedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_SupportTickets" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "SystemConfigurations" (
        "Id" uuid NOT NULL,
        "UniversityId" uuid NOT NULL,
        "ReplicationLagHours" integer NOT NULL,
        "PrimaryDbConnectionString" text NOT NULL,
        "ReadReplica1ConnectionString" text,
        "ReadReplica2ConnectionString" text,
        "WriteReplicaConnectionString" text,
        "RedisConnectionString" text NOT NULL,
        "RedisCacheTtlMinutes" integer NOT NULL,
        "SubdomainName" character varying(200),
        "CustomDnsEnabled" boolean NOT NULL,
        "DnsNameserver1" text,
        "DnsNameserver2" text,
        "RegularClassCapacity" integer NOT NULL,
        "ExamClassCapacity" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SystemConfigurations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "TeamsSyncLogs" (
        "Id" uuid NOT NULL,
        "TimetableEntryId" uuid NOT NULL,
        "TeamsTeamId" character varying(200),
        "TeamsChannelId" character varying(200),
        "SyncStatus" character varying(20) NOT NULL,
        "ErrorMessage" character varying(2000),
        "SyncedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_TeamsSyncLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "TrainingAssignments" (
        "Id" uuid NOT NULL,
        "EmployeeId" uuid NOT NULL,
        "TrainingTitle" character varying(300) NOT NULL,
        "Status" integer NOT NULL,
        "AssignedDate" timestamp with time zone,
        "CompletedDate" timestamp with time zone,
        "Provider" character varying(100),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_TrainingAssignments" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Transcripts" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "GeneratedAt" timestamp with time zone NOT NULL,
        "PdfFilePath" text NOT NULL,
        "VerificationHash" character varying(64),
        "IsOfficial" boolean NOT NULL,
        "RequestedBy" uuid,
        CONSTRAINT "PK_Transcripts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "TranslationKeys" (
        "Id" uuid NOT NULL,
        "Key" character varying(200) NOT NULL,
        "Module" character varying(100) NOT NULL,
        "Context" character varying(50),
        "Description" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TranslationKeys" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "TransportDrivers" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "LicenseNumber" character varying(50),
        "Status" integer NOT NULL,
        "AssignedVehicleId" uuid,
        "LicenseExpiry" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_TransportDrivers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "TransportRoutes" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "RouteType" integer NOT NULL,
        "DistanceKm" numeric(18,4) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_TransportRoutes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "TransportVehicles" (
        "Id" uuid NOT NULL,
        "RegistrationNumber" character varying(100) NOT NULL,
        "Model" character varying(100),
        "Capacity" integer NOT NULL,
        "Status" integer NOT NULL,
        "AssignedRouteId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_TransportVehicles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "TunnelConfigurations" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "TunnelUrl" character varying(500),
        "Status" integer NOT NULL,
        "TokenHash" character varying(100),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_TunnelConfigurations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "UniversityMissions" (
        "Id" uuid NOT NULL,
        "Statement" character varying(2000) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_UniversityMissions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "UniversitySsoProfiles" (
        "Id" uuid NOT NULL,
        "Code" character varying(20) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Domain" character varying(100) NOT NULL,
        "SsoProvider" character varying(20) NOT NULL,
        "SsoTenantId" text,
        "SsoClientId" text,
        "SsoClientSecretEncrypted" text,
        "Require2Fa" boolean NOT NULL,
        "RequirePasswordChangeOnFirstLogin" boolean NOT NULL,
        "LogoUrl" text,
        "PrimaryColor" character varying(7),
        "SecondaryColor" character varying(7),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_UniversitySsoProfiles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "UpdateDeployments" (
        "Id" uuid NOT NULL,
        "FromVersion" character varying(100) NOT NULL,
        "ToVersion" character varying(100) NOT NULL,
        "Status" integer NOT NULL,
        "StartedAt" timestamp with time zone,
        "CompletedAt" timestamp with time zone,
        "Log" character varying(5000),
        "IsRolledBack" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_UpdateDeployments" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL,
        "Username" character varying(150) NOT NULL,
        "Email" character varying(300) NOT NULL,
        "FirstName" character varying(100) NOT NULL,
        "LastName" character varying(100) NOT NULL,
        "Status" integer NOT NULL,
        "AuthProvider" integer NOT NULL,
        "ExternalId" character varying(500),
        "ExternalClaimsJson" character varying(1000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "LastLoginAt" timestamp with time zone,
        "DeactivatedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "UserSecondFactors" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Method" character varying(20) NOT NULL,
        "SecretEncrypted" text NOT NULL,
        "BackupCodesHashCsv" text NOT NULL,
        "IsEnabled" boolean NOT NULL,
        "VerifiedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_UserSecondFactors" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "VehicleMaintenances" (
        "Id" uuid NOT NULL,
        "TransportVehicleId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "Description" character varying(500),
        "Cost" numeric(18,2) NOT NULL,
        "ScheduledDate" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_VehicleMaintenances" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Vendors" (
        "Id" uuid NOT NULL,
        "VendorName" character varying(200) NOT NULL,
        "VendorCnic" character varying(15),
        "BankAccount" character varying(50),
        "ContactPerson" character varying(100),
        "Email" character varying(200),
        "Phone" character varying(20),
        "Address" text,
        "Status" character varying(20) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Vendors" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "VerificationCycles" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Status" integer NOT NULL,
        "StartDate" timestamp with time zone NOT NULL,
        "EndDate" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_VerificationCycles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "VisitorLogs" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "VisitorName" character varying(200) NOT NULL,
        "VisitorCNIC" character varying(50),
        "EntryTime" timestamp with time zone NOT NULL,
        "ExitTime" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_VisitorLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "WebhookSubscriptions" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "EndpointUrl" character varying(500) NOT NULL,
        "EventType" character varying(200) NOT NULL,
        "Secret" character varying(100),
        "Status" integer NOT NULL,
        "MaxRetries" integer NOT NULL,
        "RetryCount" integer NOT NULL,
        "LastDeliveredAt" timestamp with time zone,
        "LastFailedAt" timestamp with time zone,
        "LastError" character varying(2000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_WebhookSubscriptions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "WorkflowDefinitions" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(1000) NOT NULL,
        "Version" integer NOT NULL,
        "Status" integer NOT NULL,
        "NodesJson" jsonb NOT NULL,
        "TransitionsJson" jsonb NOT NULL,
        "TriggerType" integer NOT NULL,
        "TriggerConfig" character varying(200),
        "IsTemplate" boolean NOT NULL,
        "Category" character varying(100),
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_WorkflowDefinitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Semesters" (
        "Id" uuid NOT NULL,
        "AcademicYearId" uuid NOT NULL,
        "Name" character varying(50) NOT NULL,
        "Type" character varying(20) NOT NULL,
        "StartDate" date NOT NULL,
        "EndDate" date NOT NULL,
        "ExamStartDate" date,
        "ExamEndDate" date,
        "ResultDeclarationDate" date,
        "AddDropEndDate" date,
        "IsCurrent" boolean NOT NULL,
        "Status" character varying(20) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Semesters" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Semesters_AcademicYears_AcademicYearId" FOREIGN KEY ("AcademicYearId") REFERENCES "AcademicYears" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AccreditationCriteria" (
        "Id" uuid NOT NULL,
        "AccreditationBodyId" uuid NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Title" character varying(500) NOT NULL,
        "Description" character varying(2000),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AccreditationCriteria" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AccreditationCriteria_AccreditationBodies_AccreditationBody~" FOREIGN KEY ("AccreditationBodyId") REFERENCES "AccreditationBodies" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AccreditationCycles" (
        "Id" uuid NOT NULL,
        "AccreditationBodyId" uuid NOT NULL,
        "DepartmentId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "VisitDate" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AccreditationCycles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AccreditationCycles_AccreditationBodies_AccreditationBodyId" FOREIGN KEY ("AccreditationBodyId") REFERENCES "AccreditationBodies" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ApplicationDocuments" (
        "Id" uuid NOT NULL,
        "ApplicationId" uuid NOT NULL,
        "DocumentType" character varying(50) NOT NULL,
        "FilePath" text NOT NULL,
        "UploadDate" timestamp with time zone NOT NULL,
        "VerifiedBy" uuid,
        "VerifiedAt" timestamp with time zone,
        "VerificationStatus" character varying(20) NOT NULL,
        "RejectionReason" character varying(1000),
        CONSTRAINT "PK_ApplicationDocuments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ApplicationDocuments_AdmissionApplications_ApplicationId" FOREIGN KEY ("ApplicationId") REFERENCES "AdmissionApplications" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "LeadActivities" (
        "Id" uuid NOT NULL,
        "AdmissionLeadId" uuid NOT NULL,
        "ActivityType" integer NOT NULL,
        "Notes" character varying(1000),
        "ActivityDate" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_LeadActivities" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_LeadActivities_AdmissionLeads_AdmissionLeadId" FOREIGN KEY ("AdmissionLeadId") REFERENCES "AdmissionLeads" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AdvisorMeetings" (
        "Id" uuid NOT NULL,
        "AdvisorAssignmentId" uuid NOT NULL,
        "ScheduledAt" timestamp with time zone NOT NULL,
        "Status" integer NOT NULL,
        "Notes" character varying(2000),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AdvisorMeetings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AdvisorMeetings_AdvisorAssignments_AdvisorAssignmentId" FOREIGN KEY ("AdvisorAssignmentId") REFERENCES "AdvisorAssignments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AdvisorNotes" (
        "Id" uuid NOT NULL,
        "AdvisorAssignmentId" uuid NOT NULL,
        "Note" character varying(5000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AdvisorNotes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AdvisorNotes_AdvisorAssignments_AdvisorAssignmentId" FOREIGN KEY ("AdvisorAssignmentId") REFERENCES "AdvisorAssignments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AssessmentComponents" (
        "Id" uuid NOT NULL,
        "SchemeId" uuid NOT NULL,
        "ComponentType" character varying(50) NOT NULL,
        "WeightPercentage" integer NOT NULL,
        "TotalMarks" integer NOT NULL,
        "PassingMarks" integer,
        "IsMandatory" boolean NOT NULL,
        "OrderIndex" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AssessmentComponents" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AssessmentComponents_AssessmentSchemes_SchemeId" FOREIGN KEY ("SchemeId") REFERENCES "AssessmentSchemes" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Assets" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "SerialNumber" character varying(100),
        "AssetCategoryId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "LocationBuildingId" uuid,
        "LocationRoomId" uuid,
        "PurchaseCost" numeric(18,2) NOT NULL,
        "PurchaseDate" timestamp with time zone,
        "WarrantyExpiry" timestamp with time zone,
        "DepreciationMethod" integer NOT NULL,
        "UsefulLifeYears" integer NOT NULL,
        "CurrentValue" numeric(18,2) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Assets" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Assets_AssetCategories_AssetCategoryId" FOREIGN KEY ("AssetCategoryId") REFERENCES "AssetCategories" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "BackupRecords" (
        "Id" uuid NOT NULL,
        "ConfigurationId" uuid NOT NULL,
        "BackupType" integer NOT NULL,
        "Status" integer NOT NULL,
        "FilePath" character varying(500) NOT NULL,
        "SizeBytes" bigint NOT NULL,
        "Checksum" character varying(100),
        "IsEncrypted" boolean NOT NULL,
        "RetentionTier" integer NOT NULL,
        "StartedAt" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        "ExpiresAt" timestamp with time zone,
        "IsVerified" boolean NOT NULL,
        "VerifiedAt" timestamp with time zone,
        "ErrorMessage" character varying(2000),
        "TriggeredBy" character varying(100),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_BackupRecords" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_BackupRecords_BackupConfigurations_ConfigurationId" FOREIGN KEY ("ConfigurationId") REFERENCES "BackupConfigurations" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "OrderItems" (
        "Id" uuid NOT NULL,
        "BookOrderId" uuid NOT NULL,
        "Title" character varying(500) NOT NULL,
        "Quantity" integer NOT NULL,
        "UnitPrice" numeric(18,2) NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_OrderItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_OrderItems_BookOrders_BookOrderId" FOREIGN KEY ("BookOrderId") REFERENCES "BookOrders" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "DriveRegistrations" (
        "Id" uuid NOT NULL,
        "CampusDriveId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "RegisteredAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_DriveRegistrations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DriveRegistrations_CampusDrives_CampusDriveId" FOREIGN KEY ("CampusDriveId") REFERENCES "CampusDrives" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Buildings" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "BuildingCode" character varying(50),
        "CampusId" uuid NOT NULL,
        "FloorCount" integer NOT NULL,
        "Address" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Buildings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Buildings_Campuses_CampusId" FOREIGN KEY ("CampusId") REFERENCES "Campuses" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CampusConfigurations" (
        "Id" uuid NOT NULL,
        "CampusId" uuid NOT NULL,
        "AcademicCalendarJson" jsonb NOT NULL,
        "GradingPolicyJson" jsonb NOT NULL,
        "AdmissionRulesJson" jsonb NOT NULL,
        "FeeStructureJson" jsonb NOT NULL,
        "PoliciesJson" jsonb NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_CampusConfigurations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CampusConfigurations_Campuses_CampusId" FOREIGN KEY ("CampusId") REFERENCES "Campuses" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CareerEventRegistrations" (
        "Id" uuid NOT NULL,
        "CareerEventId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "RegisteredAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_CareerEventRegistrations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CareerEventRegistrations_CareerEvents_CareerEventId" FOREIGN KEY ("CareerEventId") REFERENCES "CareerEvents" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ChatbotMessages" (
        "Id" uuid NOT NULL,
        "ChatbotConversationId" uuid NOT NULL,
        "Sender" integer NOT NULL,
        "Content" character varying(5000) NOT NULL,
        "SentAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ChatbotMessages" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ChatbotMessages_ChatbotConversations_ChatbotConversationId" FOREIGN KEY ("ChatbotConversationId") REFERENCES "ChatbotConversations" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ComplianceControls" (
        "Id" uuid NOT NULL,
        "FrameworkId" uuid NOT NULL,
        "ControlCode" character varying(50) NOT NULL,
        "Title" character varying(300) NOT NULL,
        "Description" character varying(2000) NOT NULL,
        "Status" integer NOT NULL,
        "OwnerUserId" uuid,
        "OwnerName" character varying(200),
        "ReviewFrequency" integer NOT NULL,
        "LastReviewedAt" timestamp with time zone,
        "NextReviewDate" timestamp with time zone,
        "AutomatedCheckConfigJson" jsonb NOT NULL,
        "IsAutomated" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ComplianceControls" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ComplianceControls_ComplianceFrameworks_FrameworkId" FOREIGN KEY ("FrameworkId") REFERENCES "ComplianceFrameworks" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ConfigAuditLogs" (
        "Id" uuid NOT NULL,
        "ConfigurationEntryId" uuid NOT NULL,
        "ConfigKey" character varying(200) NOT NULL,
        "Action" character varying(50) NOT NULL,
        "PreviousValueJson" jsonb,
        "NewValueJson" jsonb,
        "FromVersion" integer,
        "ToVersion" integer,
        "ChangedByUserId" uuid NOT NULL,
        "ChangedByName" character varying(200),
        "ChangedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ConfigAuditLogs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ConfigAuditLogs_ConfigurationEntries_ConfigurationEntryId" FOREIGN KEY ("ConfigurationEntryId") REFERENCES "ConfigurationEntries" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ConfigVersions" (
        "Id" uuid NOT NULL,
        "ConfigurationEntryId" uuid NOT NULL,
        "VersionNumber" integer NOT NULL,
        "ValueJson" jsonb NOT NULL,
        "ChangeDescription" character varying(500),
        "ChangedByUserId" uuid NOT NULL,
        "ChangedByName" character varying(200),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ConfigVersions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ConfigVersions_ConfigurationEntries_ConfigurationEntryId" FOREIGN KEY ("ConfigurationEntryId") REFERENCES "ConfigurationEntries" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "TaxSlabs" (
        "Id" uuid NOT NULL,
        "CountryPackId" uuid NOT NULL,
        "TaxType" integer NOT NULL,
        "Name" character varying(200) NOT NULL,
        "MinAmount" numeric(18,2) NOT NULL,
        "MaxAmount" numeric(18,2),
        "Rate" numeric(8,4) NOT NULL,
        "FixedDeduction" numeric(18,2),
        "SortOrder" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "EffectiveFrom" timestamp with time zone NOT NULL,
        "EffectiveTo" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_TaxSlabs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TaxSlabs_CountryPacks_CountryPackId" FOREIGN KEY ("CountryPackId") REFERENCES "CountryPacks" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CurriculumCourses" (
        "Id" uuid NOT NULL,
        "CurriculumVersionId" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "Semester" integer NOT NULL,
        "IsMandatory" boolean NOT NULL,
        "IsElective" boolean NOT NULL,
        "TheoryCreditHours" integer NOT NULL,
        "LabCreditHours" integer NOT NULL,
        "TotalCreditHours" integer GENERATED ALWAYS AS ("TheoryCreditHours" + "LabCreditHours") STORED NOT NULL,
        "MappedClosCsv" character varying(1000) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_CurriculumCourses" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CurriculumCourses_CurriculumVersions_CurriculumVersionId" FOREIGN KEY ("CurriculumVersionId") REFERENCES "CurriculumVersions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CurriculumPrerequisites" (
        "Id" uuid NOT NULL,
        "CurriculumVersionId" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "PrerequisiteCourseId" uuid NOT NULL,
        "Relationship" character varying(20) NOT NULL,
        "SubstituteCourseIdsCsv" character varying(1000) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_CurriculumPrerequisites" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_CurriculumPrerequisites_NoSelf" CHECK ("CourseId" <> "PrerequisiteCourseId"),
        CONSTRAINT "FK_CurriculumPrerequisites_CurriculumVersions_CurriculumVersio~" FOREIGN KEY ("CurriculumVersionId") REFERENCES "CurriculumVersions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "DashboardSubscriptions" (
        "Id" uuid NOT NULL,
        "DashboardId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "RefreshFrequency" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_DashboardSubscriptions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DashboardSubscriptions_Dashboards_DashboardId" FOREIGN KEY ("DashboardId") REFERENCES "Dashboards" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "DashboardWidgets" (
        "Id" uuid NOT NULL,
        "DashboardId" uuid NOT NULL,
        "VisualizationType" integer NOT NULL,
        "Title" character varying(300),
        "ConfigJson" jsonb NOT NULL,
        "SortOrder" integer NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_DashboardWidgets" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DashboardWidgets_Dashboards_DashboardId" FOREIGN KEY ("DashboardId") REFERENCES "Dashboards" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "BIReports" (
        "Id" uuid NOT NULL,
        "Title" character varying(300) NOT NULL,
        "ReportCategory" integer NOT NULL,
        "DataWarehouseTableId" uuid NOT NULL,
        "QueryJson" jsonb NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_BIReports" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_BIReports_DataWarehouseTables_DataWarehouseTableId" FOREIGN KEY ("DataWarehouseTableId") REFERENCES "DataWarehouseTables" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "DWColumns" (
        "Id" uuid NOT NULL,
        "DataWarehouseTableId" uuid NOT NULL,
        "ColumnName" character varying(200) NOT NULL,
        "DataType" character varying(100),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_DWColumns" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DWColumns_DataWarehouseTables_DataWarehouseTableId" FOREIGN KEY ("DataWarehouseTableId") REFERENCES "DataWarehouseTables" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "DegreeRequirements" (
        "Id" uuid NOT NULL,
        "ProgramId" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Category" integer NOT NULL,
        "RequirementType" integer NOT NULL,
        "CourseId" uuid,
        "MinCredits" integer,
        "CourseCount" integer,
        "MinGradePoints" numeric,
        "AllowedCourseIdsJson" jsonb NOT NULL,
        "SortOrder" integer NOT NULL,
        "IsRequired" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_DegreeRequirements" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DegreeRequirements_DegreePrograms_ProgramId" FOREIGN KEY ("ProgramId") REFERENCES "DegreePrograms" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Batches" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "StartYear" integer NOT NULL,
        "EndYear" integer NOT NULL,
        "DepartmentId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Batches" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Batches_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Courses" (
        "Id" uuid NOT NULL,
        "Title" character varying(200) NOT NULL,
        "Code" character varying(20),
        "CreditHours" integer NOT NULL,
        "DepartmentId" uuid NOT NULL,
        "PrerequisiteCourseId" uuid,
        "CloMappingJson" jsonb,
        "PloMappingJson" jsonb,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Courses" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Courses_Courses_PrerequisiteCourseId" FOREIGN KEY ("PrerequisiteCourseId") REFERENCES "Courses" ("Id"),
        CONSTRAINT "FK_Courses_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Faculties" (
        "Id" uuid NOT NULL,
        "FirstName" character varying(100) NOT NULL,
        "LastName" character varying(100) NOT NULL,
        "EmployeeCode" character varying(50),
        "Designation" integer NOT NULL,
        "DepartmentId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Faculties" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Faculties_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "EmployeeContracts" (
        "Id" uuid NOT NULL,
        "EmployeeId" uuid NOT NULL,
        "StartDate" timestamp with time zone NOT NULL,
        "EndDate" timestamp with time zone,
        "Salary" numeric(18,2),
        "ContractType" character varying(200),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_EmployeeContracts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EmployeeContracts_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "EmployeeDocuments" (
        "Id" uuid NOT NULL,
        "EmployeeId" uuid NOT NULL,
        "DocumentType" character varying(200) NOT NULL,
        "FileUrl" character varying(500),
        "UploadedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_EmployeeDocuments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EmployeeDocuments_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ResearchLabs" (
        "Id" uuid NOT NULL,
        "LabName" character varying(200) NOT NULL,
        "LabCode" character varying(50) NOT NULL,
        "RoomNumber" character varying(100) NOT NULL,
        "DepartmentId" uuid NOT NULL,
        "LabSupervisorId" uuid,
        "Capacity" integer NOT NULL,
        "Description" character varying(500) NOT NULL,
        "Status" integer NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_ResearchLabs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ResearchLabs_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_ResearchLabs_Employees_LabSupervisorId" FOREIGN KEY ("LabSupervisorId") REFERENCES "Employees" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "SecurityGuards" (
        "Id" uuid NOT NULL,
        "EmployeeId" uuid NOT NULL,
        "BadgeNumber" character varying(50) NOT NULL,
        "IsArmed" boolean NOT NULL,
        "TrainingExpiryDate" timestamp with time zone,
        "Status" integer NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_SecurityGuards" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SecurityGuards_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ExtensionInstallations" (
        "Id" uuid NOT NULL,
        "ExtensionId" uuid NOT NULL,
        "InstalledVersion" character varying(50) NOT NULL,
        "Status" integer NOT NULL,
        "ConfigJson" jsonb NOT NULL,
        "InstallLog" character varying(2000),
        "InstalledAt" timestamp with time zone NOT NULL,
        "UninstalledAt" timestamp with time zone,
        "InstalledByUserId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ExtensionInstallations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ExtensionInstallations_Extensions_ExtensionId" FOREIGN KEY ("ExtensionId") REFERENCES "Extensions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ExtensionReviews" (
        "Id" uuid NOT NULL,
        "ExtensionId" uuid NOT NULL,
        "Rating" integer NOT NULL,
        "Comment" character varying(2000),
        "ReviewedByName" character varying(200) NOT NULL,
        "ReviewedByUserId" uuid NOT NULL,
        "DeveloperResponse" character varying(2000),
        "DeveloperResponseAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ExtensionReviews" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ExtensionReviews_Extensions_ExtensionId" FOREIGN KEY ("ExtensionId") REFERENCES "Extensions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "PluginRevenueShares" (
        "Id" uuid NOT NULL,
        "ExtensionId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "DeveloperEmail" text NOT NULL,
        "GrossAmount" numeric(18,2) NOT NULL,
        "AegisCoreAmount" numeric(18,2) NOT NULL,
        "DeveloperAmount" numeric(18,2) NOT NULL,
        "RecordedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PluginRevenueShares" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PluginRevenueShares_Extensions_ExtensionId" FOREIGN KEY ("ExtensionId") REFERENCES "Extensions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "FeeComponents" (
        "Id" uuid NOT NULL,
        "FeeStructureId" uuid NOT NULL,
        "ComponentName" character varying(100) NOT NULL,
        "Amount" numeric(12,2) NOT NULL,
        "IsMandatory" boolean NOT NULL,
        "OrderIndex" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_FeeComponents" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FeeComponents_FeeStructures_FeeStructureId" FOREIGN KEY ("FeeStructureId") REFERENCES "FeeStructures" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "FiscalPeriods" (
        "Id" uuid NOT NULL,
        "FiscalYearId" uuid NOT NULL,
        "Name" character varying(50) NOT NULL,
        "StartDate" timestamp with time zone NOT NULL,
        "EndDate" timestamp with time zone NOT NULL,
        "IsClosed" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_FiscalPeriods" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FiscalPeriods_FiscalYears_FiscalYearId" FOREIGN KEY ("FiscalYearId") REFERENCES "FiscalYears" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "FormSubmissions" (
        "Id" uuid NOT NULL,
        "FormId" uuid NOT NULL,
        "SubmittedByUserId" uuid,
        "SubmittedByName" character varying(200),
        "SubmittedByEmail" character varying(300),
        "Status" integer NOT NULL,
        "ValuesJson" jsonb NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "SubmittedAt" timestamp with time zone,
        "ReviewedAt" timestamp with time zone,
        "ReviewNotes" character varying(500),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_FormSubmissions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FormSubmissions_FormDefinitions_FormId" FOREIGN KEY ("FormId") REFERENCES "FormDefinitions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "GradeBands" (
        "Id" uuid NOT NULL,
        "GradeScaleId" uuid NOT NULL,
        "LetterGrade" character varying(2) NOT NULL,
        "GpaPoints" numeric(3,2) NOT NULL,
        "PercentageFrom" integer NOT NULL,
        "PercentageTo" integer NOT NULL,
        "Description" character varying(200),
        CONSTRAINT "PK_GradeBands" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_GradeBands_GradeScales_GradeScaleId" FOREIGN KEY ("GradeScaleId") REFERENCES "GradeScales" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "GrievanceResponses" (
        "Id" uuid NOT NULL,
        "TicketId" uuid NOT NULL,
        "ResponderId" uuid NOT NULL,
        "ResponseText" character varying(10000) NOT NULL,
        "StatusUpdate" character varying(20),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_GrievanceResponses" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_GrievanceResponses_GrievanceTickets_TicketId" FOREIGN KEY ("TicketId") REFERENCES "GrievanceTickets" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "HostelFloors" (
        "Id" uuid NOT NULL,
        "HostelBlockId" uuid NOT NULL,
        "FloorNumber" integer NOT NULL,
        "Name" character varying(100),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_HostelFloors" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_HostelFloors_HostelBlocks_HostelBlockId" FOREIGN KEY ("HostelBlockId") REFERENCES "HostelBlocks" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "SensorReadings" (
        "Id" uuid NOT NULL,
        "SensorId" uuid NOT NULL,
        "Value" numeric(12,4) NOT NULL,
        "Unit" character varying(50),
        "RecordedAt" timestamp with time zone NOT NULL,
        "ExtraDataJson" jsonb,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_SensorReadings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SensorReadings_IoTSensors_SensorId" FOREIGN KEY ("SensorId") REFERENCES "IoTSensors" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "JobApplications" (
        "Id" uuid NOT NULL,
        "JobPostingId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "CoverLetter" character varying(2000),
        "AppliedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_JobApplications" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_JobApplications_JobPostings_JobPostingId" FOREIGN KEY ("JobPostingId") REFERENCES "JobPostings" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "JournalEntryLines" (
        "Id" uuid NOT NULL,
        "JournalEntryId" uuid NOT NULL,
        "ChartOfAccountId" uuid NOT NULL,
        "Debit" numeric(18,2) NOT NULL,
        "Credit" numeric(18,2) NOT NULL,
        "Description" character varying(500),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_JournalEntryLines" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_JournalEntryLines_JournalEntries_JournalEntryId" FOREIGN KEY ("JournalEntryId") REFERENCES "JournalEntries" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "BookCopies" (
        "Id" uuid NOT NULL,
        "LibraryBookId" uuid NOT NULL,
        "Barcode" character varying(100),
        "Status" integer NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_BookCopies" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_BookCopies_LibraryBooks_LibraryBookId" FOREIGN KEY ("LibraryBookId") REFERENCES "LibraryBooks" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "MentoringSessions" (
        "Id" uuid NOT NULL,
        "MentorAssignmentId" uuid NOT NULL,
        "ScheduledAt" timestamp with time zone NOT NULL,
        "Status" integer NOT NULL,
        "Notes" character varying(2000),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_MentoringSessions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_MentoringSessions_MentorAssignments_MentorAssignmentId" FOREIGN KEY ("MentorAssignmentId") REFERENCES "MentorAssignments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "MeritListEntries" (
        "Id" uuid NOT NULL,
        "MeritListId" uuid NOT NULL,
        "ApplicationId" uuid NOT NULL,
        "MeritRank" integer NOT NULL,
        "MeritScore" numeric(10,2) NOT NULL,
        "MatricContribution" numeric(10,2) NOT NULL,
        "FscContribution" numeric(10,2) NOT NULL,
        "EntryTestContribution" numeric(10,2) NOT NULL,
        "SeatAllocated" boolean NOT NULL,
        "AllocationStatus" character varying(20),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_MeritListEntries" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_MeritListEntries_AdmissionApplications_ApplicationId" FOREIGN KEY ("ApplicationId") REFERENCES "AdmissionApplications" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_MeritListEntries_MeritLists_MeritListId" FOREIGN KEY ("MeritListId") REFERENCES "MeritLists" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Payslips" (
        "Id" uuid NOT NULL,
        "PayrollRunId" uuid NOT NULL,
        "EmployeeId" uuid NOT NULL,
        "Month" date NOT NULL,
        "BasicSalary" numeric(12,2) NOT NULL,
        "GrossSalary" numeric(12,2) NOT NULL,
        "EobiDeduction" numeric(12,2) NOT NULL,
        "IncomeTaxDeduction" numeric(12,2) NOT NULL,
        "ProvidentFundDeduction" numeric(12,2) NOT NULL,
        "TotalDeductions" numeric(12,2) NOT NULL,
        "NetSalary" numeric(12,2) NOT NULL,
        "PdfFilePath" text,
        "GeneratedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Payslips" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Payslips_PayrollRuns_PayrollRunId" FOREIGN KEY ("PayrollRunId") REFERENCES "PayrollRuns" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CrossDeptPermissionRequests" (
        "Id" uuid NOT NULL,
        "RequestingUserId" uuid NOT NULL,
        "RequestingDeptId" uuid NOT NULL,
        "TargetDeptId" uuid NOT NULL,
        "PermissionId" uuid NOT NULL,
        "Reason" text NOT NULL,
        "Status" character varying(20) NOT NULL,
        "ReviewedBy" uuid,
        "ReviewedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_CrossDeptPermissionRequests" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CrossDeptPermissionRequests_Permissions_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES "Permissions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "RequisitionLineItems" (
        "Id" uuid NOT NULL,
        "RequisitionId" uuid NOT NULL,
        "ItemCode" character varying(100),
        "Description" text NOT NULL,
        "Quantity" integer NOT NULL,
        "UnitPrice" numeric(12,2) NOT NULL,
        "TotalPrice" numeric(15,2) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_RequisitionLineItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RequisitionLineItems_ProcurementRequisitions_RequisitionId" FOREIGN KEY ("RequisitionId") REFERENCES "ProcurementRequisitions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CLOPOMappings" (
        "Id" uuid NOT NULL,
        "CourseLearningOutcomeId" uuid NOT NULL,
        "ProgramOutcomeId" uuid NOT NULL,
        "Strength" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_CLOPOMappings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CLOPOMappings_CourseLearningOutcomes_CourseLearningOutcomeId" FOREIGN KEY ("CourseLearningOutcomeId") REFERENCES "CourseLearningOutcomes" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_CLOPOMappings_ProgramOutcomes_ProgramOutcomeId" FOREIGN KEY ("ProgramOutcomeId") REFERENCES "ProgramOutcomes" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "GrantExpenditures" (
        "Id" uuid NOT NULL,
        "ResearchGrantId" uuid NOT NULL,
        "Description" character varying(500) NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "ExpenditureDate" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_GrantExpenditures" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_GrantExpenditures_ResearchGrants_ResearchGrantId" FOREIGN KEY ("ResearchGrantId") REFERENCES "ResearchGrants" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "PatentInventors" (
        "Id" uuid NOT NULL,
        "ResearchPatentId" uuid NOT NULL,
        "FacultyId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_PatentInventors" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PatentInventors_ResearchPatents_ResearchPatentId" FOREIGN KEY ("ResearchPatentId") REFERENCES "ResearchPatents" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ResearchDeliverables" (
        "Id" uuid NOT NULL,
        "ResearchProjectId" uuid NOT NULL,
        "Title" character varying(300) NOT NULL,
        "Type" integer NOT NULL,
        "Status" integer NOT NULL,
        "SubmittedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ResearchDeliverables" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ResearchDeliverables_ResearchProjects_ResearchProjectId" FOREIGN KEY ("ResearchProjectId") REFERENCES "ResearchProjects" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ResearchMilestones" (
        "Id" uuid NOT NULL,
        "ResearchProjectId" uuid NOT NULL,
        "Title" character varying(300) NOT NULL,
        "Status" integer NOT NULL,
        "DueDate" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ResearchMilestones" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ResearchMilestones_ResearchProjects_ResearchProjectId" FOREIGN KEY ("ResearchProjectId") REFERENCES "ResearchProjects" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ResearchProjectMembers" (
        "Id" uuid NOT NULL,
        "ResearchProjectId" uuid NOT NULL,
        "FacultyId" uuid NOT NULL,
        "Role" integer NOT NULL,
        "JoinedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ResearchProjectMembers" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ResearchProjectMembers_ResearchProjects_ResearchProjectId" FOREIGN KEY ("ResearchProjectId") REFERENCES "ResearchProjects" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "PublicationAuthors" (
        "Id" uuid NOT NULL,
        "ResearchPublicationId" uuid NOT NULL,
        "FacultyId" uuid NOT NULL,
        "AuthorOrder" integer NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_PublicationAuthors" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PublicationAuthors_ResearchPublications_ResearchPublication~" FOREIGN KEY ("ResearchPublicationId") REFERENCES "ResearchPublications" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "RolePermissions" (
        "Id" uuid NOT NULL,
        "RoleId" uuid NOT NULL,
        "PermissionId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_RolePermissions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RolePermissions_Permissions_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES "Permissions" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_RolePermissions_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "RubricCriteria" (
        "Id" uuid NOT NULL,
        "RubricId" uuid NOT NULL,
        "Description" character varying(500) NOT NULL,
        "MaxScore" integer NOT NULL,
        "SortOrder" integer NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_RubricCriteria" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RubricCriteria_Rubrics_RubricId" FOREIGN KEY ("RubricId") REFERENCES "Rubrics" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "RuleVersions" (
        "Id" uuid NOT NULL,
        "RuleId" uuid NOT NULL,
        "VersionNumber" integer NOT NULL,
        "ConditionsJson" jsonb NOT NULL,
        "ActionsJson" jsonb NOT NULL,
        "ChangeDescription" character varying(500),
        "ChangedByUserId" uuid NOT NULL,
        "ChangedByName" character varying(200),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_RuleVersions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RuleVersions_RuleDefinitions_RuleId" FOREIGN KEY ("RuleId") REFERENCES "RuleDefinitions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "JobExecutions" (
        "Id" uuid NOT NULL,
        "JobId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "StartedAt" timestamp with time zone,
        "CompletedAt" timestamp with time zone,
        "DurationMs" bigint,
        "AttemptNumber" integer NOT NULL,
        "ErrorMessage" character varying(5000),
        "Output" character varying(2000),
        "TriggeredBy" character varying(100),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_JobExecutions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_JobExecutions_ScheduledJobs_JobId" FOREIGN KEY ("JobId") REFERENCES "ScheduledJobs" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "FeePayments" (
        "Id" uuid NOT NULL,
        "StudentFeeId" uuid NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "PaidAt" timestamp with time zone NOT NULL,
        "PaymentMethod" character varying(50),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_FeePayments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FeePayments_StudentFees_StudentFeeId" FOREIGN KEY ("StudentFeeId") REFERENCES "StudentFees" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "TranslationValues" (
        "Id" uuid NOT NULL,
        "TranslationKeyId" uuid NOT NULL,
        "LanguageCode" character varying(20) NOT NULL,
        "Value" character varying(2000) NOT NULL,
        "IsMachineTranslated" boolean NOT NULL,
        "LastReviewedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TranslationValues" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TranslationValues_TranslationKeys_TranslationKeyId" FOREIGN KEY ("TranslationKeyId") REFERENCES "TranslationKeys" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "DriverShifts" (
        "Id" uuid NOT NULL,
        "TransportDriverId" uuid NOT NULL,
        "ShiftStart" timestamp with time zone NOT NULL,
        "ShiftEnd" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_DriverShifts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DriverShifts_TransportDrivers_TransportDriverId" FOREIGN KEY ("TransportDriverId") REFERENCES "TransportDrivers" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "RouteStops" (
        "Id" uuid NOT NULL,
        "TransportRouteId" uuid NOT NULL,
        "StopName" character varying(200) NOT NULL,
        "Sequence" integer NOT NULL,
        "EstimatedTime" character varying(10),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_RouteStops" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RouteStops_TransportRoutes_TransportRouteId" FOREIGN KEY ("TransportRouteId") REFERENCES "TransportRoutes" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "GPSLocations" (
        "Id" uuid NOT NULL,
        "TransportVehicleId" uuid NOT NULL,
        "Latitude" numeric(12,8) NOT NULL,
        "Longitude" numeric(12,8) NOT NULL,
        "RecordedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_GPSLocations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_GPSLocations_TransportVehicles_TransportVehicleId" FOREIGN KEY ("TransportVehicleId") REFERENCES "TransportVehicles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "UserRoles" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "RoleId" uuid NOT NULL,
        "AssignedByUserId" uuid NOT NULL,
        "AssignedAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_UserRoles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserRoles_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_UserRoles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AssetVerifications" (
        "Id" uuid NOT NULL,
        "AssetId" uuid NOT NULL,
        "VerificationCycleId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "VerifiedByUserId" uuid NOT NULL,
        "VerifiedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AssetVerifications" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AssetVerifications_VerificationCycles_VerificationCycleId" FOREIGN KEY ("VerificationCycleId") REFERENCES "VerificationCycles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "WorkflowInstances" (
        "Id" uuid NOT NULL,
        "DefinitionId" uuid NOT NULL,
        "DefinitionVersion" integer NOT NULL,
        "InstanceName" character varying(200) NOT NULL,
        "Status" integer NOT NULL,
        "CurrentNodeId" character varying(100) NOT NULL,
        "TriggeredByUserId" uuid,
        "TriggerType" integer NOT NULL,
        "ContextJson" jsonb NOT NULL,
        "VariablesJson" jsonb NOT NULL,
        "StartedAt" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        "ErrorMessage" character varying(2000),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_WorkflowInstances" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_WorkflowInstances_WorkflowDefinitions_DefinitionId" FOREIGN KEY ("DefinitionId") REFERENCES "WorkflowDefinitions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AcademicHolidays" (
        "Id" uuid NOT NULL,
        "SemesterId" uuid,
        "AcademicYearId" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "StartDate" date NOT NULL,
        "EndDate" date NOT NULL,
        "Type" character varying(50) NOT NULL,
        CONSTRAINT "PK_AcademicHolidays" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AcademicHolidays_AcademicYears_AcademicYearId" FOREIGN KEY ("AcademicYearId") REFERENCES "AcademicYears" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_AcademicHolidays_Semesters_SemesterId" FOREIGN KEY ("SemesterId") REFERENCES "Semesters" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AccreditationEvidences" (
        "Id" uuid NOT NULL,
        "AccreditationCriterionId" uuid NOT NULL,
        "DepartmentId" uuid NOT NULL,
        "FileUrl" character varying(500),
        "Description" character varying(1000),
        "UploadedByUserId" uuid NOT NULL,
        "UploadedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AccreditationEvidences" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AccreditationEvidences_AccreditationCriteria_AccreditationC~" FOREIGN KEY ("AccreditationCriterionId") REFERENCES "AccreditationCriteria" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AccreditationAuditLogs" (
        "Id" uuid NOT NULL,
        "AccreditationCycleId" uuid NOT NULL,
        "Action" character varying(50) NOT NULL,
        "Details" character varying(500),
        "PerformedByUserId" uuid NOT NULL,
        "PerformedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AccreditationAuditLogs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AccreditationAuditLogs_AccreditationCycles_AccreditationCyc~" FOREIGN KEY ("AccreditationCycleId") REFERENCES "AccreditationCycles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AccreditationCommittees" (
        "Id" uuid NOT NULL,
        "AccreditationCycleId" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AccreditationCommittees" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AccreditationCommittees_AccreditationCycles_AccreditationCy~" FOREIGN KEY ("AccreditationCycleId") REFERENCES "AccreditationCycles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "SelfStudyReports" (
        "Id" uuid NOT NULL,
        "AccreditationCycleId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "SubmittedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_SelfStudyReports" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SelfStudyReports_AccreditationCycles_AccreditationCycleId" FOREIGN KEY ("AccreditationCycleId") REFERENCES "AccreditationCycles" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CourseMarks" (
        "Id" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "AssessmentComponentId" uuid NOT NULL,
        "ObtainedMarks" integer NOT NULL,
        "EntryDate" timestamp with time zone NOT NULL,
        "EnteredBy" uuid NOT NULL,
        CONSTRAINT "PK_CourseMarks" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CourseMarks_AssessmentComponents_AssessmentComponentId" FOREIGN KEY ("AssessmentComponentId") REFERENCES "AssessmentComponents" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "EnergyReadings" (
        "Id" uuid NOT NULL,
        "BuildingId" uuid,
        "SensorId" uuid,
        "ReadingType" integer NOT NULL,
        "Consumption" numeric(14,4) NOT NULL,
        "Unit" character varying(20),
        "ReadingDate" timestamp with time zone NOT NULL,
        "Cost" numeric(14,4),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_EnergyReadings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EnergyReadings_Buildings_BuildingId" FOREIGN KEY ("BuildingId") REFERENCES "Buildings" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Floors" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "FloorNumber" integer NOT NULL,
        "BuildingId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Floors" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Floors_Buildings_BuildingId" FOREIGN KEY ("BuildingId") REFERENCES "Buildings" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "AuditFindings" (
        "Id" uuid NOT NULL,
        "ControlId" uuid NOT NULL,
        "Title" character varying(300) NOT NULL,
        "Description" character varying(2000) NOT NULL,
        "Severity" integer NOT NULL,
        "Status" integer NOT NULL,
        "IdentifiedByUserId" uuid NOT NULL,
        "IdentifiedByName" character varying(200),
        "IdentifiedAt" timestamp with time zone NOT NULL,
        "TargetResolutionDate" timestamp with time zone,
        "ResolvedAt" timestamp with time zone,
        "ResolutionNotes" character varying(2000),
        "IsLegalHold" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_AuditFindings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AuditFindings_ComplianceControls_ControlId" FOREIGN KEY ("ControlId") REFERENCES "ComplianceControls" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ControlEvidences" (
        "Id" uuid NOT NULL,
        "ControlId" uuid NOT NULL,
        "FileUrl" character varying(500) NOT NULL,
        "Description" character varying(1000) NOT NULL,
        "EvidenceType" character varying(100),
        "UploadedByUserId" uuid NOT NULL,
        "UploadedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ControlEvidences" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ControlEvidences_ComplianceControls_ControlId" FOREIGN KEY ("ControlId") REFERENCES "ComplianceControls" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "KPIAlerts" (
        "Id" uuid NOT NULL,
        "DashboardWidgetId" uuid NOT NULL,
        "AlertName" character varying(200) NOT NULL,
        "AlertCondition" integer NOT NULL,
        "Threshold" numeric(18,4) NOT NULL,
        "IsActive" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_KPIAlerts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_KPIAlerts_DashboardWidgets_DashboardWidgetId" FOREIGN KEY ("DashboardWidgetId") REFERENCES "DashboardWidgets" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "DataExportLogs" (
        "Id" uuid NOT NULL,
        "BIReportId" uuid,
        "DashboardId" uuid,
        "Format" integer NOT NULL,
        "RowCount" integer NOT NULL,
        "ExportedByUserId" uuid NOT NULL,
        "ExportedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_DataExportLogs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DataExportLogs_BIReports_BIReportId" FOREIGN KEY ("BIReportId") REFERENCES "BIReports" ("Id"),
        CONSTRAINT "FK_DataExportLogs_Dashboards_DashboardId" FOREIGN KEY ("DashboardId") REFERENCES "Dashboards" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Students" (
        "Id" uuid NOT NULL,
        "FirstName" character varying(100) NOT NULL,
        "LastName" character varying(100) NOT NULL,
        "UniversityId" character varying(50),
        "EnrollmentStatus" integer NOT NULL,
        "BatchId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Students" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Students_Batches_BatchId" FOREIGN KEY ("BatchId") REFERENCES "Batches" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CloPlomappings" (
        "Id" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "CloCode" character varying(50) NOT NULL,
        "PloCode" character varying(50) NOT NULL,
        "AlignmentLevel" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_CloPlomappings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CloPlomappings_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ExamSchedules" (
        "Id" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "ExamType" integer NOT NULL,
        "ScheduledDateTime" timestamp with time zone NOT NULL,
        "Venue" character varying(100),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ExamSchedules" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ExamSchedules_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CourseAssignments" (
        "Id" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "FacultyId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_CourseAssignments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CourseAssignments_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_CourseAssignments_Faculties_FacultyId" FOREIGN KEY ("FacultyId") REFERENCES "Faculties" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Timetables" (
        "Id" uuid NOT NULL,
        "BatchId" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "FacultyId" uuid NOT NULL,
        "DayOfWeek" integer NOT NULL,
        "StartTime" interval NOT NULL,
        "EndTime" interval NOT NULL,
        "Room" character varying(100),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Timetables" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Timetables_Batches_BatchId" FOREIGN KEY ("BatchId") REFERENCES "Batches" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Timetables_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Timetables_Faculties_FacultyId" FOREIGN KEY ("FacultyId") REFERENCES "Faculties" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "LabBookings" (
        "Id" uuid NOT NULL,
        "ResearchLabId" uuid NOT NULL,
        "BookedByUserId" uuid NOT NULL,
        "StartTime" timestamp with time zone NOT NULL,
        "EndTime" timestamp with time zone NOT NULL,
        "Purpose" character varying(500) NOT NULL,
        "ExpectedAttendees" integer NOT NULL,
        "Status" integer NOT NULL,
        "ApprovedByUserId" uuid,
        "ApprovedAt" timestamp with time zone,
        "RejectionReason" character varying(500) NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_LabBookings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_LabBookings_ResearchLabs_ResearchLabId" FOREIGN KEY ("ResearchLabId") REFERENCES "ResearchLabs" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "LabConsumables" (
        "Id" uuid NOT NULL,
        "ResearchLabId" uuid NOT NULL,
        "ItemName" character varying(200) NOT NULL,
        "Category" character varying(100) NOT NULL,
        "Unit" character varying(50) NOT NULL,
        "QuantityInStock" numeric NOT NULL,
        "MinimumStockLevel" numeric NOT NULL,
        "ReorderQuantity" numeric NOT NULL,
        "StorageLocation" character varying(100) NOT NULL,
        "ExpiryDate" timestamp with time zone,
        "IsHazardous" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_LabConsumables" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_LabConsumables_ResearchLabs_ResearchLabId" FOREIGN KEY ("ResearchLabId") REFERENCES "ResearchLabs" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "LabEquipments" (
        "Id" uuid NOT NULL,
        "ResearchLabId" uuid NOT NULL,
        "EquipmentName" character varying(200) NOT NULL,
        "AssetTag" character varying(50) NOT NULL,
        "Model" character varying(100) NOT NULL,
        "Manufacturer" character varying(100) NOT NULL,
        "SerialNumber" character varying(50) NOT NULL,
        "PurchaseDate" timestamp with time zone,
        "PurchaseCost" numeric(18,2) NOT NULL,
        "Status" integer NOT NULL,
        "Specifications" character varying(500) NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_LabEquipments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_LabEquipments_ResearchLabs_ResearchLabId" FOREIGN KEY ("ResearchLabId") REFERENCES "ResearchLabs" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "LabSafetyInspections" (
        "Id" uuid NOT NULL,
        "ResearchLabId" uuid NOT NULL,
        "InspectionDate" timestamp with time zone NOT NULL,
        "InspectedByUserId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "Findings" character varying(1000) NOT NULL,
        "CorrectiveActions" character varying(1000) NOT NULL,
        "CorrectiveActionsDue" timestamp with time zone,
        "IsCompliant" boolean NOT NULL,
        "SignedOffByUserId" uuid,
        "SignedOffAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_LabSafetyInspections" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_LabSafetyInspections_ResearchLabs_ResearchLabId" FOREIGN KEY ("ResearchLabId") REFERENCES "ResearchLabs" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "GuardRounds" (
        "Id" uuid NOT NULL,
        "SecurityGuardId" uuid NOT NULL,
        "RoundStartTime" timestamp with time zone NOT NULL,
        "RoundEndTime" timestamp with time zone,
        "Notes" character varying(500) NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_GuardRounds" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_GuardRounds_SecurityGuards_SecurityGuardId" FOREIGN KEY ("SecurityGuardId") REFERENCES "SecurityGuards" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "GuardShifts" (
        "Id" uuid NOT NULL,
        "SecurityGuardId" uuid NOT NULL,
        "ShiftDate" timestamp with time zone NOT NULL,
        "StartTime" timestamp with time zone NOT NULL,
        "EndTime" timestamp with time zone NOT NULL,
        "Post" character varying(100) NOT NULL,
        "Gate" character varying(100) NOT NULL,
        "Status" integer NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_GuardShifts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_GuardShifts_SecurityGuards_SecurityGuardId" FOREIGN KEY ("SecurityGuardId") REFERENCES "SecurityGuards" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "HostelRooms" (
        "Id" uuid NOT NULL,
        "HostelFloorId" uuid NOT NULL,
        "RoomNumber" character varying(50) NOT NULL,
        "RoomType" integer NOT NULL,
        "Status" integer NOT NULL,
        "Capacity" integer NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_HostelRooms" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_HostelRooms_HostelFloors_HostelFloorId" FOREIGN KEY ("HostelFloorId") REFERENCES "HostelFloors" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "NotificationRecords" (
        "Id" uuid NOT NULL,
        "InstanceId" uuid NOT NULL,
        "Channel" integer NOT NULL,
        "Recipient" character varying(500) NOT NULL,
        "Subject" character varying(300) NOT NULL,
        "Body" character varying(5000) NOT NULL,
        "Status" integer NOT NULL,
        "SentAt" timestamp with time zone,
        "ErrorMessage" character varying(1000),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_NotificationRecords" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_NotificationRecords_WorkflowInstances_InstanceId" FOREIGN KEY ("InstanceId") REFERENCES "WorkflowInstances" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "WorkflowSteps" (
        "Id" uuid NOT NULL,
        "InstanceId" uuid NOT NULL,
        "NodeId" character varying(100) NOT NULL,
        "NodeLabel" character varying(200) NOT NULL,
        "NodeType" integer NOT NULL,
        "Status" integer NOT NULL,
        "Sequence" integer NOT NULL,
        "StartedAt" timestamp with time zone,
        "CompletedAt" timestamp with time zone,
        "InputJson" jsonb NOT NULL,
        "OutputJson" jsonb NOT NULL,
        "ErrorMessage" character varying(2000),
        "RetryCount" integer NOT NULL,
        CONSTRAINT "PK_WorkflowSteps" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_WorkflowSteps_WorkflowInstances_InstanceId" FOREIGN KEY ("InstanceId") REFERENCES "WorkflowInstances" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "EvidenceVersions" (
        "Id" uuid NOT NULL,
        "AccreditationEvidenceId" uuid NOT NULL,
        "VersionNumber" integer NOT NULL,
        "FileUrl" character varying(500),
        "UploadedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_EvidenceVersions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EvidenceVersions_AccreditationEvidences_AccreditationEviden~" FOREIGN KEY ("AccreditationEvidenceId") REFERENCES "AccreditationEvidences" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CommitteeMeetings" (
        "Id" uuid NOT NULL,
        "AccreditationCommitteeId" uuid NOT NULL,
        "MeetingDate" timestamp with time zone NOT NULL,
        "Minutes" character varying(5000),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_CommitteeMeetings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CommitteeMeetings_AccreditationCommittees_AccreditationComm~" FOREIGN KEY ("AccreditationCommitteeId") REFERENCES "AccreditationCommittees" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CommitteeMembers" (
        "Id" uuid NOT NULL,
        "AccreditationCommitteeId" uuid NOT NULL,
        "FacultyId" uuid NOT NULL,
        "Role" integer NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_CommitteeMembers" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CommitteeMembers_AccreditationCommittees_AccreditationCommi~" FOREIGN KEY ("AccreditationCommitteeId") REFERENCES "AccreditationCommittees" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "SSRReviews" (
        "Id" uuid NOT NULL,
        "SelfStudyReportId" uuid NOT NULL,
        "ReviewedByUserId" uuid NOT NULL,
        "Comments" character varying(2000),
        "Status" integer NOT NULL,
        "ReviewedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_SSRReviews" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SSRReviews_SelfStudyReports_SelfStudyReportId" FOREIGN KEY ("SelfStudyReportId") REFERENCES "SelfStudyReports" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "SSRSections" (
        "Id" uuid NOT NULL,
        "SelfStudyReportId" uuid NOT NULL,
        "Title" character varying(200) NOT NULL,
        "Content" character varying(10000),
        "Status" integer NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_SSRSections" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SSRSections_SelfStudyReports_SelfStudyReportId" FOREIGN KEY ("SelfStudyReportId") REFERENCES "SelfStudyReports" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "FloorPlans" (
        "Id" uuid NOT NULL,
        "FloorId" uuid NOT NULL,
        "ImageUrl" character varying(500) NOT NULL,
        "RoomsLayoutJson" jsonb NOT NULL,
        "ImageWidth" integer NOT NULL,
        "ImageHeight" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_FloorPlans" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FloorPlans_Floors_FloorId" FOREIGN KEY ("FloorId") REFERENCES "Floors" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Rooms" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "RoomNumber" character varying(50),
        "FloorId" uuid NOT NULL,
        "Capacity" integer NOT NULL,
        "RoomType" character varying(100),
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Rooms" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Rooms_Floors_FloorId" FOREIGN KEY ("FloorId") REFERENCES "Floors" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CorrectiveActionPlans" (
        "Id" uuid NOT NULL,
        "FindingId" uuid NOT NULL,
        "Description" character varying(500) NOT NULL,
        "AssignedToUserId" uuid NOT NULL,
        "AssignedToName" character varying(200),
        "Status" integer NOT NULL,
        "DueDate" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        "CompletionNotes" character varying(2000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_CorrectiveActionPlans" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CorrectiveActionPlans_AuditFindings_FindingId" FOREIGN KEY ("FindingId") REFERENCES "AuditFindings" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CreditTransfers" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "ExternalCourseName" character varying(200) NOT NULL,
        "ExternalCourseCode" character varying(50),
        "ExternalInstitution" character varying(200) NOT NULL,
        "CreditHours" integer NOT NULL,
        "InternalCourseId" uuid,
        "InternalCourseCode" character varying(50),
        "IsApproved" boolean NOT NULL,
        "ApprovedAt" timestamp with time zone,
        "ApprovedBy" character varying(200),
        "Notes" character varying(1000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_CreditTransfers" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CreditTransfers_Students_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Students" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "DegreeAudits" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "ProgramId" uuid NOT NULL,
        "RequirementId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "EarnedValue" numeric,
        "RequiredValue" numeric,
        "CompletedCount" integer,
        "RequiredCount" integer,
        "Notes" character varying(500),
        "LastEvaluatedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_DegreeAudits" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DegreeAudits_DegreePrograms_ProgramId" FOREIGN KEY ("ProgramId") REFERENCES "DegreePrograms" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_DegreeAudits_DegreeRequirements_RequirementId" FOREIGN KEY ("RequirementId") REFERENCES "DegreeRequirements" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_DegreeAudits_Students_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Students" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Enrollments" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "Grade" character varying(5),
        "GpaPoints" numeric,
        "EnrollmentStatus" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Enrollments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Enrollments_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Enrollments_Students_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Students" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "StudentTransfers" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "SourceCampusId" uuid NOT NULL,
        "DestinationCampusId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "Reason" character varying(2000),
        "SourceApprovedBy" character varying(500),
        "SourceApprovedAt" timestamp with time zone,
        "DestApprovedBy" character varying(500),
        "DestApprovedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_StudentTransfers" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_StudentTransfers_Students_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Students" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Visitors" (
        "Id" uuid NOT NULL,
        "VisitorName" character varying(100) NOT NULL,
        "CNIC" character varying(20) NOT NULL,
        "Phone" character varying(20) NOT NULL,
        "Company" character varying(100) NOT NULL,
        "Purpose" character varying(200) NOT NULL,
        "HostEmployeeId" uuid,
        "HostStudentId" uuid,
        "Type" integer NOT NULL,
        "Status" integer NOT NULL,
        "CheckInTime" timestamp with time zone,
        "CheckOutTime" timestamp with time zone,
        "BadgeNumber" character varying(50) NOT NULL,
        "IsBlacklisted" boolean NOT NULL,
        "BlacklistReason" character varying(500) NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_Visitors" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Visitors_Employees_HostEmployeeId" FOREIGN KEY ("HostEmployeeId") REFERENCES "Employees" ("Id"),
        CONSTRAINT "FK_Visitors_Students_HostStudentId" FOREIGN KEY ("HostStudentId") REFERENCES "Students" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ExamSeatings" (
        "Id" uuid NOT NULL,
        "ExamScheduleId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "SeatNumber" integer NOT NULL,
        "Classroom" character varying(50),
        "RowNumber" integer NOT NULL,
        "ColumnNumber" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ExamSeatings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ExamSeatings_ExamSchedules_ExamScheduleId" FOREIGN KEY ("ExamScheduleId") REFERENCES "ExamSchedules" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_ExamSeatings_Students_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Students" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ConsumableRequisitions" (
        "Id" uuid NOT NULL,
        "LabConsumableId" uuid NOT NULL,
        "RequestedByUserId" uuid NOT NULL,
        "Quantity" numeric NOT NULL,
        "RequestedAt" timestamp with time zone NOT NULL,
        "Status" integer NOT NULL,
        "ApprovedByUserId" uuid,
        "ApprovedAt" timestamp with time zone,
        "IssuedByUserId" uuid,
        "IssuedAt" timestamp with time zone,
        "Notes" character varying(500) NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ConsumableRequisitions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ConsumableRequisitions_LabConsumables_LabConsumableId" FOREIGN KEY ("LabConsumableId") REFERENCES "LabConsumables" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CalibrationRecords" (
        "Id" uuid NOT NULL,
        "LabEquipmentId" uuid NOT NULL,
        "CalibrationDate" timestamp with time zone NOT NULL,
        "NextCalibrationDue" timestamp with time zone NOT NULL,
        "CalibratedBy" character varying(200) NOT NULL,
        "Results" character varying(500) NOT NULL,
        "Status" integer NOT NULL,
        "CertificateUrl" character varying(500) NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_CalibrationRecords" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CalibrationRecords_LabEquipments_LabEquipmentId" FOREIGN KEY ("LabEquipmentId") REFERENCES "LabEquipments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "EquipmentBookings" (
        "Id" uuid NOT NULL,
        "LabEquipmentId" uuid NOT NULL,
        "BookedByUserId" uuid NOT NULL,
        "StartTime" timestamp with time zone NOT NULL,
        "EndTime" timestamp with time zone NOT NULL,
        "Purpose" character varying(500) NOT NULL,
        "Status" integer NOT NULL,
        "ApprovedByUserId" uuid,
        "ApprovedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_EquipmentBookings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EquipmentBookings_LabEquipments_LabEquipmentId" FOREIGN KEY ("LabEquipmentId") REFERENCES "LabEquipments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "EquipmentMaintenances" (
        "Id" uuid NOT NULL,
        "LabEquipmentId" uuid NOT NULL,
        "IssueDescription" character varying(300) NOT NULL,
        "ReportedAt" timestamp with time zone NOT NULL,
        "ReportedByUserId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "AssignedToUserId" uuid,
        "CompletedAt" timestamp with time zone,
        "ResolutionNotes" character varying(500) NOT NULL,
        "Cost" numeric(18,2) NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_EquipmentMaintenances" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EquipmentMaintenances_LabEquipments_LabEquipmentId" FOREIGN KEY ("LabEquipmentId") REFERENCES "LabEquipments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "EquipmentUsageLogs" (
        "Id" uuid NOT NULL,
        "LabEquipmentId" uuid NOT NULL,
        "UsedByUserId" uuid NOT NULL,
        "StartTime" timestamp with time zone NOT NULL,
        "EndTime" timestamp with time zone,
        "Notes" character varying(500) NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_EquipmentUsageLogs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EquipmentUsageLogs_LabEquipments_LabEquipmentId" FOREIGN KEY ("LabEquipmentId") REFERENCES "LabEquipments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "SafetyChecklistItems" (
        "Id" uuid NOT NULL,
        "LabSafetyInspectionId" uuid NOT NULL,
        "ItemDescription" character varying(300) NOT NULL,
        "IsCompliant" boolean NOT NULL,
        "Remarks" character varying(500) NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SafetyChecklistItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SafetyChecklistItems_LabSafetyInspections_LabSafetyInspecti~" FOREIGN KEY ("LabSafetyInspectionId") REFERENCES "LabSafetyInspections" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "CheckpointScans" (
        "Id" uuid NOT NULL,
        "GuardRoundId" uuid NOT NULL,
        "CheckpointName" character varying(100) NOT NULL,
        "ScanTime" timestamp with time zone NOT NULL,
        "Latitude" numeric NOT NULL,
        "Longitude" numeric NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_CheckpointScans" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CheckpointScans_GuardRounds_GuardRoundId" FOREIGN KEY ("GuardRoundId") REFERENCES "GuardRounds" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "HostelAllocations" (
        "Id" uuid NOT NULL,
        "HostelRoomId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "CheckInDate" timestamp with time zone NOT NULL,
        "CheckOutDate" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_HostelAllocations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_HostelAllocations_HostelRooms_HostelRoomId" FOREIGN KEY ("HostelRoomId") REFERENCES "HostelRooms" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "RoomInspections" (
        "Id" uuid NOT NULL,
        "HostelRoomId" uuid NOT NULL,
        "InspectionType" integer NOT NULL,
        "InspectionDate" timestamp with time zone NOT NULL,
        "Findings" character varying(1000),
        "InspectedByUserId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_RoomInspections" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RoomInspections_HostelRooms_HostelRoomId" FOREIGN KEY ("HostelRoomId") REFERENCES "HostelRooms" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "UtilityReadings" (
        "Id" uuid NOT NULL,
        "HostelRoomId" uuid NOT NULL,
        "UtilityType" integer NOT NULL,
        "Reading" numeric(12,4) NOT NULL,
        "ReadingDate" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_UtilityReadings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UtilityReadings_HostelRooms_HostelRoomId" FOREIGN KEY ("HostelRoomId") REFERENCES "HostelRooms" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "ApprovalRecords" (
        "Id" uuid NOT NULL,
        "StepId" uuid NOT NULL,
        "ApproverUserId" uuid NOT NULL,
        "Sequence" integer NOT NULL,
        "Status" integer NOT NULL,
        "Comment" character varying(2000),
        "DelegatedToUserId" uuid,
        "DelegationReason" character varying(500),
        "ApproverRole" character varying(100) NOT NULL,
        "DecidedAt" timestamp with time zone,
        "TimeoutAt" timestamp with time zone NOT NULL,
        "IsEscalated" boolean NOT NULL,
        "EscalatedToUserId" uuid,
        CONSTRAINT "PK_ApprovalRecords" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ApprovalRecords_WorkflowSteps_StepId" FOREIGN KEY ("StepId") REFERENCES "WorkflowSteps" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "FireEquipments" (
        "Id" uuid NOT NULL,
        "EquipmentCode" character varying(50) NOT NULL,
        "EquipmentType" character varying(100) NOT NULL,
        "BuildingId" uuid,
        "RoomId" uuid,
        "Location" character varying(100) NOT NULL,
        "LastInspectionDate" timestamp with time zone,
        "NextInspectionDue" timestamp with time zone,
        "Status" integer NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_FireEquipments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FireEquipments_Buildings_BuildingId" FOREIGN KEY ("BuildingId") REFERENCES "Buildings" ("Id"),
        CONSTRAINT "FK_FireEquipments_Rooms_RoomId" FOREIGN KEY ("RoomId") REFERENCES "Rooms" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "MaintenanceTickets" (
        "Id" uuid NOT NULL,
        "TicketNumber" character varying(50) NOT NULL,
        "Title" character varying(300) NOT NULL,
        "Description" character varying(2000) NOT NULL,
        "Category" integer NOT NULL,
        "Priority" integer NOT NULL,
        "Status" integer NOT NULL,
        "AssetId" uuid,
        "RoomId" uuid,
        "BuildingId" uuid,
        "CreatedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ClosedAt" timestamp with time zone,
        "SLADeadline" timestamp with time zone,
        "IsSLABreached" boolean NOT NULL,
        "PhotoUrl" character varying(500) NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_MaintenanceTickets" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_MaintenanceTickets_Assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES "Assets" ("Id"),
        CONSTRAINT "FK_MaintenanceTickets_Buildings_BuildingId" FOREIGN KEY ("BuildingId") REFERENCES "Buildings" ("Id"),
        CONSTRAINT "FK_MaintenanceTickets_Rooms_RoomId" FOREIGN KEY ("RoomId") REFERENCES "Rooms" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "PreventiveMaintenanceSchedules" (
        "Id" uuid NOT NULL,
        "ScheduleName" character varying(200) NOT NULL,
        "Description" character varying(500) NOT NULL,
        "AssetId" uuid,
        "RoomId" uuid,
        "Category" integer NOT NULL,
        "Frequency" integer NOT NULL,
        "IntervalDays" integer NOT NULL,
        "LastPerformedAt" timestamp with time zone,
        "NextDueAt" timestamp with time zone,
        "IsActive" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_PreventiveMaintenanceSchedules" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PreventiveMaintenanceSchedules_Assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES "Assets" ("Id"),
        CONSTRAINT "FK_PreventiveMaintenanceSchedules_Rooms_RoomId" FOREIGN KEY ("RoomId") REFERENCES "Rooms" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "RoomOccupancies" (
        "Id" uuid NOT NULL,
        "RoomId" uuid NOT NULL,
        "State" integer NOT NULL,
        "CurrentOccupants" integer,
        "MaxCapacity" integer,
        "RecordedAt" timestamp with time zone NOT NULL,
        "CurrentEvent" character varying(200),
        "BookingUntil" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_RoomOccupancies" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RoomOccupancies_Rooms_RoomId" FOREIGN KEY ("RoomId") REFERENCES "Rooms" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "SecurityIncidents" (
        "Id" uuid NOT NULL,
        "IncidentNumber" character varying(50) NOT NULL,
        "Title" character varying(300) NOT NULL,
        "Description" character varying(2000) NOT NULL,
        "Type" integer NOT NULL,
        "Severity" integer NOT NULL,
        "Status" integer NOT NULL,
        "IncidentDateTime" timestamp with time zone NOT NULL,
        "LocationBuildingId" uuid,
        "LocationRoomId" uuid,
        "ReportedByUserId" uuid NOT NULL,
        "ReportedAt" timestamp with time zone NOT NULL,
        "AssignedToEmployeeId" uuid,
        "ResolvedAt" timestamp with time zone,
        "ResolutionNotes" character varying(1000) NOT NULL,
        "IsImmutable" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_SecurityIncidents" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SecurityIncidents_Buildings_LocationBuildingId" FOREIGN KEY ("LocationBuildingId") REFERENCES "Buildings" ("Id"),
        CONSTRAINT "FK_SecurityIncidents_Employees_AssignedToEmployeeId" FOREIGN KEY ("AssignedToEmployeeId") REFERENCES "Employees" ("Id"),
        CONSTRAINT "FK_SecurityIncidents_Rooms_LocationRoomId" FOREIGN KEY ("LocationRoomId") REFERENCES "Rooms" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Attendances" (
        "Id" uuid NOT NULL,
        "EnrollmentId" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "FacultyId" uuid NOT NULL,
        "Date" timestamp with time zone NOT NULL,
        "Status" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Attendances" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Attendances_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Attendances_Enrollments_EnrollmentId" FOREIGN KEY ("EnrollmentId") REFERENCES "Enrollments" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Attendances_Faculties_FacultyId" FOREIGN KEY ("FacultyId") REFERENCES "Faculties" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "Results" (
        "Id" uuid NOT NULL,
        "EnrollmentId" uuid NOT NULL,
        "CourseId" uuid NOT NULL,
        "TotalMarks" numeric NOT NULL,
        "ObtainedMarks" numeric NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Results" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Results_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Results_Enrollments_EnrollmentId" FOREIGN KEY ("EnrollmentId") REFERENCES "Enrollments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "VehiclePasses" (
        "Id" uuid NOT NULL,
        "VehicleNumber" character varying(20) NOT NULL,
        "VehicleModel" character varying(100) NOT NULL,
        "VehicleColor" character varying(50) NOT NULL,
        "PassType" integer NOT NULL,
        "OwnerEmployeeId" uuid,
        "OwnerStudentId" uuid,
        "VisitorId" uuid,
        "IssueDate" timestamp with time zone NOT NULL,
        "ExpiryDate" timestamp with time zone NOT NULL,
        "Status" integer NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_VehiclePasses" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_VehiclePasses_Employees_OwnerEmployeeId" FOREIGN KEY ("OwnerEmployeeId") REFERENCES "Employees" ("Id"),
        CONSTRAINT "FK_VehiclePasses_Students_OwnerStudentId" FOREIGN KEY ("OwnerStudentId") REFERENCES "Students" ("Id"),
        CONSTRAINT "FK_VehiclePasses_Visitors_VisitorId" FOREIGN KEY ("VisitorId") REFERENCES "Visitors" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "InspectionChecklistItems" (
        "Id" uuid NOT NULL,
        "RoomInspectionId" uuid NOT NULL,
        "ItemDescription" character varying(500) NOT NULL,
        "IsCompliant" boolean NOT NULL,
        "Remarks" character varying(500),
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_InspectionChecklistItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_InspectionChecklistItems_RoomInspections_RoomInspectionId" FOREIGN KEY ("RoomInspectionId") REFERENCES "RoomInspections" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "FireDrills" (
        "Id" uuid NOT NULL,
        "FireEquipmentId" uuid,
        "DrillDate" timestamp with time zone NOT NULL,
        "DrillType" character varying(100) NOT NULL,
        "ParticipantsCount" integer NOT NULL,
        "EvacuationTimeMinutes" numeric NOT NULL,
        "Observations" character varying(500) NOT NULL,
        "IsSuccessful" boolean NOT NULL,
        "ConductedByUserId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_FireDrills" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FireDrills_FireEquipments_FireEquipmentId" FOREIGN KEY ("FireEquipmentId") REFERENCES "FireEquipments" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "TicketComments" (
        "Id" uuid NOT NULL,
        "MaintenanceTicketId" uuid NOT NULL,
        "Comment" character varying(2000) NOT NULL,
        "CommentedByUserId" uuid NOT NULL,
        "CommentedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_TicketComments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TicketComments_MaintenanceTickets_MaintenanceTicketId" FOREIGN KEY ("MaintenanceTicketId") REFERENCES "MaintenanceTickets" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "TicketEscalations" (
        "Id" uuid NOT NULL,
        "MaintenanceTicketId" uuid NOT NULL,
        "EscalationLevel" integer NOT NULL,
        "EscalatedAt" timestamp with time zone NOT NULL,
        "EscalatedToUserId" uuid NOT NULL,
        "Reason" character varying(500) NOT NULL,
        "IsResolved" boolean NOT NULL,
        "ResolvedAt" timestamp with time zone,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_TicketEscalations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TicketEscalations_MaintenanceTickets_MaintenanceTicketId" FOREIGN KEY ("MaintenanceTicketId") REFERENCES "MaintenanceTickets" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "WorkOrders" (
        "Id" uuid NOT NULL,
        "WorkOrderNumber" character varying(50) NOT NULL,
        "MaintenanceTicketId" uuid NOT NULL,
        "AssignedToEmployeeId" uuid,
        "AssignedToContractorId" uuid,
        "Status" integer NOT NULL,
        "StartedAt" timestamp with time zone,
        "CompletedAt" timestamp with time zone,
        "SignedOffAt" timestamp with time zone,
        "SignedOffByUserId" uuid,
        "ResolutionNotes" character varying(1000) NOT NULL,
        "LaborCost" numeric(18,2) NOT NULL,
        "MaterialCost" numeric(18,2) NOT NULL,
        "TotalCost" numeric(18,2) NOT NULL,
        "HoursWorked" numeric NOT NULL,
        "IsPreventiveMaintenance" boolean NOT NULL,
        "PMScheduleId" uuid,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_WorkOrders" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_WorkOrders_Employees_AssignedToEmployeeId" FOREIGN KEY ("AssignedToEmployeeId") REFERENCES "Employees" ("Id"),
        CONSTRAINT "FK_WorkOrders_MaintenanceContractors_AssignedToContractorId" FOREIGN KEY ("AssignedToContractorId") REFERENCES "MaintenanceContractors" ("Id"),
        CONSTRAINT "FK_WorkOrders_MaintenanceTickets_MaintenanceTicketId" FOREIGN KEY ("MaintenanceTicketId") REFERENCES "MaintenanceTickets" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_WorkOrders_PreventiveMaintenanceSchedules_PMScheduleId" FOREIGN KEY ("PMScheduleId") REFERENCES "PreventiveMaintenanceSchedules" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "IncidentEvidences" (
        "Id" uuid NOT NULL,
        "SecurityIncidentId" uuid NOT NULL,
        "EvidenceType" character varying(100) NOT NULL,
        "FileUrl" character varying(500) NOT NULL,
        "Description" character varying(500) NOT NULL,
        "UploadedByUserId" uuid NOT NULL,
        "UploadedAt" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_IncidentEvidences" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_IncidentEvidences_SecurityIncidents_SecurityIncidentId" FOREIGN KEY ("SecurityIncidentId") REFERENCES "SecurityIncidents" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "IncidentWitnesses" (
        "Id" uuid NOT NULL,
        "SecurityIncidentId" uuid NOT NULL,
        "WitnessName" character varying(100) NOT NULL,
        "Phone" character varying(20) NOT NULL,
        "Statement" character varying(2000) NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_IncidentWitnesses" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_IncidentWitnesses_SecurityIncidents_SecurityIncidentId" FOREIGN KEY ("SecurityIncidentId") REFERENCES "SecurityIncidents" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "GateEntries" (
        "Id" uuid NOT NULL,
        "VehiclePassId" uuid,
        "VisitorId" uuid,
        "GateName" character varying(100) NOT NULL,
        "EntryTime" timestamp with time zone NOT NULL,
        "ExitTime" timestamp with time zone,
        "GuardEmployeeId" uuid,
        "Type" integer NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_GateEntries" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_GateEntries_Employees_GuardEmployeeId" FOREIGN KEY ("GuardEmployeeId") REFERENCES "Employees" ("Id"),
        CONSTRAINT "FK_GateEntries_VehiclePasses_VehiclePassId" FOREIGN KEY ("VehiclePassId") REFERENCES "VehiclePasses" ("Id"),
        CONSTRAINT "FK_GateEntries_Visitors_VisitorId" FOREIGN KEY ("VisitorId") REFERENCES "Visitors" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "WorkOrderMaterials" (
        "Id" uuid NOT NULL,
        "WorkOrderId" uuid NOT NULL,
        "MaterialName" character varying(200) NOT NULL,
        "Quantity" numeric NOT NULL,
        "Unit" character varying(50) NOT NULL,
        "UnitCost" numeric(18,2) NOT NULL,
        "TotalCost" numeric(18,2) NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_WorkOrderMaterials" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_WorkOrderMaterials_WorkOrders_WorkOrderId" FOREIGN KEY ("WorkOrderId") REFERENCES "WorkOrders" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE TABLE "WorkOrderTimeEntries" (
        "Id" uuid NOT NULL,
        "WorkOrderId" uuid NOT NULL,
        "EmployeeId" uuid NOT NULL,
        "StartTime" timestamp with time zone NOT NULL,
        "EndTime" timestamp with time zone,
        "HoursWorked" numeric NOT NULL,
        "Notes" character varying(500) NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_WorkOrderTimeEntries" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_WorkOrderTimeEntries_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_WorkOrderTimeEntries_WorkOrders_WorkOrderId" FOREIGN KEY ("WorkOrderId") REFERENCES "WorkOrders" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    INSERT INTO "PolicyConfigs" ("Id", "CreatedAt", "CreatedBy", "Description", "EffectiveFrom", "EffectiveTo", "Key", "Module", "UpdatedAt", "UpdatedBy", "Value", "ValueType")
    VALUES ('11111111-1111-1111-1111-111111111101', TIMESTAMPTZ '2026-01-01T00:00:00+00:00', '00000000-0000-0000-0000-000000000000', 'maximum % a dept head can change merit unilaterally', TIMESTAMPTZ '2026-01-01T00:00:00+00:00', NULL, 'merit_change_max_percent', 'admissions', NULL, NULL, '5', 'int');
    INSERT INTO "PolicyConfigs" ("Id", "CreatedAt", "CreatedBy", "Description", "EffectiveFrom", "EffectiveTo", "Key", "Module", "UpdatedAt", "UpdatedBy", "Value", "ValueType")
    VALUES ('11111111-1111-1111-1111-111111111102', TIMESTAMPTZ '2026-01-01T00:00:00+00:00', '00000000-0000-0000-0000-000000000000', 'VC approval required for merit overrides', TIMESTAMPTZ '2026-01-01T00:00:00+00:00', NULL, 'merit_override_requires_vc', 'admissions', NULL, NULL, 'true', 'bool');
    INSERT INTO "PolicyConfigs" ("Id", "CreatedAt", "CreatedBy", "Description", "EffectiveFrom", "EffectiveTo", "Key", "Module", "UpdatedAt", "UpdatedBy", "Value", "ValueType")
    VALUES ('11111111-1111-1111-1111-111111111103', TIMESTAMPTZ '2026-01-01T00:00:00+00:00', '00000000-0000-0000-0000-000000000000', 'Chancellor approval required for major merit overrides', TIMESTAMPTZ '2026-01-01T00:00:00+00:00', NULL, 'merit_override_requires_chancellor', 'admissions', NULL, NULL, 'true', 'bool');
    INSERT INTO "PolicyConfigs" ("Id", "CreatedAt", "CreatedBy", "Description", "EffectiveFrom", "EffectiveTo", "Key", "Module", "UpdatedAt", "UpdatedBy", "Value", "ValueType")
    VALUES ('11111111-1111-1111-1111-111111111104', TIMESTAMPTZ '2026-01-01T00:00:00+00:00', '00000000-0000-0000-0000-000000000000', 'maximum grace marks', TIMESTAMPTZ '2026-01-01T00:00:00+00:00', NULL, 'grace_marks_max', 'examination', NULL, NULL, '2', 'int');
    INSERT INTO "PolicyConfigs" ("Id", "CreatedAt", "CreatedBy", "Description", "EffectiveFrom", "EffectiveTo", "Key", "Module", "UpdatedAt", "UpdatedBy", "Value", "ValueType")
    VALUES ('11111111-1111-1111-1111-111111111105', TIMESTAMPTZ '2026-01-01T00:00:00+00:00', '00000000-0000-0000-0000-000000000000', 'attendance threshold for exam eligibility', TIMESTAMPTZ '2026-01-01T00:00:00+00:00', NULL, 'attendance_hold_threshold_percent', 'examination', NULL, NULL, '75', 'int');
    INSERT INTO "PolicyConfigs" ("Id", "CreatedAt", "CreatedBy", "Description", "EffectiveFrom", "EffectiveTo", "Key", "Module", "UpdatedAt", "UpdatedBy", "Value", "ValueType")
    VALUES ('11111111-1111-1111-1111-111111111106', TIMESTAMPTZ '2026-01-01T00:00:00+00:00', '00000000-0000-0000-0000-000000000000', 'course add/drop window', TIMESTAMPTZ '2026-01-01T00:00:00+00:00', NULL, 'add_drop_window_days', 'academic', NULL, NULL, '7', 'int');
    INSERT INTO "PolicyConfigs" ("Id", "CreatedAt", "CreatedBy", "Description", "EffectiveFrom", "EffectiveTo", "Key", "Module", "UpdatedAt", "UpdatedBy", "Value", "ValueType")
    VALUES ('11111111-1111-1111-1111-111111111107', TIMESTAMPTZ '2026-01-01T00:00:00+00:00', '00000000-0000-0000-0000-000000000000', 'late fee grace period', TIMESTAMPTZ '2026-01-01T00:00:00+00:00', NULL, 'late_fee_grace_days', 'finance', NULL, NULL, '3', 'int');
    INSERT INTO "PolicyConfigs" ("Id", "CreatedAt", "CreatedBy", "Description", "EffectiveFrom", "EffectiveTo", "Key", "Module", "UpdatedAt", "UpdatedBy", "Value", "ValueType")
    VALUES ('11111111-1111-1111-1111-111111111108', TIMESTAMPTZ '2026-01-01T00:00:00+00:00', '00000000-0000-0000-0000-000000000000', 'daily license heartbeat time', TIMESTAMPTZ '2026-01-01T00:00:00+00:00', NULL, 'heartbeat_time', 'license', NULL, NULL, '08:00', 'string');
    INSERT INTO "PolicyConfigs" ("Id", "CreatedAt", "CreatedBy", "Description", "EffectiveFrom", "EffectiveTo", "Key", "Module", "UpdatedAt", "UpdatedBy", "Value", "ValueType")
    VALUES ('11111111-1111-1111-1111-111111111109', TIMESTAMPTZ '2026-01-01T00:00:00+00:00', '00000000-0000-0000-0000-000000000000', 'lock system on first failed heartbeat', TIMESTAMPTZ '2026-01-01T00:00:00+00:00', NULL, 'lockout_on_first_failure', 'license', NULL, NULL, 'true', 'bool');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AcademicHolidays_AcademicYearId_StartDate" ON "AcademicHolidays" ("AcademicYearId", "StartDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AcademicHolidays_SemesterId" ON "AcademicHolidays" ("SemesterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AcademicYears_IsCurrent" ON "AcademicYears" ("IsCurrent");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AccreditationAuditLogs_AccreditationCycleId" ON "AccreditationAuditLogs" ("AccreditationCycleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_AccreditationBodies_Code" ON "AccreditationBodies" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AccreditationCommittees_AccreditationCycleId" ON "AccreditationCommittees" ("AccreditationCycleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AccreditationCriteria_AccreditationBodyId" ON "AccreditationCriteria" ("AccreditationBodyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AccreditationCycles_AccreditationBodyId" ON "AccreditationCycles" ("AccreditationBodyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AccreditationEvidences_AccreditationCriterionId" ON "AccreditationEvidences" ("AccreditationCriterionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_ActivityParticipations_ActivityId_StudentId" ON "ActivityParticipations" ("ActivityId", "StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_AdmissionApplications_Cnic" ON "AdmissionApplications" ("Cnic");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AdmissionApplications_ProgramId_QuotaType_ApplicationStatus" ON "AdmissionApplications" ("ProgramId", "QuotaType", "ApplicationStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AdmissionLeads_Email" ON "AdmissionLeads" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AdmissionLeads_Phone" ON "AdmissionLeads" ("Phone");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AdvisorMeetings_AdvisorAssignmentId" ON "AdvisorMeetings" ("AdvisorAssignmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AdvisorNotes_AdvisorAssignmentId" ON "AdvisorNotes" ("AdvisorAssignmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AgenticGradeRuns_Confidence" ON "AgenticGradeRuns" ("Confidence");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AgenticGradeRuns_ScanId_Status" ON "AgenticGradeRuns" ("ScanId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_AlertRules_RuleName" ON "AlertRules" ("RuleName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AlumniDonations_AlumniProfileId" ON "AlumniDonations" ("AlumniProfileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_AlumniDonations_ReceiptNumber" ON "AlumniDonations" ("ReceiptNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_AlumniProfiles_AlumniId" ON "AlumniProfiles" ("AlumniId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_AlumniProfiles_UserId" ON "AlumniProfiles" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AnonymousFeedbackSubmissions_CourseId_FacultyId" ON "AnonymousFeedbackSubmissions" ("CourseId", "FacultyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AnonymousFeedbackSubmissions_SubmittedAt" ON "AnonymousFeedbackSubmissions" ("SubmittedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_ApplicationDocuments_ApplicationId_DocumentType" ON "ApplicationDocuments" ("ApplicationId", "DocumentType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_ApprovalRecords_StepId" ON "ApprovalRecords" ("StepId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_AssessmentComponents_SchemeId_OrderIndex" ON "AssessmentComponents" ("SchemeId", "OrderIndex");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_AssessmentSchemes_CourseId_SemesterId" ON "AssessmentSchemes" ("CourseId", "SemesterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Assets_AssetCategoryId" ON "Assets" ("AssetCategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AssetVerifications_VerificationCycleId" ON "AssetVerifications" ("VerificationCycleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Attendances_CourseId" ON "Attendances" ("CourseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Attendances_EnrollmentId" ON "Attendances" ("EnrollmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Attendances_FacultyId" ON "Attendances" ("FacultyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AuditFindings_ControlId" ON "AuditFindings" ("ControlId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AuditLogs_TenantId" ON "AuditLogs" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AuditLogs_Timestamp" ON "AuditLogs" ("Timestamp");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_AuditLogs_UserId" ON "AuditLogs" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    ALTER TABLE "AuditLogs" ENABLE ROW LEVEL SECURITY;
    CREATE POLICY audit_log_no_delete ON "AuditLogs" FOR DELETE USING (FALSE);
    CREATE POLICY audit_log_no_update ON "AuditLogs" FOR UPDATE USING (FALSE);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_BackupRecords_ConfigurationId" ON "BackupRecords" ("ConfigurationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Batches_DepartmentId" ON "Batches" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_BIReports_DataWarehouseTableId" ON "BIReports" ("DataWarehouseTableId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_BookCopies_Barcode" ON "BookCopies" ("Barcode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_BookCopies_LibraryBookId" ON "BookCopies" ("LibraryBookId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Buildings_CampusId" ON "Buildings" ("CampusId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CalibrationRecords_LabEquipmentId" ON "CalibrationRecords" ("LabEquipmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CampusConfigurations_CampusId" ON "CampusConfigurations" ("CampusId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_Campuses_CampusCode" ON "Campuses" ("CampusCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CareerEventRegistrations_CareerEventId" ON "CareerEventRegistrations" ("CareerEventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CareerEvents_EventDate" ON "CareerEvents" ("EventDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_ChartOfAccounts_AccountCode" ON "ChartOfAccounts" ("AccountCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_ChatbotMessages_ChatbotConversationId" ON "ChatbotMessages" ("ChatbotConversationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CheckpointScans_GuardRoundId" ON "CheckpointScans" ("GuardRoundId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CisUploads_CourseId_ProcessingStatus" ON "CisUploads" ("CourseId", "ProcessingStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CisUploads_HumanReviewStatus" ON "CisUploads" ("HumanReviewStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CloPlomappings_CourseId" ON "CloPlomappings" ("CourseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CLOPOMappings_CourseLearningOutcomeId" ON "CLOPOMappings" ("CourseLearningOutcomeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CLOPOMappings_ProgramOutcomeId" ON "CLOPOMappings" ("ProgramOutcomeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CommitteeMeetings_AccreditationCommitteeId" ON "CommitteeMeetings" ("AccreditationCommitteeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CommitteeMembers_AccreditationCommitteeId" ON "CommitteeMembers" ("AccreditationCommitteeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_ComplianceControls_FrameworkId" ON "ComplianceControls" ("FrameworkId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_ConfigAuditLogs_ConfigurationEntryId" ON "ConfigAuditLogs" ("ConfigurationEntryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_ConfigVersions_ConfigurationEntryId" ON "ConfigVersions" ("ConfigurationEntryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_ConsumableRequisitions_LabConsumableId" ON "ConsumableRequisitions" ("LabConsumableId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_ControlEvidences_ControlId" ON "ControlEvidences" ("ControlId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CorrectiveActionPlans_FindingId" ON "CorrectiveActionPlans" ("FindingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_CostCenters_Code" ON "CostCenters" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CourseAssignments_CourseId" ON "CourseAssignments" ("CourseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CourseAssignments_FacultyId" ON "CourseAssignments" ("FacultyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_CourseGrades_CourseId_StudentId" ON "CourseGrades" ("CourseId", "StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CourseGrades_ResultStatus" ON "CourseGrades" ("ResultStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_CourseLearningOutcomes_Code" ON "CourseLearningOutcomes" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CourseMarks_AssessmentComponentId" ON "CourseMarks" ("AssessmentComponentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_CourseMarks_CourseId_StudentId_AssessmentComponentId" ON "CourseMarks" ("CourseId", "StudentId", "AssessmentComponentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CourseReEnrollments_Status" ON "CourseReEnrollments" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_CourseReEnrollments_StudentId_CourseId_TargetSemesterId" ON "CourseReEnrollments" ("StudentId", "CourseId", "TargetSemesterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_Courses_Code" ON "Courses" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Courses_DepartmentId" ON "Courses" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Courses_PrerequisiteCourseId" ON "Courses" ("PrerequisiteCourseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CreditTransfers_StudentId" ON "CreditTransfers" ("StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CrossDeptPermissionRequests_PermissionId" ON "CrossDeptPermissionRequests" ("PermissionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CrossDeptPermissionRequests_Status" ON "CrossDeptPermissionRequests" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_CurriculumChangeHistory_CurriculumVersionId_ChangedAt" ON "CurriculumChangeHistory" ("CurriculumVersionId", "ChangedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_CurriculumCourses_CurriculumVersionId_CourseId_Semester" ON "CurriculumCourses" ("CurriculumVersionId", "CourseId", "Semester");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_CurriculumPrerequisites_CurriculumVersionId_CourseId_Prereq~" ON "CurriculumPrerequisites" ("CurriculumVersionId", "CourseId", "PrerequisiteCourseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_CurriculumVersions_ProgramId_VersionNumber" ON "CurriculumVersions" ("ProgramId", "VersionNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_CurriculumVersions_ProgramId_YearEffective" ON "CurriculumVersions" ("ProgramId", "YearEffective");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_DashboardSubscriptions_DashboardId" ON "DashboardSubscriptions" ("DashboardId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_DashboardWidgets_DashboardId" ON "DashboardWidgets" ("DashboardId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_DataExportLogs_BIReportId" ON "DataExportLogs" ("BIReportId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_DataExportLogs_DashboardId" ON "DataExportLogs" ("DashboardId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_DataWarehouseTables_TableName" ON "DataWarehouseTables" ("TableName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_DegreeAudits_ProgramId" ON "DegreeAudits" ("ProgramId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_DegreeAudits_RequirementId" ON "DegreeAudits" ("RequirementId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_DegreeAudits_StudentId" ON "DegreeAudits" ("StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_DegreeRequirements_ProgramId" ON "DegreeRequirements" ("ProgramId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_Departments_Code" ON "Departments" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_DriveRegistrations_CampusDriveId" ON "DriveRegistrations" ("CampusDriveId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_DriverShifts_TransportDriverId" ON "DriverShifts" ("TransportDriverId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_DWColumns_DataWarehouseTableId" ON "DWColumns" ("DataWarehouseTableId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_EmergencyAlerts_SentAt" ON "EmergencyAlerts" ("SentAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_EmergencyContacts_Phone" ON "EmergencyContacts" ("Phone");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_EmployeeContracts_EmployeeId" ON "EmployeeContracts" ("EmployeeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_EmployeeDocuments_EmployeeId" ON "EmployeeDocuments" ("EmployeeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_Employees_Email" ON "Employees" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_Employees_EmployeeCode" ON "Employees" ("EmployeeCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Employers_ContactEmail" ON "Employers" ("ContactEmail");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_EnergyReadings_BuildingId" ON "EnergyReadings" ("BuildingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Enrollments_CourseId" ON "Enrollments" ("CourseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_Enrollments_StudentId_CourseId" ON "Enrollments" ("StudentId", "CourseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_EquipmentBookings_LabEquipmentId" ON "EquipmentBookings" ("LabEquipmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_EquipmentMaintenances_LabEquipmentId" ON "EquipmentMaintenances" ("LabEquipmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_EquipmentUsageLogs_LabEquipmentId" ON "EquipmentUsageLogs" ("LabEquipmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_EvidenceVersions_AccreditationEvidenceId" ON "EvidenceVersions" ("AccreditationEvidenceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_ExamPaperBatches_ExamId_Status" ON "ExamPaperBatches" ("ExamId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_ExamPaperScans_BatchId_StudentId" ON "ExamPaperScans" ("BatchId", "StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_ExamPaperScans_QrPayload" ON "ExamPaperScans" ("QrPayload");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_ExamRegistrations_StudentId_CourseId" ON "ExamRegistrations" ("StudentId", "CourseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_ExamSchedules_CourseId" ON "ExamSchedules" ("CourseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_ExamSeatings_ExamScheduleId_SeatNumber" ON "ExamSeatings" ("ExamScheduleId", "SeatNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_ExamSeatings_StudentId" ON "ExamSeatings" ("StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_ExtensionInstallations_ExtensionId" ON "ExtensionInstallations" ("ExtensionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_ExtensionReviews_ExtensionId" ON "ExtensionReviews" ("ExtensionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Faculties_DepartmentId" ON "Faculties" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_FeeComponents_FeeStructureId_OrderIndex" ON "FeeComponents" ("FeeStructureId", "OrderIndex");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_FeePayments_StudentFeeId" ON "FeePayments" ("StudentFeeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_FeeStructures_AcademicYearId_ProgramId_Semester_FeeType" ON "FeeStructures" ("AcademicYearId", "ProgramId", "Semester", "FeeType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_FireDrills_FireEquipmentId" ON "FireDrills" ("FireEquipmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_FireEquipments_BuildingId" ON "FireEquipments" ("BuildingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_FireEquipments_EquipmentCode" ON "FireEquipments" ("EquipmentCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_FireEquipments_RoomId" ON "FireEquipments" ("RoomId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_FiscalPeriods_FiscalYearId" ON "FiscalPeriods" ("FiscalYearId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_FloorPlans_FloorId" ON "FloorPlans" ("FloorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Floors_BuildingId" ON "Floors" ("BuildingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_FormSubmissions_FormId" ON "FormSubmissions" ("FormId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_GateEntries_GuardEmployeeId" ON "GateEntries" ("GuardEmployeeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_GateEntries_VehiclePassId" ON "GateEntries" ("VehiclePassId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_GateEntries_VisitorId" ON "GateEntries" ("VisitorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_GPSLocations_TransportVehicleId" ON "GPSLocations" ("TransportVehicleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_GradeBands_GradeScaleId_LetterGrade" ON "GradeBands" ("GradeScaleId", "LetterGrade");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_GradeOverrideRequests_ApprovalStatus_ApprovalThreshold" ON "GradeOverrideRequests" ("ApprovalStatus", "ApprovalThreshold");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_GradeOverrideRequests_StudentId_CourseId_SemesterId" ON "GradeOverrideRequests" ("StudentId", "CourseId", "SemesterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_GrantExpenditures_ResearchGrantId" ON "GrantExpenditures" ("ResearchGrantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_GrievanceResponses_TicketId" ON "GrievanceResponses" ("TicketId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_GrievanceTickets_Status" ON "GrievanceTickets" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_GrievanceTickets_SubmitterId" ON "GrievanceTickets" ("SubmitterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_GrievanceTickets_TicketNumber" ON "GrievanceTickets" ("TicketNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_GuardRounds_SecurityGuardId" ON "GuardRounds" ("SecurityGuardId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_GuardShifts_SecurityGuardId" ON "GuardShifts" ("SecurityGuardId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_HostelAllocations_HostelRoomId" ON "HostelAllocations" ("HostelRoomId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_HostelFloors_HostelBlockId" ON "HostelFloors" ("HostelBlockId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_HostelRooms_HostelFloorId" ON "HostelRooms" ("HostelFloorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_HumanGradeReviews_GradeRunId_Status" ON "HumanGradeReviews" ("GradeRunId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_IncidentEvidences_SecurityIncidentId" ON "IncidentEvidences" ("SecurityIncidentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_IncidentWitnesses_SecurityIncidentId" ON "IncidentWitnesses" ("SecurityIncidentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_InspectionChecklistItems_RoomInspectionId" ON "InspectionChecklistItems" ("RoomInspectionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_JobApplications_JobPostingId" ON "JobApplications" ("JobPostingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_JobExecutions_JobId" ON "JobExecutions" ("JobId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_JournalEntries_EntryNumber" ON "JournalEntries" ("EntryNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_JournalEntryLines_JournalEntryId" ON "JournalEntryLines" ("JournalEntryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_KPIAlerts_DashboardWidgetId" ON "KPIAlerts" ("DashboardWidgetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_LabBookings_ResearchLabId" ON "LabBookings" ("ResearchLabId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_LabConsumables_ResearchLabId" ON "LabConsumables" ("ResearchLabId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_LabEquipments_ResearchLabId" ON "LabEquipments" ("ResearchLabId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_LabSafetyInspections_ResearchLabId" ON "LabSafetyInspections" ("ResearchLabId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_LeadActivities_AdmissionLeadId" ON "LeadActivities" ("AdmissionLeadId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_LibraryBooks_Author" ON "LibraryBooks" ("Author");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_LibraryBooks_Category" ON "LibraryBooks" ("Category");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_LibraryBooks_Title" ON "LibraryBooks" ("Title");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_LicenseStates_LockedAt" ON "LicenseStates" ("LockedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_LicenseStates_Status" ON "LicenseStates" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_LmsSyncLogs_CourseId" ON "LmsSyncLogs" ("CourseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_LmsSyncLogs_CourseId_SyncType" ON "LmsSyncLogs" ("CourseId", "SyncType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_LostFoundItems_Status" ON "LostFoundItems" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_MaintenanceTickets_AssetId" ON "MaintenanceTickets" ("AssetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_MaintenanceTickets_BuildingId" ON "MaintenanceTickets" ("BuildingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_MaintenanceTickets_RoomId" ON "MaintenanceTickets" ("RoomId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_MaintenanceTickets_TicketNumber" ON "MaintenanceTickets" ("TicketNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_MarkModerations_CourseId_SemesterId" ON "MarkModerations" ("CourseId", "SemesterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_MarkModerations_Status" ON "MarkModerations" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_MedicalVisits_StudentId" ON "MedicalVisits" ("StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_MentoringSessions_MentorAssignmentId" ON "MentoringSessions" ("MentorAssignmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_MeritListEntries_ApplicationId" ON "MeritListEntries" ("ApplicationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_MeritListEntries_MeritListId_MeritRank" ON "MeritListEntries" ("MeritListId", "MeritRank");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_MeritLists_ProgramId_SemesterId_QuotaType_RoundNumber" ON "MeritLists" ("ProgramId", "SemesterId", "QuotaType", "RoundNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_NoticeBoardItems_ExpiryDate" ON "NoticeBoardItems" ("ExpiryDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_NoticeBoardItems_Visibility" ON "NoticeBoardItems" ("Visibility");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_NotificationRecords_InstanceId" ON "NotificationRecords" ("InstanceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_OfflineAttendanceEvents_ClientEventId" ON "OfflineAttendanceEvents" ("ClientEventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_OfflineAttendanceEvents_CourseId_StudentId" ON "OfflineAttendanceEvents" ("CourseId", "StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_OfflineAttendanceEvents_WasOfflineAt" ON "OfflineAttendanceEvents" ("WasOfflineAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_OrderItems_BookOrderId" ON "OrderItems" ("BookOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_PaperDestructionManifests_BatchId" ON "PaperDestructionManifests" ("BatchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_PaperDestructionManifests_ManifestHash" ON "PaperDestructionManifests" ("ManifestHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_PatentInventors_ResearchPatentId" ON "PatentInventors" ("ResearchPatentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_PayrollRuns_Month" ON "PayrollRuns" ("Month");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Payslips_EmployeeId" ON "Payslips" ("EmployeeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_Payslips_PayrollRunId_EmployeeId" ON "Payslips" ("PayrollRunId", "EmployeeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_Permissions_Module_Action_Resource" ON "Permissions" ("Module", "Action", "Resource");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_PluginRevenueShares_ExtensionId" ON "PluginRevenueShares" ("ExtensionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_PolicyConfigs_Module_Key_EffectiveFrom" ON "PolicyConfigs" ("Module", "Key", "EffectiveFrom");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_PowerBiReports_PowerBiReportId" ON "PowerBiReports" ("PowerBiReportId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_PowerBiReports_TargetRole" ON "PowerBiReports" ("TargetRole");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_PreventiveMaintenanceSchedules_AssetId" ON "PreventiveMaintenanceSchedules" ("AssetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_PreventiveMaintenanceSchedules_RoomId" ON "PreventiveMaintenanceSchedules" ("RoomId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_ProcurementRequisitions_RequisitionNumber" ON "ProcurementRequisitions" ("RequisitionNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_ProgramOutcomes_Code" ON "ProgramOutcomes" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_PublicationAuthors_ResearchPublicationId" ON "PublicationAuthors" ("ResearchPublicationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_PurchaseOrders_PoNumber" ON "PurchaseOrders" ("PoNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_PurchaseOrders_Status" ON "PurchaseOrders" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_RequisitionLineItems_RequisitionId" ON "RequisitionLineItems" ("RequisitionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_ResearchDeliverables_ResearchProjectId" ON "ResearchDeliverables" ("ResearchProjectId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_ResearchLabs_DepartmentId" ON "ResearchLabs" ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_ResearchLabs_LabCode" ON "ResearchLabs" ("LabCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_ResearchLabs_LabSupervisorId" ON "ResearchLabs" ("LabSupervisorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_ResearchMilestones_ResearchProjectId" ON "ResearchMilestones" ("ResearchProjectId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_ResearchPatents_PatentNumber" ON "ResearchPatents" ("PatentNumber") WHERE "PatentNumber" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_ResearchProjectMembers_ResearchProjectId" ON "ResearchProjectMembers" ("ResearchProjectId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Results_CourseId" ON "Results" ("CourseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Results_EnrollmentId" ON "Results" ("EnrollmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_RolePermissions_PermissionId" ON "RolePermissions" ("PermissionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_RolePermissions_RoleId_PermissionId" ON "RolePermissions" ("RoleId", "PermissionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_Roles_Name" ON "Roles" ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_RoomInspections_HostelRoomId" ON "RoomInspections" ("HostelRoomId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_RoomOccupancies_RoomId" ON "RoomOccupancies" ("RoomId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Rooms_FloorId" ON "Rooms" ("FloorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_RouteStops_TransportRouteId" ON "RouteStops" ("TransportRouteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_RubricCriteria_RubricId" ON "RubricCriteria" ("RubricId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_RuleVersions_RuleId" ON "RuleVersions" ("RuleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_SafetyChecklistItems_LabSafetyInspectionId" ON "SafetyChecklistItems" ("LabSafetyInspectionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_SeatAllocations_ApplicationId" ON "SeatAllocations" ("ApplicationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_SeatAllocations_MeritListEntryId" ON "SeatAllocations" ("MeritListEntryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_SectionOverrideRequests_StudentId_CourseId_Status" ON "SectionOverrideRequests" ("StudentId", "CourseId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_SecurityGuards_BadgeNumber" ON "SecurityGuards" ("BadgeNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_SecurityGuards_EmployeeId" ON "SecurityGuards" ("EmployeeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_SecurityIncidents_AssignedToEmployeeId" ON "SecurityIncidents" ("AssignedToEmployeeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_SecurityIncidents_IncidentNumber" ON "SecurityIncidents" ("IncidentNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_SecurityIncidents_LocationBuildingId" ON "SecurityIncidents" ("LocationBuildingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_SecurityIncidents_LocationRoomId" ON "SecurityIncidents" ("LocationRoomId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_SelfStudyReports_AccreditationCycleId" ON "SelfStudyReports" ("AccreditationCycleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Semesters_AcademicYearId" ON "Semesters" ("AcademicYearId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Semesters_IsCurrent" ON "Semesters" ("IsCurrent");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_SensorReadings_SensorId" ON "SensorReadings" ("SensorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_SickLeaveApplications_StudentId_Status" ON "SickLeaveApplications" ("StudentId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_SLADefinitions_Category_Priority" ON "SLADefinitions" ("Category", "Priority");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_SocietyMemberships_SocietyId_StudentId" ON "SocietyMemberships" ("SocietyId", "StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_SsoSessions_ExpiresAt" ON "SsoSessions" ("ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_SsoSessions_UserId_IsActive" ON "SsoSessions" ("UserId", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_SSRReviews_SelfStudyReportId" ON "SSRReviews" ("SelfStudyReportId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_SSRSections_SelfStudyReportId" ON "SSRSections" ("SelfStudyReportId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_StudentActivities_ScheduledDate" ON "StudentActivities" ("ScheduledDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_StudentFeeDues_Status" ON "StudentFeeDues" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_StudentFeeDues_StudentId_SemesterId" ON "StudentFeeDues" ("StudentId", "SemesterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_StudentFeePayments_FeeDueId" ON "StudentFeePayments" ("FeeDueId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_StudentFeePayments_ReferenceNumber" ON "StudentFeePayments" ("ReferenceNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_StudentHealthRecords_StudentId" ON "StudentHealthRecords" ("StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_StudentProfiles_StudentId" ON "StudentProfiles" ("StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_StudentResumes_StudentId" ON "StudentResumes" ("StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Students_BatchId" ON "Students" ("BatchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_Students_UniversityId" ON "Students" ("UniversityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_StudentSocieties_SocietyName" ON "StudentSocieties" ("SocietyName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_StudentTransfers_StudentId" ON "StudentTransfers" ("StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_SystemConfigurations_ReplicationLagHours" ON "SystemConfigurations" ("ReplicationLagHours");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_SystemConfigurations_UniversityId_SubdomainName" ON "SystemConfigurations" ("UniversityId", "SubdomainName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_TaxSlabs_CountryPackId" ON "TaxSlabs" ("CountryPackId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_TeamsSyncLogs_SyncStatus" ON "TeamsSyncLogs" ("SyncStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_TeamsSyncLogs_TimetableEntryId" ON "TeamsSyncLogs" ("TimetableEntryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_TicketComments_MaintenanceTicketId" ON "TicketComments" ("MaintenanceTicketId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_TicketEscalations_MaintenanceTicketId" ON "TicketEscalations" ("MaintenanceTicketId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Timetables_BatchId" ON "Timetables" ("BatchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Timetables_CourseId" ON "Timetables" ("CourseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Timetables_FacultyId" ON "Timetables" ("FacultyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_Transcripts_VerificationHash" ON "Transcripts" ("VerificationHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_TranslationValues_TranslationKeyId" ON "TranslationValues" ("TranslationKeyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_TransportDrivers_LicenseNumber" ON "TransportDrivers" ("LicenseNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_UniversitySsoProfiles_Code" ON "UniversitySsoProfiles" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_UniversitySsoProfiles_Domain" ON "UniversitySsoProfiles" ("Domain");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_UniversitySsoProfiles_SsoProvider" ON "UniversitySsoProfiles" ("SsoProvider");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_UserRoles_RoleId" ON "UserRoles" ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_UserRoles_UserId_RoleId" ON "UserRoles" ("UserId", "RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_UserSecondFactors_UserId" ON "UserSecondFactors" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_UtilityReadings_HostelRoomId" ON "UtilityReadings" ("HostelRoomId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_VehiclePasses_OwnerEmployeeId" ON "VehiclePasses" ("OwnerEmployeeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_VehiclePasses_OwnerStudentId" ON "VehiclePasses" ("OwnerStudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_VehiclePasses_VehicleNumber" ON "VehiclePasses" ("VehicleNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_VehiclePasses_VisitorId" ON "VehiclePasses" ("VisitorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Vendors_VendorName" ON "Vendors" ("VendorName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Visitors_CNIC" ON "Visitors" ("CNIC");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Visitors_HostEmployeeId" ON "Visitors" ("HostEmployeeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_Visitors_HostStudentId" ON "Visitors" ("HostStudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_WorkflowInstances_DefinitionId" ON "WorkflowInstances" ("DefinitionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_WorkflowSteps_InstanceId" ON "WorkflowSteps" ("InstanceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_WorkOrderMaterials_WorkOrderId" ON "WorkOrderMaterials" ("WorkOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_WorkOrders_AssignedToContractorId" ON "WorkOrders" ("AssignedToContractorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_WorkOrders_AssignedToEmployeeId" ON "WorkOrders" ("AssignedToEmployeeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_WorkOrders_MaintenanceTicketId" ON "WorkOrders" ("MaintenanceTicketId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_WorkOrders_PMScheduleId" ON "WorkOrders" ("PMScheduleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE UNIQUE INDEX "IX_WorkOrders_WorkOrderNumber" ON "WorkOrders" ("WorkOrderNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_WorkOrderTimeEntries_EmployeeId" ON "WorkOrderTimeEntries" ("EmployeeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    CREATE INDEX "IX_WorkOrderTimeEntries_WorkOrderId" ON "WorkOrderTimeEntries" ("WorkOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260726160353_V1_0_AegisCoreBaseline') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260726160353_V1_0_AegisCoreBaseline', '10.0.0');
    END IF;
END $EF$;
COMMIT;
