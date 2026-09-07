import { RecordTable } from "@/components/RecordTable";
export default function StudentsPage() {
  return <RecordTable title="Students" endpoint="/api/students" columns={[{ key: "universityId", label: "Student number" }, { key: "firstName", label: "First name" }, { key: "lastName", label: "Last name" }, { key: "status", label: "Enrollment status" }]} />;
}
