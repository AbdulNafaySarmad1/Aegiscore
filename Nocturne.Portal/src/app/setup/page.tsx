import type { Metadata } from "next";
import { SetupWizard } from "@/components/setup-wizard";

export const metadata: Metadata = {
  title: "University setup"
};

export default function SetupPage() {
  return <SetupWizard />;
}
