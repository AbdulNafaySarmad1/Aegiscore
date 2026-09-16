import { RecordTable } from "@/components/RecordTable";
export default function StudentsPage() {
  return <RecordTable title="Students" description="Every enrolled student in the university register." endpoint="/api/students" columns={[{ key: "universityId", label: "Student number", mono: true }, { key: "firstName", label: "First name" }, { key: "lastName", label: "Last name" }, { key: "status", label: "Enrolment status" }]} />;
}
