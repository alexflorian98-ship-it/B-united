import { lazy, Suspense } from "react";
import { useTranslation } from "react-i18next";
import { Outlet, Route, Routes } from "react-router-dom";
import { AdminLayout } from "../layouts/AdminLayout";
import { ClientLayout } from "../layouts/ClientLayout";
import { RequireAnyPermission } from "../shared/auth/RequireAnyPermission";
import { RequireAuth } from "../shared/auth/RequireAuth";
import { RequireGuest } from "../shared/auth/RequireGuest";
import { RequirePermission } from "../shared/auth/RequirePermission";
import { WellKnownPermissions } from "../shared/permissions/wellKnownPermissions";
import { ADMIN_NAV_PERMISSIONS, ADMIN_SHELL_PERMISSIONS } from "../layouts/navigation";

// Every route screen is route-level code-split (React.lazy + Suspense) instead of bundled into
// the single entry chunk: the admin surface alone (program/questionnaire/event/chat authoring,
// audit, client management) is never touched by a client-only user, and vice versa for the
// client experience an admin-only account never opens. This keeps the initial chunk close to
// what any single session actually needs instead of shipping every screen up front.
const AdminHomePage = lazy(() => import("../modules/admin/AdminHomePage").then((m) => ({ default: m.AdminHomePage })));
const AdminNewProgramPage = lazy(() => import("../modules/admin/content/AdminNewProgramPage").then((m) => ({ default: m.AdminNewProgramPage })));
const AdminProgramEditorPage = lazy(() => import("../modules/admin/content/AdminProgramEditorPage").then((m) => ({ default: m.AdminProgramEditorPage })));
const AdminProgramListPage = lazy(() => import("../modules/admin/content/AdminProgramListPage").then((m) => ({ default: m.AdminProgramListPage })));
const ConfirmPasswordResetPage = lazy(() => import("../modules/auth/ConfirmPasswordResetPage").then((m) => ({ default: m.ConfirmPasswordResetPage })));
const LoginPage = lazy(() => import("../modules/auth/LoginPage").then((m) => ({ default: m.LoginPage })));
const RegisterPage = lazy(() => import("../modules/auth/RegisterPage").then((m) => ({ default: m.RegisterPage })));
const RequestPasswordResetPage = lazy(() => import("../modules/auth/RequestPasswordResetPage").then((m) => ({ default: m.RequestPasswordResetPage })));
const VerifyEmailPage = lazy(() => import("../modules/auth/VerifyEmailPage").then((m) => ({ default: m.VerifyEmailPage })));
const ProgramDetailPage = lazy(() => import("../modules/content/ProgramDetailPage").then((m) => ({ default: m.ProgramDetailPage })));
const ProgramPlayerPage = lazy(() => import("../modules/content/ProgramPlayerPage").then((m) => ({ default: m.ProgramPlayerPage })));
const ProgramsPage = lazy(() => import("../modules/content/ProgramsPage").then((m) => ({ default: m.ProgramsPage })));
const ClientHomePage = lazy(() => import("../modules/dashboard/ClientHomePage").then((m) => ({ default: m.ClientHomePage })));
const ProfilePage = lazy(() => import("../modules/profile/ProfilePage").then((m) => ({ default: m.ProfilePage })));
const GuidanceHomePage = lazy(() => import("../modules/questionnaires/GuidanceHomePage").then((m) => ({ default: m.GuidanceHomePage })));
const QuestionnaireFillPage = lazy(() => import("../modules/questionnaires/QuestionnaireFillPage").then((m) => ({ default: m.QuestionnaireFillPage })));
const SubmissionStatusPage = lazy(() => import("../modules/questionnaires/SubmissionStatusPage").then((m) => ({ default: m.SubmissionStatusPage })));
const AdminQuestionnaireListPage = lazy(() => import("../modules/admin/questionnaires/AdminQuestionnaireListPage").then((m) => ({ default: m.AdminQuestionnaireListPage })));
const AdminNewQuestionnairePage = lazy(() => import("../modules/admin/questionnaires/AdminNewQuestionnairePage").then((m) => ({ default: m.AdminNewQuestionnairePage })));
const AdminQuestionnaireEditorPage = lazy(() => import("../modules/admin/questionnaires/AdminQuestionnaireEditorPage").then((m) => ({ default: m.AdminQuestionnaireEditorPage })));
const ExpertQueuePage = lazy(() => import("../modules/expert/ExpertQueuePage").then((m) => ({ default: m.ExpertQueuePage })));
const ExpertSubmissionPage = lazy(() => import("../modules/expert/ExpertSubmissionPage").then((m) => ({ default: m.ExpertSubmissionPage })));
const BillingPage = lazy(() => import("../modules/billing/BillingPage").then((m) => ({ default: m.BillingPage })));
const InvoiceDetailPage = lazy(() => import("../modules/billing/InvoiceDetailPage").then((m) => ({ default: m.InvoiceDetailPage })));
const AdminBillingListPage = lazy(() => import("../modules/admin/billing/AdminBillingListPage").then((m) => ({ default: m.AdminBillingListPage })));
const AdminBillingSubscriptionDetailPage = lazy(() => import("../modules/admin/billing/AdminBillingSubscriptionDetailPage").then((m) => ({ default: m.AdminBillingSubscriptionDetailPage })));
const EventsListPage = lazy(() => import("../modules/events/EventsListPage").then((m) => ({ default: m.EventsListPage })));
const EventDetailPage = lazy(() => import("../modules/events/EventDetailPage").then((m) => ({ default: m.EventDetailPage })));
const AdminEventsListPage = lazy(() => import("../modules/admin/events/AdminEventsListPage").then((m) => ({ default: m.AdminEventsListPage })));
const AdminNewEventPage = lazy(() => import("../modules/admin/events/AdminNewEventPage").then((m) => ({ default: m.AdminNewEventPage })));
const AdminEventEditorPage = lazy(() => import("../modules/admin/events/AdminEventEditorPage").then((m) => ({ default: m.AdminEventEditorPage })));
const ChatPage = lazy(() => import("../modules/chat/ChatPage").then((m) => ({ default: m.ChatPage })));
const AdminChatModerationPage = lazy(() => import("../modules/admin/chat/AdminChatModerationPage").then((m) => ({ default: m.AdminChatModerationPage })));
const AdminClientListPage = lazy(() => import("../modules/admin/users/AdminClientListPage").then((m) => ({ default: m.AdminClientListPage })));
const AdminClientDetailPage = lazy(() => import("../modules/admin/users/AdminClientDetailPage").then((m) => ({ default: m.AdminClientDetailPage })));
const AdminAuditPage = lazy(() => import("../modules/admin/audit/AdminAuditPage").then((m) => ({ default: m.AdminAuditPage })));
const ForbiddenPage = lazy(() => import("./screens/ForbiddenPage").then((m) => ({ default: m.ForbiddenPage })));
const NotFoundPage = lazy(() => import("./screens/NotFoundPage").then((m) => ({ default: m.NotFoundPage })));
const UnauthorizedPage = lazy(() => import("./screens/UnauthorizedPage").then((m) => ({ default: m.UnauthorizedPage })));

function ClientLayoutRoute() {
  return (
    <ClientLayout>
      <Outlet />
    </ClientLayout>
  );
}

function AdminLayoutRoute() {
  return (
    <AdminLayout>
      <Outlet />
    </AdminLayout>
  );
}

/** Route-transition fallback shown only for the brief window a lazy chunk takes to download —
 * matches SessionProvider's own bootstrap loading state so a route change never flashes blank. */
function RouteLoadingFallback() {
  const { t } = useTranslation();
  return (
    <div className="flex min-h-[50vh] items-center justify-center" role="status" aria-live="polite">
      <span className="text-sm text-text-muted">{t("status.loading")}</span>
    </div>
  );
}

/**
 * The full route tree from §40/§45's navigation: every remaining nav destination resolves to a
 * real screen (the last two placeholders — Notifications, Settings — were removed rather than
 * left as dead ends, see docs/IMPLEMENTATION_PLAN.md Slice A4: neither has a real persisted
 * backing feature yet, and the plan is explicit that a placeholder should be removed, not kept,
 * when that's true). The `/admin` shell opens for any user holding at least one administrative permission
 * (`ADMIN_SHELL_PERMISSIONS`); each destination inside it is further gated on its own specific
 * permission(s) (`ADMIN_NAV_PERMISSIONS`/individual `RequirePermission`s below), so a
 * specialized account (e.g. billing-only) reaches exactly the sections it holds a permission for.
 */
export function AppRouter() {
  return (
    <Suspense fallback={<RouteLoadingFallback />}>
      <Routes>
      <Route element={<RequireGuest />}>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
      </Route>

      <Route path="/verify-email" element={<VerifyEmailPage />} />
      <Route path="/password-reset/request" element={<RequestPasswordResetPage />} />
      <Route path="/password-reset/confirm" element={<ConfirmPasswordResetPage />} />
      <Route path="/forbidden" element={<ForbiddenPage />} />
      <Route path="/unauthorized" element={<UnauthorizedPage />} />

      <Route element={<RequireAuth />}>
        <Route element={<ClientLayoutRoute />}>
          <Route path="/" element={<ClientHomePage />} />
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="/programs" element={<ProgramsPage />} />
          <Route path="/programs/:slug" element={<ProgramDetailPage />} />
          <Route path="/events" element={<EventsListPage />} />
          <Route path="/events/:eventId" element={<EventDetailPage />} />
          <Route path="/guidance" element={<GuidanceHomePage />} />
          <Route path="/guidance/:questionnaireId/fill" element={<QuestionnaireFillPage />} />
          <Route path="/guidance/submissions/:submissionId" element={<SubmissionStatusPage />} />
          <Route path="/billing" element={<BillingPage />} />
          <Route path="/billing/invoices/:invoiceId" element={<InvoiceDetailPage />} />
          {/* Nested inside the persistent client nav (unlike the player below) so users can
              always reach every other destination without relying on the browser back button —
              its own internal room-sidebar (P6.12) sits inside this shell, not instead of it. */}
          <Route path="/community" element={<ChatPage />} />
        </Route>

        {/* The player is deliberately outside ClientLayout — its own immersive 3-pane/drawer
            layout (P2.22), not nested inside the persistent sidebar nav. */}
        <Route path="/programs/:slug/learn/:contentItemId" element={<ProgramPlayerPage />} />
      </Route>

      <Route element={<RequireAnyPermission permissions={ADMIN_SHELL_PERMISSIONS} />}>
        <Route element={<AdminLayoutRoute />}>
          <Route path="/admin" element={<AdminHomePage />} />

          <Route element={<RequireAnyPermission permissions={ADMIN_NAV_PERMISSIONS.programs} />}>
            <Route path="/admin/programs" element={<AdminProgramListPage />} />
            <Route path="/admin/programs/new" element={<AdminNewProgramPage />} />
            <Route path="/admin/programs/:programId" element={<AdminProgramEditorPage />} />
          </Route>

          <Route element={<RequireAnyPermission permissions={ADMIN_NAV_PERMISSIONS.questionnaires} />}>
            <Route path="/admin/questionnaires" element={<AdminQuestionnaireListPage />} />
            <Route path="/admin/questionnaires/new" element={<AdminNewQuestionnairePage />} />
            <Route path="/admin/questionnaires/queue" element={<ExpertQueuePage />} />
            <Route path="/admin/questionnaires/queue/:submissionId" element={<ExpertSubmissionPage />} />
            <Route path="/admin/questionnaires/:questionnaireId" element={<AdminQuestionnaireEditorPage />} />
          </Route>

          <Route element={<RequireAnyPermission permissions={ADMIN_NAV_PERMISSIONS.clients} />}>
            <Route path="/admin/clients" element={<AdminClientListPage />} />
            <Route path="/admin/clients/:userId" element={<AdminClientDetailPage />} />
          </Route>

          <Route element={<RequireAnyPermission permissions={ADMIN_NAV_PERMISSIONS.audit} />}>
            <Route path="/admin/audit" element={<AdminAuditPage />} />
          </Route>

          <Route element={<RequirePermission permission={WellKnownPermissions.BillingManage} />}>
            <Route path="/admin/billing" element={<AdminBillingListPage />} />
            <Route path="/admin/billing/purchases/:purchaseId" element={<AdminBillingSubscriptionDetailPage />} />
          </Route>

          <Route element={<RequirePermission permission={WellKnownPermissions.EventsManage} />}>
            <Route path="/admin/events" element={<AdminEventsListPage />} />
            <Route path="/admin/events/new" element={<AdminNewEventPage />} />
            <Route path="/admin/events/:eventId" element={<AdminEventEditorPage />} />
          </Route>

          <Route element={<RequirePermission permission={WellKnownPermissions.ChatModerate} />}>
            <Route path="/admin/community" element={<AdminChatModerationPage />} />
          </Route>
        </Route>
      </Route>

      <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </Suspense>
  );
}
