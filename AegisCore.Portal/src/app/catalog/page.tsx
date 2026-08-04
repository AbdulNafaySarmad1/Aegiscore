import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function CatalogPage() {
  return <ModuleDashboard title="Course Catalog Ingestion" description="Intelligent document-to-database pipeline with OCR, validation, and versioning" apiPath="/api/catalog/ingest/jobs/00000000-0000-0000-0000-000000000000" metricKeys={["total"]} metricLabels={["Ingestion Jobs"]} />;
}
