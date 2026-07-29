import { useEffect, useRef, useState } from "react";
import { DataSet, Edge, Network, Node } from "vis-network/standalone";
import "vis-network/styles/vis-network.css";

type CourseNode = { id: string; courseId: string; code: string; name: string; semester: number; theoryCredits: number; labCredits: number; clos: string[] };
type CourseEdge = { courseId: string; prerequisiteCourseId: string; relationship: string };
type Graph = { courses: CourseNode[]; prerequisites: CourseEdge[] };

const fallbackGraph: Graph = {
  courses: [
    { id: "cs101", courseId: "cs101", code: "CS101", name: "Programming", semester: 1, theoryCredits: 3, labCredits: 1, clos: ["CLO1"] },
    { id: "cs201", courseId: "cs201", code: "CS201", name: "Data Structures", semester: 3, theoryCredits: 3, labCredits: 1, clos: ["CLO2"] },
    { id: "cs301", courseId: "cs301", code: "CS301", name: "Algorithms", semester: 5, theoryCredits: 3, labCredits: 0, clos: ["CLO3"] }
  ],
  prerequisites: [
    { prerequisiteCourseId: "cs101", courseId: "cs201", relationship: "prerequisite" },
    { prerequisiteCourseId: "cs201", courseId: "cs301", relationship: "prerequisite" }
  ]
};

export function CurriculumBuilder() {
  const ref = useRef<HTMLDivElement | null>(null);
  const [selected, setSelected] = useState<CourseNode | null>(null);

  useEffect(() => {
    let network: Network | undefined;

    async function render() {
      const graph = await loadGraph();
      if (!ref.current) return;

      const nodes = new DataSet<Node>(graph.courses.map(course => ({
        id: course.courseId,
        label: `${course.code}\n${course.name}\n${course.theoryCredits}+${course.labCredits}`,
        color: semesterColor(course.semester),
        shape: "box",
        font: { size: 14 }
      })));
      const edges = new DataSet<Edge>(graph.prerequisites.map(edge => ({
        id: `${edge.prerequisiteCourseId}-${edge.courseId}`,
        from: edge.prerequisiteCourseId,
        to: edge.courseId,
        label: edge.relationship,
        arrows: "to"
      })));

      network = new Network(ref.current, { nodes, edges }, { physics: { stabilization: true }, interaction: { dragNodes: true, zoomView: true } });
      network.on("click", event => setSelected(graph.courses.find(course => course.courseId === event.nodes[0]) ?? null));
    }

    void render();
    return () => network?.destroy();
  }, []);

  return (
    <div className="curriculum-builder">
      <div ref={ref} className="network" aria-label="Curriculum prerequisite graph" />
      <aside className="course-editor" aria-label="Selected course">
        {selected ? (
          <>
            <h3>{selected.code}</h3>
            <p>{selected.name}</p>
            <label>Semester<input type="number" value={selected.semester} readOnly /></label>
            <label>Theory<input type="number" value={selected.theoryCredits} readOnly /></label>
            <label>Lab<input type="number" value={selected.labCredits} readOnly /></label>
            <span>{selected.clos.join(", ")}</span>
          </>
        ) : <p>Select a course node.</p>}
      </aside>
    </div>
  );
}

async function loadGraph(): Promise<Graph> {
  const versionId = import.meta.env.VITE_CURRICULUM_VERSION_ID;
  if (!versionId) return fallbackGraph;

  const response = await fetch(`/api/curriculum/versions/${versionId}/graph`);
  return response.ok ? response.json() : fallbackGraph;
}

function semesterColor(semester: number) {
  const colors = ["#e8f2fd", "#ecfdf3", "#fff7ed", "#f5f3ff"];
  return { background: colors[(semester - 1) % colors.length], border: "#50616f" };
}
