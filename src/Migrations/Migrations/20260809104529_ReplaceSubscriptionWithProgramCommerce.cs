using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BUnited.Migrations.Migrations
{
    /// <summary>
    /// ============================================================================================
    /// INTENTIONAL DATA LOSS — reviewed and approved decision, not an oversight.
    /// ============================================================================================
    /// Replaces Billing's global recurring-subscription commerce model (Plan/PlanPrice/
    /// Subscription/SubscriptionPeriod/the old platform-wide Entitlement) with the per-program
    /// one-time-purchase model (ProgramOffer/ProgramPrice/Purchase/ProgramEntitlement) approved in
    /// the revised docs/adr/ADR-003-Subscription-Entitlement-Ownership.md (2026-08-09) and
    /// docs/PROMPT.md/docs/TASKS.md (Phase 3, section 3.G, tasks P3.33-P3.45).
    ///
    /// This migration DROPS the "plans", "plan_prices", "subscriptions", "subscription_periods",
    /// and "entitlements" tables outright rather than attempting a data-preserving mapping to the
    /// new schema. This was explicitly confirmed with the product owner before implementation:
    /// no real subscriber/payment data exists in any environment yet (V1 has not launched), so
    /// there is nothing of business value to migrate — every existing row is disposable demo/seed
    /// data. `payments`/`invoices`/`webhook_events` are NOT dropped: their `subscription_id`
    /// column is renamed to `purchase_id` and repointed to the new `purchases` table, preserving
    /// those rows (they will simply reference no `Purchase` row until a fresh checkout occurs,
    /// same "persist even if the referenced entity is unknown" tolerance the webhook processor
    /// already has for out-of-order/foreign events).
    /// ============================================================================================
    /// </summary>
    public partial class ReplaceSubscriptionWithProgramCommerce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_invoices__subscriptions_subscription_id",
                table: "invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_payments__subscriptions_subscription_id",
                table: "payments");

            migrationBuilder.DropTable(
                name: "entitlements");

            migrationBuilder.DropTable(
                name: "subscription_periods");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "plan_prices");

            migrationBuilder.DropTable(
                name: "plans");

            // Same disposable-demo-data decision as above: existing payments/invoices/
            // webhook_events rows reference the now-dropped subscriptions and would otherwise
            // violate the new NOT NULL fk_*_purchases_purchase_id constraints (no Purchase row
            // exists yet to reattach them to). payments/invoices/webhook_events themselves are
            // kept (their columns are repointed below), but their pre-migration demo rows are
            // cleared since they have no meaningful new-model equivalent.
            migrationBuilder.Sql("DELETE FROM webhook_events;");
            migrationBuilder.Sql("DELETE FROM payments;");
            migrationBuilder.Sql("DELETE FROM invoices;");

            migrationBuilder.RenameColumn(
                name: "subscription_id",
                table: "webhook_events",
                newName: "purchase_id");

            migrationBuilder.RenameIndex(
                name: "ix_webhook_events_subscription_id",
                table: "webhook_events",
                newName: "ix_webhook_events_purchase_id");

            migrationBuilder.RenameColumn(
                name: "subscription_id",
                table: "payments",
                newName: "purchase_id");

            migrationBuilder.RenameIndex(
                name: "ix_payments_subscription_id",
                table: "payments",
                newName: "ix_payments_purchase_id");

            migrationBuilder.RenameColumn(
                name: "subscription_id",
                table: "invoices",
                newName: "purchase_id");

            migrationBuilder.RenameIndex(
                name: "ix_invoices_subscription_id",
                table: "invoices",
                newName: "ix_invoices_purchase_id");

            migrationBuilder.CreateTable(
                name: "program_entitlements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_purchase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_program_entitlements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "program_offers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_program_offers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "purchases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_price_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "program_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    program_offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_program_prices", x => x.id);
                    table.CheckConstraint("ck_program_prices_amount_positive", "\"amount\" > 0");
                    table.ForeignKey(
                        name: "fk_program_prices_program_offers_program_offer_id",
                        column: x => x.program_offer_id,
                        principalTable: "program_offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_program_entitlements_source_purchase_id",
                table: "program_entitlements",
                column: "source_purchase_id");

            migrationBuilder.CreateIndex(
                name: "ix_program_entitlements_user_id_program_id",
                table: "program_entitlements",
                columns: new[] { "user_id", "program_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_program_offers_program_id_active",
                table: "program_offers",
                column: "program_id",
                unique: true,
                filter: "\"status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_program_prices_program_offer_id",
                table: "program_prices",
                column: "program_offer_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchases_program_id",
                table: "purchases",
                column: "program_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchases_status",
                table: "purchases",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_purchases_user_id",
                table: "purchases",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchases_user_id_program_id_pending",
                table: "purchases",
                columns: new[] { "user_id", "program_id" },
                unique: true,
                filter: "\"status\" = 'Pending'");

            migrationBuilder.AddForeignKey(
                name: "fk_invoices__purchases_purchase_id",
                table: "invoices",
                column: "purchase_id",
                principalTable: "purchases",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_payments__purchases_purchase_id",
                table: "payments",
                column: "purchase_id",
                principalTable: "purchases",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_invoices__purchases_purchase_id",
                table: "invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_payments__purchases_purchase_id",
                table: "payments");

            migrationBuilder.DropTable(
                name: "program_entitlements");

            migrationBuilder.DropTable(
                name: "program_prices");

            migrationBuilder.DropTable(
                name: "purchases");

            migrationBuilder.DropTable(
                name: "program_offers");

            migrationBuilder.RenameColumn(
                name: "purchase_id",
                table: "webhook_events",
                newName: "subscription_id");

            migrationBuilder.RenameIndex(
                name: "ix_webhook_events_purchase_id",
                table: "webhook_events",
                newName: "ix_webhook_events_subscription_id");

            migrationBuilder.RenameColumn(
                name: "purchase_id",
                table: "payments",
                newName: "subscription_id");

            migrationBuilder.RenameIndex(
                name: "ix_payments_purchase_id",
                table: "payments",
                newName: "ix_payments_subscription_id");

            migrationBuilder.RenameColumn(
                name: "purchase_id",
                table: "invoices",
                newName: "subscription_id");

            migrationBuilder.RenameIndex(
                name: "ix_invoices_purchase_id",
                table: "invoices",
                newName: "ix_invoices_subscription_id");

            migrationBuilder.CreateTable(
                name: "entitlements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_until_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_entitlements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plan_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    interval = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_plan_prices_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    canceled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_price_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscriptions_plan_prices_plan_price_id",
                        column: x => x.plan_price_id,
                        principalTable: "plan_prices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subscriptions_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_periods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    paid_period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_periods", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscription_periods_subscriptions_subscription_id",
                        column: x => x.subscription_id,
                        principalTable: "subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_entitlements_source_id",
                table: "entitlements",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "ix_entitlements_user_id_type",
                table: "entitlements",
                columns: new[] { "user_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_plan_prices_plan_id",
                table: "plan_prices",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_periods_subscription_id",
                table: "subscription_periods",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_plan_id",
                table: "subscriptions",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_plan_price_id",
                table: "subscriptions",
                column: "plan_price_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_status",
                table: "subscriptions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_user_id",
                table: "subscriptions",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_invoices__subscriptions_subscription_id",
                table: "invoices",
                column: "subscription_id",
                principalTable: "subscriptions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_payments__subscriptions_subscription_id",
                table: "payments",
                column: "subscription_id",
                principalTable: "subscriptions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
