self.addEventListener("online", () => {
  self.registration.sync?.register("nocturne-attendance-sync");
});

self.addEventListener("sync", event => {
  if (event.tag === "nocturne-attendance-sync") {
    event.waitUntil(fetch("/api/curriculum/attendance/offline-sync", { method: "POST", body: "[]" }));
  }
});
