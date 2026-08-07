# ADR-008: Transactional Outbox Usage

## Status

Accepted

## Context

(To be expanded during Phase 1 architecture review — see prompt section 74/75.)

## Decision

A transactional outbox is used only for cross-module events where failure or retry matters (SubscriptionActivated, SubscriptionExpired, PaymentFailed, QuestionnaireSubmitted, GuidancePublished, EventPublished, EventRegistrationCreated). Trivial synchronous in-process calls do not go through the outbox.

## Consequences

(To be documented alongside the related implementation phase.)
