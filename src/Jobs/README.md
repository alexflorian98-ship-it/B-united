# Jobs

Hangfire background job definitions: event reminders (24h / 1h before),
subscription grace-period checks, notification dispatch and other
recurring/deferred work. Jobs must be idempotent, retryable and observable.
