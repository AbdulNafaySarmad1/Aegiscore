"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { apiGet } from "@/lib/api-client";

export default function StudentFinanceRedirect() {
  const router = useRouter();

  useEffect(() => {
    apiGet<{ studentId: string }>("/api/student-portal/me")
      .then(me => router.replace(`/student/finance/${encodeURIComponent(me.studentId)}`))
      .catch(() => router.replace("/login"));
  }, [router]);

  return <main style={{ display: "grid", placeItems: "center", minHeight: 200 }}><p>Loading fee status...</p></main>;
}
