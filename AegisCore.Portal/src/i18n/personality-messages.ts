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
    "challan.paid.success": "تم تأكيد دفع إشعار الرسوم.",
    "challan.paid.success.personality":
      "تمت تصفية الإشعار — استرح الآن! 💸",
    "challan.expiry.warning": "سينتهي صلاحية إشعار الرسوم قريباً.",
    "challan.expiry.warning.personality":
      "تنبيه! صلاحية الإشعار على وشك الانتهاء — بادر بالدفع لتجنب غرامة التأخير. ⚠️",
    "challan.generated.success": "تم إنشاء إشعار الرسوم بنجاح.",
    "challan.generated.success.personality":
      "الإشعار جاهز! قم بالدفع لإتمام العملية. 📋",
    "results.published": "تم نشر نتائج الامتحانات.",
    "results.published.personality":
      "ظهرت النتائج! تفقدها الآن — نتمنى لك التوفيق. 📊",
    "dues.empty": "لا توجد مستحقات معلقة.",
    "dues.empty.personality":
      "رصيدك صفري — لا توجد أي التزامات مالية. 🔓",
    "payment.declined.error": "تم رفض الدفع من قبل بوابة الدفع.",
    "payment.declined.error.personality":
      "تم رفض المعاملة — يرجى المحاولة ببطاقة أخرى أو التواصل مع المصرف. 💳",
    "enrollment.confirmed": "تم تأكيد التسجيل للفصل الدراسي.",
    "enrollment.confirmed.personality":
      "تم التسجيل بنجاح! فصل دراسي جديد بطاقة متجددة. 🎓",
    "fees.status.loading": "جار تحميل حالة الرسوم…",
    "fees.status.loading.personality":
      "جار التحقق من رصيدك… 👀",
  },
  faculty: {
    "claim.submit.success": "تم تقديم طلب الاسترداد بنجاح.",
    "claim.submit.success.personality":
      "تم تقديم الطلب! يرجى انتظار المعالجة. 🧾",
    "payslip.ready": "قسيمة الراتب الشهرية متاحة.",
    "payslip.ready.personality":
      "قسيمة راتبك جاهزة — ثمرة جهدك الشهري. 💰",
    "leave.approved": "تمت الموافقة على طلب الإجازة.",
    "leave.approved.personality":
      "تمت الموافقة على إجازتك — استمتع بها، أنت تستحقها. 🌴",
    "claims.empty": "لا توجد طلبات استرداد سابقة أو معلقة.",
    "claims.empty.personality":
      "لا توجد طلبات — إما أنها منسية أو تمت تسويتها بالكامل. 🧹",
    "payslip.loading": "جار تحميل تفاصيل قسيمة الراتب…",
    "payslip.loading.personality":
      "جار استرداد مستحقاتك الشهرية… 💼",
  },
  academic: {
    "budget.expiry.warning": "ميزانية القسم على وشك الانتهاء.",
    "budget.expiry.warning.personality":
      "الميزانية على وشك الانتهاء — استخدمها قبل فوات الأوان! 📉",
    "faculty.claim.approved":
      "تمت الموافقة على طلب استرداد عضو هيئة التدريس.",
    "faculty.claim.approved.personality":
      "تمت الموافقة على طلب عضو هيئة التدريس — حان وقت الصرف! ✅",
    "approvals.empty": "لا توجد موافقات معلقة.",
    "approvals.empty.personality":
      "جميع المهام منجزة — لا توجد موافقات معلقة. 👑",
  },
  admin: {
    "challan.verified.success": "تم التحقق من الإشعار بنجاح.",
    "challan.verified.success.personality":
      "تم التحقق من الإشعار — كل شيء سليم! 👍",
    "exam.fees.unpaid.warning":
      "هناك رسوم امتحانات غير مدفوعة للطلاب.",
    "exam.fees.unpaid.warning.personality":
      "بعض الطلاب لديهم رسوم امتحانات معلقة — قم بتذكيرهم قبل فوات الأوان. 📢",
    "results.lock.confirm":
      "هل أنت متأكد من قفل النتائج؟ هذا الإجراء لا يمكن التراجع عنه.",
    "verifications.empty": "لا توجد إشعارات معلقة للتحقق.",
    "verifications.empty.personality":
      "تم التحقق من جميع الإشعارات — السجل نظيف. ✨",
  },
  staff: {
    "challan.batch.loading": "جار تحميل بيانات دفعة الإشعارات…",
    "challan.batch.loading.personality":
      "جار تحميل مجموعة الإشعارات — يرجى الانتظار قليلاً. 📦",
    "challan.overwrite.warning":
      "سيؤدي هذا الإجراء إلى استبدال سجلات الإشعارات الحالية.",
    "challan.overwrite.warning.personality":
      "سيتم استبدال الإشعارات القديمة — هل أنت متأكد؟ لا يمكن التراجع! ⚠️",
    "batches.empty": "لا توجد دفعات إشعارات للمعالجة.",
    "batches.empty.personality":
      "لا توجد دفعات معلقة — انتهى العمل، حان وقت الاستراحة. ☕",
  },
};

const ms = {
  student: {
    "challan.paid.success": "Pembayaran slip yuran disahkan.",
    "challan.paid.success.personality":
      "Slip dah clear, boleh rileks sekarang! 💸",
    "challan.expiry.warning":
      "Slip yuran anda akan tamat tempoh tidak lama lagi.",
    "challan.expiry.warning.personality":
      "Eh! Slip nak tamat dah — cepat bayar sebelum kena denda lewat! ⚠️",
    "challan.generated.success": "Slip yuran berjaya dijana.",
    "challan.generated.success.personality":
      "Slip dah siap! Bayar je sekarang, mudah je. 📋",
    "results.published":
      "Keputusan peperiksaan anda telah diterbitkan.",
    "results.published.personality":
      "Result dah keluar! Cepat check — semoga cemerlang! 📊",
    "dues.empty": "Tiada tunggakan yuran.",
    "dues.empty.personality":
      "Baki kosong, hati senang. Semua settle! 🔓",
    "payment.declined.error":
      "Pembayaran ditolak oleh gerbang pembayaran.",
    "payment.declined.error.personality":
      "Bayaran tak lepas — cuba lagi guna kad lain, ya. 💳",
    "enrollment.confirmed": "Pendaftaran semester disahkan.",
    "enrollment.confirmed.personality":
      "Daftar dah selesai! Semester baru, semangat baru — jom! 🎓",
    "fees.status.loading": "Memuatkan status yuran…",
    "fees.status.loading.personality":
      "Tengah check baki anda… 👀",
  },
  faculty: {
    "claim.submit.success":
      "Tuntutan pembayaran balik berjaya dihantar.",
    "claim.submit.success.personality":
      "Tuntutan dah hantar! Tunggu duit masuk je sekarang. 🧾",
    "payslip.ready": "Slip gaji bulanan anda tersedia.",
    "payslip.ready.personality":
      "Hasil penat lelah sebulan dah sampai — slip gaji dah siap! 💰",
    "leave.approved": "Permohonan cuti anda telah diluluskan.",
    "leave.approved.personality":
      "Cuti dah lulus! Enjoy, memang layak pun. 🌴",
    "claims.empty":
      "Tiada tuntutan pembayaran balik terdahulu atau tertunda.",
    "claims.empty.personality":
      "Takde tuntutan langsung — semua dah clear ke atau lupa nak hantar? 🧹",
    "payslip.loading": "Memuatkan butiran slip gaji…",
    "payslip.loading.personality":
      "Tengah tarik rezeki bulanan anda… 💼",
  },
  academic: {
    "budget.expiry.warning":
      "Belanjawan jabatan akan tamat tidak lama lagi.",
    "budget.expiry.warning.personality":
      "Bajet nak tamat dah — guna sebelum terlambat! 📉",
    "faculty.claim.approved":
      "Tuntutan pembayaran balik pensyarah telah diluluskan.",
    "faculty.claim.approved.personality":
      "Tuntutan pensyarah dah lulus — masa untuk keluarkan duit! ✅",
    "approvals.empty": "Tiada kelulusan tertunda.",
    "approvals.empty.personality":
      "Semua kerja dah settle — takde kelulusan tertunda. Mantap! 👑",
  },
  admin: {
    "challan.verified.success": "Slip yuran berjaya disahkan.",
    "challan.verified.success.personality":
      "Slip dah sah, semua okay! 👍",
    "exam.fees.unpaid.warning":
      "Terdapat yuran peperiksaan pelajar yang belum dibayar.",
    "exam.fees.unpaid.warning.personality":
      "Ada pelajar belum bayar yuran exam — ingatkan mereka sebelum terlepas. 📢",
    "results.lock.confirm":
      "Adakah anda pasti mahu mengunci keputusan? Tindakan ini tidak boleh dibatalkan.",
    "verifications.empty": "Tiada pengesahan slip tertunda.",
    "verifications.empty.personality":
      "Semua slip dah disahkan — rekod bersih! ✨",
  },
  staff: {
    "challan.batch.loading": "Memuatkan data kumpulan slip…",
    "challan.batch.loading.personality":
      "Sekumpulan slip tengah dimuatkan, sekejap ya. 📦",
    "challan.overwrite.warning":
      "Tindakan ini akan menimpa rekod slip sedia ada.",
    "challan.overwrite.warning.personality":
      "Slip lama akan diganti — pasti ke? Nanti tak boleh undo! ⚠️",
    "batches.empty": "Tiada kumpulan slip untuk diproses.",
    "batches.empty.personality":
      "Tiada kumpulan slip tertunda — kerja dah habis, boleh minum teh dulu! ☕",
  },
};

export const personalityMessages: Record<PortalLocale, typeof en> = {
  "en-PK": en,
  "en-US": en,
  "en-GB": en,
  "ar-AE": ar,
  "ms-MY": ms,
};
