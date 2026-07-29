describe('Nocturne Mobile', () => {
  beforeAll(async () => {
    await device.launchApp();
  });

  it('opens the shared mobile shell', async () => {
    await expect(element(by.text('Student'))).toBeVisible();
    await expect(element(by.text('Dashboard'))).toBeVisible();
  });
});
