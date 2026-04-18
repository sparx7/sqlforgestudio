# SQL Forge Studio 1.0
Inspired by Chat2DB. Coding Agent: Google/Gemma 4.0
It is an AI-Augmented Database Management System Designed and developed a desktop-native Database IDE focused on data privacy, offline capability, and workflow acceleration. The application bridges the gap between traditional database management and modern generative AI by allowing users to query complex schemas using natural language.

By integrating directly with LM Studio's REST API, the tool processes all AI requests locally, ensuring zero data leakage—a critical requirement for sensitive or air-gapped environments. The architecture features a custom-built, token-optimized schema caching engine that condenses massive database structures into AI-friendly blueprints, resulting in lightning-fast response times. The UI is built entirely in WPF with a custom, high-contrast theming engine, offering a professional, distraction-free environment for data analysis, complete with automated history archiving and HTML/PDF export capabilities.

Key Features (For your GitHub README.md)

1. Local AI Integration (Zero-Cloud)
   - Interfaces directly with local LLMs (Llama 3, Phi-3, Qwen) via LM Studio.
   - Ensures complete data privacy; no schemas, queries, or results ever leave the local machine.
   - Dynamic temperature controls (Strict 0.0 for code generation, creative 0.6 for data summaries).
2. Multi-Engine Architecture
   - Universal connectivity to PostgreSQL, MySQL, SQLite, and Oracle.
   - Dynamic UI that adapts connection parameters based on the selected database engine (e.g., local file browsing for SQLite).
   - Persistent Connection Profile manager for one-click server switching.
3. Smart Schema Optimization & Caching
    - Automatically extracts and sanitizes database schemas (stripping system tables and recycle bins).
    - Condenses complex schemas into ultra-dense, token-efficient formats to prevent LLM hallucination and reduce memory load.
    - Caches schema structures locally using SHA-256 hashing for instant load times upon reconnection.
4. Automated Insights & Exporting
   - Translates raw SQL data grids into human-readable, AI-generated HTML reports.
   - Built-in export tools to save analysis as standalone .html files or print directly to PDF.
5. Custom UI
   - Built from the ground up in C# WPF without relying on third-party UI frameworks.
   - Custom control templates (including native dropdowns and sweeping progress bars).
   - High-contrast Dark and Light modes engineered for professional readability.
6. Invisible Auditing & Archiving
   - Automatically archives daily system logs.
   - Silently saves chronological chat histories (User Prompt + AI SQL) to local text files for easy reference and auditing.

Tech Stack Used:
    . Frontend: C#, WPF, XAML
    . Backend: ADO.NET (Npgsql, MySql.Data, System.Data.SQLite, Oracle.ManagedDataAccess.Core)
    . State Management: Local JSON Configs (config.json, connections.json)































































































































































































































  

