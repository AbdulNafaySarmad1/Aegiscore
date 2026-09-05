import Link from "next/link";

export default function NotFound() {
  return <main className="error-page"><section className="error-display warning"><div className="error-display-copy"><h1>Page not found</h1><p>The page may have moved or you may not have access to it.</p></div><Link className="btn btn-primary" href="/">Back to Dashboard</Link></section></main>;
}
