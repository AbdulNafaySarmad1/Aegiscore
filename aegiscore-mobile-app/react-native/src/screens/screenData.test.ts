import { facultyScreens, studentScreens } from './screenData';

describe('mobile screen registry', () => {
  it('keeps the requested student and faculty screen counts', () => {
    expect(studentScreens).toHaveLength(9);
    expect(facultyScreens).toHaveLength(9);
  });
});
