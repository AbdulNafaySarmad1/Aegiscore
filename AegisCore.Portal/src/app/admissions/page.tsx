import { RecordTable } from "@/components/RecordTable";
export default function AdmissionsPage() {
  return <RecordTable title="Admissions" endpoint="/api/admissions/enquiries" columns={[{ key: "name", label: "Name" }, { key: "email", label: "Email" }, { key: "status", label: "Status" }]} />;
}
