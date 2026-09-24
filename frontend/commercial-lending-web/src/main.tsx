import { StrictMode, useEffect, useState, type FormEvent } from 'react';
import { createRoot } from 'react-dom/client';
import './styles.css';

type Application = { id: string; applicationNumber: string; borrowerLegalName: string; requestedAmount: number; currency: string; status: string; ownerId: string; };
type Workspace = { recommendations: { recommendation: string; rationale: string; factors: string[]; analystId: string }[]; decisions: { decision: string; rationale: string; approverId: string }[]; verifications: { outcome: string; rationale: string; verifierId: string }[]; requiresSecondLevelVerification: boolean; collateral: { id: string; type: string; description: string; value: number; currency: string; status: string }[]; documents: { id: string; category: string; fileName: string; verificationStatus: string; findings?: string }[]; tasks: { taskType: string; status: string; assigneeId: string }[] };
const apiFetch = (input: RequestInfo | URL, init: RequestInit = {}) => {
  let demoUser = 'rm-demo';
  if (typeof init.body === 'string') {
    try {
      const payload = JSON.parse(init.body) as Record<string, unknown>;
      const actor = payload.ownerId ?? payload.actorId ?? payload.analystId ?? payload.approverId ?? payload.verifierId ?? payload.reviewerId;
      if (typeof actor === 'string' && actor.trim()) demoUser = actor;
    } catch { /* Non-JSON requests retain the default demo identity. */ }
  }
  return fetch(input, { ...init, headers: { 'X-Demo-User': demoUser, 'X-Demo-Tenant': 'tenant-demo', 'X-Demo-Roles': 'RelationshipManager,CreditAnalyst,CreditApprover,CollateralSpecialist,DocumentVerificationAnalyst,CreditVerifier', ...init.headers } });
};

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
  const [recommendation, setRecommendation] = useState('');
  const [rationale, setRationale] = useState('');
  const [factors, setFactors] = useState('');
  const [decisionRationale, setDecisionRationale] = useState('');
  const [documentFindings, setDocumentFindings] = useState('');
  const [verificationRationale, setVerificationRationale] = useState('');

  useEffect(() => { apiFetch('/api/v1/applications').then(response => response.ok ? response.json() : Promise.reject()).then(setApplications).catch(() => setError('Applications are temporarily unavailable.')).finally(() => setLoading(false)); }, []);

  const visibleApplications = statusFilter === 'all' ? applications : applications.filter(item => item.status === statusFilter);
  const createApplication = async (event: FormEvent) => {
    event.preventDefault();
    const response = await apiFetch('/api/v1/applications', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ tenantId: 'tenant-demo', borrowerLegalName: borrower, requestedAmount: Number(amount), currency: 'USD', purpose: 'Working capital', ownerId: 'rm-demo' }) });
    if (!response.ok) { setError('The application could not be created.'); return; }
    const created = await response.json() as Application;
    setApplications(current => [created, ...current]); setBorrower(''); setAmount(''); setShowCreate(false);
  };
  const submitForAnalysis = async (application: Application) => {
    const response = await apiFetch(`/api/v1/applications/${application.id}/transitions`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ targetStatus: 'Analysis', reason: 'Application submitted for credit analysis', actorId: 'rm-demo' }) });
    if (!response.ok) { setError('The application could not be submitted.'); return; }
    const updated = await response.json() as Application;
    setApplications(current => current.map(item => item.id === updated.id ? updated : item));
    setSelected(updated);
  };
  const openWorkspace = async (application: Application) => {
    setSelected(application);
    const response = await apiFetch(`/api/v1/applications/${application.id}/workspace`);
    if (response.ok) setWorkspace(await response.json() as Workspace);
  };
  const transition = async (application: Application, targetStatus: string, reason: string) => {
    const response = await apiFetch(`/api/v1/applications/${application.id}/transitions`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ targetStatus, reason, actorId: targetStatus === 'Approved' ? 'approver-demo' : 'credit-demo' }) });
    if (!response.ok) { setError('The workflow action could not be completed.'); return; }
    const updated = await response.json() as Application;
    setApplications(current => current.map(item => item.id === updated.id ? updated : item));
    setSelected(updated);
  };
  const postWorkspace = async (path: string, body: unknown, absolute = false) => {
    if (!selected) return;
    const response = await apiFetch(absolute ? `/api/v1/${path}` : `/api/v1/applications/${selected.id}/${path}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
    if (!response.ok) { const problem = await response.json().catch(() => null) as { error?: string } | null; setError(problem?.error ?? 'The workflow action could not be completed.'); return false; }
    await openWorkspace(selected); return true;
  };
  const saveRecommendation = async (event: FormEvent) => { event.preventDefault(); if (await postWorkspace('recommendations', { recommendation, rationale, factors: factors.split('\n').map(item => item.trim()).filter(Boolean), analystId: 'credit-demo' })) { setRecommendation(''); setRationale(''); setFactors(''); } };
  const saveDecision = async (decision: string) => { if (await postWorkspace('decisions', { decision, rationale: decisionRationale, approverId: 'approver-demo' })) { setDecisionRationale(''); await transition(selected!, decision, `Human approver recorded ${decision.toLowerCase()} decision`); } };
  const saveVerification = async (event: FormEvent) => { event.preventDefault(); if (await postWorkspace('credit-verification', { outcome: 'Verified', rationale: verificationRationale, verifierId: 'reviewer-demo' })) setVerificationRationale(''); };

  return <main className="shell">
    <header><div><p className="eyebrow">LOAN OPERATIONS</p><h1>Commercial Lending Workflow</h1></div><button type="button" onClick={() => setShowCreate(current => !current)}>Create application</button></header>
    {showCreate && <form className="create-form" onSubmit={createApplication}><label>Borrower legal name<input required value={borrower} onChange={event => setBorrower(event.target.value)} /></label><label>Requested amount<input required min="1" step="0.01" type="number" value={amount} onChange={event => setAmount(event.target.value)} /></label><button type="submit">Save application</button></form>}
    <section className="metrics" aria-label="Workflow summary"><div><span>Active applications</span><strong>{applications.length}</strong></div><div><span>Pending approval</span><strong>{applications.filter(item => item.status === 'PendingApproval').length}</strong></div><div><span>Documents needing review</span><strong>{workspace?.documents.filter(item => item.verificationStatus !== 'Verified').length ?? '--'}</strong></div></section>
    <section className="workspace"><div className="section-heading"><div><p className="eyebrow">WORK QUEUE</p><h2>My applications</h2></div><label>Filter <select value={statusFilter} onChange={event => setStatusFilter(event.target.value)}><option value="all">All statuses</option><option>Intake</option><option>Analysis</option><option>PendingApproval</option></select></label></div>
      {loading && <p className="state">Loading applications...</p>}{error && <p className="state error" role="alert">{error}</p>}{!loading && !error && applications.length === 0 && <p className="state">No applications assigned to you.</p>}
      {visibleApplications.length > 0 && <div className="table-wrap"><table><thead><tr><th>Application</th><th>Borrower</th><th>Amount</th><th>Status</th><th>Owner</th><th>Action</th></tr></thead><tbody>{visibleApplications.map(item => <tr key={item.id}><td><button className="link-button" type="button" onClick={() => openWorkspace(item)}>{item.applicationNumber}</button></td><td>{item.borrowerLegalName}</td><td>{new Intl.NumberFormat('en-US', { style: 'currency', currency: item.currency }).format(item.requestedAmount)}</td><td><span className="status">{item.status}</span></td><td>{item.ownerId}</td><td>{item.status === 'Intake' && <button type="button" onClick={() => submitForAnalysis(item)}>Submit for analysis</button>}{item.status === 'Analysis' && <button type="button" onClick={() => openWorkspace(item)}>Open credit review</button>}{item.status === 'PendingApproval' && <><button type="button" onClick={() => openWorkspace(item)}>Review and decide</button></>}</td></tr>)}</tbody></table></div>}
      {selected && <section className="workspace-panel"><p className="eyebrow">APPLICATION WORKSPACE</p><h2>{selected.applicationNumber} · {selected.borrowerLegalName}</h2><div className="workspace-grid"><article><h3>Credit approval</h3><p>Status: <strong>{selected.status}</strong></p>{workspace?.recommendations[0] ? <><p>Recommendation: <strong>{workspace.recommendations[0].recommendation}</strong></p><p>{workspace.recommendations[0].rationale}</p><ul>{workspace.recommendations[0].factors.map(factor => <li key={factor}>{factor}</li>)}</ul></> : <form onSubmit={saveRecommendation}><label>Recommendation<input required value={recommendation} onChange={event => setRecommendation(event.target.value)} placeholder="Approve with conditions" /></label><label>Rationale<textarea required value={rationale} onChange={event => setRationale(event.target.value)} /></label><label>Factors, one per line<textarea required value={factors} onChange={event => setFactors(event.target.value)} /></label><button type="submit">Save recommendation</button></form>}{selected.status === 'PendingApproval' && <div className="decision-form"><label>Approval rationale<textarea required value={decisionRationale} onChange={event => setDecisionRationale(event.target.value)} /></label><button type="button" onClick={() => saveDecision('Approved')}>Approve</button><button type="button" onClick={() => saveDecision('Declined')}>Decline</button></div>}</article><article><h3>Collateral</h3>{workspace?.collateral.length ? workspace.collateral.map(item => <p key={item.id}>{item.type}: {item.description} · {item.currency} {item.value.toLocaleString()} · <strong>{item.status}</strong>{item.status === 'Proposed' && <button type="button" onClick={() => postWorkspace(`collateral/${item.id}/status`, { status: 'Under Review' }, true)}>Review</button>}{item.status === 'Under Review' && <button type="button" onClick={() => postWorkspace(`collateral/${item.id}/status`, { status: 'Perfected' }, true)}>Mark perfected</button>}</p>) : <p>No collateral recorded.</p>}</article><article><h3>Documents</h3>{workspace?.documents.length ? workspace.documents.map(item => <div key={item.id}><p>{item.category}: {item.fileName} · <strong>{item.verificationStatus}</strong>{item.findings && ` · ${item.findings}`}</p>{item.verificationStatus !== 'Verified' && <><input value={documentFindings} onChange={event => setDocumentFindings(event.target.value)} placeholder="Findings (required for rejection)" /><button type="button" onClick={() => postWorkspace(`documents/${item.id}/verification`, { verificationStatus: 'Verified', reviewerId: 'ops-demo', findings: documentFindings || null }, true)}>Verify</button><button type="button" onClick={() => postWorkspace(`documents/${item.id}/verification`, { verificationStatus: 'Rejected', reviewerId: 'ops-demo', findings: documentFindings }, true)}>Reject</button></>}</div>) : <p>No documents registered.</p>}</article><article><h3>Work history</h3>{workspace?.tasks.length ? workspace.tasks.map(item => <p key={item.taskType}>{item.taskType} · {item.status} · {item.assigneeId}</p>) : <p>No open tasks.</p>}</article></div></section>}
      {selected?.status === 'Analysis' && <div className="approval-gate"><button type="button" disabled={!workspace?.recommendations[0] || (workspace.requiresSecondLevelVerification && !workspace.verifications.some(item => item.outcome === 'Verified'))} onClick={() => transition(selected, 'PendingApproval', 'Credit analysis completed')}>Submit for approval</button>{workspace?.requiresSecondLevelVerification && !workspace.verifications.some(item => item.outcome === 'Verified') && <p className="state">Complete second-level verification before submitting for approval.</p>}</div>}{workspace?.requiresSecondLevelVerification && !workspace.verifications.some(item => item.outcome === 'Verified') && <form className="verification-form" onSubmit={saveVerification}><p className="status">Second-level verification required for amounts above INR 25,000.</p><label>Verifier rationale<textarea required value={verificationRationale} onChange={event => setVerificationRationale(event.target.value)} /></label><button type="submit">Complete second-level verification</button></form>}
    </section>
  </main>;
}

createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>);
