# Business modules

This directory is the module-first home of AegisCore business capabilities.
Each module owns its domain model, vertical-slice use cases, contracts,
infrastructure adapters, API surface, database schema, and migrations.

Modules are internal engineering boundaries, not separately licensed products.
Every institution receives the complete suite.

Cross-module communication must use versioned contracts or integration events.
Direct reads of another module's tables and references to another module's
infrastructure are prohibited.
