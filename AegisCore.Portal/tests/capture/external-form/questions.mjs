// The 77-question course and teacher evaluation used by the test form. Its shape follows a typical
// university end-of-semester survey: which course, which semester, four CLO self-assessments, then
// blocks of Likert items about the instructor, the course, assessment and resources, an overall
// item and a free-text comment. MAPPING says what each question means in AegisCore — it is loaded
// through the mapping screen's "load the question list" path, the way an IT Lead would load a real
// form's 77 questions.

export const FORM_ID = "260000000077";
export const FORM_TITLE = "Course & Teacher Evaluation — Fall 2026 (TEST)";
export const CHOICES = ["Strongly Disagree", "Disagree", "Neutral", "Agree", "Strongly Agree"];

const clo = n => `I can do what Course Learning Outcome ${n} in the course outline describes (CLO${n})`;
const blocks = [
  ["Instructor", "B. The instructor", [
    "The instructor came to class prepared", "The instructor explained concepts clearly", "The instructor knew the subject well",
    "The instructor encouraged questions", "The instructor answered questions helpfully", "The instructor started and ended class on time",
    "The instructor was available in office hours", "The instructor treated students with respect", "The instructor used examples that helped me understand",
    "The instructor linked topics to real problems", "The instructor made the course interesting", "The instructor gave clear instructions for work",
    "The instructor used class time well", "The instructor spoke audibly and clearly", "The instructor used the board or slides effectively",
    "The instructor checked whether students understood", "The instructor was fair to all students", "The instructor encouraged independent thinking",
    "The instructor responded to emails in reasonable time", "I would take another course with this instructor",
  ]],
  ["Course", "C. Course content and organisation", [
    "The course objectives were made clear at the start", "The course outline was followed", "The course content was up to date",
    "The workload was appropriate for the credit hours", "The course was well organised", "Topics followed a logical order",
    "The pace of the course was right", "The course built on what I knew", "The course developed my problem-solving skills",
    "The course developed my teamwork skills", "The course developed my communication skills", "The course was relevant to my programme",
    "Lab or tutorial sessions supported the lectures", "Reading material was useful", "The course challenged me intellectually",
    "The course prepared me for later courses", "Course announcements were timely", "The course used the learning management system well",
    "I understood how the course fits my degree", "The course met my expectations",
  ]],
  ["Assessment", "D. Assessment and feedback", [
    "Assessment criteria were explained in advance", "Assignments matched the course content", "Quizzes were fair",
    "The midterm examination was fair", "Marking was consistent", "Marks were returned in reasonable time",
    "Feedback on my work was useful", "Feedback helped me improve", "Assessments tested understanding, not memorisation",
    "The grading scheme was followed", "Deadlines were reasonable", "Academic integrity rules were explained",
    "Group work was assessed fairly", "I knew how I was doing during the course", "Assessment load was spread across the semester",
  ]],
  ["Resources", "E. Learning resources and environment", [
    "The classroom was suitable for learning", "Lab equipment worked", "Software needed for the course was available",
    "The library had the books I needed", "Online resources were accessible", "The internet connection on campus was reliable",
    "Teaching assistants were helpful", "Class size allowed participation", "The timetable slot suited learning",
    "Accessibility needs were accommodated", "Course materials were available on time", "Study space was available when I needed it",
    "Printing and photocopying were available", "The course was a good use of university resources",
  ]],
];

const questions = [
  { n: 1, name: "courseName", type: "course", section: "A. About this evaluation", text: "Which course is this evaluation for?", options: ["CS101 - Programming Fundamentals", "CS102 - Object-Oriented Programming"] },
  { n: 2, name: "semester", type: "semester", section: "A. About this evaluation", text: "Semester", options: ["Fall 2026"] },
  ...[1, 2, 3, 4].map((c, i) => ({ n: 3 + i, name: `clo${c}Achieved`, type: "rating", section: "A. Course learning outcomes", text: clo(c), map: ["Clo", `CLO${c}`] })),
];
let n = 7;
for (const [category, section, items] of blocks)
  for (const text of items) {
    const name = text.replace(/^(The|I|Course)\s+/i, "").split(/\s+/).slice(0, 3).map((w, i) => (i === 0 ? w.toLowerCase() : w[0].toUpperCase() + w.slice(1).toLowerCase())).join("").replace(/[^A-Za-z0-9]/g, "");
    questions.push({ n, name, type: "rating", section, text, map: ["Satisfaction", category] });
    n++;
  }
questions.push({ n: n++, name: "overallSatisfied", type: "rating", section: "F. Overall", text: "Overall, I am satisfied with this course", map: ["Satisfaction", "Overall"] });
questions.push({ n: n++, name: "comments", type: "text", section: "F. Overall", text: "Any other comments?", map: ["Ignore"] });

export const QUESTIONS = questions;
if (QUESTIONS.length !== 77) throw new Error(`The evaluation must have 77 questions; it has ${QUESTIONS.length}.`);

/** field id, question text, meaning, CLO code or heading — the format the mapping screen accepts. */
export const MAPPING = QUESTIONS.map(q => {
  const target = q.type === "course" ? ["Course"] : q.type === "semester" ? ["Semester"] : q.map;
  return [`q${q.n}_${q.name}`, q.text.replace(/,/g, ";"), ...target].join(", ");
}).join("\n");
