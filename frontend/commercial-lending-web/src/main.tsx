import { StrictMode, useEffect, useState, type FormEvent } from 'react';
import { createRoot } from 'react-dom/client';
import './styles.css';

type Application = { id: string; applicationNumber: string; borrowerLegalName: string; requestedAmount: number; currency: string; status: string; ownerId: string; };

function App() {
  const [applications, setApplications] = useState<Application[]>([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState('all');
  const [showCreate, setShowCreate] = useState(false);
  const [borrower, setBorrower] = useState('');
  const [amount, setAmount] = useState('');

  useEffect(() => { fetch('/api/v1/applications').then(response => response.ok ? response.json() : Promise.reject()).then(setApplications).catch(() => setError('Applications are temporarily unavailable.')).finally(() => setLoading(false)); }, []);

  const visibleApplications = statusFilter === 'all' ? applications : applications.filter(item => item.status === statusFilter);
  const createApplication = async (event: FormEvent) => {
    event.preventDefault();
    const response = await fetch('/api/v1/applications', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ tenantId: 'tenant-demo', borrowerLegalName: borrower, requestedAmount: Number(amount), currency: 'USD', purpose: 'Working capital', ownerId: 'rm-demo' }) });
    if (!response.ok) { setError('The application could not be created.'); return; }
    const created = await response.json() as Application;
    setApplications(current => [created, ...current]); setBorrower(''); setAmount(''); setShowCreate(false);
  };
  const submitForAnalysis = async (application: Application) => {
    const response = await fetch(`/api/v1/applications/${application.id}/transitions`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ targetStatus: 'Analysis', reason: 'Application submitted for credit analysis', actorId: 'rm-demo' }) });
    if (!response.ok) { setError('The application could not be submitted.'); return; }
    const updated = await response.json() as Application;
    setApplications(current => current.map(item => item.id === updated.id ? updated : item));
  };

  return <main className="shell">
    <header><div><p className="eyebrow">LOAN OPERATIONS</p><h1>Commercial Lending Workflow</h1></div><button type="button" onClick={() => setShowCreate(current => !current)}>Create application</button></header>
    {showCreate && <form className="create-form" onSubmit={createApplication}><label>Borrower legal name<input required value={borrower} onChange={event => setBorrower(event.target.value)} /></label><label>Requested amount<input required min="1" step="0.01" type="number" value={amount} onChange={event => setAmount(event.target.value)} /></label><button type="submit">Save application</button></form>}
    <section className="metrics" aria-label="Workflow summary"><div><span>Active applications</span><strong>{applications.length}</strong></div><div><span>Pending approval</span><strong>{applications.filter(item => item.status === 'PendingApproval').length}</strong></div><div><span>Document readiness</span><strong>--</strong></div></section>
    <section className="workspace"><div className="section-heading"><div><p className="eyebrow">WORK QUEUE</p><h2>My applications</h2></div><label>Filter <select value={statusFilter} onChange={event => setStatusFilter(event.target.value)}><option value="all">All statuses</option><option>Intake</option><option>Analysis</option><option>PendingApproval</option></select></label></div>
      {loading && <p className="state">Loading applications...</p>}{error && <p className="state error" role="alert">{error}</p>}{!loading && !error && applications.length === 0 && <p className="state">No applications assigned to you.</p>}
      {visibleApplications.length > 0 && <div className="table-wrap"><table><thead><tr><th>Application</th><th>Borrower</th><th>Amount</th><th>Status</th><th>Owner</th><th>Action</th></tr></thead><tbody>{visibleApplications.map(item => <tr key={item.id}><td>{item.applicationNumber}</td><td>{item.borrowerLegalName}</td><td>{new Intl.NumberFormat('en-US', { style: 'currency', currency: item.currency }).format(item.requestedAmount)}</td><td><span className="status">{item.status}</span></td><td>{item.ownerId}</td><td>{item.status === 'Intake' && <button type="button" onClick={() => submitForAnalysis(item)}>Submit for analysis</button>}</td></tr>)}</tbody></table></div>}
    </section>
  </main>;
}

createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>);
