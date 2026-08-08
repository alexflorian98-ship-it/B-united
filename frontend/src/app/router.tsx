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
import { AdminBillingListPage } from "../modules/admin/billing/AdminBillingListPage";
import { AdminBillingSubscriptionDetailPage } from "../modules/admin/billing/AdminBillingSubscriptionDetailPage";
import { RequireAuth } from "../shared/auth/RequireAuth";
import { RequireGuest } from "../shared/auth/RequireGuest";
import { RequirePermission } from "../shared/auth/RequirePermission";
import { WellKnownPermissions } from "../shared/permissions/wellKnownPermissions";
import { ComingSoonPage } from "./screens/ComingSoonPage";
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
 * The full route tree from §40/§45's navigation, not just the Phase 1 slice: every nav
 * destination resolves to something (either the real Phase 1 screen or `ComingSoonPage`), so
 * `ClientLayout`/`AdminLayout` — already built against the complete nav — never link anywhere
 * broken. Admin routes are gated on `content.create` (only Expert/Administrator hold it), the
 * closest current proxy for "not a plain Client" until real role-based admin access lands.
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
          <Route path="/events" element={<ComingSoonPage titleKey="nav.events" />} />
          <Route path="/community" element={<ComingSoonPage titleKey="nav.community" />} />
          <Route path="/guidance" element={<GuidanceHomePage />} />
          <Route path="/guidance/:questionnaireId/fill" element={<QuestionnaireFillPage />} />
          <Route path="/guidance/submissions/:submissionId" element={<SubmissionStatusPage />} />
          <Route path="/billing" element={<BillingPage />} />
        </Route>

        {/* The player is deliberately outside ClientLayout — its own immersive 3-pane/drawer
            layout (P2.22), not nested inside the persistent sidebar nav. */}
        <Route path="/programs/:slug/learn/:contentItemId" element={<ProgramPlayerPage />} />
      </Route>

      <Route element={<RequirePermission permission={WellKnownPermissions.ContentCreate} />}>
        <Route element={<AdminLayoutRoute />}>
          <Route path="/admin" element={<AdminHomePage />} />
          <Route path="/admin/programs" element={<AdminProgramListPage />} />
          <Route path="/admin/programs/new" element={<AdminNewProgramPage />} />
          <Route path="/admin/programs/:programId" element={<AdminProgramEditorPage />} />
          <Route path="/admin/questionnaires" element={<AdminQuestionnaireListPage />} />
          <Route path="/admin/questionnaires/new" element={<AdminNewQuestionnairePage />} />
          <Route path="/admin/questionnaires/queue" element={<ExpertQueuePage />} />
          <Route path="/admin/questionnaires/queue/:submissionId" element={<ExpertSubmissionPage />} />
          <Route path="/admin/questionnaires/:questionnaireId" element={<AdminQuestionnaireEditorPage />} />
          <Route path="/admin/events" element={<ComingSoonPage titleKey="nav.events" />} />
          <Route path="/admin/community" element={<ComingSoonPage titleKey="nav.community" />} />
          <Route path="/admin/subscribers" element={<ComingSoonPage titleKey="nav.subscribers" />} />
          <Route path="/admin/notifications" element={<ComingSoonPage titleKey="nav.notifications" />} />
          <Route path="/admin/audit" element={<ComingSoonPage titleKey="nav.audit" />} />
          <Route path="/admin/settings" element={<ComingSoonPage titleKey="nav.settings" />} />

          <Route element={<RequirePermission permission={WellKnownPermissions.BillingManage} />}>
            <Route path="/admin/billing" element={<AdminBillingListPage />} />
            <Route path="/admin/billing/:subscriptionId" element={<AdminBillingSubscriptionDetailPage />} />
          </Route>
        </Route>
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}
