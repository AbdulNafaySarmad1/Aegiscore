// The end-of-semester course and teacher evaluation used in the course-evaluation recording, as the
// registrar would paste it into AegisCore's survey builder: "# Section" lines, one question per
// line, "[comment]" for free text, "[PLO4]" to count a rating directly towards a programme outcome.
// 73 fixed questions; each student also rates the CLOs of the course being evaluated (4 for CS101
// and CS102), so every evaluation has 77 questions.

const sections = [
  ["The instructor", [
    "The instructor came to class prepared", "The instructor explained concepts clearly", "The instructor knew the subject well",
    "The instructor encouraged questions", "The instructor answered questions helpfully", "The instructor started and ended class on time",
    "The instructor was available in office hours", "The instructor treated students with respect", "The instructor used examples that helped me understand",
    "The instructor linked topics to real problems", "The instructor made the course interesting", "The instructor gave clear instructions for work",
    "The instructor used class time well", "The instructor spoke audibly and clearly", "The instructor used the board or slides effectively",
    "The instructor checked whether students understood", "The instructor was fair to all students", "The instructor encouraged independent thinking",
    "The instructor responded to emails in reasonable time", "I would take another course with this instructor",
  ]],
  ["Course content and organisation", [
    "The course objectives were made clear at the start", "The course outline was followed", "The course content was up to date",
    "The workload was appropriate for the credit hours", "The course was well organised", "Topics followed a logical order",
    "The pace of the course was right", "The course built on what I knew", "The course developed my problem-solving skills",
    "The course developed my teamwork skills [PLO4]", "The course developed my communication skills [PLO4]", "The course was relevant to my programme",
    "Lab or tutorial sessions supported the lectures", "Reading material was useful", "The course challenged me intellectually",
    "The course prepared me for later courses", "Course announcements were timely", "The course used the learning management system well",
    "I understood how the course fits my degree", "The course met my expectations",
  ]],
  ["Assessment and feedback", [
    "Assessment criteria were explained in advance", "Assignments matched the course content", "Quizzes were fair",
    "The midterm examination was fair", "Marking was consistent", "Marks were returned in reasonable time",
    "Feedback on my work was useful", "Feedback helped me improve", "Assessments tested understanding, not memorisation",
    "The grading scheme was followed", "Deadlines were reasonable", "Academic integrity rules were explained",
    "Group work was assessed fairly", "I knew how I was doing during the course", "Assessment load was spread across the semester",
  ]],
  ["Learning resources and environment", [
    "The classroom was suitable for learning", "Lab equipment worked", "Software needed for the course was available",
    "The library had the books I needed", "Online resources were accessible", "The internet connection on campus was reliable",
    "Teaching assistants were helpful", "Class size allowed participation", "The timetable slot suited learning",
    "Accessibility needs were accommodated", "Course materials were available on time", "Study space was available when I needed it",
    "Printing and photocopying were available", "The course was a good use of university resources",
  ]],
  ["Overall", [
    "Overall, I am satisfied with this course", "I would recommend this course to other students", "This course increased my interest in the subject",
    "What should the university keep or change about this course? [comment]",
  ]],
];

export const PASTE = sections.map(([title, lines]) => [`# ${title}`, ...lines].join("\n")).join("\n");
export const FIXED = sections.reduce((n, [, lines]) => n + lines.length, 0);
export const COMMENTS = sections.reduce((n, [, lines]) => n + lines.filter(l => l.endsWith("[comment]")).length, 0);
if (FIXED !== 73) throw new Error(`The survey must have 73 fixed questions; it has ${FIXED}.`);
