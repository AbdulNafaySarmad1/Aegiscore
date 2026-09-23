"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { loadSession } from "@/lib/session";
import { usePersonalityTranslations } from "@/lib/personality-translator";

/** The student navigation links here ("Fee Status", "Pay Fee"); the statement lives under the student's own id. */
export default function StudentFinanceIndex() {
  const router = useRouter();
  const t = usePersonalityTranslations("finance");
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    loadSession()
      .then(session => session.studentId ? router.replace(`/student/finance/${encodeURIComponent(session.studentId)}`) : setFailed(true))
      .catch(() => setFailed(true));
  }, [router]);

  return <main id="main-content" className="finance-page" tabIndex={-1}>
    {failed ? <p className="finance-alert" role="alert">{t("studentUnavailable")}</p> : <p>{t("feesStatusLoading")}</p>}
  </main>;
}
