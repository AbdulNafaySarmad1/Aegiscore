import type { Metadata } from "next";
import { WizardShell } from "@/components/wizard/WizardShell";

export const metadata: Metadata = {
  title: "University setup"
};

export default function SetupPage() {
  return <WizardShell />;
}
