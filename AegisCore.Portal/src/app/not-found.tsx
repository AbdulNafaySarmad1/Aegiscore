import Link from "next/link";

export default function NotFound() {
  return (
    <main id="main-content" className="error-page" tabIndex={-1}>
      <section className="error-display warning" aria-labelledby="nf-title">
        <span aria-hidden="true" className="mono" style={{ color: "var(--brass)", fontSize: "var(--t-13)", paddingTop: 6 }}>404</span>
        <div className="error-display-copy">
          <h1 id="nf-title">This page is not in the register.</h1>
          <p>The address may have changed, or your role does not include it. Use the navigation to find the module you need.</p>
        </div>
        <Link className="btn btn-primary" href="/">Go to the register</Link>
      </section>
    </main>
  );
}
