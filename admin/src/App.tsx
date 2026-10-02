import { FormEvent, useEffect, useState } from "react";
import { ApiError, Session, api, isAdmin, login } from "./api";

const storageKey = "tevscare-admin-session";

type View = "dashboard" | "users" | "plans" | "foods" | "guidance" | "audit";

type Dashboard = {
  totalUsers: number;
  activeUsers: number;
  newUsersLast7Days: number;
  publishedPlans: number;
  draftPlans: number;
  activeAssignments: number;
  checkInsLast7Days: number;
  weightEntriesLast7Days: number;
  waterEntriesLast7Days: number;
  database: string;
  api: string;
};

type UserRow = {
  id: string;
  fullName: string;
  email: string;
  roles: string[];
  locked: boolean;
  onboardingCompleted: boolean;
  entitlementPlan: string;
};

type UserDetail = UserRow & {
  timezone: string;
  preferredLanguage: string;
  dietaryPreference: string;
  currentWeightKg: number | null;
  targetWeightKg: number | null;
  entitlementStatus: string;
  assignedPlan: string | null;
  planStartDate: string | null;
  mealLogs: number;
  waterEntries: number;
  weightEntries: number;
  activityLogs: number;
  sleepLogs: number;
  checkIns: number;
  recentWeight: { date: string; weightKg: number }[];
};

type PlanRow = { id: string; name: string; description: string; durationDays: number; isPublished: boolean; dayCount: number; assignmentCount: number };
type FoodRow = { id: string; name: string; category: string; calories: number; referencePriceInr: number | null };
type Category = { id: string; name: string };
type Guidance = { id: string; category: string; title: string; body: string; label: string };
type Audit = { id: string; createdAtUtc: string; actorRole: string; action: string; entityName: string; detail: string | null };

function loadSession(): Session | null {
  try {
    const raw = sessionStorage.getItem(storageKey);
    return raw ? (JSON.parse(raw) as Session) : null;
  } catch {
    return null;
  }
}

export function App() {
  const [session, setSession] = useState<Session | null>(loadSession);
  const [view, setView] = useState<View>("dashboard");
  const [selectedUser, setSelectedUser] = useState<string | null>(null);

  function signIn(next: Session) {
    sessionStorage.setItem(storageKey, JSON.stringify(next));
    setSession(next);
  }

  async function signOut() {
    if (session) {
      await api(session, "/api/auth/logout", { method: "POST", body: JSON.stringify({ refreshToken: session.refreshToken }) }).catch(() => undefined);
    }
    sessionStorage.removeItem(storageKey);
    setSession(null);
  }

  if (!session) {
    return <Login onSuccess={signIn} />;
  }

  const admin = isAdmin(session);
  const views: View[] = admin ? ["dashboard", "users", "plans", "foods", "guidance", "audit"] : ["dashboard", "plans", "foods", "guidance"];

  return (
    <div className="shell">
      <nav>
        <strong>TEVSCARE</strong>
        <p className="muted">{session.user.fullName}</p>
        {views.map((item) => (
          <button key={item} type="button" aria-current={view === item ? "page" : undefined} onClick={() => { setView(item); setSelectedUser(null); }}>
            {item[0].toUpperCase() + item.slice(1)}
          </button>
        ))}
        <button type="button" onClick={signOut}>Sign out</button>
      </nav>
      <main>
        {view === "dashboard" && <DashboardView session={session} />}
        {view === "users" && <UsersView session={session} selected={selectedUser} onSelect={setSelectedUser} />}
        {view === "plans" && <PlansView session={session} />}
        {view === "foods" && <FoodsView session={session} />}
        {view === "guidance" && <GuidanceView session={session} />}
        {view === "audit" && <AuditView session={session} />}
      </main>
    </div>
  );
}

function Login({ onSuccess }: { onSuccess: (session: Session) => void }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");

  async function submit(event: FormEvent) {
    event.preventDefault();
    setError("");
    try {
      onSuccess(await login(email, password));
    } catch (reason) {
      setError(reason instanceof ApiError ? reason.message : "Unable to sign in.");
    }
  }

  return (
    <div className="login-page">
      <main className="card login-card">
        <h1>TEVSCARE desk</h1>
        <p className="muted">Admin and nutritionist sign-in. This is not a medical record.</p>
        <form onSubmit={submit}>
          <label>Email<input type="email" value={email} onChange={(event) => setEmail(event.target.value)} required /></label>
          <label>Password<input type="password" value={password} onChange={(event) => setPassword(event.target.value)} required /></label>
          {error && <p className="error">{error}</p>}
          <button className="primary" type="submit">Sign in</button>
        </form>
      </main>
    </div>
  );
}

function DashboardView({ session }: { session: Session }) {
  const [data, setData] = useState<Dashboard | null>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    api<Dashboard>(session, "/api/admin/dashboard").then(setData).catch((reason) => setError(message(reason)));
  }, [session]);

  if (error) return <p className="error">{error}</p>;
  if (!data) return <p>Loading dashboard…</p>;

  const cards: [string, number | string][] = [
    ["Users", data.totalUsers],
    ["Active accounts", data.activeUsers],
    ["New in 7 days", data.newUsersLast7Days],
    ["Published plans", data.publishedPlans],
    ["Draft plans", data.draftPlans],
    ["Active assignments", data.activeAssignments],
    ["Check-ins, 7 days", data.checkInsLast7Days],
    ["Weight logs, 7 days", data.weightEntriesLast7Days],
    ["Water logs, 7 days", data.waterEntriesLast7Days],
    ["Database", data.database],
    ["API", data.api]
  ];

  return (
    <section>
      <h1>Dashboard</h1>
      <p className="muted">Counts come from the database. Notification delivery and payments are not connected, so they are not shown.</p>
      <div className="metrics">
        {cards.map(([label, value]) => (
          <article className="panel metric" key={label}><span>{label}</span><strong>{value}</strong></article>
        ))}
      </div>
    </section>
  );
}

function UsersView({ session, selected, onSelect }: { session: Session; selected: string | null; onSelect: (id: string | null) => void }) {
  const [query, setQuery] = useState("");
  const [rows, setRows] = useState<UserRow[]>([]);
  const [detail, setDetail] = useState<UserDetail | null>(null);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");

  async function load(term = query) {
    const page = await api<{ items: UserRow[] }>(session, `/api/admin/users?query=${encodeURIComponent(term)}`);
    setRows(page.items);
  }

  useEffect(() => {
    load("").catch((reason) => setError(message(reason)));
  }, [session]);

  useEffect(() => {
    if (!selected) {
      setDetail(null);
      return;
    }
    api<UserDetail>(session, `/api/admin/users/${selected}`).then(setDetail).catch((reason) => setError(message(reason)));
  }, [selected, session]);

  async function lock(user: UserRow) {
    await api(session, `/api/admin/users/${user.id}/lock`, { method: "POST", body: JSON.stringify({ locked: !user.locked }) });
    await load();
    if (selected === user.id) onSelect(user.id);
  }

  async function createCoach(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setError("");
    setNotice("");
    try {
      await api(session, "/api/admin/nutritionists", {
        method: "POST",
        body: JSON.stringify({
          fullName: form.get("fullName"),
          email: form.get("email"),
          password: form.get("password")
        })
      });
      event.currentTarget.reset();
      setNotice("Nutritionist account created.");
      await load();
    } catch (reason) {
      setError(message(reason));
    }
  }

  return (
    <section>
      <h1>Users</h1>
      <form className="toolbar" onSubmit={(event) => { event.preventDefault(); load().catch((reason) => setError(message(reason))); }}>
        <label>Search<input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Name or email" /></label>
        <button className="primary" type="submit">Search</button>
      </form>
      {error && <p className="error">{error}</p>}
      {rows.length === 0 ? <p className="panel empty">No users match this search.</p> : (
        <div className="table-wrap">
          <table>
            <thead><tr><th>Name</th><th>Email</th><th>Roles</th><th>Plan</th><th></th></tr></thead>
            <tbody>
              {rows.map((user) => (
                <tr key={user.id}>
                  <td>{user.fullName}</td>
                  <td>{user.email}</td>
                  <td>{user.roles.join(", ")}{user.locked ? " · locked" : ""}</td>
                  <td>{user.entitlementPlan}</td>
                  <td className="row-actions">
                    <button className="ghost" type="button" onClick={() => onSelect(user.id)}>Open</button>
                    <button className="ghost" type="button" onClick={() => lock(user).catch((reason) => setError(message(reason)))}>{user.locked ? "Unlock" : "Lock"}</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {detail && (
        <article className="panel" style={{ marginTop: 16, padding: 16 }}>
          <h2>{detail.fullName}</h2>
          <p>{detail.email} · {detail.timezone} · {detail.preferredLanguage}</p>
          <p>Diet: {detail.dietaryPreference}. Weight {detail.currentWeightKg ?? "—"} kg, target {detail.targetWeightKg ?? "—"} kg.</p>
          <p>Entitlement: {detail.entitlementPlan} / {detail.entitlementStatus}. Assigned plan: {detail.assignedPlan ?? "None"}{detail.planStartDate ? ` from ${detail.planStartDate}` : ""}.</p>
          <p>Logs — meals {detail.mealLogs}, water {detail.waterEntries}, weight {detail.weightEntries}, activity {detail.activityLogs}, sleep {detail.sleepLogs}, check-ins {detail.checkIns}.</p>
          {detail.recentWeight.length === 0 ? <p className="muted">No weight history yet.</p> : (
            <ul>{detail.recentWeight.map((point) => <li key={point.date}>{point.date}: {point.weightKg} kg</li>)}</ul>
          )}
        </article>
      )}
      <h2>New nutritionist</h2>
      <form className="stack" onSubmit={createCoach}>
        <label>Name<input name="fullName" required /></label>
        <label>Email<input name="email" type="email" required /></label>
        <label>Temporary password<input name="password" type="password" required minLength={8} /></label>
        {notice && <p className="ok">{notice}</p>}
        <button className="primary" type="submit">Create nutritionist</button>
      </form>
    </section>
  );
}

function PlansView({ session }: { session: Session }) {
  const [rows, setRows] = useState<PlanRow[]>([]);
  const [error, setError] = useState("");

  async function load() {
    setRows(await api<PlanRow[]>(session, "/api/admin/plans"));
  }

  useEffect(() => { load().catch((reason) => setError(message(reason))); }, [session]);

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    await api(session, "/api/admin/diet-plans", {
      method: "POST",
      body: JSON.stringify({
        name: form.get("name"),
        description: form.get("description"),
        durationDays: Number(form.get("durationDays"))
      })
    });
    event.currentTarget.reset();
    await load();
  }

  async function publish(plan: PlanRow) {
    await api(session, `/api/admin/diet-plans/${plan.id}/publish?published=${!plan.isPublished}`, { method: "POST" });
    await load();
  }

  return (
    <section>
      <h1>Plans</h1>
      <p className="muted">A published plan is what members can be assigned. Creating a plan does not change anyone’s history.</p>
      {error && <p className="error">{error}</p>}
      {rows.length === 0 ? <p className="panel empty">No plans yet.</p> : (
        <div className="table-wrap">
          <table>
            <thead><tr><th>Name</th><th>Days</th><th>Built</th><th>Assignments</th><th>Status</th><th></th></tr></thead>
            <tbody>
              {rows.map((plan) => (
                <tr key={plan.id}>
                  <td>{plan.name}</td>
                  <td>{plan.durationDays}</td>
                  <td>{plan.dayCount}</td>
                  <td>{plan.assignmentCount}</td>
                  <td>{plan.isPublished ? "Published" : "Draft"}</td>
                  <td><button className="ghost" type="button" onClick={() => publish(plan).catch((reason) => setError(message(reason)))}>{plan.isPublished ? "Unpublish" : "Publish"}</button></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <h2>New plan</h2>
      <form className="stack" onSubmit={(event) => create(event).catch((reason) => setError(message(reason)))}>
        <label>Name<input name="name" required /></label>
        <label>Description<textarea name="description" /></label>
        <label>Duration in days<input name="durationDays" type="number" min={1} max={90} defaultValue={15} required /></label>
        <button className="primary" type="submit">Create draft</button>
      </form>
    </section>
  );
}

function FoodsView({ session }: { session: Session }) {
  const [rows, setRows] = useState<FoodRow[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [error, setError] = useState("");

  async function load() {
    const [foods, cats] = await Promise.all([
      api<FoodRow[]>(session, "/api/foods?page=1&pageSize=50"),
      api<Category[]>(session, "/api/foods/categories")
    ]);
    setRows(foods);
    setCategories(cats);
  }

  useEffect(() => { load().catch((reason) => setError(message(reason))); }, [session]);

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    await api(session, "/api/admin/foods", {
      method: "POST",
      body: JSON.stringify({
        name: form.get("name"),
        categoryId: form.get("categoryId"),
        servingLabel: form.get("servingLabel"),
        servingQuantity: Number(form.get("servingQuantity")),
        servingUnit: form.get("servingUnit"),
        calories: Number(form.get("calories")),
        proteinG: Number(form.get("proteinG")),
        carbohydratesG: Number(form.get("carbohydratesG")),
        fatG: Number(form.get("fatG")),
        fibreG: Number(form.get("fibreG")),
        groceryCategory: form.get("groceryCategory"),
        suitableFor: "All",
        referencePriceInr: form.get("referencePriceInr") ? Number(form.get("referencePriceInr")) : null,
        providerNote: form.get("providerNote") || null,
        isActive: true
      })
    });
    event.currentTarget.reset();
    await load();
  }

  return (
    <section>
      <h1>Foods</h1>
      <p className="muted">Prices stay empty until a value is entered. Provider notes are program guidance, not medical rules.</p>
      {error && <p className="error">{error}</p>}
      {rows.length === 0 ? <p className="panel empty">No foods returned.</p> : (
        <div className="table-wrap">
          <table>
            <thead><tr><th>Name</th><th>Category</th><th>Calories</th><th>Reference price (INR)</th></tr></thead>
            <tbody>
              {rows.map((food) => (
                <tr key={food.id}><td>{food.name}</td><td>{food.category}</td><td>{food.calories}</td><td>{food.referencePriceInr ?? "—"}</td></tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <h2>Add food</h2>
      <form className="stack" onSubmit={(event) => create(event).catch((reason) => setError(message(reason)))}>
        <label>Name<input name="name" required /></label>
        <label>Category<select name="categoryId" required>{categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select></label>
        <label>Serving label<input name="servingLabel" required /></label>
        <label>Serving quantity<input name="servingQuantity" type="number" step="0.1" defaultValue={1} required /></label>
        <label>Unit<input name="servingUnit" defaultValue="serving" required /></label>
        <label>Calories<input name="calories" type="number" step="0.1" required /></label>
        <label>Protein g<input name="proteinG" type="number" step="0.1" defaultValue={0} required /></label>
        <label>Carbs g<input name="carbohydratesG" type="number" step="0.1" defaultValue={0} required /></label>
        <label>Fat g<input name="fatG" type="number" step="0.1" defaultValue={0} required /></label>
        <label>Fibre g<input name="fibreG" type="number" step="0.1" defaultValue={0} required /></label>
        <label>Grocery category<select name="groceryCategory"><option>Vegetables</option><option>Fruits</option><option>Grains</option><option>Pulses</option><option>Dairy</option><option>Protein</option><option>NutsAndSeeds</option><option>Other</option></select></label>
        <label>Reference price INR<input name="referencePriceInr" type="number" step="0.01" /></label>
        <label>Provider note<textarea name="providerNote" /></label>
        <button className="primary" type="submit">Save food</button>
      </form>
    </section>
  );
}

function GuidanceView({ session }: { session: Session }) {
  const [rows, setRows] = useState<Guidance[]>([]);
  const [error, setError] = useState("");
  useEffect(() => {
    api<Guidance[]>(session, "/api/admin/guidance").then(setRows).catch((reason) => setError(message(reason)));
  }, [session]);
  return (
    <section>
      <h1>Guidance</h1>
      <p className="muted">These notes are TEVSCARE program guidance. They are not a diagnosis or a prescription.</p>
      {error && <p className="error">{error}</p>}
      {rows.length === 0 ? <p className="panel empty">No guidance rows.</p> : rows.map((item) => (
        <article className="panel" key={item.id} style={{ padding: 14, marginTop: 10 }}>
          <strong>{item.title}</strong>
          <p className="muted">{item.label} · {item.category}</p>
          <p>{item.body}</p>
        </article>
      ))}
    </section>
  );
}

function AuditView({ session }: { session: Session }) {
  const [rows, setRows] = useState<Audit[]>([]);
  const [error, setError] = useState("");
  useEffect(() => {
    api<{ items: Audit[] }>(session, "/api/admin/audit").then((page) => setRows(page.items)).catch((reason) => setError(message(reason)));
  }, [session]);
  return (
    <section>
      <h1>Audit</h1>
      {error && <p className="error">{error}</p>}
      {rows.length === 0 ? <p className="panel empty">No admin actions recorded yet.</p> : (
        <div className="table-wrap">
          <table>
            <thead><tr><th>When</th><th>Role</th><th>Action</th><th>Entity</th><th>Detail</th></tr></thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.id}><td>{new Date(row.createdAtUtc).toLocaleString()}</td><td>{row.actorRole}</td><td>{row.action}</td><td>{row.entityName}</td><td>{row.detail ?? "—"}</td></tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

function message(reason: unknown) {
  return reason instanceof ApiError ? reason.message : "Something went wrong.";
}
