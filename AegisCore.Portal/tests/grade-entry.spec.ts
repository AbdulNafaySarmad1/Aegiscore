import { test, expect } from "@playwright/test";

test("submitting an edited mark does not overwrite untouched students or assessments", async ({ page }) => {
  await page.addInitScript(() => localStorage.setItem("aegiscore_demo_session", JSON.stringify({ email: "grade-test@example.invalid", label: "Faculty", desc: "Grade Test" })));
  const writes: unknown[] = [];
  await page.route("**/api/**", async route => {
    const pathname = new URL(route.request().url()).pathname;
    let body: unknown = {};
    if (pathname === "/api/courses/my") body = [{ id: "course-1", code: "CS101", title: "Systems" }];
    if (pathname === "/api/examination/schemes/course-1") body = { id: "scheme-1", components: [{ id: "midterm", name: "Midterm", totalMarks: 30, weightPercentage: 30 }, { id: "final", name: "Final", totalMarks: 70, weightPercentage: 70 }] };
    if (pathname === "/api/attendance/roster/course-1") body = { students: [{ enrollmentId: "enrol-1", studentId: "student-1", name: "Ada Learner", universityId: "STU-001" }, { enrollmentId: "enrol-2", studentId: "student-2", name: "Ben Learner", universityId: "STU-002" }] };
    if (pathname === "/api/examination/marks" && route.request().method() === "PUT") writes.push(route.request().postDataJSON());
    await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(body) });
  });
  await page.goto("/grades");
  await page.getByRole("combobox", { name: "Course" }).selectOption("course-1");
  await page.getByRole("spinbutton", { name: "Ada Learner Midterm marks out of 30" }).fill("18");
  await page.getByRole("button", { name: "Submit All Marks" }).click();
  await expect(page.getByRole("status")).toHaveText("All marks submitted successfully.");
  expect(writes).toHaveLength(1);
  expect(writes[0]).toMatchObject({ studentId: "student-1", assessmentComponentId: "midterm", obtainedMarks: 18 });
});
