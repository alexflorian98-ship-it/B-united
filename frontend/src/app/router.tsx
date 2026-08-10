import { Outlet, Route, Routes } from "react-router-dom";
import { AdminLayout } from "../layouts/AdminLayout";
import { ClientLayout } from "../layouts/ClientLayout";
import { AdminHomePage } from "../modules/admin/AdminHomePage";
import { AdminNewProgramPage } from "../modules/admin/content/AdminNewProgramPage";
import { AdminProgramEditorPage } from "../modules/admin/content/AdminProgramEditorPage";
import { AdminProgramListPage } from "../modules/admin/content/AdminProgramListPage";
import { ConfirmPasswordResetPage } from "../modules/auth/ConfirmPasswordResetPage";
import { LoginPage } from "../modules/auth/LoginPage";
import { RegisterPage } from "../modules/auth/RegisterPage";
import { RequestPasswordResetPage } from "../modules/auth/RequestPasswordResetPage";
import { VerifyEmailPage } from "../modules/auth/VerifyEmailPage";
import { ProgramDetailPage } from "../modules/content/ProgramDetailPage";
import { ProgramPlayerPage } from "../modules/content/ProgramPlayerPage";
import { ProgramsPage } from "../modules/content/ProgramsPage";
import { ClientHomePage } from "../modules/dashboard/ClientHomePage";
import { ProfilePage } from "../modules/profile/ProfilePage";
import { GuidanceHomePage } from "../modules/questionnaires/GuidanceHomePage";
import { QuestionnaireFillPage } from "../modules/questionnaires/QuestionnaireFillPage";
import { SubmissionStatusPage } from "../modules/questionnaires/SubmissionStatusPage";
import { AdminQuestionnaireListPage } from "../modules/admin/questionnaires/AdminQuestionnaireListPage";
import { AdminNewQuestionnairePage } from "../modules/admin/questionnaires/AdminNewQuestionnairePage";
import { AdminQuestionnaireEditorPage } from "../modules/admin/questionnaires/AdminQuestionnaireEditorPage";
import { ExpertQueuePage } from "../modules/expert/ExpertQueuePage";
import { ExpertSubmissionPage } from "../modules/expert/ExpertSubmissionPage";
import { BillingPage } from "../modules/billing/BillingPage";
import { InvoiceDetailPage } from "../modules/billing/InvoiceDetailPage";
import { AdminBillingListPage } from "../modules/admin/billing/AdminBillingListPage";
import { AdminBillingSubscriptionDetailPage } from "../modules/admin/billing/AdminBillingSubscriptionDetailPage";
import { EventsListPage } from "../modules/events/EventsListPage";
import { EventDetailPage } from "../modules/events/EventDetailPage";
import { AdminEventsListPage } from "../modules/admin/events/AdminEventsListPage";
import { AdminNewEventPage } from "../modules/admin/events/AdminNewEventPage";
import { AdminEventEditorPage } from "../modules/admin/events/AdminEventEditorPage";
import { ChatPage } from "../modules/chat/ChatPage";
import { AdminChatModerationPage } from "../modules/admin/chat/AdminChatModerationPage";
import { AdminClientListPage } from "../modules/admin/users/AdminClientListPage";
import { AdminClientDetailPage } from "../modules/admin/users/AdminClientDetailPage";
import { AdminAuditPage } from "../modules/admin/audit/AdminAuditPage";
import { RequireAnyPermission } from "../shared/auth/RequireAnyPermission";
import { RequireAuth } from "../shared/auth/RequireAuth";
import { RequireGuest } from "../shared/auth/RequireGuest";
import { RequirePermission } from "../shared/auth/RequirePermission";
import { WellKnownPermissions } from "../shared/permissions/wellKnownPermissions";
import { ADMIN_NAV_PERMISSIONS, ADMIN_SHELL_PERMISSIONS } from "../layouts/navigation";
import { ForbiddenPage } from "./screens/ForbiddenPage";
import { NotFoundPage } from "./screens/NotFoundPage";
import { UnauthorizedPage } from "./screens/UnauthorizedPage";

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
  );
}
