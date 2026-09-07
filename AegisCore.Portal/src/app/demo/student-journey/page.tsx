import { notFound } from "next/navigation";
import { StudentJourney } from "./student-journey";

export default function Page() {
  if (process.env.NEXT_PUBLIC_DEMO_MODE !== "true") notFound();
  return <StudentJourney />;
}
