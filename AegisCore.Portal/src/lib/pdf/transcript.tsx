// Academic transcript rendered with @react-pdf/renderer. Same type system as the screen
// (Libre Caslon Text for the title, IBM Plex Sans for text, IBM Plex Mono for identifiers),
// set on A4 with the register's rules. Imported lazily by TranscriptDownload.
import { Document, Page, Text, View, Image, StyleSheet, Font } from "@react-pdf/renderer";

export type TranscriptCourse = { code: string; title: string; credits: number; grade: string | null; status: string };
/** Institution identity from tenant config (see TenantPreferencesProvider). Images must be data URLs or same-origin. */
export type TranscriptInstitution = { name: string; logoUrl?: string; sealUrl?: string };
export type TranscriptData = {
  institution: TranscriptInstitution;
  student: { name: string; rollNumber: string; universityId?: string | null; program: string };
  academics: { cgpa: number; earnedCredits: number; requiredCredits: number; courses: TranscriptCourse[] };
  generatedAt: Date;
  reference: string;
  /** Only an institution-issued copy carries the seal. Student self-service downloads never set this. */
  official?: boolean;
};

let registered = false;
export function registerFonts(origin: string) {
  if (registered) return;
  registered = true;
  Font.register({ family: "Caslon", fonts: [
    { src: `${origin}/fonts/LibreCaslonText-Regular.ttf`, fontWeight: 400 },
    { src: `${origin}/fonts/LibreCaslonText-Italic.ttf`, fontWeight: 400, fontStyle: "italic" },
    { src: `${origin}/fonts/LibreCaslonText-Bold.ttf`, fontWeight: 700 },
  ] });
  Font.register({ family: "Plex", fonts: [
    { src: `${origin}/fonts/IBMPlexSans-Regular.ttf`, fontWeight: 400 },
    { src: `${origin}/fonts/IBMPlexSans-Italic.ttf`, fontWeight: 400, fontStyle: "italic" },
    { src: `${origin}/fonts/IBMPlexSans-SemiBold.ttf`, fontWeight: 600 },
  ] });
  Font.register({ family: "PlexMono", fonts: [
    { src: `${origin}/fonts/IBMPlexMono-Regular.ttf`, fontWeight: 400 },
    { src: `${origin}/fonts/IBMPlexMono-Medium.ttf`, fontWeight: 500 },
  ] });
  Font.registerHyphenationCallback(word => [word]);
}

const INK = "#1B1A18", INK2 = "#5C5A55", RULE = "#C9C9C1", BRASS = "#7D6122", LAUREL = "#2F5A3F";

const s = StyleSheet.create({
  page: { paddingTop: 56, paddingBottom: 64, paddingHorizontal: 56, fontFamily: "Plex", fontSize: 9.5, color: INK, lineHeight: 1.45 },
  letterhead: { flexDirection: "row", justifyContent: "space-between", alignItems: "flex-end", borderBottomWidth: 2, borderBottomColor: BRASS, paddingBottom: 8, marginBottom: 10 },
  letterheadLeft: { flexDirection: "row", alignItems: "flex-end", gap: 10 },
  crest: { width: 34, height: 34, objectFit: "contain" },
  sealImage: { width: 64, height: 64, objectFit: "contain", marginBottom: 6 },
  office: { fontSize: 8.5, color: INK2 },
  institution: { fontFamily: "Caslon", fontSize: 13, marginTop: 2 },
  ref: { fontFamily: "PlexMono", fontSize: 8, color: INK2, textAlign: "right" },
  titleBlock: { paddingVertical: 18, borderBottomWidth: 0.75, borderBottomColor: INK, marginBottom: 14 },
  title: { fontFamily: "Caslon", fontSize: 22, lineHeight: 1.25, textAlign: "center" },
  subtitle: { fontFamily: "Caslon", fontStyle: "italic", fontSize: 10.5, lineHeight: 1.4, color: INK2, marginTop: 6, textAlign: "center" },
  facts: { flexDirection: "row", flexWrap: "wrap", marginBottom: 16 },
  fact: { width: "50%", flexDirection: "row", paddingVertical: 3.5, borderBottomWidth: 0.5, borderBottomColor: RULE, paddingRight: 12 },
  factLabel: { width: 96, color: INK2, fontSize: 8.5 },
  factValue: { flex: 1 },
  mono: { fontFamily: "PlexMono", fontSize: 8.8 },
  sectionTitle: { fontFamily: "Caslon", fontStyle: "italic", fontSize: 11.5, marginBottom: 6, marginTop: 6 },
  table: { borderTopWidth: 0.75, borderTopColor: INK },
  row: { flexDirection: "row", borderBottomWidth: 0.5, borderBottomColor: RULE, paddingVertical: 4.5, alignItems: "flex-start" },
  head: { borderBottomWidth: 0.75, borderBottomColor: INK, paddingVertical: 4, color: INK2, fontSize: 8.5 },
  cCode: { width: 68 }, cTitle: { flex: 1, paddingRight: 8 }, cCredits: { width: 46, textAlign: "right" }, cGrade: { width: 46, textAlign: "right" }, cStatus: { width: 78, textAlign: "right", color: INK2 },
  summary: { flexDirection: "row", marginTop: 16, borderTopWidth: 0.75, borderTopColor: INK, borderBottomWidth: 0.5, borderBottomColor: RULE },
  figure: { flex: 1, paddingVertical: 8, paddingRight: 12, borderRightWidth: 0.5, borderRightColor: RULE, paddingLeft: 10 },
  figureFirst: { paddingLeft: 0 },
  figureLast: { borderRightWidth: 0 },
  figureLabel: { fontSize: 8.5, color: INK2 },
  figureValue: { fontFamily: "Caslon", fontSize: 18, marginTop: 1 },
  note: { marginTop: 18, fontSize: 8.5, color: INK2 },
  // Anchored from the top: react-pdf drops bottom-anchored fixed views on paginated pages.
  footer: { position: "absolute", left: 56, right: 56, top: 794, flexDirection: "row", justifyContent: "space-between", borderTopWidth: 0.5, borderTopColor: RULE, paddingTop: 6, fontSize: 7.5, color: INK2 },
  footerText: { fontSize: 7.5, color: INK2 },
  sealLine: { marginTop: 26, flexDirection: "row", justifyContent: "space-between", alignItems: "flex-end" },
  sealBox: { width: 150, borderTopWidth: 0.5, borderTopColor: INK, paddingTop: 4, fontSize: 8, color: INK2 },
});

function formatDate(d: Date) {
  return new Intl.DateTimeFormat("en-GB", { day: "numeric", month: "long", year: "numeric" }).format(d);
}

export function TranscriptDocument({ data }: { data: TranscriptData }) {
  const completed = data.academics.courses.filter(c => c.grade);
  const attempted = data.academics.courses.reduce((sum, c) => sum + c.credits, 0);
  // The institution's legal name comes from tenant config; until it is set the document says so
  // plainly rather than inventing a name.
  const institution = data.institution.name.trim() || "Institution name not configured";
  const crest = data.institution.logoUrl?.trim() || null;
  const seal = data.official ? data.institution.sealUrl?.trim() || null : null;
  return (
    <Document title={`Academic transcript — ${data.student.name}`} author={institution} subject="Academic transcript" creator="AegisCore">
      <Page size="A4" style={s.page}>
        <View style={s.letterhead} fixed>
          <View style={s.letterheadLeft}>
            {crest && <Image style={s.crest} src={crest} />}
            <View>
              <Text style={s.office}>Office of the Registrar</Text>
              <Text style={s.institution}>{institution}</Text>
            </View>
          </View>
          <View>
            <Text style={s.ref}>Ref {data.reference}</Text>
            <Text style={s.ref}>{formatDate(data.generatedAt)}</Text>
          </View>
        </View>

        <View style={s.titleBlock}>
          <Text style={s.title}>Academic Transcript</Text>
          <Text style={s.subtitle}>{data.official ? "Official copy issued from the university record." : "Student copy. An official copy bearing the university seal is issued by the Examinations office."}</Text>
        </View>

        <View style={s.facts}>
          <View style={s.fact}><Text style={s.factLabel}>Student</Text><Text style={s.factValue}>{data.student.name}</Text></View>
          <View style={s.fact}><Text style={s.factLabel}>Roll number</Text><Text style={[s.factValue, s.mono]}>{data.student.rollNumber}</Text></View>
          <View style={s.fact}><Text style={s.factLabel}>Programme</Text><Text style={s.factValue}>{data.student.program}</Text></View>
          <View style={s.fact}><Text style={s.factLabel}>Student number</Text><Text style={[s.factValue, s.mono]}>{data.student.universityId ?? "—"}</Text></View>
        </View>

        <Text style={s.sectionTitle}>Record of courses</Text>
        <View style={s.table}>
          <View style={[s.row, s.head]} fixed>
            <Text style={s.cCode}>Code</Text>
            <Text style={s.cTitle}>Course</Text>
            <Text style={s.cCredits}>Credits</Text>
            <Text style={s.cGrade}>Grade</Text>
            <Text style={s.cStatus}>Status</Text>
          </View>
          {data.academics.courses.map((c, i) => (
            <View style={s.row} key={`${c.code}-${i}`} wrap={false}>
              <Text style={[s.cCode, s.mono]}>{c.code}</Text>
              <Text style={s.cTitle}>{c.title}</Text>
              <Text style={[s.cCredits, s.mono]}>{c.credits}</Text>
              <Text style={[s.cGrade, s.mono, c.grade ? {} : { color: INK2 }]}>{c.grade ?? "—"}</Text>
              <Text style={s.cStatus}>{c.status}</Text>
            </View>
          ))}
        </View>

        <View style={s.summary} wrap={false}>
          <View style={[s.figure, s.figureFirst]}><Text style={s.figureLabel}>Cumulative GPA</Text><Text style={[s.figureValue, { color: LAUREL }]}>{data.academics.cgpa.toFixed(2)}</Text></View>
          <View style={s.figure}><Text style={s.figureLabel}>Credits earned</Text><Text style={s.figureValue}>{data.academics.earnedCredits} / {data.academics.requiredCredits}</Text></View>
          <View style={s.figure}><Text style={s.figureLabel}>Credits attempted</Text><Text style={s.figureValue}>{attempted}</Text></View>
          <View style={[s.figure, s.figureLast]}><Text style={s.figureLabel}>Courses completed</Text><Text style={s.figureValue}>{completed.length} of {data.academics.courses.length}</Text></View>
        </View>

        <Text style={s.note}>
          Grades are recorded on the university{'’'}s approved scale. This transcript reflects the student record as held by the
          university on the date shown. Alterations of any kind invalidate the document.
        </Text>

        <View style={s.sealLine} wrap={false}>
          <View style={s.sealBox}><Text>Controller of Examinations</Text></View>
          {seal && <Image style={s.sealImage} src={seal} />}
          <View style={s.sealBox}><Text>Registrar</Text></View>
        </View>

        <View style={s.footer} fixed>
          <Text style={s.footerText}>Generated by AegisCore on {formatDate(data.generatedAt)} for {data.student.name}</Text>
          <Text style={s.footerText} render={({ pageNumber, totalPages }) => `Page ${pageNumber} of ${totalPages}`} fixed />
        </View>
      </Page>
    </Document>
  );
}
