import { type PortalLocale } from "./messages";

const en = {
  student: {
    challan: {
      paid: {
        success: {
          personality: "Challan cleared hai, ab chill karo! 💸"
        }
      },
      expiry: {
        warning: {
          personality: "Oye! Challan expire hone wala hai — jaldi karo warna late fee lagegi! ⚠️"
        }
      },
      generated: {
        success: {
          personality: "Challan ready hai! Ab bas pay karo aur scene set. 📋"
        }
      }
    },
    results: {
      published: {
        personality: "Results aa gaye, bhai! Check karo — dua karo GPA phaar ho. 📊"
      }
    },
    dues: {
      empty: {
        personality: "Zero balance, zero stress. You're all clear! 🔓"
      }
    },
    payment: {
      declined: {
        error: {
          personality: "Payment decline ho gaya — bank walay kanjoos niklay. Dobara try karo ya kisi aur card se. 💳"
        }
      }
    },
    enrollment: {
      confirmed: {
        personality: "Enrollment done hai! Naya semester, nayi energy — let's get it! 🎓"
      }
    },
    fees: {
      status: {
        loading: {
          personality: "Checking if you're broke or not… 👀"
        }
      }
    }
  },
  faculty: {
    claim: {
      submit: {
        success: {
          personality: "Claim submit ho gaya! Ab bas paisa anay ka wait karo. 🧾"
        }
      }
    },
    payslip: {
      ready: {
        personality: "Aap ki mehnat ka sila aa gaya — payslip ready hai! 💰"
      },
      loading: {
        personality: "Pulling your monthly bag… 💼"
      }
    },
    leave: {
      approved: {
        personality: "Chuttiyan approved! Enjoy karo, aap deserve karte hain. 🌴"
      }
    },
    claims: {
      empty: {
        personality: "Koi claims nahi hain — either aap bhool gaye ya sab clear ho gaya. 🧹"
      }
    }
  },
  academic: {
    budget: {
      expiry: {
        warning: {
          personality: "Budget expire hone wala hai — use it or lose it, yaar! 📉"
        }
      }
    },
    faculty: {
      claim: {
        approved: {
          personality: "Faculty ka claim approve ho gaya — paisa release hone ka time! ✅"
        }
      }
    },
    approvals: {
      empty: {
        personality: "Sara kaam nipat gaya — koi approvals pending nahi. King/Queen behavior! 👑"
      }
    },
    gradeAppeals: {
      personality: "appeal your grade? we got you"
    },
    gradeAppealsDesc: {
      personality: "bhai ne marks kaat diye? let's talk it out with the committee"
    },
    appealSubmissions: {
      personality: "file your appeal, we'll listen"
    },
    appealsRegistry: {
      personality: "sab appeals ki file — kaun kya kar raha hai?"
    },
    appeaLPipeline: {
      personality: "appeals in the queue, let's process them"
    },
    submitAppeal: {
      personality: "submit karo appeal, scene on karo"
    },
    submitting: {
      personality: "appeal file ho rahi hai... thora wait"
    },
    loadingAppeals: {
      personality: "appeals load ho rahi hain, ruko zara..."
    },
    noAppealsFiled: {
      personality: "koi appeal nahi — sab khush hain ya bas bhool gaye?"
    },
    noAppealsDesc: {
      personality: "first appeal submit karo, form upar hai bhai"
    },
    generateBrief: {
      personality: "committee ko brief do — AI bhai summary likhega"
    },
    generatingBrief: {
      personality: "AI apna kaam kar raha hai, brief aa raha hai..."
    },
    underReview: {
      personality: "committee dekh rahi hai"
    },
    resolved: {
      personality: "sorted hai, ho gaya"
    },
    gradeDistributions: {
      personality: "grades ka scatter plot — let's see who aced it",
      heading: {
        personality: "grade distributions ka analysis — kon kitna score kar raha?"
      }
    },
    runAnalysis: {
      personality: "analysis chalao, dekhain kya scene hai"
    },
    analyzing: {
      personality: "data crunch ho raha hai, ek second..."
    },
    selectCourse: {
      personality: "course choose karo, phir dekhte hain"
    },
    mean: {
      personality: "average score — neeche wale ro rahe hain"
    },
    median: {
      personality: "middle child of grades"
    },
    stdDev: {
      personality: "kitna spread hai? high variance = kuch ne phaar diya, kuch ne..."
    },
    passRate: {
      personality: "pass hone walon ki tally"
    },
    failureRate: {
      personality: "jo fail hue unka score"
    },
    anomalyAlerts: {
      personality: "kuch garbar lagti hai? anomalies detected!"
    },
    noAnomaliesDetected: {
      personality: "sab set hai, koi anomaly nahi"
    },
    noAnomaliesDesc: {
      personality: "grade distributions normal hain — no drama here"
    },
    plagiarism: {
      personality: "copy-paste kiya? hum pakar lein gay"
    },
    scanDocument: {
      personality: "document scan karo — dekhain kahan se copy kiya"
    },
    scanning: {
      personality: "scan chal raha hai, sab kuch check ho raha..."
    },
    similarityScore: {
      personality: "kitna copy kiya? score dekh lo"
    },
    lowRisk: {
      personality: "safe hai — mostly original"
    },
    moderate: {
      personality: "hmmm, kuch toh garbar hai"
    },
    highRisk: {
      personality: "bhai, yeh toh puri copy hai!"
    },
    collusionGraph: {
      personality: "dost-dost ki setting? collusion check"
    },
    noCollusionPatterns: {
      personality: "koi setting nahi pakri gayi — sab independent lag rahay"
    },
    documentScanner: {
      personality: "apna text paste karo, scanner check karega"
    },
    matchedSources: {
      personality: "kahan-kahan se copy kiya? sources list"
    },
    aiWarning: {
      personality: "AI ne likha hai kya? threshold cross ho gaya, review karo!"
    },
    proctoring: {
      personality: "online exam, nazar hum par hai"
    },
    createSession: {
      personality: "new proctoring session shuru karo — cameras on!"
    },
    startSession: {
      personality: "session start! ab koi cheat nahi karega"
    },
    faceAnalysis: {
      personality: "chehra verify karo — asli banda hai ya duplicate?"
    },
    analyzeFace: {
      personality: "face scan karo, dekhain match hota hai ya nahi"
    },
    lockdownValidation: {
      personality: "browser locked hai? check karo sab sahi hai"
    },
    studentIntegrityGrid: {
      personality: "kon saaf hai, kon nahi — integrity report card"
    },
    proctorEventLog: {
      personality: "exam ke doran jo bhi hua, sab yahan likha hai"
    },
    noEventsDesc: {
      personality: "all quiet on the exam front — keep it up!"
    },
    defaulterPrediction: {
      personality: "fees kon nahi de raha? AI bata de ga",
      heading: {
        personality: "who's about to ghost their fees?"
      }
    },
    studentRiskLookup: {
      personality: "kisi bhi student ka risk score check karo"
    },
    lookUp: {
      personality: "search karo, dekhain kitna risk hai"
    },
    riskDrivers: {
      personality: "kya reasons hain risk ke? AI ka analysis"
    },
    generateInterventions: {
      personality: "har risk tier ke liye action plan banao"
    },
    generatePlan: {
      personality: "AI plan bana raha hai..."
    },
    cashFlowForecast: {
      personality: "aglay 6 mahine kitna paisa aane wala hai?"
    },
    noForecastData: {
      personality: "forecast data available nahi hai — abhi tak sab ne fees de di?"
    },
    healthy: {
      personality: "scene set hai, paisa aane wala hai"
    },
    caution: {
      personality: "thori tension hai — keep an eye"
    },
    atRisk: {
      personality: "bhai, yeh recovery mushkil lagti hai"
    },
    readiness: {
      personality: "how ready are we? let's find out",
      heading: {
        personality: "accreditation kitni dur hai? readiness check"
      }
    },
    computingReadiness: {
      personality: "scores calculate ho rahe hain... thora sabar"
    },
    checkFreshness: {
      personality: "evidence fresh hai ya purana? check karo"
    },
    overallScore: {
      personality: "total readiness — number dekh kar pata chalega"
    },
    daysUntilAudit: {
      personality: "audit anay mein kitna time baki?"
    },
    criteriaBreakdown: {
      personality: "har criterion ka score — kahan kami hai?"
    },
    gapAnalysis: {
      personality: "jo missing hai woh yahan likha hai"
    },
    noGapsIdentified: {
      personality: "sab complete hai — total king move! 👑"
    },
    noGapsDesc: {
      personality: "sare criteria meet ho gaye — accreditation ready!"
    },
    alertsFeed: {
      personality: "latest updates — kya badla hai?"
    },
    allClear: {
      personality: "sab clear, koi alert nahi — maze karo"
    },
    mentalHealth: {
      personality: "dil ki baat, anonymously share karo",
      heading: {
        personality: "mental health matters — let's talk about it"
      }
    },
    anonymousTicket: {
      personality: "apni baat batao, koi naam nahi puchega"
    },
    submitAnonymousTicket: {
      personality: "send karo, hum sun rahe hain"
    },
    counselorQueue: {
      personality: "counselor ke paas kitne cases hain?"
    },
    crisisTickets: {
      personality: "emergency cases — immediate action chahiye"
    },
    immediateAttention: {
      personality: "bhai, yeh urgent hai! abhi dekhna pare ga"
    },
    noActiveCrises: {
      personality: "sab stable hai, alhamdulillah!"
    },
    noPendingTickets: {
      personality: "queue empty — aaj chai peene ka din"
    },
    queueIsClear: {
      personality: "psychologist ki chutti, no tickets pending"
    },
    fileMandatoryReport: {
      personality: "official report file karni hai — serious matter"
    },
    noTicketsRequireReport: {
      personality: "sab chill hai, no mandatory escalation needed"
    },
    mandatoryReportPanel: {
      personality: "crisis cases yahan aatay hain — handle with care"
    },
    examinations: {
      personality: "exam season — papers, marks, results, sab yahan",
      heading: {
        personality: "papers set hain? exams ka time!"
      }
    },
    marksEntry: {
      personality: "student ke marks enter karo — dua karo fail na ho"
    },
    recordMarks: {
      personality: "marks save karo, phir result ready"
    },
    recording: {
      personality: "marks save ho rahe hain..."
    },
    resultPublication: {
      personality: "results publish karo — students ki dua milegi... ya bad-dua"
    },
    publishResults: {
      personality: "publish! ab koi wapas nahi badal sakta"
    },
    publishing: {
      personality: "results publish ho rahe hain..."
    },
    assessmentSchemes: {
      personality: "exam mein kya-kya count hoga? scheme dekh lo"
    },
    noSchemesDefined: {
      personality: "koi scheme nahi — bhai pehle components define karo!"
    },
    noPublicationSlots: {
      personality: "koi slot configured nahi — results kahan publish honge?"
    },
    noSchemesDesc: {
      personality: "assessment components banao, phir marks enter karo"
    },
    placement: {
      personality: "internship lag gayi? placement tracker",
      heading: {
        personality: "job lagwana hai? placement desk pe aao"
      }
    },
    placementRate: {
      personality: "kitne students ko job mili?"
    },
    studentReadiness: {
      personality: "student job ke liye ready hai? check karo"
    },
    checkReadiness: {
      personality: "skills scan karo, phir batayein kitni mehnat baqi hai"
    },
    readinessScore: {
      personality: "employment readiness — number jitna ooncha, utni achi job"
    },
    skillAssessment: {
      personality: "kaun si skill strong hai, kaun si weak"
    },
    partnerManagement: {
      personality: "companies jo students hire karti hain"
    },
    noPartnerCompanies: {
      personality: "koi company registered nahi — outreach time!"
    },
    activePlacements: {
      personality: "kaun kahan placed hai? tracker"
    },
    noActivePlacements: {
      personality: "abhi tak koi placed nahi — koi baat nahi, scene banega"
    },
    noActivePlacementsDesc: {
      personality: "placement abhi tak nahi hui? market mein bhejo inko!"
    },
    achievements: {
      personality: "students ke achievements — flex zone! 🏆"
    },
    convocations: {
      personality: "graduation ceremony — cap and gown time"
    },
    freezes: {
      personality: "semester freeze? let's pause the clock"
    },
    visitingFaculty: {
      personality: "guest faculty hire karo — marketplace open hai"
    },
    elections: {
      personality: "student elections — vote do, leader chuno"
    },
    boardPortal: {
      personality: "board of governors ka secret lair"
    },
    foreignStudents: {
      personality: "visa, immigration, sab manage karo"
    },
    voiceAssistant: {
      personality: "NidaBot se baat karo — AI assistant ready hai"
    },
    catalogIngestion: {
      personality: "course catalog upload karo — drag and drop"
    },
    classGeneration: {
      personality: "naya semester, nayi classes — auto-generate karo"
    },
    cafeteria: {
      personality: "aaj khane mein kya hai? menu check karo"
    },
    transport: {
      personality: "buses ready hain? transport management"
    },
    prereqConflicts: {
      personality: "prerequisite check — yeh course karne se pehle woh karna zaroori hai?"
    }
  },
  admin: {
    challan: {
      verified: {
        success: {
          personality: "Challan verify ho gaya, sab legit hai! 👍"
        }
      }
    },
    exam: {
      fees: {
        unpaid: {
          warning: {
            personality: "Kuch students ke exam fees abhi tak pending hain — unko remind karo warna exams miss karein ge. 📢"
          }
        }
      }
    },
    results: {
      lock: {
        confirm: "Are you sure you want to lock results? This action is irreversible."
      }
    },
    verifications: {
      empty: {
        personality: "Sare challan verify ho gaye — clean slate, scene on! ✨"
      }
    }
  },
  staff: {
    challan: {
      batch: {
        loading: {
          personality: "Challano ki puri fauj load ho rahi hai, thora sabar. 📦"
        }
      },
      overwrite: {
        warning: {
          personality: "Puranay challan replace hone wale hain — sure ho? Dobara wapas nahi hoga! ⚠️"
        }
      }
    },
    batches: {
      empty: {
        personality: "Koi challan batch pending nahi — aaj ka kaam khatam, ab chai peelo! ☕"
      }
    }
  }
};

const ar = {
  student: {
    challan: {
      paid: {
        success: {
          personality: "تمت تصفية الإشعار — استرح الآن!   "
        }
      },
      expiry: {
        warning: {
          personality: "تنبيه! صلاحية الإشعار على وشك الانتهاء — بادر بالدفع لتجنب غرامة التأخير. ⚠️"
        }
      },
      generated: {
        success: {
          personality: "الإشعار جاهز! قم بالدفع لإتمام العملية. 📋"
        }
      }
    },
    results: {
      published: {
        personality: "ظهرت النتائج! تفقدها الآن — نتمنى لك التوفيق. 📊"
      }
    },
    dues: {
      empty: {
        personality: "رصيدك صفري — لا توجد أية التزامات مالية. 🔓"
      }
    },
    payment: {
      declined: {
        error: {
          personality: "تم رفض المعاملة — يرجى المحاولة ببطاقة أخرى أو التواصل مع المصرف. 💳"
        }
      }
    },
    enrollment: {
      confirmed: {
        personality: "تم التسجيل بنجاح! فصل دراسي جديد بطاقة متجددة. 🎓"
      }
    },
    fees: {
      status: {
        loading: {
          personality: "جار التحقق من رصيدك… 👀"
        }
      }
    }
  },
  faculty: {
    claim: {
      submit: {
        success: {
          personality: "تم تقديم الطلب! يرجى انتظار المعالجة. 🧾"
        }
      }
    },
    payslip: {
      ready: {
        personality: "قسيمة راتبك جاهزة — ثمرة جهدك الشهري. 💰"
      },
      loading: {
        personality: "جار استرداد مستحقاتك الشهرية… 💼"
      }
    },
    leave: {
      approved: {
        personality: "تمت الموافقة على إجازتك — استمتع بها، أنت تستحقها. 🌴"
      }
    },
    claims: {
      empty: {
        personality: "لا توجد طلبات — إما أنها منسية أو تمت تسويتها بالكامل. 🧹"
      }
    }
  },
  academic: {
    budget: {
      expiry: {
        warning: {
          personality: "Budget expire hone wala hai — use it or lose it, yaar! 📉"
        }
      }
    },
    faculty: {
      claim: {
        approved: {
          personality: "Faculty ka claim approve ho gaya — paisa release hone ka time! ✅"
        }
      }
    },
    approvals: {
      empty: {
        personality: "Sara kaam nipat gaya — koi approvals pending nahi. King/Queen behavior! 👑"
      }
    },
    gradeAppeals: {
      personality: "appeal your grade? we got you"
    },
    gradeAppealsDesc: {
      personality: "bhai ne marks kaat diye? let's talk it out with the committee"
    },
    appealSubmissions: {
      personality: "file your appeal, we'll listen"
    },
    appealsRegistry: {
      personality: "sab appeals ki file — kaun kya kar raha hai?"
    },
    appeaLPipeline: {
      personality: "appeals in the queue, let's process them"
    },
    submitAppeal: {
      personality: "submit karo appeal, scene on karo"
    },
    submitting: {
      personality: "appeal file ho rahi hai... thora wait"
    },
    loadingAppeals: {
      personality: "appeals load ho rahi hain, ruko zara..."
    },
    noAppealsFiled: {
      personality: "koi appeal nahi — sab khush hain ya bas bhool gaye?"
    },
    noAppealsDesc: {
      personality: "first appeal submit karo, form upar hai bhai"
    },
    generateBrief: {
      personality: "committee ko brief do — AI bhai summary likhega"
    },
    generatingBrief: {
      personality: "AI apna kaam kar raha hai, brief aa raha hai..."
    },
    underReview: {
      personality: "committee dekh rahi hai"
    },
    resolved: {
      personality: "sorted hai, ho gaya"
    },
    gradeDistributions: {
      personality: "grades ka scatter plot — let's see who aced it",
      heading: {
        personality: "grade distributions ka analysis — kon kitna score kar raha?"
      }
    },
    runAnalysis: {
      personality: "analysis chalao, dekhain kya scene hai"
    },
    analyzing: {
      personality: "data crunch ho raha hai, ek second..."
    },
    selectCourse: {
      personality: "course choose karo, phir dekhte hain"
    },
    mean: {
      personality: "average score — neeche wale ro rahe hain"
    },
    median: {
      personality: "middle child of grades"
    },
    stdDev: {
      personality: "kitna spread hai? high variance = kuch ne phaar diya, kuch ne..."
    },
    passRate: {
      personality: "pass hone walon ki tally"
    },
    failureRate: {
      personality: "jo fail hue unka score"
    },
    anomalyAlerts: {
      personality: "kuch garbar lagti hai? anomalies detected!"
    },
    noAnomaliesDetected: {
      personality: "sab set hai, koi anomaly nahi"
    },
    noAnomaliesDesc: {
      personality: "grade distributions normal hain — no drama here"
    },
    plagiarism: {
      personality: "copy-paste kiya? hum pakar lein gay"
    },
    scanDocument: {
      personality: "document scan karo — dekhain kahan se copy kiya"
    },
    scanning: {
      personality: "scan chal raha hai, sab kuch check ho raha..."
    },
    similarityScore: {
      personality: "kitna copy kiya? score dekh lo"
    },
    lowRisk: {
      personality: "safe hai — mostly original"
    },
    moderate: {
      personality: "hmmm, kuch toh garbar hai"
    },
    highRisk: {
      personality: "bhai, yeh toh puri copy hai!"
    },
    collusionGraph: {
      personality: "dost-dost ki setting? collusion check"
    },
    noCollusionPatterns: {
      personality: "koi setting nahi pakri gayi — sab independent lag rahay"
    },
    documentScanner: {
      personality: "apna text paste karo, scanner check karega"
    },
    matchedSources: {
      personality: "kahan-kahan se copy kiya? sources list"
    },
    aiWarning: {
      personality: "AI ne likha hai kya? threshold cross ho gaya, review karo!"
    },
    proctoring: {
      personality: "online exam, nazar hum par hai"
    },
    createSession: {
      personality: "new proctoring session shuru karo — cameras on!"
    },
    startSession: {
      personality: "session start! ab koi cheat nahi karega"
    },
    faceAnalysis: {
      personality: "chehra verify karo — asli banda hai ya duplicate?"
    },
    analyzeFace: {
      personality: "face scan karo, dekhain match hota hai ya nahi"
    },
    lockdownValidation: {
      personality: "browser locked hai? check karo sab sahi hai"
    },
    studentIntegrityGrid: {
      personality: "kon saaf hai, kon nahi — integrity report card"
    },
    proctorEventLog: {
      personality: "exam ke doran jo bhi hua, sab yahan likha hai"
    },
    noEventsDesc: {
      personality: "all quiet on the exam front — keep it up!"
    },
    defaulterPrediction: {
      personality: "fees kon nahi de raha? AI bata de ga",
      heading: {
        personality: "who's about to ghost their fees?"
      }
    },
    studentRiskLookup: {
      personality: "kisi bhi student ka risk score check karo"
    },
    lookUp: {
      personality: "search karo, dekhain kitna risk hai"
    },
    riskDrivers: {
      personality: "kya reasons hain risk ke? AI ka analysis"
    },
    generateInterventions: {
      personality: "har risk tier ke liye action plan banao"
    },
    generatePlan: {
      personality: "AI plan bana raha hai..."
    },
    cashFlowForecast: {
      personality: "aglay 6 mahine kitna paisa aane wala hai?"
    },
    noForecastData: {
      personality: "forecast data available nahi hai — abhi tak sab ne fees de di?"
    },
    healthy: {
      personality: "scene set hai, paisa aane wala hai"
    },
    caution: {
      personality: "thori tension hai — keep an eye"
    },
    atRisk: {
      personality: "bhai, yeh recovery mushkil lagti hai"
    },
    readiness: {
      personality: "how ready are we? let's find out",
      heading: {
        personality: "accreditation kitni dur hai? readiness check"
      }
    },
    computingReadiness: {
      personality: "scores calculate ho rahe hain... thora sabar"
    },
    checkFreshness: {
      personality: "evidence fresh hai ya purana? check karo"
    },
    overallScore: {
      personality: "total readiness — number dekh kar pata chalega"
    },
    daysUntilAudit: {
      personality: "audit anay mein kitna time baki?"
    },
    criteriaBreakdown: {
      personality: "har criterion ka score — kahan kami hai?"
    },
    gapAnalysis: {
      personality: "jo missing hai woh yahan likha hai"
    },
    noGapsIdentified: {
      personality: "sab complete hai — total king move! 👑"
    },
    noGapsDesc: {
      personality: "sare criteria meet ho gaye — accreditation ready!"
    },
    alertsFeed: {
      personality: "latest updates — kya badla hai?"
    },
    allClear: {
      personality: "sab clear, koi alert nahi — maze karo"
    },
    mentalHealth: {
      personality: "dil ki baat, anonymously share karo",
      heading: {
        personality: "mental health matters — let's talk about it"
      }
    },
    anonymousTicket: {
      personality: "apni baat batao, koi naam nahi puchega"
    },
    submitAnonymousTicket: {
      personality: "send karo, hum sun rahe hain"
    },
    counselorQueue: {
      personality: "counselor ke paas kitne cases hain?"
    },
    crisisTickets: {
      personality: "emergency cases — immediate action chahiye"
    },
    immediateAttention: {
      personality: "bhai, yeh urgent hai! abhi dekhna pare ga"
    },
    noActiveCrises: {
      personality: "sab stable hai, alhamdulillah!"
    },
    noPendingTickets: {
      personality: "queue empty — aaj chai peene ka din"
    },
    queueIsClear: {
      personality: "psychologist ki chutti, no tickets pending"
    },
    fileMandatoryReport: {
      personality: "official report file karni hai — serious matter"
    },
    noTicketsRequireReport: {
      personality: "sab chill hai, no mandatory escalation needed"
    },
    mandatoryReportPanel: {
      personality: "crisis cases yahan aatay hain — handle with care"
    },
    examinations: {
      personality: "exam season — papers, marks, results, sab yahan",
      heading: {
        personality: "papers set hain? exams ka time!"
      }
    },
    marksEntry: {
      personality: "student ke marks enter karo — dua karo fail na ho"
    },
    recordMarks: {
      personality: "marks save karo, phir result ready"
    },
    recording: {
      personality: "marks save ho rahe hain..."
    },
    resultPublication: {
      personality: "results publish karo — students ki dua milegi... ya bad-dua"
    },
    publishResults: {
      personality: "publish! ab koi wapas nahi badal sakta"
    },
    publishing: {
      personality: "results publish ho rahe hain..."
    },
    assessmentSchemes: {
      personality: "exam mein kya-kya count hoga? scheme dekh lo"
    },
    noSchemesDefined: {
      personality: "koi scheme nahi — bhai pehle components define karo!"
    },
    noPublicationSlots: {
      personality: "koi slot configured nahi — results kahan publish honge?"
    },
    noSchemesDesc: {
      personality: "assessment components banao, phir marks enter karo"
    },
    placement: {
      personality: "internship lag gayi? placement tracker",
      heading: {
        personality: "job lagwana hai? placement desk pe aao"
      }
    },
    placementRate: {
      personality: "kitne students ko job mili?"
    },
    studentReadiness: {
      personality: "student job ke liye ready hai? check karo"
    },
    checkReadiness: {
      personality: "skills scan karo, phir batayein kitni mehnat baqi hai"
    },
    readinessScore: {
      personality: "employment readiness — number jitna ooncha, utni achi job"
    },
    skillAssessment: {
      personality: "kaun si skill strong hai, kaun si weak"
    },
    partnerManagement: {
      personality: "companies jo students hire karti hain"
    },
    noPartnerCompanies: {
      personality: "koi company registered nahi — outreach time!"
    },
    activePlacements: {
      personality: "kaun kahan placed hai? tracker"
    },
    noActivePlacements: {
      personality: "abhi tak koi placed nahi — koi baat nahi, scene banega"
    },
    noActivePlacementsDesc: {
      personality: "placement abhi tak nahi hui? market mein bhejo inko!"
    },
    achievements: {
      personality: "students ke achievements — flex zone! 🏆"
    },
    convocations: {
      personality: "graduation ceremony — cap and gown time"
    },
    freezes: {
      personality: "semester freeze? let's pause the clock"
    },
    visitingFaculty: {
      personality: "guest faculty hire karo — marketplace open hai"
    },
    elections: {
      personality: "student elections — vote do, leader chuno"
    },
    boardPortal: {
      personality: "board of governors ka secret lair"
    },
    foreignStudents: {
      personality: "visa, immigration, sab manage karo"
    },
    voiceAssistant: {
      personality: "NidaBot se baat karo — AI assistant ready hai"
    },
    catalogIngestion: {
      personality: "course catalog upload karo — drag and drop"
    },
    classGeneration: {
      personality: "naya semester, nayi classes — auto-generate karo"
    },
    cafeteria: {
      personality: "aaj khane mein kya hai? menu check karo"
    },
    transport: {
      personality: "buses ready hain? transport management"
    },
    prereqConflicts: {
      personality: "prerequisite check — yeh course karne se pehle woh karna zaroori hai?"
    }
  },
  admin: {
    challan: {
      verified: {
        success: {
          personality: "تم التحقق من الإشعار — كل شيء سليم! 👍"
        }
      }
    },
    exam: {
      fees: {
        unpaid: {
          warning: {
            personality: "بعض الطلاب لديهم رسوم امتحانات معلقة — قم بتذكيرهم قبل فوات الأوان. 📢"
          }
        }
      }
    },
    results: {
      lock: {
        confirm: "هل أنت متأكد من قفل النتائج؟ هذا الإجراء لا يمكن التراجع عنه."
      }
    },
    verifications: {
      empty: {
        personality: "تم التحقق من جميع الإشعارات — السجل نظيف. ✨"
      }
    }
  },
  staff: {
    challan: {
      batch: {
        loading: {
          personality: "جار تحميل مجموعة الإشعارات — يرجى الانتظار قليلاً. 📦"
        }
      },
      overwrite: {
        warning: {
          personality: "سيتم استبدال الإشعارات القديمة — هل أنت متأكد؟ لا يمكن التراجع! ⚠️"
        }
      }
    },
    batches: {
      empty: {
        personality: "لا توجد دفعات معلقة — انتهى العمل، حان وقت الاستراحة. ☕"
      }
    }
  }
};

const ms = {
  student: {
    challan: {
      paid: {
        success: {
          personality: "Slip dah clear, boleh rileks sekarang! 💸"
        }
      },
      expiry: {
        warning: {
          personality: "Eh! Slip nak tamat dah — cepat bayar sebelum kena denda lewat! ⚠️"
        }
      },
      generated: {
        success: {
          personality: "Slip dah siap! Bayar je sekarang, mudah je. 📋"
        }
      }
    },
    results: {
      published: {
        personality: "Result dah keluar! Cepat check — semoga cemerlang! 📊"
      }
    },
    dues: {
      empty: {
        personality: "Baki kosong, hati senang. Semua settle! 🔓"
      }
    },
    payment: {
      declined: {
        error: {
          personality: "Bayaran tak lepas — cuba lagi guna kad lain, ya. 💳"
        }
      }
    },
    enrollment: {
      confirmed: {
        personality: "Daftar dah selesai! Semester baru, semangat baru — jom! 🎓"
      }
    },
    fees: {
      status: {
        loading: {
          personality: "Tengah check baki anda… 👀"
        }
      }
    }
  },
  faculty: {
    claim: {
      submit: {
        success: {
          personality: "Tuntutan dah hantar! Tunggu duit masuk je sekarang. 🧾"
        }
      }
    },
    payslip: {
      ready: {
        personality: "Hasil penat lelah sebulan dah sampai — slip gaji dah siap! 💰"
      },
      loading: {
        personality: "Tengah tarik rezeki bulanan anda… 💼"
      }
    },
    leave: {
      approved: {
        personality: "Cuti dah lulus! Enjoy, memang layak pun. 🌴"
      }
    },
    claims: {
      empty: {
        personality: "Takde tuntutan langsung — semua dah clear ke atau lupa nak hantar? 🧹"
      }
    }
  },
  academic: {
    budget: {
      expiry: {
        warning: {
          personality: "Budget expire hone wala hai — use it or lose it, yaar! 📉"
        }
      }
    },
    faculty: {
      claim: {
        approved: {
          personality: "Faculty ka claim approve ho gaya — paisa release hone ka time! ✅"
        }
      }
    },
    approvals: {
      empty: {
        personality: "Sara kaam nipat gaya — koi approvals pending nahi. King/Queen behavior! 👑"
      }
    },
    gradeAppeals: {
      personality: "appeal your grade? we got you"
    },
    gradeAppealsDesc: {
      personality: "bhai ne marks kaat diye? let's talk it out with the committee"
    },
    appealSubmissions: {
      personality: "file your appeal, we'll listen"
    },
    appealsRegistry: {
      personality: "sab appeals ki file — kaun kya kar raha hai?"
    },
    appeaLPipeline: {
      personality: "appeals in the queue, let's process them"
    },
    submitAppeal: {
      personality: "submit karo appeal, scene on karo"
    },
    submitting: {
      personality: "appeal file ho rahi hai... thora wait"
    },
    loadingAppeals: {
      personality: "appeals load ho rahi hain, ruko zara..."
    },
    noAppealsFiled: {
      personality: "koi appeal nahi — sab khush hain ya bas bhool gaye?"
    },
    noAppealsDesc: {
      personality: "first appeal submit karo, form upar hai bhai"
    },
    generateBrief: {
      personality: "committee ko brief do — AI bhai summary likhega"
    },
    generatingBrief: {
      personality: "AI apna kaam kar raha hai, brief aa raha hai..."
    },
    underReview: {
      personality: "committee dekh rahi hai"
    },
    resolved: {
      personality: "sorted hai, ho gaya"
    },
    gradeDistributions: {
      personality: "grades ka scatter plot — let's see who aced it",
      heading: {
        personality: "grade distributions ka analysis — kon kitna score kar raha?"
      }
    },
    runAnalysis: {
      personality: "analysis chalao, dekhain kya scene hai"
    },
    analyzing: {
      personality: "data crunch ho raha hai, ek second..."
    },
    selectCourse: {
      personality: "course choose karo, phir dekhte hain"
    },
    mean: {
      personality: "average score — neeche wale ro rahe hain"
    },
    median: {
      personality: "middle child of grades"
    },
    stdDev: {
      personality: "kitna spread hai? high variance = kuch ne phaar diya, kuch ne..."
    },
    passRate: {
      personality: "pass hone walon ki tally"
    },
    failureRate: {
      personality: "jo fail hue unka score"
    },
    anomalyAlerts: {
      personality: "kuch garbar lagti hai? anomalies detected!"
    },
    noAnomaliesDetected: {
      personality: "sab set hai, koi anomaly nahi"
    },
    noAnomaliesDesc: {
      personality: "grade distributions normal hain — no drama here"
    },
    plagiarism: {
      personality: "copy-paste kiya? hum pakar lein gay"
    },
    scanDocument: {
      personality: "document scan karo — dekhain kahan se copy kiya"
    },
    scanning: {
      personality: "scan chal raha hai, sab kuch check ho raha..."
    },
    similarityScore: {
      personality: "kitna copy kiya? score dekh lo"
    },
    lowRisk: {
      personality: "safe hai — mostly original"
    },
    moderate: {
      personality: "hmmm, kuch toh garbar hai"
    },
    highRisk: {
      personality: "bhai, yeh toh puri copy hai!"
    },
    collusionGraph: {
      personality: "dost-dost ki setting? collusion check"
    },
    noCollusionPatterns: {
      personality: "koi setting nahi pakri gayi — sab independent lag rahay"
    },
    documentScanner: {
      personality: "apna text paste karo, scanner check karega"
    },
    matchedSources: {
      personality: "kahan-kahan se copy kiya? sources list"
    },
    aiWarning: {
      personality: "AI ne likha hai kya? threshold cross ho gaya, review karo!"
    },
    proctoring: {
      personality: "online exam, nazar hum par hai"
    },
    createSession: {
      personality: "new proctoring session shuru karo — cameras on!"
    },
    startSession: {
      personality: "session start! ab koi cheat nahi karega"
    },
    faceAnalysis: {
      personality: "chehra verify karo — asli banda hai ya duplicate?"
    },
    analyzeFace: {
      personality: "face scan karo, dekhain match hota hai ya nahi"
    },
    lockdownValidation: {
      personality: "browser locked hai? check karo sab sahi hai"
    },
    studentIntegrityGrid: {
      personality: "kon saaf hai, kon nahi — integrity report card"
    },
    proctorEventLog: {
      personality: "exam ke doran jo bhi hua, sab yahan likha hai"
    },
    noEventsDesc: {
      personality: "all quiet on the exam front — keep it up!"
    },
    defaulterPrediction: {
      personality: "fees kon nahi de raha? AI bata de ga",
      heading: {
        personality: "who's about to ghost their fees?"
      }
    },
    studentRiskLookup: {
      personality: "kisi bhi student ka risk score check karo"
    },
    lookUp: {
      personality: "search karo, dekhain kitna risk hai"
    },
    riskDrivers: {
      personality: "kya reasons hain risk ke? AI ka analysis"
    },
    generateInterventions: {
      personality: "har risk tier ke liye action plan banao"
    },
    generatePlan: {
      personality: "AI plan bana raha hai..."
    },
    cashFlowForecast: {
      personality: "aglay 6 mahine kitna paisa aane wala hai?"
    },
    noForecastData: {
      personality: "forecast data available nahi hai — abhi tak sab ne fees de di?"
    },
    healthy: {
      personality: "scene set hai, paisa aane wala hai"
    },
    caution: {
      personality: "thori tension hai — keep an eye"
    },
    atRisk: {
      personality: "bhai, yeh recovery mushkil lagti hai"
    },
    readiness: {
      personality: "how ready are we? let's find out",
      heading: {
        personality: "accreditation kitni dur hai? readiness check"
      }
    },
    computingReadiness: {
      personality: "scores calculate ho rahe hain... thora sabar"
    },
    checkFreshness: {
      personality: "evidence fresh hai ya purana? check karo"
    },
    overallScore: {
      personality: "total readiness — number dekh kar pata chalega"
    },
    daysUntilAudit: {
      personality: "audit anay mein kitna time baki?"
    },
    criteriaBreakdown: {
      personality: "har criterion ka score — kahan kami hai?"
    },
    gapAnalysis: {
      personality: "jo missing hai woh yahan likha hai"
    },
    noGapsIdentified: {
      personality: "sab complete hai — total king move! 👑"
    },
    noGapsDesc: {
      personality: "sare criteria meet ho gaye — accreditation ready!"
    },
    alertsFeed: {
      personality: "latest updates — kya badla hai?"
    },
    allClear: {
      personality: "sab clear, koi alert nahi — maze karo"
    },
    mentalHealth: {
      personality: "dil ki baat, anonymously share karo",
      heading: {
        personality: "mental health matters — let's talk about it"
      }
    },
    anonymousTicket: {
      personality: "apni baat batao, koi naam nahi puchega"
    },
    submitAnonymousTicket: {
      personality: "send karo, hum sun rahe hain"
    },
    counselorQueue: {
      personality: "counselor ke paas kitne cases hain?"
    },
    crisisTickets: {
      personality: "emergency cases — immediate action chahiye"
    },
    immediateAttention: {
      personality: "bhai, yeh urgent hai! abhi dekhna pare ga"
    },
    noActiveCrises: {
      personality: "sab stable hai, alhamdulillah!"
    },
    noPendingTickets: {
      personality: "queue empty — aaj chai peene ka din"
    },
    queueIsClear: {
      personality: "psychologist ki chutti, no tickets pending"
    },
    fileMandatoryReport: {
      personality: "official report file karni hai — serious matter"
    },
    noTicketsRequireReport: {
      personality: "sab chill hai, no mandatory escalation needed"
    },
    mandatoryReportPanel: {
      personality: "crisis cases yahan aatay hain — handle with care"
    },
    examinations: {
      personality: "exam season — papers, marks, results, sab yahan",
      heading: {
        personality: "papers set hain? exams ka time!"
      }
    },
    marksEntry: {
      personality: "student ke marks enter karo — dua karo fail na ho"
    },
    recordMarks: {
      personality: "marks save karo, phir result ready"
    },
    recording: {
      personality: "marks save ho rahe hain..."
    },
    resultPublication: {
      personality: "results publish karo — students ki dua milegi... ya bad-dua"
    },
    publishResults: {
      personality: "publish! ab koi wapas nahi badal sakta"
    },
    publishing: {
      personality: "results publish ho rahe hain..."
    },
    assessmentSchemes: {
      personality: "exam mein kya-kya count hoga? scheme dekh lo"
    },
    noSchemesDefined: {
      personality: "koi scheme nahi — bhai pehle components define karo!"
    },
    noPublicationSlots: {
      personality: "koi slot configured nahi — results kahan publish honge?"
    },
    noSchemesDesc: {
      personality: "assessment components banao, phir marks enter karo"
    },
    placement: {
      personality: "internship lag gayi? placement tracker",
      heading: {
        personality: "job lagwana hai? placement desk pe aao"
      }
    },
    placementRate: {
      personality: "kitne students ko job mili?"
    },
    studentReadiness: {
      personality: "student job ke liye ready hai? check karo"
    },
    checkReadiness: {
      personality: "skills scan karo, phir batayein kitni mehnat baqi hai"
    },
    readinessScore: {
      personality: "employment readiness — number jitna ooncha, utni achi job"
    },
    skillAssessment: {
      personality: "kaun si skill strong hai, kaun si weak"
    },
    partnerManagement: {
      personality: "companies jo students hire karti hain"
    },
    noPartnerCompanies: {
      personality: "koi company registered nahi — outreach time!"
    },
    activePlacements: {
      personality: "kaun kahan placed hai? tracker"
    },
    noActivePlacements: {
      personality: "abhi tak koi placed nahi — koi baat nahi, scene banega"
    },
    noActivePlacementsDesc: {
      personality: "placement abhi tak nahi hui? market mein bhejo inko!"
    },
    achievements: {
      personality: "students ke achievements — flex zone! 🏆"
    },
    convocations: {
      personality: "graduation ceremony — cap and gown time"
    },
    freezes: {
      personality: "semester freeze? let's pause the clock"
    },
    visitingFaculty: {
      personality: "guest faculty hire karo — marketplace open hai"
    },
    elections: {
      personality: "student elections — vote do, leader chuno"
    },
    boardPortal: {
      personality: "board of governors ka secret lair"
    },
    foreignStudents: {
      personality: "visa, immigration, sab manage karo"
    },
    voiceAssistant: {
      personality: "NidaBot se baat karo — AI assistant ready hai"
    },
    catalogIngestion: {
      personality: "course catalog upload karo — drag and drop"
    },
    classGeneration: {
      personality: "naya semester, nayi classes — auto-generate karo"
    },
    cafeteria: {
      personality: "aaj khane mein kya hai? menu check karo"
    },
    transport: {
      personality: "buses ready hain? transport management"
    },
    prereqConflicts: {
      personality: "prerequisite check — yeh course karne se pehle woh karna zaroori hai?"
    }
  },
  admin: {
    challan: {
      verified: {
        success: {
          personality: "Slip dah sah, semua okay! 👍"
        }
      }
    },
    exam: {
      fees: {
        unpaid: {
          warning: {
            personality: "Ada pelajar belum bayar yuran exam — ingatkan mereka sebelum terlepas. 📢"
          }
        }
      }
    },
    results: {
      lock: {
        confirm: "Adakah anda pasti mahu mengunci keputusan? Tindakan ini tidak boleh dibatalkan."
      }
    },
    verifications: {
      empty: {
        personality: "Semua slip dah disahkan — rekod bersih! ✨"
      }
    }
  },
  staff: {
    challan: {
      batch: {
        loading: {
          personality: "Sekumpulan slip tengah dimuatkan, sekejap ya. 📦"
        }
      },
      overwrite: {
        warning: {
          personality: "Slip lama akan diganti — pasti ke? Nanti tak boleh undo! ⚠️"
        }
      }
    },
    batches: {
      empty: {
        personality: "Tiada kumpulan slip tertunda — kerja dah habis, boleh minum teh dulu! ☕"
      }
    }
  }
};

export const personalityMessages: Record<PortalLocale, typeof en> = {
  "en-PK": en,
  "en-US": en,
  "en-GB": en,
  "ar-AE": ar,
  "ms-MY": ms,
};
