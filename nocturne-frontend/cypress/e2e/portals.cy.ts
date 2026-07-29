describe("Nocturne portals", () => {
  it("renders student and faculty portal screens", () => {
    cy.visit("/");
    cy.contains("h1", "Dashboard").should("exist");
    cy.contains("button", "Faculty").click();
    cy.contains("button", "Marks Entry").click();
    cy.contains("h1", "Marks Entry").should("exist");
  });
});
