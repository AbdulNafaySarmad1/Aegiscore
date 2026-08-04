self.addEventListener("online", () => {
  self.registration.sync?.register("aegiscore-attendance-sync");
});

self.addEventListener("sync", event => {
  if (event.tag === "aegiscore-attendance-sync") {
    event.waitUntil(fetch("/api/curriculum/attendance/offline-sync", { method: "POST", body: "[]" }));
  }
});
