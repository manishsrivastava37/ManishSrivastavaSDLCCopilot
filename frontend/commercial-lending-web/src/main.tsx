import { StrictMode, useEffect, useState, type FormEvent } from 'react';
import { createRoot } from 'react-dom/client';
import './styles.css';

type Application = { id: string; applicationNumber: string; borrowerLegalName: string; requestedAmount: number; currency: string; status: string; ownerId: string; };
type Workspace = { recommendations: { recommendation: string; rationale: string; analystId: string }[]; decisions: { decision: string; rationale: string; approverId: string }[]; collateral: { type: string; description: string; value: number; currency: string; status: string }[]; documents: { category: string; fileName: string; verificationStatus: string }[]; tasks: { taskType: string; status: string; assigneeId: string }[] };

function App() {
  const [applications, setApplications] = useState<Application[]>([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState('all');
  const [showCreate, setShowCreate] = useState(false);
  const [borrower, setBorrower] = useState('');
  const [amount, setAmount] = useState('');
  const [selected, setSelected] = useState<Application | null>(null);
  const [workspace, setWorkspace] = useState<Workspace | null>(null);

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
    setSelected(updated);
  };
  const openWorkspace = async (application: Application) => {
    setSelected(application);
    const response = await fetch(`/api/v1/applications/${application.id}/workspace`);
    if (response.ok) setWorkspace(await response.json() as Workspace);
  };
  const transition = async (targetStatus: string, reason: string) => {
    if (!selected) return;
    const response = await fetch(`/api/v1/applications/${selected.id}/transitions`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ targetStatus, reason, actorId: targetStatus === 'Approved' ? 'approver-demo' : 'credit-demo' }) });
    if (!response.ok) { setError('The workflow action could not be completed.'); return; }
    const updated = await response.json() as Application;
    setApplications(current => current.map(item => item.id === updated.id ? updated : item));
    setSelected(updated);
  };

  return <main className="shell">
    <header><div><p className="eyebrow">LOAN OPERATIONS</p><h1>Commercial Lending Workflow</h1></div><button type="button" onClick={() => setShowCreate(current => !current)}>Create application</button></header>
    {showCreate && <form className="create-form" onSubmit={createApplication}><label>Borrower legal name<input required value={borrower} onChange={event => setBorrower(event.target.value)} /></label><label>Requested amount<input required min="1" step="0.01" type="number" value={amount} onChange={event => setAmount(event.target.value)} /></label><button type="submit">Save application</button></form>}
    <section className="metrics" aria-label="Workflow summary"><div><span>Active applications</span><strong>{applications.length}</strong></div><div><span>Pending approval</span><strong>{applications.filter(item => item.status === 'PendingApproval').length}</strong></div><div><span>Document readiness</span><strong>--</strong></div></section>
    <section className="workspace"><div className="section-heading"><div><p className="eyebrow">WORK QUEUE</p><h2>My applications</h2></div><label>Filter <select value={statusFilter} onChange={event => setStatusFilter(event.target.value)}><option value="all">All statuses</option><option>Intake</option><option>Analysis</option><option>PendingApproval</option></select></label></div>
      {loading && <p className="state">Loading applications...</p>}{error && <p className="state error" role="alert">{error}</p>}{!loading && !error && applications.length === 0 && <p className="state">No applications assigned to you.</p>}
      {visibleApplications.length > 0 && <div className="table-wrap"><table><thead><tr><th>Application</th><th>Borrower</th><th>Amount</th><th>Status</th><th>Owner</th><th>Action</th></tr></thead><tbody>{visibleApplications.map(item => <tr key={item.id}><td><button className="link-button" type="button" onClick={() => openWorkspace(item)}>{item.applicationNumber}</button></td><td>{item.borrowerLegalName}</td><td>{new Intl.NumberFormat('en-US', { style: 'currency', currency: item.currency }).format(item.requestedAmount)}</td><td><span className="status">{item.status}</span></td><td>{item.ownerId}</td><td>{item.status === 'Intake' && <button type="button" onClick={() => submitForAnalysis(item)}>Submit for analysis</button>}{item.status === 'Analysis' && <button type="button" onClick={() => transition('PendingApproval', 'Credit analysis completed')}>Submit for approval</button>}{item.status === 'PendingApproval' && <><button type="button" onClick={() => transition('Approved', 'Approved under delegated authority')}>Approve</button><button type="button" onClick={() => transition('Declined', 'Approval declined after review')}>Decline</button></>}</td></tr>)}</tbody></table></div>}
      {selected && <section className="workspace-panel"><p className="eyebrow">APPLICATION WORKSPACE</p><h2>{selected.applicationNumber} · {selected.borrowerLegalName}</h2><div className="workspace-grid"><article><h3>Credit approval</h3><p>Status: <strong>{selected.status}</strong></p><p>Recommendation: {workspace?.recommendations[0]?.recommendation ?? 'Pending analyst assessment'}</p><p>{workspace?.recommendations[0]?.rationale ?? 'Record strengths, risks, mitigants, and rationale before approval.'}</p></article><article><h3>Collateral</h3>{workspace?.collateral.length ? workspace.collateral.map(item => <p key={item.description}>{item.type}: {item.description} · {item.currency} {item.value.toLocaleString()} · {item.status}</p>) : <p>No collateral recorded.</p>}</article><article><h3>Documents</h3>{workspace?.documents.length ? workspace.documents.map(item => <p key={item.fileName}>{item.category}: {item.fileName} · {item.verificationStatus}</p>) : <p>No documents registered.</p>}</article><article><h3>Work history</h3>{workspace?.tasks.length ? workspace.tasks.map(item => <p key={item.taskType}>{item.taskType} · {item.status} · {item.assigneeId}</p>) : <p>No open tasks.</p>}</article></div></section>}
    </section>
  </main>;
}

createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>);
