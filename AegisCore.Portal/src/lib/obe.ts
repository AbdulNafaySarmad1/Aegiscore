import { apiGet } from "@/lib/api-client";

export type ObeEvidenceReference = {
  courseCode: string;
  courseTitle: string;
  programName: string;
  cloCode: string;
  cloDescription: string;
  poCode: string;
  poDescription: string;
  strength: string;
};

export function getObeEvidence(signal?: AbortSignal) {
  return apiGet<ObeEvidenceReference[]>("/api/obe/v2/evidence?limit=50", signal);
}
