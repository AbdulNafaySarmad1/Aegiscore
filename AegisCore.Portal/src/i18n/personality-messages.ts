import { type PortalLocale } from "./messages";

const en = {
  student: {
    "challan.paid.success": "Fee challan payment confirmed.",
    "challan.paid.success.personality":
      "Challan cleared hai, ab chill karo! 💸",
    "challan.expiry.warning": "Your fee challan will expire soon.",
    "challan.expiry.warning.personality":
      "Oye! Challan expire hone wala hai — jaldi karo warna late fee lagegi! ⚠️",
    "challan.generated.success": "Fee challan generated successfully.",
    "challan.generated.success.personality":
      "Challan ready hai! Ab bas pay karo aur scene set. 📋",
    "results.published": "Your examination results have been published.",
    "results.published.personality":
      "Results aa gaye, bhai! Check karo — dua karo GPA phaar ho. 📊",
    "dues.empty": "No outstanding dues.",
    "dues.empty.personality":
      "Zero balance, zero stress. You're all clear! 🔓",
    "payment.declined.error":
      "Payment was declined by the payment gateway.",
    "payment.declined.error.personality":
      "Payment decline ho gaya — bank walay kanjoos niklay. Dobara try karo ya kisi aur card se. 💳",
    "enrollment.confirmed":
      "Enrollment confirmed for the semester.",
    "enrollment.confirmed.personality":
      "Enrollment done hai! Naya semester, nayi energy — let's get it! 🎓",
    "fees.status.loading": "Loading fee status…",
    "fees.status.loading.personality":
      "Checking if you're broke or not… 👀",
  },
  faculty: {
    "claim.submit.success":
      "Reimbursement claim submitted successfully.",
    "claim.submit.success.personality":
      "Claim submit ho gaya! Ab bas paisa anay ka wait karo. 🧾",
    "payslip.ready": "Your monthly payslip is available.",
    "payslip.ready.personality":
      "Aap ki mehnat ka sila aa gaya — payslip ready hai! 💰",
    "leave.approved": "Your leave application has been approved.",
    "leave.approved.personality":
      "Chuttiyan approved! Enjoy karo, aap deserve karte hain. 🌴",
    "claims.empty": "No pending or past reimbursement claims.",
    "claims.empty.personality":
      "Koi claims nahi hain — either aap bhool gaye ya sab clear ho gaya. 🧹",
    "payslip.loading": "Loading payslip details…",
    "payslip.loading.personality":
      "Pulling your monthly bag… 💼",
  },
  academic: {
    "budget.expiry.warning":
      "Department budget is expiring soon.",
    "budget.expiry.warning.personality":
      "Budget expire hone wala hai — use it or lose it, yaar! 📉",
    "faculty.claim.approved":
      "Faculty reimbursement claim has been approved.",
    "faculty.claim.approved.personality":
      "Faculty ka claim approve ho gaya — paisa release hone ka time! ✅",
    "approvals.empty": "No pending approvals.",
    "approvals.empty.personality":
      "Sara kaam nipat gaya — koi approvals pending nahi. King/Queen behavior! 👑",
    "gradeAppeals.personality": "appeal your grade? we got you",
    "gradeAppealsDesc.personality": "bhai ne marks kaat diye? let's talk it out with the committee",
    "appealSubmissions.personality": "file your appeal, we'll listen",
    "appealsRegistry.personality": "sab appeals ki file — kaun kya kar raha hai?",
    "appeaLPipeline.personality": "appeals in the queue, let's process them",
    "submitAppeal.personality": "submit karo appeal, scene on karo",
    "submitting.personality": "appeal file ho rahi hai... thora wait",
    "loadingAppeals.personality": "appeals load ho rahi hain, ruko zara...",
    "noAppealsFiled.personality": "koi appeal nahi — sab khush hain ya bas bhool gaye?",
    "noAppealsDesc.personality": "first appeal submit karo, form upar hai bhai",
    "generateBrief.personality": "committee ko brief do — AI bhai summary likhega",
    "generatingBrief.personality": "AI apna kaam kar raha hai, brief aa raha hai...",
    "underReview.personality": "committee dekh rahi hai",
    "resolved.personality": "sorted hai, ho gaya",
    "gradeDistributions.personality": "grades ka scatter plot — let's see who aced it",
    "gradeDistributions.heading.personality": "grade distributions ka analysis — kon kitna score kar raha?",
    "runAnalysis.personality": "analysis chalao, dekhain kya scene hai",
    "analyzing.personality": "data crunch ho raha hai, ek second...",
    "selectCourse.personality": "course choose karo, phir dekhte hain",
    "mean.personality": "average score — neeche wale ro rahe hain",
    "median.personality": "middle child of grades",
    "stdDev.personality": "kitna spread hai? high variance = kuch ne phaar diya, kuch ne...",
    "passRate.personality": "pass hone walon ki tally",
    "failureRate.personality": "jo fail hue unka score",
    "anomalyAlerts.personality": "kuch garbar lagti hai? anomalies detected!",
    "noAnomaliesDetected.personality": "sab set hai, koi anomaly nahi",
    "noAnomaliesDesc.personality": "grade distributions normal hain — no drama here",
    "plagiarism.personality": "copy-paste kiya? hum pakar lein gay",
    "scanDocument.personality": "document scan karo — dekhain kahan se copy kiya",
    "scanning.personality": "scan chal raha hai, sab kuch check ho raha...",
    "similarityScore.personality": "kitna copy kiya? score dekh lo",
    "lowRisk.personality": "safe hai — mostly original",
    "moderate.personality": "hmmm, kuch toh garbar hai",
    "highRisk.personality": "bhai, yeh toh puri copy hai!",
    "collusionGraph.personality": "dost-dost ki setting? collusion check",
    "noCollusionPatterns.personality": "koi setting nahi pakri gayi — sab independent lag rahay",
    "documentScanner.personality": "apna text paste karo, scanner check karega",
    "matchedSources.personality": "kahan-kahan se copy kiya? sources list",
    "aiWarning.personality": "AI ne likha hai kya? threshold cross ho gaya, review karo!",
    "proctoring.personality": "online exam, nazar hum par hai",
    "createSession.personality": "new proctoring session shuru karo — cameras on!",
    "startSession.personality": "session start! ab koi cheat nahi karega",
    "faceAnalysis.personality": "chehra verify karo — asli banda hai ya duplicate?",
    "analyzeFace.personality": "face scan karo, dekhain match hota hai ya nahi",
    "lockdownValidation.personality": "browser locked hai? check karo sab sahi hai",
    "studentIntegrityGrid.personality": "kon saaf hai, kon nahi — integrity report card",
    "proctorEventLog.personality": "exam ke doran jo bhi hua, sab yahan likha hai",
    "noEventsDesc.personality": "all quiet on the exam front — keep it up!",
    "defaulterPrediction.personality": "fees kon nahi de raha? AI bata de ga",
    "defaulterPrediction.heading.personality": "who's about to ghost their fees?",
    "studentRiskLookup.personality": "kisi bhi student ka risk score check karo",
    "lookUp.personality": "search karo, dekhain kitna risk hai",
    "riskDrivers.personality": "kya reasons hain risk ke? AI ka analysis",
    "generateInterventions.personality": "har risk tier ke liye action plan banao",
    "generatePlan.personality": "AI plan bana raha hai...",
    "cashFlowForecast.personality": "aglay 6 mahine kitna paisa aane wala hai?",
    "noForecastData.personality": "forecast data available nahi hai — abhi tak sab ne fees de di?",
    "healthy.personality": "scene set hai, paisa aane wala hai",
    "caution.personality": "thori tension hai — keep an eye",
    "atRisk.personality": "bhai, yeh recovery mushkil lagti hai",
    "readiness.personality": "how ready are we? let's find out",
    "readiness.heading.personality": "accreditation kitni dur hai? readiness check",
    "computingReadiness.personality": "scores calculate ho rahe hain... thora sabar",
    "checkFreshness.personality": "evidence fresh hai ya purana? check karo",
    "overallScore.personality": "total readiness — number dekh kar pata chalega",
    "daysUntilAudit.personality": "audit anay mein kitna time baki?",
    "criteriaBreakdown.personality": "har criterion ka score — kahan kami hai?",
    "gapAnalysis.personality": "jo missing hai woh yahan likha hai",
    "noGapsIdentified.personality": "sab complete hai — total king move! 👑",
    "noGapsDesc.personality": "sare criteria meet ho gaye — accreditation ready!",
    "alertsFeed.personality": "latest updates — kya badla hai?",
    "allClear.personality": "sab clear, koi alert nahi — maze karo",
    "mentalHealth.personality": "dil ki baat, anonymously share karo",
    "mentalHealth.heading.personality": "mental health matters — let's talk about it",
    "anonymousTicket.personality": "apni baat batao, koi naam nahi puchega",
    "submitAnonymousTicket.personality": "send karo, hum sun rahe hain",
    "counselorQueue.personality": "counselor ke paas kitne cases hain?",
    "crisisTickets.personality": "emergency cases — immediate action chahiye",
    "immediateAttention.personality": "bhai, yeh urgent hai! abhi dekhna pare ga",
    "noActiveCrises.personality": "sab stable hai, alhamdulillah!",
    "noPendingTickets.personality": "queue empty — aaj chai peene ka din",
    "queueIsClear.personality": "psychologist ki chutti, no tickets pending",
    "fileMandatoryReport.personality": "official report file karni hai — serious matter",
    "noTicketsRequireReport.personality": "sab chill hai, no mandatory escalation needed",
    "mandatoryReportPanel.personality": "crisis cases yahan aatay hain — handle with care",
    "examinations.personality": "exam season — papers, marks, results, sab yahan",
    "examinations.heading.personality": "papers set hain? exams ka time!",
    "marksEntry.personality": "student ke marks enter karo — dua karo fail na ho",
    "recordMarks.personality": "marks save karo, phir result ready",
    "recording.personality": "marks save ho rahe hain...",
    "resultPublication.personality": "results publish karo — students ki dua milegi... ya bad-dua",
    "publishResults.personality": "publish! ab koi wapas nahi badal sakta",
    "publishing.personality": "results publish ho rahe hain...",
    "assessmentSchemes.personality": "exam mein kya-kya count hoga? scheme dekh lo",
    "noSchemesDefined.personality": "koi scheme nahi — bhai pehle components define karo!",
    "noPublicationSlots.personality": "koi slot configured nahi — results kahan publish honge?",
    "noSchemesDesc.personality": "assessment components banao, phir marks enter karo",
    "placement.personality": "internship lag gayi? placement tracker",
    "placement.heading.personality": "job lagwana hai? placement desk pe aao",
    "placementRate.personality": "kitne students ko job mili?",
    "studentReadiness.personality": "student job ke liye ready hai? check karo",
    "checkReadiness.personality": "skills scan karo, phir batayein kitni mehnat baqi hai",
    "readinessScore.personality": "employment readiness — number jitna ooncha, utni achi job",
    "skillAssessment.personality": "kaun si skill strong hai, kaun si weak",
    "partnerManagement.personality": "companies jo students hire karti hain",
    "noPartnerCompanies.personality": "koi company registered nahi — outreach time!",
    "activePlacements.personality": "kaun kahan placed hai? tracker",
    "noActivePlacements.personality": "abhi tak koi placed nahi — koi baat nahi, scene banega",
    "noActivePlacementsDesc.personality": "placement abhi tak nahi hui? market mein bhejo inko!",
    "achievements.personality": "students ke achievements — flex zone! 🏆",
    "convocations.personality": "graduation ceremony — cap and gown time",
    "freezes.personality": "semester freeze? let's pause the clock",
    "visitingFaculty.personality": "guest faculty hire karo — marketplace open hai",
    "elections.personality": "student elections — vote do, leader chuno",
    "boardPortal.personality": "board of governors ka secret lair",
    "foreignStudents.personality": "visa, immigration, sab manage karo",
    "voiceAssistant.personality": "NidaBot se baat karo — AI assistant ready hai",
    "catalogIngestion.personality": "course catalog upload karo — drag and drop",
    "classGeneration.personality": "naya semester, nayi classes — auto-generate karo",
    "cafeteria.personality": "aaj khane mein kya hai? menu check karo",
    "transport.personality": "buses ready hain? transport management",
    "prereqConflicts.personality": "prerequisite check — yeh course karne se pehle woh karna zaroori hai?",
  },
  admin: {
    "challan.verified.success":
      "Challan verification confirmed.",
    "challan.verified.success.personality":
      "Challan verify ho gaya, sab legit hai! 👍",
    "exam.fees.unpaid.warning":
      "Students have unpaid examination fees.",
    "exam.fees.unpaid.warning.personality":
      "Kuch students ke exam fees abhi tak pending hain — unko remind karo warna exams miss karein ge. 📢",
    "results.lock.confirm":
      "Are you sure you want to lock results? This action is irreversible.",
    "verifications.empty": "No pending challan verifications.",
    "verifications.empty.personality":
      "Sare challan verify ho gaye — clean slate, scene on! ✨",
  },
  staff: {
    "challan.batch.loading": "Loading challan batch data…",
    "challan.batch.loading.personality":
      "Challano ki puri fauj load ho rahi hai, thora sabar. 📦",
    "challan.overwrite.warning":
      "This action will overwrite existing challan records.",
    "challan.overwrite.warning.personality":
      "Puranay challan replace hone wale hain — sure ho? Dobara wapas nahi hoga! ⚠️",
    "batches.empty": "No challan batches to process.",
    "batches.empty.personality":
      "Koi challan batch pending nahi — aaj ka kaam khatam, ab chai peelo! ☕",
  },
};

const ar = {
  student: {
    "challan.paid.success": "\u062a\u0645 \u062a\u0623\u0643\u064a\u062f \u062f\u0641\u0639 \u0625\u0634\u0639\u0627\u0631 \u0627\u0644\u0631\u0633\u0648\u0645.",
    "challan.paid.success.personality":
      "\u062a\u0645\u062a \u062a\u0635\u0641\u064a\u0629 \u0627\u0644\u0625\u0634\u0639\u0627\u0631 \u2014 \u0627\u0633\u062a\u0631\u062d \u0627\u0644\u0622\u0646! \u2028\u2028",
    "challan.expiry.warning": "\u0633\u064a\u0646\u062a\u0647\u064a \u0635\u0644\u0627\u062d\u064a\u0629 \u0625\u0634\u0639\u0627\u0631 \u0627\u0644\u0631\u0633\u0648\u0645 \u0642\u0631\u064a\u0628\u0627\u064b.",
    "challan.expiry.warning.personality":
      "\u062a\u0646\u0628\u064a\u0647! \u0635\u0644\u0627\u062d\u064a\u0629 \u0627\u0644\u0625\u0634\u0639\u0627\u0631 \u0639\u0644\u0649 \u0648\u0634\u0643 \u0627\u0644\u0627\u0646\u062a\u0647\u0627\u0621 \u2014 \u0628\u0627\u062f\u0631 \u0628\u0627\u0644\u062f\u0641\u0639 \u0644\u062a\u062c\u0646\u0628 \u063a\u0631\u0627\u0645\u0629 \u0627\u0644\u062a\u0623\u062e\u064a\u0631. \u26a0\ufe0f",
    "challan.generated.success": "\u062a\u0645 \u0625\u0646\u0634\u0627\u0621 \u0625\u0634\u0639\u0627\u0631 \u0627\u0644\u0631\u0633\u0648\u0645 \u0628\u0646\u062c\u0627\u062d.",
    "challan.generated.success.personality":
      "\u0627\u0644\u0625\u0634\u0639\u0627\u0631 \u062c\u0627\u0647\u0632! \u0642\u0645 \u0628\u0627\u0644\u062f\u0641\u0639 \u0644\u0625\u062a\u0645\u0627\u0645 \u0627\u0644\u0639\u0645\u0644\u064a\u0629. \ud83d\udccb",
    "results.published": "\u062a\u0645 \u0646\u0634\u0631 \u0646\u062a\u0627\u0626\u062c \u0627\u0644\u0627\u0645\u062a\u062d\u0627\u0646\u0627\u062a.",
    "results.published.personality":
      "\u0638\u0647\u0631\u062a \u0627\u0644\u0646\u062a\u0627\u0626\u062c! \u062a\u0641\u0642\u062f\u0647\u0627 \u0627\u0644\u0622\u0646 \u2014 \u0646\u062a\u0645\u0646\u0649 \u0644\u0643 \u0627\u0644\u062a\u0648\u0641\u064a\u0642. \ud83d\udcca",
    "dues.empty": "\u0644\u0627 \u062a\u0648\u062c\u062f \u0645\u0633\u062a\u062d\u0642\u0627\u062a \u0645\u0639\u0644\u0642\u0629.",
    "dues.empty.personality":
      "\u0631\u0635\u064a\u062f\u0643 \u0635\u0641\u0631\u064a \u2014 \u0644\u0627 \u062a\u0648\u062c\u062f \u0623\u064a\u0629 \u0627\u0644\u062a\u0632\u0627\u0645\u0627\u062a \u0645\u0627\u0644\u064a\u0629. \ud83d\udd13",
    "payment.declined.error": "\u062a\u0645 \u0631\u0641\u0636 \u0627\u0644\u062f\u0641\u0639 \u0645\u0646 \u0642\u0628\u0644 \u0628\u0648\u0627\u0628\u0629 \u0627\u0644\u062f\u0641\u0639.",
    "payment.declined.error.personality":
      "\u062a\u0645 \u0631\u0641\u0636 \u0627\u0644\u0645\u0639\u0627\u0645\u0644\u0629 \u2014 \u064a\u0631\u062c\u0649 \u0627\u0644\u0645\u062d\u0627\u0648\u0644\u0629 \u0628\u0628\u0637\u0627\u0642\u0629 \u0623\u062e\u0631\u0649 \u0623\u0648 \u0627\u0644\u062a\u0648\u0627\u0635\u0644 \u0645\u0639 \u0627\u0644\u0645\u0635\u0631\u0641. \ud83d\udcb3",
    "enrollment.confirmed": "\u062a\u0645 \u062a\u0623\u0643\u064a\u062f \u0627\u0644\u062a\u0633\u062c\u064a\u0644 \u0644\u0644\u0641\u0635\u0644 \u0627\u0644\u062f\u0631\u0627\u0633\u064a.",
    "enrollment.confirmed.personality":
      "\u062a\u0645 \u0627\u0644\u062a\u0633\u062c\u064a\u0644 \u0628\u0646\u062c\u0627\u062d! \u0641\u0635\u0644 \u062f\u0631\u0627\u0633\u064a \u062c\u062f\u064a\u062f \u0628\u0637\u0627\u0642\u0629 \u0645\u062a\u062c\u062f\u062f\u0629. \ud83c\udf93",
    "fees.status.loading": "\u062c\u0627\u0631 \u062a\u062d\u0645\u064a\u0644 \u062d\u0627\u0644\u0629 \u0627\u0644\u0631\u0633\u0648\u0645\u2026",
    "fees.status.loading.personality":
      "\u062c\u0627\u0631 \u0627\u0644\u062a\u062d\u0642\u0642 \u0645\u0646 \u0631\u0635\u064a\u062f\u0643\u2026 \ud83d\udc40",
  },
  faculty: {
    "claim.submit.success": "\u062a\u0645 \u062a\u0642\u062f\u064a\u0645 \u0637\u0644\u0628 \u0627\u0644\u0627\u0633\u062a\u0631\u062f\u0627\u062f \u0628\u0646\u062c\u0627\u062d.",
    "claim.submit.success.personality":
      "\u062a\u0645 \u062a\u0642\u062f\u064a\u0645 \u0627\u0644\u0637\u0644\u0628! \u064a\u0631\u062c\u0649 \u0627\u0646\u062a\u0638\u0627\u0631 \u0627\u0644\u0645\u0639\u0627\u0644\u062c\u0629. \ud83e\uddfe",
    "payslip.ready": "\u0642\u0633\u064a\u0645\u0629 \u0627\u0644\u0631\u0627\u062a\u0628 \u0627\u0644\u0634\u0647\u0631\u064a\u0629 \u0645\u062a\u0627\u062d\u0629.",
    "payslip.ready.personality":
      "\u0642\u0633\u064a\u0645\u0629 \u0631\u0627\u062a\u0628\u0643 \u062c\u0627\u0647\u0632\u0629 \u2014 \u062b\u0645\u0631\u0629 \u062c\u0647\u062f\u0643 \u0627\u0644\u0634\u0647\u0631\u064a. \ud83d\udcb0",
    "leave.approved": "\u062a\u0645\u062a \u0627\u0644\u0645\u0648\u0627\u0641\u0642\u0629 \u0639\u0644\u0649 \u0637\u0644\u0628 \u0627\u0644\u0625\u062c\u0627\u0632\u0629.",
    "leave.approved.personality":
      "\u062a\u0645\u062a \u0627\u0644\u0645\u0648\u0627\u0641\u0642\u0629 \u0639\u0644\u0649 \u0625\u062c\u0627\u0632\u062a\u0643 \u2014 \u0627\u0633\u062a\u0645\u062a\u0639 \u0628\u0647\u0627\u060c \u0623\u0646\u062a \u062a\u0633\u062a\u062d\u0642\u0647\u0627. \ud83c\udf34",
    "claims.empty": "\u0644\u0627 \u062a\u0648\u062c\u062f \u0637\u0644\u0628\u0627\u062a \u0627\u0633\u062a\u0631\u062f\u0627\u062f \u0633\u0627\u0628\u0642\u0629 \u0623\u0648 \u0645\u0639\u0644\u0642\u0629.",
    "claims.empty.personality":
      "\u0644\u0627 \u062a\u0648\u062c\u062f \u0637\u0644\u0628\u0627\u062a \u2014 \u0625\u0645\u0627 \u0623\u0646\u0647\u0627 \u0645\u0646\u0633\u064a\u0629 \u0623\u0648 \u062a\u0645\u062a \u062a\u0633\u0648\u064a\u062a\u0647\u0627 \u0628\u0627\u0644\u0643\u0627\u0645\u0644. \ud83e\uddf9",
    "payslip.loading": "\u062c\u0627\u0631 \u062a\u062d\u0645\u064a\u0644 \u062a\u0641\u0627\u0635\u064a\u0644 \u0642\u0633\u064a\u0645\u0629 \u0627\u0644\u0631\u0627\u062a\u0628\u2026",
    "payslip.loading.personality":
      "\u062c\u0627\u0631 \u0627\u0633\u062a\u0631\u062f\u0627\u062f \u0645\u0633\u062a\u062d\u0642\u0627\u062a\u0643 \u0627\u0644\u0634\u0647\u0631\u064a\u0629\u2026 \ud83d\udcbc",
  },
  academic: { ...en.academic },
  admin: {
    "challan.verified.success": "\u062a\u0645 \u0627\u0644\u062a\u062d\u0642\u0642 \u0645\u0646 \u0627\u0644\u0625\u0634\u0639\u0627\u0631 \u0628\u0646\u062c\u0627\u062d.",
    "challan.verified.success.personality":
      "\u062a\u0645 \u0627\u0644\u062a\u062d\u0642\u0642 \u0645\u0646 \u0627\u0644\u0625\u0634\u0639\u0627\u0631 \u2014 \u0643\u0644 \u0634\u064a\u0621 \u0633\u0644\u064a\u0645! \ud83d\udc4d",
    "exam.fees.unpaid.warning": "\u0647\u0646\u0627\u0643 \u0631\u0633\u0648\u0645 \u0627\u0645\u062a\u062d\u0627\u0646\u0627\u062a \u063a\u064a\u0631 \u0645\u062f\u0641\u0648\u0639\u0629 \u0644\u0644\u0637\u0644\u0627\u0628.",
    "exam.fees.unpaid.warning.personality":
      "\u0628\u0639\u0636 \u0627\u0644\u0637\u0644\u0627\u0628 \u0644\u062f\u064a\u0647\u0645 \u0631\u0633\u0648\u0645 \u0627\u0645\u062a\u062d\u0627\u0646\u0627\u062a \u0645\u0639\u0644\u0642\u0629 \u2014 \u0642\u0645 \u0628\u062a\u0630\u0643\u064a\u0631\u0647\u0645 \u0642\u0628\u0644 \u0641\u0648\u0627\u062a \u0627\u0644\u0623\u0648\u0627\u0646. \ud83d\udce2",
    "results.lock.confirm": "\u0647\u0644 \u0623\u0646\u062a \u0645\u062a\u0623\u0643\u062f \u0645\u0646 \u0642\u0641\u0644 \u0627\u0644\u0646\u062a\u0627\u0626\u062c\u061f \u0647\u0630\u0627 \u0627\u0644\u0625\u062c\u0631\u0627\u0621 \u0644\u0627 \u064a\u0645\u0643\u0646 \u0627\u0644\u062a\u0631\u0627\u062c\u0639 \u0639\u0646\u0647.",
    "verifications.empty": "\u0644\u0627 \u062a\u0648\u062c\u062f \u0625\u0634\u0639\u0627\u0631\u0627\u062a \u0645\u0639\u0644\u0642\u0629 \u0644\u0644\u062a\u062d\u0642\u0642.",
    "verifications.empty.personality":
      "\u062a\u0645 \u0627\u0644\u062a\u062d\u0642\u0642 \u0645\u0646 \u062c\u0645\u064a\u0639 \u0627\u0644\u0625\u0634\u0639\u0627\u0631\u0627\u062a \u2014 \u0627\u0644\u0633\u062c\u0644 \u0646\u0638\u064a\u0641. \u2728",
  },
  staff: {
    "challan.batch.loading": "\u062c\u0627\u0631 \u062a\u062d\u0645\u064a\u0644 \u0628\u064a\u0627\u0646\u0627\u062a \u062f\u0641\u0639\u0629 \u0627\u0644\u0625\u0634\u0639\u0627\u0631\u0627\u062a\u2026",
    "challan.batch.loading.personality":
      "\u062c\u0627\u0631 \u062a\u062d\u0645\u064a\u0644 \u0645\u062c\u0645\u0648\u0639\u0629 \u0627\u0644\u0625\u0634\u0639\u0627\u0631\u0627\u062a \u2014 \u064a\u0631\u062c\u0649 \u0627\u0644\u0627\u0646\u062a\u0638\u0627\u0631 \u0642\u0644\u064a\u0644\u0627\u064b. \ud83d\udce6",
    "challan.overwrite.warning": "\u0633\u064a\u0624\u062f\u064a \u0647\u0630\u0627 \u0627\u0644\u0625\u062c\u0631\u0627\u0621 \u0625\u0644\u0649 \u0627\u0633\u062a\u0628\u062f\u0627\u0644 \u0633\u062c\u0644\u0627\u062a \u0627\u0644\u0625\u0634\u0639\u0627\u0631\u0627\u062a \u0627\u0644\u062d\u0627\u0644\u064a\u0629.",
    "challan.overwrite.warning.personality":
      "\u0633\u064a\u062a\u0645 \u0627\u0633\u062a\u0628\u062f\u0627\u0644 \u0627\u0644\u0625\u0634\u0639\u0627\u0631\u0627\u062a \u0627\u0644\u0642\u062f\u064a\u0645\u0629 \u2014 \u0647\u0644 \u0623\u0646\u062a \u0645\u062a\u0623\u0643\u062f\u061f \u0644\u0627 \u064a\u0645\u0643\u0646 \u0627\u0644\u062a\u0631\u0627\u062c\u0639! \u26a0\ufe0f",
    "batches.empty": "\u0644\u0627 \u062a\u0648\u062c\u062f \u062f\u0641\u0639\u0627\u062a \u0625\u0634\u0639\u0627\u0631\u0627\u062a \u0644\u0644\u0645\u0639\u0627\u0644\u062c\u0629.",
    "batches.empty.personality":
      "\u0644\u0627 \u062a\u0648\u062c\u062f \u062f\u0641\u0639\u0627\u062a \u0645\u0639\u0644\u0642\u0629 \u2014 \u0627\u0646\u062a\u0647\u0649 \u0627\u0644\u0639\u0645\u0644\u060c \u062d\u0627\u0646 \u0648\u0642\u062a \u0627\u0644\u0627\u0633\u062a\u0631\u0627\u062d\u0629. \u2615",
  },
};

const ms = {
  student: {
    "challan.paid.success": "Pembayaran slip yuran disahkan.",
    "challan.paid.success.personality":
      "Slip dah clear, boleh rileks sekarang! \ud83d\udcb8",
    "challan.expiry.warning":
      "Slip yuran anda akan tamat tempoh tidak lama lagi.",
    "challan.expiry.warning.personality":
      "Eh! Slip nak tamat dah \u2014 cepat bayar sebelum kena denda lewat! \u26a0\ufe0f",
    "challan.generated.success": "Slip yuran berjaya dijana.",
    "challan.generated.success.personality":
      "Slip dah siap! Bayar je sekarang, mudah je. \ud83d\udccb",
    "results.published":
      "Keputusan peperiksaan anda telah diterbitkan.",
    "results.published.personality":
      "Result dah keluar! Cepat check \u2014 semoga cemerlang! \ud83d\udcca",
    "dues.empty": "Tiada tunggakan yuran.",
    "dues.empty.personality":
      "Baki kosong, hati senang. Semua settle! \ud83d\udd13",
    "payment.declined.error":
      "Pembayaran ditolak oleh gerbang pembayaran.",
    "payment.declined.error.personality":
      "Bayaran tak lepas \u2014 cuba lagi guna kad lain, ya. \ud83d\udcb3",
    "enrollment.confirmed": "Pendaftaran semester disahkan.",
    "enrollment.confirmed.personality":
      "Daftar dah selesai! Semester baru, semangat baru \u2014 jom! \ud83c\udf93",
    "fees.status.loading": "Memuatkan status yuran\u2026",
    "fees.status.loading.personality":
      "Tengah check baki anda\u2026 \ud83d\udc40",
  },
  faculty: {
    "claim.submit.success":
      "Tuntutan pembayaran balik berjaya dihantar.",
    "claim.submit.success.personality":
      "Tuntutan dah hantar! Tunggu duit masuk je sekarang. \ud83e\uddfe",
    "payslip.ready": "Slip gaji bulanan anda tersedia.",
    "payslip.ready.personality":
      "Hasil penat lelah sebulan dah sampai \u2014 slip gaji dah siap! \ud83d\udcb0",
    "leave.approved": "Permohonan cuti anda telah diluluskan.",
    "leave.approved.personality":
      "Cuti dah lulus! Enjoy, memang layak pun. \ud83c\udf34",
    "claims.empty":
      "Tiada tuntutan pembayaran balik terdahulu atau tertunda.",
    "claims.empty.personality":
      "Takde tuntutan langsung \u2014 semua dah clear ke atau lupa nak hantar? \ud83e\uddf9",
    "payslip.loading": "Memuatkan butiran slip gaji\u2026",
    "payslip.loading.personality":
      "Tengah tarik rezeki bulanan anda\u2026 \ud83d\udcbc",
  },
  academic: { ...en.academic },
  admin: {
    "challan.verified.success": "Slip yuran berjaya disahkan.",
    "challan.verified.success.personality":
      "Slip dah sah, semua okay! \ud83d\udc4d",
    "exam.fees.unpaid.warning":
      "Terdapat yuran peperiksaan pelajar yang belum dibayar.",
    "exam.fees.unpaid.warning.personality":
      "Ada pelajar belum bayar yuran exam \u2014 ingatkan mereka sebelum terlepas. \ud83d\udce2",
    "results.lock.confirm":
      "Adakah anda pasti mahu mengunci keputusan? Tindakan ini tidak boleh dibatalkan.",
    "verifications.empty": "Tiada pengesahan slip tertunda.",
    "verifications.empty.personality":
      "Semua slip dah disahkan \u2014 rekod bersih! \u2728",
  },
  staff: {
    "challan.batch.loading": "Memuatkan data kumpulan slip\u2026",
    "challan.batch.loading.personality":
      "Sekumpulan slip tengah dimuatkan, sekejap ya. \ud83d\udce6",
    "challan.overwrite.warning":
      "Tindakan ini akan menimpa rekod slip sedia ada.",
    "challan.overwrite.warning.personality":
      "Slip lama akan diganti \u2014 pasti ke? Nanti tak boleh undo! \u26a0\ufe0f",
    "batches.empty": "Tiada kumpulan slip untuk diproses.",
    "batches.empty.personality":
      "Tiada kumpulan slip tertunda \u2014 kerja dah habis, boleh minum teh dulu! \u2615",
  },
};

export const personalityMessages: Record<PortalLocale, typeof en> = {
  "en-PK": en,
  "en-US": en,
  "en-GB": en,
  "ar-AE": ar,
  "ms-MY": ms,
};
